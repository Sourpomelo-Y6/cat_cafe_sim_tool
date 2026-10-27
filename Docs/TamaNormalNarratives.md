2026-10-28 Unity統合追補：**本資料の採用済み本文は新パックへ収録済み。取り込み待ち0。** 全体で追加176条件362本文、有効435条件1123本文。既存画像・人物・ID対応を保持し、実カタログの全追加条件選択、Editor Canvas、固定Latestでの代表表示、専用セーブの別プロセス再開を限定確認。[統合結果・確認段階・未確認・容量](NarrativeUnityIntegration.md)。以下の「Unity取り込み待ち」「未確認」は制作当時の記録であり、最新状態は統合記録を参照する。自然プレイ全件・総合確認を完了扱いにはしない。

# タマ：6動作の通常反応文

2026-10-27。制作ID `cat-63894eff20794809aa5255290c1ae81e`、ゲームID `cat-tama`、画像ID `playtest-tama`。採用プロフィールは「活発な探検家」。対象はボール・ぬいぐるみ・トンネル・ブラッシング・声かけ・見守りの通常場面、操作共通。既存のねこじゃらし・なでる・休憩、他猫・人物・特別場面は変更しない。Tool登録とUnityへの反映を分けて扱う。

## 到達性と制作条件

現行Python `TypesInteraction` とUnityテンプレート `CafeSimulation.StepExchange` の通常判定を照合。好みゼロ→開始時体力20以下→得点6未満→18以上→10以上→通常の順。タマの好みはball 1.7、plush 0.8、tunnel 1.8、brush 0.5、voice 0.8、presence 0.4。強さの好みはgentle 0.8／standard 1／active 1.3。連続減衰0.2、下限0.4、グループ切替倍率1.2。

Pythonの個性はfrozen dataclassで、台帳から交流設定へ復元する。Unityの初期猫は `Definitions` がルールの猫定義を参照し、成長・習熟・設備倍率は反応決定後の得点へ作用する。通常の営業・休憩・交流・保存復元で、タマの好み配列を変える経路は今回のコード照合では見つからなかった。体力、直前反応、連続回数、最終交流グループは変化するため、初回得点だけでは判断しない。最大体力の成長は低体力までの操作数に影響するが、判定境界20や好みを変更しない。

|動作|制作・登録対象|現行採用設定で除外する条件と根拠|
|---|---|---|
|ball|neutral、favorable、enthusiastic、confused、listless|turn_away：動作・全強さの好みが正。|
|plush|neutral、favorable、confused、listless|turn_away：同上。enthusiastic：コンボと切替の同時成立を仮に許す緩い上限でも18×0.9×0.8×1.2＝15.552＜18。|
|tunnel|neutral、favorable、enthusiastic、confused、listless|turn_away：動作・全強さの好みが正。|
|brush|neutral、confused、listless|turn_away：同上。favorable／enthusiastic：上限12×0.9×0.5×1.2＝6.48＜10。|
|voice|neutral、confused、listless|turn_away：同上。favorable／enthusiastic：上限12×0.7×0.8×1.2＝8.064＜10。|
|presence|confused、listless|turn_away：同上。neutral／favorable／enthusiastic：上限12×0.5×0.4×1.2＝2.88＜6。|

22条件の短い実step到達例を[reachability.json](Results/TamaNormalNarratives/reachability.json)へ保存。[再現スクリプト](../check_tama_reachability.py)はルール・個性・状態を直接書き換えず、初期体力100／35／15から合法操作を進め、対象反応が通常場面であることを確認する。

- ball：初回direct 20.4で熱心、初回adapt 17で好意、direct4回目8.16で通常。体力35からdirect3回→pause→adaptで戸惑い4.352。
- plush：初回direct 8.64で通常、adapt2回目5.76で戸惑い。voice direct→plushへ切替→directは10.368で好意。
- tunnel：初回direct 23.76で熱心、adapt2回目15.84で好意、direct4回目9.504で通常。体力35からdirect3回→pause2回→adaptで戸惑い5.0688。
- brush：初回direct 5.4で戸惑い。teaser direct→brushへ切替→directは6.48で通常。
- voice：初回direct 6.72で通常、adapt 5.6で戸惑い。presence：初回direct 2.4で戸惑い。
- 全6動作：初期体力15から切替→directで低体力。

pauseは開始時体力で低体力反応を決め、その後2回復する。このため20を超えた直後にも直前反応がlistlessとなり、adaptのgentle補正と連続減衰でball／tunnelの戸惑いに届く。初回値・連続adaptだけを見て除外しない。

未確認：自然プレイの出現頻度、全セーブにおける初期体力・到達列、ユーザーによる設定変更・旧設定・将来の好み変更、Unity画面と配布版での今回本文表示。除外14条件は現行採用設定に限定した判断であり、あらゆる設定で到達不能という意味ではない。到達性未確認を理由に保留した今回の制作条件はない。

## 校正方針

好奇心を視線、耳、首の角度、反応の速さで描く。好意・熱心は遊びへの積極性、通常は観察、戸惑いは調子をつかめない様子、低体力は鈍い反応で区別する。語尾だけを変えず、各条件2案の焦点を変えた。

採用台帳と既存タマ文を読み、準備側の通常姿勢、ボール・ぬいぐるみ・トンネル・ブラシ・声かけ・見守りの実行側画像を照合した。ボール捕獲、ぬいぐるみの抱え込み、トンネルへの侵入・通過、ブラシ接触・毛並み改善を新本文で断定しない。画像は静止画で反応の瞬間を保証しないため、細かな時系列や物の位置を固定しない。声かけに玩具・接触・猫の人間語を入れず、見守りは気配と距離だけを描く。

既存Toolの画像付きプレビューで代表9画面（6動作を含む）を目視確認。全44本文×各有効操作、計136組はプレビューの本文展開と画像読込を機械照合する。実行側の否定反応は準備画像を優先するため、Toolの操作画像解決と同一とは扱わない。プレビュー上の人物セリフ・共通地の文は既存素材であり今回の制作対象に含めない。校正時にplush通常案1の「見比べる」の比較対象が曖昧だったため「様子を探る」に修正した。

## 保存・再現と限定検証

本文の入力は[本文モジュール](../tama_normal_texts.py)、既存Tool編集画面への入力・個別確認登録は[登録スクリプト](../register_tama_normal_narratives.py)。保存JSONを独自形式で直接編集しない。既存対象条件があれば停止し、未登録データで `python register_tama_normal_narratives.py --review` を実行すると保存なしの画像付き校正（F8次条件、F9次候補）が可能。登録は同スクリプトの引数なしで実施する。

採用JSONはGit管理外 `C:/Users/hi-wa/source/codex_repos/CatCafeSim/cat_cafe_sim/saves/content_images/interaction_texts.json`、下書きはGit管理外 `C:/Users/hi-wa/source/codex_repos/CatCafeSim/cat_cafe_sim_tool/user_data/interaction_text_drafts.json`。本文・条件・候補IDを本資料と[登録記録](Results/TamaNormalNarratives/registration.json)に残す。3欄目は空で、採用候補数に加えない。

登録・再読み込み、既存データ保持、候補IDと表示元、フォールバック、容量の最終結果は以下に記録する。Unity取り込み・素材パック書き出し・ビルド・画像生成・ルール変更は行っていない。

## 登録・検証結果

- 採用296→318条件、下書き312→334条件。今回22条件44本文のみ追加。候補ID・本文の重複なし、`{cat}`のみを使用し、名前展開を確認。
- 各条件の下書き保存→登録内容確認→確認登録→編集画面の再読み込み→新規ストア再読み込みに成功。対象条件は操作別を含め既存採用・下書き・生成候補に存在せず、新規制作した。
- 既存採用296条件・下書き312条件の全エントリを前後比較して保持。猫台帳、画像manifest、生成候補278 JSONのSHA256は前後一致。ソラ・ミケ・人物・特別場面も一括登録していない。
- 画像付きプレビュー136組で本文展開・画像読込成功。[登録記録](Results/TamaNormalNarratives/registration.json)に本文・候補ID・画像元・場面キー・プレビュー表示元を保存。
- [登録後の限定照合](../verify_tama_registered.py)で、102組のTool再読み込み・表示元・候補選択を確認。68組は「登録済み／操作共通」で各2候補、未登録34組は「未制作」で候補なし。[照合記録](Results/TamaNormalNarratives/tool-reload.json)。
- [実行時限定fixture](Results/TamaNormalNarratives/runtime-check.ps1)で、既存.NETアセンブリの実 `StepExchange` に22到達列を入力し同じ通常反応になることを確認。Unity有効カタログのメモリ上コピーに今回分だけ加え、204件の候補ID・本文・pool選択／定型フォールバック、6件の操作別優先を確認。未登録時は候補ID・poolなしで、従来の `Compose` 定型本文を保持。[結果](Results/TamaNormalNarratives/runtime-result.txt)。アセンブリSHA256 `E77E4DF261B456159F93C65A69677BED98FFEA4FE4675A9116B68026A64D1629`。新規コンパイル・ビルドなし。
- 上記.NET fixtureは反応とカタログをメモリ上で扱う限定検証であり、Unity Editor画面・自然プレイ頻度・配布版での表示確認ではない。初期の追加照合でreflectionの検索対象をNonPublicだけにしたためpublicメソッドを取得できなかったが、Publicを含めて修正し全件成功。既存の成功済み全動作4テストは再実行していない。

今回44本文すべてUnity取り込み待ち。素材パック書き出し・Unity取り込み・ビルド・画像生成・数値変更は実施していない。既存の取り込み待ち分もそのまま保持。

## 採用本文・候補ID

通常場面・操作共通。条件キーは `cat_response/<制作ID>/<動作>/any/<反応>/<動作>`。各2案、案3は空。反応別の校正理由は前記方針に対応する。

|動作|反応|候補ID|本文|
|---|---|---|---|
|ball|neutral|`fbce743e33ff5ecc871ee1f9f8b2c286`|{cat}はボールに目を留め、次の動きを確かめるように見ていた。|
|ball|neutral|`9be883d7f99c5c5eb3d3da39ebf2a335`|{cat}はボールへ耳を向けた。遊びへの反応はまだ控えめだ。|
|ball|favorable|`fde81ff21c285e699e41608e198c750e`|{cat}はボールの動きにぱっと顔を向け、楽しそうに目で追った。|
|ball|favorable|`b2ca5f64781d596fbfc1430223bbe2d9`|{cat}はボールに興味を引かれたようだ。耳を立て、遊びの様子をよく見ている。|
|ball|enthusiastic|`0f7b8e4fa84c5178ba759844b425028b`|{cat}はボールに目を輝かせ、次はどちらへ動くのかと熱心に見つめた。|
|ball|enthusiastic|`b4096c386b3a57a6a7ea1c485a385866`|{cat}の注意はボールに釘づけだ。遊びの誘いに、すぐさま反応している。|
|ball|confused|`b2ed9d62feba539a845095effa7ae956`|{cat}はボールを気にしつつも、どう応じようか迷うように首をかしげた。|
|ball|confused|`0efc338d6791524cb911d7d7f0966d02`|{cat}の視線がボールと相手の間を行き来した。遊びの調子をつかみかねているようだ。|
|ball|listless|`3fb12fb638b659ec884345d64f42cce2`|{cat}はボールへ目をやったが、疲れているのか反応はゆっくりだった。|
|ball|listless|`3377b2fb492f598c910cdd69f097d147`|{cat}はけだるそうにボールを眺めた。次の動きを探す元気も、今は少ないようだ。|
|plush|neutral|`f56e8fd7f8fb5b009bf35b2bc8a52a91`|{cat}はぬいぐるみの様子を探るように、首の角度を少し変えた。|
|plush|neutral|`775a81743cd654bb95be3248c2da32bc`|{cat}はぬいぐるみに目を向けたまま、遊びの様子を確かめていた。|
|plush|favorable|`e85fee237bb355fea9bf4c190b93ea38`|{cat}はぬいぐるみに興味を示し、耳を立てて遊びに応じた。|
|plush|favorable|`b848af6f896f5fbdb0006fffc006572b`|{cat}はぬいぐるみへいきいきと顔を向けた。どんな遊びになるのか楽しみなようだ。|
|plush|confused|`844612246b1d573abba4b2f3ed83834d`|{cat}はぬいぐるみに注意を向けたが、どう遊ぼうか迷っているようだ。|
|plush|confused|`4d1619d50958590c9b3bfc02c8f75d8f`|{cat}はぬいぐるみと相手を交互に見た。好奇心はあっても、遊びには乗りきれないらしい。|
|plush|listless|`9948c06f27875738af7e6130f43cdddd`|{cat}はぬいぐるみにゆっくり目を向けた。遊びを確かめるにも、少し疲れているようだ。|
|plush|listless|`f644a48da97257d1bbb2ba81b2074b21`|{cat}はけだるそうで、ぬいぐるみへの反応にも勢いがなかった。|
|tunnel|neutral|`c1e81d9ec9125c8bb455333449e6afb5`|{cat}はトンネルへ注意を向け、遊びの様子を見定めていた。|
|tunnel|neutral|`4b1ecdba50ff5c248d070342aec79612`|{cat}はトンネルを気にしながら、首を少し傾けて様子をうかがった。|
|tunnel|favorable|`31060c29e81e57238bd57783700d3c6b`|{cat}はトンネルに興味を示し、耳を立てた。探検の誘いを楽しんでいるようだ。|
|tunnel|favorable|`943d8dac19d25df19cf6e62302d81a27`|{cat}はトンネルへ弾むように顔を向け、遊びに好奇心をのぞかせた。|
|tunnel|enthusiastic|`3af29ce74db75517b552c4033e778936`|{cat}の目がトンネルに釘づけになった。何が見つかるのかと、熱心に様子を探っている。|
|tunnel|enthusiastic|`47b05e3d7fb15682bcb6e046f61068ef`|{cat}はトンネル遊びの誘いに素早く応じた。新しい発見を待ちきれないような顔だ。|
|tunnel|confused|`575b48ff473f5011a68af655589ee67d`|{cat}はトンネルを気にしているが、どう応じようか迷うように動きをためらった。|
|tunnel|confused|`7302871142ee5240a7beaec92f308bf2`|{cat}はトンネルと相手に視線を行き来させた。探検気分には、まだなれないようだ。|
|tunnel|listless|`48f79574b16c575fbce8f92959425b0f`|{cat}はトンネルへ目をやった。探検するよりも休みたいのか、反応は鈍い。|
|tunnel|listless|`2214b8f90e2a53a28dc12e3a8e908e4f`|{cat}は疲れた様子で、トンネル遊びの誘いにもゆっくりと反応した。|
|brush|neutral|`05d16e27f3685dd398128958e1190625`|{cat}はブラシの動きを確かめるように見つめ、ひとまず様子を見ていた。|
|brush|neutral|`af2e83eb4503503ca6b16395cd850d17`|{cat}はブラシへ耳を向けた。気にはなるが、大きな反応は見せていない。|
|brush|confused|`9c513db085085c2fab062d5290170d1c`|{cat}はブラシと相手の顔を見比べた。どう応じればよいのか迷っているようだ。|
|brush|confused|`2993349297a75273a33c9e69a7aafbca`|{cat}はブラシに気を取られ、首をかしげた。遊びの誘いとは勝手が違うらしい。|
|brush|listless|`84a58c971acd5a33ae991899c093d5c0`|{cat}は疲れているようで、ブラシを目で追うのもゆっくりだった。|
|brush|listless|`acd1465b8690593890816dbe00e55d43`|{cat}はけだるそうにブラシへ目をやったが、反応は小さいままだった。|
|voice|neutral|`71c5fc4b7ee358cf8d0dcd9cc6945715`|{cat}は声の方へ耳を向け、何だろうという顔で聞いていた。|
|voice|neutral|`01f825f7fd065f5b8511824ac317af53`|{cat}はかけられた声に気づき、相手の様子を確かめるように見た。|
|voice|confused|`180346bd889b53ac8c7970f57417cc7d`|{cat}は声の方へ首をかしげた。気になるものの、どう応じようか迷っているようだ。|
|voice|confused|`e05fbefa6a3055d1acc57719c8d30848`|{cat}の耳が声に反応した。相手を見つめる顔には、少し戸惑いが残っている。|
|voice|listless|`b23a3e05af34525e99ce82853608e86c`|{cat}は声に気づいたようだが、疲れているのか顔を向けるのもゆっくりだった。|
|voice|listless|`67cd60e1dedb5cb5b619252966fbbedc`|{cat}はけだるそうに呼びかけを聞いていた。いつものきびきびした反応は控えめだ。|
|presence|confused|`3ab7381ef1b15f5eae6b8356819f7e9b`|{cat}はそばの気配を気にして、相手と周りを見比べた。どう過ごそうか迷っているようだ。|
|presence|confused|`dd950f535638529f8ff0c08b10dff6d8`|{cat}は見守る相手の様子をうかがった。何か始まるのかと、少し戸惑っているらしい。|
|presence|listless|`af5c27eb89cf54b5a88becae00e68ba9`|{cat}はそばの気配にゆっくりと目を向けた。周りを探る元気も少なく、疲れた様子だ。|
|presence|listless|`1cf8b131556c5860bfb168d404e29cc2`|{cat}は静かに見守られながら、けだるそうに過ごしていた。好奇心ものんびり休んでいるようだ。|

## 容量と並行差分

開始→終了（bytes）：固定ビルド全体5,046,668,269→5,046,668,269、Logs全体18,355,554,286→18,355,554,286、Tool generated 3,277,372,585→3,277,372,585、Unity Library 7,100,713,760→7,100,713,760。content_imagesは1,220,761,826→1,220,780,543（+18,717、採用本文の追加）。下書きJSONも今回本文分だけ増加。制作記録・再現スクリプトの容量は[容量記録](Results/TamaNormalNarratives/capacity.json)を参照。大きな増加なし。

固定ビルドはLatestの1組とRuns、既存build.lockのみ、Pendingなし。ビルド・Library複製・容量整理の削除なし。Tool既存のAllCats／CatImageSchedule／CatImageTodo／presentation／CafeNarrativeCatalog差分、Rin／Yuki制作ファイル、Python側の設定・台帳の未コミット差分を保持し、今回のコミットに含めない。
