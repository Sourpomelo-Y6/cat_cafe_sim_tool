2026-10-28 Unity統合追補：**本資料の採用済み本文は新パックへ収録済み。取り込み待ち0。** 全体で追加176条件362本文、有効435条件1123本文。既存画像・人物・ID対応を保持し、実カタログの全追加条件選択、Editor Canvas、固定Latestでの代表表示、専用セーブの別プロセス再開を限定確認。[統合結果・確認段階・未確認・容量](NarrativeUnityIntegration.md)。以下の「Unity取り込み待ち」「未確認」は制作当時の記録であり、最新状態は統合記録を参照する。自然プレイ全件・総合確認を完了扱いにはしない。

# ミケ・見守りの特別場面文章と標準案の制作完了

2026-10-28。**プレイヤー＋ミケ・見守り（presence）4場面8条件×2案＝16本文をTool確認登録。全16本文はUnity取り込み待ち。** 採用427→435条件、全体1123非空本文。対象の既存特別文・下書き・生成候補は0で、今回の除外・保留はない。第3案は空欄。これを標準案の最後の特別動作制作とし、対象外の文章制作は開始しない。

[全文・条件キー・候補ID](Results/MikePresenceNarratives/registration.json)／[画像付き8組](Results/MikePresenceNarratives/preview-checks.json)／[混合候補・片側フォールバック](Results/MikePresenceNarratives/verification.json)／[保持](Results/MikePresenceNarratives/preservation.json)／[標準案と有効Unityパックの照合](Results/MikePresenceNarratives/completion-audit.json)／[容量](Results/MikePresenceNarratives/capacity.json)。

## 現物照合・ID・プロフィール

開始HEAD：Tool `d0d96ed`、Docs `a96d8ba`、Python `5df6a70`、Unity `87ea3c7`。上位フォルダはGit管理外。両AGENTS.md、完成範囲、進捗、特別文仕様、直前の声かけ制作、各リポジトリ最新履歴・未コミット差分を確認した。並行差分349ファイルは開始時ハッシュで保持（workspace-before.jsonの350件は今回のcapacity-start.jsonも1件含む）。

人物の制作・ゲームIDは `player`、猫の制作・ゲームIDは `cat-mike`、画像IDは `playtest-mike`。採用プロフィール全文は[draft-review.json](Results/MikePresenceNarratives/draft-review.json)。プレイヤーは親しみある柔らかな常体、呼称 `{cat_call}` → 採用call `{cat}` → ミケ。一人称・年齢・性別・職業・来歴を補わない。ミケは短毛三毛、機敏でのんびりした佇まいのムードメーカー。視線の素早さと座ったままの落ち着き、気を許した表情を使う。

採用・下書きに対象特別条件なし。生成済み278 JSONの条件照合も対象特別候補0。[既存通常文](Results/MikePresenceNarratives/existing-normal.json)にはpresence通常文を記録し、旧本文・候補IDは変更しない。通常文の不自然な表現やフォールバック全文の再校正は今回の範囲に加えない。

## 人物欄の意味と無言の動作

既存 `compose_preview` とC# `Compose`／`ApplyCatalog` は人物欄を `プレイヤー「本文」` として表示する。内心・無言の行動用の別種別や表示分岐はない。採用プロフィールも「短いセリフだけ、かぎ括弧・話者名・行動文は付けない」とする。既存presenceの通常人物文も見守る提案の発話であり、人物欄を内心と読み替える根拠はない。

この既存表現に合わせ、switchは静かに過ごす前の短い提案、他場面は静かな交流を振り返る短い感想とする。「ずっと無言だった」「一言も話さない」とは書かず、見守る行動の静けさと一言の発話は両立する。人物欄の発話を猫の反応の原因にせず、猫文はすべてそばの気配・相手への視線を根拠とする。連続した呼びかけや声の調整を見守りの操作として混ぜない。内心記法の新設・表示仕様変更は不要で、保留なし。

## 成立条件・優先順位・画像

優先順位は simultaneous → open_up → connect → switch → normal。実行時はopen_up_countが増えた結果をopen_upとし、connect操作と重なればsimultaneous。connectは注意を向ける働きかけの成立であり、単独では気を許したと断定しない。open_upはengagement等による確定結果で、休憩・切替でも発生し得る。本文は判定・数値・乱数を変えない。今回fixtureは確定場面での選択確認であり、自然到達の再検証ではない。

全条件はmode=presence、reaction=any。人物はspeech/player、target_cat=cat-mike。猫はcat_response/cat-mike。switchはaction=switch／scene_key=presence__switch、connectとsimultaneousはaction=connect／scene_key=presence__connect。open_upはaction=any／scene_key=presenceで、direct・adapt・pause・switchに共通。presenceにintense／feintはない。

|場面|採用画像キー・ファイル|画像と本文の対応|
|---|---|---|
|switch／connect|presence／`84005b2a4ce24bc2a344cc4ff7e5ceb9.png`|ちょこんと座るミケと開いた目。気配への注意・視線を描き、接近を要求しない|
|open_up|presence__open_up／`df37b5ffbfd44a4285392bbec18ebe2a.png`|しっぽを立てた歩み寄り・柔らかな目。自発的な親しみとして描く|
|simultaneous|presence__simultaneous／`6410c3696b734d22b814418971cd77de.png`|座った姿・閉じた目・緩んだ口元。相手への注意と親しみを併記する|

画像source_path・SHA256はpreview-checks.jsonに記録。switch／connectは専用本文条件キーから通常presence画像へ解決する。画像の開口を鳴き声と決めつけない。玩具・接触・喉鳴らし・直前の動作は保証しない。

## 本文・条件キー・候補ID

### switch

|欄・案|採用本文|候補ID|
|---|---|---|
|人物1|今度は、そばでゆっくり過ごそうか。|`ee361b9139f358f28c38206ab68f5d31`|
|人物2|{cat_call}の様子を、少し見守っていようかな。|`7d26d899b7de58b6bdc023713f18f830`|
|猫1|{cat}はそばにいる相手へ、ぱっと目を向けた。|`8cf2dc66b5f857c2b87436110259ffca`|
|猫2|{cat}はちょこんと座ったまま、相手の気配をうかがった。|`f0928df1858151a7a6b98bf38c7dff1d`|

条件キー：`speech/player/presence/switch/any/presence__switch/cat-mike/scene=switch`

条件キー：`cat_response/cat-mike/presence/switch/any/presence__switch/scene=switch`

### connect

|欄・案|採用本文|候補ID|
|---|---|---|
|人物1|この距離だと、目が合うね。|`f02e949c954f5d4d841dd3b4dba9ee8c`|
|人物2|そばにいると、{cat_call}も気になるのかな。|`62b6f3d479e157409357b929b69d0704`|
|猫1|{cat}は見守る相手に目を向け、じっとその様子を見つめた。|`e8cb52f534f15cdab90519c30f446f62`|
|猫2|そばの気配を追うように、{cat}の注意が相手へ向いた。|`f4d3408a18be5a689aff30c5188442e0`|

条件キー：`speech/player/presence/connect/any/presence__connect/cat-mike/scene=connect`

条件キー：`cat_response/cat-mike/presence/connect/any/presence__connect/scene=connect`

### open_up

|欄・案|採用本文|候補ID|
|---|---|---|
|人物1|こうしてそばにいられるの、うれしいな。|`48afb692983150f1adcbad4d03c17afe`|
|人物2|{cat_call}と過ごす時間は、ほっとするね。|`40047d0ae9d45b3298ca5ed4213b408f`|
|猫1|{cat}はしっぽを立て、気を許したような顔で相手へ歩み寄った。|`e26c4632d0945c4c837dc34966d54dcc`|
|猫2|そばの相手へ、{cat}は親しげなまなざしを向けた。|`6fc61cb7fc8e520fbc14c03cfd6a5cc4`|

条件キー：`speech/player/presence/any/any/presence/cat-mike/scene=open_up`

条件キー：`cat_response/cat-mike/presence/any/any/presence/scene=open_up`

### simultaneous

|欄・案|採用本文|候補ID|
|---|---|---|
|人物1|見守っていたら、{cat_call}がぐっと身近に感じられたよ。|`f6ed3b8a7ec9536e855c7c3be3f2b8b3`|
|人物2|そばでこんな顔を見られると、うれしくなるね。|`44fa714d9af35031bc61bbfe53de7430`|
|猫1|{cat}は見守る相手へ顔を向け、気を許すように目を細めた。|`411851e0e06152bcb86e2436e329fd39`|
|猫2|相手の気配に注意を向けた{cat}は、うれしそうに口元をゆるめた。|`56884c116ce754f0b0a4e3b34f846e1b`|

条件キー：`speech/player/presence/connect/any/presence__connect/cat-mike/scene=simultaneous`

条件キー：`cat_response/cat-mike/presence/connect/any/presence__connect/scene=simultaneous`


## 校正理由

switch：人物は「一緒に過ごす提案／様子を見守る選択」、猫は「ぱっと視線を向ける機敏さ／座ったまま気配をうかがう落ち着き」。直前の道具・動作を指定せず、見守りへの切替として成立させた。

connect：人物は「目が合う距離への気づき／相手からも関心を寄せられた推測」、猫は「じっと見る視線／注意の向き」。親密化・接触・声への返答を断定しない。「目が合う」は一瞬の視線の交わりで、見つめ続ける要求ではない。

open_up：人物は「共にいられる喜び／穏やかな時間への安堵」、猫は「自分からの歩み寄り／親しげなまなざし」。見守る操作を今も続けているとは書かず、休憩・切替にも成立する。人物の休憩と猫の自発的な歩み寄りは矛盾しない。

simultaneous：人物は「見守るうちに感じた身近さ／そばで見られた表情の喜び」、猫は「相手への顔の向き＋気を許した目元／気配への注意＋うれしそうな口元」。connectの注意とopen_upの親しみを同じ猫文に含めた。

各場面で人物2案×猫2案の全4組を校正。特定のセリフへの返答、前後関係の食い違い、命令・幼児語・恋愛・所有の断定を避けた。各案は語尾置換ではなく、時間・距離・視線・表情へ焦点を分けた。

## Tool経路・保存と限定検証

[制作スクリプト](../register_mike_presence_narratives.py)は既存Tk本文欄への入力→save_draft→登録内容確認→画像付き両欄プレビューを使用。computer-useで4場面×2案を実画面で目視後、画面の「今回の8条件だけ登録」を操作し、既存register→reload→新ストア・新編集画面で再読み込みした。保存JSONを独自形式で直接編集しない。対象外下書きの一括登録なし。プレビューのメモリ合成では採用元表示が「登録済み」になるが、タイトルと注意欄で未登録下書きと明示し、登録結果と区別した。

- 保存再読み込みで8条件16本文・候補ID・空き案3の選択除外・呼称展開・場面別選択が一致。登録前画像付き8組、片側／両側欠落の実描画12組を確認。
- [読取限定検証](../verify_mike_presence_narratives.py)：混合候補16組、片側／両側欠落21組成功。欠落はメモリ上だけで再現し保存しない。専用文のある片側は維持し、switch／connect／simultaneousは欠落欄を定型へ戻す。open_upは通常文、pauseは休憩、switchは切替定型へ戻り、猫欄に心を開いた追記。専用猫文への二重追記なし。
- [C#実行ログ](Results/MikePresenceNarratives/runtime-check.log)：既存 `Logs/MikeRestRuntime/bin/Debug/net8.0/Test.dll` を再利用し、Compose＋ApplyCatalogに登録本文をメモリ内で追加。4場面・open_up4操作・両欄／人物のみ／猫のみ／なし×2巡、**56件成功**。アセンブリSHA256もログへ保存。再コンパイル・StepExchange・Unity画面の確認ではない。
- [既存特別文テスト](Results/MikePresenceNarratives/tests-special.log)：GUI有効化、TEMP／TMPを今回Logs下へ限定し、**4/4成功（1.934秒）**。通常ロジックを変更しておらず、通常全件・総合一式・長期評価は繰り返さない。
- 開始時採用427条件と対象外下書き443条件の全フィールド・本文・候補ID、プロフィール・manifest・画像対応・生成278 JSON・採用画像SHA256、並行差分349ファイルを保持。

採用JSONはGit管理外 `C:/Users/hi-wa/source/codex_repos/CatCafeSim/cat_cafe_sim/saves/content_images/interaction_texts.json`、下書きはGit管理外 `C:/Users/hi-wa/source/codex_repos/CatCafeSim/cat_cafe_sim_tool/user_data/interaction_text_drafts.json`。開始時全文は `Logs/MikePresenceNarratives/before.json`。Tool Gitに本文・条件・候補ID・再現用スクリプトと確認記録を保存。制作スクリプトは対象採用済みなら上書きせず停止する。`--resume-reviewed-drafts` は登録前の完全一致下書きのみを再開するためのもので、現在は使わない。

再確認：Toolルートで `python verify_mike_presence_narratives.py` と `pwsh -NoProfile -File Docs/Results/MikePresenceNarratives/runtime-check.ps1`。

## 標準案の最終照合・Unityへの引き渡し

採用JSONと最新制作記録、有効Unityカタログ `d4594a5f9a7f483ca16ddfca4ea48486` の候補ID・本文を照合した。初期5猫の通常文は、現行採用設定で必要な不足分の制作が完了。初期計画の173条件は上限概算であり、到達性に基づく除外を未制作残件に数えない。

|通常文|今回までの追加採用・取り込み待ち|初期不足枠からの除外|状態・根拠|
|---|---:|---:|---|
|ミケ|1条件3本文|0|休憩listlessの既存3案を校正・採用。[記録](MikeRestNarratives.md)|
|ソラ|19条件38本文|17|ボール4、ぬいぐるみ3、残4動作12。[ボール](SoraBallNormalNarratives.md)・[ぬいぐるみ](SoraPlushNormalNarratives.md)・[残4動作](SoraRemainingNormalNarratives.md)の到達性による|
|タマ|22条件44本文|14|好みと閾値から到達対象22。[記録](TamaNormalNarratives.md)|
|コハク|34条件68本文|16|通常8動作＋休憩の到達対象34。[記録](KohakuNormalNarratives.md)|
|ムギ|35条件70本文|15|通常8動作＋休憩の到達対象35。[記録](MugiNormalNarratives.md)|
|合計|111条件223本文|62|制作残件0・保留0、全追加本文はUnity取り込み待ち|

除外は現行設定で拒否が出ない、得点上限が通常／好意／熱心の閾値に届かない等の各記録の根拠による。旧設定・将来の好み変更まで保証しない。既存登録に到達しない条件があっても削除・再制作しない。採用全通常条件数はミケ84、ソラ66、タマ68、コハク34、ムギ35であり、表の「不足分追加数」と混同しない。

プレイヤー＋ミケの特別8動作は各4場面×2欄の**64条件120本文が採用済み**。ねこじゃらし8条件8本文は既存Unity有効パック収録（各1案で採用済みのため増案しない）。ボール・ぬいぐるみ・トンネル・なでる・ブラッシング・声かけ・今回見守りの7動作は各8条件16本文、合計**56条件112本文がUnity取り込み待ち**。特別文の制作残件・除外・保留は0。

標準案のUnity取り込み待ちは通常111条件223本文＋特別56条件112本文＝**167条件335本文**。既存の高橋9条件27本文も含めた全採用データと有効パックの差は**176条件362本文**（Tool435条件1123本文／Unity259条件761本文）。completion-audit.jsonには全待機条件と候補ID・本文を保存した。

**制作範囲をここで閉じ、上記をUnity統合へ引き渡す。** 統合作業では既存候補ID・プロフィール・画像対応を保持し、通常文と特別文を採用データから取り込む。今回確認済みはTool登録・プレビュー・Python選択・C#メモリ内fixtureであり、今回本文のUnity画面・配布版表示・自然プレイは未確認。統合後の実表示・保存履歴等の確認を別工程とする。今回、素材パック書き出し・Unity取り込み・ビルド・画像生成・他対象制作・ルール数値変更はしない。残り21人物の通常文、他猫の特別文、後加入猫等は標準案の必須外で、着手指示待ちを維持する。

## 容量

|保存先|開始bytes|終了bytes|増減bytes|
|---|---:|---:|---:|
|Logs|18,362,532,258|18,363,385,760|+853,502|
|generated|3,277,372,585|3,277,372,585|+0|
|content_images|1,276,792,688|1,276,799,229|+6,541|
|Latest|4,841,096,360|4,841,096,360|+0|
|Library|7,358,000,371|7,358,000,371|+0|
|drafts|375,818|382,359|+6,541|

Logsの増加は開始時本文・下書きスナップショットと今回の調査・検証ログ。採用データと下書きの増加は8条件16本文分。generated／Latest／既存Libraryの増減0。固定StandaloneBuildは成功物Latestの1組、Runsと既存build.lockのみでPendingなし。新規ビルドフォルダ・ZIP・Unityプロジェクト・Library複製は作らず、素材・セーブ・並行成果物の削除はしていない。資料・Gitオブジェクトは容量表の測定対象外。
