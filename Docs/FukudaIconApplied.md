# 福田さん（advanced-quiet）のプロフィール・人物アイコン

2026-10-26。一人・透過画像一案。制作対象 `person-advanced-quiet` → ゲーム客 `advanced-quiet` を明示対応。既存15人を保持し16人へ。既存客の描き直し・プロフィール自動生成・本文追加なし。

## 採用プロフィールと画像

確定設定：福田さん、静かに過ごしたい条件付き接客客。新規人気3段階モード、第1段階達成翌日以降、月・水・金の営業開始刻0に来店。静かな交流スペースの席で通常の静かな交流（voice・presence）合計3回以上が成功条件、追加料金150・追加人気3。設備は接客成功の条件であり来店の必須条件ではない。常連の追加曜日はID共通のExtraWeekday経路から成立し得る。曜日・設備・条件は変更しない。quietという内部IDだけから人格を決めない。

今回の制作設定：実直で、猫の小さな動きを楽しみ、反応をよく見て待てる。短く一度声をかけ、近づくかどうかを猫に任せ、休憩や拒否を尊重。飾らない短い丁寧語「ここにいてもいいですか」「少し待ってみます」「ひと休みにしましょう」。猫の呼び方 `{cat}`。命令・幼児語・猫の気持ちや結果の断定を避ける。職業・来歴・来店歴・年齢設定は増やさない。

外見：低い位置で小さくまとめた炭色の髪、横分け、眼鏡なし、くすんだ青の襟なし上着と生成りのインナー。表情を先に設計し、小さめの縦長開眼・左右を揃えたゆるい山形眉・笑いを強めない小さな閉じ口を指定した。実生成では予定より目がやや大きく、控えめな口元の柔らかさ、横流し前髪が残る。視線は概ね正面であり下向きの効果は主張しない。坂本さんの細い下がり目・片眉・開いた口とは区別でき、強い感情に固定しない日常の聞き顔として採用。

[採用PNG](../../Docs/Prototypes/FukudaIcon-v1/fukuda-icon.png)は1254×1254 RGBA・alpha0〜255。藤原さん・坂本さんの最新PNGを実際に見て画風参照とし、組み込みimagegen一回の生成原本をそのまま採用。[完全プロンプト](../../Docs/Prototypes/FukudaIcon-v1/prompt.txt)。比較シート・差分・立ち絵なし。

## Tool・Unity登録

既存SubjectRegistry / HumanPortraitStoreによる保存・別インスタンス再読込・人物専用書き出し。通常操作の既存APIを使う `register_fukuda.py`、[登録結果](Results/FukudaIcon/registration.json)。名前ではなくpresentation.special_namesのID対応も照合。既存全プロフィール行・15画像保持：[保持照合](Results/FukudaIcon/preservation.json)。人物専用パック `ce25379edf1b80fb820bbd0257fade9b` 一組、同じ内容の再出力は同じ場所。猫画像・猫catalogを再書き出しせず、旧パック・素材・セーブを保持。

通常ContentPreviewSetup.Importから実Unityへ取り込み。[16画像hash・Importer照合](Results/FukudaIcon/unity-data-checks.json)：人物用max1024、非圧縮、alpha保持、mipmap/read-write無効、Standalone overrideなし。猫用BC7を適用しない。猫manifest・採用済み本文・猫catalogのhashは開始時と一致。

指定build_standalone.ps1 -ProjectPath C:/Users/hi-wa/unity_workspase/CatCafeSim -ExecuteMethod CatCafePreview.Editor.HumanIconsApplyVerification.VerifyFukudaAndBuild、共有StandaloneBuildStorage、固定Latestを使用。成功ログ `Logs/StandaloneBuild/Runs/20261026-055915-106-09caec3591dd49699151e643f03709c4.log`。exit0、CAT_CAFE_FUKUDA_IMPORTED・CAT_CAFE_STANDALONE_BUILD_OK。最初のsandbox Unityはライセンス接続でexit199、通常環境で成功した。ビルド後のソース改変は同内容の改行整形のみ。

## 限定検証

- Tool人物画像11試験成功。固有バックエンド5件＋Tkボタン1件、継承重複5件を含む。隔離台帳・ファイル選択差し替えのUI自動試験であり実台帳のGUI手入力ではない。実台帳はストアAPI保存・再読込。[ログ](Results/FukudaIcon/gui-tests-workspace.txt)。
- Python静かな交流客の既存回帰8件成功・GUI1件skip。[ログ](Results/FukudaIcon/quiet-tests-workspace.txt)。短期目標・play_customer除去等の既存fixtureを含み標準値の自然プレイではない。設備あり3回・不足・設備なし・保存・重複報酬防止等を既存試験で確認。初回OS一時領域はsandboxのファイル置換権限で失敗、許可済みLogs/ToolTestTempを使って成功。
- [Player133項目成功](Results/FukudaIcon/player-result.txt)。16画像の別実体解決、1280×800・1024×768の席ID別表示、客交代・未知ID・空席更新、未登録・メモリ上の欠損画像の共通人物記号への代替と復帰。描画前後の既存本文・営業状態不変。既存予約Extraと旧Extra客IDなし代替も確認。
- 解放fixtureは通常new_game(popularity)から開始。隔離メモリの人気だけcapへ設定し通常day_offで第1段階を達成、当日は不在。2〜8日目の来店曜日を本番CustomerScheduleとquiet_weekdaysで照合。通常日送りで3日目（水）へ進み、刻0の待機列到着から通常assign_serviceで席1へ着席。強制到着ではなく通常の条件判定・営業開始を使用するが、自然な人気獲得ではない。設備なしでも来店・着席することを確認。
- 接客は本番SpecialistResultへ設備あり2回→失敗、設備あり3回→成功、設備なし3回→失敗を直接入力する境界fixture。自然な接客達成や報酬獲得とは区別する。Python core/cafe_quiet_customer.py / cafe_growth.py、config/cafe_quiet_customer.json / cafe_play_customer.json、Unity CafeCustomers / CafeSpecialistsを照合。通常voice・presenceだけがquiet群、休憩・切替・特別交流は条件回数へ含めない。
- 信頼回復は客IDで除外されず既存の累積不満・来店停止で対象となり得る。停止履歴を隔離fixtureで置き、PresentCustomerTrustの既存警告対象化・ID画像表示を確認。[信頼画面](Results/FukudaIcon/trust-advanced-quiet-1024.png)。自然発生・回答後一式・新イベントは対象外。
- 通常save_game/load_gameで別backendと別プロセスへ再開。[別プロセス5項目](Results/FukudaIcon/manual-result.txt)と[全席フィールド・日・刻・資金一致](Results/FukudaIcon/image-checks.json)。3日目・刻1・資金880、席1／福田さん／コハクを保持。
- [1280×800](Results/FukudaIcon/actual-seated-fukuda-1280.png)と[1024×768](Results/FukudaIcon/actual-seated-fukuda-1024.png)を実寸で目視。席の34 logical px（1024では約27px）ではまとめ髪・青い襟が主な手がかり。細かな目・眉・視線まで読み取れるとはしない。名前も識別を担う。大きい信頼画像では開眼と閉じ口が読める。
- computer-useスキルのskyで再開Playerを前面にし、席1詳細を実クリック。福田さん／コハク／体力100／文章の記録はまだありません、を確認。[入力後PNG](Results/FukudaIcon/manual-after-input-1024.png)は実描画1024×768、外枠込み1026×800。[入力記録](Results/FukudaIcon/manual-done.txt)。最初の非表示PlayerはUI対象にならず、この作業専用の2プロセスを終了して可視Playerで再確認した。非表示起動を実入力成功とは数えない。

未確認：標準人気の自然獲得からの登場・接客条件の自然達成、常連化と追加曜日来店の自然発生、信頼回復の自然発生・回答後一式、1280×800での実入力、実台帳のGUI手入力。5席第2段カード下部切れは修正せず、2席検証には影響なし。長期評価・総合確認一式は繰り返さない。

## 保護と容量

開始時HEAD・全未コミットファイルhashはLogs/FukudaIcon/baseline.json。Tool21e57c5、Python ee63bd1、Unity ea52c59。関連チャットidle、Unity/Player不在を確認して開始。猫制作の日程・設定・素材・本文等の先行差分を保持：[照合](Results/FukudaIcon/prior-differences-preserved.json)。人物台帳はHEADに福田さん一行だけ加えた内容を選択コミットし、開始時の他行差分は作業ツリーに保持。pushなし。

[開始・終了容量と増減bytes](Results/FukudaIcon/capacity-final.json)。原本PNG一枚・登録素材一枚・16人人物専用パック一組のみ追加。旧人物パックを保持するため、Latestには16人分の新パックが収録され増加する。成功物Latest一組、StandaloneBuild直下はLatest/Runsのみ、Pendingなし。ビルド複製・ZIP・別Unityプロジェクト・検証専用Libraryなし。素材・旧パック・セーブは削除しない。

今回コミット：Python `4772145`、Unity `d2f07fe`、共有Docs `b0b57a0`、Tool本体 `a2bd9bf`。最終容量・本追記だけを後続Toolコミットへ保存。pushなし。
