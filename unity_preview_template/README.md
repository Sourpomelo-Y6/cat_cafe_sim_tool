# Cat Cafe Content Preview

Unity 2021.3.45f2向けの、採用済み画像・文章の表示試作です。

1. Pythonの制作ツールを起動し「Unity向けに書き出す…」を押します。
2. Unityの `Cat Cafe > 素材 > 素材パックを取り込む…` から、作られた `CatCafeContent-<ID>` フォルダを選びます。
3. `Cat Cafe > プレビュー > 素材プレビューを開く` を押し、Playを開始します。
4. 猫を選択し、通常・遊ぶ・撫でられる・眠るを切り替えます。文章欄はスクロールできます。

UIはシーンと `Assets/CatCafePreview/Prefabs/ContentPreview.prefab` に保存します。Play前のHierarchyに `Cat Cafe Content Preview > Content Canvas > Background` があり、SceneビューとInspectorで位置・色・文字サイズを変更できます。実行時にはUIを作り直しません。ボタンのOn Clickと猫選択のOn Value ChangedもInspectorから確認できます。

シーン内の変更はPrefabのOverrideになります。共通の配置を変更する場合はPrefabを開くか、必要な変更をApplyしてください。画像・文章・猫一覧は実行時に採用済み素材から読み直します。編集画面で最新の素材を表示したい場合は `Cat Cafe > 素材 > 表示内容を更新` を使い、保存してください。これは表示内容の更新で、配置・色・文字サイズは変更しません。

取り込み先は `Assets/Resources/CatCafeContent/Imports/<ID>/`。`catalog.json` が最新の取り込み先を示します。以前のパックは保持し、同じパックを再取り込みしても同じ内容を使います。再取り込み後はPlayを再開してください。

素材はResourcesとしてビルドに含まれ、実行時にPython、ComfyUI、LLMへ接続しません。画像未登録時は画像なしの案内を表示します。文章未登録時の定型文は制作ツールで書き出されます。日本語フォントは同梱のM PLUS 1pを利用し、OSフォントに依存しません。[Google Fontsの配布元](https://github.com/google/fonts/tree/main/ofl/mplus1p)から取得した未変更のフォントで、SIL Open Font License 1.1の著作権表示・ライセンスを `Fonts/OFL.txt` に同梱しています。フォントを含めて配布する際はこのファイルも保持してください。

`Assets/Scenes/CatCafeContentPreview.unity` はメニュー初回利用時に作成し、ビルド対象の先頭へ追加します。既存のSampleSceneは残します。営業・交流などのゲーム処理はまだ移植していません。

バッチ検証は `CAT_CAFE_CONTENT_PACK` に素材パックの絶対パスを指定し、Unityを `-batchmode -projectPath <project> -executeMethod CatCafePreview.Editor.ContentPreviewSetup.VerifyAndCapture -logFile <log> -quit` で起動します。全猫全場面の表示を確認し、`Logs/ContentPreview.png` にスクリーンショットを保存します。

検証ではシーン再読込後のUI参照、Prefab接続、保存されたボタン動作、初期化の繰り返しでUIが増えないこと、配置・文字サイズの変更が初期化後も残ることを確認します。


## Cat Cafeメニュー

- **プレビュー**：営業画面、素材プレビューを開きます。
- **素材**：素材パックの取り込み、表示内容の更新を行います。
- **開発用 > UI再生成**：猫の成長・習得選択、受け入れ依頼、飼育スペース、店舗イベント、目標の結果、自動営業のUIを再生成します。対象パネルの手動編集は再生成で置き換わるため、通常の試遊にはプレビューメニューを使います。

猫の詳細画面は、営業画面と猫一覧を準備した後、`Cat Cafe > 開発用 > UI再生成 > 猫の詳細`で配置します。Play中は猫一覧の名前または「詳細」を押し、体調と成長・習得進捗を閲覧できます。
