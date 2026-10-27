2026-10-28 Unity統合追補：**本資料の採用済み本文は新パックへ収録済み。取り込み待ち0。** 全体で追加176条件362本文、有効435条件1123本文。既存画像・人物・ID対応を保持し、実カタログの全追加条件選択、Editor Canvas、固定Latestでの代表表示、専用セーブの別プロセス再開を限定確認。[統合結果・確認段階・未確認・容量](NarrativeUnityIntegration.md)。以下の「Unity取り込み待ち」「未確認」は制作当時の記録であり、最新状態は統合記録を参照する。自然プレイ全件・総合確認を完了扱いにはしない。

# ミケの通常休憩・低体力の限定校正

2026-10-27 14:24:50 JSTにTool確認登録。Unity取り込み待ち。対象は `cat_response/cat-mike/pause/pause/listless/pause` 1条件のみ。制作・ゲーム猫ID `cat-mike`、画像ID `playtest-mike`。人物はプレビュー／限定fixtureで `player`。人物文の登録・変更はない。

|案|採否|候補ID|変更前|登録本文・理由|
|---|---|---|---|---|
|1|採用・修正|`e1294066bd295216938771aca9937c18`|ミケは動きを止めて静かに佇んだ。|ミケは疲れた様子で、静かに佇んだ。通常休憩の既存案とほぼ同じため、低体力の様子を表す。|
|2|採用・変更なし|`361c2045db6759e2a90028fbd8918c28`|ミケはどこか遠くを見るようにじっとした。|同左。静かな視線の描写で、接触や姿勢を前提にしない。|
|3|採用・修正|`14e25980e0465533bcff03a35c5e1137`|ミケは体力を温存するように動きを最小限にした。|ミケは動きを控え、ゆったりと休んでいた。体力を温存する意図の推測を除く。|

3案とも固有名表記を維持し、プレースホルダーなし。本文・IDは互いに重複しない。プロフィールは穏やかな甘えん坊、機敏さとのんびりした佇まい。休憩画像は座るミケと手前の道具だが、その姿勢や道具は本文の前提にしない。低体力は `StepExchange` で操作前の体力が閾値以下の場合に確定し、直前の接触や回復・好感を保証しない。既存neutral休憩3案と照合した。

開始時の採用276条件に対象はなく、下書き293条件に3案があった。生成候補278ファイル中1ファイル `821ac5c6b9d14df982b7461a53ed98bc.json` が対象と一致し、その3案は既存下書きと同一。重複追加はない。既存Toolの編集欄→下書き保存→登録内容確認→画像付きプレビュー→確認登録→再読込、新ストア・新編集画面で再読込。画像 `pause → pause`、3案の表示・展開を確認。登録後277条件、下書き293条件。対象外採用・下書き、人物台帳、画像対応・manifest・生成候補のハッシュは維持。[登録記録](Results/MikeRestNarratives/registration.json)に前後・ID・プレビューを記録。画像付きプレビューはToolの描画処理で確認したもので、Unityの画面表示ではない。

[限定fixture](Results/MikeRestNarratives/RuntimeFixture.cs)は既存Unity有効カタログ `d4594a5f9a7f483ca16ddfca4ea48486` を読み、対象1条件をメモリだけに追加しrevisionを再計算。既存C#ランタイムの `player_step` で初期体力10、休憩回復1のfixtureを使い3回実行。追加前は3回とも候補IDなしの定型「ミケは体を丸め、ゆっくり息を整えた。」、追加後は3候補のIDと本文を順に選択した。[出力](Results/MikeRestNarratives/runtime-result.txt)。通常の優先順位は変更せず、同条件の専用操作が選ばれる。特別場面の優先や自然プレイの頻度は未検証。これはメモリ上の確認でUnity取り込み・表示ではない。実行は `Logs/MikeRestRuntime/Test.csproj` に保存した既存Runtime参照の.NET fixtureで行った。

採用データはGit管理外の `cat_cafe_sim/saves/content_images/interaction_texts.json`、下書きは `cat_cafe_sim_tool/user_data/interaction_text_drafts.json`。候補IDと本文を上記と登録記録に保存し、[制作スクリプト](../register_mike_rest_narratives.py)は未登録状態から既存Tool経路で再現する（既存対象があれば停止）。今回1条件3本文、高橋27本文、ミケ・ボール16本文はそれぞれUnity取り込み待ち。パック出力・Unity取り込み・ビルドは行っていない。

容量：開始時に実測したLatest 4,841,096,360 bytes、Runs 205,571,909 bytesは終了時も同値。Latestのみ、Pendingなし。終了時Logs 18,354,929,352、generated 3,277,372,585、content_images 1,220,746,618、既存Unity Library 7,100,713,760 bytes。Logs・generated・content_images・Libraryは今回開始時に計測し損ねたため今回の正確な増減は不明。直前の別作業記録値とは差があるが、並行作業を含むため今回の増加と断定しない。今回の.NET fixture生成物はLogsに保持、削除・Library追加・別Unityプロジェクトは行っていない。
