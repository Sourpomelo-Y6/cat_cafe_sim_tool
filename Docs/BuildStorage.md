# 検証ビルドの保存ルール

通常のビルドはPowerShellから次のコマンドで実行する。

```powershell
./build_standalone.ps1 -ProjectPath C:/Users/hi-wa/unity_workspase/CatCafeSim
```

Unityを閉じてから実行する。EditorはプロジェクトのバージョンとUnity Hubの登録先から検出する。必要な場合は `-UnityPath` を指定できる。既存の検証とビルドを合わせて実行する場合は `-ExecuteMethod CatCafePreview.Editor.NarrativeVerification.VerifyAndBuild` などを指定し、必要な検証用環境変数は従来どおり設定する。

- 成功ビルド：ソース作業フォルダの `Logs/StandaloneBuild/Latest/CatCafeStandalone.exe`。
- 実行ログ：同じ作業フォルダの `Logs/StandaloneBuild/Runs/<日時>-<ID>.log`。
- ビルド中のみ固定の Pending を使用し、成功時にフォルダを移動してLatestを置き換える。コピーはしない。失敗時はPendingを削除して前回の成功ビルドを保持する。更新中に中断した場合は次回ビルドでPreviousから復旧する。
- 排他ロックにより同じ出力先への並行ビルドを拒否する。最新版Playerを終了してからビルドする。
- `CATCAFE_WORKSPACE_ROOT` はソース作業フォルダを指定する。ラッパーが設定・復元する。旧 `CATCAFE_STANDALONE_BUILD` で `Logs/<機能>/Build...` を指定しても共有処理が固定先へ振り替える。Logs配下でない旧指定は、作業フォルダが明示されていない限りエラーになる。
- 両環境変数がない既存の検証専用呼び出しは、従来どおりビルドを行わない。
- テンプレートと実プロジェクトの両ビルド入口は `StandaloneBuildStorage.Build` を使用する。新しい入口も必ずこの共有処理を使用する。
- 検証ごとにビルドやUnityプロジェクトの複製を残さない。記録・セーブ・スクリーンショットはビルドとは別に保存できる。

保存処理のテスト（Unity不要）：`./tests/test_build_storage.ps1`。本番の保存処理に対してビルドAPIだけを置き換え、上書き、失敗時の保持と片付け、旧指定の振替、中断復旧、排他制御を確認する。
