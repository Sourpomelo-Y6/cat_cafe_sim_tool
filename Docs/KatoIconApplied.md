# 加藤さん（guest-10）のプロフィール・人物アイコン

2026-10-25。一人・一案を制作し、既存Tool登録・人物専用書き出し・通常Unity取り込み・固定Latestへ反映。プロフィール自動生成、セリフ本文追加、UI再設計、ルール・数値・登場条件変更なし。

## 採用内容

- 制作対象 `person-guest-10` → ゲーム客 `guest-10`。名前で推定しない。
- 性格：落ち着いた好奇心があり、猫の動きを丁寧に観察する。
- 接し方：気になった様子に短く声をかけ、一度誘ったら反応を待つ。離れた猫を追わず、休憩を尊重する。
- 口調：やわらかく簡潔な丁寧語。「〜でしょうか」「〜もよさそうですね」「ここで待ちましょう」。猫の呼び名 `{cat}`。一人称をなるべく省略し、命令・幼児語・猫の気持ちや結果の決めつけを避ける。
- 外見：片耳にかけた短いアッシュブラウンのボブ、横分けの斜め前髪、眼鏡なし。プラム色のカーディガンと象牙色の丸首。
- 表情：やや丸く開いた目、左右差をつけて上げた眉、小さく閉じた口の片側をわずかに上げる。既存の均等な微笑みから少し変え、日常の落ち着いた好奇心を表す。小さい席でも髪の輪郭・プラム色・丸首は識別要素になるが、27〜34pxで眉や口の細線まで判読できるとはしない。

固定客定義の名前・ID・登場条件を維持。台帳・指定資料・最新制作資料・途中データに加藤さんの専用登録はなかったため、上記を今回の制作設定として追加。年齢・性別の文章設定・職業・来歴・来店歴を増やさない。

[採用透過PNG](../../Docs/Prototypes/KatoIcon-v1/kato-icon.png)は1254×1254 RGBA、alpha0〜255。imagegenスキルの組み込みimage_genを一回使用し、山本・中村・小林の最新採用画像を実際に目視して画風参照に入力。原本をそのまま採用。SHA256 `dc827908f8d40151e8339c971302e9be3723622b13ff9237297daa04bf5ce374`、1,361,083 bytes。[完全プロンプト](../../Docs/Prototypes/KatoIcon-v1/README.md)。比較案・表情差分・立ち絵・シートなし。

## 登録・既存差分の保持

開始時Tool f77ec85、Python 1baa6c4、実Unity 85016ef、共有Docs a82a31a。AGENTS・指定資料・最新履歴/差分と小林さんまでの制作記録を確認。関連人物チャットはidle、開始時Unity/Playerプロセスなし。猫制作差分・日程・設定、人物台帳catalog-2先行差分を保持。

`register_kato.py` が既存SubjectRegistry.save / HumanPortraitStore.register/exportを利用。別インスタンス再読込と、既存全人物行・10画像対応の保持を確認。[登録結果](Results/KatoIcon/registration.json)。実台帳のGUI手入力登録とは主張しない。11人の人物専用パック `88b9d40a6140bb9bac08690a6a2840ac` を一組だけ書き出し、同内容再出力は同じ場所を利用。猫画像・猫catalog再書き出しなし。

通常ContentPreviewSetup.Importで取り込み。11件の明示ID・画像hash・既存人物Importerを[照合](Results/KatoIcon/unity-data-checks.json)。max1024・非圧縮・alpha保持・mipmap/read-write無効・Standalone overrideなし。猫用BC7なし。猫manifest・登録済み本文・実Unity猫catalogの開始hash一致。旧パック・素材・セーブは削除しない。

## 確認区分と結果

- Toolデータ検査：保存再読込、既存人物/画像保持、透過、人物専用11人書き出し・同出力利用、保護hashが成功。
- Toolバックエンド5件成功/GUI6件skip。[ログ](Results/KatoIcon/tool-tests.txt)。GUI有効で11件成功（継承バックエンド5件の再実行と実Tkボタン1件を含む）。[ログ](Results/KatoIcon/gui-tests.txt)。隔離データとファイル選択等を差し替えたGUI自動操作。
- 人気追加客Python回帰8件成功・1件skip。[ログ](Results/KatoIcon/popular-tests.txt)。ワークスペース内TEMP/TMPとUTF-8を使用。
- 登場条件：Python cafe_customers固定名、core/cafe_weekdays popular_customers/popular_unlocked_day/popular_schedule、標準データとUnity CafeCustomers.CustomerScheduleを確認。通常6人の次の人気第1段階4人の4人目。達成記録が必要、`day > resolved_day`、標準到着刻28、曜日制限なし。雨天・不満/信頼の来店制限を維持。
- 解放済みfixture：通常人気挑戦開始後、隔離メモリの人気だけをcapへ置き、通常休業決算で達成。未解放・達成当日はguest-10なし、翌日予定ありを確認。通常売上から5席購入、到着刻28の処理後に既存手動割り当てで案内し自動割り当てへ戻す。3日目29刻・第3席・ミケ担当。自然な人気獲得の実証とは区別。

指定build_standalone.ps1・既存Unity・共有StandaloneBuildStorageの固定Latestで成功。ログ `Logs/StandaloneBuild/Runs/20261025-160239-297-329ef15abe9f4c8f8b2f8335739c6eed.log`、exit0・CAT_CAFE_STANDALONE_BUILD_OK。制限環境の初回はライセンス接続exit199、同じ指定スクリプトを通常環境で再実行。

固定Latestの `--catcafe-human-icons <隔離出力> --catcafe-kato` は[90項目成功](Results/KatoIcon/player-result.txt)。実Playerのview/event/replay fixtureで11画像別解決、客交代・空席、未知/未登録guest-24/メモリ上だけの欠損画像の共通人物記号、参照復帰、実客IDguest-10を持つ信頼案件、森さん予約/Extra、客IDなし旧Extraの代替を確認。通常save_game/load_gameで別backendへ再開して全席客IDを保持。描画/回想前後の状態JSON（保存本文を含む）一致。

[席1280](Results/KatoIcon/seat-guest-10-1280.png)／[席1024](Results/KatoIcon/seat-guest-10-1024.png)／[信頼1280](Results/KatoIcon/trust-guest-10-1280.png)／[信頼1024](Results/KatoIcon/trust-guest-10-1024.png)を目視。席撮影fixtureは第一席のID/名前だけを置換するので第1・第3席に加藤さんが見えるが、保存した本来の加藤さんは第3席一人、右欄も席3を示す。ゲーム状態の二重登録ではない。5席カード第2段の下部が切れる既存問題は未修正。対象の第3席は表示可能。

別プロセスresume起動の通常load_gameで加藤さん/席3/ミケを復元。[自動5項目](Results/KatoIcon/manual-result.txt)／[復元view](Results/KatoIcon/resumed.json)。最初のhidden再開を終了合図で閉じ、可視再開でcomputer-useスキルの実マウスを使用。1024×768で猫一覧→席一覧→席3詳細→閉じるを確認。名前・担当・アイコン保持、日数/刻進行なし。[実入力メモ](Results/KatoIcon/input-notes.txt)／[実入力後](Results/KatoIcon/manual-after-input-1024.png)。manual-resultの汎用文は自動検査部分を指す。全27PNG（26自動描画＋実入力後1枚）の寸法・非黒画像[検査](Results/KatoIcon/image-checks.json)も成功。

未確認：自然な人気獲得からの登場、信頼回復の自然発生、1280×800実入力、実台帳のGUI手入力、5席カード全体。長期評価・総合一式は繰り返さない。

## 容量とコミット

| 対象 | 開始 bytes | 終了 bytes | 増減 bytes |
| --- | ---: | ---: | ---: |
| Logs | 15397165978 | 15464857247 | +67691269 |
| Logs/StandaloneBuild/Latest | 3233521288 | 3279665080 | +46143792 |
| cat_cafe_sim/saves/content_images | 1142901934 | 1144264467 | +1362533 |
| C:/Users/hi-wa/unity_workspase/CatCafeSim/Library | 5465227805 | 5521953177 | +56725372 |
| Docs/Prototypes | 525677987 | 527041043 | +1363056 |

Latestは156ファイルの一組。Logs約67.7MB増はLatest約46.1MB増と人物パック・検証PNG・ログ等。素材登録約1.36MB増、既存Library約56.7MB増は同一プロジェクトの通常取り込み/ビルドキャッシュ。終了スナップショット後の本資料等の微小増分は含まない。

容量は[開始](Results/KatoIcon/capacity-start.json)・[終了](Results/KatoIcon/capacity-end.json)のbytes記録を参照。Latest成功物一組、Pendingなし。新パックの11人分非圧縮textureを旧パックと共存させるためLatestは増加する。素材・旧パック・セーブを削除せず、追加Build/ZIP/Unityコピー/Libraryを作らない。

今回分だけを各リポジトリへコミット、pushなし。人物台帳はHEADへ加藤さん一行だけを追加してstageし、catalog-2先行差分を作業ツリーに保持する。

登録コミット：Python `79fe477`、実Unity `37c0d0e`、共有Docs `ec7d6e0`。Toolは本資料と同じコミット。pushなし。生成されたUnity metaの空値末尾空白・書き出しJSONのCRLFを保持し、該当形式を許容して差分検査。手書きC#の差分検査は通常設定で成功。
