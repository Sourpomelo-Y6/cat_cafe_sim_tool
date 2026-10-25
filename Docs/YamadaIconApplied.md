# 山田さん（guest-12）のプロフィール・人物アイコン

2026-10-25制作、2026-10-26引継ぎ整理。一人・一案を既存Toolへ登録し、人物専用パックを通常Unity取り込み・固定Latestへ反映。元チャットの停止は `Selected model is at capacity` という実行環境エラーで、ゲーム検証の失敗ではない。引継ぎでは既存成功記録を照合し、再生成・再ビルド・Player再検証は行っていない。

## 採用プロフィールと表情

- 制作対象 `person-guest-12` → ゲーム客 `guest-12`、山田さん。表示名による推定なし。
- 性格：気さくでゆったりしており、猫と過ごす時間を穏やかに楽しむ。
- 接し方：短く親しみを込めて一度誘い、反応を待つ。近づかない猫を追わず、休憩を尊重する。
- 口調：やわらかく簡潔な常体。「〜してみようか」「ゆっくりでいいよ」「ここで待とう」。猫の呼び方 `{cat}`。命令・幼児語・猫の気持ちや結果の決めつけを避ける。
- 外見：耳が見える波打つ短いチャコールブラウン髪、不揃いの前髪、丸みのある輪郭、眼鏡なし。くすんだ青緑のカーディガン、象牙色の丸首。
- 表情：両目を緩い弧に閉じ、なだらかな眉と小さく開いた口で穏やかに話す平常の顔。吉田さんの細めの開眼・水平寄りの閉じ口と差別化する。年齢・性別の文章設定・職業・来歴・来店歴は追加しない。

[採用透過PNG](../../Docs/Prototypes/YamadaIcon-v1/yamada-icon.png)は1254×1254 RGBA、alpha0〜255、SHA256 `1313db1e0f1182300a8093c93313f4d8d8e40e9a0591664e33a7c207d39d56c9`。生成原本一枚を採用。[制作記録・完全プロンプト](../../Docs/Prototypes/YamadaIcon-v1/README.md)。引継ぎ時に原本と[実入力後1024画面](Results/YamadaIcon/manual-after-input-1024.png)を目視した。大きい表示では閉じ目と開いた口が明瞭。27〜34pxの席では閉じ目のまとまり、髪と服の色で区別できるが、眉や口の細線まで読めるとはしない。

## 登録・取り込み・配布

開始HEAD：Tool `2c38b4b`、Python `a8a8846`、Unity `3227f54`、共有Docs `994813e`。AGENTSと吉田さんまでの資料、履歴・差分を確認。`register_yamada.py` が既存SubjectRegistry / HumanPortraitStoreを使い保存・別インスタンス再読込・人物専用書き出しを実施。[登録記録](Results/YamadaIcon/registration.json)。実台帳のGUI手入力とは区別する。

人物パック `c5126d3e23b5b0536bc25773994fd065` は通常客guest-1〜12と予約客の森さん `reservation-longhair` の13人。既存12人の人物設定・画像対応を保持。同内容再出力は同じ場所を利用。猫画像・猫catalogは再書き出ししない。[Unity照合](Results/YamadaIcon/unity-data-checks.json)：明示ID・画像hash・max1024・非圧縮・alpha保持・mipmap/read-write無効・Standalone overrideなし。人物へ猫用BC7は適用しない。

固定Latestの成功ログ：`Logs/StandaloneBuild/Runs/20261025-215948-567-870b7323e86642b09932801d5354eb56.log`、CAT_CAFE_YAMADA_IMPORTED / CAT_CAFE_STANDALONE_BUILD_OK / CAT_CAFE_STANDALONE_SCREENS_BUILD_OK。指定build_standalone.ps1、既存Unity、VerifyYamadaAndBuild、共有StandaloneBuildStorageを使用。前の試行やライセンス診断を成功根拠にしない。

引継ぎの[データ・hash監査](Results/YamadaIcon/handoff-audit.json)で現台帳13件とUnityパック全画像、既存speech行、保護対象の猫manifest・採用本文・Unity猫catalog、Tool/Unity検証コード一致を確認。ルール・数値・登場条件・保存形式は変更していない。

## 検証区分と限界

- [Tool GUI試験](Results/YamadaIcon/gui-tests.txt)：11件成功（継承の重複5件を含む、固有バックエンド5件＋実Tkボタン1件）。隔離データ・ダイアログ差し替えによる自動試験。
- [第2段階回帰](Results/YamadaIcon/popular-tests.txt)：4件成功、desktop GUI1件skip。
- [Player106項目](Results/YamadaIcon/player-result.txt)：13画像の異なる解決、客交代・空席更新、未知・未登録・メモリ上の欠損画像の共通人物記号と復帰、実客IDguest-12の信頼案件、森さん予約Extra、客IDなし旧Extraの代替、描画・回想前後の保存本文を含む状態一致。
- 解放済みfixture：通常人気挑戦を開始し隔離メモリの人気をcapへ置いて通常休業決算。第1段階後は不在、第2段階達成当日も不在、翌日予定と通常到着刻を確認。自然な人気獲得とは区別。固定名・標準データ・Python cafe_customers/core.cafe_weekdays・Unity CustomerScheduleの第2段階条件を維持（達成記録と翌日以降、標準到着刻10、既存来店制限）。
- [着席状態](Results/YamadaIcon/seated.json)：4日目31刻、資金1850、山田さん/席4/コハク。通常save_game/load_gameで別backendへ再開し、全席客IDを保持。
- [別プロセス再開5項目](Results/YamadaIcon/manual-result.txt)・[復元状態](Results/YamadaIcon/resumed.json)。[実入力メモ](Results/YamadaIcon/input-notes.txt)：1024×768で実マウスによる猫一覧→席一覧→席4詳細→閉じる。名前・担当猫・画像を保持し、日数・刻・資金不変。詳細で体力17を確認。自動resume検査と実入力は別の証拠。
- [席1280](Results/YamadaIcon/actual-seated-yamada-1280.png)／[席1024](Results/YamadaIcon/actual-seated-yamada-1024.png)／[信頼1280](Results/YamadaIcon/trust-guest-12-1280.png)／[信頼1024](Results/YamadaIcon/trust-guest-12-1024.png)。描画fixtureの席1置換画像には本来の席4と重複表示されるが、実着席画像・保存は通常客IDを保持。[PNG検査](Results/YamadaIcon/image-checks.json)も成功。

未確認：自然な人気獲得からの登場、信頼回復の自然発生、1280×800実入力、実台帳のGUI手入力、5席カード全体。既存の下段切れは空席5に残るが山田さんの席4詳細確認は可能だった。UI修正・長期評価・総合確認一式は行わない。通常客12人と森さんの登録・画像が揃った範囲であり、特殊客・拡張客を含む全客完成ではない。

## 容量・作業分離

| 対象 | 制作開始 bytes | 制作終了・引継ぎ開始 bytes | 増減 bytes |
| --- | ---: | ---: | ---: |
| Library | 5635917932 | 5623246147 | -12671785 |
| content_images | 1203771636 | 1205073710 | +1302074 |
| Logs | 15546409352 | 15655380078 | +108970726 |
| Docs/Prototypes | 528330428 | 529633171 | +1302743 |
| Latest | 3330006800 | 3412903856 | +82897056 |

[制作開始](Results/YamadaIcon/capacity-start.json)／[制作終了](Results/YamadaIcon/capacity-end.json)／[引継ぎ開始](Results/YamadaIcon/handoff-capacity-start.json)。Logs約109.0MB増にはLatest約82.9MB増を含み、13人人物パックと画像・検証記録等が残る。旧パックを保持するため配布収録量も増える。Libraryは既存プロジェクトの通常キャッシュ更新。引継ぎではビルド・素材・Libraryを増やさず、資料と監査記録だけを追加。

Latestは156ファイル・3,412,903,856 bytesの成功ビルド一組、Pendingなし。StandaloneBuild直下はLatest/Runsとbuild.lock。ロックや他作業成果物を削除しない。追加Build・ZIP・Unityコピーなし。素材原本・旧パック・セーブを保持。

山田さん由来だけを各リポジトリへコミット、pushなし。人物台帳はHEADへ山田さん一行を加えた内容だけstageし、catalog-2等の先行猫差分とcatalog-3/ユキ差分を作業ツリーに保持。リン・ユキの素材/取り込み/検証コード・猫制作予定・presentation/Narrativeの他作業差分は含めない。コミットIDは最終報告と共有Docs追補に記録する。

引継ぎ終了：[容量snapshot](Results/YamadaIcon/handoff-capacity-end.json)。開始比Logsのみ監査スクリプト2,723 bytes増、他の測定対象は不変。Python `7c2f4a1`、Unity `a24a094`、共有Docs `dd223d6`。Tool本体は `467c4f3`、終了容量追記は後続コミット。
