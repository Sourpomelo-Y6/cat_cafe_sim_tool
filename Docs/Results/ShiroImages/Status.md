# シロ画像の制作記録

2026-10-30に制作・登録・Unity検証を実施。中断後、2026-10-31に画面目視と記録更新を終えて完了。

- [画像一覧](index.html)・[内蔵image_genの生成プロンプトと参照元](prompts.json)・[外見6項目](profile.json)・[場面対応表](manifest.json)・[検証記録](Verification.json)
- 白猫・長毛、活発な探検家、費用200・特性relaxedを維持。黄緑色の目、長い脚、幅のある逆三角形の顔、房毛のある大きな耳、豊かな白い尾でソラ・チロ・セナと区別。
- 採用27枚を52場面へ対応し、交流文画像32枠を登録。基準画像を個体参照に使用、準備画像は共用。全27枚を目視確認。
- ぬいぐるみ基本・ボールopen_up・ぬいぐるみopen_upは尾の余白や手足を修正してv2採用。旧候補3枚と[プロンプト](rejected-candidates.json)を保持し、ゲームには登録していない。
- 素材ID `catalog-7`。ゲーム7日目・募集3回目の実加入 `rescue-7`（シロ（紹介3））を標準募集候補の位置から解決。別猫との素材区別を確認。
- Python回帰28件成功。実加入で費用・特徴・特性・外見・素材ID・保存再開、拒否反応6通りの準備画像への切り替えを確認。
- Unity Editor40ケース成功。実募集加入、通常／睡眠、8行動の準備／実行・拒否反応、特殊画像、保存再開を確認。通常・睡眠・ボール・なでる4画面を目視確認。
- 制作カタログ、Python画像manifest、実Unity有効パック `cbe0df94e7a5401a8f466406c105b493` へ追加。既存19匹、version 4の本文435件と全既存文章項目を保持。有効猫数20匹。
- 既存のCompleteActivePackRegistrationを通し、Standalone用1024・BC7等の取り込み方針を適用。27枚の設定を最終照合。
- 証跡：`Logs/CatDailyImages/20261030/unity.log`、同`Unity/result.txt`・画面4枚・`python-result.json`・登録前JSON。

容量：採用PNG 56,824,502 bytes（54.2 MiB）。PythonとUnityの登録先へそれぞれ同量。旧候補3枚6,410,198 bytesと既定生成先原本を保持。既存素材・セーブは削除していない。

## 2026-10-31 選別保存時の追補

末尾の「ビルド未実施・収録未確認」は画像制作時の記録。後続の現行[進捗](../../../../Docs/DevelopmentStatus.md)では固定Latest収録済み。今回の保存ではビルド・ゲーム起動・既存テスト再実行・公開を行っていない。

[静的照合](GitIntegrityAudit20261031.json)：採用27枚のSHA256を原本・Python・Unity間で照合し、52場面・32交流画像枠、PNGとmeta、新規GUIDの一意性、既存19猫・435本文条件と全既存文章項目、既存45対象の保持を確認。Toolと実Unityの変更も一致する。Pythonの画像manifestと画像は従来どおりsaves/のignore対象として保持し、強制追加しない。Toolの採用原本・対応表・登録スクリプトとUnityのPNG/metaを保存する。旧候補3枚も制作証跡として保持。スズ10月31日20時・全完了予定11月10日を維持。

本作業ではビルド未実施で、配布exeへのシロ収録は未確認。再開までの他作業を含む容量はLatest 5,563,471,046→5,563,471,638 bytes、Runs 233,688,515→234,861,418 bytes。tool/generatedは3,277,372,585 bytesで不変。Latest増592 bytes・Runs増1,172,903 bytesを本画像制作のビルド増分としては扱わない。StandaloneBuildはLatest一組とRunsのみ、Pendingなし。ビルド・Unityプロジェクト・Libraryのコピーを増やしていない。
