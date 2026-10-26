# 後藤さん（vip-calico）のプロフィール・人物アイコン

2026-10-26。一人・透過画像一案。制作対象 `person-vip-calico` → ゲーム客 `vip-calico`。開始時の18人を保持し19人へ。既存客の描き直し・セリフ本文追加・ルール変更なし。

## 採用した個性

核は「猫にだけは鑑定眼が甘くなる審美家」。50代後半の男性で、長年小さな画廊を営む裕福な目利き。気に入った作品には値札より先に愛称を付ける。自分の審美眼には絶対の自信があり、芝居がかった言い回しをする。三毛猫の不揃いな模様を唯一無二と愛し、そっぽを向かれても「その気位、実にいい」と嬉しそうに評する。少し気位が高いが、猫に対しては評価が甘い。この年代・性別・職業・来歴・性格は今回新しく採用した創作設定であり、既存ゲームから発見した設定ではない。

口調は歯切れのよい、気取った常体。一人称は「私」、猫の呼び方は `{cat}君`。例：「ほう、見事だ」「その気位、実にいい」「本日の主役は君だな」。休憩や拒否には「ふむ、鑑賞の時間としよう」と自分の方針を切り替える。例文は人物資料だけに登録し、ゲーム内の既存会話本文へ追加していない。

今回の明示指示に従い、VIPや三毛猫への好みを性格・装いの手掛かりとして積極的に使用した。以前の「役割から富裕・高慢・年代・職業を決めない」などの消極的な指針はこの人物制作へ適用しない。他客の設定は変更しない。

外見は銀灰色の髪を大きな波で後ろへ流し、濃灰色のこめかみ・広い額・鼻筋の通った面長の顔と頬の年輪。葡萄色のジャケット、生成りシャツ、黄土色のクラバット。片眉を上げ、細い目をこちらへ向け、少し顎を上げた斜め顔と片側の口角を上げた閉じ口の笑み。青木さんの正面の開眼・開いた聞き口、西村さんの平静な横目と異なる、得意げな日常の顔とした。

[採用PNG](../../Docs/Prototypes/GotoIcon-v1/goto-icon.png)。1254×1254 RGBA、alpha 0〜255。青木さん・西村さんの最新採用画像を目視し、画風と構図のみ参照。組み込みimage_genの一回の生成原本を採用し、比較案・立ち絵・表情差分なし。[最終プロンプト全文](../../Docs/Prototypes/GotoIcon-v1/prompt.txt)。

## ID・登録・既存ルール

`register_goto.py` は既存のSubjectRegistry / HumanPortraitStoreを使用し、プロフィール保存・別インスタンス再読込・画像登録・人物専用書き出しを行う。人物画像登録機能を再実装しない。人物専用パック `f68910394f6d4347dd62471992fb2487` 一組を出力し、同内容の再出力は同じ場所となる。猫画像・猫catalogは再書き出ししない。

既存確定設定は後藤さんの名前・VIP接客・三毛猫への好み。Python `cafe_vip_customer.py` / `cafe_vip_customer.json`、Unity `CafeCustomers` / `CafeSpecialists`を照合。新規段階式人気モードで最終第3段階達成の翌日以降、営業開始刻0に一名登場。三毛猫を担当にし、心をつかむ・心を開くを各1回以上、同時発動を1回以上で追加料金400・追加人気10。これらの条件と数値は変更しない。

## 限定検証

Tool人物画像11試験成功（固有backend5＋Tkボタン1＋継承重複5）。隔離台帳・ファイル選択差替えの自動試験。実台帳はストアAPI登録であり、GUI手入力とは区別する。既存VIP3試験成功：最終段階解放翌日・名簿・保存再開／リプレイ、複合成功の料金・人気、別猫失敗・旧セーブと不正設定。テスト固有の短期fixtureであり自然な人気獲得や長期評価ではない。初回のテスト起動は既定一時ディレクトリのsandbox権限とcp932読込で失敗したため、既存workspace内の一時ディレクトリとUTF-8モードで実施し成功。

通常 `ContentPreviewSetup.Import` でUnityへ取り込み。[19画像のID・hash・Importer照合](Results/GotoIcon/unity-data-checks.json)。max1024・非圧縮・alpha保持・mipmap/read-write無効・Standalone overrideなし。猫用BC7は適用しない。[登録結果](Results/GotoIcon/registration.json)／[既存18人保持](Results/GotoIcon/preservation.json)。

指定 `build_standalone.ps1 -ProjectPath C:/Users/hi-wa/unity_workspase/CatCafeSim -ExecuteMethod CatCafePreview.Editor.HumanIconsApplyVerification.VerifyGotoAndBuild` と共有StandaloneBuildStorageで固定Latestを更新。初回sandbox起動はライセンス接続でexit199。通常環境で成功。Player検証の途中2回は、休業内の自動翌日移行で発生する予約への回答順を検証コードが飛ばして停止した。検証側に通常の予約辞退を入れて解決し、ゲームルールは変更しない。最終成功ビルドログ `Logs/StandaloneBuild/Runs/20261026-114652-704-85c0eda903f1410392dcd215114bad3f.log`、exit0、CAT_CAFE_GOTO_IMPORTED・CAT_CAFE_STANDALONE_BUILD_OK。途中の失敗を成功根拠にしない。

- [Player153項目成功](Results/GotoIcon/player-result.txt)。19画像の別実体解決、両解像度での席表示、客交代・未知ID代替・空席更新、未登録・メモリ上欠損画像の共通人物記号への代替と復帰、既存予約Extra、IDなし旧Extraの代替、描画前後の営業状態・本文不変を確認。
- 来店は通常 `new_game(popularity)` から開始。隔離メモリの人気だけを本番capへ置き、通常 `day_off` と目標回答で3段階を進める。初期・第1段階翌日・第2段階翌日は不在、最終段階達成当日は不在、翌4日目刻0に通常来店。通常 `assign_service` で席1へ着席した。人気の自然獲得ではないが、強制来店・強制着席のfixtureとは区別する。
- 接客境界は本番SpecialistResultへ直接入力。三毛猫＋各特別交流1回＋同時発動1回で成功、connect不足・open不足・同時不足・別特徴の猫で失敗。自然な操作の複合成功や報酬獲得の一式をPlayerで実施したものではない。Pythonの既存短期試験では成功時の料金・人気も確認。
- 信頼回復は客IDによる除外なし。隔離した停止履歴fixtureから本番PresentCustomerTrustで警告対象化し、客IDの画像解決を確認。[信頼画面1024](Results/GotoIcon/trust-vip-calico-1024.png)。自然発生や回答後一式は対象外。
- 通常save_game/load_gameで別backend・別プロセスに再開。[別プロセス5項目](Results/GotoIcon/manual-result.txt)／[全席・日・刻・資金一致](Results/GotoIcon/image-checks.json)。4日目刻1・資金820、席1の後藤さん／コハクを保持。隔離保存を使用し、ユーザーのセーブに触れない。
- [1280×800](Results/GotoIcon/actual-seated-goto-1280.png)／[1024×768](Results/GotoIcon/actual-seated-goto-1024.png)の実寸画像を目視。現行席アイコンは24 logical px、1024では19.2px相当。銀髪の大きな輪郭と葡萄色の服、名前が小表示での主な手掛かり。細かな眉・口角まで小表示で読めるとはしない。信頼画面では上げた片眉・細い目・非対称な笑みを確認。
- computer-useスキルのskyで可視Playerを前面化し、別プロセス再開後の席1詳細を実クリック。後藤さん／担当コハク／体力100/100／文章の記録はまだありません、を確認。[実入力後PNG](Results/GotoIcon/manual-after-input-1024.png)は実描画1024×768、外枠込み1026×800。[実入力記録](Results/GotoIcon/manual-done.txt)。1280は自動描画のみ。先行した非表示再開は別プロセス読込だけを確認した記録として分離し、実入力の根拠には使わない。

未確認：標準人気の自然獲得からの登場、Playerでの接客条件の自然達成、常連化・追加曜日の自然発生、信頼回復の自然発生と回答後一式、1280での実入力、実台帳のGUI手入力。既存5席下段カード切れは修正せず、今回の2席確認には影響なし。長期評価・総合確認一式は繰り返さない。

## 保持・容量・コミット

開始Tool `c26e5ef`（青木さん本体`7c8fabd`）、Python `0817141`、Unity `bd86da6`、共有Docs `fd3d8b3`。開始時にUnity/Player不在を確認。[先行200ファイル保持](Results/GotoIcon/prior-differences-preserved.json)：人物台帳だけ今回行追加で変わり、既存全行は同値。他199ファイルはhash一致。猫制作日程・素材・加入設定・既存本文・猫catalog・ルールを保持。人物台帳コミットはHEAD＋後藤さん一行だけとし、以前からの猫プロフィール差分は作業ツリーへ保持した。テキストの改行整形以外はビルド後に内容変更なし。

原本PNG一枚1,462,910 bytes・登録素材一枚同容量・19人人物専用パック一組のみ追加。旧素材・旧パック・セーブは削除しない。Latestは156ファイルの成功物一組のみ、StandaloneBuild直下はLatest/Runs、Pendingなし。ビルドZIP・別出力・Unityプロジェクト複製・追加Libraryなし。

| 対象 | 開始bytes | 終了bytes | 増減bytes |
| --- | ---: | ---: | ---: |
| Latest | 3,748,508,192 | 3,828,214,968 | +79,706,776 |
| Logs | 16,112,171,729 | 16,221,742,686 | +109,570,957 |
| Library | 6,094,421,882 | 6,064,160,808 | -30,261,074 |

[開始容量](Results/GotoIcon/capacity-start.json)／[終了容量](Results/GotoIcon/capacity-final.json)。Latest約79.7MB増の主因は旧パックを保持したまま19人人物パックを追加収録したこと。Logs約109.6MB増にはLatest増分と書出し人物パック・隔離セーブ・画像・実行ログを含む。Libraryは既存プロジェクトの通常キャッシュ更新で約30.3MB減、手動整理していない。

コミット：Python `c3039bd`、Unity `850d044`、共有Docs `dfa90c8`。Toolは本書・登録スクリプト・検証入口・結果記録を今回分としてコミット。pushなし。
