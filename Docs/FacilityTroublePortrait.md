# R1：施設トラブル会話の猫画像非表示（2026-10-31）

後続確認：固定Latestへ反映済み。通常構成Playerの1280×800で208確認、別プロセス1024×768の保存再開で196確認が成功。R1はユーザー指示で選別ローカルGit保存。コミットIDは進捗文書を参照。push・公開なし。以下の未実施表記は初回ソース実装時点の記録。

ユーザーが非表示案の採用と実装・検証を承認。ランダム猫画像は使用しない。

## 対象と実装

設備トラブル（`equipment.trouble.v1`）の導入・選択肢・結果・イベント一覧からの再開・Extra回想で、猫画像とその枠を非表示にする。背景、案件名／日付、本文、選択肢、バックログ、保存・読込等の操作は維持する。

共通`AcquisitionConversationPanel`に画像表示の切り替えを追加し、非表示の場合は画像参照・代替シルエットの読み込みも行わない。設備トラブル用画面とExtra回想だけで非表示を指定する。画像表示を再度有効にしたときは、既存の画像選択と代替シルエットに戻る。

支援依頼、保護猫紹介などの猫の会話、猫が当事者である派遣トラブル等は従来表示を維持。案件名の札は施設トラブルでも残す。画像制作、ルールID・保存形式・費用・乱数・イベント進行の変更はない。既存保存も新しい表示処理でそのまま読む。

Toolテンプレートと実Unityへ対象3画面のソースを反映。検証入口`FacilityTroublePortraitVerification.Run`とmetaを配置し、Toolのインストール対象に加えた。R1のコミット・Playerビルド・Latest反映は今回未実施。

## 確認

- Unity実コンパイル・専用保存を用いた既存設備トラブル191確認を成功。修理／応急処置／休業、結果会話の保存再開、Extra等の既存検証を利用。
- R1表示29確認を成功。導入・選択肢・結果・保存再開・Extraの画像枠無効／画像参照なし、同じ画面を支援依頼や猫の回想に再利用したときの画像復帰を確認。
- 1280×800／1024×768の導入画面、選択肢・結果・Extra、支援依頼と猫の回想を専用の即時文章表示で描画し、PNGを目視確認。通常Playerの実入力や全会話の網羅確認とは区別する。
- 保存・設定・Extraは`Logs/FacilityTroublePortrait`内へ隔離。実ユーザー保存は読まない。Editorは既存シーンを開いたが保存・再生成していない。並行するスズ制作の素材・登録・台帳・表示定義は編集しない。

証跡：`Logs/FacilityTroublePortrait/result.txt`、`editor-final.log`、`trouble-introduction-1280.png`、`trouble-introduction-1024.png`、`trouble-choices.png`、`trouble-result.png`、`trouble-extra.png`、`support-unchanged.png`、`cat-unchanged.png`。

確認の限界：現行LatestはR2反映時のPlayerで、R1未収録。通常Playerでの実入力・別プロセス再開は未確認。将来の統合ビルドは共有の固定Latest手順を使い、並行素材の収録範囲と区別して記録する。push・公開は行っていない。

ソース照合：対象4ファイルのTool／実UnityのSHA-256一致、最終Editorログに例外・C#コンパイルエラー・Assertなし。`Logs/FacilityTroublePortrait/verification.json`を参照。LatestのAssembly-CSharp.dllはR2確認時ハッシュと一致し、R1は未収録。

容量：開始Logs 20,687,117,439→確認時20,688,644,367 bytes（約1.53MB増、専用PNG・保存・検証ログ中心）。Latest 5,696,696,301／Runs 246,995,934 bytesは不変、成功Latest一組、Pendingなし。Libraryは既存Unity起動で8,239,931,136→8,110,987,036 bytesへ更新された。Libraryの手動削除・コピーや容量整理は行っていない。


## 固定Latest・Player確認（2026-10-31）

次作業の指示により既存`build_standalone.ps1`の標準入口・共有`StandaloneBuildStorage.Build`で固定Latestを更新。追加defineなしの通常構成検証Playerで、正式公開物ではない。

- 成功ログ：`Logs/StandaloneBuild/Runs/20261031-212206-176-8e76153089514a53907ff1e13cc0de95.log`。
- 1280×800の専用Player入口で208確認成功。導入・休業確定後の結果会話・イベント一覧からの再開・修理／応急処置／休業のExtra回想に非表示チェックを追加。画像参照null・RawImage無効・枠非表示を確認した。
- 結果会話の1日目案件・結果ノード1を専用slot9へ保存。別PIDの1024×768で196確認成功し、2日目準備中の1日目案件・ノード1・資金940・画像非表示を保持。再開後に実入力で次の本文へ進むことも確認した。
- 1280で実入力によるバックログを開く操作を確認し、両解像度で実Playerの結果会話を目視確認。スクリプトによるUGUIコールバックの確認と、実入力を区別する。Player画像は`Player/manual-ready.png`・`restart.png`、ログは`player1280.log`・`player1024.log`、結果は`result.txt`・`restart-result.txt`。最終両Playerログに例外・Error・Assertなし。
- 検証入口は既存の専用初期条件（加入日・成長閾値等）を使用する。通常バランスの再評価や全自然発生経路の保証ではない。Extra閲覧で資金・状態・会話進行を変えない既存チェックを維持。
- 保存・設定・Extraは`Logs/FacilityTroublePortrait/Player`へ隔離。実セーブは読まない。確認後のPlayerは終了した。

今回の実Unityには、別作業で制作・Unity取り込み・限定検証済みのスズ27画像と表示対応が入っていたため、この通常ビルドも有効21猫パックを含む。スズ側の表示定義・登録対応・制作Statusのハッシュ保持を照合した。スズの素材・台帳・登録処理を編集・コミットしていない。スズ全場面のPlayer再確認を本件で実施したという意味ではない。

`player-verification.json`に実行条件・対象ソースのTool／実Unity一致・Playerアセンブリhashを記録。R1は後続のユーザー指示により選別ローカルGit保存を実施。スズ制作の素材・登録・台帳・共有表示定義は除外して作業ツリーに保持する。保存時の追加起動・ビルドは行わない。ビルドZIP・Unity／Libraryコピー・公開・pushなし。

容量：開始Latest 5,696,696,301→終了5,725,060,149 bytes（+28,363,848、約28.36MB）。既存スズ素材の今回収録とコード差分を含む。開始Logs 20,688,645,271→確認時20,719,186,610 bytes、Runs 246,995,934→248,180,989 bytes。Library 8,110,987,036→8,139,165,274 bytes。generated 3,277,372,585 bytesは不変。成功ビルドはLatestの156ファイル一組、Pending／Previousなし。容量整理・素材削除なし。
