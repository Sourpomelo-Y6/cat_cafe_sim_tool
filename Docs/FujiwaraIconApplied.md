# 藤原さん（advanced-white）のプロフィール・人物アイコン

2026-10-26。一人・一案を既存Toolの人物台帳／画像台帳へ登録。制作対象 `person-advanced-white` → ゲーム客 `advanced-white` を明示する。名前だけで対応させない。プロフィール自動生成・既存客の描き直し・本文追加なし。

## 採用設定

- 確定設定：藤原さん、白猫好き。人気第1段階の達成翌日以降、営業開始時（刻0）の条件付き接客客。担当猫の特徴whiteと、その接客中の「心をつかむ」1回以上で追加料金100。これはゲーム上の判定であり、厳格・高慢という人物像の根拠にはしない。
- 今回の制作設定：落ち着いて率直で、猫の小さな動きに関心を向ける。短く穏やかに一度誘って待ち、追わず、休憩を尊重する。
- 口調：簡潔な自然な丁寧語。「〜してみますか」「ここで待ちましょう」「ゆっくりで大丈夫です」。猫の呼び方 `{cat}`。命令・幼児語・猫の気持ちや結果の断定を避ける。職業・来歴・来店歴を増やさず、年齢・性別の文章設定も追加しない。
- 外見：片耳にかけた顎丈のアッシュブラウンのボブ、横分け、眼鏡なし。くすんだプラム色のハイネック、象牙色の襟縁。
- 表情：横長に開いた目、力を抜いた水平寄りの眉、小さな閉じ口、わずかに横への視線。生成結果は視線の横向きが控えめで、口元にもわずかな柔らかさが残る。大きな丸目＋微笑みや、直前の山田さんの閉じ目＋開いた口を繰り返さず、通常の聞き顔とした。

[採用PNG](../../Docs/Prototypes/FujiwaraIcon-v1/fujiwara-icon.png)は1254×1254 RGBA、alpha0〜255。山田・吉田の最新採用像を実際に見て、同じ線・色調・簡略度を画風参照にした。組み込みimage_gen一回の原本をそのまま採用。[完全プロンプト](../../Docs/Prototypes/FujiwaraIcon-v1/prompt.txt)。比較画像・表情差分・立ち絵なし。

## 登録と保護

`register_fujiwara.py` は既存SubjectRegistry / HumanPortraitStoreの保存・別インスタンス再読込・人物専用書き出しを使用。[登録結果](Results/FujiwaraIcon/registration.json)。通常客候補のgame_subjectsには特殊客が含まれないため、実presentation.special_namesの明示IDと固定名を照合して登録。Toolの候補機能・生成機能は変更しない。

人物パック `c61623ada8cf79395b9e9995c531dba2` は通常客guest-1〜12と森さんを保持する14人。既存全プロフィール行・画像対応を保持した[照合記録](Results/FujiwaraIcon/preservation.json)。同内容再出力は同じ場所を使う。猫画像・猫catalogの再書き出しなし。素材・旧パック・セーブを削除しない。

通常のContentPreviewSetup.Importから実Unityへ取り込み、[Unity画像・Importer照合](Results/FujiwaraIcon/unity-data-checks.json)。max1024、非圧縮、alpha保持、mipmap/read-write無効、Standalone overrideなし。人物に猫用BC7を適用しない。猫manifest・採用済み本文・猫catalogのhashは開始時と一致。

## 検証区分

- Tool人物画像11試験成功（継承バックエンド5件の重複を含む。固有バックエンド5件＋実Tkボタン1件）。隔離データ・ダイアログ差し替えの自動試験で、実台帳のGUI手入力とは区別する。[ログ](Results/FujiwaraIcon/gui-tests.txt)。
- 既存advanced-whiteのPython限定回帰6件成功。[ログ](Results/FujiwaraIcon/advanced-tests.txt)。この既存試験は短期目標設定等を使用しており、標準値の自然な長期プレイを意味しない。
- 条件はPython core/cafe_advanced_customers.py・config/cafe_advanced_customers.json、Unity CafeCustomers.CustomerSchedule / CafeSpecialists.SpecialistResultを照合。第1段階達成翌日、刻0、white＋connect_count1、追加100を維持。雨・来店停止・信頼等の既存可用性制限も変更しない。
- 信頼回復はPython consider_suspension、Unity PresentCustomerTrustが客IDで除外せず、藤原さんも既存の累積不満／来店停止条件で対象になり得る。固有の予約や新イベントは作らない。

最初のsandbox試験は一時ファイル置換権限で失敗したため、UTF-8の通常権限で再実行して上記成功を確認した。最初のsandbox Unityはライセンス接続待機でexit199、通常環境のビルド結果と区別する。

Player・実入力・容量・コミットは下記の最終記録へ追記する。

## 最終Player・保存・実入力

指定 `build_standalone.ps1 -ProjectPath C:/Users/hi-wa/unity_workspase/CatCafeSim -ExecuteMethod CatCafePreview.Editor.HumanIconsApplyVerification.VerifyFujiwaraAndBuild` と共有StandaloneBuildStorageを利用。成功ログ `Logs/StandaloneBuild/Runs/20261026-050557-978-2e3b7e7ef84c4271b402ac895e57c7c9.log`、exit0・CAT_CAFE_FUJIWARA_IMPORTED・CAT_CAFE_STANDALONE_BUILD_OK・CAT_CAFE_STANDALONE_SCREENS_BUILD_OKを確認した。

- [Player112項目成功](Results/FujiwaraIcon/player-result.txt)。14画像が異なる実体へ解決し、両解像度で全客の席画像照合／未知IDへの交代／空席更新を自動確認。未登録・未知・メモリ上の欠損画像は共通人物記号へ戻り、画像を戻すと復帰。既存本文・営業状態は席／ADV／Extra描画前後で一致。
- 解放済みfixture：通常人気挑戦を開始し、隔離メモリの人気だけcapに設定して通常休業決算。第1段階達成当日は不在、翌日刻0の来店予定、通常営業開始後の到着・着席を確認。自然な人気獲得とは区別する。接客結果は本番SpecialistResultへwhite/0回→失敗、white/1回→成功、非white/1回→失敗を直接入力して境界を確認した。通常の判定関数を使用する入力fixtureであり、実接客で自然発動を獲得したという意味ではない。
- 着席fixtureは上記解放後の通常営業から得た[2日目・2刻・資金940、席1／藤原さん／ソラ](Results/FujiwaraIcon/seated.json)。save_game/load_gameで別backendへ再開、さらに[別プロセス5項目](Results/FujiwaraIcon/manual-result.txt)で再開し、[全席の全フィールド・日・刻・資金一致](Results/FujiwaraIcon/image-checks.json)。保存形式や本文は変更しない。
- 信頼案件は隔離した来店停止履歴を用意し、本番PresentCustomerTrustで藤原さんの既存警告が出ることを確認した。信頼画面の人物ID、森さんの既存予約Extra／予約画像、旧Extraの客IDなし共通代替を確認。信頼回復の自然発生・回答後の接客一式は今回の対象にせず、新イベントは作っていない。
- [実着席1280×800](Results/FujiwaraIcon/actual-seated-fujiwara-1280.png)／[実着席1024×768](Results/FujiwaraIcon/actual-seated-fujiwara-1024.png)、[信頼1280](Results/FujiwaraIcon/trust-advanced-white-1280.png)／[信頼1024](Results/FujiwaraIcon/trust-advanced-white-1024.png)を実寸目視。27〜34pxの席アイコンはボブとプラムの色面が主な識別要素。大きいADVでは横長の目・閉じ口が読める。小表示で眉や視線の細線まで識別可能とはしない。名前も識別を担う。
- computer-useスキルのskyによる実マウスで席1詳細を開き「藤原さん／担当ソラ／体力95」と既存本文を確認。[実入力画像1920×1080](Results/FujiwaraIcon/manual-after-input-1920.png)／[入力メモ](Results/FujiwaraIcon/input-notes.txt)。当初1024×768を要求したが、Windows拡大とデスクトップ操作後の画像は1920×1080だった。指定2解像度の実入力成功とは扱わない。DPI座標のずれで初回クリックは空振りし、再観察して調整後に詳細を開けた。画面外への移動操作はツールに拒否されて変更なし。デスクトップ側の右下クリップにより実入力での閉じる／猫一覧への切替は未確認。

未確認：自然な人気獲得からの登場・心をつかむ自然発動、信頼回復の自然発生と回答後一式、指定2解像度での実入力、実台帳のGUI手入力、猫一覧の今回実入力。5席カード下部切れは修正せず、今回は2席のため確認を妨げなかった。5席表示自体の再検証は行っていない。長期評価・総合一式の反復なし。

## 容量と作業分離

開始・終了snapshot：[開始](Results/FujiwaraIcon/capacity-start.json)／[終了](Results/FujiwaraIcon/capacity-end.json)。

| 対象 | 開始bytes | 終了bytes | 増減bytes |
| --- | ---: | ---: | ---: |
| C:/Users/hi-wa/unity_workspase/CatCafeSim/Library | 5623246147 | 5775118994 | +151872847 |
| Docs/Prototypes | 529633171 | 531029991 | +1396820 |
| cat_cafe_sim/saves/content_images | 1205073710 | 1206469498 | +1395788 |
| Logs/StandaloneBuild/Latest | 3412903856 | 3471635296 | +58731440 |
| Logs | 15655382801 | 15735218056 | +79835255 |

Logs約79.8MB増のうち、Latest約58.7MB増。残りは14人人物専用パック一組と画像・保存fixture・ログ。旧パックを保持したため配布収録量も増える。Library約151.9MB増は既存プロジェクトのPlayerDataCacheと新しい人物パックのImporter成果物等の更新による。更新された大きいファイルはPlayerDataCache/Win64/Data/resources.assets.resS（約3.38GB）とArtifactsの人物texture（約6.35MBずつ）。別プロジェクト・検証専用Libraryは作らず、既存キャッシュを使った。キャッシュの自動更新差分であり手動で素材を整理しない。
Latestは156ファイル・3,471,635,296 bytesの成功ビルド一組、Pendingなし。StandaloneBuild直下はLatest/Runs/build.lock。追加Buildフォルダ・ビルドZIPなし。新しいPNG原本一枚、登録素材一枚と人物専用パック一組以外の試作は増やさない。旧パック・採用素材・ソース・セーブ・猫制作の先行差分は保持。
開始時はTool 00028a2、Python 7c2f4a1、Unity a24a094。共有Docs開始HEADはLogs/FujiwaraIcon/baseline-git.jsonへ記録。関連チャットはidle、Unity/Player実行なしを確認してから着手。共有台帳は既存全行を保持したまま一行追加し、コミットにはHEADへ今回行だけ追加した内容をstageする。猫制作の日程・設定・素材・presentation/Narrative等の差分を含めない。pushなし。

今回コミット：Python `e38e8b2`、Unity `2e92abb`、Docs `a54478a`。ToolのコミットIDは最終報告に記す。
