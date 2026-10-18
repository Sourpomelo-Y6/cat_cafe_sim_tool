# 通常派遣「近所のお店への訪問」の会話（2026-10-18）

受け入れ依頼、常連紹介に続く「やさしいADV」の第3弾。別スレッドの休業画面修正 6b657be を取り込んだ状態で開始し、その表示情報・停止理由・イベント再開導線を維持する。参照は作業ルート Docs/FutureDevelopmentPlan.md、UnityStandaloneDispatch.md、既存の派遣・一般派遣画面・保存と、UnityIntakeConversation／UnityRegularIntroductionConversation。

## 遊び方と範囲

既存の一般派遣画面で猫と行き先を選び、「出発内容を確認」から全画面の導入へ進む。対象は neighborhood_visit 一種類だけ。出発前4節→出発／今回は出発しない→結果3節／2節→通常画面、派遣中の状況説明3節、帰還の導入3節→報酬受取／あとで確認→受取結果3節→通常画面を接続した。猫選び、派遣先比較、他の行き先の確認画面は既存の仕組みを使う。

猫名・猫画像、話者名・地の文、既存の図形の店内背景を表示する。相手は「お店の人」とし、既存設定にない名前・関係・訪問先での出来事や評価は確定しない。猫の個性を限定しない穏やかな仕草にして、どの対象猫でも成立させる。登録済み猫画像を優先し、画像がなければ既存シルエットへ戻る。人物画像、新規画像生成、音声、実行時LLMは使わない。

全文表示／次へ、本文クリック、現在の段階の導入と結果のバックログ、保存／読込、途中で閉じる操作は既存のADV表示を共用する。段階の終わりで会話終了を操作し、営業・自動進行は手動で再開する。派遣中の会話は記録の閲覧であり、読むだけでは日数・状態・報酬を変えない。

出発の選択肢に対象・費用・所持金・閉店回数・報酬見込み・疲労などの条件と不可理由を表示する。帰還では固定済み報酬、ケア用品、派遣経験、帰還時ストレスと受取不可理由を表示する。「今回は出発しない」は日程・費用を変えず同日に再相談可能。「あとで確認する」は未受取のまま閉じ、元の帰還待ち制限を残す。初回派遣前にも一般派遣をイベント一覧から開けるよう、候補のある入口を有効にした。

派遣中の記録／帰還の会話は一般派遣の記録選択から開く。帰還待ちや未完了の結果会話はイベント一覧の「一般派遣・帰還」から直接再開する。出発しないと選んだ結果を閉じた場合も、派遣記録がまだ0件であっても再開できる。複数帰還は一件ずつ処理し、他の猫の帰還待ちや加入イベント待ちを会話完了だけで解除しない。

## ルールと共用構造

回答は既存の PythonStateClient.DispatchGeneral／ReceiveGeneral → dispatch_general／receive_general → CafeSimulation の派遣・帰還処理だけを呼ぶ。費用0、既存の日数・疲労・在店・健康・体力・担当可能数・同日一回の条件、固定報酬、歓迎加算、成長倍率、帰還ストレス、用品・派遣経験・収入計上、出勤除外を変更していない。設定JSON、ルールID、乱数、ゲームバランスを変更しない。他の行き先、有力者派遣、派遣中の二択や宿のトラブル、帰還後の紹介は従来画面のまま。

AcquisitionConversationPanel に本文・選択可否・効果・確定コマンドなどの小さな差し替え口を追加し、DispatchConversationPanel が派遣の情報を渡す。図形・マスク・文字送り・バックログ・操作・レイアウトを複製しない。CafeDispatchDialogue はUnityに依存しない本文と表示用進行、LocalCafeDispatchConversation はゲーム回答との同期と保存所有、既存のCafeDispatchはゲームルールを担当する。

表示中は営業と自動進行を止める。出発／出発しない／受取の確定後は閉じても結果会話が完了するまで止め、イベント一覧に結果会話待ちとして載せる。未確定の出発相談を閉じた場合と、派遣中の閲覧を閉じた場合は追加の進行制限を残さない。帰還未受取は既存のゲーム制限が残る。別スレッドの休業画面は共通のConversationProgressReasonを通して派遣の停止理由も表示する。

通常の自動進行は開いた会話で停止する。複数日おまかせでは、既存の帰還待ち停止と管理操作の制限を維持し、おまかせを止めた後に会話を開く。会話の終了で自動進行やおまかせを勝手に再開しない。

## 保存とExtra

CafeSaveEnvelopeへ任意の dispatch_conversations 配列と dispatch_conversation_selected を追加。既存の依頼conversationとregular_conversationは維持する。配列の一行は派遣ID、対象猫、出発日、表示段階と、出発・派遣中・帰還の各CafeConversationProgressを持つ。ゲーム状態・リプレイダイジェストからは分離する。同じ猫の後日の派遣は別のIDになり、前回の閲覧完了を上書きしない。

| 会話ID | 分岐ID |
| --- | --- |
| dispatch.neighborhood_visit.v1.departure | introduction / departed / cancelled |
| dispatch.neighborhood_visit.v1.travelling | travelling |
| dispatch.neighborhood_visit.v1.return | introduction / received |

各段階にnode、completed、viewed_branchesを保存する。同日の再相談ではcancelledの完了を保持し、後のdeparted完了を重複なく加える。帰還のreceivedは最大1件。将来のExtraには会話ID＋分岐IDの集合を渡せるが、今回も営業スロット内の記録のみで、全プレイ共通プロフィール・回想UIは未実装。公開後に節順を変える場合はIDの版と移行を用意する。

出発・受取後はゲーム内の実際の派遣記録へ同期して結果分岐に移す。ロードと再表示でコマンドを再発行しない。同フレームの連続操作、送信中無効化、既存のrequest_id／revision／再送応答、既存の受取済み拒否・ルールの冪等性で二重出発・報酬・用品・経験を防ぐ。

ロード前に対象猫・出発日・派遣ID・行き先・段階・節番号・分岐と実際の状態・閲覧完了を検証し、不正時は現在のゲームと会話を保持する。保存は既存の原子的置換とバックアップを使う。本文途中の保存は同じ節の先頭から。日付が変わった未確定の相談は出発可能な相談として自動表示しない。

会話配列のない旧セーブは受け付ける。対象は既存ローダーが受理するルールID・営業形式の保存であり、別ルールの保存を許可する変更ではない。派遣中／帰還待ちを開くと対応する段階から開始し、未観測の出発・閲覧完了を補わない。受取済みの旧記録は従来の記録表示に戻り、会話完了を捏造しない。Unityが任意のnullクラスを空オブジェクトへ変換する場合は、空IDの各段階をnullへ正規化する。

## 検証と残る課題

C#会話・ルール回帰は311アサーション成功。導入・出発しない・出発済み・派遣中・帰還前・受取済みの節ごとの保存往復、再受取、無変更の閲覧、分岐と閲覧完了の検証、既存の加入・休業の検証を含む。

Editorは既存の受け入れ／紹介の実シーン検証969項目と、派遣会話303項目が成功。Unity Error／Exception／Assertログ0件。非表示の生成、マスクの開閉、Raycast、2匹の同時帰還、加入依頼との重複、旧パネルを会話の上に出さない導線を確認した。EditorのPlay Modeのネイティブ通し確認ではなく、実シーンのEdit Modeでライフサイクルを直接検証する。

最初のEditor試行では、旧セーブ用テストファイルを別スロットへ保存する際にenvelope.slotを変更し忘れ、既存ローダーがUnsupported save envelopeとして拒否した。テストデータだけを修正し、不正会話の拒否検証も正しいスロット番号で実施するようにした。ローダーの制限は緩めていない。証跡はEditor/initial-fixture-slot-mismatch.json。

Playerの初回試行では旧StandaloneDispatchSmokeがテスト用にrules_idへ接尾辞を付け、現在の表示カタログの整合性検証と衝突した。専用ディレクトリと複製ルールの店舗イベント抑止は保ち、テスト用IDの改変だけを廃止した。配布用JSONとルールIDは変更していない。既存派遣テストをEditorでも先に実行して成功することを確認した。証跡はPlayer/legacy-smoke-catalog-id-failure.txt。

Playerは初回47項目、最終ビルドの別プロセス再起動55項目が成功。Unity Error／Exception／Assertログ0件。出発しない→同日再相談→出発、派遣中の閲覧、帰還の保留→再開→受取、request_id再送と新しい要求による二重受取拒否、用品・派遣経験・資金の一度だけの反映、各段階のUnity JSON／実ファイル保存・新しいバックエンドでの再開、旧セーブ、不正会話のロード拒否と現在状態保持、後日の派遣の記録分離、同一フレーム連打、疲労・体力の選択条件、バックログ、通常画面への復帰と手動再開待ちを確認した。

共用ADVを変更したため、同じ機能コードのPlayerで既存の受け入れ依頼37項目、常連紹介177項目、別スレッドの休業画面151項目も成功。いずれもUnityエラー0件。最後の出発見出しの文言調整後もEditor303項目と既存Editor969項目、および上記の再起動55項目を通した。

computer-useでWindows実画面を操作した。1280×800で出発前の保存のロード、「出発する／今回は出発しない」、帰還報酬の受取、結果会話中のスロット8への保存確定を確認。選択肢の出発費用は共通要約の「詳細で確認」を使わず、この行き先の既存ルールどおり0を明示する。見出しも出発前／出発済み／今回は出発しないを区別した。1024×768でPlayerを終了後に再起動し、同じ受取結果節へ復帰、履歴の最後までスクロール、会話終了→営業準備への復帰を確認した。受取保存と再開は資金1065、派遣resolved、return.receivedのnode 0、派遣経験1。結果会話を再開しても受取の選択へ戻らず、通常画面の資金も1065のままだった。

証跡は作業ルート Logs/StandaloneDispatchConversation。core-result.txt、Editor/result.json（303項目）、Player/result-first.txt（47項目）、Player/result-restart.txt（55項目）とplayer-first.log／player-restart.log、IntakeRegression/result.txt、RegularRegression/result.txt、DayOffRegression/result-first.txt。既存Editor969項目は共通検証のRendererFix/Editor/result.jsonへも出力され、今回のコピーをEditor/acquisition-regression.jsonへ保持する。

実画面の画像はPlayer/departure-choice-1280.png、return-choice-1280.png、received-reopened-1280.png、received-reopened-1024.png、history-bottom-1024.png、finished-1024.png。専用の検証セーブと途中の検出記録も同じLogs内に保持し、通常セーブは使わない。最終ビルドログは Logs/StandaloneBuild/Runs/20261018-103249-296-6dde1dc438594b3c98623df66aaab8a3.log 。Sourceと既存Unityプロジェクトへ反映済みで、出力は固定Latest一組。

### 容量管理

| 対象 | 開始時 | 終了時 |
| --- | ---: | ---: |
| Logs全体 | 17,580,252,852 bytes | 17,606,577,802 bytes |
| Unity Library | 10,983,099,755 bytes | 10,983,387,059 bytes |
| 固定Latest一組 | — | 8,247,546,389 bytes / 156 files |
| 今回の検証記録・セーブ | — | 約22.38MB / 135 files |

Logsは約26.32MB増。主に派遣の各段階と既存会話・休業の回帰検証用セーブ、画面証跡、ビルドログ。Libraryは約0.29MB増で、大きなキャッシュ増加はない。計測JSON自体の保存などで数百bytes前後の差がある。Latest一組だけを残し、Pending・追加ビルド・ZIP・新たなプロジェクト／Libraryの複製はない。自分の検証Playerもすべて終了した。ソース・素材・通常セーブや他作業の成果物は整理対象にしていない。

作業前からのDocs/AllCats.md、rules／presentation JSON、猫画像予定資料はコミットに含めない。別スレッドの休業修正を巻き戻さず、今回の会話・接続・検証・記録だけをコミットする。

派遣先の専用背景や人物素材、他の通常派遣・有力者派遣・途中二択・トラブル・派遣紹介の会話本文、Extraの共通プロフィールと閲覧画面は後続。今回の範囲は近所のお店への訪問一本。Pythonとの全期間比較やバランス再評価は再実施せず、既存の派遣回帰・境界と会話接続を検証する。

再実行は作業ルートから dotnet run --project cat_cafe_sim_tool/tests/standalone/StandaloneChecks.csproj -- --conversation cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/rules.json 。ビルドは cat_cafe_sim_tool/build_standalone.ps1 -ProjectPath C:/Users/hi-wa/unity_workspase/CatCafeSim -ExecuteMethod CatCafePreview.Editor.DispatchConversationVerification.VerifyAndBuild 。既存のStandaloneBuildStorageを使い、Logs/StandaloneBuild/Pendingから成功時に移動してLatest一組を置換する。

Player専用検証は --catcafe-dispatch-conversation-check <専用記録ディレクトリ>、別プロセス再開は --dispatch-conversation-restart 、ネイティブ保存したmanual/slot-8.jsonの確認には --dispatch-manual-load を併用する。通常セーブは使わない。

2026-10-18追補：[商店街の派遣中二択「追加の交流」](UnityDispatchEncounterConversation.md)をやさしいADVへ接続した。既存の回答・効果・日程は維持し、両結果と保存再開を追加。
