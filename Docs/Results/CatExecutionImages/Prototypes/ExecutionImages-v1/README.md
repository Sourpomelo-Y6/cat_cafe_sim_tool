# 猫7匹：準備と対になる通常実行画像

2026-10-17。画像制作と記録まで完了。ゲーム登録・組み込み・ビルド・コミットは実施していない。

## 対象と結果

「まだ画像が描かれていない猫」は、ミケの追加実行画像の制作が済んでいない7匹として扱った。基準画像自体は8匹すべてに存在する。全56行動を画像と記録で棚卸しし、不足13枚を組み込み `image_gen` で1枚ずつ生成、43場面は既存素材を再利用する判断とした。

| 猫 | ゲームID | 制作・表示ID | 追加画像 | 既存再利用 |
| --- | --- | --- | --- | --- |
| タマ | cat-tama | playtest-tama | なし | 8行動すべて |
| ソラ | cat-sora | playtest-sora | ぬいぐるみ1枚 | その他7行動 |
| コハク | cat-kohaku | playtest-kohaku | ボール・ぬいぐるみ2枚 | その他6行動 |
| ムギ | cat-mugi | playtest-mugi | ボール・ぬいぐるみ2枚 | その他6行動 |
| ナナ | request-cat-1 | request-cat-1 | ボール・ぬいぐるみ2枚 | その他6行動 |
| マロン | regular-rescue-1 | regular-rescue-1 | ねこじゃらし・ボール・ぬいぐるみ3枚 | その他5行動 |
| ココ | visiting-cat-1 | visiting-cat-1 | ねこじゃらし・ボール・ぬいぐるみ3枚 | その他5行動 |

## 素材・棚卸し・比較

追加PNGは各猫専用の `../<Cat>ActionExecution-v1/<cat>-<mode>-execute.png` に保存した。タマのフォルダには記録と比較ページのみ置く。全画像は1254×1254、RGB PNG、背景込み・非透過。元画像は上書きせず、生成原本からバイトを変更せずコピー。各回の参照2枚・生成元・生成プロンプト全文は [GeneratedSources.json](GeneratedSources.json) と猫別 `Prompts.json` に保存した。

[全猫の比較入口](index.html) から、各猫8行動を準備候補／通常実行候補の2列で確認できる。猫別 `Generation.md` に行動別棚卸し、再利用と追加の理由、外見プロフィール、比較結果、制約を記載。猫別 `Comparison.jpg` は追加素材の準備／実行を並べた縮小プレビューであり、ゲーム用素材ではない。比較HTMLは既存素材を相対参照し、コピーを増やさない。

[Inventory.json](Inventory.json) は全56行動の対応表。`identity`、`existing_action`、`preparation`、`execution`、`keys` のパスは**この全体フォルダ基準**。猫別manifestに含む同じパスもこの基準で、ゲーム登録ファイルとして直接読み込ませる形式ではない。

### 再利用の判断

- 頭なで：7匹ともbaseはすでに手が頭の毛へ接している。開いた目の自然な反応を再利用。閉眼して甘える旧petとの差も確認した。
- ブラッシング：7匹とも木製ブラシの毛先が肩の毛と接する既存baseを再利用。人の肌・袖・握りは元素材を維持する。
- トンネル：7匹とも胴体が筒の内側、前足が入口に出ている。すでにトンネルへ入り遊ぶ静止場面として再利用。座って入っている画であり、走り抜ける運動や方向までは断定しない。
- ねこじゃらし：タマ・ソラ・ナナのbaseは前足を伸ばす既存遊び。コハク・ムギはCatAppearanceのplayを再利用。マロン・ココはbase／playの両方が眺める段階なので接触する1枚を追加。
- ボール：タマ・ソラはbaseで前足が球に接しているので再利用。それ以外は未接触なので追加。
- ぬいぐるみ：タマはbaseで前足が鼠に乗っているので再利用。それ以外は未接触なので追加。
- 声かけ：baseの注意を向けた開眼・耳・顔の姿勢を再利用。接触を足さない。準備の基準画像との姿勢差が小さい猫もあるため、発話の意味は後日の行動文と合わせて伝える。
- 見守り：baseに青いズボンの膝が画面端へ入り、猫は触れられず落ち着いて座る。既存の非接触場面を再利用。

すでに実行を描くbaseしかない行動は、道具・手がまだ出ていない基準画像を中立の準備候補とした。道具あり・未接触という専用準備が揃ったわけではない。今回の対象は不足する実行画像であり、専用準備画像は新規制作していない。既存の登録内容を変更する判断ではなく、後日の登録提案として分離した。準備候補と実行候補を同じファイルにして違いを装うことはしていない。

### 比較確認の結果

新規13枚は基準・既存行動画像を目視したうえで生成し、出力を原寸で確認、保存後に準備と並べたComparison.jpgでも確認した。顔・目・模様の左右と配置、毛長、体格、尾、暖かな画風、壁・巾木・木床・セージ色マットが同一個体／同じ場面として整合する。前足と球・鼠・羽根の接触を確認。反対の前足と後肢の支持関係に明瞭な重複・分裂なし。マロンとココのねこじゃらしは元の羽根・棒・手の進入位置を維持し、見える指に明瞭な破綻なし。

13枚はいずれも通常／穏やかな実行で共用する自然な反応。文字・UI・ハート・特殊発動演出なし。修正が必要な明瞭な不備は見つからず、修正生成はしていない。姿勢変化に伴う毛筋・縞の微差はあり、ピクセル完全一致を意味しない。トンネルや毛の奥に隠れた四肢・指の全輪郭は確認できない。既存再利用のねこじゃらしでは準備とplayの尾・猫の大きさに既存差があり、猫別に記録した。

## 事前に確認した資料

- [外見プロフィール](../../CatAppearanceProfiles.md)、[今後の計画](../../FutureDevelopmentPlan.md) の「交流画像の追加：準備と実行、特別な反応」。
- CatAppearance-v1の各猫normal／pet／play、各猫BasePoses-v1の8行動とmanifest・prompts・Verification・Registration。Reactions-v1のmanifest・promptsは特殊用途であることを確認し、通常実行へ流用しない。
- ミケの [頭なで制作](../MikePetExecution-v1/Generation.md)、[7行動の棚卸し](../MikeActionExecution-v1/Inventory.md)・[制作](../MikeActionExecution-v1/Generation.md)・[登録対応](../MikeActionExecution-v1/Registration.json)、[表示記録](../../MikeExecutionImages.md)、[最新の実装追補](../../../cat_cafe_sim_tool/Docs/MikeExecutionImages.md)。ミケは追加制作しない。
- `cat_cafe_sim/config/cafe_playtest_cats.json` の全8匹の外見6項目。ナナは `cafe_intake_request.json`、マロンは `cafe_regular_introduction.json`、ココは `cafe_visiting_cat.json` がゲーム側の正本。マロンの緑眼・短毛、ココの琥珀眼・濃いキジトラを確認した。

## 後日の登録に必要なキーと変更箇所

8モードは `pet / teaser / ball / plush / tunnel / brush / voice / presence`。各猫・各モードについて共通 `<mode>` と `<mode>__switch` を準備候補へ、`<mode>__direct` と `<mode>__adapt` を実行候補へ登録する案。画像とキーの全対応はInventory.jsonと猫別manifestの `keys` に記録。同一PNGは内容ハッシュで共有し、キーごとに画像を複製しない。新しい `__execute` キーは不要。

1. `cat_cafe_sim_tool/interaction_scenes.py` の既存キー、`content_images.py` の `ImageRegistry` と `cat_cafe_sim/saves/content_images/manifest.json` を確認し、上記のゲームID／表示ID双方に明示対応する。IDが同一のナナ・マロン・ココに架空のplaytest IDを作らない。旧configureスクリプトを無条件に再実行しない。
2. `cat_cafe_sim_tool/register_mike_execution_images.py` はミケ専用。今回の画像をそのまま登録できる汎用スクリプトではない。後日の対応では、既存の他猫・特殊反応・保存済み参照を保持しつつ必要な対応だけ追加する。
3. Unity `C:/Users/hi-wa/unity_workspase/CatCafeSim/Assets/Resources/CatCafeContent/catalog.json` が指定する有効パックの `bundle.json` と `interaction_texts.json` の `cat_bindings` / `image_bindings` を更新し、revisionを整合させる。パックIDを推測して固定しない。
4. `Assets/CatCafePreview/Runtime/Standalone/CafeNarrativeCatalog.cs` の `UsePreparationImage` は現在 `cat-mike` 限定。ほかの7匹の通常／穏やかな実行にも、拒否 `turn_away`・戸惑い `confused`・疲労 `listless` で準備へ戻る判定を適用する必要がある。`CafeNarratives.cs` のreceiptと `LocalCafePlayer.cs` のフォールバック、Pythonの `cat_cafe_sim/cat_cafe_sim/unity_state_server.py`、制作ツール側の同等処理、`cat_cafe_sim_tool/unity_preview_template` を合わせて確認する。
5. `PythonPlayerPanel.cs` は保存済み `narrative_image` を優先する。画像キー追加だけでは表示が変わらない場合があるため、今後確定するreceiptと画像bindingを検証する。過去の履歴・保存済み画像参照を勝手に書き換えない。

`__connect`・`__open_up`・`__simultaneous` は今回の制作・登録案の対象外。強い行動・フェイント・停止・拒否結果へ無条件で通常接触画像を出さない。今回これらのコード・登録ファイル・Unityには変更を加えていない。

## 容量と検証

開始時：Docs/Prototypes 479,355,843 bytes、Logs 13,383,956,556 bytes、StandaloneBuild/Latest 4,184,220,754 bytes、Runs 2,168,462 bytes。Pendingなし。

画像13枚の寸法・背景形式・保存元とのSHA-256一致、比較ページの参照先、56行動の準備／実行対応を確認した。最終容量と検証結果は [Verification.json](Verification.json)。ビルド・ZIP・Unityプロジェクト複製・Library生成なし。成功ビルドはLatestの1組だけで、Pendingなしを終了時にも確認。他作業の成果物・素材・セーブは削除していない。

終了時のプロジェクト内増加は約26.9 MiB（PNG 26,084,929 bytesと比較プレビュー・記録）。生成ツール保存先にも同じ13枚の原本26,084,929 bytes（約24.9 MiB）を保持する。Logs・Latest・Runsの容量変化は0 bytes。Verification.jsonのend値は同ファイル自身を書き出す直前の測定値で、最終的な小さな記録ファイル分を除く。
