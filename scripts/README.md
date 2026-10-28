# 制作・検証スクリプトの配置

2026-10-28後続：人物別の登録、共通文章の登録・校正、特定制作分の統合出力、検証・容量調査を追加整理。今回32件、前回と合わせて68件。

| 保存先 | 現在の件数・用途 |
|---|---|
| scripts/production/ | 49件。猫・人物の登録、制作済み文章の校正・統合出力 |
| scripts/verification/ | 16件。表示・登録・LLM・サーバー検証、容量調査 |
| Docs/ProductionData/ | 3件。猫の登録元本文 |

通常起動のrun.py、通常出力のunity_export.py、UI・保存処理・モデル接続の共通モジュールは直下に維持。export_standalone_rules.py／export_standalone_presentation.py／convert_python_save.pyも既存の通常運用入口として維持。

register_cat_execution_images.pyは並行制作4件が現在もimportしている共通処理のため、引き続き直下に保持。未コミットのリン・ユキ・アキ・モモの登録スクリプト、資料、Editor検証には触れていない。実Unity／metaも変更なし。

## 新しい呼び出し方

Toolフォルダで `python -X utf8 scripts/production/register_takahashi.py`、または `python -X utf8 -m scripts.production.register_takahashi`。絶対パスの直接実行にも対応する。登録・統合スクリプトは制作時点の前提を持ち、採用データへ書き込むため、整理確認のために一括実行するものではない。

旧パスの互換ファイルは作らず、クリーンな操作資料を新しいパスへ更新。未コミットの日程表などは保持した。素材・画像・記録・Unity参照先は従来どおり。

## 今回の検証

追加32件は移動前Git版とのAST比較により、パス・import・実行入口以外の処理と埋め込みプロフィール／本文が同一。初期客文登録と既存文校正の2本は読み込み時の実行を避けるためmain関数＋直接実行ガードで囲んだ。

累計68件を新しいモジュール名と直接ファイル読込で検証。書込・mkdir・削除・rename・子プロセス・通信を拒否する監査フック下で成功。参照先も検証。`python -B -m unittest discover -s tests -p test_script_layout.py`。Python本体・Toolの追跡済みソースから、移動した旧ファイル名の操作参照が残っていないことも検索した。

登録・出力・検証サーバー・LLMは起動していない。採用データ・ゲーム処理の実行確認ではなく配置の検証。

開始時Logs 20,107,349,955 bytes、generated 3,277,372,585 bytes、Latest 5,506,516,854 bytes。前回終了時よりLogsが2,587,265 bytes増えていたが、今回の作業開始前からの増加。終了時も上記3容量は同値（今回増減0）、Latest成功1組・Pendingなし。ビルド・素材生成・容量整理なし。

---
以下は前回の猫別整理時点の記録。現在の件数・対象範囲は上記を参照。

# 猫別の制作・検証スクリプト

2026-10-28。Tool直下に蓄積していたコミット済み猫別ファイル36件を用途別に移動。

| 保存先 | 用途 |
|---|---|
| scripts/production/ | 個別画像・本文の登録、21ファイル |
| scripts/verification/ | 登録結果・反応到達性の検証、12ファイル |
| Docs/ProductionData/ | ムギ・コハク・タマの登録元本文、3ファイル |

通常のTool起動は従来どおり `python run.py`。個別制作スクリプトは通常起動から呼ばれない。共通の画像登録処理 `register_cat_execution_images.py` は既存の並行制作からも参照されるため直下に維持。人物別スクリプトは今回の猫別整理の範囲外。Unity検証コードは既にEditor領域に分類されており、実Unity／テンプレートとmetaは移動していない。

## 実行場所

Toolフォルダからは、例えば `python -X utf8 scripts/verification/verify_mugi_registered.py` または `python -X utf8 -m scripts.verification.verify_mugi_registered`。絶対パスによる直接実行も可能。登録スクリプトも同様に新しいパスを指定する。

これらは制作時点の前提・固定対象を持つ作業記録であり、一括再実行する保守コマンドではない。登録側は採用素材・下書き・Unityを書き換えるものがある。検証側にも結果JSONを出力するものがある。今回の整理ではmain処理を実行していない。

旧パスの互換ファイルは残さず、既存の操作資料のパスを更新した。古い外部コマンドは下記の対応表で読み替える。ファイル位置から計算する素材・結果・ゲームプロジェクト参照先は移動前と同じ。

## 保持した並行成果

未コミットの `register_rin_images.py`、`register_yuki_images.py`、`register_aki_images.py` と対応資料・Editor検証は元の場所に保持。移動・編集・コミットしていない。並行作業が完了して保存された時点で同じ分類へ移せる。日程表などの既存未コミット資料も変更しない。

## 検証

36ファイルすべてを新しいモジュール名と直接ファイル読込の両経路で検証。書込・mkdir・削除・リネーム・子プロセス・通信を拒否する監査フック下で成功。登録・生成・書き出しは実行しない。テストは `python -B -m unittest discover -s tests -p test_script_layout.py`。

移動前Git版とのAST比較でパス・import・入口ガード以外の処理が同じことを確認。本文3ファイルはバイト単位で同一。ミケ休憩文の旧スクリプトだけはimport時に処理が走っていたためmain関数と直接実行ガードで囲み、登録処理自体を維持した。

開始・終了のLogsは20,104,762,690 bytes。ビルド・素材生成・Unity取り込み・容量整理なし。固定Latestは成功1組、Pendingなし。


## 移動対応表（累計）

機械可読の正本は [relocations.json](relocations.json)。

| 旧Tool直下 | 新しいパス |
|---|---|
| `check_kohaku_reachability.py` | `scripts/verification/check_kohaku_reachability.py` |
| `check_mugi_reachability.py` | `scripts/verification/check_mugi_reachability.py` |
| `check_tama_reachability.py` | `scripts/verification/check_tama_reachability.py` |
| `kohaku_normal_texts.py` | `Docs/ProductionData/kohaku_normal_texts.py` |
| `mugi_normal_texts.py` | `Docs/ProductionData/mugi_normal_texts.py` |
| `register_chiro_images.py` | `scripts/production/register_chiro_images.py` |
| `register_hana_images.py` | `scripts/production/register_hana_images.py` |
| `register_haru_images.py` | `scripts/production/register_haru_images.py` |
| `register_kohaku_normal_narratives.py` | `scripts/production/register_kohaku_normal_narratives.py` |
| `register_leo_images.py` | `scripts/production/register_leo_images.py` |
| `register_luka_images.py` | `scripts/production/register_luka_images.py` |
| `register_mike_ball_narratives.py` | `scripts/production/register_mike_ball_narratives.py` |
| `register_mike_brush_narratives.py` | `scripts/production/register_mike_brush_narratives.py` |
| `register_mike_execution_images.py` | `scripts/production/register_mike_execution_images.py` |
| `register_mike_pet_narratives.py` | `scripts/production/register_mike_pet_narratives.py` |
| `register_mike_plush_narratives.py` | `scripts/production/register_mike_plush_narratives.py` |
| `register_mike_presence_narratives.py` | `scripts/production/register_mike_presence_narratives.py` |
| `register_mike_rest_narratives.py` | `scripts/production/register_mike_rest_narratives.py` |
| `register_mike_tunnel_narratives.py` | `scripts/production/register_mike_tunnel_narratives.py` |
| `register_mike_voice_narratives.py` | `scripts/production/register_mike_voice_narratives.py` |
| `register_mugi_normal_narratives.py` | `scripts/production/register_mugi_normal_narratives.py` |
| `register_sena_images.py` | `scripts/production/register_sena_images.py` |
| `register_sora_ball_narratives.py` | `scripts/production/register_sora_ball_narratives.py` |
| `register_sora_plush_narratives.py` | `scripts/production/register_sora_plush_narratives.py` |
| `register_sora_remaining_narratives.py` | `scripts/production/register_sora_remaining_narratives.py` |
| `register_tama_normal_narratives.py` | `scripts/production/register_tama_normal_narratives.py` |
| `tama_normal_texts.py` | `Docs/ProductionData/tama_normal_texts.py` |
| `verify_kohaku_registered.py` | `scripts/verification/verify_kohaku_registered.py` |
| `verify_mike_brush_narratives.py` | `scripts/verification/verify_mike_brush_narratives.py` |
| `verify_mike_pet_narratives.py` | `scripts/verification/verify_mike_pet_narratives.py` |
| `verify_mike_plush_narratives.py` | `scripts/verification/verify_mike_plush_narratives.py` |
| `verify_mike_presence_narratives.py` | `scripts/verification/verify_mike_presence_narratives.py` |
| `verify_mike_tunnel_narratives.py` | `scripts/verification/verify_mike_tunnel_narratives.py` |
| `verify_mike_voice_narratives.py` | `scripts/verification/verify_mike_voice_narratives.py` |
| `verify_mugi_registered.py` | `scripts/verification/verify_mugi_registered.py` |
| `verify_tama_registered.py` | `scripts/verification/verify_tama_registered.py` |
| `audit_image_storage.py` | `scripts/verification/audit_image_storage.py` |
| `export_completed_narratives.py` | `scripts/production/export_completed_narratives.py` |
| `export_special_narratives.py` | `scripts/production/export_special_narratives.py` |
| `proofread_existing_narratives.py` | `scripts/production/proofread_existing_narratives.py` |
| `register_aoki.py` | `scripts/production/register_aoki.py` |
| `register_fujita.py` | `scripts/production/register_fujita.py` |
| `register_fujiwara.py` | `scripts/production/register_fujiwara.py` |
| `register_fukuda.py` | `scripts/production/register_fukuda.py` |
| `register_goto.py` | `scripts/production/register_goto.py` |
| `register_initial_guest_narratives.py` | `scripts/production/register_initial_guest_narratives.py` |
| `register_ishida.py` | `scripts/production/register_ishida.py` |
| `register_ito.py` | `scripts/production/register_ito.py` |
| `register_kato.py` | `scripts/production/register_kato.py` |
| `register_kobayashi.py` | `scripts/production/register_kobayashi.py` |
| `register_maeda.py` | `scripts/production/register_maeda.py` |
| `register_mori_profile.py` | `scripts/production/register_mori_profile.py` |
| `register_nakamura.py` | `scripts/production/register_nakamura.py` |
| `register_nishimura.py` | `scripts/production/register_nishimura.py` |
| `register_okada.py` | `scripts/production/register_okada.py` |
| `register_sakamoto.py` | `scripts/production/register_sakamoto.py` |
| `register_special_narratives.py` | `scripts/production/register_special_narratives.py` |
| `register_takahashi.py` | `scripts/production/register_takahashi.py` |
| `register_takahashi_narratives.py` | `scripts/production/register_takahashi_narratives.py` |
| `register_tanaka.py` | `scripts/production/register_tanaka.py` |
| `register_ueda.py` | `scripts/production/register_ueda.py` |
| `register_watanabe.py` | `scripts/production/register_watanabe.py` |
| `register_yamada.py` | `scripts/production/register_yamada.py` |
| `register_yamamoto.py` | `scripts/production/register_yamamoto.py` |
| `register_yoshida.py` | `scripts/production/register_yoshida.py` |
| `verify_completed_narrative_import.py` | `scripts/verification/verify_completed_narrative_import.py` |
| `verify_interaction_llm.py` | `scripts/verification/verify_interaction_llm.py` |
| `verify_unity_flow_server.py` | `scripts/verification/verify_unity_flow_server.py` |
