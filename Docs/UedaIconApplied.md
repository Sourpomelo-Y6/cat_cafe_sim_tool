# 上田さん（expanded-guest-5）のプロフィール・人物アイコン

2026-10-27。一人・透過画像一案。制作ID `person-expanded-guest-5` → ゲーム客ID `expanded-guest-5`。開始時の採用23人と台帳行を再集計し、上田さんの既存制作・途中画像がないことを確認して24人へ追加。名前から対応を推定しない。他客の描き直し・猫画像/猫catalogの再書き出しなし。

## 今回採用した制作設定

役割：8席目増設の翌営業日以降、刻24に来店する上田さん。名前・ゲーム客ID・来店条件は既存定義。今回採用した制作設定：58歳の女性、手描き看板の文字職人。核は「余白に厳しいが、猫の寝姿には即座に合格を出す人」。文字の傾きや間隔には容赦なく指摘する一方、猫が好きな場所を占めると「そこが正解か」と潔く認める。猫のしっぽを勝手に動かしたり姿勢を直したりはしない。短い断定形の常体で、評点のように言い切る。猫の呼び名は{cat_call}。一人称はあたし。例「その余白、いいね」「親方、そこを取るか」「よし。一発で決まった」。父の看板店で文字を習い、今も商店街の小店の手書き看板を請け負う。趣味は古い商店の看板採集と銭湯めぐり。外見は幅広の頬と短めの四角い顔、耳にかけた黒灰色のボブと前髪の太い銀白の一房、眼鏡・帽子なし。赤紫の作業上着と生成りのハイネック。両目は横長の開眼、眉は低く太い直線寄りで左右に角度差、口はすぼめず閉じて口角をやや下げる。少し顎を上げ、見る側の右へ浅く向けた顔と値踏みする視線で自負と審美眼を表す。採用画像は指定した水平眉より角度があり、切りそろえ前髪でなく流した前髪。髪だけでなく両目・眉・口・顔向きで差別化。年齢・性別・職業・来歴・趣味・性格・服装・口調は今回採用した創作設定。条件・数値・過去本文は変更しない。例文は人物制作資料のみ。

猫の呼び名は `{cat}親方`。例文は人物制作資料だけに置き、既存のゲーム本文・過去本文へ追加しない。今回の積極的な創作方針を、古い年齢・職業を固定しない指針より優先した。

[採用PNG](../../Docs/Prototypes/UedaIcon-v1/ueda-icon.png)／[最終プロンプト全文](../../Docs/Prototypes/UedaIcon-v1/prompt.txt)。組み込みimage_genを一回使用、1254×1254 RGBA、alpha 0〜255。画像加工・比較案・差分・立ち絵なし。石田さん・藤田さん・岡田さんの採用原本を実際に目視してから、両目・眉・口・顔向きを先に設計。画風、暖色の肌、輪郭線、簡略化された胸上構図のみを参照した。両目の横長の開眼と閉じた口、顎を上げた審美眼で「大きな目＋微笑み」から離す。石田さんの片目/すぼめ口は繰り返さない。銀白の太い前髪、幅広の顔、赤紫と生成りの色面は小表示の補助手掛かり。眉の角度と流した前髪は指示と異なるが、採用像に合わせて記録した。

## 登録・通常取り込み・条件維持

`register_ueda.py` で既存SubjectRegistry / HumanPortraitStoreを使用。プロフィール保存→別インスタンス再読み込み、PNG登録→再読み込み、既存23人の資料と画像対応保持を照合。[登録記録](Results/UedaIcon/registration.json)。これは既存ToolのStore API経由で、実台帳GUIへの手入力とは区別する。

24人人物専用パック `3fe7b8e14fcb1665a18c19f8153d9168` を一組だけ書き出し。同内容再出力は同じ場所を利用。通常 `ContentPreviewSetup.Import` → HumanPortraitImportで取り込み。[Unity24行のID/hash/Importer照合](Results/UedaIcon/unity-data-checks.json)成功。DefaultTexturePlatformはmax1024・非圧縮、alpha保持、mipmap/read-write無効。Standalone欄は overridden=0 でDefaultを使う。人物へ猫用BC7を適用しない。

Python `cafe_customers.py`、`core/cafe_expansion.py` のEIGHTH_CUSTOMER_ID/extra_schedule/reason/validate、`config/cafe_expansion.json`、Unity `CafeCustomers.CustomerSchedule` / `CafeExpansion`を照合。2→3席500、3→4席1000、4→5席1000、5→6席1500、6→7席2000、7→8席2500。8席は人気第2段階達成後・7席購入済み、購入後にも資金が残る条件。保存上の6席以降の購入日は第2段階達成翌日以降。購入日は上田さん不在、翌営業日以降の刻24で生成、休業日は来店なし。2〜7席で上田さんは生成しない。古い拡張資料の「最終段階達成後」と現行コードは異なるため、現行コード/データを維持して記録する。条件・数値・セーブ形式・既存本文は変更しない。

## 限定検証と区分

- [Tool人物画像11試験](Results/UedaIcon/tool-tests.txt)成功（継承重複5件含む）。GUI1件は実Tkのボタン処理とファイル選択差替えfixtureで、実台帳の実マウス入力ではない。
- [Python8席拡張10試験](Results/UedaIcon/expansion-tests.txt)成功、GUI1件を含む。資金・目標・猫構成・日程等を固定する既存試験で、自然な人気獲得の評価とは区別。最初はsandbox一時フォルダでのreplace拒否/既定cp932読込に失敗し、workspace内TestTempとPYTHONUTF8=1で成功。製品コード変更なし。
- 指定build_standalone.ps1、既存Unity、共有StandaloneBuildStorageで固定Latest更新。最初の制限環境はライセンス接続exit199、通常環境で成功。最終成功ログ `Logs/StandaloneBuild/Runs/20261027-040610-199-005daae21ed445c683ab6406f0eb1cfb.log`、CAT_CAFE_UEDA_IMPORTED / CAT_CAFE_STANDALONE_BUILD_OK、exit0。
- [Player182項目](Results/UedaIcon/player-result.txt)成功。24人のdistinct画像解決、実席UIでの交代/未知ID代替/空席更新、未登録IDとメモリ上の欠損画像の共通人物記号・復帰、既存の予約Extra/客IDなし旧Extraの代替、描画/回想前後の状態・過去本文不変を確認。各人画像を上段席1に置く描画はview fixtureで、全24人の通常着席とはしない。
- 通常new_game(popularity)から、隔離メモリ上の人気だけを本番capへ置き、day_off / advance_goal / continue_goalで第2段階を達成。資金は通常営業で獲得し、通常expand_seatsで3〜8席を購入。2〜7席で不在、8席購入日は不在、翌営業日刻24を通常スケジュールで検証。増設履歴・来店・着席を直接投入していない。通常の飼育スペース購入/募集で8匹、待合購入/強化を実行。帰還・健康回復・出勤は既存操作。資金獲得中に信頼回復が発生して初回Player準備処理が停止したため、検証ヘルパーに既存resolve_customer_trust(recover)で回答する処理を追加して再ビルドした。これは本番イベント/ルールの変更ではない。
- 17日目に8席増設、18日目刻24に上田さん来店、刻25に通常自動割り当てで席2へ着席。刻26・資金5220を通常save_gameで保存、別backend load_gameと別プロセス再開[5項目](Results/UedaIcon/manual-result.txt)成功。全席ID・過去の席本文・日/刻/資金を照合。[寸法・状態確認](Results/UedaIcon/image-checks.json)。ユーザーのセーブは使用しない。
- [通常着席1280×800](Results/UedaIcon/actual-seated-ueda-1280.png)／[1024×768](Results/UedaIcon/actual-seated-ueda-1024.png)を目視。席の小人物アイコンは24 logical px、1024では19.2px相当。銀白前髪と赤紫の上着・名前で識別できるが、細かな表情まで小表示で読み取れるとはしない。既存の5席以降の下段切れは残り、本文・詳細ボタンが隠れる。今回は上段席2のため詳細検証への影響なし。下段切れは修正しない。
- 信頼回復は停止履歴のみfixture、本番PresentCustomerTrustで対象と画像を確認。[1280](Results/UedaIcon/trust-expanded-guest-5-1280.png)／[1024](Results/UedaIcon/trust-expanded-guest-5-1024.png)。大表示で両目・閉じた口・顎を上げた顔が読める。本人の自然発生や回答後一式へ範囲を広げない。
- computer-useスキルのskyで、別プロセス再開した可視Playerの席2詳細を実クリック。上田さん／ハル／体力95/100と既存のやり取りを確認。[実入力記録](Results/UedaIcon/manual-done.txt)／[操作後画像](Results/UedaIcon/manual-after-input-1024.png)。実描画1024×768、skyのウィンドウ取得1026×800（ウィンドウ枠含む）。1280は自動描画のみ。非表示の初回Player試験を実入力の根拠に含めない。

未確認：人気の自然獲得からの到達、常連化・追加曜日の自然発生、本人の信頼回復自然発生/回答後一式、1280での実入力、実台帳GUI手入力、下段席の実詳細操作。長期評価・総合確認一式は繰り返さない。

## 固定24人の登録棚卸し

[全24行のID・プロフィール欄・画像hash](Results/UedaIcon/fixed-24-inventory.json)。ID対応と画像は24/24、プロフィール資料欄の非空は23/24。予約客森さんは有効な制作対象と画像はあるが、既存台帳notesが空。採用記録には画像設定があるものの、台帳の詳細プロフィール不足として記録し補作しない。既存23人の状態は保持。通常客12、条件付き5、予約1、VIP1、席拡張5の棚卸しであり、全24人の自然登場/全イベント検証ではない。臨時客・イベントの役名は別枠で「ゲーム内の全人物完成」としない。

|区分|名前|ゲーム客ID|制作対象ID|プロフィール資料欄|画像|
|---|---|---|---|---|---|
|通常客|佐藤さん|`guest-1`|`guest-1`|登録済み|登録済み|
|通常客|鈴木さん|`guest-2`|`person-952c631405e649fdb50c390881804977`|登録済み|登録済み|
|通常客|高橋さん|`guest-3`|`person-guest-3`|登録済み|登録済み|
|通常客|田中さん|`guest-4`|`person-guest-4`|登録済み|登録済み|
|通常客|伊藤さん|`guest-5`|`person-guest-5`|登録済み|登録済み|
|通常客|渡辺さん|`guest-6`|`person-guest-6`|登録済み|登録済み|
|通常客|山本さん|`guest-7`|`person-guest-7`|登録済み|登録済み|
|通常客|中村さん|`guest-8`|`person-guest-8`|登録済み|登録済み|
|通常客|小林さん|`guest-9`|`person-guest-9`|登録済み|登録済み|
|通常客|加藤さん|`guest-10`|`person-guest-10`|登録済み|登録済み|
|通常客|吉田さん|`guest-11`|`person-guest-11`|登録済み|登録済み|
|通常客|山田さん|`guest-12`|`person-guest-12`|登録済み|登録済み|
|条件付き客|藤原さん|`advanced-white`|`person-advanced-white`|登録済み|登録済み|
|条件付き客|坂本さん|`advanced-longhair`|`person-advanced-longhair`|登録済み|登録済み|
|条件付き客|福田さん|`advanced-quiet`|`person-advanced-quiet`|登録済み|登録済み|
|条件付き客|西村さん|`advanced-play`|`person-advanced-play`|登録済み|登録済み|
|条件付き客|青木さん|`advanced-contact`|`person-advanced-contact`|登録済み|登録済み|
|予約客|森さん|`reservation-longhair`|`person-reservation-longhair`|空欄（既存不足）|登録済み|
|VIP|後藤さん|`vip-calico`|`person-vip-calico`|登録済み|登録済み|
|席拡張客|岡田さん|`expanded-guest`|`person-expanded-guest`|登録済み|登録済み|
|席拡張客|前田さん|`expanded-guest-2`|`person-expanded-guest-2`|登録済み|登録済み|
|席拡張客|藤田さん|`expanded-guest-3`|`person-expanded-guest-3`|登録済み|登録済み|
|席拡張客|石田さん|`expanded-guest-4`|`person-expanded-guest-4`|登録済み|登録済み|
|席拡張客|上田さん|`expanded-guest-5`|`person-expanded-guest-5`|登録済み|登録済み|

## 先行差分の保持・容量・コミット

開始Tool `20edb07`、Python `250ebcd`、Unity `d27817f`、共有Docs `683df79`。先行差分200ファイルを記録し、人物台帳以外の199ファイルはhash一致、人物台帳は今回追加行以外が全行同値。[保持照合](Results/UedaIcon/prior-differences-preserved.json)。リン・ユキ等の猫画像/台帳/日程/加入設定、猫catalogと過去本文を保持。Python人物台帳コミットにはHEAD＋上田さん行だけを入れ、先行の猫プロフィール差分は作業ツリーへ残す。

|対象|開始bytes|終了bytes|増減bytes|
|---|---:|---:|---:|
|Latest|4,188,985,032|4,289,663,080|+100,678,048|
|Logs|16,725,945,370|16,865,851,133|+139,905,763|
|Library|6,587,990,045|6,546,974,655|-41,015,390|
|content_images|1,219,228,896|1,220,723,801|+1,494,905|

Latestは成功ビルド一組。StandaloneBuild直下はLatest/Runsのみ、Pendingなし。Latestの増加は旧人物パックを保持したまま新24人人物パックを収録したため。Logs増加はLatest増分、人物パック一組、隔離セーブ・検証画像・ログ。Libraryは既存Unityの通常取り込み・ビルドによるキャッシュ更新で減少。content_images増加は採用PNGと台帳。素材・旧パック・セーブの削除、ZIP、別Build、Unity複製、追加Libraryなし。[開始容量](Results/UedaIcon/capacity-start.json)／[終了容量](Results/UedaIcon/capacity-final.json)。

コミット：Python `af0d2e9`、Unity `8715e0d`、共有Docs `ea6eb1f`。Toolは本資料・採用資料リンク・登録スクリプト・検証拡張・結果を含む今回コミット（IDは最終報告参照）。pushなし。
