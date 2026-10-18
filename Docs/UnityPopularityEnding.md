# 人気目標の達成エンディング

2026-10-19実装。既存の人気目標の最終段階達成を、やさしいADV形式の締めくくり会話へ接続する。対象は成功1本。中間段階・期限内未達・経営終了・好感度／有力者の結果画面は維持する。ルール・達成条件・報酬・バランスは変更しない。

## 内容と画面

達成時のCafeClearRecordを使い、達成日・目標と実績の人気・在籍猫数・資金・累計接客売上を表示する。現在値から再計算しない。会話本文はCafePopularityEnding、位置と猫IDはCafeEndingProgress、UIはPopularityEndingPanel、接続はLocalCafePopularityEndingへ分離した。

既存のAcquisitionConversationPanelで話者・地の文・通常猫画像・背景・全文表示・次へ・バックログ・保存／読込・オプションを使う。在店中の猫がいればその1匹を演出用に選び、猫がいなければ店内を見渡す文章とシルエットにする。固有の来歴や未設定の性格を付け足さず、新規画像・音声・LLMは使わない。

閉じると人気目標の結果画面から「エンディングを再開」できる。最後まで読むと、達成成果・保存・継続営業・タイトルへの画面を表示する。タイトルへ戻る前に必要なら保存する旨を表示し、既存の保存ブラウザを開く。継続営業は既存continue_goal処理を一度だけ呼ぶ。営業・自動進行を止め、手動で再開する。

## 進行・保存・互換

営業セーブ形式1に任意のpopularity_endingを追加する。conversation_id=ending.popularity.v1、branch_id=cleared、node、completed、viewed_branchesとcat_idを持ち、ゲームルールのスナップショットやリプレイダイジェストから分離する。会話途中・最後の文章・読了後の画面を保存／ロードできる。完了前のcontinue_goalはUIとバックエンドの両方で防ぐ。他目標の結果確認コマンドは妨げない。表示は既存required_actionの優先順に従い、優先イベントや別の会話が出ている間は上に重ねない。

旧セーブで人気最終結果が未確認なら先頭から演出を始める。すでに継続営業へ進んだ旧セーブは演出を強制せず、未閲覧として制限へ戻さない。過去の閲覧完了は推測しない。Unityの空オブジェクトとして出力される未設定項目は、既存会話メタデータと同様に未設定へ正規化する。位置・ID・分岐・完了記録・猫IDを読み込み時に検証する。

## Extra

既存extra-v1.jsonとCafeReplayStoreを共用する。最後まで読んだ時だけ安定IDと本文・猫表示・達成時の固定成果を記録する。Extraに「エンディング」切替を加え、会話回想12分岐と成功エンディング1本を分けて表示する。未解放は再生不可。件数は表示中の分類について数える。旧Extra記録をそのまま読み、営業セーブとバックアップに残る実際の閲覧完了記録を取り込める。新規ゲーム・古いスロットロードで解放を消さず、重複登録しない。再生は既存の読み取り専用ReplayConversationPanelを使う。

## 検証とビルド

専用入口--catcafe-ending-check <検証先>、再起動は--ending-restart。通常セーブを使用しない。専用ルールの複製で最終段階を1段・低い目標値に短縮し、既存の新規開始・休業・達成処理を通す。標準3段階の長期通しプレイは今回再実施しない。

各文章位置の保存復元、未読の解放禁止、読了の重複防止、成果の不変、完了前の継続拒否、完了後の継続、新規開始、旧未確認／確認済み、無効位置、中間段階・期限未達・他目標、優先イベント、他目標確認の操作可否、閉じる／再開、連打、バックログ、読了後の保存復元、Extra再生を検証する。

結果は.NET会話保存430 assertions、Unity Editorのエンディング22 checks・既存受け入れ989・派遣313 checksが成功。最終配布版は初回36・別プロセス再起動38 checks、既存受け入れ37・Extra18・CG一覧23 checksが成功し、Unityエラー0件だった。別イベントの優先と他目標の操作可否は表示投影を使った合成検証で、長期プレイ中の同時達成は今回再現していない。

1280×800と1024×768の実画面で一覧・本文・成果画面の収まりを確認した。Extraから再生／閉じる、タイトルから読了スロットをロード、成果画面から保存ブラウザを開いて空きスロットへ保存、継続営業、成果画面からタイトルへ戻る操作を行った。すべて専用検証データで、通常のセーブや設定を使用していない。

証跡は作業ルートLogs/StandaloneEnding。Editor/result.txt・acquisition-regression.json・dispatch-regression.json、core-result.txt、Player/result-first.txt・result-restart.txt、各回帰検証のresult.txtとplayer.log。画面はPlayer/list-1280.png・replay-1280.png・outro-1280.png・save-overlay-1280.png・continued-1280.png・list-1024.png・replay-1024.png・outro-1024.png。初期検証で未設定メタデータの空オブジェクトを修正した。再実行では過去の解放を保持するテストへ直し、失敗記録はfailure-reused-fixture.txtに保持した。

## ビルド・容量

指定のcat_cafe_sim_tool/build_standalone.ps1 -ProjectPath C:/Users/hi-wa/unity_workspase/CatCafeSim -ExecuteMethod CatCafePreview.Editor.EndingVerification.VerifyAndBuildを使用した。共通StandaloneBuildStorageで固定PendingからLatestへ移動して置換。最終ログはLogs/StandaloneBuild/Runs/20261019-065524-965-578a10f1c4214144821bd827e6095c3b.log。出力はLogs/StandaloneBuild/Latest/CatCafeStandalone.exe。

| 対象 | 開始 | 終了 |
| --- | ---: | ---: |
| Logs全体 | 17,645,561,513 bytes | 17,780,514,974 bytes |
| 既存Unity Library | 11,113,246,727 bytes | 11,240,562,746 bytes |
| Latest | 前回記録 8,247,655,370 bytes | 8,375,096,784 bytes / 156 files |
| 今回の検証記録 | — | 4,323,640 bytes / 143 files |

Logsは約134.95MB、Libraryは約127.32MB増。Latestは前回成功版から約127.44MB増えた。主因は別作業で既存Unity有効パックへ登録済みのチロ画像27枚が再ビルドで配布版へ含まれたこと。チロ側Status.mdの以前のLatest容量、catalog／bundleの27個の画像キー、最終ビルドログの各4.5MiBの使用画像27件、Library/PlayerDataCache/Win64/Data/resources.assets.resSの更新を照合した。画像だけでログ上約121.5MiB（約127.4MB）に相当する。対応はasset-growth.txtに記録した。残る増分は5回のビルド実行ログと専用保存・画面証跡。計測後のログ等で小差がある。

新規画像生成・新しいビルド保存先・ZIP・Unityプロジェクト／Library複製はない。StandaloneBuild直下のフォルダはLatestとRunsだけでPendingなし、成功ビルドは一組。自分の検証Playerは終了。素材・通常セーブ・別作業のキャッシュや成果物は削除していない。

今回の実装・検証・記録だけをコミットし、pushしない。既存の未コミットAllCats・CatImageSchedule・CatImageTodo・ChiroImages・register_chiro_images.py・ChiroDailyImageVerification・CafeNarrativeCatalog・rules／presentationは含めない。

## 後続

好感度・有力者の成功演出、未達・経営終了の短い結末、専用CG・音声・猫別分岐は後続。全体の長期バランスや通常ルールによる別乱数の通し評価も後続とする。
