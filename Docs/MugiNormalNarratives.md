# ムギの通常反応文

2026-10-27。通常8動作と休憩の35条件に各2案、計70本文を制作・校正し、既存Toolの個別確認登録経路で登録。**今回の70本文はすべてUnity取り込み待ち**。素材パック書き出し・Unity取り込み・ビルド・画像生成・数値変更・他対象の本文制作は実施していない。

## 現物照合

制作ID・ゲームIDは `cat-mugi`、画像IDは `playtest-mugi`。採用台帳のプロフィール全文・画像対応・開始時の本文棚卸しは[入力記録](Results/MugiNormalNarratives/inputs.json)。性格は中立。オリーブグリーンの両目、穏やかな丸顔、三角耳、均整の取れた中くらいの成猫。暖かい茶褐色の短毛キジトラで、細く明瞭な黒褐色の縞、中程度の長さの縞尾、黒褐色の尾先、白斑なし。新しいプロフィールは追加していない。

開始時HEADはTool `c1ae886`、Docs `2fb1dd2`、Python `5df6a70`、Unity `87ea3c7`。AGENTS.md、最新履歴・未コミット差分、InteractionNarrativeCompletionScope、DevelopmentStatus、AllNormalInteractionNarratives、KohakuNormalNarrativesを確認。[開始記録](Results/MugiNormalNarratives/start-worktrees.json)に履歴・status・既存追跡差分SHA256を保存し、今回の資料追記前に全4リポジトリの既存追跡差分不変を確認した（[照合記録](Results/MugiNormalNarratives/preserved-worktrees.json)）。親フォルダ自体はGit管理外。

開始時採用352条件・下書き368条件にムギの通常・操作別・特別文は0。生成候補278 JSONにもムギ0。既存採用本文・候補ID・下書き・プロフィール・画像対応を保持して不足分だけ追加。コハク資料は手順だけを参照し、本文を名前置換していない。

## 到達性と制作条件

[到達性スクリプト](../check_mugi_reachability.py)はPython実ルール `TypesInteraction.step` と採用プリセット「中立」を使う。Unity用rulesのムギの個性・全exchange数値・全8動作定義との一致をassertで照合。[結果](Results/MugiNormalNarratives/reachability.json)に35条件の初期体力・全操作列・確定反応・得点・判定前後状態と、8動作の休憩後adapt列、得点上限・除外理由を記録。探索で見つからないことだけを除外理由にしない。

|動作|制作反応|条件数|除外|
|---|---|---:|---|
|teaser|neutral / favorable / enthusiastic / confused / listless|5|turn_away|
|pet|neutral / favorable / confused / listless|4|turn_away / enthusiastic|
|ball|neutral / favorable / enthusiastic / confused / listless|5|turn_away|
|plush|neutral / favorable / confused / listless|4|turn_away / enthusiastic|
|tunnel|neutral / favorable / enthusiastic / confused / listless|5|turn_away|
|brush|neutral / favorable / confused / listless|4|turn_away / enthusiastic|
|voice|neutral / confused / listless|3|turn_away / favorable / enthusiastic|
|presence|neutral / confused / listless|3|turn_away / favorable / enthusiastic|
|pause|neutral / listless|2|仕様上neutral・listlessのみ|

通常枠50中35条件を制作し、15条件を現行設定に限り除外。保留なし。動作好み・強さ好みは全て1、切替親和性0、連続減衰0.15。得点は基礎gain×動作gain×両好み×連続減衰×切替倍率。減衰は `max(0.4, 1−0.15×(同グループ連続数−1))`。同じplayグループ内の動作切替では連続数が消えず、異なるグループでリセットするが、切替倍率は1。

- 拒否8条件は好み0の条件がないため除外。低体力より0好みの拒否が優先する実仕様も確認。
- pet熱心：全合法操作の上限12 < 18。brush熱心：10.8 < 18。
- plush熱心：休憩後feintの基礎18も含む保守的上限16.2 < 18。コンボを使える実列でも減衰が掛かる。初回得点だけで除外していない。
- voice好意・熱心：上限8.4 < 10。presence好意・熱心：上限6 < 10。

通常の境界は6・10・18、低体力は判定開始時体力20以下。負反応直後のadaptは基礎8のgentle。feintは直前が休憩かつ交流後休憩なら基礎18のコンボを使用。切替や休憩を含む実操作列を実行した。

到達例：teaser・ballはintense初回24で熱心、tunnelは26.4。petはdirect初回12で好意、adapt2回目8.5で通常、4回目5.5で戸惑い。plush・brushはdirect初回10.8で好意、adapt初回9で通常、direct4回目5.94で戸惑い。voiceはdirect初回8.4で通常、adapt2回目5.95で戸惑い。presenceはdirect初回6で通常、adapt初回5で戸惑い。全8動作は初期体力15のdirectでlistless。pauseは初期体力100でneutral、15でlistless。

初期体力21→各動作direct→必要回数pause→adaptも確認。休憩は開始体力で反応を決めてから2回復するため、20を超えた直後も前反応listlessが残る。次adaptはgentleとなり、teaser/pet/ballは6.8、plush/brushは6.12、tunnelは7.48で通常、voiceは4.76、presenceは3.4で戸惑い。自然プレイ頻度、全セーブ、旧設定、設定変更後の到達性は未確認。今回の除外は将来設定を含む恒久的な到達不能を意味しない。

## 校正

中立を無反応の理由にせず、自然な落ち着きと反応までの間合いを軸にした。通常は静かな注意・控えめな受け止め、好意はくつろぎや楽しさ、熱心は視線の集中や応じる間の短さ、戸惑いはためらいと間合いの不一致、低体力は重い視線・鈍い耳や尾の動きとして区別。各2案で、視線と表情／耳や尾、注意の向け方／応じる調子など焦点を変えた。目色・丸顔・縞尾は必要な箇所に留め、外見説明の反復にしない。

[画像比較](Results/MugiNormalNarratives/image-review.jpg)で通常・play・全9動作の既存画像を目視。既存Toolの画像付きプレビューでは9動作代表9画面と休憩listlessの2案、計11画面を目視校正。全70本文は制作ソース上でも通読した。本文と画像の両方にムギの対応・落ち着き・動作が合うことを確認。目や尾の動きは反応時の描写で、静止画像との完全一致を保証しない。

捕獲・追走成功・抱え込み・トンネルへの侵入／通過・ブラシ接触の成功・毛並み改善を本文で断定しない。なでるは許可された行為への反応、声かけは声への注意、見守りは気配と距離への反応として記述。休憩画像に玩具が残っていても、本文は直前動作・接触継続・睡眠姿勢・回復完了を条件にしない。操作画像に侵入や接触が写っていても、共通本文はそれを必須にしない。拒否等の実行時画像は準備画像が優先するため、Toolの操作画像解決と同じ画像が出るとは扱わない。

プレビューには既存の人物文・共通行動文・地の文が併記される。これらは制作対象外として保持。ウィンドウ再作成直後のUI取得が数回失敗したが、ハンドルを取り直して全11画面を確認できた。保存・登録処理の失敗はない。

## 保存・再現と限定検証

本文は[mugi_normal_texts.py](../mugi_normal_texts.py)。[登録スクリプト](../register_mugi_normal_narratives.py)は既存 `InteractionTextEditorWindow` の入力欄に渡し、単一条件ずつ `save_draft → preview（登録内容確認）→ register → reload` を使う。保存JSONの独自形式への直接書換えは行わない。`--review` は保存なしの画像付き校正、引数なしは確認登録。既存の対象採用・下書き・生成候補があれば停止するため、登録後に再実行しても上書きしない。プレビューのmanifestは正規ImageRegistry.readで検証したスナップショットを再利用し、製品Toolコードは変更しない。

採用データはGit管理外 `C:/Users/hi-wa/source/codex_repos/CatCafeSim/cat_cafe_sim/saves/content_images/interaction_texts.json`、下書きはGit管理外 `C:/Users/hi-wa/source/codex_repos/CatCafeSim/cat_cafe_sim_tool/user_data/interaction_text_drafts.json`。再現可能な本文・条件・候補IDを制作ソースと[登録記録](Results/MugiNormalNarratives/registration.json)に保存。

- 採用352→387条件、下書き368→403条件。35条件・各2本文、案3は空。条件ごとの保存・登録内容確認・確認登録・編集画面再読込・新規ストア読込に成功。既存採用352・下書き368の全entryを前後比較して保持。台帳・画像manifest・生成候補278 JSONのSHA256も不変。
- 70本文・候補IDに重複なし。差し込みは `{cat}` だけで全候補のムギへの展開を確認。全候補×有効操作204組のTool画像付きプレビューで本文・画像読込・表示元・解決画像キーを照合し、登録記録に保存。専用操作画像がない場合の動作共通画像への補完は既存仕様を使用。
- [登録後照合](../verify_mugi_registered.py)で140組を新規ストアから確認。102組が登録済み2案（操作共通100組・休憩専用2組）、38組が未制作・候補なし。[再読込結果](Results/MugiNormalNarratives/tool-reload.json)。
- [実行時fixture](Results/MugiNormalNarratives/runtime-check.ps1)は既存.NETアセンブリの実 `StepExchange` に35到達列・8休憩後列を入力し、Pythonと同じ反応・休憩後体力を確認。Unity有効カタログのメモリ上コピーへ今回分だけを追加し、280件の候補ID・展開本文・pool選択／未登録条件の定型保持と8動作の操作別優先を確認。[実結果](Results/MugiNormalNarratives/runtime-result.txt)。アセンブリSHA256 `E77E4DF261B456159F93C65A69677BED98FFEA4FE4675A9116B68026A64D1629`。新規コンパイルなし。
- Tool登録・画像付き校正と実行時fixtureの限定確認を完了。Unity Editor画面・自然プレイでの今回本文表示・配布版表示は未確認。70本文すべてUnity取り込み待ち。書き出し・Unity取り込み・ビルド・長期評価・総合確認は実施していない。

## 採用本文・条件・候補ID

通常場面。8動作は `cat_response/cat-mugi/<動作>/any/<反応>/<動作>`、休憩は `cat_response/cat-mugi/pause/pause/<反応>/pause`。

|動作|反応|候補ID|本文|
|---|---|---|---|
|teaser|neutral|`cef7808736bb52578e4e25763c73a0d8`|{cat}はねこじゃらしの動きを眺めている。急いで応じず、自分の間合いで目を向けた。|
|teaser|neutral|`b573e0c412ec541e8056b8afea701cf1`|{cat}の三角の耳がねこじゃらしへ向いた。丸い顔の表情は穏やかなままだ。|
|teaser|favorable|`914a33f425f85926b9f763a7001e1968`|{cat}はねこじゃらしを目で追い、楽しげな表情を見せた。ひと呼吸おいてから、誘いに応じている。|
|teaser|favorable|`733fe7dce4c059fd87452a039da7b529`|{cat}はねこじゃらしの動きを気に入ったようだ。縞のある尾の先が、軽く揺れた。|
|teaser|enthusiastic|`a2c5dda068f05288b29cdfab13f3e237`|{cat}のオリーブグリーンの目が、ねこじゃらしに釘づけになった。次の動きを待つ顔に熱がこもる。|
|teaser|enthusiastic|`f55e4bde939d517aab8520a7821c7eb2`|{cat}はねこじゃらし遊びにすっかり乗り気だ。さっきまでのゆったりした間が、短くなっている。|
|teaser|confused|`f3821ba303c15d278fb6fb1ef816bd9b`|{cat}はねこじゃらしへ目を向けたところで、反応を止めた。誘いの調子をつかめずにいるようだ。|
|teaser|confused|`756a0c5b28135a2c81c8d7a097506253`|{cat}の耳がねこじゃらしと相手の方を向き直った。どう応じるか、決めかねている。|
|teaser|listless|`c7230ef43a585a54afa8c30fae148620`|{cat}はねこじゃらしを眺めているが、目で追う動きは重い。今は疲れが出ているようだ。|
|teaser|listless|`c0c41dffb04c50ad8e065626f18e488a`|{cat}はねこじゃらしにわずかに耳を向けた。遊びに応じる勢いはなく、けだるそうだ。|
|pet|neutral|`5dce1a35a2785313b41742f4755f3af2`|{cat}はなでる手に気づき、静かに目を向けた。大きく甘えず、穏やかに受け止めている。|
|pet|neutral|`4f0cd2cdbb865e8888c2725554b84c15`|{cat}はなでられる間、相手の様子を見ている。丸い顔には、まだはっきりした喜びは浮かばない。|
|pet|favorable|`e73e70ae5f0b58a1b68e23a6ae5a887f`|{cat}はなでる手に心地よさそうな顔をした。ゆっくりと目元が和らいでいく。|
|pet|favorable|`05eafdfc7d5557beaca8448bf7198719`|{cat}はなでられることを気に入ったようだ。縞のある尾の先を、くつろいだ調子で動かした。|
|pet|confused|`3484972c9ca15fe2a322a7d24a0071e6`|{cat}はなでる手を気にして、相手へ視線を戻した。いつもの落ち着いた顔に、迷いが見える。|
|pet|confused|`e7b1d0ff77455a8cb5c40ebebcc25d35`|{cat}はなでられることに戸惑った。耳を少し動かし、心地よい間合いを探しているようだ。|
|pet|listless|`29dbdbacee585ec48723b6571954718c`|{cat}はなでる手へ重たげに目を向けた。疲れているらしく、表情の変化も小さい。|
|pet|listless|`3734aba4d5b052fe98102397aa388de2`|{cat}はなでられても、けだるそうにしている。甘えるより、静かに過ごしたいようだ。|
|ball|neutral|`3de6a6ec4c285488a59bd5a28440991e`|{cat}はボールに目を留めた。しばらく眺めてから、ゆっくりと顔を向けている。|
|ball|neutral|`2b28bd74c4e25d0f8ac5f1322cb062b8`|{cat}はボール遊びに控えめに応じた。耳は向いているが、張り切るほどではないらしい。|
|ball|favorable|`9efb0d46d1c4599f8e1707957b9c870b`|{cat}はボールに楽しげな視線を送った。落ち着いた顔にも、遊びへの期待がのぞく。|
|ball|favorable|`0d40e8e66fb05ac09ac81df0b120f90f`|{cat}はボール遊びの調子が合ってきたようだ。誘いへの反応が、少し軽やかになっている。|
|ball|enthusiastic|`9ffebace32c859ddaba9106d54ad2a6a`|{cat}はボールから目が離せない様子だ。オリーブグリーンの目に、遊びの続きを待つ輝きがある。|
|ball|enthusiastic|`7b138dacde3b502ca9b17425859df5fa`|{cat}はボール遊びに夢中になった。ゆったりした顔つきにも、抑えきれない楽しさが表れている。|
|ball|confused|`6915f468fe9554b49fa9583aaccf8c9a`|{cat}はボールを見てから、相手の顔をうかがった。遊ぶきっかけがつかめないようだ。|
|ball|confused|`91697a947a0e570fab018ca191d59e29`|{cat}はボールへの反応をためらった。耳を向け直し、誘いの調子を確かめている。|
|ball|listless|`f8eebbb4c1bb54108e010889be4418fd`|{cat}はボールをぼんやり見ている。目を動かすのもおっくうそうで、疲れがうかがえる。|
|ball|listless|`81a4d03ef8e656c8acac18bd654f3b38`|{cat}はボールに気づいても、反応が遅い。遊びへの期待より、けだるさが表に出ている。|
|plush|neutral|`dc7ecc4804735307bf2ce08464182f74`|{cat}はぬいぐるみへ静かに顔を向けた。遊びの誘いを、ひとまず眺めている。|
|plush|neutral|`d292c8aca9ef514c91bd386f648913c5`|{cat}はぬいぐるみに耳を向けている。興味を大きく示すことなく、落ち着いて様子を見た。|
|plush|favorable|`3f77315e5cb859ebae9aefacd99f93e6`|{cat}はぬいぐるみを見て、楽しそうな顔になった。ゆっくりした応じ方にも、遊ぶ気持ちがある。|
|plush|favorable|`b44c27a27fd95fbf9429e0eebeccba6a`|{cat}はぬいぐるみ遊びに機嫌よく応じた。黒褐色の尾先が、小さく弾むように動いた。|
|plush|confused|`4b148a1f8f3e56c1af369df572c355ac`|{cat}はぬいぐるみを眺めたまま、首をわずかに傾けた。誘い方がしっくりこないようだ。|
|plush|confused|`05009e12ab5f56d88799fd70e5cd879d`|{cat}はぬいぐるみと相手を見比べている。遊びに乗るには、もう少し間が欲しそうだ。|
|plush|listless|`821ef713e85553f383600a5ccdb3cfb7`|{cat}はぬいぐるみに重たげな視線を送った。遊びの誘いにも、疲れた様子が隠れない。|
|plush|listless|`c8beb6a5bff153d084fc0b67785eb1b1`|{cat}はぬいぐるみへの反応が薄い。耳の動きも鈍く、今は遊ぶ元気が少ないようだ。|
|tunnel|neutral|`61e22afc45ce53d6b4b528c75eaf805b`|{cat}はトンネルへ目を向け、静かに眺めた。すぐには気持ちを決めず、様子を見ている。|
|tunnel|neutral|`a318604b13e559499cc2fccdd0c8f883`|{cat}はトンネル遊びに穏やかな反応を返した。関心はあるが、まだ控えめだ。|
|tunnel|favorable|`c8c17e8629aa544190735bd9d0dc59e3`|{cat}はトンネルへ楽しそうな顔を向けた。ゆったりした間合いの中にも、期待が見える。|
|tunnel|favorable|`3538591d6db55367a44beaf774548101`|{cat}はトンネル遊びの誘いを気に入ったようだ。耳を向けたまま、いきいきと様子を見ている。|
|tunnel|enthusiastic|`4e6b31a78d8d566ba05fcb03293a6b18`|{cat}はトンネル遊びにすっかり気を引かれている。丸い顔に、次も遊びたいという気持ちがあふれた。|
|tunnel|enthusiastic|`aa2c140e42b15426a9ecedf74f46f0ea`|{cat}はトンネルへ熱心な視線を注いだ。普段の静かな目にも、はずむような楽しさがある。|
|tunnel|confused|`1855208193b75fe8b5e69cab35d6c0fc`|{cat}はトンネルを気にしつつ、相手の様子をうかがった。遊びに応じる調子が合わないらしい。|
|tunnel|confused|`f480c12a03a15be78c034b478a78124f`|{cat}はトンネルへの反応をためらっている。三角の耳にも、少し迷いが表れた。|
|tunnel|listless|`27b67c1f46e05ed2a5d57775519088e6`|{cat}はトンネルを眺める目にも力がない。疲れているらしく、誘いへの反応は小さい。|
|tunnel|listless|`fd95c8feab51590e8364bfe8388df5e1`|{cat}はトンネルへゆっくりと顔を向けた。遊びを始めるには、気だるさが勝っているようだ。|
|brush|neutral|`608c203af7e65ac784a16c8f0d63fc2a`|{cat}はブラシの動きを静かに見ている。手入れの様子を、落ち着いた顔で受け止めた。|
|brush|neutral|`24fe332710a65d4e97010bf033bff7ea`|{cat}はブラッシングに控えめに応じた。三角の耳が、時折ブラシの方を向く。|
|brush|favorable|`9db8347fe5dc505893067682f59f10cb`|{cat}はブラッシングに心地よさそうな表情を見せた。丸い顔の目元が、ゆるやかに和らぐ。|
|brush|favorable|`ed58e79899df5f86b8a709707a463166`|{cat}はブラシの動きに機嫌よく応じている。相手を見る目にも、くつろいだ様子がにじんだ。|
|brush|confused|`55f481c536f15791bff1b393172eb9ed`|{cat}はブラシを気にして、耳を動かした。手入れの調子が合わず、落ち着きどころを探している。|
|brush|confused|`2bd7efd245ba507795347ea1aadc750f`|{cat}はブラシから相手へ目を移した。どう受け止めようかと、少し戸惑っているようだ。|
|brush|listless|`e17a1e237af15bd8b18962cbb0365ce1`|{cat}はブラシの方へゆっくり目をやった。疲れがあるのか、手入れへの反応も乏しい。|
|brush|listless|`3981400a2e0d56a5a57e75f5cca4ba97`|{cat}はブラッシングにもけだるそうだ。いつもの穏やかさに、反応の鈍さが重なっている。|
|voice|neutral|`e84c5a0709c85ad8a9d1d750ac22300f`|{cat}は声を聞いて、ひと呼吸おいてから顔を向けた。落ち着いた目で相手を見ている。|
|voice|neutral|`1cb7ceced27e526daf272b20f460e9d0`|{cat}の耳が声の方を向いた。呼びかけには気づいたが、表情は穏やかなままだ。|
|voice|confused|`cefb660958a750568e2b956ab221c8ef`|{cat}は呼びかける相手を、不思議そうに見た。声の調子をどう受け取るか迷っているらしい。|
|voice|confused|`138313daeefe5fcc947543b520ae8dbe`|{cat}は声に耳を向けたものの、反応が定まらない。丸い顔に、小さな戸惑いが浮かんだ。|
|voice|listless|`11070973c9c459168f9f419c82c01c84`|{cat}は声の方へ重たげに目を向けた。疲れているらしく、応じるまでにも間がある。|
|voice|listless|`71b87273c7f95f2a8ecbe1d0b31eb3c3`|{cat}は呼びかけを聞いているようだが、耳の動きはわずかだ。けだるさが表情ににじんでいる。|
|presence|neutral|`613cfd92594c5d2ba59e6359713918d5`|{cat}は見守る相手に一度目を向け、静かに過ごしている。この距離を穏やかに受け止めているようだ。|
|presence|neutral|`acbc9204e29154c2915b7704c1cf2850`|{cat}はそばの気配に気づき、耳を向けた。急いで応じることなく、落ち着いている。|
|presence|confused|`f8a6947d4c585a92b5606af9701fee2b`|{cat}は見守る相手へ、確かめるような視線を送った。静かな間合いの取り方に迷っているようだ。|
|presence|confused|`28388509739f578e8e1839dff0644c53`|{cat}はそばの気配を気にしている。相手を見る顔には、少し落ち着かなさがある。|
|presence|listless|`227f236cf0c75c24a235b227837dc7c7`|{cat}は見守る相手へゆっくり目を向けた。周りを気にする元気も少ないらしく、けだるそうだ。|
|presence|listless|`81d1a8b1bcb2551f9a11fa62299dc3ea`|{cat}はそばの気配への反応も鈍い。静かに過ごす顔に、疲れが見える。|
|pause|neutral|`b0afd6b3ec1c5f968896088474eda1ea`|{cat}は休憩のひとときを過ごしている。穏やかな丸い顔で、周りの様子を眺めた。|
|pause|neutral|`bf7c3a468c90533e90864215d6d05b44`|{cat}はひと息つき、ゆったりと耳を動かした。何かに急かされることなく、静かにしている。|
|pause|listless|`9a8967122c9a5e8e95c5f83a5e3ba9dd`|{cat}は疲れた様子で休んでいる。オリーブグリーンの目にも勢いはなく、反応はゆっくりだ。|
|pause|listless|`14b27c5a6dd658179d89c077ed53575e`|{cat}はけだるそうに休憩している。尾の先の動きも小さく、今は静かに過ごしたいようだ。|

## 容量・保持した差分

開始→終了（bytes）：

|対象|開始|終了|増減|
|---|---:|---:|---:|
|build|5,046,668,269|5,046,668,269|+0|
|logs|18,358,473,210|18,358,473,210|+0|
|generated|3,277,372,585|3,277,372,585|+0|
|content_images|1,276,729,646|1,276,759,681|+30,035|
|drafts|312,776|342,811|+30,035|
|library|7,358,000,371|7,358,000,371|+0|

増加は今回の採用本文・下書きのみ。大きな生成物増加なし。[容量記録](Results/MugiNormalNarratives/capacity.json)。StandaloneBuildはLatest成功ビルド1組とRuns、既存build.lockのみでPendingなし。削除・ビルド・UnityプロジェクトやLibraryの複製は行っていない。制作資料・画像比較・再現スクリプトの容量は同記録へ別集計。

ToolのAllCats／CatImageSchedule／CatImageTodo／presentation／CafeNarrativeCatalog、Rin／Yuki／Aki画像作業、Pythonの猫設定・募集設定・台帳、Unityの既存並行差分を保持。コミットは今回の本文・再現スクリプト・制作記録とDocsの進捗追記だけとし、pushしない。
