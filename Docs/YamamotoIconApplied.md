# 山本さん（guest-7）のプロフィール・人物アイコン

2026-10-25。山本さん一人について、一案だけ制作して登録。既存Toolの人物保存・画像登録・人物専用書き出しとUnityの客ID別解決を利用し、プロフィール自動生成・UI再設計・ルール/数値/登場条件変更なし。

## 採用内容

- 制作対象ID `person-guest-7` → ゲーム客ID `guest-7`。名前で推定しない。
- 性格：率直で落ち着いており、猫の小さな反応をよく見る。
- 接し方：短く声をかけて待つ。遊びや接触を重ねず、猫が離れたら静かに見守る。
- 口調：気負わない短い常体。「〜してみる？」「〜もいいね」「ここで休もう」。猫の呼び名 `{cat}`。一人称はなるべく省略。命令・幼児語・猫の気持ちや結果の決めつけを避ける。
- 外見：額を見せた赤褐色の短髪、幅のある柔らかな角形の輪郭、やや太い眉、眼鏡なし、くすんだ橙のカーディガンと象牙色の襟、閉じた口の微笑み。年齢・性別の文章設定・職業・来歴・来店歴は固定しない。

既存確定設定は `cafe_customers.py` の名前・客IDとゲーム登場条件。実人物台帳・人物画像台帳・Docsに専用設定・画像・制作途中データはなく、性格・口調・外見は今回の制作設定。最新Tool `bc8cf72`、Python `3c4aa11`、実Unity `7ec45f5`、共有Docs `2e97cbf` と未コミット差分を確認。人物関連チャットと猫作業チャットはidle、Unity/Playerプロセスなし。既存リン素材・猫制作日程・設定・人物台帳の先行差分を保持。

[採用原本](../../Docs/Prototypes/YamamotoIcon-v1/yamamoto-icon.png)：1254×1254 RGBA、alpha0〜255、SHA256 `77665685f662f477bfd65b030812e4347f918933a402cce795df8815d4ae97d1`。[制作設定・完全なプロンプト](../../Docs/Prototypes/YamamotoIcon-v1/README.md)。組み込みimage_genを一回使用。最新7人を目視し、上限5参照に佐藤・高橋・田中・伊藤・渡辺を使用。比較案・シート・表情差分・立ち絵なし。

## 登録・取り込み・保護

`register_yamamoto.py` は既存 `SubjectRegistry.save` / `HumanPortraitStore.register/export` を使用。保存・別インスタンス再読込、既存全人物行と7画像対応の保持、人物専用8人パック、同内容の出力先再利用を確認。[登録結果](Results/YamamotoIcon/registration.json)。GUI手入力で実登録したとの主張ではない。

人物パック `7e78731fb131307f6b5aa55a220cef42` 一組。通常 `ContentPreviewSetup.Import` → 既存HumanPortraitImportで取り込み。8画像hash・明示ID・既存Importer（Default max1024・非圧縮・alpha保持・mipmap/read-write無効・Standalone overrideなし）を[検査](Results/YamamotoIcon/unity-data-checks.json)。猫BC7を適用せず、猫bundle/catalogを再書き出ししない。既存本文・猫manifest・実Unity猫catalogの開始hash一致。素材・旧パック・ユーザー保存を削除/更新しない。

## 検証区分

- Toolデータ検査：保存再読込、既存全人物保持、画像hashと透過、8人書き出し・同出力再利用・保護対象hashが成功。
- Tool回帰：バックエンド5件成功・GUI6件skip。[ログ](Results/YamamotoIcon/tool-tests.txt)。隔離fixtureの実Tkボタン経路（ファイル選択等は差し替え）のGUI自動操作1件成功。[ログ](Results/YamamotoIcon/gui-tests.txt)。実台帳の手入力・実マウス登録とは区別する。
- 人気追加客Python回帰：8件成功・1件skip。[ログ](Results/YamamotoIcon/popular-tests.txt)。初回はWindows既定文字コード、次はOS一時領域の置換権限で失敗し、UTF-8とワークスペース内一時領域に切り替えて成功。製品コード変更なし。
- 登場条件：Python `popular_customers/popular_unlocked_day/popular_schedule` とUnity `CafeCustomers.CustomerSchedule`、標準rulesを確認。通常客6人の次がguest-7＝山本さん。第1段階の達成済み記録が必要で、`day > resolved_day`。標準第1段階4人のうち先頭、通常到着刻4。曜日制限なし。雨天・不満/信頼制限等は別途維持。
- Playerの準備は解放済みfixture。新規隔離backendの通常コマンドで人気挑戦を開始し、メモリ上の人気だけを上限へ設定、通常休業決算で第1段階達成、成長選択・目標継続・翌日・店舗イベント回答を実行。未解放/達成当日はguest-7なし、翌日は予定に入る境界を検査する。ルール・達成値・日程を緩めない。自然な人気達成の実証とはしない。
- 初回準備は人気挑戦未開始かつ人気を達成値ぴったりに置き、達成しなかった。修正後は先着の専門客2人で2席が埋まりguest-7が待ち時間切れ。通常の3席目購入（500）を加える。途中失敗を成功根拠に混ぜない。
- 席・信頼回復・予約Extraは既存view/event/replay fixtureによる実Player UI自動描画。信頼案件の実客IDguest-7から画像を解決する。自然発生や物理入力とは区別する。第一席だけ置換する撮影fixtureでは他席・右欄が元のままの場合がある。
- 実入力、自然な人気達成からの登場、信頼回復の自然発生は未確認。長期評価・総合確認一式は繰り返さない。

## 最終Player結果

指定スクリプト・既存Unity・共有StandaloneBuildStorageの固定Latestでビルド成功。最終ログ `Logs/StandaloneBuild/Runs/20261025-133656-406-d60512c157e645199d47d3d0c903400c.log`、exit0・CAT_CAFE_STANDALONE_BUILD_OK。検証コードの準備修正で固定Latestを更新し、別Build/ZIP/Unityコピーは作らない。

固定Latest `--catcafe-human-icons <隔離出力> --catcafe-yamamoto` はexit0・[71項目成功](Results/YamamotoIcon/player-result.txt)。解放済みfixtureで2日目・営業6刻にguest-7が3席目へ着席。通常save_game/load_gameコマンドで保存→別backendインスタンスの再開後も全席の客IDと山本さんの着席を保持（別プロセス再開とは主張しない）。8画像が別々、未登録guest-8・未知ID・メモリ上だけの欠損参照は共通人物記号、元参照復帰で採用画像。席交代・空席更新、guest-7を持つ信頼回復fixture、森さんの予約Extra/予約ADV、IDなし旧Extraの共通記号を確認。描画・回想前後のゲーム状態JSON（保存本文を含む）は一致。

[席1280](Results/YamamotoIcon/seat-guest-7-1280.png)／[席1024](Results/YamamotoIcon/seat-guest-7-1024.png)／[信頼1280](Results/YamamotoIcon/trust-guest-7-1280.png)／[信頼1024](Results/YamamotoIcon/trust-guest-7-1024.png)を目視。全20PNGの実寸・非黒画像[検査](Results/YamamotoIcon/image-checks.json)も成功。赤褐色髪と橙服は識別できるが、席34 logical px（1024時約27px）の細線まで読めるとはしない。第一席だけ差し替える席fixtureでは山本さんが第1・第3席に見えるが、保存した本来の状態は第3席一人。既存本文・ゲーム状態に二重登録した結果ではない。

## 容量（bytes）

| 対象 | 開始 | 終了 | 増減 |
| --- | ---: | ---: | ---: |
| Logs | 15223617065 | 15274875611 | +51258546 |
| Logs/StandaloneBuild/Latest | 3120254904 | 3153815688 | +33560784 |
| cat_cafe_sim/saves/content_images | 1138858692 | 1140146088 | +1287396 |
| C:/Users/hi-wa/unity_workspase/CatCafeSim/Library | 5320199035 | 5313572905 | -6626130 |

Latestは156ファイルの成功ビルド一組、直下Latest/RunsだけでPendingなし。Latest +33,560,784 bytesは既存人物を保持した新8人パックの非圧縮texture等。Logs +51,258,546 bytesはLatest増分を含み、人物パック・20PNG・4回の固定出力ビルドログ等。登録元 +1,287,396 bytes、既存Libraryは6,626,130 bytes減。原本・旧パック・素材・セーブを整理削除せず、追加Libraryは作らない。容量は終了スナップショット（記録自身の微小増分を除く）。

今回分だけを各Gitへ保存、pushなし。人物台帳はHEADへ山本さん一行だけを加えてステージし、先行の台帳差分・猫設定・リン素材/制作日程は作業ツリーへ保持する。

今回の登録コミット：Python `031a449`、実Unity `5e60fc1`、共有Docs `91c87b8`。Toolは本資料と同じコミットに登録。pushなし。
