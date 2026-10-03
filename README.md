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

ゲーム側には画像登録ボタンを置かず、画像の読み込み・表示だけを残します。未登録・画像欠落時は仮画像になります。PNGのみ対応し、ゲームの数値やセーブは変更しません。ローカル画像生成AI・LLMへの接続は今後このツールへ追加します。

このフォルダはゲーム・資料とは別のGitリポジトリで管理します。既定ブランチは `main` です。リモートへの公開・pushは行っていません。
