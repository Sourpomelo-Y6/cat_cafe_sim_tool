2026-10-28 Unity統合追補：**本資料の採用済み本文は新パックへ収録済み。取り込み待ち0。** 全体で追加176条件362本文、有効435条件1123本文。既存画像・人物・ID対応を保持し、実カタログの全追加条件選択、Editor Canvas、固定Latestでの代表表示、専用セーブの別プロセス再開を限定確認。[統合結果・確認段階・未確認・容量](NarrativeUnityIntegration.md)。以下の「Unity取り込み待ち」「未確認」は制作当時の記録であり、最新状態は統合記録を参照する。自然プレイ全件・総合確認を完了扱いにはしない。

# ミケ・ブラッシングの特別場面文章

2026-10-28。**Tool確認登録済み・16本文すべてUnity取り込み待ち**。対象4場面8条件×2案＝16本文を追加し、採用411→419条件。対象の既存特別文・下書き・生成候補がなかったため全8条件を制作。除外・保留なし、案3は空欄。

[全文・条件キー・候補ID](Results/MikeBrushNarratives/registration.json)／[画像付きプレビュー](Results/MikeBrushNarratives/preview-checks.json)／[混合候補・片側フォールバック](Results/MikeBrushNarratives/verification.json)／[保持確認](Results/MikeBrushNarratives/preservation.json)／[容量](Results/MikeBrushNarratives/capacity.json)／[制作スクリプト](../register_mike_brush_narratives.py)／[限定検証](../verify_mike_brush_narratives.py)。

## 現物照合・保存経路

開始HEAD：Tool `1db9029`、Docs `313dd2f`、Python `5df6a70`、Unity `87ea3c7`。上位フォルダはGit管理外。AGENTS.md、完成範囲、進捗、特別仕様、直前の[なでる制作記録](MikePetSpecialNarratives.md)、履歴・差分を参照。並行の猫画像・台帳・日程・設定・Unityコード・取り込み素材の差分を保持し、今回のコミットから除外。

制作／ゲームID：人物 `player`、猫 `cat-mike`、画像制作ID `playtest-mike`。採用プロフィール全文は[draft-review.json](Results/MikeBrushNarratives/draft-review.json)。プレイヤーは親しみある柔らかな常体、呼称設定 `{cat}` を `{cat_call}` からミケへ展開。年齢・性別・一人称・職業・来歴は創作しない。ミケは短毛三毛、機敏でのんびりした佇まいのムードメーカー。

開始時に対象特別文の採用・下書きは各0、生成278 JSON中の対象特別候補0。brush通常文は採用23条件・下書き23条件（他人物・猫を含む）を[既存通常文](Results/MikeBrushNarratives/existing-normal.json)へ記録。通常文にある毛並み・体の動き等の表現を今回の特別文の保証根拠にはせず、既存本文を変更しない。

既存Tk本文欄入力→save_draft→登録内容確認→両欄の画像付きプレビュー→画面の「今回の8条件だけ登録」→register→reload→新ストア・新編集画面で再読込。保存JSONの直接編集・独自保存形式・実モデル要求なし。computer-useスキルで4場面×2案の実画面を目視。プレビュー合成の採用元ラベルは実装上「登録済み」になるが、タイトルと注意書きで未登録下書きと明記し、採用ストアへの保存と区別した。今回の対象限定ボタンによる登録後、プレビューで確認した本文・候補IDと全件一致。対象外下書きの一括登録なし。

再現用スクリプトは対象採用済みなら上書きせず停止する。`--resume-reviewed-drafts` は登録前下書きのみの再開用であり、現在の採用状態では使用しない。再確認はToolルートで `python verify_mike_brush_narratives.py`、`pwsh -NoProfile -File Docs/Results/MikeBrushNarratives/runtime-check.ps1`。

## 成立条件・優先順位・画像

優先順位は simultaneous → open_up → connect → switch → normal。open_up は open_up_count が増えた確定結果、同時成立はその結果とconnectの両方。connectのみの成立では親密化まで断定せず、同時成立で働きかけへの応答と親しみを合わせて描く。本文の有無はゲームの成功判定・数値・乱数を変えない。

人物条件は `speech/player`・`target_cat=cat-mike`、猫条件は `cat_response/cat-mike`。mode=brush、reaction=any。switchはaction=switch・scene_key=brush__switch、connect／simultaneousはaction=connect・scene_key=brush__connect。open_upはaction=any・scene_key=brushで、direct／adapt／pause／switchに共通。brushにはintense／feintがないため限定検証にも追加しない。

|場面|実際の採用画像|画像から扱う範囲|
|---|---|---|
|switch／connect|brush `7ef61a7a3fec44a58041b4d5ee1e8997.png`|座ったミケ、近くに差し出すブラシ。接触開始・継続を断定しない|
|open_up|brush__open_up `45c9b0cada9545ae9139ec592c823a02.png`|立ち姿、柔らかな目元、顔の横にブラシ。操作共通のためブラシの運動や接触継続を本文に要求しない|
|simultaneous|brush__simultaneous `455ba0fda7df4a0592a97ea705e2c9c4.png`|座り姿、閉じた目、開いて緩んだ口元と顔の横のブラシ。応答と親しみを描く|

採用ストアの実画像とTool表示を確認。元パス・SHA256・要求／解決画像キーはpreview-checks.jsonに記録。本文のscene_keyと画像の選択キーは別。抜け毛、毛並み改善、喉鳴らし、ブラシの接触部位、接触継続、直前の動作を保証しない。

## 制作本文・候補ID

### switch

|欄・案|本文|候補ID|
|---|---|---|
|人物1|今度はブラシでお手入れしてみようか、{cat_call}。|`dd1c786b20325478ba3f55854db0ebae`|
|人物2|ブラシを持ってきたよ。ゆっくり始めようか。|`e88d7830cb5852a48d94736744b8eea2`|
|猫1|{cat}はそばに出てきたブラシへ、ぱっと目を向けた。|`3de69494c33f5ee2b52ac954a9b1327e`|
|猫2|{cat}はちょこんと座ったまま、ブラシを持つ手を見つめた。|`43c55e870276522981590105d67e8173`|

条件キー：`speech/player/brush/switch/any/brush__switch/cat-mike/scene=switch`

条件キー：`cat_response/cat-mike/brush/switch/any/brush__switch/scene=switch`

### connect

|欄・案|本文|候補ID|
|---|---|---|
|人物1|ブラシを近づける頃合い、わかってきた気がする。|`76a53cfe91a855fc89be06e8e81db6b8`|
|人物2|{cat_call}、お手入れにも誘う間合いがあるんだね。|`8f61984961795ed0b7eb351262ee1765`|
|猫1|{cat}はブラシを持つ手に注意を向け、お手入れの誘いに応じた。|`6cabcd4c76c951238a71c26f87323e1d`|
|猫2|{cat}は近くのブラシをとらえ、相手の働きかけを受け止めた。|`b346e609eb69520aa87cabae095bcea6`|

条件キー：`speech/player/brush/connect/any/brush__connect/cat-mike/scene=connect`

条件キー：`cat_response/cat-mike/brush/connect/any/brush__connect/scene=connect`

### open_up

|欄・案|本文|候補ID|
|---|---|---|
|人物1|お手入れの時間も、のんびりいこうね。|`b1973d8fba945819b5c926a6ed91d624`|
|人物2|{cat_call}、そんな顔を見せてくれると、うれしいな。|`0c61c2eee958573984101a37d2d3150b`|
|猫1|{cat}はブラシのそばで目元をゆるめ、相手に親しみを見せた。|`b5ab687693f759a9bce445911d037dd0`|
|猫2|{cat}は気を許したような表情で、お手入れのひとときを相手と過ごした。|`e5f3a20459d1586f9a2fb47774a5cddd`|

条件キー：`speech/player/brush/any/any/brush/cat-mike/scene=open_up`

条件キー：`cat_response/cat-mike/brush/any/any/brush/scene=open_up`

### simultaneous

|欄・案|本文|候補ID|
|---|---|---|
|人物1|お手入れの間合いが合うと、ぐっと仲良くなれた気がする。|`5cc3a194a08953ef85da439622d4337c`|
|人物2|{cat_call}、ブラシ越しにも気持ちが伝わったかな。|`42fbced428f659cb9b317095e8a9ecc7`|
|猫1|{cat}はブラシを持つ手の働きかけに応え、うれしそうに目を閉じた。|`ea84fc17b1ca51d0b588925ecc373277`|
|猫2|{cat}はお手入れの誘いを受け止め、相手に気を許すように口元をほころばせた。|`790cbfe5505e5f38a77e92a10bbd4f94`|

条件キー：`speech/player/brush/connect/any/brush__connect/cat-mike/scene=simultaneous`

条件キー：`cat_response/cat-mike/brush/connect/any/brush__connect/scene=simultaneous`

## 校正と限定検証

switchは「ブラシで手入れの提案／持ってきた道具の紹介」に対し、猫は「機敏な注目／ちょこんと座って観察」。前の動作は指定しない。connectは「近づける頃合い／お手入れへ誘う間合い」と、それを受け止める注意の反応。「ブラシをとらえ」は注目の描写で、くわえる・捕まえる等の接触行動を加えない。open_upは「のんびりした時間／表情を見せてもらう喜び」、猫は「目元の親しみ／相手と過ごす安堵」。休憩や切替でも成立し、直前までブラシを動かしていたことを要求しない。同時成立は「手入れの間合いと仲良く感じる気持ち／ブラシ越しに気持ちが通じる実感」、猫は「働きかけへの応答＋嬉しそうな表情／誘いへの応答＋気を許す口元」。

各場面で人物2案×猫2案のどの組合せにも矛盾がない。命令、幼児語、恋愛、手入れの成果、成功の先取りや特定の直前操作を避けた。「なでる」の名詞置換だけにせず、道具の紹介、手入れへ誘う頃合い、ゆったりした時間、ブラシを介した親しみと視点を変えた。

- Toolで8条件16本文の下書き保存・確認登録、新ストア・新編集画面での再読込、候補ID一致、空き案3の除外、呼称展開、場面別選択を確認。画像付き8組と、片側／両側欠落の実描画12組を制作時に確認。
- 登録済みデータで混合候補16組、片側／両側欠落21組を[限定確認](Results/MikeBrushNarratives/verification.json)。欠落はメモリ上だけ。片側の専用文は維持し、switch／connect／simultaneousの欠落欄は既存定型へ復帰。open_upの欠落欄は通常文（pauseは休憩、switchは切替）へ戻り、猫欄だけに心を開く追記。専用猫文の二重追記なし。既存通常フォールバック全文の再校正は対象外。
- 既存C#アセンブリ `Logs/MikeRestRuntime/bin/Debug/net8.0/Test.dll` を再利用。Compose＋ApplyCatalogに登録済み本文をメモリ内で渡し、4場面・open_up4操作・両欄／片側／なし×2巡、**56件成功**。[実行ログ・アセンブリSHA256](Results/MikeBrushNarratives/runtime-check.log)。ゲームソースの変更・再コンパイルなし。これは確定済み場面の選択fixtureであり、StepExchange・自然到達・現行Unityバイナリとの同一性を確認したものではない。
- 既存の特別文4テスト（GUI含む）を **4/4成功、1.957秒**。[ログ](Results/MikeBrushNarratives/tests-special.log)。前回の成功手順を踏まえ、TEMP／TMPは `Logs/MikeBrushNarratives/temp` とした。通常本文ロジックを変更しないため通常68件・総合一式・長期評価は無条件に再実行しない。
- 開始時採用411条件の全フィールド・候補ID・本文、対象外下書き、プロフィール、manifest、画像対応、生成278 JSONのハッシュと採用画像を保持。レビュー時と登録後の本文・候補IDが全件一致。[保持記録](Results/MikeBrushNarratives/preservation.json)。

採用データはGit管理外 `C:/Users/hi-wa/source/codex_repos/CatCafeSim/cat_cafe_sim/saves/content_images/interaction_texts.json`（今回8条件16本文追加）、下書きは `cat_cafe_sim_tool/user_data/interaction_text_drafts.json`。開始時全文はローカル `Logs/MikeBrushNarratives/before.json`。再現可能な本文・条件キー・候補ID・制作スクリプトをTool Gitに保存。

確認済みはTool確認登録・保存再読込・画像付きプレビュー・Python選択・既存C#メモリ内fixture。**全16本文はUnity取り込み待ち**。Unity画面・配布版表示・自然プレイは未確認。素材パック書き出し・Unity取り込み・ビルド・画像生成・ゲームルール数値変更は実施していない。

## 容量

開始／終了実測は[capacity.json](Results/MikeBrushNarratives/capacity.json)。Logs増加は開始スナップショット・検証記録、content_imagesとdrafts増加は8条件16本文の保存による。generated・Latest・Libraryは増減0。固定Latestは成功1組、Pendingなし。新しいビルドフォルダ・ZIP・Unityプロジェクトのコピー・Libraryを作らず、既存素材・セーブ・並行成果物を削除しない。

|保存先|開始bytes|終了bytes|増減bytes|
|---|---:|---:|---:|
|Logs|18,360,843,674|18,361,697,542|+853,868|
|generated|3,277,372,585|3,277,372,585|+0|
|content_images|1,276,779,522|1,276,786,174|+6,652|
|Latest|4,841,096,360|4,841,096,360|+0|
|Library|7,358,000,371|7,358,000,371|+0|
|drafts|362,652|369,304|+6,652|
