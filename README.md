# MornInject

<p align="center">
  <img src="https://img.shields.io/github/license/TsukumiStudio/MornInject" alt="License" />
</p>

## 概要

Unity Editor 上で `[SerializeField]` フィールドへのコンポーネント / GameObject 参照を属性ベースで自動注入するツール。`Tools/自動注入` メニュー、または `Alt + Shift + I` ショートカットで、ActiveScene または PrefabStage 内の全 MonoBehaviour を走査し、各属性のルールに従って参照を解決する。

## 機能

| 属性 | スコープ | 取得内容 |
|------|---------|----------|
| `[Me]` | 自身 | 型一致のコンポーネント |
| `[Child]` | 直下の子 | 型一致のコンポーネント (単数、2 件以上でエラー) |
| `[ChildDeep]` | 子孫全体 | 型一致のコンポーネント (単数、2 件以上でエラー) |
| `[Childrens]` | 直下の子 | 型一致のコンポーネント配列 / `List<T>` |
| `[ChildrensDeep]` | 子孫全体 | 型一致のコンポーネント配列 / `List<T>` |
| `[Find("name")]` | シーン全体 (PrefabStage 中はステージ内) | 完全一致名 GameObject / Component (単数、2 件以上でエラー) |
| `[Finds("name")]` | シーン全体 (PrefabStage 中はステージ内) | 完全一致名 GameObject / Component 配列 / `List<T>` |
| `[OnMornInject]` | メソッド属性 | 注入完了後に呼び出されるフック (基底 → 派生の順で引数なし void メソッドを実行) |

## 使い方

```csharp
public sealed class SampleMono : MonoBehaviour
{
    [Me] [SerializeField] private Rigidbody _rb;
    [Child] [SerializeField] private Button _okButton;
    [ChildDeep] [SerializeField] private Canvas _canvas;
    [Childrens] [SerializeField] private List<Image> _images;
    [Find("MainCamera")] [SerializeField] private Camera _camera;

    [OnMornInject]
    private void OnInject()
    {
        Debug.Log($"注入完了: images={_images.Count}");
    }
}
```

## 実行

- `Tools/自動注入` メニュー
- `Alt + Shift + I` ショートカット

PrefabStage 編集中の場合は、そのステージの `prefabContentsRoot` 以下のみを走査する。

## ライセンス

[The Unlicense](LICENSE)
