# 猫7匹の準備／実行画像の登録とビルド

2026-10-17。タマ・ソラ・コハク・ムギ・ナナ・マロン・ココの8行動を登録し、通常／穏やかな実行で行動に対応する画像を表示する。拒否・戸惑い・疲労時は準備画像へ戻す。数値ルール・反応条件・特別発動の画像・保存済み履歴は変更しない。

## 素材と登録

[制作記録・全56行動の対応](Results/CatExecutionImages/Prototypes/ExecutionImages-v1/README.md)、[登録結果](Results/CatExecutionImages/Registration.json)。不足13枚を追加制作し、43場面は既存画像を再利用した。基準・準備・実行・生成プロンプト・比較確認・ID対応を、参照するPNGとともに `Docs/Results/CatExecutionImages/Prototypes` へ保存した。元の作業フォルダ `../Docs/Prototypes` も保持する。保存した制作記録は制作時点の記録であり、「未登録」は当時の状態を指す。一部の過去資料への相対リンクは元の作業フォルダを基準とする。

ゲームID → 表示IDは、`cat-tama → playtest-tama`、`cat-sora → playtest-sora`、`cat-kohaku → playtest-kohaku`、`cat-mugi → playtest-mugi`。ナナ `request-cat-1`、マロン `regular-rescue-1`、ココ `visiting-cat-1` は両ID共通。

8モードの共通キーと `__switch` を準備、`__direct` / `__adapt` を実行へ明示登録した。7匹×32キー＝224キー（ゲームIDと表示IDの別名にも同じ画像を対応）。同一PNGはキー間で共有し、新規保存は13枚のみ。基準画像を準備として使う行動は制作時の判断を引き継ぐ。

再登録：

```powershell
python -X utf8 cat_cafe_sim_tool/register_cat_execution_images.py --project-path C:/Users/hi-wa/unity_workspase/CatCafeSim
```

有効パックは `Assets/Resources/CatCafeContent/catalog.json` から読み取り、既存パックのbundleと文章カタログを更新する。パックをコピーせず、PNGは内容ハッシュで共有。元JSONは `Logs/CatExecutionImages` に記録する。

現在の作業用パック `4762dd1bd978461bb07c041c59417281` には文章239条件を保持した。残る5匹にも画像bindingを追加し、既存の汎用文章へフォールバックさせる。架空の専用文章候補は作らず、元の全文章と既存3匹のID対応を保持。8匹・256の行動画像bindingを検証した。

作業開始前から存在した未コミットのパック全体やcatalog切替を今回のコミットに混ぜないため、Git管理済みの標準パック `8d646380ed694a51be2c16dbe7b19134` にも同じ7匹の対応を反映した。標準パック側はv2からv3へ拡張し、不足する共通行動文17件と画像bindingを補う。元の専用文章180条件、ミケの既存画像対応と特殊反応は保持する。標準パックの登録結果は [別記録](Results/CatExecutionImages/Registration-8d646380ed694a51be2c16dbe7b19134.json)。既存パックを明示して更新するときは次を使う（有効なcatalogは切り替えない）。

```powershell
python -X utf8 cat_cafe_sim_tool/register_cat_execution_images.py --project-path C:/Users/hi-wa/unity_workspase/CatCafeSim --content-pack-id 8d646380ed694a51be2c16dbe7b19134
```

## 表示処理

- Unityの `CafeNarrativeCatalog.UsePreparationImage` とPythonの `player_image_action` を、対応済みの8匹へ拡張。`direct` / `adapt` で `turn_away` / `confused` / `listless` のとき準備へ戻す。他の操作・独自猫の扱いは維持する。
- 今後の交流で確定するreceiptに画像参照を保存し、従来どおり保存再開で復元。過去のreceiptは再計算・書き換えしない。
- Unityの実プロジェクトと `unity_preview_template` のランタイムを同期。専用の `CatExecutionImageVerification.Run` でResources・実交流・UGUI・保存再開を確認する。
- 既存の画面スモーク検証が席詳細ボタンから旧仕様の猫詳細を期待していたため、現在の席交流詳細パネルを確認するよう検証を修正した。製品UIは変更しない。
- `build_standalone.ps1` はEditorプロセス自体の終了を待つよう修正。ライセンス／クラッシュハンドラーの子プロセスを待ち続けない。固定出力先・Pending→Latestの移動方式は維持する。

## 検証

- C#：有効パックの8匹×8行動で1,024 assertions成功。通常／穏やかな実行、拒否・戸惑い・疲労、実操作からのreceipt、保存再開、表示読み取りによる状態不変。
- Git管理済み標準パックも対象7匹の896 assertions成功。旧v2からの形式更新後に全画像binding・共通行動文・revisionの整合を検証した。
- Unity：320ケース成功。8匹・8行動について準備／実行のResourcesとUGUIの画像一致、反応別の画像選択、ディスク保存再開。画像は `Logs/CatExecutionImages/Unity`、成功マーカー `CAT_CAFE_CAT_EXECUTION_IMAGES_OK`。ボール・なでるの16画面を保存し、タマ・ナナ・ココの画面も目視確認した。
- Python：Unity交流テスト4件成功。反応別の96ケースで8匹の選択と独自猫の互換、実APIの画像キー、ローカルHTTP操作、保存再開を確認。WindowsではUTF-8モードとworkspace内のTEMPを使用した。
- 既存カタログ96 assertions、全行動の専用fixture609 assertions成功。制作プレビュー14件中9件成功、GUI5件は既存の実行条件によりスキップ。
- 配布exe：既存の画面操作スモーク217項目成功、errorsなし。タイトル、新規開始、営業、交流、保存再開などを実画面で確認。結果 `Logs/CatExecutionImages/Player/result.json`。
- 登録前の309場面、特殊反応、元文章239条件が保持され、画像13枚は生成元・登録先・Unity用PNGでバイト一致。

加入猫の画像境界テストでは、対象猫を初期猫として置く検証用ルールを使用し、依頼候補IDとの重複を避けた。実ゲームの加入条件は変更しない。低体力テストで元々の苦手行動が拒否判定を先に起こす場合は、検証用好みだけを調整して狙う反応を成立させた。生成時と同じ検証ルールで保存再開する。

## ビルドと容量

ビルドは `build_standalone.ps1 -ProjectPath C:/Users/hi-wa/unity_workspase/CatCafeSim` を使用。出力先は固定の `Logs/StandaloneBuild/Latest/CatCafeStandalone.exe`。実行ログは `Logs/StandaloneBuild/Runs`。ビルドZIP・別出力フォルダ・Unityプロジェクトのコピー・検証用Libraryは作らない。

開始時はLogs 13,383,956,556 bytes、Latest 4,184,220,754 bytes、Runs 2,168,462 bytes、制作ツールgenerated 3,277,372,585 bytes。素材正本のGit保存約150 MiB、画像登録用約24.9 MiB、有効パックとGit管理済み標準パックへ各約24.9 MiBを追加した。既存素材・セーブ・他作業のパックは削除しない。終了時の容量・ビルド結果は [Verification.json](Results/CatExecutionImages/Verification.json) に記録する。

最終Latestは8,247,442,140 bytes（約7.68 GiB）、前回から4,063,221,386 bytes（約3.78 GiB）増加。最大ファイルは画像のビルドデータ `CatCafeStandalone_Data/resources.assets.resS` の8,157,703,904 bytes。前回成功ビルドは10月15日で、以後の猫素材・履歴用パックも今回のResourcesへ含まれる。プロジェクトのResources配下には13個の既存画像パックがあり、PNGファイルの容量より大きいUnityのテクスチャ形式で格納される。今回の13枚だけによる増加ではない。過去receiptの参照を維持するため既存パックは削除しない。成功ビルドはLatestの1組、PendingとPreviousなし。制作ツールgeneratedの容量変化は0 bytes。Gitの素材オブジェクトは別途保持する。
