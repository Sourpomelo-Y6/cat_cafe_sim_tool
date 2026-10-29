# UI修正の固定Latest反映とモモ27画像の取り込み追補

2026-10-29。[猫詳細修正](CatDetailsOverlapFix.md)・[編集プレビュー](BusinessEditPreview.md)の後続。旧ユキ・アキ54metaの解決済み作業とは別件。

## 現物照合と恒久対応

開始HEADはUnity `0c80715`、Tool `c5520f6`、Docs `d51b968`。開始差分は展開済み391ファイルをハッシュ記録。失敗ログ `Logs/StandaloneBuild/Runs/20261029-092811-079-35b123e633544feea299b0ee7f7be9e8.log` の27パスは、有効 `cbe0df94e7a5401a8f466406c105b493` のモモ `catalog-5` の採用PNG27枚とSHA-256で一致。52場面への参照と制作manifestを照合した。

`register_momo_images.py` はPNGを有効パックへ直接配置し、未追跡の `MomoDailyImageVerification.Run` はRefreshだけを実行していた。正式 `ContentPreviewSetup.Import` が持つ `CatTexturePolicy.ApplyPack` を通らず、Unity既定のStandalone override無効・2048・自動形式・quality50になったのが原因。

既存方針を呼ぶ明示入口 `CatTexturePolicy.CompleteActivePackRegistration` を追加。Refresh後にcatalogの有効パックだけをApplyPackし、全対象をValidateする。メニューは Cat Cafe → Images → Complete active pack registration。直接追加後のモモ検証入口もこの呼出しへ変更した。正式Import・読み取り専用ビルドガードは保持し、ビルド処理から設定を変更しない。

対象27metaの変更はStandaloneのmaxTextureSize 2048→1024、textureFormat -1→25（BC7）、compressionQuality 50→100、overridden 0→1だけ。GUID、元PNG、他プラットフォーム・他Importerフィールド、画像対応、本文は不変。既存全素材metaの前後比較で対象外変更0。実Importer監査3,790枚はすべて適合（1024、BC7、mip1、readable false）。証跡は `Logs/UIDistribution/{targets.json,mapping.json,meta-result.json,meta-diff.txt,importers/policy.csv}`。

## 再読込で見つかった編集表示の追補

先行の編集プレビューは適用直後のCanvasで成功していたが、Editorを開き直すとPrefabから継承した右側の旧見本が残った。スクリプトで変更したPrefabプロパティのoverride記録が不足していた。`BusinessEditPreview.Apply` でPrefabインスタンスの各変更を明示記録し、`VerifySaved` で再読込したシーンをInitialize／再適用せずに検査する。保存するのは営業準備の猫一覧。実セーブ読込・ゲームコマンドなしという仕様は維持する。

## 検証の入口と区別

`UIDistributionVerification.Run` はEditor Canvas描画で5タブ・両解像度・長文末尾・下段操作の境界・一覧復帰を検査する。`BusinessEditPreview.VerifyAndSave` は保存／再読込の検査。Sceneビューの表示倍率はゲームの解像度ではなく、Canvas描画およびGame／Playerの実解像度と区別する。

Playerは `--catcafe-ui-distribution <専用出力>` で限定検証。5タブの通常リスナー、一覧復帰、営業準備の表示と代表の通常交流コマンドを使う。長文は実プロフィールを8回繰り返した画面用fixtureで、末尾 `[UI QA END]` とスクロール底面を検査する。メモリ内の表示検査前後でSimulation.Stateが一致し、専用slot1にfixtureがないことを検査する。fixture保持のためQA内だけ状態ポーリングを停止する。通常起動には作用しない。F8はQA入力待ち画面だけ1280×800／1024×768を切り替える。

`--ui-resume` は同じ出力の専用slot1を別プロセスから読み、代表交流の本文・画像を含むplayer_interaction全JSONをreceiptと照合する。実ユーザーの `standalone-v1` は検証保存先に使わない。

## コミットと依存

今回保存する画像変更は明示対象の27metaのみ。対になるPNG、bundle／interaction_texts、制作記録、Python登録スクリプト等の先行制作一式は未追跡／未コミットのまま保持する。したがって今回のコミットだけを新規checkoutしてもモモ素材一式は再現しない。PNGのファイル名は内容ハッシュで、今回の対応一覧に制作元ファイルを記録した。

未追跡のMomoDailyImageVerificationも今回丸ごと追加しない。そのローカル変更はRefresh→登録完了の1行だけで、[再適用patch](Results/UIDistribution/momo-registration.patch)を保存する。制作一式を保存する段階で同変更も保存する必要がある。今回コミットする汎用登録入口・検証コードと、ローカル制作コードの依存を混同しない。

## 最終結果

通常の `build_standalone.ps1 -ProjectPath C:/Users/hi-wa/unity_workspase/CatCafeSim -UnityPath "C:/Program Files/Unity/Hub/Editor/2021.3.45f1/Editor/Unity.exe"` で成功。ExecuteMethodは既定の `StandaloneScreenSetup.Build`、共有StandaloneBuildStorageと固定Latest／Pending／Runsを使用。最終成功ログは `Logs/StandaloneBuild/Runs/20261029-101101-261-3f6b8d9a1cc34e38b8cfd7e5c466b6a0.log`、exit0と成功マーカーを確認。今回UI修正とモモ27画像を配布Latestへ反映済み。

最初の通常ビルドはガード通過後、resources.assets.resSのコピー時にディスク空き不足（112）で失敗。Pendingは共有処理で削除され、直前Latestを保持した。その後空き約25GBを確認し、素材・セーブ・過去証跡を削除せず再実行に成功。残存UnityLockfileは対象Editorが終了済みと確認して1件だけ解除した。sandboxの初回登録試行はライセンスIPC待ちで終了199、通常権限で登録成功。ProjectVersion.txtは開始時のf2表記へバイト復元した。

|確認方法|1280×800|1024×768|
|---|---|---|
|Editor Canvas・メモリ表示fixture|営業準備、5タブ、長文末尾、境界、一覧復帰成功|同じ検査成功|
|Editor Play・Gameビュー|実Screen寸法を検査して営業準備、5タブ、長文末尾、境界、一覧復帰成功|同じ検査成功|
|固定LatestのUIリスナー／fixture|営業準備、5タブ、長文末尾、境界、一覧復帰成功|同じ検査成功|
|OS入力・猫詳細|ホイール位置変化。スクロールバーのページ送りで位置0・末尾マーカーを確認|ホイール位置変化。バー操作後の残りをホイールで位置0・末尾マーカーへ到達。戻るボタンの実クリックで一覧復帰|
|代表画像・本文／専用保存|ミケの通常ねこじゃらしdirectをコマンドで実行し画像・本文を描画。別PIDでslot1のplayer_interaction全JSON一致、画像ロード・描画を確認|初回の代表描画・専用保存を確認。別PID再開の表示確認は1280で実施|

Sceneビューは保存済み初期営業準備を開き直して目視し、旧見本が消えていることを確認。SceneのズームやGame窓のScale・表示窓の大きさとゲーム解像度は区別した。Game窓が768pxより低い場合は表示窓が一部を切り取り、ScreenCaptureは指定解像度全体を取得する。比較の根拠は同じ状態の全解像度キャプチャ。

Game検証の初回2回は検証用の内部API参照／未Refreshのコードで失敗したため、参照とRefreshを修正して両解像度で再実行成功。非表示Player起動の初期1280／再開PNGは黒かったので採用せず、前面起動の `player-visible` で取り直し、全画面が描かれたことを確認した。これらを製品UI不具合の件数に含めない。最終ビルド後のRuntimeソース変更は `#if UNITY_EDITOR` 内のGameサイズ選択のみで、Playerの実行分岐に変更はない。表示窓回復用に使った一時Editor初期化hookは撤去した。

OSドラッグAPIではバー操作に伴う位置変化は記録したが、本文面をつかんだ連続ドラッグの成功を確証できなかった。人の実マウスによる猫詳細ドラッグは今回未実施。過去の交流本文ドラッグ成功とは区別する。ホイールは詳細欄の既存感度で1入力あたり約1pxと小さいが、入力は届いている。ゲームの感度・スクロール処理は変更していない。

Play停止直後の編集画面では動的な猫一覧が消える状態も観測した。保存ファイルの変更ではなく、開き直した保存済みSceneの表示は確認済み。停止直後の表示まで自動で再構築する変更は加えておらず、必要時は既存の編集プレビューメニューで再適用する。通常Play／配布版の状態と保存へのfixture混入はなく、実ユーザーのstandalone-v1の6ファイルは前後ハッシュ一致。

自然プレイ全条件・全画像目視・モモ全場面の配布版再検証・長期評価／総合一式・他GPUは対象外。最終PlayerはRTX5070／D3D11環境。証跡は `Logs/UIDistribution/editor`、`game-final`、`player-visible`、`player-final/input-trace.txt`。選別した画面・監査・結果を [Results/UIDistribution](Results/UIDistribution) に保存。元の検証ログ・専用保存はLogsに保持した。

## 容量・保持状態

LogsはLatestとRunsを内包し、単純加算しない。[容量記録](Results/UIDistribution/capacity-after.json)の測定後の少量の文書／監査更新は表の差分外。

|保存先|開始bytes|終了bytes|増減bytes|
|---|---:|---:|---:|
|Latest|5,506,516,854|5,535,003,690|+28,486,836|
|Logs全体|20,122,378,256|20,170,182,591|+47,804,335|
|Runs|213,489,416|216,070,089|+2,580,673|
|既存Unity Library|7,891,773,893|7,859,010,357|-32,763,536|
|Tool generated|3,277,372,585|3,277,372,585|0|

Latest増約28.5MBは主にモモ27枚の収録。Logsの残りの増分は両解像度のEditor／Game／Playerの画面、監査、専用保存、ログ。Libraryの変化はUnity自身の再取り込み／ビルドキャッシュ更新で、手動整理なし。成功Latestは156ファイルの1組。StandaloneBuildのディレクトリはLatest／Runsのみ、Pending／Previousなし。ビルドZIP・別ビルド・Unityプロジェクト・Libraryコピーなし。PNG原本・実セーブ・並行制作を削除していない。

開始差分391件のうち今回の明示変更は27meta、Tool／Unityのモモ検証1行ずつ、ToolのモモStatus追記の30件。残り361件は開始時とハッシュ一致。モモStatus追記も制作一式へ混入させず [patch](Results/UIDistribution/momo-status.patch) を保存する。pushなし。
