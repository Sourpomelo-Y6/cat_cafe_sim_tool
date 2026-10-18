# 商店街からの猫紹介を「やさしいADV」へ接続

実装・検証日：2026-10-18。[商店街の派遣中の二択](UnityDispatchEncounterConversation.md)に続き、商店街への派遣から帰還した後の猫紹介一本を会話化した。撮影スタジオからの紹介など他の候補は既存画面のまま。

## 会話と画面

導入5節→「迎える／見送る」→各結果4節→通常の営業準備へ復帰する。商店街の担当者、あなたのセリフ、地の文、紹介猫の名前・個性・特徴を全画面で表示する。本文・個性・特徴・特性の表示には既存の候補データを渡す。現行の候補は遊び好きで繊細な猫・白猫・長毛・接客好き。動くものに興味を向けながら、一歩近づいて立ち止まるなど、設定に沿った短い仕草を描く。新たな来歴や担当者の固有名を確定していない。

AcquisitionConversationPanelの暖かな室内、文字送り、全文表示／次へ、スクロール、バックログ、保存／読込、閉じるを再利用する。DispatchIntroductionConversationPanelは候補とシナリオの差し替えだけを担当し、ゲームルールを持たない。猫の表示用IDは既存カタログのintro-shopping_street_eventへ接続。専用画像は未登録のため、現在は既存の猫シルエットを表示する。画像不足でも本文と選択が動作する。新規画像・音声・実行時LLMは追加しない。

選択肢の近くに初期費用、所持金の前後、在籍数／上限、健康・体力全回復・休養予定で加入する効果と選べない理由を表示する。見送りは費用・人気・派遣報酬を変えず、この候補への回答を変更できないことを明示する。回答後は受け入れ済み／見送り済みを見出しで区別する。

右上の「飼育枠を確認」から既存の飼育枠画面へ移動し、拡張後に同じ会話・選択位置へ戻れる。PythonCatAcquisitionNavigationを再利用し、共通ADVにCanRaiseの差し替え口を追加。飼育枠の画面で状態が更新されても、元の会話をその上へ出さない。受け入れ時は既存Watch経路へ加入猫IDを渡し、完了後の紹介記録から猫詳細へ進む既存導線を残す。

## ゲームルールと停止

既存CanRespondDispatchIntroduction／RespondDispatchIntroduction→resolve_dispatch_introduction→CafeSimulationだけを呼ぶ。候補・候補ID・紹介の序数・費用の出発時固定、帰還を受け取ってから営業準備中に紹介する条件、帰還・派遣二択・目標結果・特別予約・信頼回復・店舗イベント・成長などの先行制限、飼育枠と資金の条件を変更しない。

現行の初期費用200と同額では迎えられず、受け入れ後に資金が残ることが必要。加入は既存JoinCatの健康・体力全回復、疲労・ストレス・好感度0、在店休養で、自動で出勤にはしない。派遣報酬の受取と紹介の費用を混ぜない。rules／presentationや既存のバランスは編集していない。

会話中は共通の営業・自動進行停止へ接続する。回答前に閉じても未回答と節位置を保ち、元の紹介待ちによる進行制限が残る。回答後に閉じても結果会話完了まで停止し、イベント一覧「派遣先からの猫紹介」の回答・会話待ちから再開できる。会話終了後も営業・自動進行は手動で再開する。別スレッドの休業画面の停止理由は共通ConversationProgressReasonを通して表示する。別のADVが出ている場合は重ねず、一覧から開く。

## 保存とExtra

CafeSaveEnvelope版1へ任意のdispatch_introduction_conversations配列とdispatch_introduction_conversation_selectedを追加。既存の依頼・常連紹介・派遣往復・派遣中二択の保存は維持する。一行は派遣IDとCafeConversationProgress。後日の派遣は別ID、別候補として保持し、前回の閲覧完了を上書きしない。

| 会話ID | 分岐ID | 記録 |
| --- | --- | --- |
| dispatch.shopping_street_introduction.v1 | introduction | 導入の節・選択直前を保存。閲覧完了にはしない |
| 同上 | accepted | 迎えた結果を最後まで読んだ時に完了 |
| 同上 | declined | 見送った結果を最後まで読んだ時に完了 |

node、completed、viewed_branchesを持ち、会話ID＋分岐IDの完了集合を将来のExtraへ渡せる設計。記録は営業スロット内であり、全プレイ共通プロフィール・回想・Extra画面は未実装。公開後に節順を変える場合は会話IDの版または移行を用意する。

本文途中は同じ節の先頭、選択前は選択画面、回答後は確定した結果から再開する。表示のために回答を再発行しない。共通の同フレーム連打抑止・送信中無効化と、既存request_id再送キャッシュ／回答済みの拒否で二重支払い・二重加入を防ぐ。本文はCafeDispatchIntroductionDialogue、保存所有と実回答との同期はLocalCafeDispatchIntroductionConversation、ルールは既存CafeSimulationに分けた。

ロード前に派遣先・候補を持つ派遣との一致、実際の回答と分岐、節番号、完了記録、重複行、選択IDを検証。不正ロードでは現在のゲームと会話を保持する。会話配列がない旧セーブは受理し、未回答は導入から開始できる。回答済みの旧セーブに未観測の閲覧完了を追加せず、既存記録画面へ戻す。既存のルールID照合・原子的置換・バックアップは維持する。

## 検証

C#会話・ルール回帰390アサーション成功。導入・両結果の各節のJSON保存往復、完了の重複抑止、資金同額・飼育枠不足、営業中の回答拒否、不正結果の拒否、旧メタなし、同じ回答の反復で資金と所属数が変わらないことを確認。別の帰還待ちと紹介が重なった場合、迎える／見送るの両方が先行帰還の確認まで拒否され、拒否で状態が変わらず、帰還後に紹介へ回答できることを確認した。既存の依頼・常連紹介・通常派遣・派遣二択・休業の回帰を含む。

初回C#試行では検証コードがバックエンド用のreceive_generalを直接シミュレーションへ渡して既存の帰還制限に拒否された。実装を変えず、検証を本来のresolve_activity / receive経路へ修正して成功した。

Editorは既存の受け入れ／紹介969項目、近所の派遣303項目と今回バックエンド37項目が成功。今回の実シーンUI検証は配布Playerで実施。Player初回72項目、別プロセス再起動80項目が成功、Unity Error／Exception／Assert 0件。全画面、CanvasRenderer、全文表示／次へ、同フレーム連打、両分岐、資金と飼育枠のボタン可否、相談の閉じる／一覧から再開、結果未完了一覧、飼育枠画面への往復と更新時の前後関係、バックログの長文、会話完了と手動再開待ち、完了後の既存記録画面を確認した。

実ファイル保存では導入途中・選択直前・結果途中・完了の各スロットを新しいバックエンドと再起動Playerでロード。同じrequest_idの再送応答、新request_idの二重回答拒否と状態の無変更、費用・所属・加入状態の一度だけの反映、旧セーブの未回答／回答済み、不正節ロードの現在状態保持、後日の派遣で候補IDと会話記録が別になることを両分岐で確認した。

検証用に複製したルールは店舗ランダムイベントを抑止し、商店街の解放前の待機だけを省略する。接客好きの出発条件、派遣中二択、帰還の受取、候補固定と加入条件・効果は維持する。配布用設定を緩めたものではない。通常の解放まで全行程を進めるバランス検証や全期間のPython比較は今回再実施していない。

computer-useによる実画面：1280×800で選択肢、飼育枠の確認→6から9匹へ拡張→元の選択位置、迎える、結果会話中のスロット8保存を実操作。資金1140から拡張500・受け入れ200を一度ずつ差し引いて440となった。保存はdispatch-rescue-1が一匹だけ在店休養、会話acceptedのnode0・未完了。終了して1024×768で再起動し、同じ結果から再開、長いバックログの末尾へドラッグ、結果最後まで進めて営業準備への復帰を確認。資金440のままで営業は始まらない。さらに選択前スロット2を実ロードし、見送る分岐の結果も確認した。紹介猫のシルエット・名前・個性表示は両画面で欠けずに表示された。

証跡は作業ルートLogs/StandaloneDispatchIntroductionConversation。core-result.txt、Editor/result.txt、Editor/acquisition-regression.json、Editor/dispatch-regression.json、Player/result-first.txt、result-restart.txt、player-first.log、player-restart.log、専用cases／manualの各セーブ。画像はchoice-1280.png、housing-expanded-1280.png、accepted-1280.png、accepted-restart-1024.png、history-bottom-1024.png、finished-1024.png、choice-1024.png、declined-1024.png。通常セーブは使わない。

## ビルドと容量

既存Unityプロジェクトへソースを反映し、cat_cafe_sim_tool/build_standalone.ps1 -ProjectPath C:/Users/hi-wa/unity_workspase/CatCafeSim -ExecuteMethod CatCafePreview.Editor.DispatchIntroductionConversationVerification.VerifyAndBuildでビルド。既存のStandaloneBuildStorageを使い、PendingからLatestへ移動して置換した。出力はLogs/StandaloneBuild/Latest/CatCafeStandalone.exe。ビルドログはRuns/20261018-105539-068-037e9a2937974f09b225e6a185d7ef2f.log。

| 対象 | 開始時 | 終了時 |
| --- | ---: | ---: |
| Logs全体 | 17,610,370,795 bytes | 17,613,388,450 bytes |
| 既存Unity Library | 10,983,619,585 bytes | 10,984,021,668 bytes |
| 固定Latest | — | 8,247,586,842 bytes / 156 files |
| 今回の検証記録 | — | 約2.22MB / 57 files |

Logsは約3.02MB増、主に検証セーブ・画面証跡・ビルドログ。Libraryは約0.40MB増で、大きなキャッシュ増加はない。記録コピーと計測JSONの保存により数KB程度の差がある。Latest一組だけでPendingはない。ビルドZIP、プロジェクト・Libraryのコピーを作らず、素材・通常セーブ・他作業の成果物を整理対象にしない。自分の検証Playerは終了した。

再実行は作業ルートからdotnet run --project cat_cafe_sim_tool/tests/standalone/StandaloneChecks.csproj -- --conversation cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/rules.json。Playerの専用検証は--catcafe-dispatch-introduction-conversation-check <専用出力>、別プロセス再開は--dispatch-introduction-restart、実操作で保存したスロット8へ復帰する場合は--dispatch-introduction-manual-loadを併用する。

猫の専用画像、撮影スタジオの紹介、宿のトラブル、他の派遣二択、Extra・CG一覧・エンディング・全体オプションは後続。別スレッドの休業変更と作業前からの未コミットの猫資料・rules／presentationは保持し、今回の会話・接続・検証・記録だけをコミットする。pushは行わない。

2026-10-18追補：[タイトルのExtra・会話の回想](UnityExtraConversation.md)を実装。営業スロット内の完了記録を独立プロフィールへ結合し、閲覧済み分岐を読み返せる。
