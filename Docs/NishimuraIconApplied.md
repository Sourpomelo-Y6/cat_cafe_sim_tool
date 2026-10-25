# 西村さん（advanced-play）のプロフィール・人物アイコン

2026-10-26。対象一人・透過画像一案。制作対象 `person-advanced-play` → ゲーム客 `advanced-play`。開始時の実台帳16人を保持し17人へ。既存客の描き直し・本文制作・プロフィール自動生成は行わない。

## 採用プロフィール・表情

確定設定：西村さん、遊びを希望する条件付き接客客。新規人気3段階モードで第1段階達成翌日以降、火・木・土の営業開始刻0に来店。キャットタワーの席で通常遊び（ねこじゃらし・ボール・ぬいぐるみ・トンネル）合計4回以上が成功条件。追加料金180・人気4。設備は来店条件ではなく接客成功条件。常連追加曜日は既存ExtraWeekday経路があり得る。playという内部ID・客種名だけから陽気・活動的な人格を決めない。Python core/cafe_play_customer.py、cafe_growth.py、config/cafe_play_customer.json、Unity CafeCustomers・CafeSpecialists・CafeEquipmentを照合。条件・数値は変更なし。

今回の制作設定：落ち着いて観察し、猫の興味の向きを確かめる。一度短く誘い、追いかけず反応を待つ。拒否や休憩を尊重する。率直で静かな常体「気になるかな」「ここで待とう」「今日はこのくらいにしようか」。猫の呼び方 `{cat}`。命令・幼児語・気持ちや結果の断定を避ける。職業・来歴・来店歴・年齢は追加しない。

外見：暗い栗色の短髪、横流し前髪、眼鏡なし、赤土色の襟なしセーターと生成りの襟元。福田さん・坂本さんの最新採用PNGを実際に見てから、細い半開きの目・小さな虹彩・低めで外側が下がる眉・水平の閉じ口・横への視線を先に指定。実生成は眉が緩く斜めに上がり、短い閉じ口にわずかな柔らかさがある。目の半開きと横目、口を開かない落ち着いた観察顔を採用し、実際の形を台帳へ記録。福田さんの縦のある楕円開眼、坂本さんの開いた口とは区別。強い怒り・悲しみ・驚きに固定しない。

[採用PNG](../../Docs/Prototypes/NishimuraIcon-v1/nishimura-icon.png)：1254×1254 RGBA、alpha0〜255。組み込みimage_gen一回、生成原本を編集せず採用。[プロンプトと参照](../../Docs/Prototypes/NishimuraIcon-v1/prompt.txt)。比較シート・表情差分・立ち絵なし。

## 登録・取り込み・限定検証

- `register_nishimura.py` が既存SubjectRegistry / HumanPortraitStoreで保存・別インスタンス再読込・明示ID登録・人物専用書き出しを実行。[登録結果](Results/NishimuraIcon/registration.json)。人物専用パック `a744f5811c5c4eec0f95f4a6e4df3ebf` 一組、同内容再出力は同じ場所。既存プロフィール全行・16画像対応を保持：[照合](Results/NishimuraIcon/preservation.json)。猫画像・猫catalogは再書き出ししない。
- 通常ContentPreviewSetup.Importで実Unityへ取り込み。17画像hash・人物Importer max1024/非圧縮/alpha/mipmapとread-write無効を確認。猫BC7は適用しない。[データ検査](Results/NishimuraIcon/unity-data-checks.json)。開始時の猫manifest・既存本文・猫catalog hash一致。
- 人物Tool11試験成功（固有backend5＋Tkボタン1、継承重複5）。隔離台帳とダイアログ差し替えによるUI自動試験。実台帳GUI手入力ではなく実登録はストアAPI。遊び客既存回帰は11件成功・GUI1件skip。短期目標等の既存fixtureを含む。初回はcp932読込で8件失敗し、PYTHONUTF8=1で成功。本番データ不具合とは扱わない。[Tool](Results/NishimuraIcon/gui-tests-display.txt)／[遊び客](Results/NishimuraIcon/play-tests-utf8.txt)。
- 指定build_standalone.ps1、VerifyNishimuraAndBuild、共有StandaloneBuildStorage、固定Latestを使用。初回sandboxはライセンス接続でexit199、通常環境でexit0。成功ログ `Logs/StandaloneBuild/Runs/20261026-063048-804-1bea8529356b4380818b0055bd95516c.log`、CAT_CAFE_NISHIMURA_IMPORTED・CAT_CAFE_STANDALONE_BUILD_OK。ビルド後ソースは内容不変のLF整形のみ。
- [配布Player139項目](Results/NishimuraIcon/player-result.txt)成功。17画像別実体解決、1280×800・1024×768の客ID別席表示、交代・未知ID・空席更新、未登録・メモリ上の欠損画像の共通人物記号代替と復帰。既存予約Extraと旧IDなしExtra代替、描画前後の営業状態・既存本文不変を確認。
- 通常new_game(popularity)から開始し、隔離メモリの人気だけcapへ設定、通常day_offで第1段階達成。達成当日は不在、2〜8日目を本番曜日条件と照合。2日目（火）刻0の通常来店を待機列で確認し、通常assign_serviceで席1へ着席。設備なしでも来店する。人気自然獲得ではないが、強制来店・着席fixtureとは区別し、通常の条件判定を使用。
- 接客境界は本番SpecialistResultへ設備あり3回→失敗、あり4回→成功、なし4回→失敗を直接入力。自然な接客達成・報酬獲得とは区別。通常play群のみ集計し、休憩・切替・特別交流は含めない。
- 信頼回復はIDで除外されず、既存の累積不満・来店停止で対象になり得る。停止履歴を隔離fixtureで用意し、本番PresentCustomerTrustで警告対象化・ID画像を確認。[信頼表示](Results/NishimuraIcon/trust-advanced-play-1024.png)。自然発生・回答後一式は対象外。
- 通常save_game/load_gameで別backendと別プロセス再開。[別プロセス5項目](Results/NishimuraIcon/manual-result.txt)、[全席フィールド一致・描画寸法](Results/NishimuraIcon/image-checks.json)。2日目・刻1・資金940、席1の西村さん／コハクを保持。
- [1280×800](Results/NishimuraIcon/actual-seated-nishimura-1280.png)・[1024×768](Results/NishimuraIcon/actual-seated-nishimura-1024.png)を実寸目視。席は34 logical px、1024では約27px。短髪と赤土色の襟が主な識別要素。小表示で眉や横目の細部まで読めるとはしない。名前も識別を担う。大きい信頼表示では半開きと横目・閉じ口を確認。
- computer-useのskyで可視Playerを選択・前面化し席1詳細を実クリック。西村さん／コハク／体力100／文章の記録はまだありません、を確認。[実入力PNG](Results/NishimuraIcon/manual-after-input-1024.png)は実描画1024×768、外枠込み1026×800。[入力記録](Results/NishimuraIcon/manual-done.txt)。初回通常sandbox起動は描画初期化途中で終了し実入力根拠に数えず、通常環境の可視再開で確認。

未確認：標準人気の自然獲得からの登場、接客条件の自然達成、常連化・追加曜日来店の自然発生、信頼回復自然発生と回答後一式、1280×800実入力、実台帳GUI手入力。5席の第2段カード下部切れは修正せず、今回の2席確認には影響なし。長期評価・総合確認一式は再実施しない。

## 差分保護・容量

開始Tool b299265（福田本体a2bd9bfも確認）、Python4772145、Unity d2f07fe。関連人物チャットidle、Unity/Player不在を確認。全開始差分hashは `Logs/NishimuraIcon/baseline.json`。猫制作の日程・設定・素材・本文等199ファイルを保持：[hash照合](Results/NishimuraIcon/prior-differences-preserved.json)。人物台帳の今回コミットはHEAD＋西村さん一行のみとし、開始時の他行差分は作業ツリーへ保持する。pushなし。

容量は[開始](Results/NishimuraIcon/capacity-start.json)と終了記録を参照。原本1枚・登録素材1枚・17人人物パック一組だけ追加。旧パック・素材・セーブは削除しない。Latest成功物は一組、StandaloneBuild配下のディレクトリはLatest/Runsのみ・Pendingなし（共有build.lockファイルは保持）。ビルド複製・ZIP・Unityコピー・検証専用Libraryなし。

容量終了：Latest 3,601,683,304→3,672,998,320 bytes（+71,315,016）、Logs 15,914,135,861→16,010,292,800（+96,156,939）、Library 5,926,383,000→6,008,300,025（+81,917,025）。原本+1,320,362、登録素材等+1,320,493 bytes。主因は旧パックを保持した17人人物パックの追加収録と通常Unity取り込み／ビルドキャッシュ。複製ビルドはない。[終了内訳](Results/NishimuraIcon/capacity-final.json)。

Unityコミット `cef95f0`、共有Docs `6c8cb6d`。Python・Toolコミットは後続の最終記録へ記載。pushなし。
