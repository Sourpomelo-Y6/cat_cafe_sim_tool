# 編集時のゲームUIプレビュー

2026-10-29。Sceneビューが古い見本表示（自動進行・旧猫紹介欄）で、Play中の営業準備と異なる問題への対応。

## 理由

シーンに保存された静的な素材プレビューは、ContentPreview.Start→PythonStateClient.Accept→PythonBusinessHeader.RefreshControls／PythonServiceScreen.Renderが走る前の状態。Play中はゲーム状態に応じてボタンの位置・表示、右側説明、状態本文が更新される。前回の猫詳細の重なり修正とは別の問題。GameビューのScale設定だけでは編集時の旧表示は更新されない。

## 対応と使い方

BusinessEditPreview（Editor専用）を追加。初期の営業準備表示を、実行時と同じLocalCafeBackend.ViewとPythonStateClient.Acceptでシーンへ反映して保存する。再生しなくても現在の営業準備の配置・ボタン表示を確認できる。

CatCafePythonStateシーンを編集モードで開き、メニューから明示的に切り替えられる。

- Cat Cafe → 編集プレビュー → 営業準備を表示
- Cat Cafe → 編集プレビュー → 猫詳細・個性と外見を表示

手動メニューはUndo対応。表示変更はシーン変更として扱うため、必要なら通常のシーン保存を行う。毎フレーム再適用やExecuteAlwaysは使わず、編集内容を自動で戻さない。実セーブをロードせず、存在しない固有一時ディレクトリを指定した新規メモリ状態を表示。進行コマンド、保存API、Unityビルドは実行しない。

営業中やロード済みの進捗・客席等の動的内容が、静的プレビューと同じになるという意味ではない。再生時は通常どおり本来のゲーム状態で更新される。

## 確認

BusinessEditPreview.VerifyAndSaveで営業準備の自動進行非表示・営業準備ボタン表示・旧猫紹介の除去、猫詳細の本文488×236を検査。1280×800／1024×768の営業準備と、猫詳細のCanvasを描画。証跡はLogs/EditorLayoutPreview。物理マウス入力・全画面の編集プレビューは対象外。

検証成功。保存する初期表示は営業準備ダイアログを閉じた猫一覧。容量bytes：{"before": {"Logs": 20109581747, "Latest": 5506516854, "Library": 7890064146}, "after": {"Logs": 20110450571, "Latest": 5506516854, "Library": 7891773893}, "delta": {"Logs": 868824, "Latest": 0, "Library": 1709747}}。成功Latest1組・Pendingなし、ビルドなし。Library変化はUnity自身のバージョン別キャッシュ更新。ProjectVersionは作業前のf2へ復元した。
