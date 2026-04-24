# MornInject

<p align="center">
  <img src="https://img.shields.io/github/license/TsukumiStudio/MornInject" alt="License" />
</p>

## 概要

Unity Editor 上で `[SerializeField]` フィールドへのコンポーネント / GameObject 参照を属性ベースで自動注入するツール。`Tools/自動注入` メニュー、または `Alt + Shift + I` ショートカットで、ActiveScene または PrefabStage 内の全 MonoBehaviour を走査し、各属性のルールに従って参照を解決する。

## 機能

引数の有無で挙動が切り替わる。`[Child]` 系の第 2 引数 `bool deep` が `true` のとき子孫全体を走査する。文字列 `name` を指定した属性は名前フィルタを追加する (GameObject 名で完全一致)。

| 属性 | スコープ | 取得内容 |
|------|---------|----------|
| `[Me]` | 自身 | 型一致のコンポーネント |
| `[Child]` / `[Child(true)]` | 直下の子 / 子孫全体 | 型一致のコンポーネント (単数、2 件以上でエラー) |
| `[Child("name")]` / `[Child("name", true)]` | 直下の子 / 子孫全体 | 型 + 名前一致のコンポーネント (単数、2 件以上でエラー) |
| `[Childrens]` / `[Childrens(true)]` | 直下の子 / 子孫全体 | 型一致のコンポーネント配列 / `List<T>` |
| `[Childrens("name")]` / `[Childrens("name", true)]` | 直下の子 / 子孫全体 | 型 + 名前一致のコンポーネント配列 / `List<T>` |
| `[Find]` / `[Find("name")]` | シーン全体 (PrefabStage 中はステージ内) | 型 (+名前) 一致の GameObject / Component (単数、2 件以上でエラー) |
| `[Finds]` / `[Finds("name")]` | シーン全体 (PrefabStage 中はステージ内) | 型 (+名前) 一致の GameObject / Component 配列 / `List<T>` |
| `[FindAsset]` / `[FindAsset("name")]` | プロジェクト全体 (`AssetDatabase`) | 型 (+名前) 一致のアセット (単数、2 件以上でエラー) |
| `[FindAssets]` / `[FindAssets("name")]` | プロジェクト全体 (`AssetDatabase`) | 型 (+名前) 一致のアセット配列 / `List<T>` |
| `[OnMornInject]` | メソッド属性 | 注入完了後に呼び出されるフック (基底 → 派生の順で引数なし void メソッドを実行) |

## 使い方

```csharp
public sealed class SampleMono : MonoBehaviour
{
    [Me] [SerializeField] private Rigidbody _rb;
    [Child] [SerializeField] private Button _okButton;                   // 直下の子
    [Child(true)] [SerializeField] private Canvas _canvas;               // 子孫全体
    [Child("Header")] [SerializeField] private Image _headerImage;       // 直下の子 + 名前
    [Childrens] [SerializeField] private List<Image> _images;
    [Find] [SerializeField] private GameManager _manager;                // シーン全体
    [Find("MainCamera")] [SerializeField] private Camera _camera;        // 名前指定
    [FindAsset] [SerializeField] private GameSettings _settings;
    [FindAssets] [SerializeField] private List<EnemySettings> _enemies;
    [FindAsset("Default")] [SerializeField] private Texture2D _texture;

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
