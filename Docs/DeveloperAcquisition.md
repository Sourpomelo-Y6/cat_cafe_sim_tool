2026-10-30 観戦モード：[通常画面でプレイを見ながら進める開発専用の観戦自動プレイ](DeveloperWatch.md)を追加。初版は人気開始から8日分の営業・会話・保存再開。内部9ルート検証は比較用に維持。

2026-10-30 収集拡張：[9独立ルートで猫31系統・設備・用品・派遣を通常条件で獲得](DeveloperAcquisitionCollection.md)。既定計画を更新し、担当対象・枠予約・route_id・収集途中の再開を追加。以下は初版の記録。

# 開発専用・通常条件獲得検証（初版）

2026-10-30実装。[設計案](../../Docs/DeveloperAcquisitionTestDesign20261030.md)の段階0～1を中心に、内部コマンドで4開始ルートを通す最小構成。通常のおまかせ・通常ゲームの係数や回答待ち停止は変更しない。公開必須条件には追加しない。

## 実装範囲

- `CATCAFE_DEV_ACQUISITION` を明示した専用Playerだけに収集制御器を含める。通常ビルドではコンパイル対象外。
- 開発専用引数、専用画面（実行／一時停止保存／保存終了）、専用出力を持つ。普通のタイトル・おまかせ設定に追加しない。
- 通常の `CafeSimulation.Execute` と既存 `PlanAutoplay` を使用する独立simulation。資金・XP・フラグ・抽選の直接変更なし。標準ルールとstamina_growthをそのまま使用する。
- 人気／有力者／好感度／自由を独立ルート保存で順番に実行。必須イベント、受入、帰還、成長、派遣途中回答、目標結果を合法操作で処理する。
- 有力者のソラは通常接客でservice専門を育成する。自由ルートでは余裕資金の範囲で代表用品・設備・店猫・飼育枠と近所派遣を試す。
- 猫31系統、設備・用品・派遣・成長・目標の台帳を生成。系統IDと実行時個体ID、一度獲得と現所持、ルートとunionを分離。未獲得のReasonと、獲得根拠のルートを保存する。
- 日数／確定操作数／実時間／全操作ログ容量を上限として停止。失敗・期限切れ・無変化・不明操作を無制限再試行しない。
- 200操作ごとの保存、明示一時停止保存、別プロセス再開。2スロットを交互に保存し、checkpointの原子的置換が完了するまで前のセーブを保持。
- checkpointはルール・素材入力・実行assembly・計画の指紋、セーブSHAと照合。再開直後は停止状態。異なる条件、改変セーブ、同時実行、空でない新規出力を拒否する。

**内部検証でありUIの読了・画像実表示・物理入力は確認しない。** 会話選択のゲーム効果は通常操作で処理するが、回想110分岐・画像既読は `excluded / internal_runner_does_not_observe...` とする。全解放は実装しない。通常画面の会話進捗セーブはこの初版の保存対象ではなく、再開するのは独立した内部検証simulationの完全なゲーム状態・乱数・交流累計である。

ルートの `status=acquired` は選んだ目標達成、または自由営業の指定日数完走を指す。`completion_scope` に明記し、全台帳取得完了とはしない。未獲得項目は残る。初版で猫31系統・全排他回答・全画像を狙う複数収集ルートの自動生成まで実装したという意味ではない。

## 実行

専用Playerのビルド（通常ビルドと同じ固定Latestを置き換える）：

```powershell
cat_cafe_sim_tool/build_standalone.ps1 -ProjectPath C:/Users/hi-wa/unity_workspase/CatCafeSim -ExecuteMethod CatCafePreview.Editor.DevAcquisitionSetup.Build
```

環境により `-UnityPath` でインストール済みUnityを指定。`StandaloneBuildStorage.Build` の共有Pending→Latest処理を使用し、PlayerSettingsの定義は恒久変更しない。通常Playerへ戻す場合は同じスクリプトの既定入口で再ビルドする。

新規4ルートを明示実行：

```powershell
cat_cafe_sim_tool/run_dev_acquisition.ps1 -Start
```

既定計画はseed=0、各ルート150日／100000操作／7200秒、自由30日、全ルートの操作ログ合計256MiB。計画JSONは `Logs/DeveloperAcquisition/Plans`、実行ごとのデータは `Logs/DeveloperAcquisition/Runs/<run-id>`。計画にはソースリポジトリのcommitとdirty状態も記録する。`-PlanPath <絶対JSON>` で全フィールドを指定した計画に差し替えられる。

```powershell
cat_cafe_sim_tool/run_dev_acquisition.ps1 -ResumeDirectory C:/.../Logs/DeveloperAcquisition/Runs/<run-id> -Start
```

`-Start` を省略すると専用の実行／一時停止画面で停止して待つ。Playerの直接起動は `--catcafe-dev-acquisition <絶対run> --dev-acquisition-plan <絶対JSON>`、明示実行は `--dev-acquisition-start`、既存checkpoint再開は追加で `--dev-acquisition-resume`（フラグ）。設計時のcheckpointパス引数案からrunディレクトリ指定へ整理した。通常profileを併用する引数は不要かつ拒否する。`-Wait` はPlayerの終了を待ち、終了コードを表示する。

通常保存先との祖先・子孫関係、リンクを含む出力パス、他QA／HTTP／profile指定との同時起動を拒否する。`CafeProfile.Root` はこの起動時のみrun配下のprofileを返し、営業ログも同Rootを使用する。通常起動では従来の保存先のまま。ゲームの通常sceneは専用検証時だけ非活性にし、プレイヤーのbackendへ接続しない。

## 出力と再開

|ファイル|内容|
|---|---|
|plan.json / manifest.txt / environment.txt|計画、全入力の指紋、runner/assembly/rules/contentの識別|
|routes/<objective>/events.jsonl|session・確定操作連番・日/刻・通常操作・前後状態digest・例外|
|routes/<objective>/saves/slot-1/2.json|検証simulation用の完全状態。通常の保存エンベロープ・数値ビット復元・交流累計検証を使用|
|checkpoint.json / .bak|最後に正常保存できたルート、スロット、操作数、経過時間、セーブSHA|
|routes/<objective>/result.json|各ルートの終了条件と対象別結果|
|summary.json / coverage.csv|各ルート、union、獲得根拠route、未実行ルート、未到達／対象外理由|
|failure.txt|例外の詳細。最後の正常checkpointは保持|

外部から安全に中断する場合はrun直下へ空の `pause.request` ファイルを置く。次の操作境界でファイルを消費し、保存してpausedにする。`--dev-acquisition-start` 起動時はその後終了する。予期しない強制終了後の再開は最後のcheckpointから行う。checkpoint以降に残るログは削除せず、sessionが異なる試行として保持する。獲得集計はログ行数ではなく復元した通常状態から再計算するため二重加算しない。既存通常profile・Extraの自動読込やマージは行わない。

専用起動の終了コードは0=ルート終了条件達成、2=未到達／上限、3=実行エラー、4=初期化／計画不一致。全対象獲得の判定にはsummary/coverageを参照する。

## 検証記録

内部テストは `tests/development_acquisition/DevelopmentAcquisitionChecks.csproj`。標準rulesにstamina_growthを読み、短縮したゲーム係数を作らない。正常/異常系テストの上限や対象日数だけを計画で制限する。

```powershell
dotnet run --project cat_cafe_sim_tool/tests/development_acquisition/DevelopmentAcquisitionChecks.csproj -- cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/rules.json Logs/DeveloperAcquisition/<new-check-dir>
# 同じ出力へ、別プロセスで再現を照合
dotnet run --no-build --project cat_cafe_sim_tool/tests/development_acquisition/DevelopmentAcquisitionChecks.csproj -- cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/rules.json Logs/DeveloperAcquisition/<same-check-dir> restart
# 新しい出力へ4標準ルート
dotnet run --no-build --project cat_cafe_sim_tool/tests/development_acquisition/DevelopmentAcquisitionChecks.csproj -- cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/rules.json Logs/DeveloperAcquisition/<new-standard-dir> routes
```

Checks-07：15確認成功、別プロセス再開2確認成功。明示開始、pause不進行、同seed操作列、保存後60操作一致、計画差替拒否、改変セーブ拒否、二重起動拒否、上限、ログ上限、保存置換失敗時の前checkpoint維持を含む。Standard-02：標準seed0で人気34日目1438操作、有力者51日目1915操作、好感度13日目1291操作、自由31日目1309操作。4独立結果、猫31系統台帳、未獲得が残ることを確認。これは各1seedであり達成率の保証ではない。

Unityの通常defineなし／開発defineありのRuntime+Editor参照コンパイルも区別して確認する。Playerのビルド・実行結果と容量は後記する。

## 後続の範囲

31猫系統の収集担当割当、専門/習熟の全組合せ、排他選択checkpoint分岐、素材スロット別の未登録区分、UIでの会話読了・実表示の観測、物理入力確認は後続。初版はその成功数を捏造せず対象外/未到達を出す。正式公開条件は従来のまま。

## Player確認・容量（最終）

Unity 2021.3.45f2で専用define付きWindows Playerのビルド成功。初回はライセンスIPC接続で終了199、接続可能な起動環境で再実行し成功した。最終ビルドログは `Logs/StandaloneBuild/Runs/20261030-145237-887-4436cd1b8680406cab3e295aa50a179e.log`。通常defineなしのRuntime+Editor参照コンパイルも成功し、そのassemblyに開発runner型がないことを確認。

Player初回は `Runs/20261030-145052-860ed121c2514ba4b1194a845e9a050b` で4ルート完走。最終コードでは `Runs/20261030-145428-ea282ff65df447d2bee0eafa2d735972` の256操作目で外部保存停止→別プロセスで明示開始なしに再開→3秒間操作ログが増えずpaused→明示再開で4ルート完走、終了コード0。各ルートの日数・操作数・最終状態digestが中断なしPlayerと一致した。集計のunionは獲得50、未到達38、観測対象外111（回想110＋画像観測枠1）で、全獲得とは報告しない。

通常保存先 `LocalLow/DefaultCompany/CatCafeSim` の8ファイルは実行前後でパス・SHA256不変。テンプレートとUnity反映6ソースはバイト一致。専用画面の人による見栄え評価、通常UIの自動操作、Windows物理入力による確認は未実施。

容量は開始時StandaloneBuild 5,783,011,583 bytes、Library 7,914,090,446 bytes。最終値は作業終了時の記録を参照。今回も検証Playerは固定Latestの1組のみ、Pending/Previousは残さず、プロジェクト・Libraryの複製やビルドZIPは作成しない。Latestは今回の開発専用検証入口を含むPlayerであり、正式配布の承認を意味しない。

終了時：StandaloneBuild 5785409471 bytes（開始比 2397888 bytes）、今回の検証記録 38961183 bytes、Library 7914527096 bytes（開始比 436650 bytes）。大きなキャッシュ/Player複製の増加なし。
