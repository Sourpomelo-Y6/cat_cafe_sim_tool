# 石田さん（expanded-guest-4）のプロフィール・人物アイコン

2026-10-26制作・検証、2026-10-27資料整理。一人・透過画像一案。制作ID `person-expanded-guest-4` → ゲーム客ID `expanded-guest-4`。開始時22人を再集計し、今回行の既存制作・途中画像がないことを確認して23人へ追加。名前からの推定対応は使わない。既存客は描き直さない。

## 今回採用した制作設定

役割：7席目の増設翌日から刻18に来店する石田さん。既存確定設定は名前・ゲーム客ID・増設来店条件。今回採用した制作設定：46歳の男性、出張ピアノ調律師。核は「音の違いにうるさいが、猫の不協和音には弱い人」。仕事では僅かなうなりも聞き分ける几帳面さ。猫の掠れた声や気まぐれな足音には調律不要、と真顔で降参する。短く拍を取るように区切り、乾いたユーモアを交える。一人称は僕、猫は{cat_call}と呼ぶ。例「一拍、二拍。先生、そこで休符？」「その声は直さない。味がある」「本日の演奏、ここまで」。猫を鳴かせようと追い立てず、沈黙も休符として受け取る。音大の舞台裏で調律を手伝ったのをきっかけに職人へ。趣味は口笛で古い映画音楽を奏でることと真空管ラジオの修理。来店は仕事の合間の耳休めだが、つい音を採集してしまう。外見は長めの角張った顔と通った鼻、茶の横分け髪にこめかみの太い白髪一房、眼鏡・ひげ・帽子なし。生成りのバンドカラーシャツと深い藍色のベスト、銅色の音叉ピン。正面寄りに少し傾けた顔、片目をほぼ閉じて片目を細く開け、低めの直線的な眉を少し寄せ、小さくすぼめた閉じ口で口笛の音を聴き分ける集中を表す。採用画像の眉は完全な水平でなく角度と左右差がある。微笑み・歯・感嘆の開き口にしない。職業・年代・性別・趣味・来歴・性格・服装・口調は今回採用した創作設定。条件・数値・既存本文は変更しない。例文は人物資料のみ。

猫の呼び名は `{cat}先生`。例文は人物制作資料だけでゲーム本文へ追加しない。古い創作抑制指針より今回のユーザー方針を優先した。年齢・職業・来歴・趣味・話し方・外見は今回採用した創作で、既存ゲームの確定設定と区別する。

[採用PNG](../../Docs/Prototypes/IshidaIcon-v1/ishida-icon.png)／[生成プロンプト全文](../../Docs/Prototypes/IshidaIcon-v1/prompt.txt)。組み込みimage_gen一回、1254×1254 RGBA、alpha 0〜255。画像加工・比較案・立ち絵・表情差分なし。藤田さん・前田さん・岡田さんの採用原本を実際に目視して線・暖色調・簡略度・構図だけ参照。目・眉・口・顔向きを先に設計した。正面寄りに傾けた顔、片目をほぼ閉じて他方を開けた非対称な目と小さなすぼめ口で「耳を澄ます職人」を表現。指示の水平眉に対して採用像は傾きと左右差があり、資料を原本に合わせた。藤田さんの高い眉・感嘆の開き口、前田さんの両半眼・閉じた片笑い、岡田さんの歯を見せる笑顔を繰り返さない。白髪の一房、長い顔、藍と生成りの色面が小表示の補助手掛かり。

## 登録・取り込み・保持した条件

`register_ishida.py` で既存SubjectRegistry / HumanPortraitStoreを使用。プロフィール保存→別インスタンス再読込、透過PNG登録→再読込、既存22人のプロフィール・画像対応保持を確認。[登録記録](Results/IshidaIcon/registration.json)。実台帳GUIへの手入力とは区別する。23人人物専用パック `7dbaa0652ce89a2551608be921c03712` 一組。同内容の再出力は同一場所。猫画像・猫catalogの再書き出しなし。

通常 `ContentPreviewSetup.Import` → HumanPortraitImportでUnityへ取り込み。[23行のID/hash/Importer照合](Results/IshidaIcon/unity-data-checks.json)成功。実効max1024、非圧縮、alpha保持、mipmap/read-write無効。Standalone設定欄は自動作成されるが overridden=0 でDefaultTexturePlatformの設定を使う。猫用BC7を人物へ適用しない。

Pythonの `cafe_customers.py`、`core/cafe_expansion.py` のSEVENTH_CUSTOMER_ID/extra_schedule、`config/cafe_expansion.json`、7席拡張試験、Unity CafeCustomers.CustomerSchedule / CafeExpansionと照合した。2→3席500、3→4席1000、4→5席1000、5→6席1500、6→7席2000。7席は人気第2段階達成後、保存上の購入日は達成翌日以降、購入後にも資金が残る必要がある。購入日は不在、翌営業日以降の刻18で生成。3〜6席では生成せず、8席の追加客は別ID。休業日は来店なし。これらの条件・数値・過去本文・セーブ形式を変更しない。

## 限定検証と区分

- [Tool人物画像11試験](Results/IshidaIcon/tool-tests.txt)成功。継承重複5件を含み、GUIのファイル選択差替えfixtureによるボタン操作を含む。実台帳への実マウス入力とは区別する。
- [Python7席拡張10試験](Results/IshidaIcon/expansion-tests.txt)成功。GUI1件含む。資金・日程・猫構成を試験用に固定する既存試験で、自然な人気獲得の評価とは区別する。最初はGUI opt-inがないためskipが出たが、CAT_CAFE_TEST_GUI=1で再実行し最終記録は全件成功。
- 指定build_standalone.ps1、既存Unity、共有StandaloneBuildStorageで固定Latestを更新。最初の制限環境はライセンス接続exit199、通常実行環境で成功。成功ログ `Logs/StandaloneBuild/Runs/20261026-165800-581-af07f0934e9f47bf99c7c865427ab0ba.log`、CAT_CAFE_ISHIDA_IMPORTED / CAT_CAFE_STANDALONE_BUILD_OK、exit0。Pendingは残らない。
- [Player174項目](Results/IshidaIcon/player-result.txt)成功。23人の画像解決、客交代・空席更新、未知ID・未登録ID・メモリ上の欠損画像の共通人物記号と復帰、予約Extra/客IDなし旧Extra代替、描画・回想前後のゲーム状態と過去本文不変を確認。席1へ人物画像を差し替える描画はview fixtureで、通常着席とは分ける。
- 通常new_game(popularity)から隔離メモリ上の人気だけを本番capへ置き、day_off / advance_goal / continue_goalで第2段階達成。資金は通常営業で獲得し、通常expand_seatsで3〜7席を購入。3〜6席では不在、7席購入日は不在、翌日の刻18を通常スケジュールで確認。購入履歴・来店・着席を直接投入していない。通常の飼育スペース購入・募集で7匹、待合購入・強化を使い、帰還・健康回復・出勤は既存操作で処理。設定値は変更しない。
- 14日目に7席まで増設、15日目刻18に通常来店、刻21に通常自動割り当てで席4へ着席。刻22・資金6360を通常save_gameで保存、別backendのload_gameと別プロセス再開[5項目](Results/IshidaIcon/manual-result.txt)成功。全席ID・日・刻・資金が一致。[状態・画像寸法照合](Results/IshidaIcon/image-checks.json)。ユーザーのセーブは使用しない。
- [通常着席1280×800](Results/IshidaIcon/actual-seated-ishida-1280.png)／[1024×768](Results/IshidaIcon/actual-seated-ishida-1024.png)を目視。席アイコン24 logical px、1024では19.2px相当。白髪一房・藍と生成り・名前で識別し、細かな表情まで小アイコンで読めるとはしない。既存の5席以降の下段切れは7席でも残り、本文・詳細が隠れる。今回は本人が上段席4に着席し詳細確認への影響なし。下段切れは修正しない。
- 信頼回復は停止履歴だけfixtureにし、本番PresentCustomerTrustで警告対象と画像解決を確認。[1280](Results/IshidaIcon/trust-expanded-guest-4-1280.png)／[1024](Results/IshidaIcon/trust-expanded-guest-4-1024.png)。片目とすぼめ口が大表示で読める。自然発生・回答後一式は未確認で既存対応範囲を拡張しない。
- computer-useスキルのskyで可視Playerの別プロセス再開後に席4詳細を実クリック。石田さん／ムギ／体力29/100と既存のやり取りを確認。[実入力記録](Results/IshidaIcon/manual-done.txt)／[操作後画像](Results/IshidaIcon/manual-after-input-1024.png)。実描画1024×768、skyのウィンドウ取得は514×414 logical px。1280は自動描画のみ。前段の非表示再開は実入力根拠に含めずLogs/IshidaIcon/hidden-restart-*へ分けた。

未確認：人気の自然獲得からの登場、常連化・追加曜日の自然発生、信頼回復の自然発生・回答後一式、1280での実入力、実台帳GUI手入力、下段席の実詳細操作。長期評価・総合確認一式は繰り返さない。

## 先行作業の保持・容量

開始Tool `13cb97c`、Python `1039d0c`、Unity `a74dcf3`、共有Docs `938414e`。開始時200ファイルの先行差分を記録し、人物台帳以外の199ファイルはhash一致、人物台帳は今回行以外の全行同値。[保持照合](Results/IshidaIcon/prior-differences-preserved.json)。猫画像・猫台帳・制作日程・加入設定・本文・猫catalogを保持。Pythonの人物台帳コミットにはHEAD＋石田さん行だけを入れ、先行の猫プロフィール差分は作業ツリーに残す。

|対象|開始bytes|終了bytes|増減bytes|
|---|---:|---:|---:|
|Latest|4,092,501,856|4,188,985,032|+96,483,176|
|Logs|16,593,203,558|16,725,884,988|+132,681,430|
|Library|6,341,274,859|6,587,990,045|+246,715,186|
|content_images|1,217,804,046|1,219,228,896|+1,424,850|

Latestは成功ビルド一組。StandaloneBuild直下はLatest/Runsのみ、Pendingなし。Latest増約96.5MBは旧人物パックを保持し新23人人物パックを収録したため。Logs増約132.7MBはLatest増分、人物パック一組、隔離セーブ・画像・ログ等を含む。Library増約246.7MBは既存Unityの通常取り込み・ビルドキャッシュ。content_images増約1.42MBは採用PNGと台帳。素材・旧パック・セーブの削除、ZIP、別Build、Unity複製・追加Libraryなし。[開始容量](Results/IshidaIcon/capacity-start.json)／[終了容量](Results/IshidaIcon/capacity-final.json)。

コミット：Python `250ebcd`、Unity `d27817f`、共有Docs `683df79`。Toolは本資料・採用資料リンク・登録スクリプト・検証拡張・結果を今回分だけコミット。pushなし。
