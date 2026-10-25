# 吉田さん（guest-11）のプロフィール・人物アイコン

2026-10-25。一人・一案を制作し、既存Tool登録・人物専用書き出し・通常Unity取り込みを実施。プロフィール自動生成、セリフ追加、UI再設計、ルール・数値・登場条件の変更なし。

## 採用内容

- 制作対象 `person-guest-11` → ゲーム客 `guest-11`。表示名から推定しない。
- 性格：静かで注意深く、猫の小さな動きを落ち着いて見守る。
- 接し方：まず様子を見てから短く一度誘い、返事や接近を急がせない。離れた猫を追わず、休憩を尊重する。
- 口調：短く落ち着いた丁寧語。「〜ですね」「少し待ちましょう」「無理せずどうぞ」。猫の呼び方 `{cat}`。一人称をなるべく省略し、命令・幼児語・猫の気持ちや結果の決めつけを避ける。
- 外見：額と耳が見える短いスレートグレーの横流し髪、やや幅のある卵形の輪郭、眼鏡なし。くすんだ黄土色のカーディガンと象牙色の丸首。
- 表情設計：細めの開眼、水平寄りの上まぶた、低く緩い眉、短く水平な閉じ口、少し横へ向けた視線。採用像は丸く大きく見開いた目より上まぶたが落ち着き、強い口角の上がりを抑えた静かな見守り顔。怒り・泣き・驚き等の一過性表情に固定しない。

固定定義の名前・客ID・解放条件を維持。指定資料・最新台帳・途中データに専用登録は見つからず、上記を今回の制作設定として追加した。年齢・性別の文章設定・職業・来歴・来店歴は増やさない。

[採用透過PNG](../../Docs/Prototypes/YoshidaIcon-v1/yoshida-icon.png)：1254×1254 RGBA、alpha0〜255。imagegenスキルの組み込みimage_gen一回、加藤・小林・中村の最新採用画像を目視し画風参照に使用。原本をそのまま採用。SHA256 `3ddc4aba9164fab5e2a588ebbac95c3ad0a44bda299161ff4325a1cf1325d4c5`、1286903 bytes。[完全プロンプト](../../Docs/Prototypes/YoshidaIcon-v1/README.md)。比較案・表情差分・立ち絵なし。

## 登録と保護

開始時Tool `576bba4`、Python `79fe477`、実Unity `37c0d0e`、共有Docs `ec7d6e0`。AGENTS、指定資料、最新Git履歴・未コミット差分、小林・加藤までの制作資料と採用PNGを確認。人物関連チャットはidle、Unity/Playerプロセスなし。猫制作差分・日程・設定と人物台帳catalog-2の先行差分を保持。

`register_yoshida.py` が既存SubjectRegistry.save / HumanPortraitStore.register/exportを使用。別インスタンスで再読込し既存全人物行・11画像対応を保持。[登録結果](Results/YoshidaIcon/registration.json)。実台帳のGUI手入力とは区別する。人物専用12人パック `e33ffb55daee067807b70dcb480906b5` を一組書き出し、同内容再出力は同じ場所を利用。猫画像・猫catalogは再書き出ししない。

通常ContentPreviewSetup.Importで取り込み、12件の明示ID・画像hash・人物Importerを[照合](Results/YoshidaIcon/unity-data-checks.json)。max1024・非圧縮・alpha保持・mipmap/read-write無効・Standalone overrideなし。猫用BC7なし。猫manifest・登録済み本文・実Unity猫catalogは開始hashと一致。旧パック・素材・セーブは削除しない。

## 確認区分

- Toolデータ検査：保存・別インスタンス再読込、既存人物・画像保持、透過、人物専用12人書き出し、同出力利用、保護hashが成功。
- Tool GUI有効の人物画像試験11件成功（継承バックエンド5件の重複を含む、固有バックエンド5件＋実Tkボタン1件）。隔離データ・ファイル選択等の差し替えによるGUI自動操作。[ログ](Results/YoshidaIcon/gui-tests.txt)。実台帳のGUI手入力とは主張しない。
- 人気第2段階Python回帰5件中4件成功・GUI1件skip。[ログ](Results/YoshidaIcon/popular-tests.txt)。ワークスペース内TEMP/TMPとUTF-8を使用。
- 登場条件：Python cafe_customersの固定名、core/cafe_weekdaysのpopular_customer_stage / popular_unlocked_day / popular_schedule、標準データ、Unity CafeCustomers.CustomerScheduleを確認。通常6人＋第1段階4人の次の第2段階2人の1人目。第2段階の達成記録と `day > resolved_day`、標準到着刻10が必要。曜日制限なし、雨天・不満・信頼の来店制限を維持。既存セーブへ第2段階客を後付けする変更なし。

## Player・保存再開・目視

指定 `build_standalone.ps1 -ProjectPath C:/Users/hi-wa/unity_workspase/CatCafeSim -ExecuteMethod CatCafePreview.Editor.HumanIconsApplyVerification.VerifyYoshidaAndBuild` と共有StandaloneBuildStorageの固定Latestを使用。最終成功ログ `Logs/StandaloneBuild/Runs/20261025-163109-591-f7b4871258c0405d890670e28619949b.log`、exit0・CAT_CAFE_STANDALONE_BUILD_OK。最初の制限環境ではライセンス接続exit199、同じ指定スクリプトを通常環境で再実行した。

初回のPlayerはfixtureに第2段階挑戦の操作が不足して失敗。その後も休業の自動翌日移行で予約が発生する順序と、4日目の保護猫受け入れ待ちに合わせてfixtureの通常回答を補った。製品の条件・ルール・データは変更せず、失敗記録はLogs/YoshidaIconに保持。最終成功以外を検証成功の根拠としない。各ビルドは同じLatestを置換し、別の成果物を増やさない。

固定Latestを `--catcafe-human-icons <隔離出力> --catcafe-yoshida` で起動し、[100項目成功](Results/YoshidaIcon/player-result.txt)。

- 解放済みfixture：通常人気挑戦開始後、隔離メモリの人気のみcapへ置き、通常休業決算で第1・第2段階を達成。第1段階後もguest-11なし、第2段階達成当日なし・翌日予定あり、通常の到着刻10を確認。自然な人気獲得の実証とは区別する。発生した特別予約・保護猫受け入れは通常辞退し、猫数・猫素材を増やさない。通常売上から5席購入。到着処理を手動割り当てにしてguest-11を案内後、自動割り当てへ戻す。
- [保存時view](Results/YoshidaIcon/seated.json)：4日目11刻、guest-11一人が席5・タマ担当。通常save_game/load_gameで別backendへ再開し、全席の客IDを保持。
- 実Player UIのview/event/replay fixture：12人の異なる画像解決、客交代・空席更新、未知ID・未登録guest-24・メモリ上だけの欠損画像の共通人物記号、欠損参照復帰、実客IDguest-11を持つ信頼案件、森さんの予約/Extra、客IDなし旧Extraの代替を確認。描画/回想前後の状態JSON（保存本文を含む）一致。

[席1280](Results/YoshidaIcon/seat-guest-11-1280.png)／[席1024](Results/YoshidaIcon/seat-guest-11-1024.png)／[信頼1280](Results/YoshidaIcon/trust-guest-11-1280.png)／[信頼1024](Results/YoshidaIcon/trust-guest-11-1024.png)を原寸目視。大きい表示では落ち着いた上まぶたと短い水平寄りの口が分かる。27〜34pxの実席表示では目の開き方が少し落ち着いて見え、灰髪・黄土色のまとまりと合わせて既存客から区別できる。瞳の方向や眉の細線まで判読できるとはしない。単なる口角差や髪・服色だけの設計にはしていない。

席撮影の既存fixtureは第1席のID/名前だけを置換するため、席1と本来の席5に吉田さんが見える。ゲーム状態の二重登録ではない。[本来の着席1280](Results/YoshidaIcon/actual-seated-yoshida-1280.png)／[1024](Results/YoshidaIcon/actual-seated-yoshida-1024.png)も追加で撮影し、右欄の吉田さん/席5と席5上部の専用画像を確認。既存の5席表示の第2段カード下部切れにより、席5の名前・担当行・詳細ボタンは画面外になる。対象外のためUI修正は加えない。

同じLatestを別プロセスでresume起動し、通常load_gameで吉田さん/席5/タマを復元。[自動5項目](Results/YoshidaIcon/manual-result.txt)／[復元view](Results/YoshidaIcon/resumed.json)。computer-useスキルの実マウスで1024×768の席一覧→猫一覧→猫欄スクロール試行→席一覧を確認。席5とタマの担当客アイコン、右欄の吉田さん/席5が維持され、4日目11刻・資金70も不変。スクロールでは下部の詳細ボタンを露出できなかった。[実入力メモ](Results/YoshidaIcon/input-notes.txt)／[実入力後画面](Results/YoshidaIcon/manual-after-input-1024.png)。manual-resultの汎用文は自動検査部分を指す。30自動描画＋実入力後1枚の寸法・非黒画像[検査](Results/YoshidaIcon/image-checks.json)成功。

未確認：自然な人気獲得からの登場、信頼回復の自然発生、1280×800実入力、実台帳のGUI手入力、席5詳細の実入力・5席カード全体。長期評価・総合確認一式は繰り返さない。

## 容量とコミット

| 対象 | 開始 bytes | 終了 bytes | 増減 bytes |
| --- | ---: | ---: | ---: |
| C:/Users/hi-wa/unity_workspase/CatCafeSim/Library | 5521953177 | 5506736989 | -15216188 |
| cat_cafe_sim/saves/content_images | 1144264192 | 1145552814 | +1288622 |
| Logs | 15464880526 | 15544140239 | +79259713 |
| Docs/Prototypes | 527041043 | 528330428 | +1289385 |
| Logs/StandaloneBuild/Latest | 3279665080 | 3330006800 | +50341720 |

Latestは156ファイルの成功ビルド一組、直下Latest/Runsのみ・Pendingなし。Logs約79.3MB増はLatest約50.3MB増を含み、人物パック・検証PNG・ログ等。12人分の新人物パックを旧パックと共存させるためLatestが増加する。人物素材約1.29MB増。既存Library約15.2MB減は同一プロジェクトの通常取り込み・ビルドキャッシュ更新で、手動整理は行っていない。画像原本とプロンプト約1.29MB増。資料自身の微小増分は終了snapshot後。素材・旧パック・セーブを削除せず、追加Build/ZIP/Unityコピー/Libraryなし。

今回分だけを各リポジトリへコミットし、pushなし。人物台帳はHEADへ吉田さん一行だけを追加した内容をstageし、catalog-2先行差分を作業ツリーに保持する。猫制作差分・日程・設定は含めない。Unity生成metaの既定末尾空白・書き出しJSONのCRLFは既存形式を保持。

登録コミット：Python `a8a8846`、実Unity `3227f54`、共有Docs `994813e`。Toolは本資料と同じコミット。pushなし。
