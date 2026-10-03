# コンテンツ制作ツール

ゲーム本体と別のフォルダに置く、独立したPython/Tkinterツールです。現在は背景込みPNGを猫・場面ごとに確認・登録できます。ゲームの起動やSessionの生成は不要です。

親の `CatCafeSim` フォルダから起動します。

```powershell
python -X utf8 cat_cafe_sim_tool/run.py
```

猫と場面を選択 → 「画像を選ぶ…」 → プレビュー確認 → 「この画像を登録」の順に操作します。初期の猫一覧は隣のゲームの `config/cafe_playtest_cats.json` を読み込みます。独自の猫一覧や保存先も指定できます。

```powershell
python -X utf8 cat_cafe_sim_tool/run.py --catalog cats.json --output cat_cafe_sim/saves/content_images
```

猫一覧は `[{"cat_id": "cat-1", "name": "ミケ"}]` 形式のUTF-8 JSONです。ゲーム内の猫IDと一致させてください。ゲームデータを読み込むPythonモジュールには依存しません。

標準の出力先は `cat_cafe_sim/saves/content_images`。元画像をコピーし、`manifest.json` と `images/` を書き出します。この配置ならゲームの「猫の画像…」が自動で読み込みます。別の出力先を指定した場合は、フォルダの内容をゲームの標準読み込み先へ配置してください。

ゲーム側には画像登録ボタンを置かず、画像の読み込み・表示だけを残します。未登録・画像欠落時は仮画像になります。PNGのみ対応し、ゲームの数値やセーブは変更しません。ローカルLLMへの接続は後続です。

## ComfyUIで生成する

1. ComfyUIを `http://127.0.0.1:8188/` で起動します。
2. 制作画面で猫と場面を選び、「ComfyUIで生成…」を押します。
3. 接続先、モデル、seed、幅・高さ、Steps、CFG、正負プロンプトを確認します。
4. 「1枚生成」を押します。通信は別スレッドで動きます。
5. 完成画像が制作画面へ未保存候補として戻ります。気に入ったら「この画像を登録」で採用します。

ユーザーの `t2iSimple (2).json` を `workflows/t2i-simple-source.json` に保存し、その設定を初期値に使っています。API送信ではCheckpointLoaderSimple、CLIPTextEncode、EmptyLatentImage、KSamplerAdvanced、VAEDecode、SaveImageを組み立てます。画面編集用JSONの汎用インポート機能ではありません。

初期設定：`waiNSFWIllustrious_v140.safetensors`、1024×1024、27 Steps、CFG 7、`euler_ancestral` / `simple`。元のPrimitiveIntに接続されていた負のseedは使わず、非負のseedを直接指定します。参照画像・LoRA・同一個体の固定はまだ対応していません。生成画面の猫プロンプトは仮の三毛猫向けなので、選んだ猫の外見と行動に合わせて編集してください。ネガティブプロンプトは元ワークフローのものなので、猫には合わない制約も必要に応じて編集してください。

候補PNGと設定・実行seed・送信グラフ・ジョブIDのJSONは、ツール内の `generated/` へ保存します。Git除外対象です。ComfyUI側にも `CatCafeSimTool/cat` の接頭辞で出力を保存します。確認前にゲームの対応表を更新しません。生成中に猫・場面を変えた場合は自動で候補を紐付けず、保存先を案内します。

生成画面を閉じると結果の受け取りを止めますが、ComfyUI側のジョブは継続します。他のジョブを止めるAPIは呼びません。待ち時間上限は10分、各通信の上限は15秒です。失敗時は生成画面へ理由を表示し、再実行できます。

検証：GUIを含むテスト7件成功。起動中のComfyUIへ実際に通常猫の1枚生成を依頼し、PNGの取得と設定JSONの保存を確認しました。生成した候補は未登録です。

このフォルダはゲーム・資料とは別のGitリポジトリで管理します。既定ブランチは `main` です。リモートへの公開・pushは行っていません。
