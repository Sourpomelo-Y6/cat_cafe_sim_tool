# ミケ：準備／実行素材の棚卸し

2026-10-16。画像と制作記録を照合した制作判断。ゲーム未登録。特殊反応は今回対象外。

| 行動 | 準備に使える既存画像 | 実行に使える既存画像 | 不足と今回の対応 |
| --- | --- | --- | --- |
| ねこじゃらし | `../MikeBasePoses-v1/mike-teaser-base.png`：両前足を下ろし羽根を見る | `../CatAppearance-v1/mike-play.png`：前足を上げ羽根へ働きかける | なし。既存実行を再利用。棒色や尾の位置に既存差があるが、同じ個体・背景・遊びの画として成立 |
| ボール | `../MikeBasePoses-v1/mike-ball-base.png`：未接触の球を見る | 通常実行として適切な専用素材なし | 前足で球を転がす実行を1枚追加 |
| ぬいぐるみ | `../MikeBasePoses-v1/mike-plush-base.png`：鼠の前で座る | 通常実行として適切な専用素材なし | 前足で鼠に触れて遊ぶ実行を1枚追加 |
| トンネル | `../MikeBasePoses-v1/mike-tunnel-base.png`：入口の外に立つ | 通常実行として適切な専用素材なし | 入口を実際に通る実行を1枚追加 |
| ブラッシング | `../MikeBasePoses-v1/mike-brush-base.png`：ブラシ未接触 | 通常実行として適切な専用素材なし | 毛にブラシを接触させる実行を1枚追加 |
| 声かけ | `../CatAppearance-v1/mike-normal.png`：正面に座る中立の準備候補 | `../MikeBasePoses-v1/mike-voice-base.png`：顔と耳・視線を右上の話者へ向けて聞く | なし。接触不要。既存baseは制作当初は準備だが、視線による通常実行も十分に表すため役割を見直す提案 |
| 見守り | `../CatAppearance-v1/mike-normal.png`：正面座りの準備候補 | `../MikeBasePoses-v1/mike-presence-base.png`：目を開けて落ち着いて伏せる | なし。見守られて過ごす静的行動として十分。座り→伏せで準備と区別する提案 |

`MikeBasePoses-v1/Generation.md` はbase画像を全て準備・切り替え用として制作した記録。今回は実画像の行動表現も確認して声かけ・見守りの役割を再評価した。過去の記録や登録は書き換えない。`MikeReactions-v1` の `__open_up`／`__simultaneous` は特殊用途として保持し、通常実行へ流用しない。汎用の複数場面比較画像 `MikeChatGPTImages-actions-v1.png` は専用実行画像として使用しない。

使用頻度の実測順位は今回確認した制作資料にはない。ブラッシング→ボール→ぬいぐるみ→トンネルを仮の制作順とし、頻度の事実とは区別する。接触不足を直前の頭なでと同じ基準で解決し、簡単な遊びから空間の遮蔽がある遊びへ進む順。ユーザーに順の確認を提示済み。ねこじゃらし・声かけ・見守りは既存実行を再利用するため生成順から除外。

参照記録：`../../CatAppearanceProfiles.md`、`../../FutureDevelopmentPlan.md` 交流画像追加節、`../MikeBasePoses-v1/Generation.md` とmanifest、`../MikeReactions-v1/Generation.md` とmanifest、`../CatAppearance-v1/PlayPrompts.md`、前回 `../MikePetExecution-v1/Generation.md`。
