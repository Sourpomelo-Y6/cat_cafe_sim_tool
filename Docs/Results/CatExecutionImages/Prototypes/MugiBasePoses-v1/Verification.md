# ムギ画像の確認

2026-10-13、内蔵 image_gen で基本9枚と特殊16枚を個別生成しました。参照は `../CatAppearance-v1/mugi-normal.png`。短毛のキジトラ、オリーブグリーンの目、白斑なしを維持し、8動作に基本・（なにか）+心を開く・心をつかむ+心を開くを対応させています。待つ場面は基本のみです。

25枚を目視確認し、採用画像と生成プロンプトを各フォルダーの manifest.json と prompts.json に保存しました。

cat-mugi と playtest-mugi に25枚を登録し、Unityの画像パック `7ede2b392dfb445a8b65fe85ccb88f10` を有効にしました。保存元、両IDの登録画像、Unity画像のバイト一致とSHA-256を確認し、他の猫の登録内容が維持されていることも確認済みです。登録の詳細は Registration.json に記録しています。

隔離Unityプロジェクト `cat_cafe_sim_tool/generated/tama-image-verification` の既存ゲーム状態フィクスチャの対象をムギへ変更し、PythonPlayerPanel.Apply を通した32ケースを確認しました。8動作の基本・心を開く・同時反応・結果表示について、対応画像、表示有効、動作名、画像不足表示なし、縦横比の一致が成功しました。成功マーカーは `CAT_CAFE_MUGI_IMAGES_OK`、画面保存先は `Logs/MugiImages` です。ボール遊びの同時反応と見守りの心を開く場面は、保存した実画面でも目視確認しました。

この検証は画像表示を対象とし、HTTP通信や反応の発生確率は含みません。ゲーム本体のコード変更はありません。
