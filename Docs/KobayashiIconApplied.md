# 小林さん（guest-9）のプロフィール・人物アイコン

2026-10-25。一人・一案を制作し、既存Toolの人物保存・画像登録・人物専用書き出し・客ID表示で反映。プロフィール自動生成機能・セリフ本文・UI修正・登場条件や数値の変更なし。

## 採用内容

- 制作対象 `person-guest-9` → ゲーム客 `guest-9`。名前で推定しない。
- 性格：落ち着いて親しみやすく、小さな変化を楽しむ。
- 接し方：短く一度だけ誘って待ち、猫が離れたら追わずに見守る。休憩を尊重する。
- 口調：簡潔で穏やかな常体。「〜してみようか」「〜もいいね」「ここで待とう」。猫の呼び名 `{cat}`。一人称をなるべく省略し、命令・幼児語・猫の気持ちや結果の決めつけを避ける。
- 外見：横分けの短い黒に近いチャコール髪、耳が見える輪郭、柔らかな角形の顔、濃茶の角形眼鏡、深い青緑のカーディガンと象牙色の襟、閉じた口の微笑み。

固定客定義の名前・客ID・登場条件を維持。開始時の実人物台帳・人物画像台帳・指定資料にguest-9の専用設定・画像・途中登録はなかった。上記は今回の制作設定。年齢・性別の文章設定・職業・来歴・来店歴を増やさない。

[採用透過PNG](../../Docs/Prototypes/KobayashiIcon-v1/kobayashi-icon.png)：1254×1254 RGBA、alpha0〜255、SHA256 `56401e2a19d15a7419f1e2f09bd345198757a75da61108717807244bdad1f1e8`。[完全プロンプト](../../Docs/Prototypes/KobayashiIcon-v1/README.md)。imagegenスキルの組み込みimage_gen一回、山本・中村・渡辺を画風参照に使用。伊藤さんとは角形眼鏡の有無、佐藤さんとは眼鏡の形・髪・服色で区別。席の27〜34pxでは細線の判読は保証しない。比較案・表情差分・立ち絵・シートなし。

## 登録と保護

開始時Tool `b154bd6`、Python `b22744e`、実Unity `d3204a6`、共有Docs `3026f9c`。AGENTS・指定資料・最新履歴/差分を確認。人物関連チャットはidle、Unity/Playerプロセスなし。リンの画像・猫設定・制作日程・人物台帳catalog-2の先行差分は保持。

`register_kobayashi.py` が既存SubjectRegistry.save / HumanPortraitStore.register/exportを使用。実台帳の保存と別インスタンス再読込、既存全行・9画像対応を保持し、10人の人物専用パック `bd9f097f1eae61553f7b3f6ab745c6d5` 一組を書き出し。同内容再出力は同じフォルダ。[登録結果](Results/KobayashiIcon/registration.json)。実台帳をGUI手入力で登録したとの主張ではない。

通常ContentPreviewSetup.ImportでUnityへ取り込み。10件の明示ID・画像hash・既存人物Importer（max1024、非圧縮、alpha保持、mipmap/read-write無効、Standalone overrideなし）を[検査](Results/KobayashiIcon/unity-data-checks.json)。猫用BC7なし。猫画像・猫catalogの再書き出しなし。猫manifest・登録済み本文・実Unity猫catalogの開始hash一致。素材・旧パック・セーブの整理削除なし。

## 検証と登場条件

- Toolデータ検査：保存再読込・既存人物/画像保持・透過・人物専用書き出し・同出力利用・保護hashが成功。
- Toolバックエンド5件成功/GUI6件skip。GUI有効で11件成功（継承によるバックエンド5件の再実行＋実Tkボタン1件を含む）。ファイル選択等を差し替えた隔離GUI自動操作。初回はOS一時領域のreplace権限で失敗し、ワークスペース内TEMP/TMPへ切り替えて成功。製品コード変更なし。[Tool](Results/KobayashiIcon/tool-tests.txt)／[GUI](Results/KobayashiIcon/gui-tests.txt)。
- 人気追加客Python回帰8件成功・1件skip。[ログ](Results/KobayashiIcon/popular-tests.txt)。
- Python cafe_customersの名前順、core/cafe_weekdaysのpopular_customers・popular_unlocked_day・popular_schedule、Unity CafeCustomers.CustomerScheduleを確認。guest-9は通常6人に続く人気第1段階4人の3人目。達成記録と `day > resolved_day` が必要、標準到着刻20、曜日制限なし。雨天・不満/信頼の来店制限は維持。条件変更なし。
- 解放済みfixture：通常の人気挑戦開始後、隔離メモリの人気だけをcapへ置き、通常休業決算で第1段階達成。未解放/達成当日にguest-9なし、翌日予定ありを確認。通常売上から5席購入、到着刻20の処理で通常手動割り当てにより第1席へ案内し、自動割り当てへ戻す。3日目21刻・コハク担当。自然な人気獲得の実証とは区別する。

指定build_standalone.ps1・既存Unity・共有StandaloneBuildStorageの固定Latestでビルド成功。ログ `Logs/StandaloneBuild/Runs/20261025-154833-424-f5135a08b10544e68f6e3ae637e26263.log`、exit0・CAT_CAFE_STANDALONE_BUILD_OK。制限環境の初回はライセンス接続exit199、同じスクリプトを通常環境で再実行。Playerも制限環境では起動せず、通常環境で確認。

固定Latestの `--catcafe-human-icons <隔離出力> --catcafe-kobayashi` は[84項目成功](Results/KobayashiIcon/player-result.txt)。実Player UIのview/event/replay fixtureで10画像別解決、客交代・空席更新、未知/未登録guest-24/メモリ上の欠損参照の共通人物記号、参照復帰、実客IDguest-9を持つ信頼案件、森さん予約/Extra、客IDなし旧Extraの代替を確認。通常save_game/load_gameで別backendへ再開し全席客IDを保持。描画/回想前後のゲーム状態JSON（保存本文を含む）一致。

[席1280](Results/KobayashiIcon/seat-guest-9-1280.png)／[席1024](Results/KobayashiIcon/seat-guest-9-1024.png)／[信頼1280](Results/KobayashiIcon/trust-guest-9-1280.png)／[信頼1024](Results/KobayashiIcon/trust-guest-9-1024.png)を目視。黒髪・角形眼鏡・青緑服を保持。撮影fixtureは第1席だけID/名前を差し替える方式だが、今回の本来の小林さんも第1席。右欄も小林さん/席1を示す。既存の5席カード第2段下部がviewportで切れ、第5席詳細行は読めない。小林さんの第1席は全体表示可能。UI修正は混ぜない。

同じLatestを別プロセスでresumeフラグにより起動し、通常load_gameで小林さん/席1/コハク担当を復元。[自動5項目](Results/KobayashiIcon/manual-result.txt)／[復元view](Results/KobayashiIcon/resumed.json)。computer-useスキルの実マウスで1024×768の猫一覧→席一覧→席1詳細→閉じるを確認。名前・担当・アイコン保持、進行操作なし。[入力メモ](Results/KobayashiIcon/input-notes.txt)／[実入力後画面](Results/KobayashiIcon/manual-after-input-1024.png)。manual-resultの汎用文は自動検査部分を指し、実入力は別記録。最初のhidden再開プロセスを終了合図で閉じ、可視再開で入力確認した。24自動描画＋入力後1枚の[サイズ・非黒画像検査](Results/KobayashiIcon/image-checks.json)成功。

未確認：自然な人気獲得からの登場、信頼回復の自然発生、1280×800の実入力、実台帳のGUI手入力、5席カード全体の表示。長期評価・総合一式は繰り返さない。

## 容量（bytes）

| 対象 | 開始 | 終了 | 増減 |
| --- | ---: | ---: | ---: |
| Logs | 15336052057 | 15397143983 | +61091926 |
| Logs/StandaloneBuild/Latest | 3191571840 | 3233521288 | +41949448 |
| cat_cafe_sim/saves/content_images | 1141645807 | 1142901934 | +1256127 |
| C:/Users/hi-wa/unity_workspase/CatCafeSim/Library | 5355579166 | 5465227805 | +109648639 |

Latestは156ファイルの成功ビルド一組、直下Latest/Runsのみ。Pending・別Build・ZIP・Unityコピー・追加Libraryなし。Latest約41.9MB増は旧人物を保持する新10人パックの非圧縮texture等。Logs約61.1MB増はLatestを含み、人物パック・検証PNG・ログ等。素材登録約1.26MB、既存Library約109.6MB増は同一プロジェクトの通常取り込み/ビルドキャッシュ。原本PNGは1,254,837 bytes。素材・旧パック・セーブは保持。記録自身の微小増は終了スナップショット後。

今回分だけを各リポジトリへコミットし、pushなし。既存catalog-2差分はHEADへ小林さん一行だけ加えた台帳をstageすることで除外し、作業ツリーに保持。

登録コミット：Python 1baa6c4、実Unity 85016ef、共有Docs a82a31a。Toolは本資料と同じコミット。pushなし。
