# コハク画像の確認

2026-10-12、内蔵 image_gen で基本9枚と特殊16枚を個別生成しました。参照は `../CatAppearance-v1/kohaku-normal.png`。茶トラ・長毛・金色の目を維持し、各動作に基本、（なにか）+心を開く、心をつかむ+心を開くを対応させています。待つ場面は基本のみです。

全画像を目視確認しました。声かけの同時反応は、しっぽが収まるよう広い構図で再生成しました。採用画像とプロンプトは各フォルダーの manifest.json と prompts.json に保存しています。

cat-kohaku と playtest-kohaku に25枚を登録し、Unityの新規パック `9fb4383b47854aeea789ece120d4b281` を有効にしました。登録スクリプトで画像ハッシュと他の猫の登録内容の保持を確認済みです。詳細は Registration.json を参照してください。

隔離Unityプロジェクト `cat_cafe_sim_tool/generated/tama-image-verification` で既存のゲーム状態フィクスチャの対象をコハクへ変更し、PythonPlayerPanel.Apply を通して32ケースを確認しました。8動作の基本・心を開く・同時反応・結果表示について、画像の一致、表示有効、現在の動作名、画像不足表示なし、縦横比の一致が成功しました。ログの成功マーカーは `CAT_CAFE_KOHAKU_IMAGES_OK`、画面保存先は `Logs/KohakuImages` です。

この確認は画像表示の検証であり、HTTP通信や反応の発生確率の検証は含みません。ゲーム本体のコード変更はありません。
