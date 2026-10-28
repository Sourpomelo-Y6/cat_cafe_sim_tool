# 本文スクロールと旧54画像metaの追補

2026-10-28。[文章統合](NarrativeUnityIntegration.md)と[画像取り込み方針](UnityImageImportPolicy.md)の後続。本文・画像制作、数値・保存形式の変更なし。

## 現物と変更範囲

開始HEAD：Python `5df6a70`、Tool `f1185ec`、Docs `2075d3f`、実Unity `b94a527`。すべてmain。既存未コミットはPython 3・Tool 116・Unity 230＝349ファイル（ディレクトリを展開した実ファイル数）で、引き継ぎ値と一致。リン・ユキ・アキの制作記録と登録コード・有効パックを照合した。

旧 `d4594a5f9a7f483ca16ddfca4ea48486` のユキ `catalog-3`／アキ `catalog-4` 各27枚だけを、既存Editor `CatTexturePolicy.ApplyAll` → `Configure` で恒久整合。Standalone override 1024px／BC7／quality100。mipmap・read/write・Crunch無効。元PNG、GUID、他プラットフォームと対象外Importerフィールドは不変。54件の差分はStandaloneのサイズ・形式・品質・overrideだけだった。対象全3,763枚の実Importer監査が適合。PNG／JSON全件ハッシュ、並行349件のうち対象54meta以外295件のハッシュ一致。[54件の対応と前後ハッシュ](Results/ScrollMetaFollowup/meta-audit.json)。

54metaは開始時から未追跡だったため、今回明示対象のmetaだけを選別してGit保存する。対になるPNGを含む先行画像制作・登録コード・台帳は未コミットのまま保持する。画像制作一式の保存完了を意味しない。

統合入口 `CompletedNarrativeVerification.BuildLatest` のバックアップ・一時Configure・finally復元を撤去。現在は `CatTexturePolicy.Validate` と通常の `StandaloneScreenSetup.Build`。ガードは読み取りのみで、設定不一致を無効化しない。成功後も54metaを戻さない。

## 今後の取り込み

正式な `ContentPreviewSetup.Import` は配置・Refresh後の既存 `CatTexturePolicy.ApplyPack(target)` を通るため、今回と同じ漏れは生じない。一方、制作時の `register_yuki_images.py`／`register_aki_images.py` は既存パックへPNGを直接追加し、各DailyImageVerificationはRefreshするだけだった。この直接追加では再発し得る。今後は正式Importを使用し、既存パックへの直接更新が必要な場合も登録直後に既存 `ApplyPack`（全既存パックの明示移行は `ApplyAll`）を実行してから検証する。Refreshだけを取り込み完了と扱わない。正式経路はすでに正しく、ガード・原本圧縮・AssetPostprocessorの変更は不要。

## 入力の期待仕様と確認の区別

`PlayerInteractionLayout.Apply` は同じScrollRectを最新描写／全履歴に使用し、Viewportのraycastを有効、ホイール感度24。本文面のドラッグもScrollRectへ届く設計。`PythonPlayerPanel.Render` は表示本文が変わる時だけ先頭へ戻し、スクロールだけで本文・行動・状態を変えない。

固定Latest、既存のミケ・見守り同時成立の長文セーブを専用フォルダへコピーして確認。自然プレイ到達の新しい主張はしない。

|方法|1280×800|1024×768|
|---|---|---|
|OSホイール・最新本文|位置1→0、content Y 0→38|位置1→0、Y 0→35.5|
|OSホイール・全履歴|位置1→0、Y 0→1024|位置1→0、Y 0→1013|
|OS行動クリック|スクロール中revision0・履歴3件を維持。1クリックでrevision1・4件・残17→16、本文・画像更新、先頭復帰|同じ結果|
|本文面ドラッグ・人の実マウス|最新／全履歴とも先頭→末尾→先頭。ユーザー操作・回答と位置ログを照合|最新／全履歴とも先頭・末尾到達。最新は末尾から先頭への復帰も記録。全履歴の末尾後の先頭復帰は今回の手入力ログでは未確認（fixtureでは成功）|
|Unityイベントfixture|Viewport→History Scrollのraycastと、最新／全履歴で位置1→0→1、状態不変を確認|同じfixture成功（浮動小数点誤差内で0）|

OS入力はcomputer-useのWindows SendInput経路。ホイールとクリックは実入力成功として記録。ドラッグのAPI試行を、人がゆっくり押したまま動かす操作の成功／失敗と同一視しない。300fps・vSync無効の検証内試行でも明瞭な変化を得られず、その一時設定は最終コードから除いた。その後、ユーザー本人の実マウスで両解像度・最新／全履歴の位置変化と末尾到達を確認。最新132文字／全履歴519文字について、1280の押下サンプル304／193、1024は219／109を記録し、すべてrevision0・履歴3件を維持した。[実入力集計](Results/ScrollMetaFollowup/manual-summary.json)と各manual-traceを参照。ユーザーの「履歴でも動いた」「本文・履歴とも動いた」と一致。製品側のスクロール修正は不要。実入力の操作はユーザー本人、OSホイールと行動クリックはWindows API、イベントfixtureはUnity内呼出しとして区別する。

Editor非再生でのイベント検査はEventSystem未稼働で失敗し、同じ限定fixtureをPlayerへ移した。初回Player fixtureはテストの画面座標上下の誤りを修正した。これら検証実装の失敗はゲームの入力不具合に数えない。

## 再現手順と保存

既存QA入口に `--scroll-only` を追加。`--catcafe-completed-narratives <専用フォルダ> --scroll-only --scroll-1280` で1280×800、省略で1024×768。専用フォルダには既存検証の `presence-simultaneous-saves` と `presence-simultaneous-receipts.json` のコピーが必要。slot2だけを既存54ケース用の厳密なreceipt検査で読み、本文・画像参照を復元する。起動時の2つのイベントfixture後は実入力待ち。選択fixture4,498件・54ケース一式は再実施しない。

`input-scroll.txt` は位置・content／viewport高・revision・履歴件数・押下フレームを記録。通常起動には記録・fixtureを適用しない。今回のOS入力証跡は `Logs/ScrollMetaFollowup/input-1280`／`input-1024`、限定fixtureは各 `scroll-routing.txt`。

1280のスクロール後に行動を1回実クリックし、通常セーブボタンで専用コピー内slot1へ保存。Player終了後の別PIDからロード一覧のslot1を選択・確定し、4手目の完成本文・画像・残16ターン・体力97を復元。旧slot2のreceiptは不変。実ユーザーの `standalone-v1` は検証用に上書きしていない（実ファイル更新日は10月19日以前）。

## ビルド・代表表示・容量

指定 `build_standalone.ps1 -ProjectPath C:/Users/hi-wa/unity_workspase/CatCafeSim`、既定の通常 `StandaloneScreenSetup.Build` を使用。Unityは先行検証と同じ2021.3.45f1を明示指定し、ProjectVersion.txtは元のf2へバイト復元。初回の制限環境実行はライセンスIPC待ちで終了199、実行権限を切り替えた既存Editorで成功。検証失敗で残ったUnityLockfileは、Unityプロセスが存在しないことを確認して記録・解除した。

最終成功ログ：`Logs/StandaloneBuild/Runs/20261028-113921-114-32f41b74c8f14e978b50326a3d03dce6.log`。通常入口のガード有効のまま成功。すべて共有 `StandaloneBuildStorage.Build`、固定Latest／Pending／Runsを使用。入力診断の変更ごとに同じLatestを置換し、ビルドの複製・ZIP・別プロジェクト・Libraryは作成していない。

最終Playerの既存画像fixtureでミケ・タマ・ソラの4場面×両解像度＝24描画を成功。BC7・1024px・mip1・readable falseを検査し、代表3画面（三毛normal 1280、黒白direct 1280、白長毛open_up 1024）を目視して明瞭な崩れ・欠損なし。猫名・本文は画像差替えfixtureのミケのままで、別猫の文章選択試験ではない。17パック各1参照のロード成功、`75f31c…` の保存画像参照を別プロセスで同一文字列のまま復元。旧54枚は全件実Importer監査で確認し、全54枚のPlayer目視はしていない。RTX5070／D3D11／BC7対応環境。

[開始・終了容量](Results/ScrollMetaFollowup/capacity-after.json)。LogsにはLatestを内包し、単純加算しない。測定JSON自体とその後の少量の証跡書込み・Gitオブジェクトは表の差分外。

|保存先|開始bytes|終了bytes|増減bytes|
|---|---:|---:|---:|
|cat_cafe_sim_tool/generated|3,277,372,585|3,277,372,585|+0|
|Logs|20,087,474,178|20,104,761,801|+17,287,623|
|C:/Users/hi-wa/unity_workspase/CatCafeSim/Library|8,057,188,296|7,800,872,599|-256,315,697|
|Logs/StandaloneBuild/Latest|5,506,514,294|5,506,516,854|+2,560|
|C:/Users/hi-wa/unity_workspase/CatCafeSim/Assets/Resources/CatCafeContent|7,962,384,026|7,962,384,080|+54|
|Logs/StandaloneBuild/Runs|208,708,977|213,357,506|+4,648,529|
|cat_cafe_sim/saves/content_images|1,276,799,229|1,276,799,229|+0|

Latestは156ファイル・成功物1組（約5.507GB）、Pending／Previousなし。Logs増約17.3MBは代表24画面、手動／OS入力記録、専用保存、ビルドログ。Runs増約4.65MBは入力診断の更新を含む通常入口4回。同じLatestを順に置換した。Libraryは約256.3MB減で、Unity自身の再取り込み・ビルド後の既存キャッシュ変化。CatCafeContentの54 bytes増はqualityの50→100による54meta各1 byte。元PNG・生成原本・採用content_imagesは増減0、素材や実セーブの容量整理はしていない。


## 残件

依頼された両解像度の本文／履歴のホイール・実マウスドラッグ・末尾到達、および旧54metaの恒久整合と通常ビルドは完了。1024全履歴を末尾から先頭へ戻す手動ログだけは今回未記録で、fixtureの復帰成功と区別する。全条件の自然プレイ・全画像全場面の目視・他GPU/API・長期評価・総合確認一式は今回対象外。過去の完了結果は維持。残り21人の通常文や新素材は制作していない。

Unityコミット：`2a91ef6`（今回対象54metaと検証入口2ファイルのみ）。pushなし。
