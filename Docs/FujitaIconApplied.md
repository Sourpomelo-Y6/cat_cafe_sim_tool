# 藤田さん（expanded-guest-3）のプロフィール・人物アイコン

2026-10-26。一人・透過画像一案。制作ID `person-expanded-guest-3` → ゲーム客ID `expanded-guest-3`。開始時の採用21人を保持して22人。藤田さんの既存制作行・画像がないことを確認して追加。既存客の描き直しなし。

## 今回採用した制作設定

54歳の女性で、小さなボードゲーム店の店主。核は「勝負好きで、猫の一手に感心してしまう人」。負けず嫌いで自分から勝負を提案するが、負けは相手の見事な手として褒める。猫が玩具でなく紐の影を追うと「ほう、その手があったか！」と感嘆する。一人称はあたし、猫を `{cat}名人` と呼ぶ。短く弾む断定口調で、作戦・一手・参ったをよく使う。無反応は自分の読み負けと受け止める。「さあ名人、次の一手は？」「ほう、そっち！ 一本取られた」「参った、今日は名人の勝ち。閉会！」。休憩も明るく勝負を畳み、猫を追い回さない。

元は印刷会社の営業。出張土産に集めた盤上遊戯をきっかけに独立。趣味は古い双六の収集と詰め将棋。ふくよかな頬、短い銀灰色の巻き髪、額を見せる横分け、眼鏡・帽子なし。珊瑚色のカーディガン、生成りのブラウスと紺の小さなスカーフ。斜め上向きの顔、丸みのある開眼、高く丸く上がる両眉、歯を見せない感嘆の開き口で「思いつかなかった一手への感心」を表す。

年齢・性別・職業・来歴・性格・外見・話し方は今回採用した創作で、既存ゲームの確定事実とは区別する。古い消極的な指針より今回のユーザー方針を優先。例文は人物資料のみで、ゲーム本文に追加しない。全プロフィールは `register_fujita.py` のNOTESと人物台帳へ保存。

[採用PNG](../../Docs/Prototypes/FujitaIcon-v1/fujita-icon.png)／[生成プロンプト全文](../../Docs/Prototypes/FujitaIcon-v1/prompt.txt)。組み込みimage_gen一回、1254×1254 RGBA、alpha 0〜255。画像加工なし。岡田さん・前田さんの採用画像を実際に見て画風・線・暖色調・構図を参照した。目・眉・口・顔向きを先に設計。指示の小さい開眼・右向きに対し採用像は丸みのある開眼と斜め上向きで、記録を原本に合わせた。微笑み、前田さんの半眼・寄せた眉・閉じ口、岡田さんの歯を見せる大笑いを繰り返さない。巻き髪の輪郭・珊瑚色の面・紺スカーフは小表示の補助手掛かり。比較案・表情差分・立ち絵なし。

## 登録・取り込み・維持した条件

`register_fujita.py` で既存SubjectRegistry / HumanPortraitStoreの保存・別インスタンス再読込・PNG登録・人物専用書き出しを実施。実台帳のGUI手入力とは区別する。既存21人のプロフィール・画像対応を保持。22人人物専用パック `03fb64219aa00953b392bb49774aa5c6` 一組。同内容の再出力は同一場所。猫画像・猫catalogの再書き出しなし。通常 `ContentPreviewSetup.Import` で取り込む。22行のID/hashと人物Importerを照合。実効max1024・非圧縮・alpha保持・mipmap/read-write無効・Standalone overrideなし。猫用BC7なし。

Python `cafe_customers.py` のID/名前、`core/cafe_expansion.py` のSIXTH_CUSTOMER_ID / extra_schedule、`config/cafe_expansion.json`、既存6席試験とUnity `CafeCustomers.CustomerSchedule` / `CafeExpansion` を照合。2→3席500、3→4席1000、4→5席1000、5→6席1500。6席以降は人気第2段階達成後で、保存上の購入日は達成翌日以降、購入後にも資金を残す必要がある。購入当日は不在、翌営業日以降に刻12で追加。3〜5席では生成せず、7席以降の追加客は別ID。休業日に来店なし。条件・数値・本文・セーブ形式は変更しない。

## 限定検証

Tool人物画像11試験成功（継承重複5件、backendとTkファイル選択差替えfixture）。Python6席拡張9試験成功（GUI自動試験1件含む）。最初のテスト実行はtemp制限とcp932読み込みで失敗したため、workspace内tempとPYTHONUTF8で再実行した。[Tool](Results/FujitaIcon/tool-tests.txt)／[6席](Results/FujitaIcon/expansion-tests.txt)。自然な人気獲得・長期評価と区別する。

指定build_standalone.ps1・既存Unityプロジェクト・共有StandaloneBuildStorageで固定Latestだけを更新。初回制限環境はライセンス接続exit199。通常環境で成功。途中のPlayer失敗は検証側の問題：休業後の自動翌日遷移を考慮しない日付比較と、通常進行中の帰還待ち未処理。検証側を修正しゲーム条件・数値・保存ガードを変更しない。失敗を成功根拠にしない。

最終成功ビルドは `Logs/StandaloneBuild/Runs/20261026-163749-297-1e156d984c0d41c798ea8aa6bea54a43.log`。CAT_CAFE_FUJITA_IMPORTED / CAT_CAFE_STANDALONE_BUILD_OK、exit0。

- [最終Player166項目](Results/FujitaIcon/player-result.txt)成功。22人が別画像として解決し、両解像度の席、客交代・未知ID代替・空席更新、未登録・メモリ上欠損画像の共通人物記号と復帰、予約Extra・客IDなし旧Extraの代替、描画前後の営業状態・過去本文不変を確認。席1への画像差し替え表示はview fixtureであり、通常着席の確認と区別する。
- 通常new_game(popularity)から、隔離メモリの人気だけを本番capへ置き、通常day_off / advance_goal / continue_goalで第2段階まで達成。資金は通常営業で獲得し、通常expand_seatsで3〜6席を購入。3〜5席では不在、6席購入日は不在、翌日以降の通常スケジュールで刻12を確認。購入履歴・来店・着席を直接投入していない。初期5匹では担当が先客で埋まり待機離脱したため、通常募集で6匹目を受け入れ、通常待合スペース購入・強化を行った。帰還・健康回復・出勤も既存操作で処理。ルール・費用・待ち時間の設定値は変えない。
- 9日目の刻12に来店し、刻21に通常自動割り当てで席1へ着席。刻22、資金1830の状態を保存。通常save_game/load_gameで別backendへ再開し、さらに別プロセスの再開[5項目](Results/FujitaIcon/manual-result.txt)成功。全席ID・状態・日・刻の一致を確認。[照合結果](Results/FujitaIcon/image-checks.json)。ユーザーのセーブは使用しない。
- [通常着席1280×800](Results/FujitaIcon/actual-seated-fujita-1280.png)／[1024×768](Results/FujitaIcon/actual-seated-fujita-1024.png)を実寸目視。席アイコン24 logical px、1024では19.2px相当。銀灰色の巻き髪・珊瑚色・紺の色面と名前で識別し、小表示で目や口の細部まで読めるとはしない。既存の5席以降の下段切れは6席でも残り、下段の本文・詳細が隠れる。今回は席1に通常着席したため本人の表示・詳細確認への影響はない。下段切れの修正は行っていない。
- 信頼回復は停止履歴のみfixtureで本番PresentCustomerTrustにより警告対象化し、藤田さんの画像を解決。[信頼1024](Results/FujitaIcon/trust-expanded-guest-3-1024.png)／[1280](Results/FujitaIcon/trust-expanded-guest-3-1280.png)。眉・開き口を大表示で確認。自然発生・回答後一式は未確認で、既存対応範囲を拡張しない。
- computer-useスキルのskyを使い、可視Playerで別プロセス再開後の席1詳細を実クリック。藤田さん／コハク／体力19/100と既存のやり取りを確認。[実入力記録](Results/FujitaIcon/manual-done.txt)／[実入力後PNG](Results/FujitaIcon/manual-after-input-1024.png)。実描画1024×768、外枠1026×800。1280は自動描画のみ。前段の非表示起動・非表示再開は実入力根拠に含めない。

途中の失敗には、先客がいる状態で自動割り当てを早くOFFにして進行停止、帰還後の出勤なし、混雑による着席前の待機離脱も含む。検証側を通常自動割り当て・通常募集・待合設備・出勤処理へ修正した。最終成功後は古いfailure.txtを `Logs/FujitaIcon/previous-player-failure.txt` に移し、成功記録と区別して保持。

未確認：標準人気の自然獲得からの登場、常連化・追加曜日の自然発生、信頼回復の自然発生と回答後一式、1280での実入力、実台帳GUI手入力、下段席の実詳細操作。長期評価・総合確認一式は繰り返さない。

## 保持・容量・コミット

開始Tool `501dcf7`、Python `92e2d56`、Unity `028ecec`、共有Docs `c5b4704`。開始時200ファイルの先行差分を記録。人物台帳以外の199ファイルは終了時にもhash一致、台帳は今回行以外の全行が開始時と同値。猫画像・日程・加入設定・本文・猫catalogを保持した。Pythonの人物台帳コミットはHEAD＋藤田さん1行のみで、先行猫プロフィール差分は作業ツリーに残した。

|対象|開始bytes|終了bytes|増減bytes|
|---|---:|---:|---:|
|Latest|4,000,209,960|4,092,501,856|+92,291,896|
|Logs|16,460,126,591|16,593,142,529|+133,015,938|
|Library|6,244,711,157|6,341,274,859|+96,563,702|
|content_images|1,216,134,162|1,217,804,046|+1,669,884|

Latestは156ファイル・4,092,501,856 bytesの成功物一組だけ。StandaloneBuild直下のディレクトリはLatest/Runs、Pendingなし。約92.3MB増の主因は旧パック保持のまま22人人物パックを収録したこと。Logs増約133.0MBにはLatest増分、人物専用パック一組、隔離セーブ・画像・複数回の実行ログを含む。Library増約96.6MBは既存プロジェクトの通常取り込み・ビルドキャッシュ。採用PNG原本・登録PNGは各1,667,293 bytes。旧パック・素材・セーブを削除せず、ZIP・別Build・Unity複製・追加Libraryなし。[開始](Results/FujitaIcon/capacity-start.json)／[終了](Results/FujitaIcon/capacity-final.json)。

コミット：Python `1039d0c`、Unity `a74dcf3`、共有Docs `938414e`。Toolも今回分だけコミットし、pushしない。
