2026-10-28 Unity統合追補：**本資料の採用済み本文は新パックへ収録済み。取り込み待ち0。** 全体で追加176条件362本文、有効435条件1123本文。既存画像・人物・ID対応を保持し、実カタログの全追加条件選択、Editor Canvas、固定Latestでの代表表示、専用セーブの別プロセス再開を限定確認。[統合結果・確認段階・未確認・容量](NarrativeUnityIntegration.md)。以下の「Unity取り込み待ち」「未確認」は制作当時の記録であり、最新状態は統合記録を参照する。自然プレイ全件・総合確認を完了扱いにはしない。

# ミケ・声かけの特別場面文章

2026-10-28。**Tool確認登録済み・全16本文はUnity取り込み待ち**。プレイヤー＋ミケの声かけ（voice）4場面8条件×2案＝16本文を追加し、採用419→427条件。対象の既存特別文・下書き・生成候補は0のため全8条件を制作。除外・保留なし。案3は空欄。

[全文・条件キー・候補ID](Results/MikeVoiceNarratives/registration.json)／[画像付きプレビュー](Results/MikeVoiceNarratives/preview-checks.json)／[混合候補・片側フォールバック](Results/MikeVoiceNarratives/verification.json)／[保持確認](Results/MikeVoiceNarratives/preservation.json)／[容量](Results/MikeVoiceNarratives/capacity.json)／[制作スクリプト](../register_mike_voice_narratives.py)／[限定検証](../verify_mike_voice_narratives.py)。

## 現物照合とTool保存経路

開始HEADはTool `913350d`、Docs `8c29f61`、Python `5df6a70`、Unity `87ea3c7`。上位フォルダはGit管理外。AGENTS.md、完成範囲、進捗、特別文仕様と直前の[ブラッシング制作](MikeBrushSpecialNarratives.md)、最新履歴と差分を確認。並行の猫画像・台帳・日程・設定・Unityコード・取り込み素材の差分は開始時の[ハッシュ記録](Results/MikeVoiceNarratives/workspace-before.json)と照合して保持し、今回のコミットから除外した。

人物の制作・ゲームIDは `player`、猫は `cat-mike`、画像制作IDは `playtest-mike`。採用プロフィール全文は[draft-review.json](Results/MikeVoiceNarratives/draft-review.json)。プレイヤーは親しみある柔らかな常体。`{cat_call}` は採用呼称 `{cat}` を介してミケに展開する。年齢・性別・一人称・職業・来歴は追加しない。ミケは短毛三毛、機敏でのんびりした佇まいのムードメーカー。

採用・下書きの対象特別条件は各0。生成済み278 JSONを条件で照合し、対象特別候補0。voice通常文は他人物・猫も含め採用24条件・下書き24条件を[既存通常文](Results/MikeVoiceNarratives/existing-normal.json)に記録した。通常文の採用済み表現を今回の声・接触・反応の保証にせず、本文も候補IDも変更しない。既存特別文も全件保持。

既存Tk本文欄への入力→save_draft→登録内容確認→画像付き両欄プレビュー→画面の「今回の8条件だけ登録」→register→reload→新ストア・新編集画面で再読み込み。保存JSONの直接編集、独自形式、実LLM要求は使用していない。computer-useスキルを使い、4場面×2案のTool実画面で画像・本文・呼称展開を目視した。プレビュー用メモリ合成の採用元ラベルは実装上「登録済み」だが、ウィンドウタイトルと注意書きで未登録下書きと表示し、採用保存と区別。目視後に対象限定ボタンを操作し、登録前レビューと登録後の本文・候補IDは全件一致。対象外下書きの一括登録なし。

再現用制作スクリプトは対象採用済みなら停止し、上書きしない。`--resume-reviewed-drafts` は登録前の完全一致する下書きのみの再開用で、現在は使用しない。再確認はToolルートで `python verify_mike_voice_narratives.py` と `pwsh -NoProfile -File Docs/Results/MikeVoiceNarratives/runtime-check.ps1`。

## 成立条件・操作・画像

優先順位は simultaneous → open_up → connect → switch → normal。open_upは実際にopen_up_countが増えた確定結果、simultaneousはその結果とconnectの両方。connectだけでは親密化まで断定せず、同時成立は声への注目と親しみの双方を描く。本文の有無は成功判定・数値・乱数を変えない。

全条件がmode=voice・reaction=any。人物は `speech/player`・target_cat=cat-mike、猫は `cat_response/cat-mike`。switchはaction=switch・scene_key=voice__switchで、切替前の動作を指定せず声かけへの切替を描く。connectとsimultaneousはaction=connect・scene_key=voice__connect。open_upはaction=any・scene_key=voiceで、direct／adapt／pause／switchに共通。voiceにはintense／feintがないため検証に加えない。

|場面|実際の採用画像|本文で扱う範囲|
|---|---|---|
|switch／connect|voice `84005b2a4ce24bc2a344cc4ff7e5ceb9.png`|座り姿と相手への注目。耳・視線に焦点を置き、接近や接触を要求しない|
|open_up|voice__open_up `f18933dc47724e82b8aaee8f45bb5b44.png`|しっぽを立てた歩み寄り、柔らかな目元。声の継続を要求しない|
|simultaneous|voice__simultaneous `d87771ddf4ea4f22becd2156107ceb12.png`|座り姿、閉じた目と緩んだ開口。鳴き声や言葉への返事と断定しない|

元パス・SHA256・要求／解決画像キーはpreview-checks.jsonに記録。本文条件のscene_keyと画像選択キーは別で、switch／connectは通常voice画像に解決する。玩具・接触・喉鳴らし・鳴き声・言葉の理解・直前の動作は保証しない。

## 制作本文・条件キー・候補ID

### switch

|欄・案|本文|候補ID|
|---|---|---|
|人物1|今度は声をかけてみようか、{cat_call}。|`67c5716d0f525fde8a0c96d2a1b39e65`|
|人物2|{cat_call}、少しお話ししようか。|`13e9efa1def05c109881fa3d0fec6a32`|
|猫1|声がすると、{cat}はぱっと耳をそちらへ向けた。|`611091294d4558b688e6d95377a78e7a`|
|猫2|{cat}はちょこんと座ったまま、呼びかける相手に目を向けた。|`03387d50908e54e18636c0fef5018b5d`|

条件キー：`speech/player/voice/switch/any/voice__switch/cat-mike/scene=switch`

条件キー：`cat_response/cat-mike/voice/switch/any/voice__switch/scene=switch`

### connect

|欄・案|本文|候補ID|
|---|---|---|
|人物1|このくらいの声が、気になるのかな。|`073f7ab691bb572eb0e450ea0342e594`|
|人物2|声をかける間が、ちょうどよかったみたいだね。|`e704c35aebef59f1bd1ee8445760751f`|
|猫1|声をかける相手へ、{cat}の注意がまっすぐ向いた。|`4c668bc5ee9057119bb61cad253c6403`|
|猫2|{cat}は耳をそばだて、声のするほうをじっと見つめた。|`f3a648ff94c051229d99460e61df3296`|

条件キー：`speech/player/voice/connect/any/voice__connect/cat-mike/scene=connect`

条件キー：`cat_response/cat-mike/voice/connect/any/voice__connect/scene=connect`

### open_up

|欄・案|本文|候補ID|
|---|---|---|
|人物1|言葉のない間も、いっしょにいられるといいね。|`5e81b31c9b9557628a62ea3beb28ddc7`|
|人物2|{cat_call}、近くでのんびりしてくれるとうれしいな。|`a42b24e3573158de9b388b7db5ade04d`|
|猫1|{cat}はしっぽを立て、親しげな表情で相手へ歩み寄った。|`2aa51ccef3295171abaefa8b5f4253a6`|
|猫2|そばにいる相手に、{cat}は気を許したようなまなざしを向けた。|`952d7d81daae52c1a29692fce5c40abd`|

条件キー：`speech/player/voice/any/any/voice/cat-mike/scene=open_up`

条件キー：`cat_response/cat-mike/voice/any/any/voice/scene=open_up`

### simultaneous

|欄・案|本文|候補ID|
|---|---|---|
|人物1|声をかけたら、{cat_call}がぐっと身近に感じられたよ。|`16394f24f5b35fbf97b4742f69478c0c`|
|人物2|そんな顔を見せてくれると、声まで弾むね。|`ab0dca5902875c6c8e914faa7c97847f`|
|猫1|{cat}は呼びかけに注意を向け、うれしそうに目を細めた。|`56a222a7937a51938c55ae122c8beb3c`|
|猫2|{cat}は声のするほうに顔を向け、気を許すように口元をゆるめた。|`27ea7d53c03453528302d0b124b6155f`|

条件キー：`speech/player/voice/connect/any/voice__connect/cat-mike/scene=simultaneous`

条件キー：`cat_response/cat-mike/voice/connect/any/voice__connect/scene=simultaneous`

## 校正理由と候補の組み合わせ

switchは「声かけへ切り替える提案／短いお話への誘い」、猫は「ぱっと耳を向ける機敏さ／座ったまま目を向ける落ち着き」を描く。前の遊びや道具を置く動作は要求しない。「お話ししようか」は人物の誘いであり、猫が会話を理解し発話するという記述ではない。

connectは「声の調子／呼びかける間」に人物の着眼点を分け、猫は「注意の向き／耳と視線」に分けた。「みたい」「かな」の柔らかな推測を保ち、言葉の意味を理解した、名前を聞き分けた、信頼が深まったという断定は加えない。

open_upは「言葉のない間も共にいる願い／近くでくつろいでもらえる喜び」、猫は「自分から歩み寄る親しみ／気を許したまなざし」。現在も話し続けていることや、特定の言葉を受けて近づいたことを要求せず、休憩や切替でも成立する。人が休憩する操作で猫の自発的な歩み寄りは妨げられない。通常操作時にも「言葉のない間も」の「も」は静かな時間を含める願いとして成立する。

同時成立は「声かけを通じて身近に感じる実感／表情が自分の声にももたらす喜び」、猫は「呼びかけへの注意＋うれしそうな目元／声への顔の向き＋気を許す口元」。画像の開いた口を返事の鳴き声と決めつけず、声への反応と親しみを一つの猫文に併記した。

人物2案×猫2案の全組み合わせを校正し、特定のセリフへの返答、動作の前後の食い違い、命令・幼児語・恋愛・触る前提を避けた。玩具やブラッシングの単語置換にせず、声の調子、呼びかけの間、言葉のない時間、相手の表情によって変わる自分の声へ焦点を変えた。

## 限定検証と未確認範囲

- Tool下書き保存・対象限定確認登録・reload・新ストアと新編集画面で再読み込み、8条件16本文と候補IDの一致、空き案3の選択除外、呼称展開、場面別選択を確認。登録時に画像付き8組と、片側／両側欠落の実描画12組を確認。
- 登録済みデータで混合候補16組、片側／両側欠落21組を確認。欠落はメモリ上だけで再現。専用文がある片側は維持し、switch／connect／simultaneousの欠落欄は既存定型へ戻る。open_upは通常文へ戻り、pauseは休憩、switchは切替定型を使用、猫欄だけに心を開いた追記。専用猫文への二重追記なし。既存通常フォールバック全文の再校正は今回の対象外。
- 既存C#アセンブリ `Logs/MikeRestRuntime/bin/Debug/net8.0/Test.dll` を再利用。Compose＋ApplyCatalogに今回登録本文をメモリ内で渡し、4場面・open_up4操作・両欄／人物のみ／猫のみ／なし×2巡、**56件成功**。[実行ログ・アセンブリSHA256](Results/MikeVoiceNarratives/runtime-check.log)。ソース変更・再コンパイルなし。確定済み場面の本文選択fixtureであり、StepExchange・自然到達・現行Unityバイナリとの同一性は未確認。
- 特別文の既存テストはGUI有効化 `CAT_CAFE_TEST_GUI=1`、TEMP／TMP=`Logs/MikeVoiceNarratives/temp` で **4/4成功、1.875秒**。[ログ](Results/MikeVoiceNarratives/tests-special.log)。初回はGUI環境変数未指定で1件スキップだったため、GUI有効化して同4件を再実行した。通常本文ロジックを変更しておらず、通常68件・過去の総合一式・長期評価は繰り返さない。
- 開始時採用419条件の全フィールド・本文・候補ID、対象外下書き、採用プロフィール・manifest・画像対応・生成278 JSONと採用画像のハッシュを保持。並行作業差分も開始記録と一致。[保持結果](Results/MikeVoiceNarratives/preservation.json)。

採用データはGit管理外 `C:/Users/hi-wa/source/codex_repos/CatCafeSim/cat_cafe_sim/saves/content_images/interaction_texts.json`、下書きは `cat_cafe_sim_tool/user_data/interaction_text_drafts.json`。各8条件16本文追加。開始時全文はローカル `Logs/MikeVoiceNarratives/before.json` に保持。再現可能な条件・本文・候補ID・Tool経路の制作スクリプトをTool Gitへ保存した。

今回確認したのはTool登録・保存再読み込み・画像付きプレビュー・Python選択・既存C#メモリ内fixture。**全16本文はUnity取り込み待ち**。Unity画面・配布版・自然プレイ表示は未確認。素材パック書き出し・Unity取り込み・ビルド・画像生成・他対象の文章制作・ルール数値変更は実施していない。

## 容量

開始／終了実測は[capacity.json](Results/MikeVoiceNarratives/capacity.json)。Logs増は開始スナップショット、採用データと下書き増は今回の8条件16本文による。generated／Latest／Libraryは増減0。固定Latestは成功1組、Pendingなし。新しいビルドフォルダ・ZIP・Unityプロジェクトコピー・Libraryは作らず、既存素材・セーブ・並行成果物を削除していない。資料とGitオブジェクトは表の測定対象外。

|保存先|開始bytes|終了bytes|増減bytes|
|---|---:|---:|---:|
|Logs|18,361,697,542|18,362,530,088|+832,546|
|generated|3,277,372,585|3,277,372,585|+0|
|content_images|1,276,786,174|1,276,792,688|+6,514|
|Latest|4,841,096,360|4,841,096,360|+0|
|Library|7,358,000,371|7,358,000,371|+0|
|drafts|369,304|375,818|+6,514|

開始時の並行差分349ファイルのハッシュが一致。採用419条件と対象外下書き435条件を保持。生成278 JSONの追加・変更なし。
