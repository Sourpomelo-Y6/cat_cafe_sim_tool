# マロン：準備／通常実行の棚卸しと制作

2026-10-17。画像制作・記録のみ。ゲーム登録・組み込み・ビルド・コミットなし。

ゲームID `regular-rescue-1` → 制作・表示ID `regular-rescue-1`。自動で同一視せず登録時に明示対応する。

## 外見設定

{
  "eye_color": "両目とも明るい緑色",
  "body_type": "中くらいの成猫。丸みのある標準体型、標準的な脚の長さ。短毛で滑らかな輪郭。",
  "markings": "明るい茶トラ。額に濃い橙色のM字、頬に左右二本ずつの縞、脇に細めの濃い橙色の縞。脚と尻尾に輪状の縞。口元・顎・胸・腹は淡いクリーム色。白い斑や白い靴下模様はない。",
  "face_shape": "穏やかな丸顔、標準サイズの三角耳、やわらかな丸い目。鼻はサーモンピンク。",
  "tail": "中程度の長さの細い橙色の尻尾。濃い橙色の輪状の縞、先端も濃い橙色。長毛やふさふさした尾ではない。",
  "notes": "茶トラ・短毛。穏やかで甘える表情。長毛で金色の目のコハクと区別する。"
}

## 行動別棚卸し

| 行動 | 準備候補 | 通常実行候補 | 判断・不足 |
| --- | --- | --- | --- |
| 頭をなでる | [maron-normal.png](../CatAppearance-v1/maron-normal.png) | [maron-pet-base.png](../MaronBasePoses-v1/maron-pet-base.png) | 既存baseの手は頭の毛に接している。開眼した通常反応なので再利用。旧pet画像の閉眼・甘えた反応へ置換しない。 |
| ねこじゃらし | [maron-teaser-base.png](../MaronBasePoses-v1/maron-teaser-base.png) | [maron-teaser-execute.png](maron-teaser-execute.png) | 既存baseとplayは前足を下ろし羽根を眺める。前足で羽根に触れる通常実行を1枚追加。 |
| ボール | [maron-ball-base.png](../MaronBasePoses-v1/maron-ball-base.png) | [maron-ball-execute.png](maron-ball-execute.png) | 既存は前足を下ろして球の横に座る。接触する通常実行が不足。1枚追加。 |
| ぬいぐるみ | [maron-plush-base.png](../MaronBasePoses-v1/maron-plush-base.png) | [maron-plush-execute.png](maron-plush-execute.png) | 既存は鼠の横に座っており接触なし。前足で触れて遊ぶ通常実行を1枚追加。 |
| トンネル | [maron-normal.png](../CatAppearance-v1/maron-normal.png) | [maron-tunnel-base.png](../MaronBasePoses-v1/maron-tunnel-base.png) | 既存baseで胴体が筒の内側、前足が入口に出ている。トンネルへ入って遊ぶ静止場面として再利用。速度や通過方向は断定しない。 |
| ブラッシング | [maron-normal.png](../CatAppearance-v1/maron-normal.png) | [maron-brush-base.png](../MaronBasePoses-v1/maron-brush-base.png) | 既存baseでブラシ毛先と肩の毛の境界が接している。通常のブラッシングとして再利用。 |
| 声かけ | [maron-normal.png](../CatAppearance-v1/maron-normal.png) | [maron-voice-base.png](../MaronBasePoses-v1/maron-voice-base.png) | 既存baseの開いた目・耳・軽い顔の向きで話者へ注意を向ける通常場面として再利用。接触は加えない。 |
| 見守り | [maron-normal.png](../CatAppearance-v1/maron-normal.png) | [maron-presence-base.png](../MaronBasePoses-v1/maron-presence-base.png) | 既存baseの画面端に青いズボンの膝があり、接触せず穏やかに座る。そばで見守る実行として再利用。 |

## 比較確認

基準・既存行動・実行を並べて目視確認。画像比較は [index.html](index.html)。新規画像はすべて1254×1254のRGB PNG、背景込み・非透過で、リサイズ・加工なし。

顔・目の色・模様の左右・体格・毛長・尾の識別点は同一個体として整合。前足・支持脚・尾の重複や明瞭な奇形なし。背景・構図・道具を維持。姿勢変化に伴う毛筋・縞の細部はピクセル一致ではない。

- **頭をなでる（既存再利用）**：既存baseの手は頭の毛に接している。開眼した通常反応なので再利用。旧pet画像の閉眼・甘えた反応へ置換しない。 準備候補は道具・手をまだ出していない中立の基準画像。専用の道具あり未接触準備画像は今回制作しない。現在のbase登録は変更しない。
- **ねこじゃらし（追加制作）**：片前足が羽根へ接触し、反対前足と後肢で支持。茶色と斑点の羽根、暗色の棒、右上の手を維持。見える手指に明瞭な余剰・分裂なし。
- **ボール（追加制作）**：前足が赤い球の上側に接する。反対前足で支持し、頭と視線を球へ向ける。球の質感・色、尾、床・マット配置を維持。
- **ぬいぐるみ（追加制作）**：片前足が布製の鼠の背に接する。反対前足は接地、鼠の耳・鼻・編み尾を維持。頭を下げた自然な遊び。
- **トンネル（既存再利用）**：既存baseで胴体が筒の内側、前足が入口に出ている。トンネルへ入って遊ぶ静止場面として再利用。速度や通過方向は断定しない。 準備候補は道具・手をまだ出していない中立の基準画像。専用の道具あり未接触準備画像は今回制作しない。現在のbase登録は変更しない。 後半身・後足はトンネルで遮蔽されるため全四肢の輪郭は確認できない。入口内で座る遊びを表し、走り抜ける専用画ではない。
- **ブラッシング（既存再利用）**：既存baseでブラシ毛先と肩の毛の境界が接している。通常のブラッシングとして再利用。 準備候補は道具・手をまだ出していない中立の基準画像。専用の道具あり未接触準備画像は今回制作しない。現在のbase登録は変更しない。
- **声かけ（既存再利用）**：既存baseの開いた目・耳・軽い顔の向きで話者へ注意を向ける通常場面として再利用。接触は加えない。 準備候補は道具・手をまだ出していない中立の基準画像。専用の道具あり未接触準備画像は今回制作しない。現在のbase登録は変更しない。 基準との姿勢差は控えめ。発話は静止画で見えないため後日の行動文で補う。大きな耳・視線差を必須にする場合は別途差分を検討。
- **見守り（既存再利用）**：既存baseの画面端に青いズボンの膝があり、接触せず穏やかに座る。そばで見守る実行として再利用。 準備候補は道具・手をまだ出していない中立の基準画像。専用の道具あり未接触準備画像は今回制作しない。現在のbase登録は変更しない。

## 参照・生成

各追加画像は組み込み `image_gen` を1枚ずつ実行し、当該猫の基準画像を第1参照、当該行動のbase画像を第2参照に指定。採用13枚は各1回で目視基準を満たし、修正生成なし。タマは追加生成なし。

完全なプロンプト・2つの参照パス・生成元パスは [Prompts.json](Prompts.json)。参照元と保存画像の寸法・SHA-256は [manifest.json](manifest.json)。生成ツールの原本は保持し、猫別追加素材フォルダへコピーした。

既存のBasePoses/Reactionsのmanifest・prompts・Verification・RegistrationとCatAppearanceの基準／pet／playを照合。特殊の `__open_up` / `__simultaneous` は通常実行へ流用しない。

## 後日の登録

本フォルダのmanifest内 `keys` は提案であり、登録済みという意味ではない。モード共通と `__switch` が準備、`__direct` / `__adapt` が通常／穏やかな実行。同一PNGをキー間で共有する。拒否・回避・疲労・戸惑いに接触画像を無条件表示しない。`__connect` / `__open_up` / `__simultaneous` は今回対象外。

変更対象と現在のミケ限定処理に関する注意は [全体記録](../ExecutionImages-v1/README.md)。保存済み履歴の参照は書き換えない。
