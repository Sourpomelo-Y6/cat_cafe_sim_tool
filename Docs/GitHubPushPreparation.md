# GitHubプッシュ前調査（2026-10-23）

## 現在のGit状態

- 対象は `cat_cafe_sim_tool` 内の独立したGitリポジトリ。親フォルダーはGit管理されていない。
- ブランチは `codex/unity-standalone-stage1`、調査時HEADは `e99265b`。
- リモート・追跡先は未設定。`main` 以降に101コミットある。未コミット差分だけでなく、この既存履歴もプッシュ対象になるため、接続先決定後にfetchして比較する。
- 初回調査時点ではコミット・ステージング・プッシュを実施していない。
- 続くユーザー依頼により、調査済みの素材・登録処理・画像解決修正と準備記録をまとめてコミットする。GitHubリポジトリは未作成のため、remote設定・プッシュは保留。

## 未コミット差分

- チロ・レオ・ハナ・セナ・ルカの制作成果物：各27枚の採用PNG、プロンプト、外見、場面対応、検証記録、画像一覧。レオには検証記録に記載された保持候補PNGが追加で1枚ある。素材は削除していない。
- 5匹分の登録PythonスクリプトとUnity Editor検証入口。
- 制作予定表・TODO・全猫一覧、5匹の外見と重複猫名の修正、rules_idの更新。
- 派遣加入時の `dispatch-rescue-N` を派遣元に対応する `intro-*` 素材IDへ解決し、通常・切り替え・特殊反応の画像参照を修正。
- `CafeDetailPresentation.cs` と `CafeNarratives.cs` の改行差分を整理。`rules.json` は既存のCRLFを維持。
- `.gitignore` に `/Logs/` を追加し、実行ログ・ローカルの検証用セーブを候補から除外。

準備記録追加前の未追跡候補は178ファイル、277,775,611 bytes（約264.9 MiB）。100 MiBを超える追加ファイルはない。既存の全ブランチのGit履歴にも100,000,000 bytes以上のblobは見つからなかった。

## 今回の検証

- `python -X utf8 -m unittest discover -s tests -v`：102件、55件成功、GUI環境が必要な47件スキップ、失敗0件。
- sandbox標準一時領域ではファイル置換の権限エラーが発生したため、TEMP/TMPを親の `Logs/ToolTestTemp` に指定して再実行し成功。
- 実行ログ：親フォルダーの `Logs/tool-prepush-tests-20261023.log`。
- 追加JSONの構文、追加Pythonの構文、5匹各27件のプロンプト参照PNGの存在を確認。
- Unityテンプレートのrulesとpresentationのrules_id一致を確認。
- `git -c core.whitespace=cr-at-eol diff --check`：成功。
- Unity実行・ビルドは今回未実施。各制作時の検証結果は `Docs/Results/*Images/Verification.json` と `Status.md` にある。

## プッシュ前に残ること

1. GitHubリポジトリURLと対象ブランチを確定し、remote設定後にfetchして既存履歴を比較する。現在、GitHub側との差分・認証・書き込み権限は未確認。
2. 登録処理は隣接する `cat_cafe_sim` の設定・画像manifestと、`C:/Users/hi-wa/unity_workspase/CatCafeSim` の有効コンテンツパックへ書き込む。これらは本リポジトリに含まれず、登録スクリプトにはローカル絶対パスもある。このリポジトリ単体のクローンでは再登録環境が揃わない。
3. `Docs/Results` は既存方針どおり素材・制作証跡として候補に含める。初回プッシュでは既存画像履歴も転送される。
4. コミット案：実行ログ除外／猫名と外見・制作計画／5匹の素材・登録・検証入口／派遣加入猫の画像解決修正。最後の修正とセナ・ルカ素材の依存に注意してまとめる。
5. 準備完了後に明示したパスをstageし、cached diffを確認してコミットする。プッシュはremote比較後に行う。

## 容量

- 開始時：親 `Logs` は18,603,597,467 bytes、tool全体は3,757,694,674 bytes（無視対象の生成画像を含む）。
- 検証ビルドは `Logs/StandaloneBuild/Latest` の1組のみ。約8,473.55 MiB。`Runs` は約94.35 MiB。`Pending` なし。
- 今回ビルド・Unityコピー・画像生成は行っていない。画像、ソース、セーブ、既存生成物は削除していない。増分は調査記録とテストログのみ。
