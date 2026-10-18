# Extra・猫の通常画像CG一覧

2026-10-18実装。タイトルのExtraから「CG一覧」で開く。ゲームのプレイ画面で表示した既存の猫の通常画像を一覧・拡大鑑賞できる。現在の素材は8枚。会話の回想とタブで往復でき、タイトルへ戻れる。ゲームルール・費用・加入・イベント処理は変更していない。

## 表示と閲覧記録

- インポート済みcatalogのnormalだけを対象とする。遊び・撫で・睡眠・イベントCG・背景・人物・タイトル・シルエットは対象外。新規画像やサムネイル用画像ファイルは生成せず、既存Textureを共用する。
- ViewedCatImagesは営業画面の描画後に0.25秒間隔でRawImageを観測する。非表示、透明、画面外、マスク外、上位の不透明なUIに隠れた画像を除外する。画像取得だけでは記録しない。タイトル・回想・CG鑑賞では新たに解放しない。
- 現在の単一CanvasのUIを対象とした判定。中央と四分点の5点を確認するため、ごく細い端だけの表示や短時間の表示は記録されない場合がある。任意のシェーダー、複数Canvas間、3D遮蔽の汎用判定は未対応。
- IDはnormal/画像ファイルのSHA256。同じ画像は一件に集約し、代表名はcatalogの先頭を使う。画像を差し替えると新画像は未解放になる。素材がなくなっても過去の閲覧キーは保持する。
- 未解放は名前・画像を出さず「未解放」の空カードとする。直接指定でも開けない。欠損画像はボタンを無効にする。catalogがない場合も空一覧で成立する。
- 一覧はスクロール可能。拡大は縦横比を維持し、一覧へ戻るとスクロール位置を保持する。

## 保存

LocalCafeBackendの通常セーブディレクトリに独立したextra-images-v1.jsonを保存する。version=1とviewedキー配列。全スロット・新規ゲームで共有し、営業セーブや会話のextra-v1.jsonとは分離する。旧セーブから表示実績を推測して解放しない。端末間同期はない。

CafeImageGalleryStoreは形式・件数・重複・キーを検証し、最新ディスク記録との和集合を保存する。.tmpに書いてflush・再読込後に置換し、.bakを保持する。主ファイル破損時はバックアップを読み、修復保存では壊れた主ファイルを.invalidに残す。両方壊れていれば上書きしない。書込失敗時は既存記録を維持し、表示観測側は5秒後に再試行する。同時複数プロセスの排他保証は対象外。鑑賞操作は営業状態・セーブスロット・閲覧記録を変更しない。

## 構成

Standalone/CafeImageGallery.csがキーと保存、CatImageGalleryCatalog.csが既存素材カタログ、ViewedCatImages.csが描画観測、CatImageGalleryPanel.csが一覧・拡大を担当する。ContentPreviewが観測器を付け、TitleExtraPanelが入口と切替を持つ。install_unity_preview.ps1に追加ファイルを登録した。

## 検証結果

- .NET会話・保存検証430 assertions。新規、和集合、古いインスタンスからの追加、形式不正、書込失敗、バックアップ復旧、両ファイル破損を含む。
- Unity Editor: 既存受け入れ969・派遣303 checks、CGバックエンド4 checks。
- 配布版専用検証: 初回23、プロセス再起動24 checks。非表示・遮蔽・マスク・透明・シルエットを除外し、実際に表示したミケとタマだけ2/8解放。新規ゲーム・保存ロード後の共有、未解放の直接呼出拒否、拡大・一覧・回想切替と営業状態／slot-3／記録ファイルの不変を確認。Unityエラー0件。
- 既存配布版回帰: Extra回想18、受け入れ会話37 checks、Unityエラー0件。
- 実画面1280×800と1024×768で一覧、未解放クリック、拡大、一覧復帰、再起動の保持を操作。1024ではスクロールして最終行まで読めることを確認。

証跡は作業ルートLogs/StandaloneCGGallery。core-result.txt、Editorの各結果、Player/result-first.txt・result-restart.txt、ExtraRegression/result.txt、IntakeRegression/result.txt、各player.log。画面はPlayer/grid-1280.png・enlarged-1280.png・grid-1024.png・grid-bottom-1024.png・enlarged-1024.png。専用cases等に保存し、通常プレイのセーブは使用していない。

検証中、UnityのIndexedSetがforeach未実装である問題を添字走査で修正した。また遮蔽テストのオーバーレイがCanvas外だったfixtureを修正し、描画前の古いdepthを読む問題を描画終了後の観測に直した。初期失敗ログはPlayerにindexedset-failure、visibility-fixture-failure、before-render-failureの名前で保持。最終ビルドは全項目成功。

## ビルド・容量

指定入口cat_cafe_sim_tool/build_standalone.ps1 -ProjectPath C:/Users/hi-wa/unity_workspase/CatCafeSim -ExecuteMethod CatCafePreview.Editor.CGGalleryVerification.VerifyAndBuildを使用。共通StandaloneBuildStorageでPendingからLatestへ移動して置換。最終ログはLogs/StandaloneBuild/Runs/20261018-181541-445-0d4e0d9f84584485b80a5846babe46c8.log。出力はLogs/StandaloneBuild/Latest/CatCafeStandalone.exe。

| 対象 | 開始 | 終了 |
| --- | ---: | ---: |
| Logs全体 | 17,634,349,392 bytes | 17,639,760,344 bytes |
| 既存Unity Library | 10,984,176,292 bytes | 10,984,463,893 bytes |
| Latest | — | 8,247,636,092 bytes / 156 files |
| 今回の検証記録 | — | 2,318,254 bytes / 83 files |

Logsは約5.41MB、Libraryは約0.29MB増。主因は4回のビルドログ、検証保存とスクリーンショット。大きな増加はない。計測後のログ・容量JSON保存分に小差がある。StandaloneBuild直下はLatestとRunsだけでPendingなし。成功ビルド一組、ZIP・プロジェクト・Libraryの複製なし。自分の検証Playerは終了。ソース・素材・通常セーブ・別作業の成果物は削除していない。

再検証はdotnet run --project cat_cafe_sim_tool/tests/standalone/StandaloneChecks.csproj -- --conversation cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/rules.json。Playerは--catcafe-cg-gallery-check <専用出力>、再起動時--cg-restartを併用する。

## 残る課題

他の画像種別・イベントCG、エンディング、全体オプションは後続。全8枚の通常進行による長期収集は今回未実施。観測の汎用化、複数プロセス同期は必要時に検討する。既存の未コミットAllCats・CatImageSchedule・CatImageTodoとrules／presentationは今回のコミットから除外する。pushしない。


全体オプションは[UnityOptions.md](UnityOptions.md)で追加済み。音声とエンディングは引き続き後続。
