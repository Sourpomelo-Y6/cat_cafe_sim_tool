# 伊藤さん（guest-5）のプロフィールと人物アイコン

2026-10-25。対象は伊藤さん一人。ユーザー方針に従い一案だけを制作・採用。人物登録基盤・プロフィール自動生成・UI・ゲームルールの追加なし。

## 採用内容と根拠

- 制作対象 `person-guest-5` → ゲーム客ID `guest-5`。表示名で推定しない。
- 性格：慎重で穏やか。少し考えてから短く話す。
- 接し方：先に猫の様子を確かめ、近づくのを待つ。反応がなければ誘いを重ねず静かに休む。
- 口調：控えめな自然な常体。「〜してみようか」「ここで待とう」「無理しなくていいよ」。猫の呼び名 `{cat}`。命令・幼児語・結果の決めつけを避け、一人称はなるべく省略する。
- 外見：炭色の黒い横分け短髪、耳を見せる。やや面長で柔らかく角張る顎、眼鏡なし、くすんだ青緑のカーディガンと象牙色の襟、口を閉じた微笑み。
- 年齢・性別の文章設定・職業・来歴・来店歴を固定しない。既存本文や数値・来店曜日を変更しない。

既存確定設定は `cafe_customers.py` の名前と客ID。実台帳・画像台帳・制作資料を調べ、guest-5の専用プロフィール・画像・制作途中データは存在しなかった。性格・外見は今回の制作設定。田中さんはTool `9ddebd1`、Python `147c3ca`、共有Docs `bc37334`、実Unity `fab7450` まで完了を確認してから共有台帳を更新した。開始時のリン関連差分・猫設定・以前からの人物台帳差分を保持する。

原本：[ito-icon.png](../../Docs/Prototypes/ItoIcon-v1/ito-icon.png)。1254×1254 RGBA、alpha 0〜255。[制作設定・完全なプロンプト](../../Docs/Prototypes/ItoIcon-v1/README.md)。組み込みimage_genで、最新簡略版の佐藤・鈴木・森・高橋・田中の採用画像を画風参照。比較シートや二案、表情差分・立ち絵は作らない。

## 登録・書き出し・取り込み

`register_ito.py` は既存 `SubjectRegistry.save` と `HumanPortraitStore.register/export` を使用。プロフィール・明示ID対応・画像の保存再読込、既存全人物行と5画像対応保持、同じ内容の再書き出し先再利用、採用本文・猫manifest・実Unity猫catalogのSHA-256保持を検査。[登録結果](Results/ItoIcon/registration.json)。実台帳を手入力したGUI登録とは区別する。

人物専用パック `fcfa5755294680d11157181fe893a726` 一組に既存5人と伊藤さんの6人を収録。猫bundleなし。通常 `ContentPreviewSetup.Import` → 既存HumanPortraitImportで取り込み。人物Importerは1024上限・非圧縮・alpha保持・mipmap/read-write無効。人物に猫BC7を適用せず、猫catalogを変更しない。旧人物パック・素材・セーブを保持。

## 検証の区別

- データ検査：登録・保存再読込・既存対応保持・6画像書き出し・保護対象hash保持が成功。
- Tool回帰：既存バックエンド5件成功・GUI6件skip。[ログ](Results/ItoIcon/tool-tests.txt)。
- GUI自動操作：既存 `HumanPortraitGuiTests.test_actual_manager_buttons` 1件成功。[ログ](Results/ItoIcon/gui-tests.txt)。隔離fixtureの実Tk画面ボタンを使い、ファイル選択等を差し替える。伊藤さんの実台帳の手入力・実マウス操作ではない。
- Player検証は既存 `StandaloneHumanIconsSmoke` に `--catcafe-ito` を追加。初日は既存の雨天イベントが最後の通常客guest-5を予定から除外するため、初回は着席を待ち続けて閉店後の営業段階エラーとなった。出勤不足・席不足を疑った途中の検証も失敗。状態記録でguest-5の来店自体がないことと `CustomerSchedule` の雨天処理を確認し、不要な出勤・席拡張を取り除いた。最終手順は通常の休業・成長選択・翌日・店舗イベント回答で来店対象の日へ進み、営業する。閉店時は待機を止める。途中の失敗4件は成功記録へ含めずLogsに保持。通常セーブ・ゲーム設定は変更しない。
- 初回の制限環境内ビルドはUnityライセンス接続でexit199。通常環境で指定スクリプト・固定Latest・共有StandaloneBuildStorageを使用し、通常のImporterとビルドガードを通す。
- 席の客交代・空席と信頼回復・予約Extraの表示はview/event/replay fixtureで確認する。自然発生イベントや実入力とは区別する。席fixtureは第一席だけを置き換えるため、他の席・右欄が元の客のままになることがある。
- 実入力・信頼回復の自然発生・長期評価・総合確認一式は未実施。

## 最終結果

最終ビルドは `Logs/StandaloneBuild/Runs/20261025-130840-267-3f762067a120454aaa678c8920c22915.log`、exit0・CAT_CAFE_STANDALONE_BUILD_OK。固定LatestのPlayerはexit0、[56項目成功](Results/ItoIcon/player-result.txt)。通常の休業・成長選択・翌日処理で3日目へ進み、営業26刻のguest-5着席→保存→別backend再開で全席客IDと伊藤さんの着席を保持した。最終手順は既定の2席・出勤のまま、席購入・出勤変更・状態直接変更なし。初日の雨でguest-5が除外される既存ルールは維持。

6画像の区別、未登録guest-6・未知ID・メモリ上だけの欠損画像fixtureの共通人物記号と参照復帰、席の客交代・空席、実guest-5を持つ信頼回復イベントfixture、森さん予約Extra、客IDなし旧回想の代替を確認。描画・回想前後のゲーム状態JSON一致、採用本文・猫manifest・実Unity猫catalogの開始hash一致、全6画像hash・明示ID対応とImporter設定一致を確認。人物の表示基盤は再実装しない。

全16撮影PNGの実寸・非黒画像を検査。[検査記録](Results/ItoIcon/image-checks.json)。[席1280](Results/ItoIcon/seat-guest-5-1280.png)／[席1024](Results/ItoIcon/seat-guest-5-1024.png)／[信頼1280](Results/ItoIcon/trust-guest-5-1280.png)／[信頼1024](Results/ItoIcon/trust-guest-5-1024.png)を目視した。黒い短髪・眼鏡なし・青緑服で識別できるが、34 logical px（1024時約27px）の席では細い顔線まで判読できるとはしない。実マウス・キーボード入力、信頼回復の自然発生は未確認。保存は通常のsave_game/load_gameコマンド経路を自動実行した隔離セーブで、ユーザーの通常セーブは触らない。長期評価・総合確認は繰り返さない。

原本1,197,505 bytes、SHA-256 `2c72a486f9e703f6b1822670ccf5e0e5ea8e34ec4b4623978e9db88f229431f1`。共有Docs原本・既存Tool登録素材・新人物パックへ保存。

## 容量（bytes）

| 対象 | 開始 | 終了 | 増減 |
| --- | ---: | ---: | ---: |
| Logs | 15141246038 | 15181479477 | +40233439 |
| Latest | 3065719952 | 3090890512 | +25170560 |
| content_images | 1136336842 | 1137535625 | +1198783 |
| 実Unity Library | 5212762964 | 5242177476 | +29414512 |

Latestは成功物156ファイルの一組、StandaloneBuild直下はLatest/Runsのみ、Pendingなし。約25.2MBのLatest増分は既存方式による6人の新人物パック収録・非圧縮人物textureと検証コード等。Logs約40.2MB増はLatest増分を含み、人物パック約7.6MB・16枚描画・複数回のビルド/失敗記録等。Library約29.4MB増は既存プロジェクトの取り込み・ビルドキャッシュで、別Unityコピー・Library・Build・ZIPを作らない。容量は終了時スナップショット値（記録ファイル自身の数百bytes等を除く）。旧人物パック・素材・猫画像・セーブ・並行作業成果物は削除せず保持。

Python `7009f29`、実Unity `7b45b4c`。人物台帳のGitステージはHEADへ伊藤さん一行だけを足し、開始前からの人物台帳差分を作業ツリーへ残した。猫制作の日程・設定・リン関連の未コミット差分を混ぜず、今回分のみ個別コミット。pushなし。
