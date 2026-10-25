# 高橋さん（guest-3）のプロフィール・採用A

2026-10-25。ユーザーの「ではAでお願いします」で、提示したプロフィールと左Aの外見を採用。対象は一人のみ。既存の人物登録・人物専用書き出し・ID別Unity表示を使用し、新機能・専用イベントは追加していない。

## 採用内容

- 名前：高橋さん。制作対象ID `person-guest-3`、ゲーム客ID `guest-3`。名前による推定は行わない。
- 性格・口調：落ち着いていて猫の小さな変化に気づく。短く穏やかな常体「〜かな」「〜だね」「〜しようか」。まず様子を見てから誘い、反応がなければ静かに待つ。呼び名 `{cat}`。年齢・職業・来歴・来店歴は固定しない。
- 外見：青みのある黒髪の顎丈ボブと前髪、丸みのある輪郭、眼鏡なし、赤茶のカーディガンとクリーム色の襟、穏やかな微笑み。
- 原本：共有Docs `Prototypes/TakahashiIcons-v1/takahashi-icon-a.png`。1254×1254 RGBA、alpha 0〜255、1,328,171 bytes。SHA-256 `3821a02fbe187442a3fb7d2843c1191fb8e71a3544a41d6fb0802a89d4d915e6`。

既存の確定設定は `cafe_customers.py` のguest番号と名前の対応。性格・口調・外見は今回の採用であり、既存設定を発見したとの意味ではない。詳細版佐藤さんではなく最新の簡略3アイコンを参照したA/Bから、組み込みimage_genでA一人だけを透過画像に仕上げた。[調査・試作・プロンプト](../../Docs/Prototypes/TakahashiIcons-v1/README.md)。比較シートは登録しない。表情差分・立ち絵・大量のセリフ制作は行わない。

## 登録・取り込み

`register_takahashi.py` が既存 `SubjectRegistry.save` と `HumanPortraitStore.register/export` を使用。プロフィール・ゲームID対応・画像の保存再読込、既存全人物行と既存3画像対応保持、人物だけの書き出し、採用本文・猫manifest・Unity猫catalogのSHA-256保持を検査した。[結果](Results/TakahashiIcon/registration.json)。実GUIの手入力による登録とは区別する。

人物専用パック `7f5d49c1d9b4f2a5c5980b872aaed35c` 一組に4人を収録。`ContentPreviewSetup.Import`→既存HumanPortraitImportで実Unityへ取り込んだ。人物Importerは1024上限・非圧縮・alpha保持・mipmap/read-write無効。猫BC7を適用しない。旧人物パックは保持。人物専用パックに猫bundleはなく、猫catalogは開始時hashと一致。

## 検証の区別

- データ検査：上記保存再読込・既存人物保持・4画像書き出し・保護対象hash保持が成功。
- Tool回帰：`tests.test_human_portraits` は5件成功・GUI6件skip。初回はOSの一時領域の置換権限で失敗し、ワークスペース内の専用一時領域へ切り替えて成功。製品コードの変更なし。
- GUI自動操作：既存 `HumanPortraitGuiTests.test_actual_manager_buttons` 1件成功。隔離fixtureで登録・差し替え・プレビュー・書き出し・解除の実Tk画面経路を通す。高橋さんの実台帳を手入力した結果ではない。
- 配布Player：`--catcafe-human-icons <output> --catcafe-takahashi` で既存検証入口を拡張。4画像が異なること、未登録guest-4・未知ID・メモリ上だけの欠損画像fixtureの共通人物記号、高橋さんの通常営業14刻の着席→保存→別backend再開、全席客ID保持を確認。席の入替・空席、実際の信頼回復画面のguest-3解決、森さんの予約Extra・旧IDなし回想代替も確認。表示前後のゲーム状態JSON一致を検査し、本文・ゲーム状態を変更していない。
- 席と信頼回復の描画はview/event fixture。席fixtureは最初の席だけを置換し、右の来客一覧ともう一席は元のままなので、画像中に高橋さんが2席ある場合がある。自然発生イベントや実入力の結果とはしない。
- 実入力：未実施。信頼回復の自然発生、長期評価、総合確認一式は対象外。プロフィールは制作資料であり、既存の共通本文を書き換えたものではない。

## ビルドと保護

指定build_standalone.ps1・既存実Unity・固定Latestを使用。最初の制限環境内起動はライセンス接続でexit199。通常環境で標準Importer・ビルドガードを通して成功。今回、猫Importer・猫自動制作設定を変更していない。リンなどの既存未コミット差分を残し、今回コミットに混ぜない。人物台帳もHEADに高橋さん一行だけを足した内容をステージし、以前からの台帳差分を作業ツリーへ保持した。

撮影の初回1280指定は実寸1920で、次の起動は黒画像だったため、そのまま成功記録に採用しない。検証コードに事前の解像度切替待ちを追加し、同じLatestと記録パスを更新して確認した。最終12PNGすべて指定実寸・非黒画像をデータ検査し、guest-3の席・信頼回復を両解像度で目視確認。髪と赤茶の服色は保持し、席34 logical px（1024時は約27px）の細かな目・髪の線まで判読できるとはしない。

最終ビルド：`Logs/StandaloneBuild/Runs/20261025-123952-213-810cb5b31b4f4ca6a4564d466466e130.log`、exit0・CAT_CAFE_STANDALONE_BUILD_OK。[Player43項目成功](Results/TakahashiIcon/player-result.txt)。[席1280](Results/TakahashiIcon/seat-guest-3-1280.png)／[席1024](Results/TakahashiIcon/seat-guest-3-1024.png)／[信頼回復1280](Results/TakahashiIcon/trust-guest-3-1280.png)／[信頼回復1024](Results/TakahashiIcon/trust-guest-3-1024.png)。実入力・イベント自然発生は未確認。

## 終了容量

| 対象 | 開始 bytes | 終了 bytes | 増減 bytes |
| --- | ---: | ---: | ---: |
| Logs | 15083123422 | 15108852710 | +25729288 |
| Latest | 3027962840 | 3044744232 | +16781392 |
| content_images | 1133745963 | 1135075244 | +1329281 |
| 実Unity Library | 5294582507 | 5187554086 | -107028421 |

Latestは156ファイルの成功ビルド一組、StandaloneBuild配下はLatest/RunsのみでPendingなし。Latest増加約16.8MBは人物専用新パックの4画像収録（旧パック保持）とコード更新等。Logs増加約25.7MBにはLatest増分と人物パック一組・描画記録・ビルドログを含む。新原本は1,328,171 bytesで共有Docs・登録素材・新人物パックへ保存。人物パックは採用4人を保持する既存方式で、旧パックの整理は行わない。Libraryは通常の取り込み・ビルドで減少し、手動削除なし。追加Unityコピー・別Build・ZIPなし。セーブ・素材・並行作業・旧パックは削除していない。

Python採用データコミット `e65dd7d`。Tool・共有Docs・実Unityも今回の人物関係だけを個別コミットし、pushしない。
