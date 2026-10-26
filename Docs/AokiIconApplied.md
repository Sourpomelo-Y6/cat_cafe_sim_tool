# 青木さん（advanced-contact）のプロフィール・人物アイコン

2026-10-26。一人・透過画像一案。制作対象 `person-advanced-contact` → ゲーム客 `advanced-contact` を明示対応。実台帳の既存17人を保持し18人へ。既存客の描き直し・プロフィール自動生成・会話本文追加なし。

## 採用プロフィールと表情

確定設定：青木さん、触れ合いを希望する条件付き接客客。新規人気3段階モードで第1段階達成の翌日以降、日曜日の開店刻0に来店。お手入れブラシの席で、通常のなでる・ブラッシングを合計3回以上行うと追加料金160・追加人気3。設備は接客成功条件であり来店必須条件ではない。既存の常連ExtraWeekday経路による追加曜日もあり得る。Python `core/cafe_contact_customer.py`・`cafe_growth.py`、`config/cafe_contact_customer.json`、Unity `CafeCustomers`・`CafeSpecialists`を照合。contactという内部IDや回数条件から接触を強要する人格を決めない。条件・数値は変更なし。

今回の制作設定：相手の様子を確かめ、落ち着いて聞く実直な人。触れる前に短く声をかけ、猫が近づくか待つ。拒否・休憩を尊重し、追わず手を止める。短く穏やかな丁寧語「少し待ちましょうか」「ここで見ていますね」「今日は休みましょう」。猫の呼び方 `{cat}`。命令・幼児語・猫の気持ちや結果の断定を避ける。職業・来歴・年齢・来店歴は増やさない。

福田さんと西村さんの最新採用PNGを実際に見てから表情を設計。小さめで横長の開眼・小さい正面向きの瞳・水平寄りの太い両眉・小さく開いた口の聞き顔を指定した。実生成は目と虹彩が指定より大きく、眉に緩い傾きがある。正面寄りの開眼と開いた口は、西村さんの半開きの横目・短い閉じ口、福田さんの閉じた口元と異なるため採用。大きな目と微笑みの組合せにはしない。強い怒り・悲しみ・驚きに固定しない日常の聞き顔。

外見：あごに届く暗い灰褐色のゆるいボブ、横へ流れた分け目、眼鏡なし。くすんだオリーブ色の襟なし上着と生成りのインナー。髪型・服色だけでなく口の開閉と視線も変える。

[採用PNG](../../Docs/Prototypes/AokiIcon-v1/aoki-icon.png)：1254×1254 RGBA、alpha0〜255。組み込みimage_gen一回の生成原本をそのまま採用。[完全プロンプトと参照](../../Docs/Prototypes/AokiIcon-v1/prompt.txt)。比較シート・表情差分・立ち絵なし。

## 登録・Unity・限定検証

- `register_aoki.py` は既存SubjectRegistry / HumanPortraitStoreを使用。保存・別インスタンス再読込・明示ID・人物専用書き出しを確認。[登録結果](Results/AokiIcon/registration.json)。人物パック `43fcb613be29b79299cc0ccc9fff9f3a` 一組、同内容の再出力は同じ場所。猫画像・猫catalogは再書き出ししない。
- 既存17画像・全プロフィール行を保持。[保持照合](Results/AokiIcon/preservation.json)。開始時の猫制作等199ファイルのhashが一致：[先行差分照合](Results/AokiIcon/prior-differences-preserved.json)。今回意図して更新した人物台帳は行比較で既存全行一致。開始Tool `aac36dc`（本体`713ae4b`）、Python `5229377`、Unity `cef95f0`、共有Docs `6c8cb6d`。関連人物チャットidle、Unity/Player不在を確認して開始。
- 通常ContentPreviewSetup.Importで実Unityへ取り込み。[18画像hash・Importer確認](Results/AokiIcon/unity-data-checks.json)：max1024・非圧縮・alpha保持・mipmap/read-write無効・Standalone overrideなし。猫用BC7は適用しない。猫manifest・既存本文・猫catalogのhashは開始時と一致。
- Tool人物画像11試験成功（固有backend5＋Tkボタン1＋継承重複5）。隔離台帳とファイル選択差替えのUI自動試験。実台帳への登録はストアAPIで、GUI手入力とは区別。[Tool結果](Results/AokiIcon/gui-tests.txt)。触れ合い客の既存テスト11成功・GUI1skip。通常触れ合い分類、回数不足・設備なし・成功報酬・保存・重複報酬防止等を短期fixtureで確認。[接客テスト](Results/AokiIcon/contact-tests.txt)。
- 指定 `build_standalone.ps1 -ProjectPath C:/Users/hi-wa/unity_workspase/CatCafeSim -ExecuteMethod CatCafePreview.Editor.HumanIconsApplyVerification.VerifyAokiAndBuild`、共有StandaloneBuildStorage、固定Latestを使用。初回sandboxはライセンス接続でexit199。通常環境でexit0、CAT_CAFE_AOKI_IMPORTED・CAT_CAFE_STANDALONE_BUILD_OK。成功ログ `Logs/StandaloneBuild/Runs/20261026-112613-491-f0c5d9c04ff849b294041b05033160da.log`。ビルド後のソース変更は同内容のLF整形のみ。
- [Player145項目成功](Results/AokiIcon/player-result.txt)。18画像別実体解決、1280×800・1024×768の席表示、客交代・未知ID・空席更新、未登録・メモリ上欠損画像の共通人物記号への代替と復帰を確認。既存予約Extra・IDなし旧Extraの代替、描画前後の営業状態・本文不変も確認。
- 来店検証は通常new_game(popularity)から開始。隔離メモリの人気だけcapへ設定して通常day_offで第1段階達成。達成当日は不在、2〜8日目の通常曜日判定を本番設定と照合し、通常日送りで7日目（日）刻0に到着。通常assign_serviceで席1へ着席。設備なしでも来店・着席する。人気の自然獲得ではないが、強制来店・着席fixtureとは区別する。
- 接客境界は本番SpecialistResultへ設備あり2回→失敗、あり3回→成功、なし3回→失敗を直接入力。自然な接客達成・報酬獲得ではない。条件回数は通常pet・brushのみで、切替・休憩・特別交流は含めない。
- 信頼回復は客IDで除外されず累積不満・来店停止から対象となり得る。停止履歴を隔離fixtureへ置き、本番PresentCustomerTrustによる警告対象化・ID画像表示を確認。[信頼画面](Results/AokiIcon/trust-advanced-contact-1024.png)。自然発生・回答後一式は検証対象外。
- 通常save_game/load_gameで別backend・別プロセス再開。[別プロセス5項目](Results/AokiIcon/manual-result.txt)、[全席・日・刻・資金一致](Results/AokiIcon/image-checks.json)。7日目・刻1・資金640、席1の青木さん／コハクを保持。ユーザーのセーブを利用せず隔離保存を使用。
- [1280×800](Results/AokiIcon/actual-seated-aoki-1280.png)／[1024×768](Results/AokiIcon/actual-seated-aoki-1024.png)を実寸目視。現行PythonServiceLayoutとprefabの席アイコンは24 logical pxで、1280では24px、1024では19.2px相当。実際の席prefabはPythonSeatCard（sceneのGUID対応も照合）。別のPythonCatCardは34pxであり、以前の人物資料の34px記述を席表示へ転用しない。小表示ではボブの輪郭・オリーブ色の上着と名前が主な手がかり。目・眉・口の細部まで読み分けられる保証はしない。信頼画面の大表示では開眼と開いた口が確認できる。
- computer-useスキルのskyで可視Playerを前面化し、席1詳細を実クリック。青木さん／担当コハク／体力100／文章の記録はまだありません、を確認。[入力後PNG](Results/AokiIcon/manual-after-input-1024.png)は実描画1024×768、外枠込み1026×800。[実入力記録](Results/AokiIcon/manual-done.txt)。1280はUI自動描画のみ。

未確認：標準人気の自然獲得からの登場、接客条件の自然達成、常連化・追加曜日来店の自然発生、信頼回復の自然発生と回答後一式、1280での実入力、実台帳GUI手入力。既存の5席第2段カード下部切れは修正せず、今回の2席確認には影響なし。長期評価・総合確認一式は繰り返さない。

## 保護・容量・コミット

猫制作の日程・素材・加入設定・本文・開始時差分を保持。人物台帳の今回コミットはHEAD＋青木さん一行だけとし、開始時の他行差分は作業ツリーへ保持。ルール・数値・登場／接客条件・セーブ形式・既存UIは変更なし。pushなし。

原本PNG一枚・登録素材一枚・18人人物パック一組のみ追加。旧パック・素材・セーブは削除しない。Latest成功物一組、StandaloneBuild直下はLatest/Runsのみ・Pendingなし。ビルド複製・ZIP・追加Unityプロジェクト・検証専用Libraryなし。[開始容量](Results/AokiIcon/capacity-start.json)。[終了容量](Results/AokiIcon/capacity-final.json)：Latest 3,672,998,320→3,748,508,192 bytes（+75,509,872）、Logs 16,010,353,985→16,112,170,665（+101,816,680）、Library 6,008,300,025→6,094,421,882（+86,121,857）。原本PNGは1,402,070 bytes、登録素材も同容量。主因は旧パックを保持した18人人物パックの追加収録と通常のUnity取り込み／ビルドキャッシュ。LogsにはLatest増加を含む。Latest156ファイルの成功物一組・Pendingなし。

コミット：Python `0817141`、Unity `bd86da6`、共有Docs `fd3d8b3`。Tool本体コミットは後続の最終参照記録へ記載。pushなし。


最終参照：Tool本体 `7c8fabd`、Python `0817141`、Unity `bd86da6`、共有Docs `fd3d8b3`。後続の本記録では新規テキスト成果物の改行をLFへ整形。ゲーム・画像内容は同一。最終再照合で先行199ファイルと既存人物行を保持、Latest一組・Pendingなし。
