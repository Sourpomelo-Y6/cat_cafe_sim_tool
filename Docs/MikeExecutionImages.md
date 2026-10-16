# ミケの交流画像：準備と実行の表示

2026-10-17 コミット追補：素材と制作記録を `Docs/Results/MikeExecutionImages/Prototypes` に保存した。登録スクリプトはこのGit管理下の素材を正本として参照する。従来の作業ルート `Docs/Prototypes` の元画像も保持する。ゲーム処理・制作ツール・Unityランタイムを各リポジトリでコミットし、既存のビルド容量管理の変更や別作業の未追跡Unityパックは含めない。現在のローカルパックへの画像登録は `Registration.json` に記録済みで、他環境では既存の有効な交流カタログに登録スクリプトを適用する。

2026-10-16。追加制作した頭なで・ブラッシング・ボール・ぬいぐるみ・トンネルの通常実行画像5枚と、既存のねこじゃらし・声かけ・見守りの画像を、8行動の表示へ接続した。

## 表示

| 場面 | 画像 |
| --- | --- |
| 交流開始・動作の切り替え | モード共通／`<mode>__switch` の準備画像 |
| 通常実行・穏やかな実行 | `<mode>__direct`／`<mode>__adapt` の実行画像 |
| 拒否・戸惑い・疲労の反応 | 実行用の接触・遊び画像を避け、準備画像へ戻す |

声かけ・見守りは正面座りを準備、話者に視線を向ける／落ち着いて伏せる既存画像を実行に割り当てた。ねこじゃらしは既存の前足を伸ばす画像を実行へ再利用。その他の準備画像は従来のbaseを保持する。

強い行動・フェイント・休憩・特別発動の画像割り当ては変更しない。今回の反応による準備画像への切り替えはミケの `direct`／`adapt` だけに限定する。猫の好み・反応条件・消費・ターン数・文章本文は変更していない。トンネルなど実際に戸惑う場合は、実行画像を強制しない。

## 接続箇所

- `register_mike_execution_images.py`：制作ツールの `cat-mike`／`playtest-mike` に共通・切り替え・通常・穏やかの計32キーを明示登録。元の画像は変更しない。同じPNGは登録キー間で共有する。
- 現在有効なUnityパック `4762dd1bd978461bb07c041c59417281` のミケの `bundle.json` 場面と、`interaction_texts.json` の画像bindingsを更新。カタログのrevisionを再計算。パックの複製・参照先の切り替えは行わない。画像は内容ハッシュで保存し、既存の同じ画像を再利用する。
- `Standalone/CafeNarratives.cs`：新たに確定した反応に合わせ、実行用または準備用の画像参照をreceiptへ保存する。
- `Standalone/CafeNarrativeCatalog.cs`：ミケの通常・穏やかな実行で、拒否・戸惑い・疲労なら準備画像を使う判定。
- `LocalCafePlayer.cs`：receiptを使わないフォールバックも同じ反応で `__switch` を返す。
- Python側 `cat_cafe_sim/cat_cafe_sim/unity_state_server.py`：同じ条件で準備用のキーを返す。Unity用テンプレートと実プロジェクトの変更ファイルは同期済み。

保存済みreceiptの画像参照は書き換えない。過去の表示は当時の画像を保持し、今後の行動で新画像を確定する。既存パックID・画像ファイルは保持するため、過去の画像参照を失わない。既に再生中ならUnityの再生を終了して再度開始すると、更新後のResourcesとコードを読み込める。配布済みexeは更新していない。

登録の再実行：

```powershell
python cat_cafe_sim_tool/register_mike_execution_images.py --project-path C:/Users/hi-wa/unity_workspase/CatCafeSim
```

制作画像・参照元・プロンプトは `Docs/Prototypes/MikePetExecution-v1` と `MikeActionExecution-v1`。今回の全キー・生成画像参照・更新前JSONバックアップ先は後者の `Registration.json`。他の猫・特殊反応・採用済み文章が不変であることを照合した。

## 検証

- 専用.NET検証128 assertions成功：8行動の準備／通常／穏やかな実行画像の区別、実際の行動からの画像receipt、拒否・戸惑い・疲労の境界fixture、保存再開、表示の読み取りによる状態不変。
- 現在の実カタログで既存96 assertions成功。Python/C#のrevision一致、旧receiptの維持、文章・画像の保存再開を含む。
- 全猫・全モード用の既存fixtureで609 assertions成功。実カタログはこのfixture専用のタマの文章条件を全て含まないため、全モード回帰は専用fixtureで実施した。
- Python交流画面テスト4件成功（反応別キー選択96条件を含む）。制作プレビューのテスト14件中9件成功、GUI5件は既存の条件によりスキップ。
- 登録された24操作キーが準備／通常／穏やかのPNGへ警告なしで解決されること、実プロジェクトとテンプレートのコード一致を確認。
- Unity Editorでの画面・Resources検証入口 `CatCafePreview.Editor.MikeExecutionImageVerification.Run` を追加。既存Editorがこのプロジェクトを開いていたため別プロセスを起動できず、今回の実画面検証は未実施。既存Editorは終了していない。検証入口は場面を開くため、未保存の作業がない状態で実行する。

ゲームのPlayerビルド・コミットは実施していない。.NET検証プログラムのみコンパイルした。記録は `Logs/MikeExecutionImages`。

## 容量

新規PNGの登録用保存とUnity用保存は各5枚（キー間で共有）、合計18,505,912 bytes。更新前JSONバックアップ351,231 bytes。検証ログ・セーブ・小さいfixtureを別途保持し、Unityパックやプロジェクト全体の複製は作らない。

`Logs/StandaloneBuild` は開始時・終了時とも `Latest` 4,184,220,754 bytes、`Runs` 2,168,462 bytes。成功ビルドはLatestの1組のみ、Pendingなし。
