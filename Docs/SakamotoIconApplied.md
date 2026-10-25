# 坂本さん（advanced-longhair）のプロフィール・人物アイコン

2026-10-26。一人・透過画像一案を制作し、既存ToolのSubjectRegistry / HumanPortraitStoreで登録。制作対象 `person-advanced-longhair` → ゲーム客 `advanced-longhair` を明示し、名前だけで対応させない。プロフィール自動生成や既存客の描き直し、セリフ本文追加は行わない。

## 採用プロフィールと表情

- 確定設定：坂本さん、長毛猫好き。新規人気3段階モードで第2段階達成翌日以降、日付・seedのSHA-256による30％の判定で営業開始刻0に来店する。長毛猫 `long_hair` との通常触れ合い3回以上で追加料金120・追加人気2。必須設備なし。長毛は猫側の条件であり人物の髪の長さを指定しない。「気まぐれ」は既存の客種名として保持し、確率来店から人格を断定しない。
- 今回の制作設定：気さくで、猫の反応を面白がりながら待てる。軽く一度誘って見守り、触る前に距離を確かめ、嫌がれば引いて休憩を尊重する。
- 口調：短く気さくな常体。「〜してみようか」「そっちが気になる？」「ここで待ってるよ」。呼び方 `{cat}`。命令・幼児語・猫の気持ちや結果の断定を避ける。職業・来歴・来店歴・年齢を増やさない。既存本文は変更しない。
- 外見：額の一部が見える短い濃茶の髪、眼鏡なし、黄土色の襟付きシャツと生成りのインナー。
- 表情：細めの開いた下がり目、片側を少し上げた左右差のある眉、話しかけるように小さく開いた口。藤原さんの横長開眼・水平眉・閉じ口、山田さんの弧の閉じ目・開いた笑い口と差をつけた。実際の生成結果は口角に明るさがあり、短髪も刈り上げではなく軽い束のある髪となった。下向き視線は控えめで、小表示の識別要素とは断定しない。

[採用PNG](../../Docs/Prototypes/SakamotoIcon-v1/sakamoto-icon.png)は1254×1254 RGBA、alpha0〜255。藤原さん・山田さんの最新採用PNGを実際に確認して画風参照に使用。組み込みimage_gen一回の原本を編集せず採用。[完全プロンプト](../../Docs/Prototypes/SakamotoIcon-v1/prompt.txt)。比較シート・表情差分・立ち絵なし。

## 登録・保護・条件照合

`register_sakamoto.py` は既存ストアの保存、別インスタンス再読込、人物専用書き出しを利用。[登録結果](Results/SakamotoIcon/registration.json)／[開始14人・終了15人の保持照合](Results/SakamotoIcon/preservation.json)。パック `7ecb8f766e46aec8cba9a3a895fa4374` 一組、同内容再出力は同じ場所を使用。猫画像・猫catalog再書き出しなし。通常のContentPreviewSetup.Importで実Unityへ取り込む。[画像hashとImporter検査](Results/SakamotoIcon/unity-data-checks.json)：max1024・非圧縮・alpha保持・mipmap/read-write無効、Standalone overrideなし。猫BC7を人物に適用しない。猫manifest・採用本文・猫catalogは開始hashと一致。

Python `core/cafe_longhair_customer.py`、config/cafe_longhair_customer.json、Unity CafeSpecialists / CafeCustomersを照合。通常なでる・ブラッシングのcontact群を数え、休憩・切替・特別交流は含めない。常連はCafeCustomersのID共通の忠誠度閾値から成立し、翌日以降のExtraWeekdayに追加来店し得る。長毛客の保存検証にも常連曜日の例外が既存実装にある。確率来店と常連追加来店を混同せず、変更しない。信頼はIDで除外されず、既存の累積不満・来店停止条件で坂本さんも対象になり得る。新しいイベントを作らない。

## 限定検証

- Tool人物画像11試験成功（継承バックエンド5件の重複を含む、固有バックエンド5＋Tkボタン1）。隔離データ・ダイアログ差し替えのUI自動試験。実台帳のGUI手入力とは区別。[ログ](Results/SakamotoIcon/gui-tests.txt)。実台帳保存・再読込はストアAPIで別途確認。
- 長毛客の既存Python限定回帰6件成功、表示必須1件はskip。[ログ](Results/SakamotoIcon/longhair-tests.txt)。この既存試験は短期目標・確率1などを使う場面もあり、標準値での自然プレイではない。
- 最初のsandboxテストは一時ファイル置換権限で失敗。通常権限で上記の成功を確認。初回Player fixtureは自由営業の途中開始人気挑戦を使っており、新規人気モード限定の長毛客が来ず、休業継続で経営終了になった。通常new_game(popularity)から始めるfixtureへ修正。ゲームの条件・数値を変更せず、この失敗を本番の登場不具合とは扱わない。

最終Player・保存・表示・容量は後段に記録する。

## 最終Player・保存・実入力

指定build_standalone.ps1と固定Latest、既存Unityプロジェクトを使用。通常取り込みのVerifySakamotoAndBuildで15画像の別実体解決、後続は通常の既定ビルド入口。最終成功ログ `Logs/StandaloneBuild/Runs/20261026-053505-736-8d3416e2795d47c59ba93dc724ecb458.log`。exit0・CAT_CAFE_STANDALONE_BUILD_OKを確認。共有StandaloneBuildStorageを使い、新しいBuildPipeline.BuildPlayerは追加しない。

- [Player123項目](Results/SakamotoIcon/player-result.txt)成功。15画像を別実体に解決し、両解像度で席の画像対応・客交代・空席・未知／未登録／メモリ上の欠損画像の共通記号代替と復帰、既存予約Extra・旧Extra客IDなし代替を確認。描画前後のゲーム状態・既存本文不変。
- 解放fixture：通常new_game(popularity)で開始。隔離メモリの人気のみcapに置き通常休業決算し、第1段階では未解放、第2段階達成当日も不在を確認。seed0の日付抽選は3日目不来店、4日目来店（0.19864 < 0.3）。本番の日付抽選・来店予定・営業開始を使う。人気の自然獲得とは区別。
- 自動割当の最初の試行では他客で席が埋まり、坂本さんは着席前に待機終了。実装不具合ではなく検証の選択が不適切だったため、営業開始前に通常手動割当へ切替。刻0の通常到着を待機列で確認し、既存assign_serviceで席1へ選び、その後自動割当へ戻した。刻0到着直後の時間送りは手動待機ガードが拒否したため、不要なadvance_businessをfixtureから除いた。強制到着・本番条件変更なし。
- 接客は本番SpecialistResultへ長毛／2回→失敗、長毛／3回→成功、短毛／3回→失敗を入力する境界fixture。設備falseでも成功。自然な接客3回・報酬発生とは区別。Python既存限定回帰で自動触れ合い・会計・保存を別途確認。
- [着席1280×800](Results/SakamotoIcon/actual-seated-sakamoto-1280.png)／[着席1024×768](Results/SakamotoIcon/actual-seated-sakamoto-1024.png)を実寸確認。短髪と黄土色の襟が小アイコンの主な手がかり。細い開眼・片眉・開いた口は[大きい信頼表示](Results/SakamotoIcon/trust-advanced-longhair-1024.png)で確認。27〜34pxで細かな眉や視線まで読めるとはしない。名前表示も識別を担う。
- 信頼は来店停止履歴を隔離fixtureで用意し、本番PresentCustomerTrustで坂本さんが既存警告対象となることとID画像対応を確認。自然発生・回答後一式・新イベントは対象外。
- [4日目・刻1・資金820、坂本さん／コハク／席1の保存](Results/SakamotoIcon/seated.json)を通常save_game/load_gameで別backendと別プロセスへ再開。[別プロセス5項目](Results/SakamotoIcon/manual-result.txt)成功、[全席フィールド・日・刻・資金一致](Results/SakamotoIcon/image-checks.json)。
- computer-useスキルのskyで1024×768要求の再開Playerを前面にし、実マウスで席1詳細を開いた。[実入力後PNG](Results/SakamotoIcon/manual-after-input-1024.png)は実描画1024×768、skyの外枠込みウィンドウは1026×800。坂本さん／コハク／体力100／文章の記録はまだありません、を確認。[入力記録](Results/SakamotoIcon/input-notes.txt)。最初の遮蔽されたキャプチャは他アプリを写したため入力せず、選択済みPlayerを前面にして再観察後にクリックした。

未確認：自然な人気獲得からの登場・接客達成、常連化と追加来店の自然発生、信頼回復の自然発生と回答後一式、1280×800の実入力、実台帳のGUI手入力。常連条件はコード照合まで。5席の第2段カード下部切れは修正せず、今回は2席で確認の妨げなし。5席再検証・長期評価・総合一式は繰り返さない。

## 容量と作業分離

| 対象 | 開始bytes | 終了bytes | 増減bytes |
| --- | ---: | ---: | ---: |
| C:\Users\hi-wa\unity_workspase\CatCafeSim\Library | 5775118994 | 5753449698 | -21669296 |
| C:\Users\hi-wa\source\codex_repos\CatCafeSim\Docs\Prototypes | 531029991 | 532353722 | 1323731 |
| C:\Users\hi-wa\source\codex_repos\CatCafeSim\cat_cafe_sim\saves\content_images | 1206469498 | 1207793129 | 1323631 |
| C:\Users\hi-wa\source\codex_repos\CatCafeSim\Logs\StandaloneBuild\Latest | 3471635296 | 3534561608 | 62926312 |
| C:\Users\hi-wa\source\codex_repos\CatCafeSim\Logs | 15735374219 | 15823440039 | 88065820 |

Latest成功物一組・Pendingなし。StandaloneBuild直下のディレクトリはLatest/Runsのみ。ビルドは複製せず置換。原本PNG一枚、登録素材一枚、15人人物専用パック一組を追加し、旧素材・旧パック・セーブは保持。Latest増加は旧パック保持に加わる15人のtexture収録、Libraryは約21.7MB減少し、既存プロジェクトの通常Importer／PlayerDataCache更新。別Unityプロジェクト・検証専用Library・ビルドZIPは作らない。

開始HEAD：Tool c7dfa2a、Python e38e8b2、Unity 2e92abb、Docs a54478a。関連チャットidle・Unity/Player不在を確認して開始。開始未コミット差分はTool11行、Python3行、Unity118行（git status件数）をsnapshot保存。猫制作の日程・設定・素材・presentation／Narrative等を保持。共有人物台帳の全既存行と画像14人保持を照合。コミットは今回分だけ選択しpushしない。最終コミットIDは最終回答で報告する。
