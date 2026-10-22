# 小さな美術館・来館者との交流ADV

実装日：2026-10-22。対象は `small_art_museum / museum_visitor_request`。派遣中の選択は現行8種類すべて共通ADVに接続済み。通常派遣の往復は別範囲で、今回追加していない。

## 定義・本文・表示

現行rulesの依頼は「来館者との交流をお願いされました」。既存の `accept`「来館者との交流を引き受ける」と `decline`「交流を見送って予定どおり終える」をそのまま使用する。標準設定の受諾追加分は金銭100・疲労10・ストレス10、見送りは0。表示は固定値ではなくLocalCafeDispatchのDTO、DispatchChoiceReason、保存された回答時変化から取得する。発生・段階解放・必要特性「マイペース」・出発条件・2日の日程・歓迎補正・数値・バランス・本番rulesは変更していない。

本文の正本は `Runtime/Standalone/CafeEncounterConversation.cs` の `Lines`。導入4節：実猫名と美術館からの相談／担当者の来館者交流依頼と帰還時のお礼／現在の状態と両案比較／疲労・ストレスと両択の用品受取時点確認。結果各3節：プレイヤーの確定回答／美術館担当者の受領／確定した交流受諾または見送りと効果時点。担当者・来館者の固有名、作品名、猫の過去、交流の成功・能力・成績を追加しない。猫名と既存画像・欠落時の共通代替を使用。新規画像・音声・実行時LLMなし。

回答時は疲労とストレスの丸めた変化のみ反映し、所持金を動かさない。金銭と既存用品ブラッシングセットは帰還受領時。どちらの選択でも既存用品1件、依頼に追加用品はない。選択欄は可否と理由／帰還金銭前後・増減／疲労・ストレス前後／反映時点を既存用品対応表示で示す。結果にも保存済みの前後値と確定金銭額を表示する。

共通AcquisitionConversationPanelの最大3行ページ、文字速度、全文表示・次へ・バックログ・オプション・保存を再利用。美術館とそのExtraにThreeLinePagesを有効化。閉じた未回答・未読結果は一覧の「派遣中の選択イベント」から再開できる。共通の進行停止理由と解消先を使用し、読了後に自動営業を再開しない。

## 保存・互換・Extra

既存の派遣案件ID＋イベントに対応する会話ID＋分岐＋節・ページ位置を維持。`dispatch.museum_visitor_request.v1` を追加し、既存7種類のIDは変えない。対象判定は派遣先とencounter.idの両方を照合。Validateは案件に対応しないID・分岐・不正位置を拒否し、ロード失敗で現在状態を変えない。複数猫・同じ猫の別派遣は別案件。保存版と効果コマンドは変更しない。

旧未回答は導入から開始できる。旧回答済みでメタがないものは記録表示を維持し、過去会話を既読補完しない。request_id再送キャッシュ・回答済み拒否・UI同フレーム抑止を再利用し、ロード・表示・回想で回答や効果を実行しない。

結果読了したaccept / declineのみExtra登録。未読・導入途中で解放しない。回想は導入4＋該当結果3節。共通回想は閲覧専用で、派遣・金銭・用品・猫状態を変えない。

## 8種類の対応表

全行とも対象判定は派遣先＋イベントID一致。Extraキーは会話ID＋分岐。

| 派遣先 | イベントID | 会話ID | Extra結果分岐 |
| --- | --- | --- | --- |
| shopping_street_event | extra_work | dispatch.extra_work.v1 | accept / decline |
| out_of_town_visit | rest_stop | dispatch.rest_stop.v1 | rest / continue |
| cat_photo_studio | extra_photo_session | dispatch.extra_photo_session.v1 | accept / decline |
| cat_exercise_class | exercise_program | dispatch.exercise_program.v1 | accept / decline |
| quiet_reading_salon | reading_extension | dispatch.reading_extension.v1 | accept / decline |
| cat_product_trial | extra_product_trial | dispatch.extra_product_trial.v1 | accept / decline |
| cat_product_trial | product_model_request | dispatch.product_model_request.v1 | accept / decline |
| small_art_museum | museum_visitor_request | dispatch.museum_visitor_request.v1 | accept / decline |

## 検証と残課題

検証用ルールでランダム店舗イベント・解放待ちのみ省略し、必要特性を検証猫へ設定する。本番ルールは編集しない。美術館と必要な既存7種類の回帰に限定。全目標の長期通し・標準バランス・総合確認は予定の会話追加後に実施する。

C#共通回帰1,248項目成功。8種類×両択×負担上下限近傍、導入・結果全節JSON往復、即時入金なし・連打相当の重複回答、読了Extra7節を確認。美術館の帰還金銭・用品1件・再受領無変更、同じ猫の次回案件、美術館とサロン同時待ち・回答順・営業中不可・日程停止・別会話ID拒否・Extra抽出無変更を含む。

再実行：既存 `dotnet run --project cat_cafe_sim_tool/tests/standalone/StandaloneChecks.csproj -- --conversation cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/rules.json`。ビルドは指定build_standalone.ps1と `-ExecuteMethod CatCafePreview.Editor.EncounterConversationVerification.VerifyAndBuild`。Playerは `--catcafe-encounter-conversation-check <専用出力> --encounter-museum`、別プロセスは `--encounter-restart`、実操作保存slot8の復元は `--encounter-manual-load`。証跡は作業ルート `Logs/EncounterMuseum`。

残課題：通常派遣8か所と有力者3人の往復、既知の旧統合描画ランナーの店舗未読結果待ちへの追従、全猫・全解像度・全イベント組み合わせ。これらは今回の完了条件へ混ぜない。

### 最終結果・実画面

Editor290項目、配布Player初回399・別プロセス463・最終別プロセス463項目成功。8種類の全選択で導入途中・選択前・結果途中・読了の実保存、旧未回答／回答済み、不正分岐ロード時の現状態保持、同一request_id再送／新IDでの重複拒否、帰還金銭・既存用品1件と再受領無変更を確認。UIは一覧再開・旧画面非重複・同フレーム次へ／回答連打・結果待ち・バックログ高さ・3行本文の高さとスクロール不要・読了後の自動営業なしを確認。Unity Error／Exception／Assertなし。

computer-use実操作：1280×800で両択の金銭437.5→537.5／437.5、疲労・ストレス0→10／0、両択の既存ブラッシングセットと帰還受領時点を確認。受諾の結果node0を専用slot3へ保存し、同じ専用保存をslot8へ用意して別プロセス1024×768で復元した。共通設定・バックログ・担当者名・結果読了から営業準備へ復帰、所持金940を確認。slot2を実ロードし、見送りを回答・読了。結果の確定負担0→0・帰還金銭437.5が3行内に収まり、所持金940・営業未開始へ戻った。Extra一覧は美術館の読了2分岐のみ（2/60）、他会話は未解放。見送り回想が導入から開き、保存操作・回答選択がないことを実画面確認。専用Extra JSONも美術館accept/decline各7節の2件のみを照合した。

証跡：`Logs/EncounterMuseum/core-result.txt`、`editor-result.txt`、`Player/result-first.txt`、`result-restart.txt`、`result-final.txt`、各Playerログ、`manual-verification.txt`、専用実セーブ。最終ビルドログは `Logs/StandaloneBuild/Runs/20261022-115045-861-6d9cad95d7a1464d869f37d3c536ca54.log`。

通常権限の初回ビルドはライセンス接続待ちで199終了。権限付き実行では検証用タマの特性変更が郊外試験へ干渉して失敗したため、美術館ケースだけの設定へ修正した。終了済みUnityの残留ロックをプロセス不在確認後に除去し、同じ指定入口で再実行して成功した。本番ルール・特性は変更なし。ビルド失敗中は直前の成功Latestを保持した。

### 容量・コミット範囲

| 対象 | 開始bytes | 終了bytes |
| --- | ---: | ---: |
| Logs/StandaloneBuild全体 | 8,846,738,367 | 8,847,550,043 |
| 固定Latest | 8,757,700,876 | 8,757,702,412 |
| 既存Unity Library | 12,011,063,723 | 12,011,046,005 |
| 今回の専用検証記録 | 0 | 約7,555,922 |

ビルド領域+811,676 bytes（主に実行ログ3本）、Latest+1,536 bytes、Library−17,718 bytes。大きな容量増加なし。Latestは156ファイルの一組のみ・Pendingなし、Runsに実行ログを保存。ZIP・Unityプロジェクト・Library複製なし。通常セーブ・素材・他作業成果物を削除していない。計測JSON・記録追加後の専用検証容量は数KB増える。今回の検証Playerは終了済み。

今回のソース・検証拡張・本資料・派遣二択資料の追補だけをツールGitへコミットする。作業前からのDocs/AllCats、presentation、rules、LocalCafeBackend、猫描写関連コード・素材・画像検証等は含めない。上位共有DocsのDevelopmentStatus／EventConversationInventoryはリポジトリ外のため更新のみで、ツールコミットには含まれない。
