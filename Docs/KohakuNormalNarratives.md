2026-10-28 Unity統合追補：**本資料の採用済み本文は新パックへ収録済み。取り込み待ち0。** 全体で追加176条件362本文、有効435条件1123本文。既存画像・人物・ID対応を保持し、実カタログの全追加条件選択、Editor Canvas、固定Latestでの代表表示、専用セーブの別プロセス再開を限定確認。[統合結果・確認段階・未確認・容量](NarrativeUnityIntegration.md)。以下の「Unity取り込み待ち」「未確認」は制作当時の記録であり、最新状態は統合記録を参照する。自然プレイ全件・総合確認を完了扱いにはしない。

# コハクの通常反応文

2026-10-27。通常8動作と休憩の34条件に各2案、計68本文を制作し、既存Toolの個別確認登録経路で登録。**今回の68本文はUnity取り込み待ち**。素材パック書き出し・Unity取り込み・ビルド・画像生成・数値変更は対象外。

## 現物照合と対象

制作IDとゲームIDはともに `cat-kohaku`、画像IDは `playtest-kohaku`。募集猫シロや同じ性格のアキとは別個体。採用台帳のプロフィール全文を[入力資料記録](Results/KohakuNormalNarratives/inputs.json)に保存。遊び好きで繊細、金色の両目、丸い顔と小さな丸みのある耳、明るい橙色の長毛の茶トラ、ふわふわの胸と頬、長くふさふさした縞の尾。静かな場面では柔らかく慎重な表情という設定を生かす。

開始時Tool HEADは指定のタマ制作 `d286eae`。Docs `ee2abd4`、Python `5df6a70`、Unity `87ea3c7`。最新履歴と未コミット差分を確認した。画像・台帳・設定などの並行差分は保持し、今回のコミットに含めない。各作業ツリーの開始記録は本資料と同名のResultsフォルダに置く。親フォルダ自体はGit管理外。

開始時の採用318条件・下書き334条件に、コハクの通常・操作別・特別条件は0。生成候補278 JSONもコハク0。既存本文を作り直さず新規に制作した。既存の人物文・共通行動文・地の文はプレビューに表示されるが、今回の制作対象には含まない。

## 到達性と制作条件

Python実ルール `TypesInteraction.step` と採用プリセット「遊び好きで繊細な猫」を使用。現行ToolのUnity用rulesの猫個性・全exchange数値・全8動作定義と一致することをassertで照合した。[到達性スクリプト](../check_kohaku_reachability.py)と[実記録](Results/KohakuNormalNarratives/reachability.json)に34到達列、初期体力・全操作列・確定反応・得点・判定前後状態、別途8動作の低体力からの休憩後adapt列を残す。

|動作|制作する反応|条件数|除外|
|---|---|---:|---|
|teaser|neutral / favorable / enthusiastic / confused / listless|5|turn_away|
|pet|neutral / favorable / confused / listless|4|enthusiastic / turn_away|
|ball|neutral / favorable / enthusiastic / confused / listless|5|turn_away|
|plush|neutral / favorable / enthusiastic / confused / listless|5|turn_away|
|tunnel|neutral / favorable / enthusiastic / confused / listless|5|turn_away|
|brush|neutral / confused / listless|3|favorable / enthusiastic / turn_away|
|voice|neutral / confused / listless|3|favorable / enthusiastic / turn_away|
|presence|confused / listless|2|neutral / favorable / enthusiastic / turn_away|
|pause|neutral / listless|2|休憩の仕様どおり他反応枠なし|

通常枠50のうち34条件を制作、16条件を現行設定に限って除外。保留なし。単なる探索未発見を到達不能の根拠にしない。

- 拒否8条件：`turn_away` は動作好みまたは強さ好みが0の時。コハクは全値が正のため該当しない。強い動きの好み0.2は拒否そのものではなく、得点低下として働く。
- pet熱心：全合法操作を含む得点上限10.8で18未満。
- brush好意・熱心：上限8.64で10未満。負反応後のgentleも7.488以下。
- voice好意・熱心：上限8.4で10未満。gentleも7.28以下。
- presence通常・好意・熱心：上限4.8で通常下限6未満。gentleも4.16以下。体力があれば戸惑い、低体力時はlistless。

得点は基礎gain×動作gain×動作好み×強さ好み×連続減衰×切替倍率。連続減衰は `max(0.4, 1−0.15×(同グループ連続数−1))`。動作を切り替えるだけでは同じplayグループの連続数は消えず、異なるグループでリセットされる。コハクの切替親和性は0なので切替倍率は1で上限を押し上げない。負反応の直後のadaptは基礎8×gentle好み1.3を使う。通常判定の境界は6、10、18、低体力は開始時体力20以下で、0好みの拒否がそれより優先。

到達例（すべて通常場面。必要な切替を先に実施）：

- teaser：direct初回21.6で熱心、adapt2回目15.3で好意、intense初回8.64で通常、intense4回目4.752で戸惑い。
- pet：direct初回10.8で好意、adapt初回9で通常、direct4回目5.94で戸惑い。
- ball：direct初回19.2で熱心、adapt初回16で好意、intense初回7.68で通常、intense3回目5.376で戸惑い。
- plush：direct初回15.12で好意、adapt3回目8.82で通常、adapt5回目5.04で戸惑い。direct→pause→feintは19.278で熱心。
- tunnel：direct初回15.84で好意、intense初回6.336で通常、intense2回目5.3856で戸惑い。direct→pause→feintは20.196で熱心。
- brush：direct初回8.64で通常、adapt3回目5.04で戸惑い。voice：direct初回8.4で通常、adapt2回目5.95で戸惑い。
- presence：direct初回4.8で戸惑い。全8動作は初期体力15のdirectでlistless。pauseは初期体力100でneutral、15でlistless。

ぬいぐるみ・トンネルの熱心は初回得点では除外しない。休憩直後のfeintは基礎18で、直前の遊びを含む連続2回目の減衰0.85を受けても18に達する。pauseは開始時の体力で反応を決めてから2回復するため、回復後に20を超えていても直前反応listlessが残り、次のadaptでgentleを使う。初期体力21から各動作direct→必要回数pause→adaptを実stepで確認した。休憩文は回復の完了・直前の玩具・寝姿・接触継続を条件にしない。

自然プレイでの頻度、全セーブ・旧設定・ユーザーによる変更後の到達性は未確認。上の除外はあらゆる設定での到達不能を意味しない。

## 校正

タマの文章は登録・検証の手順だけを参考にした。コハクは遊びへの期待と慎重さの揺れを軸に、目の輝き、耳、ふさふさの尾、反応の間合いを使い分けた。通常は控えめな観察、好意は楽しさ、熱心は夢中、戸惑いは遊びたい気持ちと調子の不一致、低体力は鈍い反応として区別。各2案で視線と耳・尾、表情と応じ方など焦点を変える。人物の口調や他猫の名前・単語の置換で作らない。

既存準備9枚・実行画像・通常/play画像を[画像比較](Results/KohakuNormalNarratives/image-review.jpg)で目視確認。既存Toolの画像付きプレビューでは全9動作の代表9画面と休憩listlessの2案、計11画面を目視確認した。通常実行画像に接触が見えても、捕獲・抱え込み・トンネルへの侵入／通過・ブラシ接触の成功や毛並み改善を本文で保証しない。なでるは許可された行為への反応、声かけは声への注意、見守りは気配と距離だけを描く。休憩画像に玩具が残っていても本文で直前の遊びを固定しない。

校正時にteaser戸惑い案1の「目で追いかけかけて」を「目を向けたものの」へ改め、読みにくい反復を解消。なでる好意案2の目を細める描写などは反応時の様子で、静止画像と表情の完全一致を保証するものではない。開眼画像から閉眼・接触継続を断定していない。否定反応のゲーム実行側は準備画像を優先するため、Toolの操作画像解決と同じ画像が出るとは扱わない。

## 保存と再現

本文は[kohaku_normal_texts.py](../kohaku_normal_texts.py)。[登録スクリプト](../register_kohaku_normal_narratives.py)は既存 `InteractionTextEditorWindow` の入力欄へ本文を入れ、単一条件ずつ `save_draft → preview（登録内容確認）→ register → reload` を実行する。保存JSONを独自形式で書き換えない。未登録データで `python register_kohaku_normal_narratives.py --review` は保存なし校正、引数なしは確認登録。対象の既存採用・下書き・生成候補があれば停止し、再実行で上書きしない。

採用データはGit管理外 `C:/Users/hi-wa/source/codex_repos/CatCafeSim/cat_cafe_sim/saves/content_images/interaction_texts.json`、下書きはGit管理外 `C:/Users/hi-wa/source/codex_repos/CatCafeSim/cat_cafe_sim_tool/user_data/interaction_text_drafts.json`。条件・本文・候補IDは[登録記録](Results/KohakuNormalNarratives/registration.json)と下表に保存し再現可能にした。案3は空で採用本文数に含めない。

プレビュー照合は、正規 `ImageRegistry.read` で検証した画像manifestのメモリ上スナップショットを再利用する。全候補の描画処理と画像読込は既存Toolそのもの。manifestのハッシュ不変を終了時に確認する。最初のプレビュー走査は同じmanifestのパス検証を毎回繰り返して時間がかかったため、本文保存前に中断してこの読み取りの再利用を加え、全件を最初から再確認した。製品Toolのコードは変更していない。

## 限定検証結果

- 採用318→352条件、下書き334→368条件。コハク34条件68本文だけを追加。各条件の下書き保存・登録内容確認・確認登録・編集画面再読み込み・新規ストア再読み込みが成功。候補ID・本文の重複なし、プレースホルダーは `{cat}` のみで、全本文のコハクへの展開を確認。
- 既存採用318条件・下書き334条件の全エントリを前後比較して保持。台帳・画像manifest・生成候補278 JSONのSHA256は登録前後で一致。本文・候補ID・プレビュー表示元・画像元・解決画像キーを[登録記録](Results/KohakuNormalNarratives/registration.json)に保存。
- 全68本文×有効操作、計202組の画像付きプレビューで本文展開と画像読込に成功。intense／feint等の専用画像がない箇所は動作共通画像への既存フォールバックをそのまま使用し、解決先を記録。
- [登録後照合](../verify_kohaku_registered.py)で新規ストアから140組を照合。101組は登録済み候補2案（操作共通99組、休憩専用2組）、39組は未制作・候補なし。[Tool再読込結果](Results/KohakuNormalNarratives/tool-reload.json)。登録前に一度実行した中間照合は、未登録なので候補一致assertで停止。本登録後の再実行で140組すべて成功した。
- [実行時fixture](Results/KohakuNormalNarratives/runtime-check.ps1)は既存.NETアセンブリの実 `StepExchange` に34到達列と8休憩後列を入力し、Pythonと同じ反応・休憩後体力を確認。Unity有効カタログのメモリ上コピーに今回分だけを追加し、280件の候補ID・本文・pool選択／定型フォールバック、8動作の操作別優先を確認。未登録は候補ID・poolなしで既存 `Compose` の本文を保持。[実行結果](Results/KohakuNormalNarratives/runtime-result.txt)。アセンブリSHA256 `E77E4DF261B456159F93C65A69677BED98FFEA4FE4675A9116B68026A64D1629`。新規コンパイルなし。
- fixtureは反応判定と文章選択の限定確認で、Unity Editor画面・自然プレイ頻度・配布版での今回本文表示は未確認。68本文すべてUnity取り込み待ち。素材パック書き出し・Unity取り込み・ビルド・長期評価を実施していない。既存の取り込み待ち本文も保持。

## 採用本文・条件・候補ID

通常場面。8動作は操作共通キー `cat_response/cat-kohaku/<動作>/any/<反応>/<動作>`、休憩は `cat_response/cat-kohaku/pause/pause/<反応>/pause`。

|動作|反応|候補ID|本文|
|---|---|---|---|
|teaser|neutral|`66ec2a7426d45c2b9822cbdc92ac8381`|{cat}はねこじゃらしをじっと見た。遊び心をのぞかせつつ、誘いの調子を確かめている。|
|teaser|neutral|`ae8b124f9755570090dbcf9156af5ecb`|{cat}の丸い耳がねこじゃらしへ向いた。まだ大きく応じず、動きを見ている。|
|teaser|favorable|`52e35e07757052a6b549f7a3371e9deb`|{cat}はねこじゃらしを楽しそうに目で追った。慎重な顔にも、遊びたい気持ちがにじむ。|
|teaser|favorable|`c7b43edcabfc58b891e0949ae44bc19d`|{cat}はねこじゃらしに気を引かれ、ふさふさの尾の先を小さく動かした。|
|teaser|enthusiastic|`189b8bd4c477569c8213416cf27fdd21`|{cat}は金色の目をきらきらさせ、ねこじゃらしに夢中になっている。|
|teaser|enthusiastic|`7948a58d1a5d53989f185d6a57ea2c8f`|{cat}はねこじゃらしの誘いに意気込んだ。ふっくらした顔いっぱいに、遊ぶ喜びが表れている。|
|teaser|confused|`4dbf65e8982258bcbf6fefbf7f7a48e5`|{cat}はねこじゃらしへ目を向けたものの、ためらった。遊びたいのに調子が合わないようだ。|
|teaser|confused|`92369506a59957d4b412cefd8ce3eb79`|{cat}の視線がねこじゃらしと相手の顔を行き来する。誘いをどう受け取るか迷っている。|
|teaser|listless|`a76e2a4892185344aec925ecac8fd024`|{cat}はねこじゃらしへ目を向けたが、疲れているらしく、視線の動きもゆっくりだ。|
|teaser|listless|`26b686dbe5435b4daa0217ff6c693db8`|{cat}はねこじゃらしをぼんやり眺めている。遊び好きの目にも、今は勢いがない。|
|pet|neutral|`6eb5d2b89ff75fa8a6e118449758ae11`|{cat}はなでる手の様子をうかがっている。ふっくらした頬の表情は、まだ控えめだ。|
|pet|neutral|`4c7b5280e91e5b25ad9e6071d5a2d9d4`|{cat}はなでられることに静かに応じた。甘えるでもなく、慎重な顔で相手を見ている。|
|pet|favorable|`5a830e48a1815e7d98907b8a46cd97ae`|{cat}はなでる手にやわらかな目を向けた。ふわふわの頬にも、くつろいだ表情が浮かぶ。|
|pet|favorable|`76f7c39966ff53c8a113751d083f4149`|{cat}はなでられながら、気持ちよさそうに目を細めた。遊ぶ時とは違う、穏やかな顔だ。|
|pet|confused|`9bddb660328b5ea69384feaf81e99f2d`|{cat}はなでる手を気にして、相手の顔をちらりと見た。落ち着く間合いを探しているようだ。|
|pet|confused|`a887da9a95d95d78835f764c64958fec`|{cat}はなでられることに戸惑いを見せた。丸い耳にも、少し迷いが表れている。|
|pet|listless|`f8c26b401aa857d9bb6cd66293635c5b`|{cat}はなでる手にゆっくりと目を向けた。疲れているようで、甘える反応も小さい。|
|pet|listless|`e37f40ab45d05d04a0897c8a62f24962`|{cat}はなでられても、けだるそうな様子だ。ふっくらした顔に眠たげな表情を浮かべている。|
|ball|neutral|`a1c28352f9305ccc83605eb036eaf31a`|{cat}はボールの動きを見ている。遊びに乗る前に、少し確かめておきたいようだ。|
|ball|neutral|`53b153322fdb509a84c518f54648abbf`|{cat}はボールを気にしながらも、控えめに応じた。金色の目は注意深いままだ。|
|ball|favorable|`171a465c7e5d5262af95a68abd7c1a35`|{cat}はボールを楽しそうに見つめた。ふさふさの尾の先にも、遊び心がのぞく。|
|ball|favorable|`18247d7ff1c25c0fbbe1397aebb9f46b`|{cat}はボールの動きに気持ちが弾んだようだ。丸い顔を向け、いきいきと応じている。|
|ball|enthusiastic|`06c944b89155563da9b3fa7312bc8877`|{cat}はボールにすっかり夢中だ。金色の目に、遊びの続きを待つ輝きがある。|
|ball|enthusiastic|`67d9b98f6d595128a61a16f53c2fa2f7`|{cat}はボール遊びの誘いに張り切った。ふだんの慎重さも忘れたような、熱のこもった反応だ。|
|ball|confused|`993a48d0fb3e5819bd447fea1d03df42`|{cat}はボールを見つめたまま、応じるのをためらった。遊びの調子がつかめないらしい。|
|ball|confused|`fde54686b9265a7da9a72253dc4cfa32`|{cat}はボールから相手へ目を移した。遊ぶ気持ちと戸惑いが、丸い顔に同居している。|
|ball|listless|`ef8fb10837e35305b7dd99a0b7d37ba7`|{cat}はボールを目で追うのもおっくうそうだ。遊びへの関心より、疲れが勝っているように見える。|
|ball|listless|`9f7036f0aa215f48b0b266f32ef7a50a`|{cat}はボールへゆっくり顔を向けた。いつもの遊びたがりな表情は、今は控えめだ。|
|plush|neutral|`e4b24be2d58b5ab49d0120125fe6105c`|{cat}はぬいぐるみの様子を注意深く見ている。遊び相手として、ひとまず気に留めたようだ。|
|plush|neutral|`45c3b4ff1f865d1f96356ced26ea431f`|{cat}はぬいぐるみに控えめな反応を返した。丸い耳は向いているが、まだ様子見らしい。|
|plush|favorable|`9480b0b0d2525eab932c8e64d1112e41`|{cat}はぬいぐるみに楽しげな顔を向けた。慎重に見ていた目にも、遊び心がのぞく。|
|plush|favorable|`6cdcd397d8235d06971c50cff2c98b1a`|{cat}はぬいぐるみ遊びの誘いに機嫌よく応じた。ふさふさの尾の先が、小さく揺れる。|
|plush|enthusiastic|`6192a68cf8b0560dbc049b81ee331054`|{cat}はぬいぐるみに夢中になっている。金色の目が輝き、遊びへの期待を隠しきれない。|
|plush|enthusiastic|`00cc8ac117f25fb7a84c1e810a6c768e`|{cat}はぬいぐるみ遊びにすっかり気を良くした。丸い顔から、次も遊びたい気持ちが伝わってくる。|
|plush|confused|`5a628b48dc435fe5a5fc89ac8362ba95`|{cat}はぬいぐるみを気にしながら、ためらうような顔をした。遊び方がしっくりこないらしい。|
|plush|confused|`0e9db862fb2458ee9e4ee97738656671`|{cat}はぬいぐるみと相手の顔を交互に見た。楽しみたいのに、どう応じるか迷っているようだ。|
|plush|listless|`b21aba3d017a5442af29bfb1f66ea561`|{cat}はぬいぐるみにぼんやり目を向けた。遊びの誘いにも、疲れた様子がにじむ。|
|plush|listless|`e89965ed0e645eafbfd7a59b96136ebb`|{cat}はぬいぐるみへの反応が鈍い。ふっくらした顔もけだるそうで、遊びに身が入らないらしい。|
|tunnel|neutral|`2959fd5a514c5929a4532bd92fd835d6`|{cat}はトンネルの様子を慎重にうかがった。遊びへの興味はあるが、反応は控えめだ。|
|tunnel|neutral|`0c9308561b765853a652a2a72e16454b`|{cat}はトンネルへ目を向けている。丸い耳にも、様子を確かめるような用心深さがある。|
|tunnel|favorable|`5977a9e1665351bca1697ee59db326b3`|{cat}はトンネル遊びに楽しそうに応じた。慎重な表情の中にも、期待が見える。|
|tunnel|favorable|`ae6fa9aad5225890ae03b2a30c76d029`|{cat}はトンネルへいきいきと顔を向けた。遊びの誘いを気に入ったようだ。|
|tunnel|enthusiastic|`ba0eb037e6f851af91c455c02d0d22c6`|{cat}はトンネル遊びに心を弾ませた。金色の目は、遊びの様子に釘づけになっている。|
|tunnel|enthusiastic|`92228490f63852bb9021f27d99bfcb2b`|{cat}はトンネルへ熱のこもった反応を返した。丸い顔に、もっと遊びたいという期待があふれる。|
|tunnel|confused|`ad55bd1771da5c3ca7a90787ba11a0d8`|{cat}はトンネルの様子に気を取られつつ、応じるのを迷っている。気持ちよく遊ぶ調子が合わないようだ。|
|tunnel|confused|`090b00799b745d268f69e85403433b08`|{cat}はトンネルと相手を見比べた。遊びに誘われても、少し用心しているらしい。|
|tunnel|listless|`2aab88e002d156be82557378e43f87aa`|{cat}はトンネルへ重たげな視線を向けた。遊びの誘いに応じる元気が、今は少ないようだ。|
|tunnel|listless|`a521762a16205a9c8c3ebd3adf438420`|{cat}はトンネル遊びにもけだるそうだ。目の輝きも控えめで、疲れがうかがえる。|
|brush|neutral|`04dab13afb355ffaae8fce03b62b9437`|{cat}はブラシの動きを静かに見ている。ふわふわの頬の表情は、まだ慎重だ。|
|brush|neutral|`9191badcfeb85313ab68f7bf7031b654`|{cat}はブラッシングに控えめに応じた。気にしてはいるが、くつろぐほどではないらしい。|
|brush|confused|`f4931addc4255b4d927870ba38b19edb`|{cat}はブラシと相手の顔を見比べた。遊びの時とは勝手が違うのか、少し戸惑っている。|
|brush|confused|`9350b21c91d95ef9a02254c9dd419c55`|{cat}はブラシを気にして耳を動かした。落ち着いて受け止めるには、まだ迷いがあるようだ。|
|brush|listless|`7efae6a565915811bb3808a087915993`|{cat}はブラシへゆっくり目をやった。ふわふわの頬にも、疲れた表情が浮かんでいる。|
|brush|listless|`929676db64695436a21320692dc342c8`|{cat}はブラッシングへの反応も小さい。気にする元気があまりないのか、けだるそうにしている。|
|voice|neutral|`4bb9ee3f35b454d59888415b026a3253`|{cat}は声の方へ丸い耳を向けた。何か始まるのかと、静かに様子をうかがっている。|
|voice|neutral|`a282c3ff44d051ce815d3e71060ebf2d`|{cat}は呼びかけに気づき、相手を見た。金色の目に、控えめな関心がのぞく。|
|voice|confused|`5544ccaeb4f15b1fbbe1de684c6c14e1`|{cat}は声に耳を傾けながら、不思議そうな顔をした。どう応じればよいか迷っているらしい。|
|voice|confused|`1aee77f1534f5004b13cca07202ca01d`|{cat}は呼びかける相手を慎重に見つめた。気にはなるが、反応のきっかけをつかめないようだ。|
|voice|listless|`cff4aeaa51e058309051d197f0871a0a`|{cat}は声の方へゆっくり目を向けた。疲れがあるのか、応じる様子は乏しい。|
|voice|listless|`4874611d494258ccb0a78e4f819292eb`|{cat}は呼びかけを聞いているようだが、けだるそうだ。丸い耳の反応も、ごく小さい。|
|presence|confused|`60a43af901885098b0d81bdb13ce8746`|{cat}はそばの気配を意識して、相手の様子をうかがった。この距離でどう過ごすか迷っているようだ。|
|presence|confused|`f2091ac0ec6454258d75c4e8f6c43a1a`|{cat}は見守る相手へ、ためらいがちな視線を向けた。静かな間合いに、まだなじみきれないらしい。|
|presence|listless|`52a9216523795f81b2553bdce67c7af6`|{cat}はそばの気配へゆっくりと目をやった。相手との距離を気にするより、今は休みたいようだ。|
|presence|listless|`a7600d8c9cbf560d8f924b218f6610ad`|{cat}は見守られながら、けだるそうに過ごしている。周りへの反応も控えめだ。|
|pause|neutral|`3882c8e6f79156ffb44150ad2518f0df`|{cat}はひと息ついている。遊ぶ時の張り切った顔とは違い、やわらかな表情だ。|
|pause|neutral|`c85a01e3cdf25d5e9bd1885cf9bc04ee`|{cat}は休憩のひとときを静かに過ごしている。ふっくらした顔にも、落ち着きが見える。|
|pause|listless|`d486d911f8df57b9b97cd1d4d66ab9c6`|{cat}は疲れた様子で休んでいる。金色の目にも勢いはなく、今は静かにしていたいようだ。|
|pause|listless|`70657f3744105033b1bc6e0ebf55176f`|{cat}はけだるそうに休憩している。遊び好きの表情もひと休みして、反応は小さい。|

## 容量・保持した差分

開始→終了（bytes）：固定ビルド5,046,668,269→5,046,668,269、Logs全体18,358,473,210→18,358,473,210、Tool generated 3,277,372,585→3,277,372,585、Unity Library 7,358,000,371→7,358,000,371。content_imagesは1,276,700,516→1,276,729,646（+29,130）、下書きJSONは283,646→312,776（+29,130）。増加は今回の採用・下書き本文のみ。大きな生成物の増加なし。[容量記録](Results/KohakuNormalNarratives/capacity.json)。制作資料・画像比較・再現スクリプトは同記録に別集計する。

固定StandaloneBuildはLatestの成功ビルド1組とRuns、既存build.lockのみ。Pendingなし。ビルド、Library複製、容量整理による削除なし。Tool既存AllCats／CatImageSchedule／CatImageTodo／presentation／CafeNarrativeCatalog、Rin／Yuki／Aki画像制作ファイル、Pythonの猫設定・募集設定・台帳、Unityの並行差分を保持。今回のコミットは本文・再現スクリプト・制作記録とDocsの取り込み待ち追記のみ。pushなし。
