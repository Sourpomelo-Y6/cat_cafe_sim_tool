# 開発専用獲得検証：複数ルート収集の拡張

2026-10-30。[初版の起動・保存隔離・操作方法](DeveloperAcquisition.md)の続き。通常おまかせ・ゲーム係数は変更せず、開発専用runnerの収集方策を拡張した。正式公開条件へ追加しない。

## 今回の範囲

新規起動の既定計画は、従来4ルート＋収集5ルートの計9ルート。`collection_routes: true` で追加する。falseなら初版と同じ4ルートのみを選べる。Unity専用define・引数・保存隔離は引き続き必要。

|route_id|通常開始目標|担当する獲得対象|内部確認の終了日/操作数（seed0）|
|---|---|---|---|
|popularity|人気|従来の3段階達成|34日/1438|
|patron|有力者|従来の3人達成・有力者派遣|51日/1915|
|bond|好感度|従来の6匹80達成|13日/1291|
|free|自由|従来の30日営業|31日/1309|
|collect-main|人気|依頼・常連・来訪猫、店猫3、設備5、店舗設備等13|17日/712|
|collect-dispatch|人気|紹介猫2、派遣に不足する特性の募集猫、通常派遣9、用品4|21日/955|
|collect-catalog-1|自由|募集catalog-1～6|7日/270|
|collect-catalog-2|自由|募集catalog-7～12|10日/398|
|collect-catalog-3|自由|募集catalog-13～18|16日/653|

収集ルートは選んだ開始目標のクリアではなく、**そのルートの `assigned_targets` がすべて通常条件で獲得済み**になった場合に終了する。`completion_scope=assigned_collection_targets` を保存する。人気開始の収集ルートが人気の最終目標まで達成したと読み替えない。従来4ルートのクリア条件はそのまま。

複数ルートが同じ開始目標を使うため、`objective` と一意の `route_id` を分離した。保存ディレクトリ・結果・操作ログ・集計・獲得根拠・再開をroute_idで区別する。`pending_objectives` フィールドも未実行route_idの一覧を返す。CSVにはassigned列を追加。

全ルート合算の確認対象は猫31系統、接客設備5種、店舗設備等13項目、用品4種、派遣先12か所。猫のうち初期5系統は通常新規開始時の加入として数え、募集/紹介の個体IDを既存ContentImageIdで系統へ正規化する。単一セーブの在籍数や、画像制作の登録数とは合算しない。

## 通常条件で集める方策

- 主収集と派遣収集、募集猫の担当を分割。募集は既存の候補追加日（3日ごと）と循環18系統を利用し、対象系統に一致する実行時個体を受け入れる。
- 空き枠は通常の飼育設備購入で6→9→12へ拡張する。一回限りの依頼・常連・来訪・紹介に必要な残枠を予約し、店猫/募集猫で先に埋めない。初回の試行で4日目の依頼を店猫による満員で見送ったため、この予約を追加した。ゲームの受入条件は緩和していない。
- 一回限りの担当加入を見送って再到達できなくなった場合は `one_time_admission_declined/<id>` で未到達停止し、150日まで無意味に続けない。
- 派遣先の特性条件をrulesから読み、初期猫に不足する特性を持つ募集系統を担当へ加える。現在はhardy/hardworking/easygoingが必要。特性・経験・資金を直接書き換えない。
- 購入は通常のReason関数と費用を使い、最低600または3日分の運営費を残す。必要な人気段階を通常営業で達成し、席数拡張は達成翌日以降に行う。
- 派遣は解放・健康・疲労・必要特性・席数を超える担当可能猫を確認し、同じ目的の未帰還案件を重ねない。帰還受領と紹介回答を通常操作で処理する。
- 全ルートは最初から独立した正常状態で開始。受入や用品の履歴から一度獲得と現所持を分ける。資金/XP/フラグの付与、デバッグ全解放は使用しない。

ルート生成は最新rulesの募集数・飼育上限から計算するが、追加ルートを含め12ルートを超える計画は拒否する。派遣特性の通常加入元が見つからない場合も計画不一致として停止する。対象を黙って消して成功率を上げない。

## 実行と互換

[起動スクリプト](../run_dev_acquisition.ps1)は同じ固定Latestを使用する。新規の既定計画にはcollection_routes=trueが入る。任意計画を指定する場合は明示的に同フィールドを設定する。

runnerは版2。実行assemblyと計画・ルール・素材の指紋が変わるため、初版runnerや別ビルドのcheckpointを今回へ自動移行しない。旧runは保存したままとし、新規runで検証する。今回版の同一ビルド内では9ルートの途中保存・再開を利用できる。

```powershell
# 内部通し（新しい出力先）
dotnet run --project cat_cafe_sim_tool/tests/development_acquisition/DevelopmentAcquisitionChecks.csproj -- cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/rules.json Logs/DeveloperAcquisition/<new-dir> collect
# 募集収集2の途中で停止 → 別プロセスで再開
dotnet run --no-build --project cat_cafe_sim_tool/tests/development_acquisition/DevelopmentAcquisitionChecks.csproj -- cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/rules.json Logs/DeveloperAcquisition/<new-dir> collect-pause
dotnet run --no-build --project cat_cafe_sim_tool/tests/development_acquisition/DevelopmentAcquisitionChecks.csproj -- cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/rules.json Logs/DeveloperAcquisition/<same-dir> collect-resume
```

## 検証

- Collection-02：標準ルールseed0の9ルートを完走。各ルート担当の全獲得、5カテゴリの全ルート合算、単一ルートの猫数が12以下、一意なルートと獲得根拠を確認。
- Collection-Checks-03：初版からの基本15確認・別プロセス再開2確認を継続成功。
- Collection-Resume-03：collect-catalog-2の75操作で保存停止→別プロセスで同じルートを復元→残りを完走。9ルートの最終digestが中断なしのCollection-02とすべて一致。従来4ルートの最終digestも前回版Standard-02と一致。

Player確認と最終容量は後記する。今回も回想・画像既読は内部操作から獲得済みとせず、観測対象外として区別する。

## 残る範囲

全専門/習熟の組合せ、排他回答の分岐網羅、会話読了・画像実表示のUI観測、素材スロットの登録不足の細分化、Windows物理入力は後続。今回の5カテゴリの獲得成功を、それらの完了へ読み替えない。多数seedでの達成率やゲームバランスの保証でもない。

## Player・再開・既知の差

Unity 2021.3.45f2で専用Playerのビルド成功。ログは `Logs/StandaloneBuild/Runs/20261030-151545-633-9da356816bf641c2a240d5881ff3ca15.log`。固定Latestを更新し、追加ビルドフォルダーやZIPは作成していない。

Player `Runs/20261030-151734-922f195769d54dbb94e9059e3e2cfd36` はcollect-catalog-2の239操作目で保存停止。別プロセスで再開後3秒間操作ログ不変、明示開始後に9ルート完走・終了0。同じPlayerの中断なし実行 `Runs/20261030-152026-5edd532f7df54250b5e86218d1045529` と、9ルートすべての日数・操作数・最終digestが一致した。5カテゴリも31/5/13/4/12で一致。通常保存先8ファイルは実行前後SHA不変、テンプレートとUnityの今回3ソースはバイト一致。

.NETとPlayerの比較では8ルートの最終digestが一致したが、collect-dispatchは一致しない。復元した状態を既存canonical規約で比較すると、未指定のDispatchWelcomeがUnity JSONでは空オブジェクトとなることによる `specialization_matched` の6フラグが異なる。今回の試行では獲得対象・操作列・費用/報酬等の数値に差はなかったが、意味上の差があるためクロスランタイム完全一致とは報告しない。通常の派遣判定と共有保存仕様の見直しは本変更へ混ぜていない。同一Playerでの保存再開一致は別途上記のとおり確認済み。

画面の見栄え・会話のUI読了・CG実表示・Windows物理入力は未確認。既知の差の詳細はローカル `Logs/DeveloperAcquisition/collection-cross-runtime-differences.json`。

全ルートunion：獲得82、未到達6、観測対象外111。後続の習熟等が未到達として残り、回想/画像観測は対象外のまま。

容量：StandaloneBuild開始5,785,409,471 bytes→終了5786593264 bytes。検証記録全体105915887 bytes（前回終了38,961,183 bytesからの追加はログ/セーブ中心）、Library 7914592941 bytes。Latest/Runsのみ、Pending/Previousなし。ソース・素材・セーブの整理削除、プロジェクト/Library複製なし。
