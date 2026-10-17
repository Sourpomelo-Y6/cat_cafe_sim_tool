# 常連からの猫紹介：やさしいADV会話

実装日：2026-10-18。既存の受け入れ依頼に続き、常連からの固定紹介（マロン、regular-rescue-1）を全画面の会話に接続した。参照：作業ルートの FutureDevelopmentPlan、UnityStandaloneIntake、UnityStandaloneRegularIntroduction と、Git管理下の UnityIntakeConversation。

## 範囲と本文

導入5節→迎える／見送る→結果各4節→会話終了、という流れを追加。実際の紹介者の表示名を話者に使用する。既存の「穏やかな甘えん坊・のんびり屋・茶トラ・短毛」に沿う仕草を描き、元の飼い主、保護の経緯、紹介者との私生活上の関係など、未設定の来歴は確定しない。既存の猫画像と代替シルエット、図形の店内背景を利用。新規画像生成・音声・実行時LLMは追加していない。

全文表示／次へ、本文クリック、スクロール可能なバックログ、保存／読込、途中で閉じてイベント一覧から再開する操作は受け入れ依頼と同じ。選択カードの近くに費用・残金・飼育枠・加入状態と選べない理由を表示し、見送りでは費用・人気・常連度が変わらないことを明示する。紹介前の案内、会話完了後と旧セーブの回答済み紹介の情報画面も維持する。

## 共用構造とゲームルール

受け入れ依頼の表示コードを AcquisitionConversationPanel に移し、IntakeConversationPanel と RegularConversationPanel が別のイベントパネル上で同じ表示を使う。本文はUnityに依存しない CafeRegularConversation、保存と回答状態の同期は LocalCafeConversation に置く。受け入れ依頼の会話ID・本文・節順は維持する。

回答は既存の RespondRegular → resolve_regular_introduction を使う。発生条件・紹介者決定・費用200・支払い後にも資金が必要な条件・飼育枠・休養での加入・費用記録・冪等性は変更していない。設定JSONとルールIDは変更しない。帰還・店舗イベントなどの回答順序も既存判定に従う。受け入れ依頼と紹介はどちらからでも回答でき、先に迎えた猫で枠が埋まれば残る相談は見送れる。

会話は一つだけ表示する。イベント一覧から別の相談を開くと元の会話を未回答のまま隠し、位置を保持する。それぞれの表示中フラグを独立させ、隠れた側のUpdateで他方の進行停止を解除しない。会話中は営業と自動進行を止め、終了しても手動再開を待つ。回答後に閉じた結果会話も進行を止め、イベント一覧の「常連からの猫紹介」から再開する。両方の結果会話が未完了なら、片方の完了だけでは解除しない。

## 保存・互換・Extra

既存CafeSaveEnvelopeへ任意の regular_conversation を追加。依頼の conversation はそのまま残し、両方の位置を同時に保存する。ゲーム状態・乱数・ルール・リプレイのダイジェストとは分離する。

| 項目 | 内容 |
| --- | --- |
| conversation_id | regular-introduction.regular-rescue-1.v1 |
| branch_id | introduction / accepted / declined |
| node | 現在節。導入末尾は選択待ち、結果末尾は終了操作待ち |
| completed | 結果の最後で会話終了を操作したか |
| viewed_branches | 閲覧完了の結果分岐。重複なし、最大1件 |

結果分岐は保存済みゲーム回答から同期し、ロード・再表示で費用、加入、回答コマンドを再実行しない。位置・ID・分岐・完了記録をゲーム回答と照合してからロードする。拒否時は元のゲームと会話を保持する。本文途中の保存は同じ節の先頭から再表示する。

regular_conversationがない旧セーブは、未回答なら開いた時に導入開始、回答済みなら従来の情報表示。閲覧完了を補完しない。今回より前の受け入れ依頼の途中セーブも維持する。空の任意クラスがUnity JSONで既定値化される場合は空IDをnullへ正規化する。

将来のExtraには conversation_id + branch_id の完了集合を渡せる。現在は営業スロットに属する記録だけを保存する。全プレイ共通プロフィール、タイトルExtra、回想再生、CG一覧、エンディング、全体オプションは未実装。公開後に節順の互換を壊す変更をする場合はIDの版と移行を別途用意する。

## 検証

C#の --conversation は230アサーション成功。既存依頼に加え、紹介の導入各節／両結果の全節で保存往復、独立した依頼の位置、二重回答／完了、旧セーブ、不正ID・節・分岐・完了、資金・枠不足、帰還／店舗待ち、依頼と紹介の両回答順を確認した。加速発生条件は検証内の複製ルールだけで使い、配布設定は変更しない。

Editorの実シーンのEdit Mode検証は969項目成功、Unity Error／Exception／Assertログ0件。1日目に非表示のUI→4日目の依頼、通常営業で発生した紹介の保存からの表示、マスクの有効化／無効化、Raycast、再表示、同時発生する2会話の切替・独立保存・両結果完了後の休業を確認した。同時発生検証の最初の試行は表示優先順を固定したテストの前提で失敗し、排他表示と双方向切替を確認する形に直して成功した。EditorのPlay Modeをネイティブ操作した通し確認ではない。

配布Playerは初回177項目、別プロセス再起動194項目成功。両分岐、導入途中・選択直前・両結果の全節のUnity JSON／実ファイル保存再開、回答・加入・支出・閲覧完了の重複防止、無効データの拒否と稼働状態保持、旧未回答／回答済みセーブ、資金・満員、全文表示・同フレーム連打・バックログの長文・マスク・表示再開・結果完了後の営業開始を確認。Unity Error／Exception／Assertログは0件。共用表示に移した既存受け入れ依頼も同じ最終Playerで37項目成功、Unityエラー0件。

Windows実画面をcomputer-useで操作し、1280×800で「迎える」→結果会話の保存画面→スロット8への保存確定→終了、1024×768で再起動して同じ結果節へ復帰、紹介者「佐藤さん」の名前欄、バックログ末尾へのスクロール、会話終了→営業準備への復帰を確認した。保存時は資金6390・所属6匹・accepted・結果node 0で、再開時も再回答を求めずその結果会話へ戻った。会話終了後も営業・自動進行は手動開始待ち。見送りの配布画面操作と結果はPlayer自動検証で確認し、ネイティブ入力の別通しは迎える側で実施した。

証跡は作業ルート Logs/StandaloneRegularConversation：core-result.txt、Editor/result.json、Player/result-first.txt、Player/result-restart.txt、Player/player-first.log、Player/player-restart.log、IntakeRegression/result.txt、IntakeRegression/player.log。画面証跡はPlayer/choices-1280.png、result-reopened-1024.png、history-bottom-1024.png、finished-1024.png。通常セーブとは分離した検証セーブと、検証途中の失敗記録も同ディレクトリに保持。最終ビルドログは Logs/StandaloneBuild/Runs/20261018-041600-754-bde6b4ea77854fea92d1c071a795f750.log 。Editorの共通検証は既存RendererFix/Editor/result.jsonにも出力し、今回の結果をEditor/result.jsonへ保存した。

### 容量管理

| 対象 | 開始時 | 終了時 |
| --- | ---: | ---: |
| Logs全体 | 17,494,293,160 bytes | 17,534,605,651 bytes |
| Unity Library | 10,982,928,598 bytes | 10,982,905,934 bytes |
| 固定Latest一組 | — | 8,247,501,219 bytes / 156 files |
| 今回の検証記録・専用セーブ | — | 約37.17MB / 100 files |

Logsは約40.31MB増。主に通常営業の履歴と表示記録を含む検証用セーブ（旧セーブ・各分岐・再起動用）と画面証跡、ビルドログ。Libraryは約23KB減で、大きなキャッシュ増加はない。容量計測JSON自体の保存で数百bytes前後の差がある。Latest一組だけを残し、Pending・追加のビルドコピー・ZIP・新しいプロジェクトやLibrary複製はない。自分の検証Playerもすべて終了した。

既存のDocs/AllCats.md、rules／presentation JSON、猫画像予定資料と別リポジトリの変更はコミット対象から除外する。通常セーブ・ソース・素材・他作業の生成物を削除していない。

途中のPlayer検証では、文字送り途中の本文に紹介者名がまだ出ていない段階での確認と、単独版の既存Viewがday_off DTOを提供しないことを検出した。前者は紹介者が話す節の名前欄で確認し、後者は会話の進行停止が解除されたことと実際の営業開始で検証するように分けた。失敗記録は first-typewriter-test-failure.txt と day-off-dto-test-failure.txt に保持する。休業画面用DTOの補完は今回追加せず、既存の休業コマンド自体はEditorで実行確認する。

再実行：作業ルートから dotnet run --project cat_cafe_sim_tool/tests/standalone/StandaloneChecks.csproj -- --conversation cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/rules.json 。Unityは cat_cafe_sim_tool/build_standalone.ps1 -ProjectPath C:/Users/hi-wa/unity_workspase/CatCafeSim -ExecuteMethod CatCafePreview.Editor.ConversationRendererVerification.VerifyAndBuild 。出力は Logs/StandaloneBuild/Latest/CatCafeStandalone.exe 固定、成功時にPendingから移動置換する。

専用Player検証は --catcafe-regular-conversation-check <専用記録ディレクトリ>、別プロセス再起動は --regular-conversation-restart を併用。通常セーブは使わない。ネイティブ保存の確認に --regular-manual-load を併用すると、専用manualディレクトリのスロット8へ戻る。既存依頼の --catcafe-conversation-check も回帰確認する。

## 残る課題

既存単独版の休業画面用DTOの補完は別途必要。今回の会話由来の停止解除と、バックエンドの休業は検証済み。派遣イベントの会話本文と接続、専用人物・背景素材、Extraの共通プロフィールと閲覧UIは後続。今回の範囲は常連からの固定紹介一本であり、派遣帰還後の紹介・通常募集・譲渡の会話化は含めない。ゲームバランスの再評価やPythonとの長期全比較は再実施せず、既存ルールの回帰と会話接続の検証を実施する。
