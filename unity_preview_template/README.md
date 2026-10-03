# Cat Cafe Content Preview

Unity 2021.3.45f2向けの、採用済み画像・文章の表示試作です。

1. Pythonの制作ツールを起動し「Unity向けに書き出す…」を押します。
2. Unityの `Cat Cafe > Import Content Pack…` から、作られた `CatCafeContent-<ID>` フォルダを選びます。
3. `Cat Cafe > Open Content Preview` を押し、Playを開始します。
4. 猫を選択し、通常・遊ぶ・撫でられる・眠るを切り替えます。文章欄はスクロールできます。

取り込み先は `Assets/Resources/CatCafeContent/Imports/<ID>/`。`catalog.json` が最新の取り込み先を示します。以前のパックは保持し、同じパックを再取り込みしても同じ内容を使います。再取り込み後はPlayを再開してください。

素材はResourcesとしてビルドに含まれ、実行時にPython、ComfyUI、LLMへ接続しません。画像未登録時は画像なしの案内を表示します。文章未登録時の定型文は制作ツールで書き出されます。日本語フォントはWindowsのYu Gothic/Meiryo/MS Gothicを利用します。別OSや配布向けの埋め込みフォントは後続です。

`Assets/Scenes/CatCafeContentPreview.unity` はメニュー初回利用時に作成し、ビルド対象の先頭へ追加します。既存のSampleSceneは残します。営業・交流などのゲーム処理はまだ移植していません。

バッチ検証は `CAT_CAFE_CONTENT_PACK` に素材パックの絶対パスを指定し、Unityを `-batchmode -projectPath <project> -executeMethod CatCafePreview.Editor.ContentPreviewSetup.VerifyAndCapture -logFile <log> -quit` で起動します。全猫全場面の表示を確認し、`Logs/ContentPreview.png` にスクリーンショットを保存します。
