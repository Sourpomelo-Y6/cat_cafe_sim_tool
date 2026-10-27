# ミケ・なでるの特別場面文章

2026-10-27。**Tool確認登録済み・16本文すべてUnity取り込み待ち**。対象4場面8条件×2案＝16本文を追加し、採用403→411条件。除外・保留なし。案3は空欄。

[全文・条件キー・候補ID](Results/MikePetNarratives/registration.json)／[画像付きプレビュー](Results/MikePetNarratives/preview-checks.json)／[混合候補・片側フォールバック](Results/MikePetNarratives/verification.json)／[保持確認](Results/MikePetNarratives/preservation.json)／[容量](Results/MikePetNarratives/capacity.json)／[制作スクリプト](../register_mike_pet_narratives.py)／[限定検証](../verify_mike_pet_narratives.py)。

## 現物照合と保存経路

開始HEAD：Tool `27fae5d`、Docs `7630c30`、Python `5df6a70`、Unity `87ea3c7`。上位フォルダはGit管理外。AGENTS.md・完成範囲・進捗・特別仕様・直前トンネル制作記録、履歴・既存差分を確認。並行の猫画像・台帳・日程・Unityコード・取り込み素材の差分を保持する。

制作／ゲームIDは人物 `player`、猫 `cat-mike`、画像制作ID `playtest-mike`。実台帳の採用プロフィール全文はdraft-review.jsonに記録。プレイヤーは親しみある柔らかな常体、呼称設定 `{cat}` を `{cat_call}` からミケへ展開。一人称・年齢・性別・来歴は創作しない。ミケは短毛三毛、機敏な動きとのんびりした佇まいを併せ持つムードメーカー。

対象の採用・下書き・生成候補は開始時0件。生成278 JSONを照合。通常petの採用75条件・下書き75条件（他人物・猫を含む）を[既存通常照合](Results/MikePetNarratives/existing-normal.json)へ記録。既存の喉鳴らし・身を預ける等を含む通常文は変更対象外。今回16本文は既存特別玩具文の名詞置換でなく、差し出す手・間合い・そばで過ごすひとときを題材にした。

Toolの既存Tk本文欄入力→save_draft→登録内容確認→両欄の画像付きプレビューを利用。保存JSONを直接編集せず、実モデル要求なし。4場面×2案の実画面をcomputer-useで目視校正。プレビュー合成の採用元ラベルは実装上「登録済み」となるが、タイトル・注意書きで未登録下書きと明記。採用ストアに保存済みという意味ではない。

初回の登録クリックは自動承認審査により拒否され、下書き・検証記録をTool `255e223`／Docs `f7df783`へ保存した。その後ユーザーが「今回の8条件・16本文を登録する」と直接承認し、開いていた確認画面の対象限定登録ボタンから登録完了。Toolの既存register→reload→新ストア・新編集画面で8条件の本文・ID一致を確認。対象外下書きの一括登録なし。登録前の画像付き校正で確認した候補ID・本文と、採用内容が全件一致。

[登録結果](Results/MikePetNarratives/registration.json)に8条件・候補ID・本文・採用前後件数・プロフィール・保護ハッシュを保存。登録済みの再確認は `pwsh -NoProfile -File Docs/Results/MikePetNarratives/runtime-check.ps1` と `python verify_mike_pet_narratives.py`。制作スクリプトは対象採用済みなら上書きせず停止する。`--resume-reviewed-drafts` は登録前下書きのみの再開用で、現在の採用状態では不要。

## 条件・成立順序・画像

優先順位は simultaneous → open_up → connect → switch → normal。open_upはopen_up_count増加、同時成立はそれとconnectの同時成立。本文有無は数値・成功判定を変えない。

人物は `speech/player`・`target_cat=cat-mike`、猫は `cat_response/cat-mike`、modeはpet、reactionはany。switchはaction=switch・scene_key=pet__switch、connect／simultaneousはaction=connect・scene_key=pet__connect、open_upはaction=any・scene_key=pet。キー末尾は `/scene=場面名`。下記キーとJSONが正本。

switch／connectの画像はpet（頭上に手を差し出す準備姿勢）。接触開始・継続を本文で断定しない。open_upはpet__open_up（立ち姿、半眼、頭へ添えた手）、同時成立はpet__simultaneous（座り姿、目を閉じた柔らかな表情、頭の手）。採用元パス・SHA256はpreview-checks.jsonへ記録。特定箇所への継続接触、抱き上げ、喉鳴らし、直前操作は保証しない。open_upはdirect／adapt／pause／switchの4操作で成立する操作共通文にする。petにはintense／feintがないため検証対象から除外した（本文条件の除外ではない）。connectの成否と親しさを区別し、同時成立では両方を本文へ反映する。

## 制作本文・候補ID

### switch

|欄・案|本文|候補ID|
|---|---|---|
|人物1|今度はなでてみようか、{cat_call}。|`8937cd31dbc35a6fb741e03127dd6050`|
|人物2|そっと手を近づけてもいいかな。|`ec6c16c065dd5e87a80a37fe54174f2c`|
|猫1|{cat}は差し出された手へ、ぱっと目を向けた。|`578ed31978425364aabe35bb27bbb265`|
|猫2|{cat}は落ち着いた佇まいのまま、頭上の手の様子をうかがった。|`2971458f794753f49b88d49ac91030f3`|

条件キー：`speech/player/pet/switch/any/pet__switch/cat-mike/scene=switch`
条件キー：`cat_response/cat-mike/pet/switch/any/pet__switch/scene=switch`

### connect

|欄・案|本文|候補ID|
|---|---|---|
|人物1|手を近づける間合い、つかめてきたかな。|`590d12addeb25a17b21a3d85f36d586a`|
|人物2|{cat_call}、そっと誘うと伝わるんだね。|`85ee798b9bdb54069fe314ee9fc8f649`|
|猫1|{cat}は差し出された手に注意を向け、その働きかけに応じた。|`93df76c976b95af391af3f000b9d569d`|
|猫2|{cat}は相手の手の動きをとらえ、なでる誘いを受け止めた。|`9738b6c5a2b5535c81bc372d1c0e7cf3`|

条件キー：`speech/player/pet/connect/any/pet__connect/cat-mike/scene=connect`
条件キー：`cat_response/cat-mike/pet/connect/any/pet__connect/scene=connect`

### open_up

|欄・案|本文|候補ID|
|---|---|---|
|人物1|{cat_call}、そばでゆっくり過ごそうか。|`529899bdde07551089de4ab351e05459`|
|人物2|こうして向き合っていると、ほっとするね。|`24bc28e1820856c3b3eeb8ca7b2b4dee`|
|猫1|{cat}は目元をやわらげ、そばの相手へ親しみを見せた。|`aa257d819fba559e9a80c60e2377a21b`|
|猫2|{cat}はのんびりした表情になり、相手とのひとときにくつろいだ様子を見せた。|`d58e52f99ac95f1aa4a1caf359340aa7`|

条件キー：`speech/player/pet/any/any/pet/cat-mike/scene=open_up`
条件キー：`cat_response/cat-mike/pet/any/any/pet/scene=open_up`

### simultaneous

|欄・案|本文|候補ID|
|---|---|---|
|人物1|なでる間合いが合うと、ぐっと近く感じるね。|`edc69df656715e059ea1dd114c198a26`|
|人物2|{cat_call}、手から気持ちも伝わったかな。|`722cb6f2d9f051f4a74abea7adfc4d96`|
|猫1|{cat}は相手の手の働きかけに応じ、気を許したように目を細めた。|`5767d7a96f5155ddb0b25a108a756234`|
|猫2|{cat}はなでる誘いを受け止めると、口元をゆるめて親しげな表情を見せた。|`64a4ebd6bb6b5fd7ac2c7fb8aea01f3d`|

条件キー：`speech/player/pet/connect/any/pet__connect/cat-mike/scene=simultaneous`
条件キー：`cat_response/cat-mike/pet/connect/any/pet__connect/scene=simultaneous`

## 校正・限定検証と未確認範囲

switchの2案は新しい触れ合いの提案／手を近づける許可、猫は機敏な注目／のんびりした観察。connectは手の間合い／控えめな誘いを受け止める反応。open_upはそばで過ごす提案／向き合う安堵、猫は目元／全体のくつろぎ。同時成立は間合いが合う感覚／手を通じた気持ち、猫は応答＋気を許す表情。人物と猫はどの案の組合せでも矛盾しない。恋愛・命令・幼児語・成功の過剰断定を避けた。

- 下書き保存・対象限定確認登録後、新ストア・新編集画面で8条件16本文を再読込して一致。案3は空欄。人物呼称展開後に括弧残りなし。画像付き対プレビュー8組、片側／両側欠落の実描画12組を確認。
- 下書き時の確認後、登録済みデータで読取限定検証を再実施：混合候補16組、片側／両側欠落21組。片側の専用文を維持し、switch/connect/simultaneous欠落欄は既存定型へ復帰。open_up欠落欄は通常文（pauseは休憩、switchは切替）へ戻り、猫欄へ心を開く追記。専用猫文の二重追記なし。欠落状態はメモリ内のみ。既存通常フォールバックの文章品質全体の再校正ではない。
- 既存C#アセンブリを再利用し、下書き時の確認後、登録済み16本文をメモリ内catalogへ設定。Compose＋ApplyCatalogで4場面・open_up4操作・両欄／片側／なし×2巡、56件成功。[実行ログとアセンブリSHA256](Results/MikePetNarratives/runtime-check.log)。初回はトンネル用6操作を流用して到達しないpet/intenseで拒否されたため、現物のActions・MODE_ACTIONSから4操作へ修正。ゲームソースの変更なし。これは確定済み場面の選択fixtureで、StepExchange・自然プレイ・Unity画面・配布版・現行Unityバイナリ同一性の確認ではない。
- 既存特別4テスト：[初回](Results/MikePetNarratives/tests-special.log)は2成功・GUI1スキップ・一時draft.replaceのWinError 5が1件。前回の通常一式と同じ一時保存エラー。作業フォルダ内TEMP/TMPで[非GUI3テスト成功](Results/MikePetNarratives/tests-special-workspace-temp.log)（0.712秒）、CAT_CAFE_TEST_GUI=1で[GUI1件のみ成功](Results/MikePetNarratives/tests-special-gui.log)（1.074秒）。4件すべて成功。通常ロジックを変更しないため通常68件・総合一式を無条件再実行しなかった。
- 開始時採用403条件の全フィールド・候補ID・本文、対象外下書き、プロフィール・manifest・画像対応・生成278 JSONのハッシュを保持。実採用画像のハッシュ一致。

採用データはGit管理外 `C:/Users/hi-wa/source/codex_repos/CatCafeSim/cat_cafe_sim/saves/content_images/interaction_texts.json`（今回8条件16本文追加）、下書きは `cat_cafe_sim_tool/user_data/interaction_text_drafts.json`。開始時全文はローカル `Logs/MikePetNarratives/before.json`、再現可能な本文・キー・候補ID・スクリプトはTool Gitに保存。

確認済みはTool確認登録・保存再読込・画像付きプレビュー・Python選択・既存C#アセンブリのメモリ内fixture。**16本文すべてUnity取り込み待ち**。Unity画面・配布版表示・自然到達は未確認。素材パック書き出し・Unity取り込み・ビルド・画像生成・ゲームルール数値変更・長期評価は実施していない。

## 容量

開始・終了の全作業実測は[capacity.json](Results/MikePetNarratives/capacity.json)、承認後の登録作業のみの実測は[registration-capacity.json](Results/MikePetNarratives/registration-capacity.json)。Logsに開始スナップショット、Toolに本文・下書きと制作／検証記録を追加。generated・Latest・Libraryは増減0。固定Latestは4,841,096,360 bytesの成功1組、Pendingなし。ビルドZIP・Unityコピー・別Library追加なし。素材・セーブ・並行成果物の削除なし。大きな容量増加なし。

|保存先|開始bytes|終了bytes|増減bytes|
|---|---:|---:|---:|
|Logs|18,360,039,244|18,360,843,674|+804,430|
|generated|3,277,372,585|3,277,372,585|+0|
|content_images|1,276,773,028|1,276,779,522|+6,494|
|Latest|4,841,096,360|4,841,096,360|+0|
|Library|7,358,000,371|7,358,000,371|+0|
|drafts|356,158|362,652|+6,494|
