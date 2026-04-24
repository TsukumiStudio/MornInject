using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MornLib
{
    internal static class MornInjectAutoInjector
    {
        private const string MenuPath = "Tools/自動注入 &#i";

        private const BindingFlags MemberFlags = BindingFlags.Instance
            | BindingFlags.NonPublic
            | BindingFlags.Public
            | BindingFlags.DeclaredOnly;

        [MenuItem(MenuPath)]
        private static void Inject()
        {
            var targets = CollectTargets();
            var success = 0;
            var error = 0;
            foreach (var mb in targets)
            {
                if (mb == null)
                {
                    continue;
                }

                InjectTo(mb, ref success, ref error);
            }

            MornInjectLogger.Log($"自動注入完了: 成功 {success} 件 / エラー {error} 件 (対象 MonoBehaviour {targets.Count} 個)");
        }

        private static List<MonoBehaviour> CollectTargets()
        {
            var list = new List<MonoBehaviour>();
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                list.AddRange(prefabStage.prefabContentsRoot.GetComponentsInChildren<MonoBehaviour>(true));
                return list;
            }

            var scene = SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                list.AddRange(root.GetComponentsInChildren<MonoBehaviour>(true));
            }

            return list;
        }

        private static void InjectTo(MonoBehaviour mb, ref int success, ref int error)
        {
            var so = new SerializedObject(mb);
            foreach (var field in EnumerateSerializedFields(mb.GetType()))
            {
                var result = ProcessField(mb, so, field);
                switch (result)
                {
                    case InjectResult.Changed:
                        success++;
                        break;
                    case InjectResult.Error:
                        error++;
                        break;
                    case InjectResult.Skipped:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            so.ApplyModifiedProperties();
            InvokeOnMornInject(mb);
        }

        private static IEnumerable<Type> EnumerateUserTypes(Type type)
        {
            while (type != null && type != typeof(MonoBehaviour) && type != typeof(Behaviour) && type != typeof(Component) && type != typeof(UnityEngine.Object))
            {
                yield return type;
                type = type.BaseType;
            }
        }

        private static IEnumerable<FieldInfo> EnumerateSerializedFields(Type type)
        {
            foreach (var t in EnumerateUserTypes(type))
            {
                foreach (var field in t.GetFields(MemberFlags))
                {
                    yield return field;
                }
            }
        }

        private static void InvokeOnMornInject(MonoBehaviour mb)
        {
            var types = new List<Type>();
            foreach (var t in EnumerateUserTypes(mb.GetType()))
            {
                types.Add(t);
            }

            types.Reverse();
            foreach (var type in types)
            {
                foreach (var method in type.GetMethods(MemberFlags))
                {
                    if (method.GetCustomAttribute<OnMornInjectAttribute>() == null)
                    {
                        continue;
                    }

                    if (method.GetParameters().Length > 0)
                    {
                        MornInjectLogger.LogError($"{type.Name}.{method.Name}: [OnMornInject] は引数なしのメソッドにのみ使用できます");
                        continue;
                    }

                    if (method.ReturnType != typeof(void))
                    {
                        MornInjectLogger.LogError($"{type.Name}.{method.Name}: [OnMornInject] は戻り値 void のメソッドにのみ使用できます");
                        continue;
                    }

                    try
                    {
                        method.Invoke(mb, null);
                    }
                    catch (Exception e)
                    {
                        MornInjectLogger.LogError($"{type.Name}.{method.Name}: [OnMornInject] 実行中に例外が発生しました\n{e}");
                    }
                }
            }
        }

        private static InjectResult ProcessField(MonoBehaviour mb, SerializedObject so, FieldInfo field)
        {
            if (field.GetCustomAttribute<MeAttribute>() != null)
            {
                return InjectMe(mb, so, field);
            }

            if (field.GetCustomAttribute<ChildAttribute>() != null)
            {
                return InjectChild(mb, so, field, deep: false);
            }

            if (field.GetCustomAttribute<ChildDeepAttribute>() != null)
            {
                return InjectChild(mb, so, field, deep: true);
            }

            if (field.GetCustomAttribute<ChildrensAttribute>() != null)
            {
                return InjectChildrens(mb, so, field, deep: false);
            }

            if (field.GetCustomAttribute<ChildrensDeepAttribute>() != null)
            {
                return InjectChildrens(mb, so, field, deep: true);
            }

            var findNameAttr = field.GetCustomAttribute<FindNameAttribute>();
            if (findNameAttr != null)
            {
                return InjectFindName(so, field, findNameAttr.Name);
            }

            if (field.GetCustomAttribute<FindTypeAttribute>() != null)
            {
                return InjectFindType(so, field);
            }

            if (field.GetCustomAttribute<FindsTypeAttribute>() != null)
            {
                return InjectFindsType(so, field);
            }

            var findAssetNameAttr = field.GetCustomAttribute<FindAssetNameAttribute>();
            if (findAssetNameAttr != null)
            {
                return InjectFindAssetName(so, field, findAssetNameAttr.Name);
            }

            if (field.GetCustomAttribute<FindAssetTypeAttribute>() != null)
            {
                return InjectFindAssetType(so, field);
            }

            if (field.GetCustomAttribute<FindAssetsTypeAttribute>() != null)
            {
                return InjectFindsAssetType(so, field);
            }

            return InjectResult.Skipped;
        }

        private static InjectResult InjectMe(MonoBehaviour mb, SerializedObject so, FieldInfo field)
        {
            if (!typeof(Component).IsAssignableFrom(field.FieldType))
            {
                MornInjectLogger.LogError($"{Describe(mb, field)}: [Me] は Component 派生型のフィールドにのみ使用できます");
                return InjectResult.Error;
            }

            var value = mb.GetComponent(field.FieldType);
            if (value == null)
            {
                MornInjectLogger.LogError($"{Describe(mb, field)}: [Me] {field.FieldType.Name} が自身に見つかりません");
                return InjectResult.Error;
            }

            return AssignObject(so, field, value);
        }

        private static InjectResult InjectChild(MonoBehaviour mb, SerializedObject so, FieldInfo field, bool deep)
        {
            var label = deep ? "[ChildDeep]" : "[Child]";
            if (!typeof(Component).IsAssignableFrom(field.FieldType))
            {
                MornInjectLogger.LogError($"{Describe(mb, field)}: {label} は Component 派生型のフィールドにのみ使用できます");
                return InjectResult.Error;
            }

            var components = deep
                ? GetDescendantComponents(mb, field.FieldType)
                : GetDirectChildComponents(mb, field.FieldType);
            if (components.Count == 0)
            {
                MornInjectLogger.LogError($"{Describe(mb, field)}: {label} {field.FieldType.Name} が見つかりません");
                return InjectResult.Error;
            }

            if (components.Count >= 2)
            {
                MornInjectLogger.LogError(
                    $"{Describe(mb, field)}: {label} {field.FieldType.Name} が {components.Count} 件見つかりました(1 件のみ期待)");
                return InjectResult.Error;
            }

            return AssignObject(so, field, components[0]);
        }

        private static InjectResult InjectChildrens(MonoBehaviour mb, SerializedObject so, FieldInfo field, bool deep)
        {
            var label = deep ? "[ChildrensDeep]" : "[Childrens]";
            if (!TryGetElementType(field.FieldType, out var elementType))
            {
                MornInjectLogger.LogError($"{Describe(mb, field)}: {label} は配列または List<T> 型のフィールドにのみ使用できます");
                return InjectResult.Error;
            }

            if (!typeof(Component).IsAssignableFrom(elementType))
            {
                MornInjectLogger.LogError($"{Describe(mb, field)}: {label} の要素型は Component 派生である必要があります");
                return InjectResult.Error;
            }

            var components = deep
                ? GetDescendantComponents(mb, elementType)
                : GetDirectChildComponents(mb, elementType);
            var values = new List<UnityEngine.Object>(components.Count);
            foreach (var c in components)
            {
                values.Add(c);
            }

            return AssignArray(so, field, values);
        }

        private static InjectResult InjectFindName(SerializedObject so, FieldInfo field, string name)
        {
            var mb = (MonoBehaviour)so.targetObject;
            if (!IsValidSceneFieldType(field.FieldType))
            {
                MornInjectLogger.LogError($"{Describe(mb, field)}: [FindName] は GameObject か Component 派生型のフィールドにのみ使用できます");
                return InjectResult.Error;
            }

            var matches = FindSceneTransformsByName(name);
            if (matches.Count == 0)
            {
                MornInjectLogger.LogError($"{Describe(mb, field)}: [FindName(\"{name}\")] 名前一致の GameObject が見つかりません");
                return InjectResult.Error;
            }

            if (matches.Count >= 2)
            {
                MornInjectLogger.LogError(
                    $"{Describe(mb, field)}: [FindName(\"{name}\")] 名前一致が {matches.Count} 件見つかりました(1 件のみ期待)");
                return InjectResult.Error;
            }

            if (!TryResolveSceneTarget(matches[0], field.FieldType, out var value))
            {
                MornInjectLogger.LogError(
                    $"{Describe(mb, field)}: [FindName(\"{name}\")] {field.FieldType.Name} が {matches[0].name} に付いていません");
                return InjectResult.Error;
            }

            return AssignObject(so, field, value);
        }

        private static InjectResult InjectFindType(SerializedObject so, FieldInfo field)
        {
            var mb = (MonoBehaviour)so.targetObject;
            if (!typeof(Component).IsAssignableFrom(field.FieldType))
            {
                MornInjectLogger.LogError($"{Describe(mb, field)}: [FindType] は Component 派生型のフィールドにのみ使用できます");
                return InjectResult.Error;
            }

            var matches = FindSceneComponentsByType(field.FieldType);
            if (matches.Count == 0)
            {
                MornInjectLogger.LogError($"{Describe(mb, field)}: [FindType] {field.FieldType.Name} がシーン内に見つかりません");
                return InjectResult.Error;
            }

            if (matches.Count >= 2)
            {
                MornInjectLogger.LogError(
                    $"{Describe(mb, field)}: [FindType] {field.FieldType.Name} がシーン内に {matches.Count} 件見つかりました(1 件のみ期待)");
                return InjectResult.Error;
            }

            return AssignObject(so, field, matches[0]);
        }

        private static InjectResult InjectFindsType(SerializedObject so, FieldInfo field)
        {
            var mb = (MonoBehaviour)so.targetObject;
            if (!TryGetElementType(field.FieldType, out var elementType))
            {
                MornInjectLogger.LogError($"{Describe(mb, field)}: [FindsType] は配列または List<T> 型のフィールドにのみ使用できます");
                return InjectResult.Error;
            }

            if (!typeof(Component).IsAssignableFrom(elementType))
            {
                MornInjectLogger.LogError($"{Describe(mb, field)}: [FindsType] の要素型は Component 派生である必要があります");
                return InjectResult.Error;
            }

            var matches = FindSceneComponentsByType(elementType);
            var values = new List<UnityEngine.Object>(matches.Count);
            foreach (var c in matches)
            {
                values.Add(c);
            }

            return AssignArray(so, field, values);
        }

        private static InjectResult InjectFindAssetName(SerializedObject so, FieldInfo field, string name)
        {
            var mb = (MonoBehaviour)so.targetObject;
            if (!typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
            {
                MornInjectLogger.LogError($"{Describe(mb, field)}: [FindAssetName] は UnityEngine.Object 派生型のフィールドにのみ使用できます");
                return InjectResult.Error;
            }

            var matches = FindAssetsByName(name, field.FieldType);
            if (matches.Count == 0)
            {
                MornInjectLogger.LogError($"{Describe(mb, field)}: [FindAssetName(\"{name}\")] 名前一致の {field.FieldType.Name} アセットが見つかりません");
                return InjectResult.Error;
            }

            if (matches.Count >= 2)
            {
                MornInjectLogger.LogError(
                    $"{Describe(mb, field)}: [FindAssetName(\"{name}\")] 名前一致が {matches.Count} 件見つかりました(1 件のみ期待)");
                return InjectResult.Error;
            }

            return AssignObject(so, field, matches[0]);
        }

        private static InjectResult InjectFindAssetType(SerializedObject so, FieldInfo field)
        {
            var mb = (MonoBehaviour)so.targetObject;
            if (!typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
            {
                MornInjectLogger.LogError($"{Describe(mb, field)}: [FindAssetType] は UnityEngine.Object 派生型のフィールドにのみ使用できます");
                return InjectResult.Error;
            }

            var matches = FindAssetsByType(field.FieldType);
            if (matches.Count == 0)
            {
                MornInjectLogger.LogError($"{Describe(mb, field)}: [FindAssetType] {field.FieldType.Name} アセットが見つかりません");
                return InjectResult.Error;
            }

            if (matches.Count >= 2)
            {
                MornInjectLogger.LogError(
                    $"{Describe(mb, field)}: [FindAssetType] {field.FieldType.Name} アセットが {matches.Count} 件見つかりました(1 件のみ期待)");
                return InjectResult.Error;
            }

            return AssignObject(so, field, matches[0]);
        }

        private static InjectResult InjectFindsAssetType(SerializedObject so, FieldInfo field)
        {
            var mb = (MonoBehaviour)so.targetObject;
            if (!TryGetElementType(field.FieldType, out var elementType))
            {
                MornInjectLogger.LogError($"{Describe(mb, field)}: [FindAssetsType] は配列または List<T> 型のフィールドにのみ使用できます");
                return InjectResult.Error;
            }

            if (!typeof(UnityEngine.Object).IsAssignableFrom(elementType))
            {
                MornInjectLogger.LogError($"{Describe(mb, field)}: [FindAssetsType] の要素型は UnityEngine.Object 派生である必要があります");
                return InjectResult.Error;
            }

            var matches = FindAssetsByType(elementType);
            var values = new List<UnityEngine.Object>(matches.Count);
            foreach (var a in matches)
            {
                values.Add(a);
            }

            return AssignArray(so, field, values);
        }

        private static List<Component> GetDirectChildComponents(MonoBehaviour mb, Type componentType)
        {
            var parent = mb.transform;
            var list = new List<Component>();
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                var comps = child.GetComponents(componentType);
                foreach (var c in comps)
                {
                    list.Add(c);
                }
            }

            return list;
        }

        private static List<Component> GetDescendantComponents(MonoBehaviour mb, Type componentType)
        {
            var all = mb.GetComponentsInChildren(componentType, true);
            var list = new List<Component>(all.Length);
            foreach (var c in all)
            {
                if (c.transform == mb.transform)
                {
                    continue;
                }

                list.Add(c);
            }

            return list;
        }

        private static List<Transform> FindSceneTransformsByName(string name)
        {
            var result = new List<Transform>();
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                foreach (var t in prefabStage.prefabContentsRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == name)
                    {
                        result.Add(t);
                    }
                }

                return result;
            }

            var scene = SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == name)
                    {
                        result.Add(t);
                    }
                }
            }

            return result;
        }

        private static List<Component> FindSceneComponentsByType(Type componentType)
        {
            var result = new List<Component>();
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                result.AddRange(prefabStage.prefabContentsRoot.GetComponentsInChildren(componentType, true));
                return result;
            }

            var scene = SceneManager.GetActiveScene();
            foreach (var root in scene.GetRootGameObjects())
            {
                result.AddRange(root.GetComponentsInChildren(componentType, true));
            }

            return result;
        }

        private static List<UnityEngine.Object> FindAssetsByName(string name, Type type)
        {
            var list = new List<UnityEngine.Object>();
            var guids = AssetDatabase.FindAssets($"{name} t:{type.Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath(path, type);
                if (asset == null)
                {
                    continue;
                }

                if (asset.name != name)
                {
                    continue;
                }

                list.Add(asset);
            }

            return list;
        }

        private static List<UnityEngine.Object> FindAssetsByType(Type type)
        {
            var list = new List<UnityEngine.Object>();
            var guids = AssetDatabase.FindAssets($"t:{type.Name}");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath(path, type);
                if (asset == null)
                {
                    continue;
                }

                list.Add(asset);
            }

            return list;
        }

        private static bool IsValidSceneFieldType(Type type)
        {
            return type == typeof(GameObject) || typeof(Component).IsAssignableFrom(type);
        }

        private static bool TryGetElementType(Type fieldType, out Type elementType)
        {
            if (fieldType.IsArray)
            {
                elementType = fieldType.GetElementType();
                return elementType != null;
            }

            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                elementType = fieldType.GetGenericArguments()[0];
                return true;
            }

            elementType = null;
            return false;
        }

        private static bool TryResolveSceneTarget(Transform target, Type fieldType, out UnityEngine.Object value)
        {
            if (fieldType == typeof(GameObject))
            {
                value = target.gameObject;
                return true;
            }

            value = target.GetComponent(fieldType);
            return value != null;
        }

        private static InjectResult AssignObject(SerializedObject so, FieldInfo field, UnityEngine.Object value)
        {
            var prop = so.FindProperty(field.Name);
            if (prop == null)
            {
                MornInjectLogger.LogError($"{field.DeclaringType?.Name}.{field.Name} の SerializedProperty が見つかりません (SerializeField 指定が必要です)");
                return InjectResult.Error;
            }

            if (prop.objectReferenceValue == value)
            {
                return InjectResult.Skipped;
            }

            prop.objectReferenceValue = value;
            return InjectResult.Changed;
        }

        private static InjectResult AssignArray(SerializedObject so, FieldInfo field, List<UnityEngine.Object> values)
        {
            var prop = so.FindProperty(field.Name);
            if (prop == null)
            {
                MornInjectLogger.LogError($"{field.DeclaringType?.Name}.{field.Name} の SerializedProperty が見つかりません (SerializeField 指定が必要です)");
                return InjectResult.Error;
            }

            if (!prop.isArray)
            {
                MornInjectLogger.LogError($"{field.DeclaringType?.Name}.{field.Name} は配列 SerializedProperty として解釈できません");
                return InjectResult.Error;
            }

            var changed = false;
            if (prop.arraySize != values.Count)
            {
                prop.arraySize = values.Count;
                changed = true;
            }

            for (var i = 0; i < values.Count; i++)
            {
                var element = prop.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue != values[i])
                {
                    element.objectReferenceValue = values[i];
                    changed = true;
                }
            }

            return changed ? InjectResult.Changed : InjectResult.Skipped;
        }

        private static string Describe(MonoBehaviour mb, FieldInfo field)
        {
            return $"{mb.name}.{mb.GetType().Name}.{field.Name}";
        }

        private enum InjectResult
        {
            Skipped,
            Changed,
            Error,
        }
    }
}
