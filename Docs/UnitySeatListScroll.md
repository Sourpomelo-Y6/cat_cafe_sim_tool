# 営業画面の席一覧：増設後の下段と詳細への到達

2026-10-27。初期2席から現行設定上限8席までを対象とするUI修正。人物24人の画像・プロフィールは再制作せず、素材パックを書き出し直していない。

## 原因と修正

修正前の固定Latestで前田さんの隔離営業を再実行し、5席の通常着席を1280×800／1024×768で描画した。2段目はマスクで本文と詳細ボタンが隠れる。既存ScrollRect自体は縦スクロール可能だが感度1、バーなし、案内なし。通常のホイール移動量が極小で、下段操作へ容易に到達できない。スクロール不能と断定する問題ではなく、発見性と実用的な到達性の問題である。従来カード350高に対し、シーン・prefab・配置再生成には旧252高、基本生成には266高が残っていた。

`SeatListLayout` をランタイム・基本生成・配置再生成の共通定義にした。4列、カード230×350、間隔8、本文15pt・210×164、詳細210×30（y314）を維持。猫画像112×96、人物24×24も維持。上に24高の全席数と操作案内、下に944×488のマスク領域を置く。右の既存余白に16幅の縦バーを設置し、横スクロールなし・端で停止・ホイール感度40・慣性なし。2段分のContent高708に対し移動範囲220。下端で2段目全体と詳細ボタンを表示できる。バーは必要なとき表示する。

一覧を更新しても配置設定を毎フレームやり直さず、詳細開閉・猫一覧との切替でスクロール位置を保持する。客／猫／席の対応とクリック登録、営業・保存・拡張・交流・ADVの処理は変更していない。生成検証中の他UIへの副作用は元のシーン／prefabを復元し、最終成果物には席の配置だけを適用した。

## 検証の区分

- 修正前：既存Playerの前田さん検証を再実行。人気だけを隔離fixtureで調整し、通常の資金獲得・増設・来店・着席を使う既存限定試験154項目。指定2解像度の画面は自動描画。
- 再生成：基本生成、配置再生成、配置再生成の再実行で350高・4列・バー・488高viewport・詳細位置を検査。古い閉店画面の検証条件を現行の見出しへ追従。最終の他UI配置は保持。
- 修正後：専用view fixtureとUGUIのRaycast／PointerClick／Scrollイベントによる限定Player試験。実入力とは区別する。fixture中は定期取得を停止し、実バックエンド更新と保存再読込は別途適用・照合する。
- 実入力：computer-useのskyで可視Playerにマウス／ホイール／バードラッグを送る。描画寸法はPlayerのScreen.width／heightで記録し、ウィンドウ外枠やDPI上の論理寸法と区別する。使用セーブは隔離した既存検証用保存で、ユーザー保存は不使用。

検証入口：`CatCafePreview.Editor.SeatListScrollVerification.Build`。指定 `build_standalone.ps1 -ProjectPath C:/Users/hi-wa/unity_workspase/CatCafeSim -ExecuteMethod CatCafePreview.Editor.SeatListScrollVerification.Build`。Playerは固定Latestへ `--catcafe-seat-scroll <絶対出力先>`。最初に既存の8席検証save/slot-1.jsonを出力先saveへコピーする。`--seat-scroll-hold` は自動検証後に1280／1024の実入力用fixtureを保持し、出力先のdone-1280.txt／done-1024.txtで各確認を終了する。

途中の失敗は成功根拠にしない：制限環境のUnityライセンス接続待ち、古い閉店表示チェック、生成直後のモーダルの描画待ち不足、通常ポーリングによるfixture置換。いずれも作業環境／専用検証側で対処し、製品の営業ルールは変更しない。

## 最終結果

[Player432項目成功](Results/SeatListScroll/result.txt)／[項目別結果](Results/SeatListScroll/checks.txt)／[描画寸法](Results/SeatListScroll/dimensions.txt)。2〜8席を各解像度で確認し、4→5席の境界を含む。先頭・中間・末尾の詳細はUGUI Raycastからクリックし、席番号・客名・猫名を照合。全下段カードの四隅と詳細がviewport内にあること、ホイールとバーの上下端、モーダルのスクロール遮断、詳細開閉・一覧切替・再表示後の位置保持、客交代時の画像とクリック先、空席で両画像が消えること、通常営業1刻更新後の8席対応、保存と別backendでの読込を確認した。停止中／通常自動進行ONの両状態で、ホイール操作自体は営業状態を変えない。自動進行の長時間タイマー試験やおまかせ一式は今回繰り返していない。

可視Playerは終了コード0。実入力は[入力記録](Results/SeatListScroll/physical-input.txt)と[1280の確認](Results/SeatListScroll/done-1280.txt)／[1024の確認](Results/SeatListScroll/done-1024.txt)を参照。両解像度で、席1＝佐藤さん／コハク、席5＝前田さん／タマ、席8＝上田さん／ユキの詳細を実クリックした。ホイール1入力で40 logical px移動し、バーを下端へドラッグするとoffset220に到達。最下段の席5〜8は画像・本文欄・詳細ボタンを含めて表示され、閉じてもoffset220を保持。1024ではバーで上端へ戻す操作と、席8詳細→担当猫ユキの詳細→席一覧への復帰も確認した。fixtureの配置を実入力で操作した確認であり、8人の自然同時着席を主張するものではない。

実描画は1280×800／1024×768。skyが取得した枠込み画像は1282×832／1026×800で、判定にはPlayerの実描画値を使用。カードは230×350 logical px（1280では230×350px、1024では184×280px）、人物は24／19.2px、viewportは944×488 logical px（1024では755.2×390.4px）。最終席の詳細中心は1280で画面下から45px、1024で164px。サイズ変更は1280→1024→1280→1024の順で実施した。

|範囲|1280×800|1024×768|
|---|---|---|
|初期2席|[画面](Results/SeatListScroll/initial-2-1280.png)|[画面](Results/SeatListScroll/initial-2-1024.png)|
|修正前5席|[下段切れ](Results/SeatListScroll/before-5-1280.png)|[下段切れ](Results/SeatListScroll/before-5-1024.png)|
|修正後5席下端（自動UI）|[画面](Results/SeatListScroll/after-5-1280.png)|[画面](Results/SeatListScroll/after-5-1024.png)|
|最大8席下端（実入力後）|[全下段カード](Results/SeatListScroll/after-8-1280.png)|[全下段カード](Results/SeatListScroll/after-8-1024.png)|
|最後の席8の詳細（実クリック）|[上田さん／ユキ](Results/SeatListScroll/physical-detail-8-1280.png)|[上田さん／ユキ](Results/SeatListScroll/physical-detail-8-1024.png)|

再生成成功ログ：`Logs/StandaloneBuild/Runs/20261027-042405-086-e0217bcd60be4838ab8b9e440dac3b7e.log`（CAT_CAFE_SEAT_SCROLL_REGEN_OK）。専用QAのフレーム待ちを修正した最終成功ビルド：`Logs/StandaloneBuild/Runs/20261027-042931-593-a6fa85cc14f449bdb57ae7b0b35461f4.log`（CAT_CAFE_STANDALONE_BUILD_OK、exit0）。最終Player記録：`Logs/SeatListScroll/Visible`。途中の非表示起動で黒くなったスクリーンショットは採用せず、資料の修正後画面は可視Playerで再取得したものを使用。バー値の検証にはScrollRectのLateUpdateを待ち、浮動小数の境界誤差は0.2 logical px以内だけ許容する。

## 保持・容量・残る確認範囲

[先行差分200ファイルは全件hash一致](Results/SeatListScroll/prior-preserved.json)。猫画像制作・素材・catalog・プロフィール・日程・セーブと既存の他作業を保持した。シーン保存時にUnityが付けた、席と無関係な無効best-fitのmax値7件の正規化だけは元の値へ戻し、不要な差分に含めていない（best-fit無効のため描画差なし）。

[開始容量](Results/SeatListScroll/capacity-start.json)／[終了容量](Results/SeatListScroll/capacity-final.json)。単位bytes。

|対象|開始|終了|増減|
|---|---:|---:|---:|
|Latest|4,289,663,080|4,289,680,386|+17,306|
|ビルド実行ログRuns|197,409,110|201,431,058|+4,021,948|
|Logs全体（Latest含む）|16,865,851,133|16,893,060,843|+27,209,710|
|既存Unity Library|6,546,974,655|6,546,993,814|+19,159|
|制作素材content_images|1,220,723,801|1,220,723,801|0|

Logs増約27.2MBは実行ログ・隔離保存・画面・調査記録が中心。本件Logsは約23.2MB、コミットする画像と検証記録は約1.8MB（容量取得時点）。Latestは156ファイルの成功物一組、StandaloneBuild直下のディレクトリはLatestとRunsだけ、Pendingなし。追加のUnityプロジェクト・Library・ビルドZIP・素材パックは作成していない。ビルドの一時Pendingは共有処理が成功後にLatestと置き換えている。

未確認：タッチ操作・他DPI/GPU・指定外解像度・最大8席の自然同時着席からの長時間営業・おまかせ一式・全目標の長期評価。個別交流の大画像UI／ADVの総合確認も今回再実行していない。今回の対象である2〜8席・指定2解像度の下段到達と詳細実クリックに未確認の残件はない。

変更リポジトリはTool・実Unity・共有Docsのみ。Pythonゲーム側の今回変更なし。今回分だけコミットし、pushしない。


コミット：実Unity `f0f4dc7`、共有Docs `1f48c56`。Toolの今回コミットは最終報告を参照。
