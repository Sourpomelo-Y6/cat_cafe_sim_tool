# 新UIに合わせた画像圧縮の再評価（2026-10-24）

> 2026-10-24後続：今回の比較時点とは別に、[1024px・BC7最高品質の限定恒久適用と配布版検証](UnityImageImportPolicy.md)を完了。Latestは9,012,696,043 → 2,082,007,384 bytes、実測76.90%削減。以下の「未適用／0 bytes」は以前の比較・UI実装作業時点の記録。

**提案：大きい不透明・正方形の猫画像は1024px・BC7最高品質を採用候補とする。今回の画質比較では現行維持を必須とする劣化は見つからず、同じ12画像の追加GPU比較は不要。ただし本採用の確定には、承認された適用作業で実Importerと配布Playerを確認する。設定は未適用、削減後ビルドは0回、実際の削減量は0 bytes。**

[比較画像一覧（原寸HTML）](Results/ImageStorageNewUI/index.html)／[元調査](UnityImageStorage.md)／[新UI実装](UnityPlayerImageLayout.md)。閲覧ソフトが画像を縮める場合は100%表示で比較する。各組は左が原本／現行、右が一時BC7候補。

本書の未適用・容量の記述は比較作業完了時点（HEAD `6130bd6`）の記録。後続の設定適用と実測は別作業の `3c9aaea` および [適用記録](UnityImageImportPolicy.md) を参照する。

## 開始時の照合

制作ツールGitのHEADは `6130bd6`（個別交流UI）、直前は `be0593e`（容量調査）。前者の差分・実装と後者の比較コード・資料・CSVを確認。共有 `Docs/DevelopmentStatus.md`、`Docs/UnityPlayerImageLayoutPlan.md`、制作・登録資料 `CatImageTodo.md`、`CatExecutionImages.md`、ハルの制作記録も照合した。

制作リストの冒頭「登録8匹」は初回調査の記録で、追補ではチロ／レオ／ハナ／セナ／ルカ／ハルまで制作・登録済み。ハルの登録・ID解決などに未コミット差分がある。実Unityの有効パックは引き続き `4762dd1bd978461bb07c041c59417281`、401 PNG。全13パックは1,897 PNG、414種類、原本3,714,881,558 bytesで前回と同じ。採用素材や登録資料は変更していない。

HEADの新UIは画像枠792×400 Canvas座標、FitInParent。1280×800では正方形400×400px、1024×768では320×320px。`PythonPlayerPanel` は保存済み `narrative_image` を `CafeNarrativeResources.Texture` で優先ロードし、不足時は既存の場面選択へ戻す。新UIは比率・表示枠の変更で、画像参照やImporterの変更ではない。

## 比較方法と証拠の区別

| 証拠 | 今回の範囲 | 判断できる点／限界 |
| --- | --- | --- |
| 既存150／600px GPU比較 | 既存12画像・72測定を再利用 | 模様・色・圧縮ブロックの大きな異常、候補間の傾向。600pxは400pxより大きいが、縮小時のサンプリング差があるので「600で良好なら必ず400でも良好」とはしない |
| 新400／320px GPU比較 | 同じ12画像×両寸法×RGBA32／DXT5／BC7＝72測定 | 新UIと一致する実表示寸法で全体・部分を比較。原本PNG→一時Texture、1024へGraphics.Blit、CompressTexture Best、実寸へ再描画。実Importerのリサイズとは異なる |
| 新UIの既存Canvas | ミケ・ねこじゃらし実行を両解像度で現行Resources RGB24と一時BC7へ差替え、計4画面 | 既存シーン・RawImage・Bilinear・画像枠・余白・周囲UIで確認。表示寸法400／320を実測。EditorのCamera/RenderTextureで撮影したfixtureで、配布Playerではない |
| 実ImporterのBC7／削減後Player | 未実施 | 一時GPU結果と同一とは保証しない。適用時の検証項目 |

Unity 2021.3.45f2、Direct3D 11（level 11.1）、RTX 5070。mipなし、原本1254×1254、比較候補1024×1024。RGBA32は縮小だけの対照、DXT5は同容量の対照。BC7 raw payloadは1,048,576 bytes／枚を実測した。これはTextureデータ量で、Playerのメモリ使用量・PNG容量ではない。Canvasの現行側はResourcesからロードしたRGB24、候補側だけが一時GPU圧縮。独立した全体比較の原本側はPNGを直接デコードして描画している。

既存 `ContentTextureComparison` に環境変数による実寸比較と明示的な代表猫指定、Editor限定 `RunCanvas` を追加。通常インストーラー・通常ビルド・Runtimeには追加していない。シーンを保存せず、Importerの設定・再インポート処理は呼ばない。新UI Canvasの画像差替えは画質fixtureであり、実操作による全反応の選択試験ではない。

## 目視結果と比較画像

代表はミケ（三毛短毛）、タマ（白黒短毛）、ソラ（白長毛）。各通常 `normal`、準備 `teaser__switch`、実行 `teaser__direct`、特別反応 `teaser__open_up`。既存12画像で必要な特徴を補えており、追加猫画像は作っていない。通常と準備が同じ絵のタマ／ソラも、別場面の対応として表示し、異なる12種類の絵とは数えない。

| 猫・特徴 | 400px／320pxでの観察 | 比較資料 |
| --- | --- | --- |
| ミケ | 顔・目の縁・ひげ、三毛境界と尻尾、持ち手・棒・羽根を判別可能。全4場面で明瞭なブロック崩れ・輪郭欠落なし。2倍拡大では模様の細線や羽根に微小な滑らかさの差がある | [400px](Results/ImageStorageNewUI/playtest-mike-400.png)・[320px](Results/ImageStorageNewUI/playtest-mike-320.png)・[部分2倍](Results/ImageStorageNewUI/playtest-mike-crops.png) |
| タマ | 暗い毛と白い胸の境界、黄色い目、ひげ、手・棒・金具の輪郭を保持。細かい毛の粒状感は完全一致ではないが、両実寸で識別を損なう崩れなし | [400px](Results/ImageStorageNewUI/playtest-tama-400.png)・[320px](Results/ImageStorageNewUI/playtest-tama-320.png)・[部分2倍](Results/ImageStorageNewUI/playtest-tama-crops.png) |
| ソラ | 青い目、白長毛と背景の低コントラスト境界、頬・胸・尾の毛先を保持。拡大では淡い毛筋・羽根の質感に小差がある。白い面の明瞭なブロック化・大きな色ずれなし | [400px](Results/ImageStorageNewUI/playtest-sora-400.png)・[320px](Results/ImageStorageNewUI/playtest-sora-320.png)・[部分2倍](Results/ImageStorageNewUI/playtest-sora-crops.png) |
| ミケ実行・新UI Canvas | 現行と候補で全身・上げた前足・手・道具が同じ枠に収まり、400／320pxで目立つ劣化なし。周囲の本文・操作は同じ | [1280現行](Results/ImageStorageNewUI/canvas-1280-source.png)・[1280 BC7](Results/ImageStorageNewUI/canvas-1280-BC7.png)・[1024現行](Results/ImageStorageNewUI/canvas-1024-source.png)・[1024 BC7](Results/ImageStorageNewUI/canvas-1024-BC7.png) |

全体パネルは実寸PNGをリサイズせず配置。部分拡大は**表示後のピクセルをnearestで2倍**にしたもの（400px側80×80、320px側64×64の切り出し）。元1254pxの等倍細部ではない。各行は顔／毛・模様／手・道具（ない場面は足・尾）の3組。切り出し座標、原本ハッシュ、画像寸法、パネル配置は [manifest.json](Results/ImageStorageNewUI/manifest.json)。全文の顔・ひげ・羽根は全体パネルも併せて確認する。

## 数値結果

原本を同じ表示寸法で描画した画像とのRGB PSNR。各欄12測定、単位dB。大きいほどRGB誤差が小さいが、人の知覚の合否基準ではない。

| 候補 | 400px 最小／平均／最大 | 320px 最小／平均／最大 |
| --- | --- | --- |
| 1024 RGBA32（縮小のみ） | 41.293／43.536／45.390 | 41.974／43.268／45.410 |
| 1024 DXT5 Best | 38.946／40.316／41.742 | 39.333／40.213／41.665 |
| 1024 BC7 Best | 41.043／43.104／44.860 | 41.660／42.880／44.899 |

[72行CSV](Results/ImageStorageNewUI/comparison.csv)・[集計](Results/ImageStorageNewUI/stats.json)・[Canvas寸法と形式](Results/ImageStorageNewUI/canvas.csv)。BC7平均はDXT5より400pxで2.788dB、320pxで2.668dB良好。縮小のみとの平均差は0.432／0.387dB。異なる寸法のPSNRを単純に品質順位として比較しない。原本はRGBで、微小なalpha誤差の数値があっても透過縁を検証した根拠にはならない。

## 採否比較

| 案 | 画質 | 容量の位置付け | 互換・不確実性 | 提案 |
| --- | --- | --- | --- | --- |
| 現行1254 RGB24主体 | 原本解像度を保持 | Latest **9,012,696,043 bytes実測**。代表1254のraw RGB24は4,717,548 bytes／枚 | 現行環境の基準。容量の問題が残る | 適用が承認されるまでは維持 |
| **1024 BC7 Best、mip/read-writeなし** | 新実寸で良好。毛筋・羽根の微差あり | 候補1MiB／枚実測。全1,897枚一律の場合、Latest約2.09GB、約6.93GB削減は**推計** | 今回はDX11対応GPUのみ。実Importerの縮小・色・対応GPU/API・実Playerのメモリと切替は未確認 | **大きい不透明正方形猫画像に限定して推奨**。追加の同種GPU比較より適用時のImporter/Player確認を優先 |
| 1024 DXT5 Best | 同容量BC7より誤差大 | 1MiB／枚実測。理論上BC7同程度 | 対応範囲の実機検証なし | 今回選ぶ理由が弱い。BC7非対応を支援する必要が判明した場合に再評価 |
| 1024非圧縮 | 縮小だけの基準 | RGBA32 4MiB／枚実測（RGB24なら3MiB理論）。減量効果が小さい | 実Importerは未比較 | 縮小の影響を分離する対照として使用 |
| 2048 BC7／512 BC7 | 今回未比較 | 4MiB／0.25MiB理論 | 前者は拡大、後者は表示細部不足の懸念を未解決 | 追加比較は現時点で優先しない |

6.93GBは前回と同じ全枚一律のモデル（1,889枚RGB24＋初期8枚DXT1を全て1024 BC7にし、その他容量を固定）で、今回の実測削減量ではない。初期8枚をDXT1維持なら一律BC7よりさらに4MiB小さい推計。推奨する限定適用では対象確定後に別途算出する。透過シルエット・96px人物アイコン・将来の透過猫・非正方形画像にはこの推計・評価を一律流用しない。GPU非対応時のフォールバック動作・メモリは未測定なので、全Windows環境で同じ容量／画質とはしない。

## 適用を依頼された場合に必要な作業

1. 対象を大きい不透明正方形猫PNGへ限定し、既存パックの原本・GUID・パックID・catalog・保存文字列・Extraキーを保持したままStandalone max1024／BC7／quality100を明示設定する。初期1024 DXT1・透過・小アイコンの扱いは別に決める。今回は実装しない。
2. 代表12画像を**実Importer**で再確認し、実サイズ・format・mip数・read/write・sRGB・リサイズ方法と絵を記録する。今回のGPU描画とは別の結果として比べる。手・目・ひげ・毛先・模様・道具に破綻が出た画像だけ個別に保留／例外へ回す。
3. 適用が認められた時点で指定 `build_standalone.ps1 -ProjectPath C:/Users/hi-wa/unity_workspase/CatCafeSim` から固定Latestへ一回ビルドし、成功後にPendingを移動。配布Playerの新UI両解像度・通常／準備／実行／特別反応、過去パックの保存画像、保存再開、Extra解放非変更、未登録代替を確認する。旧rules_id全体の移行とは切り分ける。
4. Latest/resSの前後実バイト、メモリ・ロード／切替時間、対象GPU/APIでのBC7対応と表示を記録して採用を確定する。設定の継続適用方法・検出方法の実装は別途依頼された範囲で行う。今回AssetPostprocessorやビルドガードは追加していない。

パック間重複削除とaliasは別作業。本文・UI・数値・バランスの変更や全目標の長期評価・総合確認の繰り返しは実施していない。

## 再現

既存Unityプロジェクトの `Assets/CatCafePreview/Editor/ContentTextureComparison.cs` に今回のEditorコードだけを同期する。通常インストーラーは使わない。

```powershell
$env:CATCAFE_TEXTURE_NEW_UI='1'
$env:CATCAFE_TEXTURE_OUTPUT='C:/Users/hi-wa/source/codex_repos/CatCafeSim/Logs/ImageStorageNewUI/comparison'
# Unity.exe -batchmode -quit -projectPath <既存プロジェクト> -executeMethod CatCafePreview.Editor.ContentTextureComparison.Run -logFile <ログ>
# 同じプロジェクト、OUTPUTを Logs/ImageStorageNewUI/canvas に変更して RunCanvas を実行
python cat_cafe_sim_tool/workflows/compare_new_ui_images.py
```

`CATCAFE_TEXTURE_NEW_UI` 未指定では従来150／600px。実寸モードはRGBA32／DXT5の数値のみ保存し、PNGは原本とBC7に絞る。HTML／代表パネル再生成はPillow 11.0.0を使用。ログは `Logs/ImageStorageNewUI/unity-retry.log`（72測定成功）、`unity-canvas.log`（Canvas成功）。初回sandbox実行はライセンス接続でexit199、ホスト実行の再試行はexit0。黒い画面は今回の目視根拠に含めていない。

## 容量と変更管理

終了容量と保護対象の一致結果は [capacity-end.json](Results/ImageStorageNewUI/capacity-end.json)。開始時Logsは19,210,493,431 bytes（スナップショットスクリプト設置後19,210,494,613）。StandaloneBuildは9,142,002,840 bytes、Latestは9,012,696,043 bytes／156ファイル、既存Libraryは12,524,130,696 bytes。ビルド・別プロジェクト・別Libraryを作らず、素材・セーブ・他作業成果物を削除していない。

変更対象は本資料、既存容量／新UI資料の追記、Results/ImageStorageNewUIの比較資料、Editor比較コード、比較パネル生成スクリプト、Git外共有進捗・計画の追記のみ。制作ツールのハル関連先行差分と実Unityの多数の先行差分はコミットへ混入させていない。比較成果物は今回の専用コミットに保存する。圧縮設定の恒久適用へ進まず、本比較・提案で完了する。

終了時Logsは19,224,713,155 bytes（開始から+14,219,724）、今回の比較ログは14,219,724 bytes。Libraryは12,524,371,316 bytes（+240,620）で、既存プロジェクトのEditor比較コードのコンパイルに伴う増加。比較資料は約11.4MB（13 PNGとCSV・HTML等）。容量記録JSON自身の追記分を除く採取時点の値を示す。StandaloneBuildとLatestは開始時と同一、Latest一組・Pendingなし。UnityのResources PNG／meta／catalog等と保存JSONを含む3,883ファイルは開始時SHA-256と全件一致、Latest全156パスとバイト数も一致した。原本の再圧縮・設定適用・ビルドによる増減はない。
