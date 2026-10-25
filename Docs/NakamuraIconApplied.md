# 中村さん（guest-8）のプロフィール・人物アイコン

2026-10-25。一人・一案を制作。既存の人物登録、画像登録、人物専用パック書き出し、客ID別表示を使用。プロフィール自動生成・本文追加・UI再設計・ルールや数値の変更は行わない。

## 採用設定

- 制作対象ID `person-guest-8` → ゲーム客ID `guest-8`。名前から推定しない。
- 性格：穏やかでおおらか。猫の過ごし方に合わせ、静かな時間も楽しむ。
- 接し方：少し距離を置いて短く誘い、反応を待つ。応じなければ繰り返さず、休憩を尊重する。
- 口調：やわらかな短い丁寧語。「〜してみますか」「〜でも大丈夫ですよ」「ゆっくりしましょう」。猫の呼び名 `{cat}`。命令・幼児語・結果の決めつけを避ける。
- 外見：中央分けの濃い茶髪、肩の上までの幅のあるウェーブ。丸みのある卵形輪郭、眼鏡なし、淡い空色のカーディガンと象牙色の襟、口を閉じた微笑み。

確定設定は固定客定義の名前・客ID・登場条件。台帳・資料に専用設定・画像・途中制作はなく、上記の性格・口調・外見を今回の制作設定として採用した。年齢・性別の文章設定・職業・来歴・来店歴は固定しない。既存本文は変えない。

[採用PNG](../../Docs/Prototypes/NakamuraIcon-v1/nakamura-icon.png)：1254×1254 RGBA、alpha0〜255。SHA256 `933103ca4b0b5258feee1edc393944e8bcb4d3e8cdf526da7568de5f4d723ef6`。[全プロンプトと参照](../../Docs/Prototypes/NakamuraIcon-v1/README.md)。imagegenスキル・組み込みimage_genを一回使用。既存8人を目視し、山本・渡辺・田中の採用画像を画風参照に入力。大きな髪の輪郭と服の色面を優先。比較案・表情差分・立ち絵・シートなし。

## 登録・保護

開始時Tool `aab3e45`、Python `031a449`、実Unity `5e60fc1`、共有Docs `91c87b8`。AGENTSと指定資料・最新履歴・未コミット差分を確認。人物制作関連チャットはidle、Unity/Playerプロセスなし。既存リン制作・日程・猫設定・素材・人物台帳のcatalog-2差分を保持。共有作業の並行実行は見られなかった。

`register_nakamura.py` は既存SubjectRegistry.save / HumanPortraitStore.register/exportを利用。実台帳の保存と別インスタンス再読込、既存全人物行と8画像対応の完全保持を確認。[登録結果](Results/NakamuraIcon/registration.json)。人物専用9人パック `047ea83cdbe061bc3e639be8f4c7a62e` を一組だけ書き出し、再書き出しは同じ出力を利用。猫画像・猫catalogは再書き出ししない。通常ContentPreviewSetup.Importを使用し、9画像のhash・明示ID・既存Importerを[検査](Results/NakamuraIcon/unity-data-checks.json)。Default max1024・非圧縮・alpha保持・mipmap/read-write無効、Standalone overrideなし。猫BC7は適用しない。

登録前後の猫manifest・登録済み本文・実Unity猫catalogのSHA256一致。旧パック・素材・ユーザーセーブは削除/更新しない。今回の人物台帳コミットはHEADへ中村さん一行だけを加え、catalog-2の先行差分を作業ツリーに残す。

## 検証区分と登場条件

- Toolデータ検査：保存再読込、既存人物・8画像保持、透過、9人書き出し・同出力再利用、保護hashが成功。
- Toolバックエンド5件成功・GUI6件skip。[ログ](Results/NakamuraIcon/tool-tests.txt)。GUI有効で11件成功（継承によるバックエンド5件の再実行と実Tkボタン1件を含む）。[ログ](Results/NakamuraIcon/gui-tests.txt)。隔離データ・ファイル選択等を差し替えたGUI自動操作であり、実台帳の手入力登録ではない。
- 人気追加客Python回帰8件成功・1件skip。[ログ](Results/NakamuraIcon/popular-tests.txt)。UTF-8とワークスペース内一時領域を使用。
- 登場条件：Python cafe_customersの固定名とcore/cafe_weekdaysのpopular_customers/popular_unlocked_day/popular_schedule、標準設定、Unity CafeCustomers.CustomerScheduleを確認。中村さんは通常6人の次の人気追加客第1段階4人の2人目。達成記録が必要、`day > resolved_day`、通常到着刻12、曜日制限なし。雨天・不満/信頼の来店制限は引き続き適用される。条件を緩めない。
- Player準備は解放済みfixture。通常の人気挑戦開始後、隔離メモリ上の人気だけを上限へ置き、通常休業決算で第1段階達成。未解放・達成当日はguest-8なし、翌日予定に入る境界を確認。自然な人気達成からの登場の実証ではない。
- 最初の3席fixtureは先客で満席、次の即4席購入は資金不足、通常営業後の4席でも先客で満席だった。通常営業売上から5席まで購入しても同時刻のguest-3が先に入るため、到着刻12の処理後、既存のset_auto_assignment/assign_serviceコマンドで空席へguest-8を優先案内し、自動割り当てを戻す準備へ修正した。途中失敗を成功根拠に含めない。データやルール・数値・到着刻・待ち時間を変更しない。
- 席/信頼回復/予約Extraは実Playerのview/event/replay fixtureによるUI自動描画。信頼回復は実案件のcustomer_idから解決する。自然な信頼発生・長期評価・総合確認一式は実施しない。

## 最終Player・実入力結果

指定build_standalone.ps1、既存実Unity、共有StandaloneBuildStorageの固定Latestで成功。最終ログ `Logs/StandaloneBuild/Runs/20261025-153649-097-3cad8cf33ef2427bad4451013977c28c.log`、exit0・CAT_CAFE_STANDALONE_BUILD_OK。制限環境の初回はライセンス接続exit199で、通常環境の同スクリプトに切り替えた。検証準備の修正と同期漏れによる再ビルドでも固定Latestを置換し、別Build/ZIPを増やしていない。

`--catcafe-human-icons <隔離出力> --catcafe-nakamura` はexit0・[78項目成功](Results/NakamuraIcon/player-result.txt)。3日目13刻、既存手動割り当てでguest-8を第5席・ムギ担当に案内。通常save_game/load_gameで別backendへ再開し全席の客ID保持。9画像の別々の解決、未知ID・未登録guest-24・メモリ上だけの欠損参照で共通人物記号、参照復帰、客交代・空席更新、guest-8の実客IDを持つ信頼案件fixture、森さん予約/Extra・客IDなし旧Extraの代替を確認。描画・回想前後の状態JSON（保存本文を含む）一致。

同じLatestを別プロセスで `--catcafe-human-icons-resume` 追加起動し、通常load_gameでguest-8の第5席・ムギ担当を復元。[5項目の自動検査](Results/NakamuraIcon/manual-result.txt)と[復元view](Results/NakamuraIcon/resumed.json)。続いてcomputer-useスキルの実マウス・キー入力でAlt+Enter全画面、猫一覧、席一覧へ切替。ムギの担当人物アイコンと右欄「中村さん 接客中/席5」を確認。[実入力メモ](Results/NakamuraIcon/input-notes.txt)／[実入力後画面](Results/NakamuraIcon/manual-after-input.png)。実入力時は1920×1080であり、指定2解像度の物理入力確認とはしない。ホイール入力では第5席カード下部を表示できず、5席表示の第2段下部がviewportで切れる既存レイアウト制約は今回修正しない。別プロセス5項目の汎用結果文の「physical input claimなし」は自動検査部分を指し、後続の入力証拠は別記録。

[席1280](Results/NakamuraIcon/seat-guest-8-1280.png)／[席1024](Results/NakamuraIcon/seat-guest-8-1024.png)／[信頼1280](Results/NakamuraIcon/trust-guest-8-1280.png)／[信頼1024](Results/NakamuraIcon/trust-guest-8-1024.png)を目視。濃茶ウェーブの幅のある輪郭・空色服を保持。席の小アイコンでは細線の判読を保証しない。全23PNGの[実寸と非黒画像検査](Results/NakamuraIcon/image-checks.json)も成功（22自動描画＋実入力後1920画像）。席撮影fixtureでは第1席だけID/表示名を置換するため第1・第5席に中村さんが見えるが、保存状態は第5席一人で、右欄は本来の状態を保持する。二重客登録ではない。

自然な人気獲得からのguest-8登場、自然な信頼回復発生、実台帳の手入力登録、指定2解像度での実入力、5席カード全体の表示は未確認/制約あり。長期評価・総合確認一式は繰り返さない。

## 容量（bytes）

| 対象 | 開始 | 終了 | 増減 |
| --- | ---: | ---: | ---: |
| Logs | 15274875950 | 15336051718 | +61175768 |
| Logs/StandaloneBuild/Latest | 3153815688 | 3191571840 | +37756152 |
| cat_cafe_sim/saves/content_images | 1140146088 | 1141645757 | +1499669 |
| C:/Users/hi-wa/unity_workspase/CatCafeSim/Library | 5313572905 | 5355579166 | +42006261 |

Latestは156ファイルの成功ビルド一組。直下Latest/Runsのみ、Pending・別Build・ZIP・Unityコピー・追加Libraryなし。Latest約37.8MB増は既存人物を保持した新9人パックの非圧縮texture等。Logs約61.2MB増はLatest増分を含み、人物パック・23PNG・複数回の固定出力ビルドログ/検証記録等。登録素材約1.50MB増、既存Library約42.0MB増は同プロジェクトの通常取り込み/コンパイルキャッシュ。旧パック・素材・ユーザーセーブを削除せず保持。記録自身の微小増分は終了容量スナップショット後。

今回分だけを各リポジトリへコミットし、pushなし。リン制作・日程・猫設定・台帳catalog-2の先行差分は作業ツリーに保持する。

登録コミット：Python `b22744e`、実Unity `d3204a6`、共有Docs `3026f9c`。Toolは本資料と同じコミット。pushなし。
