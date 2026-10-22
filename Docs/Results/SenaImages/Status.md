# セナ画像の制作記録

2026-10-21完了。派遣紹介猫セナの採用PNG27枚を52場面へ登録した。

- [画像一覧](index.html)・[生成プロンプトと参照元](prompts.json)・[外見6項目](profile.json)・[場面対応表](manifest.json)・[検証記録](Verification.json)
- 白・長毛、遊び好きで繊細な性格を維持。青い目、細身の体、卵形の顔、高めの房毛の耳、長い羽毛状の尾を制作設定として記録。費用200・特性hospitalityは維持。
- 基準・睡眠・待機・8行動・特殊16枚の27枚を内蔵image_genで生成。準備／実行を共用可能な画像で52場面へ対応。交流文画像32枠も登録。
- 素材IDは `intro-shopping_street_event`。実加入IDは `dispatch-rescue-1` など加入順で変わるため、派遣記録の紹介元 `shopping_street_event` から素材を解決する。名前・番号の固定対応は使わない。
- 制作カタログ、Python画像manifest、実Unityの有効パックへ登録。既存猫・既存本文・セーブは保持。
- Python回帰テストは31件中28件成功、GUI等3件スキップ。テスト相互参照のパスを補い、簡略状態データへの対応を修正してunity_player全4件を再実行・成功。
- 実派遣→帰還→加入、外見保存再開、費用・毛色・特性を確認。ルカが先に加入し、セナが `dispatch-rescue-2` になる経路でも素材IDを区別して保存再開・リプレイを確認。
- Unity Editorで40ケース成功。実際の派遣加入、通常／睡眠、8行動の準備／実行と拒否反応、特殊画像リソース、保存再開を確認。通常・睡眠・ボール・なでるの4画面を目視確認。
- 証跡：`Logs/CatDailyImages/20261021/unity.log`、同`Unity/result.txt`と4枚の画面画像。

容量：採用元PNG 58,633,278 bytes（55.9 MiB）、Python登録先とUnity登録先もそれぞれ同量。既定生成先の原本も保持。tool/generatedは3,277,372,585 bytes。Latestは開始時8,630,202,196 bytes、終了時8,630,202,196 bytes。Runsは73,746,986 bytes。

exeは再ビルドしていないため、既存Latestには今回の画像は未反映。実Unityプロジェクトで検証済み。ビルドとUnityプロジェクトのコピーは増やしていない。Pendingなし。
