# 渡辺さん（guest-6）のプロフィール・人物アイコン

2026-10-25。対象は渡辺さん一人。ユーザー方針により一案だけを制作・登録し、通常Unity取り込み・固定Latest・保存再開まで確認。プロフィール自動生成・UI再設計・ルール/数値変更なし。

## 採用プロフィール・画像

- 制作対象ID `person-guest-6` → ゲーム客ID `guest-6`。表示名で推定しない。
- 性格：好奇心があり、物事をゆったり楽しむ。小さな変化を素直に言葉にする。
- 接し方：猫の視線や距離を見て一度だけ穏やかに誘い、反応がなければ遊びを止めて待つ。
- 口調：親しみのある短い丁寧語。「〜してみませんか」「〜もよさそうですね」「ひと休みしましょうか」。猫の呼び名 `{cat}`。一人称はなるべく省略し、命令・幼児語・結果の決めつけを避ける。
- 外見：明るい灰茶の短いゆるいウェーブ、丸みのある輪郭、眼鏡なし。ラベンダーのカーディガンと象牙色の襟、閉じた口の微笑み。年齢・性別の文章設定・職業・来歴・来店歴は固定しない。

既存確定設定は `cafe_customers.py` の名前とguest番号、来店曜日等のゲーム定義。実人物台帳・画像台帳・制作資料に専用設定・画像・制作途中データはなく、性格・外見は今回の制作設定。既存本文・来店曜日・ルールを変更しない。開始時の最新Tool `4a1a180`、Python `7009f29`、実Unity `7b45b4c`、共有Docs `a359d2c` を確認。人物関連チャットはidle、実Unity Editor/Player実行なし（通常環境のプロセス確認でHubサービス/auth brokerのみ）。リンの未コミット素材・日程・設定・人物台帳の以前からの差分は保持。

[採用原本](../../Docs/Prototypes/WatanabeIcon-v1/watanabe-icon.png)：1254×1254 RGBA、alpha 0〜255、1,321,696 bytes。SHA-256 `35267d0ee7f23dd2756a5cd942e215b66e07d1348e8fdbc9f5c2e6425ed4887b`。[制作設定・プロンプト](../../Docs/Prototypes/WatanabeIcon-v1/README.md)。組み込みimage_genを使用。最新6人を目視、参照上限5のため佐藤・鈴木・高橋・田中・伊藤を画風参照として渡した。6参照の初回要求は引数検査で拒否され画像生成なし。実際の生成は一案のみ。比較シート・表情差分・立ち絵なし。

## 登録・取り込み

`register_watanabe.py` が既存 `SubjectRegistry.save` と `HumanPortraitStore.register/export` を使用。実台帳へのプロフィール保存・再読込、既存全人物行と6画像対応の保持、人物専用書き出し・再書き出し先再利用を確認。[登録結果](Results/WatanabeIcon/registration.json)。GUI手入力で登録したとの主張ではない。

人物パック `a02fd76a7285a62eb61d680fad28ace8` 一組、既存6人＋渡辺さんの7人。猫bundle・猫catalog再書き出しなし。通常 `ContentPreviewSetup.Import` → 既存HumanPortraitImportで取り込み。人物Importerは既存max1024・非圧縮・alpha保持・mipmap/read-write無効、Standalone固有overrideなし。猫BC7を人物へ適用しない。[7画像hash/Importer・保護対象hash検査](Results/WatanabeIcon/unity-data-checks.json)。採用本文・猫manifest・実Unity猫catalogの開始hash一致。

## 検証結果と区分

- データ検査：Tool保存再読込・既存全プロフィールと6画像保持・7人パック・同内容出力再利用・保護対象hash保持、実Unityの7画像hash/ID/Importerが成功。
- Tool回帰：[バックエンド5件成功・GUI6件skip](Results/WatanabeIcon/tool-tests.txt)。別途[GUI自動操作1件成功](Results/WatanabeIcon/gui-tests.txt)。隔離fixtureの実Tk画面ボタン経路で登録・差替・プレビュー・書き出し・解除。ファイル選択を差し替える自動検証であり、渡辺さんの実台帳への手入力/実マウス入力ではない。
- ビルド：制限環境内の初回はUnityライセンス接続でexit199。通常環境で指定 `build_standalone.ps1 -ProjectPath C:/Users/hi-wa/unity_workspase/CatCafeSim -ExecuteMethod CatCafePreview.Editor.HumanIconsApplyVerification.VerifyWatanabeAndBuild` を実行。通常Importer・ビルドガード・共有StandaloneBuildStorageを使用。成功ログ `Logs/StandaloneBuild/Runs/20261025-131839-989-f35400d48b884168b57defc0135ebef9.log`、exit0と `CAT_CAFE_STANDALONE_BUILD_OK`。別ビルド・ZIP・Unityコピーなし。
- 固定Latest Player：`--catcafe-human-icons <隔離出力> --catcafe-watanabe`、exit0、[62項目成功](Results/WatanabeIcon/player-result.txt)。通常の休業・成長選択・翌日・店舗イベント回答で2日目へ進み、既定2席・既定出勤の営業32刻にguest-6着席。通常の `save_game/load_game` コマンドで保存→別backend再開し、全席の客IDと渡辺さんの着席を保持。ユーザーの通常セーブには触れない。
- 実際の席UI：7画像を別々に解決、客交代・未知IDの共通記号・空席で非表示。未登録guest-7・未知ID・メモリ上だけの欠損画像fixtureで共通人物記号へ戻り、元の参照復帰後は採用画像を解決。
- 信頼回復：実際の案件客ID `guest-6` を持つイベントfixtureから画像を解決。森さん予約Extra/予約ADVと客IDなし旧Extraの共通記号も確認。席・回想の表示前後でゲーム状態JSON（保存本文を含む）が一致。fixtureの信頼警告や本文は製品データに登録しない。
- 代表描画：[席1280](Results/WatanabeIcon/seat-guest-6-1280.png)／[席1024](Results/WatanabeIcon/seat-guest-6-1024.png)／[信頼1280](Results/WatanabeIcon/trust-guest-6-1280.png)／[信頼1024](Results/WatanabeIcon/trust-guest-6-1024.png)を目視。全18PNGの実寸・非黒画像を[検査](Results/WatanabeIcon/image-checks.json)。明るい灰茶髪・紫服を識別できるが、席34 logical px（1024時約27px）の細線まで読めるとはしない。
- 描画はview/event/replay fixtureを用いたPlayer自動検証。席fixtureは第一席だけを差し替えるため他の席/右欄は元のまま。実入力・信頼回復の自然発生は未確認。長期評価・総合確認一式は繰り返さない。

## 容量（bytes）

| 対象 | 開始 | 終了 | 増減 |
| --- | ---: | ---: | ---: |
| Logs | 15181479844 | 15223616646 | +42136802 |
| Logs/StandaloneBuild/Latest | 3090890512 | 3120254904 | +29364392 |
| cat_cafe_sim/saves/content_images | 1137535625 | 1138858692 | +1323067 |
| C:/Users/hi-wa/unity_workspase/CatCafeSim/Library | 5242177476 | 5320199035 | +78021559 |

Latestは156ファイルの成功ビルド一組、StandaloneBuild直下はLatest/Runsのみ、Pendingなし。Latest約29.4MB増は7人を保持する新人物パックの非圧縮textureとPlayerコード等の収録。Logs約42.1MB増はLatest増加を含み、新人物パック約8.9MB・18枚描画・ビルドログ等。Library約78.0MB増は既存プロジェクトの通常取り込み・ビルドキャッシュ。登録元はPNG一枚と台帳追加で約1.3MB増。原本・旧パック・猫素材・セーブは保持し、手動容量削除なし。容量は終了時スナップショット（記録自身の数百bytesを除く）。

今回分だけを各リポジトリにコミット、pushなし。人物台帳のステージはHEADへ渡辺さん一行だけ追加し、以前からの台帳差分は作業ツリーに残す。リン関連・加入猫設定・猫画像制作日程の差分を混ぜない。

Python `3c4aa11`、実Unity `7ec45f5`。Tool・共有Docsも今回分を個別コミット。
