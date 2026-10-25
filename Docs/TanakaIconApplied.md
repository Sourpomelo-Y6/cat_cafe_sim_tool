# 田中さん（guest-4）のプロフィール・採用B

2026-10-25。ユーザーの「Bでお願いします」によりプロフィール案と外見Bを採用。対象は田中さん一人。人物の自動生成機能や登録基盤を追加しない。

## 採用内容

- 制作対象 `person-guest-4` → ゲーム客ID `guest-4`。名前で推定しない。
- 性格：気さくでおおらか。猫の反応を急がず、うまくいかない時もさらりと受け止める。
- 口調：短い自然な常体。「〜してみよう」「〜でもいいね」「ゆっくりでいいよ」。呼び名 `{cat}`。命令・幼児語・結果の決めつけを避け、一人称はなるべく省略する。
- 外見：暗褐色の高いお団子、卵形の輪郭、眼鏡なし、くすんだピンクのカーディガン、クリーム色の襟、閉じた口の柔らかな笑顔。年齢・性別の文章設定・職業・来歴・来店歴は固定しない。
- 原本：[採用PNG](../../Docs/Prototypes/TanakaIcons-v1/tanaka-icon-b.png)。1254×1254 RGBA、alpha 0〜255。比較シートを登録せず、組み込みimage_genでB一人を仕上げた。[試作・完全な生成プロンプト](../../Docs/Prototypes/TanakaIcons-v1/README.md)。

既存ゲームで確定していたのは名前と客IDの対応。性格・口調・外見は今回の新規採用。既存本文・ゲーム数値・進行ルールを変更しない。

## 選択手順の見直し

ユーザーから、別キャラクター時にも選択は不要ではないかと指摘されていた旨の確認を受けた。確認した高橋さんチャットには「なんで2パターンあるのですか？」という発言と、依頼文に従ったとの説明がある。選択不要を過去に明示合意していたとまでは断定しない。今回の意向を受け、以後のお客さんは通常こちらで1案にまとめ、毎回A/B確認を求めず登録・表示まで進める方針を共有DevelopmentStatusに記録した。田中さんは明示選択のBを使う。

## 登録と検証の区分

`register_tanaka.py` は既存SubjectRegistryとHumanPortraitStoreを使用。プロフィール・客ID対応・画像の保存再読込、既存全人物行と既存4画像対応の保持、人物専用書き出し、採用本文・猫manifest・Unity猫catalogのhash保持を検査。[登録結果](Results/TanakaIcon/registration.json)。実台帳の手入力によるGUI登録とは区別する。

人物専用パック `ea0ffe4403dfe9a790b2b70425c252fc` 一組に5人を収録。猫bundleなし。通常 `ContentPreviewSetup.Import` の人物取り込みを使用。人物Importerは既存の1024上限・非圧縮・alpha保持・mipmap/read-write無効。猫BC7を適用せず、旧パックを保持する。

Tool回帰はバックエンド5件成功、GUI6件skip。別途 `HumanPortraitGuiTests.test_actual_manager_buttons` 1件成功。隔離fixtureで実Tk画面のボタン経路を確認し、ファイル選択等を差し替える自動検証。田中さんの実台帳を手入力した結果ではない。検証一時領域はワークスペース内。

開始容量bytes：Logs 15,108,852,710、Latest 3,044,744,232（156ファイル）、content_images 1,135,075,244、実Unity Library 5,187,554,086。既存のリン関連差分・猫設定・人物台帳差分を保護し、今回コミットに混ぜない。

## ビルド・Player確認手順

指定スクリプトと固定Latest、共有StandaloneBuildStorageを使用。最初の制限環境内起動はUnityライセンス接続でexit199。通常環境では通常Importer・ビルドガードを通して成功。初回Player検証はguest-4が来店しない初日で40刻進めようとして営業段階エラーとなった。成功記録に含めず、田中さんの既存来店曜日に対応する2日目の隔離fixtureへ検証手順を修正した。日付だけの直接変更は日別店舗イベント記録と整合せず停止したため、その中間結果も成功扱いせず、通常の休業・成長選択・翌日処理で2日目へ進む最終手順に直した。ゲームの来店patternsや通常セーブは変更しない。

Playerは `--catcafe-human-icons <専用出力> --catcafe-tanaka`。通常コマンドで2日目へ進めた隔離セーブで通常の来店・着席を待ち、保存→別backendで再開する。席はview fixture、信頼回復は実guest-4を持つevent fixture、Extraは予約/旧記録fixtureを使う。自然発生イベントや実マウス入力とは区別する。

採用原本1,260,434 bytes、SHA-256 `04b48ed19be7cca7490b47950ca4eba20cb8de4ec3e1b246f6aac2bf06a2cdfc`。服色・お団子・表情を試作Bと目視比較した。生成は組み込みimage_gen、最終プロンプトは共有Docs `Prototypes/TanakaIcons-v1/SelectedPrompt.txt`。

## 最終結果

最終ビルド `Logs/StandaloneBuild/Runs/20261025-125402-735-0c346d5535d14124a988013722b0144a.log` はexit0・CAT_CAFE_STANDALONE_BUILD_OK。配布LatestのPlayerはexit0・[50項目成功](Results/TanakaIcon/player-result.txt)。通常コマンドで初日休業から2日目へ進め、営業20刻にguest-4着席を確認し、保存→別backend読込で席客IDを保持した。全5画像の区別、未登録guest-5・未知ID、メモリ上だけで画像参照を欠損させたfixtureの共通人物記号と復帰、客交代・空席、実guest-4を指定した信頼回復、森さん予約Extra、客IDなし旧回想の代替を確認。描画前後のゲーム状態JSON一致、採用本文・猫manifest・猫catalogの開始hash一致。

全14撮影PNGは指定実寸・非黒画像を検査。[席1280](Results/TanakaIcon/seat-guest-4-1280.png)／[席1024](Results/TanakaIcon/seat-guest-4-1024.png)／[信頼1280](Results/TanakaIcon/trust-guest-4-1280.png)／[信頼1024](Results/TanakaIcon/trust-guest-4-1024.png)を目視。お団子とピンクの服色を保持するが、34 logical pxの席（1024時約27px）で細線や目まで判読できる保証はしない。席描画fixtureは第1席だけを書き換え、右の来客一覧・もう一席は元のままなので、田中さんが2席になる撮影がある。ゲーム通常状態で二重登録された結果ではない。実入力、信頼回復の自然発生、長期評価・総合確認は未実施。

## 終了容量（bytes）

| 対象 | 開始 | 終了 | 増減 |
| --- | ---: | ---: | ---: |
| Logs | 15108852710 | 15141246038 | +32393328 |
| Latest | 3044744232 | 3065719952 | +20975720 |
| content_images | 1135075244 | 1136336842 | +1261598 |
| 実Unity Library | 5187554086 | 5212762964 | +25208878 |

Latest成功物156ファイルの一組、StandaloneBuild配下Latest/Runsのみ、Pendingなし。Latest増分約21.0MBは5人収録の新人物パック・人物texture・コード等。Logs増分約32.4MBはLatest差分を含み、人物パック約6.4MB・描画・検証ログ等。Library約25.2MB増は既存プロジェクトの再取り込み・ビルドキャッシュ。採用原本は1,260,434 bytes、共有Docs・登録素材・新人物パックへ保存。旧素材・旧パック・セーブを削除せず、別Build・ZIP・Unityコピーを作らない。猫制作の日程・設定・既存未コミット差分を保持。Python台帳のステージ内容はHEADへ田中さん一行だけ追加し、以前からの台帳差分を作業ツリーへ残す。
