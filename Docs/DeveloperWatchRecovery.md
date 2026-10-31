# 観戦プレイの待機とエラー後の保存

2026-10-31。64倍速で、修理の回答が正常に処理された直後に「選べる回答がない会話」と誤判定する不具合を修正。

通常クライアントは、コマンド返信を画面へ反映した後、最新状態・セーブ一覧を取得してボタンを更新する。従来の観戦操作は、その途中で無効な選択ボタンを検索していた。今はキュー・送信・状態取得・ボタン更新がすべて落ち着くまで、選択の判断と予約済みのUI操作を待つ。ゲームの条件や結果は変更しない。

エラー停止中も「保存して終了」を使える。保存本体とチェックポイントの記録が成功してから保存完了を表示する。保存に失敗した場合や、保存待ちが30秒を超えた場合は、理由を表示して画面を残す。状況を解消して「保存して終了」を押すと再試行できる。失敗した保存を完了扱いにして終了しない。

`-ExitOnComplete` の実行で操作エラーが発生した場合は、現在位置の保存を試み、成功後に終了コード3で終了する。保存に失敗した場合は、このオプションでも画面を残す。ロード自体が失敗した場合など、安全な保存対象をまだ復元できていないときは上書き保存を行わない。

|記録|内容|
|---|---|
|watch-failure.json|最初の自動操作停止の理由・日・刻・コマンド数・会話位置。再開後も残す|
|watch-status.json のerror|自動操作の停止理由。保存に成功しても消さない|
|save_status|最後の保存要求の状態：not_requested / waiting / saving / saved / failed|
|save_error|保存失敗の具体的な理由。保存の再試行時にリセットする|
|watch-checkpoint.json|正常保存のSHA・状態digest・会話位置。保存本体と合わせて再開に使う|

ウィンドウの×は保存要求を送らない。再開したい場合は「保存して終了」を使う。ビルド変更後の実行では、従来どおり古いビルドのcheckpointを自動で受け入れない。

## 回帰検証

専用Playerの `DevWatchRegression` は `--dev-watch-regression` を明示したときだけ動く。通常の観戦・通常ビルドでは検証用の停止やI/O失敗を注入しない。ゲーム進行は通常の観戦UIを使い、返信待ちの再現ではUIクライアントの待機状態だけを一時的に再現して元へ戻す。

```powershell
.\cat_cafe_sim_tool\tests\test_dev_watch.ps1 -Mode race
.\cat_cafe_sim_tool\tests\test_dev_watch.ps1 -Mode save-error -ShortDialogue
.\cat_cafe_sim_tool\tests\test_dev_watch.ps1 -Mode resume-error -ResumeDirectory "保存検証の実行フォルダー" -ShortDialogue
.\cat_cafe_sim_tool\tests\test_dev_watch.ps1 -Mode save-failure -ShortDialogue
.\cat_cafe_sim_tool\tests\test_dev_watch.ps1 -Mode exit-error -ShortDialogue
```

- race：文字表示を待つ64倍速で、実際の修理ボタンを押した直後と、キューが空になっても画面更新が未完了の瞬間を確認する。
- save-error / resume-error：意図した自動操作停止の後に保存し、別プロセスで営業・会話位置・停止状態を復元してから8日観戦を完走する。
- save-failure：前回保存を作り、一時ファイルへの書き込みを妨げて保存を失敗させる。終了しないこと・直前の保存が保持されること・再試行・待機タイムアウトを確認する。検証専用runの空の妨害ディレクトリだけを取り除く。
- exit-error：保存が成功してからエラー終了コード3で終了することを確認する。

検証先は `Logs/DeveloperWatch/Regression`。ビルドは共有の固定Latestを使用する。

## 最終Playerの結果

Unity 2021.3.45f2、`Logs/StandaloneBuild/Runs/20261031-080222-196-df0e5d7c9a5c4b9eb76c46b04a278067.log` の最終Playerで以下を確認した。

|検証|結果|
|---|---|
|race|5項目成功。文字表示を待つ64倍速で実際の修理回答を処理し、キュー待ち・画面更新待ちの両方で誤停止しない|
|save-error|9項目成功。結果会話の途中で意図した停止を起こし、停止理由を保ったまま通常保存・checkpoint作成に成功|
|resume-error|7項目成功。別プロセスで同じ営業・会話位置と停止状態を復元し、続行して8日観戦を完走|
|save-failure|15項目成功。自動終了指定中でも保存失敗で終了せず、前回保存を保持。再試行・30秒待機上限・未ロード時の上書き防止を確認|
|exit-error|9項目成功。保存とcheckpoint作成の成功を確認してから終了コード3で終了|

合計45項目。エラー注入は検証用runだけで実施。再開後の8日観戦の最終digestは `742057522db322227169fcb4032db9e2043a667726d144d11cf3f2647e16aa64` で、中断なしの既存結果と一致した。

通常の収集観戦も `Runs/20261031-080524-85d9f34fb04f444e80037c347247e6c4` で紹介カタログ1→2の2ルートを完走し、全担当対象を獲得・終了0。双方の最終digestは修正前の通し検証と一致。専用defineあり/なしのRuntime+Editor参照コンパイル成功。通常profileの11ファイルは追加・削除・SHA変更なし。機械照合はローカル `Logs/DeveloperWatch/recovery-verification.json`。

StandaloneBuildは開始5,827,871,596→終了5,831,411,852 bytes（約3.54 MB増、検証用コード・ビルドログを含む）。成功ビルドはLatestの1組のみ、Runsは実行ログ。別Playerフォルダー・ZIP・Unityプロジェクト・Library複製は作成していない。

追加確認：`Runs/20261031-083029-1392b6f3a8d743d586f615ffba68d80c` は既定順の収集9ルートすべてがcompleted。未完了ルートなし、エラーなし、最後の紹介カタログ3を16日目0刻で保存済み。ここでの停止は正常完走によるもの。
