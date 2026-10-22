# 派遣中の二択「商店街からの追加のお願い」の会話化

実装・検証日：2026-10-18。[近所のお店への訪問](UnityDispatchConversation.md)に続き、既存の shopping_street_event / extra_work 一本を「やさしいADV」へ接続した。追加の仕事を引き受ける／予定どおりの仕事を終える、両分岐の結果会話から通常画面へ戻る。

## 表示と操作

商店街の担当者から追加の交流を頼まれる導入4節、各結果3節。派遣猫の名前・既存画像、地の文、担当者とあなたのセリフを全画面で表示する。猫の仕草は短い状況描写にとどめ、過去や担当者の固有名を追加しない。既存の暖かな室内図形と猫画像を利用し、画像欠落時は共通の代替表示を使う。専用の新規背景、画像生成、音声、実行時LLMはない。

AcquisitionConversationPanelの本文表示・全文表示／次へ・バックログ・保存／読込・閉じる・可変レイアウトを再利用する。EncounterConversationPanelは本文とDTOの選択肢を供給するだけ。初回発生時には会話を開き、他のADVが出ているときは重ねない。閉じた相談と結果はイベント一覧の「派遣中の選択イベント」から再開する。完了後と対象外の二択は既存の記録画面を使う。

両選択肢の近くに選択可否と理由、帰還報酬の前後、回答時の疲労・ストレスの実際の前後を表示する。LocalCafeDispatchのDTOに上限・下限で丸めた予測と回答時記録を渡し、画面側にルール値を埋め込まない。標準ルールのミケでは報酬260→360／260、疲労0→10／0、ストレス0→5／0。報酬は回答時に入金せず帰還で受け取る。日程・出発条件・追加交流の条件・報酬・疲労・ストレス・加入条件・バランスは変更していない。

既存のRespondDispatchChoice→resolve_dispatch_choice→CafeSimulationを使用する。商店街の解放条件と「接客好き」の出発条件も維持する。検証初期にタマを指定して既存の特性条件に拒否されたため、検証猫を条件に合うミケへ修正した。ゲーム側の条件は緩めていない。

## 保存・停止・Extra接続

会話本文と表示進行はCafeEncounterDialogue、表示進行の所有と回答同期はLocalCafeEncounterConversation、効果は既存CafeSimulationに分離する。会話中は共通の営業・自動進行停止へ接続する。回答前に閉じても未回答と節位置を保持し、既存の派遣二択待ちによる進行制限を維持する。回答後は結果会話完了まで止め、閉じた結果を一覧の回答・会話待ちに載せる。終了後の営業・自動進行は手動で再開する。休業画面の停止理由も既存のConversationProgressReasonへ渡す。

セーブ版1の任意フィールドencounter_conversationsとencounter_conversation_selectedを追加。派遣IDごとにCafeConversationProgressを保存する。同じ猫の後日の派遣は別IDになる。本文途中は同じ節の先頭から、選択直前は選択画面から、回答後は確定した結果分岐から再開する。ロード・表示は回答コマンドを再実行しない。連打抑止・送信中の無効化・既存request_id再送キャッシュと回答済み拒否により二重反映を防ぐ。

| 会話ID | 分岐ID | 閲覧完了 |
| --- | --- | --- |
| dispatch.extra_work.v1 | introduction | 導入と選択位置を保存、完了対象外 |
| 同上 | accept | 追加の仕事の結果を最後まで読んだ時に記録 |
| 同上 | decline | 予定どおり終える結果を最後まで読んだ時に記録 |

node、completed、viewed_branchesを保存。将来のExtraは会話ID＋分岐IDの完了集合を利用できる。複数の派遣記録の集合を取ればよく、同じ結果を繰り返し読んでも一つの進行の閲覧完了を重複追加しない。営業スロット内の記録であり、共通プロフィール・回想・Extra画面は後続。本文の節順を変更する公開後の更新ではIDの版または移行を用意する。

対象の派遣とextra_workの一致、実際の回答との分岐一致、選択派遣ID、重複行、節番号、閲覧完了の整合性を復元前に検証。不正ロードは現在のゲーム・会話を保持する。会話フィールドのない旧セーブは受理し、未回答は導入から開始できる。回答済みの旧セーブは会話閲覧を捏造せず既存記録に戻す。既存のルールID照合や営業形式の互換性条件は変えない。保存は従来の原子的置換とバックアップを利用する。

## 検証

C#の会話・ルール回帰344アサーション成功。導入と両結果の各節でJSON保存往復、選択位置、同じ回答の再実行による無変更、報酬の支払時期、完了の重複抑止、不正節拒否、旧メタなしの読込を確認。二つの異なる派遣二択の同時待ちでは、既存の回答順序・もう一方の未回答保持・日付進行停止を確認した。営業中の回答拒否、疲労98／ストレス99からの上限での丸めも確認。既存の受け入れ・常連紹介・近所の派遣・休業検証を含む。

Editorは既存の受け入れ／紹介969項目、近所の派遣303項目、今回のバックエンド34項目が成功。Playerは初回47項目、別プロセス再起動55項目成功、Unity Error／Exception／Assert 0件。実シーンで会話発生と旧画面の非重複、CanvasRenderer、全画面の大きさ、全文表示・次へと同フレーム連打、閉じる／一覧から再開、確定結果からの再開、両分岐、バックログの長文高さ、完了後に自動再開しないことを確認した。

各分岐の専用実ファイルを使い、導入途中・選択前・回答結果途中・完了の別スロットを新しいバックエンドと再起動Playerでロード。同一request_idの再送応答、新request_idでの重複拒否、実状態の無変更、帰還時の一度だけの入金、メタなし旧セーブの未回答／回答済み、不正分岐ロードの現在状態保持を確認した。

専用テスト用ルールの複製では店舗ランダムイベントを抑止し、商店街の解放前の待機だけを省略する。特性条件と既存効果は維持し、配布用rules／presentationは編集していない。これは通常プレイの解放まで全行程を通すバランステストではない。追加二択以外の会話化、全期間のPython比較、画像を物理的に除去する試験は今回再実施していない。

computer-useで1280×800の二択・予定どおりの結果・保存画面を実操作し、結果会話node0をスロット8へ保存。終了後1024×768で同じ結果へ復帰し、履歴末尾へのドラッグ、結果を最後まで読む操作、営業準備への復帰を確認した。資金は940のままで自動で営業は始まらない。同じ画面で選択前のスロット2を実ロードし、「追加の仕事を引き受ける」も実操作した。保存ファイルはdispatch-1-cat-mike、dispatch.extra_work.v1、decline、node0、未完了を保持している。

証跡：作業ルートLogs/StandaloneEncounterConversation。core-result.txt、Editor/result.txt、Editor/acquisition-regression.json、Editor/dispatch-regression.json、Player/result-first.txt、Player/result-restart.txt、player-first.log／player-restart.log。画像はchoice-1280.png、decline-1280.png、decline-restart-1024.png、history-bottom-1024.png、finished-1024.png、choice-1024.png、accept-1024.png。専用cases／manualに保存ファイルを保持し通常セーブは使わない。

最終確認で画面を開く前の既存の送信中・記録なしガードを会話分岐より先へ揃えたため、固定Latestを同じ入口で再ビルドし、最終Playerでも再起動検証を通す。

再実行（作業ルート）：

- dotnet run --project cat_cafe_sim_tool/tests/standalone/StandaloneChecks.csproj -- --conversation cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/rules.json
- cat_cafe_sim_tool/build_standalone.ps1 -ProjectPath C:/Users/hi-wa/unity_workspase/CatCafeSim -ExecuteMethod CatCafePreview.Editor.EncounterConversationVerification.VerifyAndBuild
- Player引数 --catcafe-encounter-conversation-check <専用出力>。再起動は --encounter-restart、実操作保存スロット8の再開は --encounter-manual-load を併用。

対象外の派遣二択、帰還後の猫紹介、宿のトラブル、Extra・CG一覧・エンディング・全体オプションは後続。別スレッドの休業画面の変更は保持し、今回の派遣中会話だけを追加した。既存未コミットの猫名／画像予定資料／rules／presentationはコミットに含めず、pushしない。

## 最終ビルドと容量管理

最終ビルドログはLogs/StandaloneBuild/Runs/20261018-104744-667-8a5486ff50ce48c6b3b0518fd86f5a03.log。送信中・記録なしガードを整えた最終ソースで既存Editor969＋303と今回Editor34が成功し、Player再起動55項目・Unityエラー0件も再確認した（Player/result-final.txt、player-final.log）。実画面の両分岐とレイアウトは同じ機能コードの直前ビルドで確認し、最終ビルドでも保存済みの結果会話を実画面で確認した。ソースと既存Unityプロジェクトへ反映済み。

| 対象 | 開始時 | 終了時 |
| --- | ---: | ---: |
| Logs全体 | 17,606,578,293 bytes | 17,610,293,047 bytes |
| 既存Unity Library | 10,983,387,059 bytes | 10,983,619,585 bytes |
| 固定Latest | — | 8,247,565,802 bytes / 156 files |
| 今回の検証記録 | — | 約2.16MB / 67 files |

Logsは約3.71MB増、主に専用検証セーブ・スクリーンショット・2回分のビルドログ。Libraryは約0.23MB増で大きなキャッシュ増加はない。計測JSONや終了ログの保存後に数KB程度の差がある。Latest一組だけを保持しPendingは存在しない。ビルド・ZIP・Unityプロジェクト・Libraryの複製は作らず、通常セーブ・素材・他作業の成果物を削除していない。検証Playerは終了する。

2026-10-18追補：[商店街からの猫紹介](UnityDispatchIntroductionConversation.md)をやさしいADVへ接続した。帰還後の紹介・両結果・加入と保存再開を追加し、既存条件と費用は維持する。

## 2026-10-22追補：郊外での休憩・追加撮影

商店街の二択の表示・保存・回答同期・Extra抽出を共通化し、郊外とスタジオを追加した。旧 `dispatch.extra_work.v1 / accept / decline` は維持。新規は `dispatch.rest_stop.v1 / rest / continue` と `dispatch.extra_photo_session.v1 / accept / decline`。導入4節・各結果3節で、既存猫画像と代替表示を利用する。新規素材・音声・実行時LLMはない。

表示値は既存 LocalCafeDispatch のDTOを利用し、上限・下限で丸めた前後値、選択不可理由、帰還報酬前後、反映時点を選択付近に示す。標準設定では休憩は報酬−50・疲労−10・ストレス−10、予定どおりは0。撮影継続は＋100・＋15・＋15、辞退も0・＋5・＋5。これは説明用の現行値であり、画面に固定値を埋め込まない。休憩で所持金を即時消費せず、報酬は帰還受領時、疲労・ストレスは既存回答時の一度だけの反映。既存ルールと効果処理を変更していない。

派遣案件IDごとに進行を保持。回答前・結果途中を閉じた後はイベント一覧から再開し、未読結果は既存の進行停止に接続。旧メタなし未回答は導入から、旧回答済みは閲覧履歴を補完しない。保存版は据え置き。会話IDと対象案件・実回答の整合性、節・ページ位置を検証する。読了した結果だけを各イベント2分岐でExtraへ登録し、導入＋当該結果の本文を共通回想で再生する。表示・回想は回答コマンドを呼ばない。

検証条件は専用ルール複製で店舗ランダムイベントと派遣解放待ちだけを省略。郊外は外出好きのタマ、商店街・撮影は接客好きのミケを使い、特性制限は維持した。既存未コミットの素材・文章・設定を保持し、今回のコミットへ含めない。

旧統合 ConversationRendererVerification は常連紹介の検証途中で、設備イベントの未読結果による `set_shifts` 拒否に停止する既知の未追従が再現した。派遣専用Editor検証入口はこの旧統合ランナーを切り分け、BackendChecksと固定ビルド、Playerの共通画面検証を実施する。旧統合ランナーの追従、標準条件の長期評価、全目標通し・全イベント組み合わせ・全解像度網羅は後続であり、今回の完了条件に含めない。

進捗ページと棚卸しは作業ルートの `Docs/DevelopmentStatus.md`、`Docs/EventConversationInventory.md` を更新した。これらはGitリポジトリ外の共有資料であり、今回のGitコミットは制作ツールリポジトリ内のソース・検証・本資料を対象とする。

### 今回の検証結果と容量

- C#共通会話回帰820項目成功。商店街・休憩・撮影の各2択×上限近傍／下限近傍、各節JSON往復、即時資金非変更、回答重複防止、Extra全6分岐・回想抽出時の状態非変更、複数派遣の別会話ID・別結果保持を確認。
- Editor102項目成功。各2択の導入途中・選択前・結果途中・読了スロット、同一request_id再送と別ID重複拒否、旧メタなしの未回答／回答済み、不正分岐ロード時の現状態保持、帰還時の一度だけの精算を確認。
- 最終配布Player初回139項目、別プロセス163項目成功。商店街二択回帰、両追加イベント全選択、既存画面との非重複、一覧から相談・結果再開、同フレーム次へ／回答連打、共通バックログ高さ、読了後の自動再開なしを確認。ログにUnity Error／Exception／Assertなし。
- computer-use実操作：1024×768で休憩の報酬562.5→512.5／予定どおり562.5、即時支出なしを表示。休憩結果をスロット3へ保存し、専用検証用にスロット8へ移した同じ会話を別プロセス1280×800で復元、共通設定とバックログを開いた。資金940を維持。撮影は1024×768で継続350→450・疲労／ストレス＋15、辞退350→350・＋5を表示。辞退を実操作し、結果読了→通常画面でミケの疲労5／ストレス5、資金940、営業未開始を確認。Extraでは切り上げるだけが閲覧済みとなり、導入から回想できた。

証跡：作業ルート `Logs/EncounterExpansion/Player`（初回・再起動・実操作保存）、`Logs/EncounterExpansion/Photo`（撮影・Extra）、`Logs/StandaloneEncounterConversation/Editor/result.txt`（102項目）、固定ビルドログ `Logs/StandaloneBuild/Runs/20261022-102011-266-1d70a45a61694413803468bce3a88c8d.log`。C#再実行とビルド入口は上記の既存コマンドを使う。Player引数は従来と同じ、実操作待機を撮影へ切り替える場合は `--encounter-photo` を追加する。

| 対象 | 開始 | 終了（計測時） |
| --- | ---: | ---: |
| Logs/StandaloneBuild全体 | 8,840,458,559 bytes / 323 files | 8,841,301,998 bytes / 327 files |
| 固定Latest | 前回成功分を維持して開始 | 8,757,693,708 bytes / 156 files |
| 既存Unity Library | 12,010,935,546 bytes / 32,058 files | 12,010,972,902 bytes / 32,057 files |
| 今回のPlayer検証記録 | 未作成 | 約4.70MB / 176 files |

ビルド領域は843,439 bytes増（実行ログ4本とコード）、Libraryは37,356 bytes増。大きな増加なし。Latest一組・Pendingなしを確認。新しいビルドZIP、プロジェクト複製、Library複製は作成していない。通常セーブ・既存素材・他作業成果物は削除していない。実画面検証Playerは終了した。
