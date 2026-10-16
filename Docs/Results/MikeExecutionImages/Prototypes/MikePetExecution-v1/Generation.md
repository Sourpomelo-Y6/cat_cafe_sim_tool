# ミケ「頭をなでる」実行画像

追補（2026-10-16）：後続の組み込み依頼により、通常／穏やかな実行画像として登録した。[実装・検証記録](../../MikeExecutionImages.md) と [8行動の登録対応](../MikeActionExecution-v1/Registration.json) を参照。以下は制作時点の記録として保持する。

制作日：2026-10-16。制作済み・エージェント目視確認済み。ユーザーの採用判断待ち、ゲーム未登録、実画面未確認。ゲーム組み込み・ビルド・コミットは実施していない。

## 成果物と参照

- 追加素材：`mike-pet-execute.png`。1254×1254、RGB PNG、背景込み・非透過。既存の準備画像と同寸法。
- 準備として保持：`../MikeBasePoses-v1/mike-pet-base.png`。未変更。
- 個体基準：`../CatAppearance-v1/mike-normal.png`。
- 事前確認：`../../CatAppearanceProfiles.md`、`../../FutureDevelopmentPlan.md` の「交流画像の追加：準備と実行、特別な反応」、`../MikeBasePoses-v1/Generation.md` と `Registration.json`、`../MikeReactions-v1/Generation.md`、`../CatAppearance-v1/PetPrompts.md`。従来の甘えた頭なで画像と今回の通常実行画像を区別した。
- 比較表示：`index.html`。元画像を参照する2列表示で、比較用の画像複製は作らない。
- 生成：組み込み `image_gen` 1回。参照2枚を用いた編集。リサイズ・画像加工なし。完全なプロンプトは `Prompt.md`。
- 生成元：`C:/Users/hi-wa/.codex/generated_images/01a143ba-a0bb-79f1-b684-96c3ac6a0cfb/exec-7126882a-7dcc-42a5-b00c-a37d39445b0a.png`。このファイルをプロジェクトへコピーして保存した。

## 比較確認

元画像と生成画像を続けて表示し、次の点を目視確認した。比較ページにも並べて保存した。

| 項目 | 確認結果 |
| --- | --- |
| 同一個体 | 琥珀色の丸い目、薄桃色の鼻、丸顔、小柄な短毛の体格、白い胸・足、猫自身の右顔が橙・左顔が黒、白い額の模様、右に巻いた尾を維持。同じミケとして扱える。細かな毛筋や耳の角度は生成差がある。 |
| 接触 | 頭頂の毛と手の下面が連続して重なり、接触部に陰影と毛の押さえがある。元画像の空隙がなく、優しく頭に手を添えてなでる瞬間と読める。 |
| 手指 | 元と同じ明るい肌色、短い爪、クリーム色のニット袖、右上からの手。見える親指と指・手首のつながりに明瞭な余剰・分裂・不自然な関節はない。奥の指は猫の頭で隠れるため5本全ての独立した輪郭は確認できない。 |
| 表情 | 開いた目・閉じた口・自然な座りで穏やかに受け入れる。自ら強くすり寄る姿勢や恍惚・特別発動の演出なし。通常／穏やかの共用向け。 |
| 背景・構図 | クリーム色の壁、巾木、木の床、セージ色のマット、正面全身、足と尾の配置、暖かな光と画風を維持。手に合わせて片耳の角度などがわずかに変わる。文字・UIなし。 |

## 後日の登録に必要なキーと変更箇所（調査のみ）

| 用途 | 既存キー／登録候補 | 素材 |
| --- | --- | --- |
| 共通・準備の互換維持 | `pet`、明示的な切り替え用 `pet__switch` | 既存 `mike-pet-base.png` |
| 通常実行 | `pet__direct` | 新規 `mike-pet-execute.png` |
| 穏やかな実行 | `pet__adapt` | 同じ新規画像を共有 |

猫IDはゲームの `cat-mike` と制作・表示用 `playtest-mike` を明示的に対応させる。`pet__execute` という新キーは現状の登録対象ではなく、今回必要ない。`pet__connect`、`pet__open_up`、`pet__simultaneous` は今回の素材の登録対象にしない。拒否・回避結果にも一律で接触画像を出さないよう、後日の実装で反応を含めて選択する必要がある。

1. `cat_cafe_sim_tool/interaction_scenes.py` の `MODE_ACTIONS`／`COMBINATION_SCENES` は既に `pet__direct`、`pet__adapt`、`pet__switch` を定義し、`content_images.py` の `SCENE_IDS` が受け付ける。キー追加自体は不要。
2. `cat_cafe_sim/saves/content_images/manifest.json` は両ミケIDとも `pet` のみが基本画像に登録され、今回の3つの操作別キーは未登録。後日の承認・組み込み時に `ImageRegistry.register` で対応付ける。既存 `pet` を実行画像へ置換すると準備が変わるので保持する。
3. Unityの現在の `Assets/Resources/CatCafeContent/catalog.json` は `Imports/4762dd1bd978461bb07c041c59417281` を参照。後日のコンテンツ出力で `bundle.json` の `playtest-mike` の `scenes` と画像ファイルを更新する。画像共有には同じハッシュの画像参照を使用可能。旧 `configure_mike_images.py` は25枚を仮定しており、そのまま今回用に実行しない。
4. `Assets/CatCafePreview/Runtime/LocalCafePlayer.cs` は `mode + "__" + action` を `image_key` とする。`ContentPreview.cs` の `PlayerTexture` はイベント画像→操作画像→モード共通などの順に選択するため、操作別キーがなければ現在の準備画像へ戻る。Python経路も `cat_cafe_sim/cat_cafe_sim/unity_state_server.py` が操作別キーを出力する。
5. `PythonPlayerPanel.cs` は `narrative_image` を `PlayerTexture` より先に表示する。`Standalone/CafeNarratives.cs` は `interaction_texts.json` の `image_bindings` から画像を確定して記録する。組み込み時はミケの `pet__direct`／`pet__adapt` の binding と `resolved_scene_key`・画像参照も再出力して確認する。bundleだけの変更では古い画像が優先され得る。保存済み履歴の `image_resource` は別扱いとし、勝手に書き換えない。

上記パスの `Assets/` は `C:/Users/hi-wa/unity_workspase/CatCafeSim/` 配下。それ以外はこのリポジトリ基準。今回はすべて読み取り調査のみ。

## ファイル検証・容量

- 準備画像 SHA-256：`43A1925B8977AE102B3EDE796B2EC9720C7C02CDA70080FF688B93E06D200B8D`。
- 追加画像 SHA-256：`D819C0931DCC17264D267C3CD6456C1F24814BF4AB9B1ED0EB114167C2CC3247`。
- 作業開始時：`Logs/StandaloneBuild/Latest` 4,184,220,754 bytes、`Runs` 2,168,462 bytes、Pendingなし。ビルドを実行せず保持。
- 素材保存直前の `Docs/Prototypes`：470,057,414 bytes。追加容量は今回フォルダの画像1枚とテキスト記録のみ（最終値は `Storage.json`）。生成元の原本は生成ツールの指示に従い保持。
