# ルカ画像の制作記録

2026-10-22完了。撮影スタジオからの紹介猫ルカの採用PNG27枚を52場面へ登録した。

- [画像一覧](index.html)・[生成プロンプトと参照元](prompts.json)・[外見6項目](profile.json)・[場面対応表](manifest.json)・[検証記録](Verification.json)
- 白・短毛、穏やかな甘えん坊を維持。緑色の目、丸い顔、コンパクトな体、滑らかな白い尾を制作設定として記録。費用250・特性hospitalityは維持。
- 基準・睡眠・待機・8行動・特殊16枚の27枚を内蔵image_genで生成。準備／実行を共用可能な画像で52場面へ対応。交流文画像32枠も登録。
- 素材IDは `intro-cat_photo_studio`。実加入ID `dispatch-rescue-N` から派遣記録の紹介元 `cat_photo_studio` を使って解決する。固定番号や名前には依存しない。
- 制作カタログ、Python画像manifest、実Unity有効パックへ登録。既存猫の対応・既存本文・セーブを保持。
- Python回帰テスト21件中20件成功、1件スキップ。実加入で費用250・毛色・特性・外見・素材ID・保存再開とリプレイを確認。後からセナが加入する経路でも画像を区別できることを確認。
- Unity Editorで40ケース成功。実際の撮影スタジオ派遣→帰還→加入、通常／睡眠、8行動の準備／実行・拒否反応、特殊画像リソース、保存再開を確認。通常・睡眠・ボール・なでるの4画面を目視確認。
- 証跡：`Logs/CatDailyImages/20261022/unity.log`、同`Unity/result.txt`・画面4枚・`python-result.json`。

容量：採用元PNG 50,539,358 bytes（48.2 MiB）、Python登録先とUnity登録先もそれぞれ同量。既定生成先の原本も保持。tool/generatedは3,277,372,585 bytes。Latestは開始時8,757,722,753 bytes、終了時8,757,722,753 bytes。Runsは92,682,454 bytes。

exeは再ビルドしていないため、既存Latestには今回の画像は未反映。実Unityプロジェクトで検証済み。ビルドとUnityプロジェクトのコピーは増やしていない。Pendingなし。
