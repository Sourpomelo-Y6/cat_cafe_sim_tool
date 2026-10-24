# 猫画像のStandalone取り込み設定（2026-10-24）

2026-10-24履歴照合：限定適用はツール3c9aaea、実Unity25cd623へコミット済み。先行ハル制作もツールb468bccへ保存済み。共有DocsはツールGit外の独立リポジトリ。容量・設定・検証値は本作業時点の記録で、後続リン素材はLatest未収録。

今回の依頼は恒久適用と配布版検証。前回の[比較・提案](UnityImageStorageNewUI.md)とは別の作業。限定適用・最終配布Player検証を完了し、Latestは実測で6,930,688,659 bytes（76.90%）減少した。

## 対象と維持するもの

`Assets/Resources/CatCafeContent/Imports/<pack>/images/*.png` のうち、元の辺が1024pxを超える正方形で、完全に不透明な猫画像。bundleの猫sceneに画像名が対応することを確認する。過去パック内でそのbundleから使われなくなった25枚も、別パックの猫sceneと内容ハッシュ名が一致する場合に対象とする。13パックのPNG 1,897枚中1,889枚が候補。初期1024px・DXT1の8枚は維持する。

小アイコン・シルエット・タイトル・非正方形・透過・1024px以下は対象外。新しい透過PNGでは実alpha値も確認し、形式名だけで不透明と判断しない。RGB PNGのtRNSも除外判定する。

Standalone override有効、maxTextureSize 1024、BC7、compressionQuality 100、Crunch無効、mipmap無効、read/write無効。他プラットフォーム設定、sRGB、filter、wrap、resize algorithmは保持する。採用元PNG、GUID、pack ID、catalog・本文・画像対応・保存参照・Extraキー・ゲームロジックは変更しない。

`CatTexturePolicy.ApplyAll` は明示的な既存パック移行。今後の登録は既存の `ContentPreviewSetup.Import` がハッシュ検証・配置・Refresh後に `ApplyPack` を呼ぶ。AssetPostprocessorで既存全素材へ自動適用する方式は使わない。インストーラーへ新ファイルを登録する。

ビルドの `IPreprocessBuildWithReport` はStandaloneWindows64の対象設定を読み取り、不一致パスを示して失敗させる。ビルドからConfigure・SaveAndReimportは呼ばない。

## 代表画像の実Importer比較

ミケ・タマ・ソラのnormal / teaser__switch / teaser__direct / teaser__open_upの12場面（画像は10種類）。実Importer出力は1024×1024、BC7、mip数1、readable falseを確認。既存新UI Canvasで、元PNGを原寸ロードした対照と実Importer出力を、1280×800・1024×768で48枚描画した。画像差替えfixtureであり、12場面すべてを通常行動や実入力で発生させた検証ではない。

400／320pxの実寸部分で、目・ひげ・三毛模様・黒白境界・白長毛の毛先・手・棒・羽根の判別を保持。細かな毛筋・羽根に微差があるが、明瞭な輪郭欠落・ブロック崩れ・大きな色ずれは見られず、限定対象への適用を継続した。ログは `Logs/ImageImportApplication/actual-canvas-retry.log`、全画面PNGは `actual-canvas`、実寸比較パネルは `native-<cat>-<width>.png`。半縮小JPEG一覧は索引用で、実寸判定の根拠にしない。

初回同期時のエスケープ誤りによるC#コンパイルエラーを修正して再実行した。初期試作のAssetPostprocessor登録によって全textureの再取り込みが起きたため、明示的な登録経路へ変更した。この初期処理では代表以外の設定を変更していない。追加のプロジェクト／Libraryは作っていない。

## 開始容量・変更管理

Latest 9,012,696,043 bytes／156ファイル、画像stream `resources.assets.resS` 8,921,956,544 bytes。Logs 19,224,713,155 bytes、Runs 129,306,797 bytes、既存Library 12,524,371,316 bytes。

開始時ステータス・先行制作ツール差分、原本／JSONのSHA-256、GUID、meta、Latestのパス別容量を `Logs/ImageImportApplication` へ保存。先行するハル制作と前回画質比較資料は別作業として保持する。Git外の共有Docsも別に扱う。

変更前Latestの既存UI自動検証は36項目成功。専用保存と画面は `baseline-player`。通常のボタンリスナーを呼ぶ自動操作と、長文・最大体力・欠落画像のfixtureを区別する。

## 配布版・実容量

| 実測対象 | 変更前 bytes | 変更後 bytes | 削減 |
| --- | ---: | ---: | ---: |
| Latest（各156ファイル） | 9,012,696,043 | 2,082,007,384 | 6,930,688,659 / 76.90% |
| resources.assets.resS | 8,921,956,544 | 1,991,260,880 | 6,930,695,664 / 77.68% |
| 採用元PNG | 3,714,881,558 | 3,714,881,558 | 0 |

[全件Importer実測](Results/ImageImportPolicy/importers.csv)の1,889行が1024×1024 / BC7 / mip 1 / readable false / 設定一致。初期8枚は除外し、そのmetaを含む対象外metaは開始時と一致した。PNG／JSON 1,928ファイルのSHA-256、GUID 1,955件が一致し、meta変更は対象1,889件のみ。Standalone以外は末尾空白の整形を除いて一致。ユーザー保存JSON 3件も一致した。[保護対象の結果](Results/ImageImportPolicy/protected-end.json)。

指定スクリプトで2回、同じLatestへ順次ビルドした。初回は検証fixtureとImporter監査を併せた入口、最終は通常の既定入口。最終ログは `Logs/StandaloneBuild/Runs/20261024-164534-898-4571acde51114915b2f3aa93ef4c97ec.log`。成功マーカーとexit 0を確認。最終コードの通常ビルドガードおよび `VerifyAndBuild` は設定の読み取りのみで、設定不一致fixture／登録の冪等性試験は独立した `VerifyGuardAndRegistration` に分けた。設定不一致の試験はメモリ上のoverrideを無効にして拒否を確認後、finallyで復元し、meta無書き込みを確認した。

### 最終配布Playerの結果

| 区分 | 結果 |
| --- | --- |
| 通常UI自動＋境界fixture | 既存交流UI36項目成功。通常リスナーの行動・履歴・途中／終了保存、長文80行・最大体力・未登録代替を含む。新UIの両解像度で画像を保持 |
| 代表描画fixture | 三毛・黒白・白長毛の4場面×両解像度＝24枚。実PlayerでBC7・1024px・mip／readabilityを確認。全代表の実寸目視で明瞭な劣化なし |
| 過去画像 | 13パックから各1参照をResourcesロード。75f31c…の画像参照をそのまま保存し、別プロセスで同じ文字列・画像ロードを確認 |
| 保存再開 | 最終Playerで作った途中／終了保存を別プロセスで再開。変更前Playerのslot2／slot3も専用コピーから再開し、ファイル全バイトが元と一致 |
| Extra会話 | 初回18／別プロセス19項目成功。既読分岐・バックログ・回想・保存／解放情報の非変更 |
| CG | 初回23／別プロセス24項目成功。画像ロード・未解放除外・可視時解放・拡大・会話との往復・保存／解放キー非変更 |
| Windows実入力 | ミケ1024×768の履歴開閉、待機行動で別画像へ切替、手動終了後の画像、営業準備復帰。[実入力記録](Results/ImageImportPolicy/physical-input.md) |
| 旧ルール | ActiveInteractionCatalogの古い保存は従来同様 `Invalid standalone save`。旧rules_id 6c3e11…と現行2613fc…を比較し不一致を確認。現在ルール内の古いパック画像は上記のとおり成功。旧ルール全体の移行は追加しない |

Extra／CGの既存検証はPASS後に待機するため、成功ファイルを確認して専用プロセスを停止した。通常UIリスナー、自動描画差替え、境界状態、実入力を混同しない。24枚の描画fixtureは全特別反応の自然発生確認ではない。起動時の黒画面は今回の画質根拠に使っていない。

元PNGを同じUIで描いた対照（左）と最終Player（右）の実寸比較：

| 猫 | 400px（1280×800） | 320px（1024×768） |
| --- | --- | --- |
| ミケ | [比較](Results/ImageImportPolicy/player-compare-mike-1280.png) | [比較](Results/ImageImportPolicy/player-compare-mike-1024.png) |
| タマ | [比較](Results/ImageImportPolicy/player-compare-tama-1280.png) | [比較](Results/ImageImportPolicy/player-compare-tama-1024.png) |
| ソラ | [比較](Results/ImageImportPolicy/player-compare-sora-1280.png) | [比較](Results/ImageImportPolicy/player-compare-sora-1024.png) |

目・ひげ・模様・長毛の淡い境界・手・棒・羽根を保持。小さな毛筋の滑らかさには差がある。24比較のRGB PSNRは40.845～44.561dBで、知覚品質の合否を数値だけで決めていない。部分切出しは400pxで(232,176)-(632,576)、320pxで(186,205)-(506,525)、再拡縮なし。全画面PNGは `Logs/ImageImportApplication/texture-player`。周囲の文・猫名は画像差替えfixtureのミケ文のままで、別猫の文章選択試験ではない。

### 時間・メモリ・互換性

Windows 11 (10.0.26300) / NVIDIA GeForce RTX 5070 / Direct3D11、`SupportsTextureFormat(BC7)=true`。この環境の実Player描画を確認。他GPU、非対応GPUでの展開・画質・メモリ、別APIは未確認。

同じ交流UI検証を各1回、Editor終了後・1280×800開始・専用の新保存で比較した。[変更前](Results/ImageImportPolicy/baseline-process.json)／[変更後](Results/ImageImportPolicy/final-process.json)。

| プロセス測定 | 変更前 | 変更後 |
| --- | ---: | ---: |
| 起動から初回準備PNG検出 | 7,793 ms | 6,317 ms |
| 全36項目の実行時間 | 13,984 ms | 12,471 ms |
| peak working set bytes | 375,758,848 | 284,651,520 |
| peak private bytes | 649,449,472 | 434,176,000 |

100ms間隔のプロセス測定。初回PNGまでには検証の固定待機も含み、操作可能までの純粋な起動時間やcold-cacheベンチマークではない。1回ずつなので改善率を性能保証にしない。別描画fixtureのResources.Loadは初解像度で中央値0.119ms／最大0.279ms、次の解像度で0.0115／0.0167ms。ただし既ロード参照・キャッシュを含み、GPU転送完了や入力から画像表示までの時間ではない。画像切替の厳密な変更前後比較は未測定。最終非Development PlayerのProfiler.GetRuntimeMemorySizeLongは0を返し測定不能だった。0 bytes使用とはせず、Editorの約2MiBという返値や上記プロセスメモリをPlayerのGPU使用量とみなさない。

### 生成物の容量・残る範囲

[開始](Results/ImageImportPolicy/capacity-start.json)／[終了](Results/ImageImportPolicy/capacity-end.json)の採取時点：Logs 19,224,713,155 → 12,366,800,539 bytes（−6,857,912,616）。今回の記録は71,187,766 bytes、Runsは129,306,797 → 130,895,074 bytes（＋1,588,277）。既存Libraryは12,524,371,316 → 4,171,077,548 bytes（−8,353,293,768）。Libraryは再取り込み／ビルド後の既存キャッシュを計測したもので、手動削除はしていない。記録JSON自身やその後の資料追記で数KB程度増える場合がある。

Latestは156ファイルの1組、子フォルダはLatest／Runsのみ、Pending／Previousなし。成功時は移動で置換し、別ビルド・ZIP・Unityプロジェクト・Libraryを作っていない。素材、セーブ、他作業成果物は削除していない。比較画像・記録を資料へ約9MB保存する。

全猫の全絵の目視、他解像度、他GPU/API、非対応GPU、厳密なGPUメモリ／cold-load／入力からの切替時間、旧rules_id全体の移行、全目標長期評価は未確認・対象外。パック削除・重複排除・aliasは未実装。

## 再現・変更ファイル

通常ビルドは `build_standalone.ps1 -ProjectPath C:/Users/hi-wa/unity_workspase/CatCafeSim`。明示移行はEditorの `CatTexturePolicy.ApplyAll`、監査は `Audit`、代表比較は `RepresentativeCanvas`、検証専用のガード試験は `VerifyGuardAndRegistration`。監査の出力先は環境変数 `CATCAFE_TEXTURE_OUTPUT`。Playerは `--catcafe-texture-policy <専用出力>`、同じ出力へ `--texture-resume` を追加すると過去参照の別プロセス復元。

実装は新規Editor/CatTexturePolicy.cs、既存Editor/ContentPreviewSetup.csの登録呼出し1行、install_unity_preview.ps1の登録、新規Runtime/StandaloneTexturePolicySmoke.cs。実Unityにはこの3コードファイルと対象1,889 metaを反映。素材・GUID・シーン・ゲーム処理の追加変更はない。資料は本書・Results/ImageImportPolicy・旧比較資料の後続リンク・共有進捗／索引。先行するハル制作と前回比較コード・資料は保持。制作ツールの今回分は独立コミットへ保存する。実Unityの先行差分と画像metaは作業ツリーに保持し、先行する未登録素材を含むプロジェクト全体をこのコミットへ混ぜない。
