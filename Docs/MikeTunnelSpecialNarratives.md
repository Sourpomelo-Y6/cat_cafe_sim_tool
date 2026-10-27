# ミケ・トンネル遊びの特別場面文章

2026-10-27。**Tool確認登録済み・16本文すべてUnity取り込み待ち**。プレイヤー＋ミケの4場面8条件に各2案。対象の採用・下書き・生成候補は0件で、不足8条件のみ制作。案3は空欄。除外・保留なし。

[登録全文・条件キー・候補ID・プロフィール](Results/MikeTunnelNarratives/registration.json)／[既存通常文照合](Results/MikeTunnelNarratives/existing-normal.json)／[画像付きプレビュー](Results/MikeTunnelNarratives/preview-checks.json)／[追加検証](Results/MikeTunnelNarratives/verification.json)／[容量](Results/MikeTunnelNarratives/capacity.json)／[再現用制作スクリプト](../register_mike_tunnel_narratives.py)／[読取限定検証](../verify_mike_tunnel_narratives.py)。

## 照合と保存経路

開始HEADはTool `d5172eb`、Docs `17bf549`、Python `5df6a70`、Unity `87ea3c7`。上位フォルダはGit管理外。AGENTS.md・完成範囲・進捗・特別仕様・ぬいぐるみ制作記録と履歴・未コミット差分を確認。猫画像・人物台帳・日程・Unityコード／取り込み差分は保持し、今回のファイルだけコミットする。

開始時採用395条件、下書き411条件、生成278 JSON。対象の特別8条件はいずれも未登録・下書きなし・生成候補なし。通常tunnel文は採用34条件・下書き34条件（他人物・猫を含む）を照合記録へ保存し、本文・IDを変更しない。ミケ通常の侵入等を含む既存表現は今回の制作対象外。

制作ID・ゲームID：人物 `player`、猫 `cat-mike`。画像制作ID `playtest-mike`。実台帳のプレイヤーは親しみのある柔らかな常体、呼称設定 `{cat}` を本文の `{cat_call}` からミケへ展開。一人称・年齢・性別・来歴を加えない。ミケは三毛短毛、機敏さとのんびりした佇まいを併せ持つムードメーカー。採用プロフィール全文を登録記録に残した。

既存Tk本文欄入力→下書き保存→登録内容確認→両欄の画像付きプレビュー→今回8条件のみ確認登録→再読込→新ストア・新編集画面で再開。Toolの既存処理を利用し、保存JSONの直接編集はしていない。実モデル要求なし。再現スクリプトは既存対象があれば停止する。対象外下書きの一括登録なし。

下書き合成プレビューの採用元ラベルはTool実装上「登録済み」となるが、タイトル・注意書きは未登録下書きと明記。computer-useで4場面×2案を前面に表示して画像・本文を目視し、対象限定登録ボタンを押した。画面PNGは追加保存していない。

## 成立条件・優先順位・画像

全条件 `mode=tunnel / reaction=any`。人物 `speech / player / target_cat=cat-mike`、猫 `cat_response / cat-mike`。下表のscene_keyは本文条件であり、画像選択キーとは別。

|場面|action / scene_key|成立・焦点|画像解決キー・目視|
|---|---|---|---|
|switch|switch / tunnel__switch|切替先がtunnel。前の動作を限定せず丸い入口への注意。|tunnel：入口の横に座り、丸い目|
|connect|connect / tunnel__connect|心をつかむ操作の成立。入口での誘い・間合いへの応答。心を開いたとは断定しない。|tunnel：基本画像|
|open_up|any / tunnel|実open_up_count増加。操作共通。休憩・切替でも成立する穏やかな親しみ。|tunnel__open_up：入口に身を置き、穏やかなまなざし|
|simultaneous|connect / tunnel__connect|connectとopen_up_count増加の同時成立。合図への応答と親しげな表情。|tunnel__simultaneous：入口で目を細め、口元をゆるめた姿|

`CafeNarratives.Observe`は同時成立→心を開く→心をつかむ→切替→通常の順。switchにopen_upが重なればopen_upを選ぶ。StepExchangeの心をつかむはテンション任意／強制条件、心を開くはengagementとtension・残ターン・低体力等の判定。本文の有無はルール・数値を変えない。open_upはdirect/adapt/intense/feint/pause/switchの6操作で共通候補を使う。

Docs原画3枚とTool実採用PNGの画像付き表示を確認。実採用パス・解決キー・SHA-256はpreview-checksへ記録。特別画像は入口に身を置くが、侵入・通過・出口からの登場・追走成功を本文で追加断定しない。開口を鳴き声にしない。画像・対応の変更なし。

## 登録本文

各場面は人物2案×猫2案の独立選択。候補の混合でも移動・接触の成否が食い違わない。

### switch

|欄・案|本文|候補ID|
|---|---|---|
|人物1|今度はトンネルで遊ぼうか、{cat_call}。|`c471c4d6077a57468d4d182f7e24a0b4`|
|人物2|丸い入口があるよ。どんな遊びにしようか。|`bb16fb9e1e4750ea9eb3910e218bc3c9`|
|猫1|{cat}はトンネルの丸い入口へ、興味をひかれたように目を向けた。|`5261301cf95d5772a532d6e3cb6718c4`|
|猫2|{cat}は新しく出てきたトンネルのそばで、じっと様子をうかがった。|`fa6e293fb800588c993a05aa8b3bf971`|

### connect

|欄・案|本文|候補ID|
|---|---|---|
|人物1|{cat_call}、入口で誘う間合い、つかめてきたかな。|`3eb65bb7ddf6533ba9dbce5b485b22a3`|
|人物2|トンネル遊びの誘い、うまく届いたみたいだね。|`af5280eff7005cef854493140e8cb1ee`|
|猫1|{cat}はトンネルの入口に注意を向けながら、相手の誘いに応じた。|`745f01b446305429ad4fe058077f46fc`|
|猫2|{cat}はトンネルを前に、相手の働きかけを遊びの合図として受け止めた。|`a36062b0d6965d429067bb6a4ce5b015`|

### open_up

|欄・案|本文|候補ID|
|---|---|---|
|人物1|{cat_call}、トンネルのそばで過ごすのもいいね。|`50adada41bd65cb3941056aaf31ffee9`|
|人物2|入口のそばにいると、のんびりしたくなるね。|`97dcfff20ee95f82baa8fd8fd497a923`|
|猫1|{cat}はトンネルのそばでまなざしを和らげ、相手へ親しみを見せた。|`b8212d6279445e4ea91e04ff3eab6808`|
|猫2|{cat}はトンネルと過ごすひとときに表情をゆるめ、穏やかな目を相手へ向けた。|`d11cc7fc923c590ea0ce3e88a19da9fa`|

### simultaneous

|欄・案|本文|候補ID|
|---|---|---|
|人物1|トンネルで息が合って、距離も縮まった気がするね。|`60a21f558e045673ae3face57bcf7407`|
|人物2|{cat_call}、入口での誘いが通じて、うれしいね。|`502ac548523b55588685245131b3cba1`|
|猫1|{cat}はトンネル遊びの誘いに応じると、目を細めて親しげな表情を見せた。|`f23a455c9d205c6490d0c2a40ee9e8d2`|
|猫2|{cat}はトンネルを前に相手の合図を受け止め、口元までやわらげた。|`40c2f5ae2a255efb8c1e4f23834899c0`|

## 校正・検証結果と限界

switchは丸い入口への注目と新しい遊びの提案、connectは入口で誘う間合いと合図の受け止め、open_upは入口付近で過ごす落ち着き、simultaneousは遊びが通じた感覚と親しみを焦点にした。名詞置換だけにせず、トンネルの空間と間合いを題材にし、各2案の着眼点を変えた。人物は短い柔らかな問いかけ・感想、ミケは注目する目とのんびりした表情で個性を表す。命令・幼児語・心情の過剰な断定を避ける。

- 8条件の保存・再読込・新編集画面で一致。16非空候補の本文／IDに重複なし。案3は選択対象外。呼称展開後に未展開括弧なし。open_upの6操作で両欄の候補IDが一致。
- Tool実描画：8組の対プレビュー、12組の片側／両側欠落。読取限定追加確認：混合候補16組、フォールバック27組。専用欄は維持、open_up欠落欄は通常文へ戻り猫欄に追記、pauseは休憩、switchは切替＋追記。他場面は既存定型。専用猫文への二重追記なし。欠落状態はメモリ内のみ。
- 開始時既存採用395条件の全フィールド・候補ID・本文、対象外下書き、プロフィール、manifest、画像対応、生成278 JSONを保持。実採用画像ハッシュも一致。
- 特別場面の既存4テスト（GUI含む）成功、1.857秒。[ログ](Results/MikeTunnelNarratives/tests-special.log)。通常68テストは初回67成功／一時settings置換のWinError 5が1件（46.039秒）、全体再実行65成功／同じ一時保存エラー3件（44.313秒）。両回でエラーとなった計4テストを個別再実行し、4件すべて成功（6.082秒）。全68テストが少なくとも一度成功したが、一括実行は一時保存の不安定さが残る。[初回](Results/MikeTunnelNarratives/tests-normal.log)／[再実行](Results/MikeTunnelNarratives/tests-normal-retry.log)／[4件限定再確認](Results/MikeTunnelNarratives/tests-normal-isolated.log)。Tk終了後ThemeChanged警告はテストassertion失敗と区別する。
- 既存C#検証アセンブリを再利用し、登録16本文をメモリ内catalogへ設定。Compose＋ApplyCatalogで4場面・open_up6操作・両欄／片側／なし×2巡の72件成功。ID・本文・呼称展開・定型復帰・二重追記防止を確認。[再現PS](Results/MikeTunnelNarratives/runtime-check.ps1)／[実行ログ・アセンブリSHA-256](Results/MikeTunnelNarratives/runtime-check.log)。これは確定済み場面の選択fixtureで、StepExchangeの自然到達・Observeのゲーム進行・現行Unityバイナリ同一性を保証しない。

採用本文はGit管理外 `C:/Users/hi-wa/source/codex_repos/CatCafeSim/cat_cafe_sim/saves/content_images/interaction_texts.json`、下書きはTool `user_data/interaction_text_drafts.json`。本文・条件キー・候補IDと再現スクリプトはTool Git管理。開始時全文スナップショットはローカル `Logs/MikeTunnelNarratives/before.json`。

**全16本文はUnity取り込み待ち**。確認済みはTool登録・画像付きプレビュー・Python選択・既存C#アセンブリでのメモリ内選択fixture。Unity画面・配布版表示・自然プレイでの到達／候補表示は未確認。パック書き出し・Unity取り込み・ゲームビルド・画像生成・ゲームルール変更・長期評価は実施していない。

## 容量

開始・終了実測と増減は[capacity.json](Results/MikeTunnelNarratives/capacity.json)。今回の追加は開始時本文／下書きスナップショット、16本文と下書き、制作／検証記録。固定Latest成功ビルド1組を保持し、Pendingなし。別ビルド・ZIP・Unityコピー・Library追加なし。素材・セーブ・並行成果物の削除なし。

|保存先|開始bytes|終了bytes|増減bytes|
|---|---:|---:|---:|
|Logs|18,359,249,122|18,360,039,244|+790,122|
|generated|3,277,372,585|3,277,372,585|+0|
|content_images|1,276,766,330|1,276,773,028|+6,698|
|Latest|4,841,096,360|4,841,096,360|+0|
|Library|7,358,000,371|7,358,000,371|+0|
|drafts|349,460|356,158|+6,698|

Logs増加は開始時スナップショット790,122 bytesを主とする。既存固定Latestは4,841,096,360 bytesの1組、Pendingなし。generated・Latest・Library増減0。大きな容量増加なし。
