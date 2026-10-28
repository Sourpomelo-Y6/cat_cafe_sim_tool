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

## 移動対応表

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
