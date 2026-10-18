# 全体オプション

2026-10-18実装。タイトルの「オプション」、営業メニュー右上の「オプション」、主要イベント・Extra回想の会話操作列「設定」から共通画面を開く。

## 設定と操作

- 会話の文字速度は10〜80文字／秒、初期40文字／秒。全文即時表示は初期OFF。設定画面の文章で確認でき、「もう一度読む」で再表示する。
- 受け入れ依頼・常連紹介・派遣会話などAcquisitionConversationPanelを使う主要会話とExtra回想へ反映する。全文表示・次へ・バックログの既存操作を維持する。日常交流や営業履歴の既存全文表示は維持する。
- 全画面ONはボーダーレス全画面、OFFはウィンドウ。解像度・描画品質・モニター選択は今回は追加しない。初期値はウィンドウ。設定ファイルがなければ起動直後のビルド既定／起動引数の画面モードを維持し、設定を保存すると選択したモードを反映する。
- 「保存して戻る」で反映・保存し、「変更を取り消す」またはEscで変更を破棄する。「初期設定」は編集中の値を初期値にし、保存するまでは現在設定を変更しない。保存に失敗すると画面を閉じず理由を表示し、編集中の値と従来設定を保持する。
- BGM・効果音の再生処理と音声素材は現行配布版に存在しないため、音声未対応と表示する。効果のない音量スライダーや新規音声は追加しない。音声導入時に音量設定を追加する。

## 営業・会話との関係

営業中に開くと既存の自動進行を停止する。開いている間は営業コマンドを受け付けず、会話の文字送り・次へ・回答も停止する。閉じても自動再開せず、手動操作で再開する。処理送信中・おまかせ実行中／停止処理中は入口を無効にする。おまかせは既存の停止操作を行ってから開く。会話やメニューは閉じずに背面へ残し、戻った際の位置・選択肢・バックログを保持する。設定画面が別画面の更新で背面にならないよう最前面を維持する。

## 保存と構成

CafeOptions.csに設定データ・保存・適用、GameOptionsPanel.csに共通画面を分離した。通常はApplication.persistentDataPath/options-v1.jsonへ保存する。ゲームスロット・共通Extra記録とは別で、新規開始・セーブロード・シーン切替で設定を戻さない。営業状態・ルール・費用・加入・バランス・旧セーブ形式は変更しない。

version=1、charactersPerSecond、instant、fullscreenを保存する。形式と速度範囲を検証し、一時ファイルへ書込・flush・再読込後に既存の置換処理で保存する。.bakから復旧でき、修復保存では壊れた主ファイルを.invalidに残す。両方読めない場合は初期値と説明を表示し、読み込みだけでは元ファイルを上書きしない。明示的に保存した場合に修復する。複数プロセスの同時編集と端末間同期は対象外。

## 検証

配布Playerの専用入口--catcafe-options-check <検証先>を用意し、この場合だけ設定保存先を検証先/profile/options-v1.jsonにする。再起動は--options-restartを併用する。通常の設定・営業セーブを使わず検証する。

初期値、速度両端、保存・再読込、無効値、書込失敗による既存記録保持、バックアップ復旧、両方破損、バージョン不足、UI保存失敗、取消、初期設定、文章プレビュー、会話の低速表示／即時表示／次へ、設定中の会話停止、自動営業停止、営業状態の不変、全画面／ウィンドウ復帰、シーン切替、プロセス再起動を確認する。

結果は.NET会話・保存430 assertions、Unity Editorの設定保存16 checks・既存受け入れ989・派遣313 checksが成功。最終配布版は初回38・別プロセス再起動39 checks、既存受け入れ37・Extra回想18・CG一覧23 checksが成功し、Unityエラー0件だった。

実画面は1280×800でタイトル入口、即時表示切替、16文字／秒への変更とプレビュー、取消と復元を確認。1024×768で全項目と操作ボタンが収まり、営業メニューの入口・共通画面・Escでの復帰を操作した。検証先の営業データで新規開始して確認し、通常セーブ・通常設定を変更していない。

証跡は作業ルートLogs/StandaloneOptions。Editor/result.txt・acquisition-regression.json・dispatch-regression.json、core-result.txt、Player/result-first.txt・result-restart.txt、各回帰検証のresult.txt・player.log。画面はPlayer/options-1280.png・slow-preview-1280.png・title-1280.png・options-1024.png・menu-1024.png。初期ビルドの検証fixtureのコンストラクタ指定を修正し、残留していた前作業の自分のPlayerだけを終了してLatest置換を再実行した。実画面確認後にチェック欄・スライダーの配色と営業メニュー入口の配置を調整し、最終ビルドで再検証した。

## ビルド・容量

指定のcat_cafe_sim_tool/build_standalone.ps1 -ProjectPath C:/Users/hi-wa/unity_workspase/CatCafeSim -ExecuteMethod CatCafePreview.Editor.OptionsVerification.VerifyAndBuildを使用し、共通StandaloneBuildStorage経由で固定Latestへ置換した。最終ビルドログはLogs/StandaloneBuild/Runs/20261018-191249-459-ced9f4da6f1f4de3a1e55642ba190f3b.log。配布版はLogs/StandaloneBuild/Latest/CatCafeStandalone.exe。

| 対象 | 開始 | 終了 |
| --- | ---: | ---: |
| Logs全体 | 17,639,808,576 bytes | 17,644,386,068 bytes |
| 既存Unity Library | 10,984,583,247 bytes | 10,984,656,808 bytes |
| Latest | — | 8,247,655,370 bytes / 156 files |
| 今回の検証記録 | — | 2,248,614 bytes / 84 files |

Logsは約4.58MB、Libraryは約0.074MB増。主因は4回のビルド実行ログと専用保存・画面証跡で、大きな増加はない。計測後のJSON・ログ書込に小差がある。StandaloneBuild直下のフォルダはLatestとRunsだけで、Pendingなし。成功ビルド一組だけを保持し、ZIP・プロジェクト・Libraryの複製なし。自分の検証Playerは終了した。ソース・素材・通常セーブ・別作業の成果物は整理対象にしていない。

今回の実装・検証・記録だけをコミットする。既存の未コミットAllCats・CatImageSchedule・CatImageTodoとrules／presentationは対象外とし、pushしない。

## 残る課題

音声の導入と音量調整、解像度・モニター選択、任意の極小ウィンドウへの対応、エンディング画面は後続。現在の文章速度はUTF-16文字数を基準とし、現行の日本語本文を対象とする。絵文字の書記素単位送りは対象外。
