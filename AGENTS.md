# 検証ビルドの容量管理

- 検証ビルドの保存先は `C:/Users/hi-wa/source/codex_repos/CatCafeSim/Logs/StandaloneBuild/Latest/CatCafeStandalone.exe` に固定する。機能名・日付・検証回数ごとの Build フォルダやビルドZIPを作らない。
- ビルドは `cat_cafe_sim_tool/build_standalone.ps1 -ProjectPath C:/Users/hi-wa/unity_workspase/CatCafeSim` を使う。検証入口を変える場合は `-ExecuteMethod` を指定し、出力先は変えない。
- 既存の検証入口も共有の `StandaloneBuildStorage.Build` を利用する。独自に `BuildPipeline.BuildPlayer` を追加して保存先を増やさない。
- ビルド中だけ固定の `Logs/StandaloneBuild/Pending` を使用し、成功後は移動して Latest を置き換える。複製しない。失敗時は Pending を削除し、直前の成功ビルドを保持する。
- 作業終了時に成功ビルドが Latest の1組だけであることを確認する。実行中の別作業や明示的な保存依頼がある成果物は勝手に削除しない。
- 実行ごとに分けるのはログ・検証記録だけ。ビルド実行ログは `Logs/StandaloneBuild/Runs` に置く。ソース・素材・セーブデータを容量整理の対象にしない。
- Unityプロジェクトのコピーを検証ごとに増やさない。既存プロジェクトを再利用し、検証用のLibraryキャッシュを蓄積させない。
- 作業開始時・終了時に生成物の容量を確認し、大きな増加があれば原因と残した容量を報告する。
