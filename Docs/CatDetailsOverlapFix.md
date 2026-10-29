# 猫詳細の本文欄と下段操作の重なり修正

2026-10-29後続：モモ27metaを恒久整合し、通常入口の固定LatestへUI修正を反映済み。Editor Canvas／Game／Standaloneの両解像度で5タブ・長文末尾・下段境界・一覧復帰と専用保存の別PID再開を限定確認。以下の配布版未反映は先行検証時点の記録。 [配布反映追補・確認区分・依存・容量](UIDistributionFollowup.md)。

2026-10-29。ユーザー提示のUnity Gameビューで「個性・外見」本文欄が右へはみ出し、下段の操作へ重なる問題を修正。

## 原因と変更

PythonCatDetailsLayoutは860×600のカード内に、本文を位置348,126・488×236で配置する。一方、PythonPlayerSetup.Buildは単一本文欄だった旧UIを前提に、同じ欄を812×426へ拡張し、交流ボタンを下端へ作り直していた。現在の保存済みCatCafePythonStateシーンにもこの上書きが存在した。本文欄の右端がカード幅を超え、下段と重なる。Maskの不具合ではなく、スクロール領域自体の誤った大きさが原因。

PythonPlayerSetupの旧詳細変更はstateDetailsがない旧単一欄だけへ限定し、分割済み詳細の本文・操作を保持。実Unityシーンは猫詳細の配置を修復し、それ以外のシーンオブジェクトは元の内容を保持した。素材・本文・数値・保存形式の変更なし。

## 検証と制限

既存PythonCatDetailsLayout.BuildAndVerify成功。猫詳細再生成→交流画面再生成の順を再現し、本文488×236と交流ボタン位置が保持されることを確認。実LocalCafeBackend.Viewの5タブを1280×800／1024×768でCanvas描画し、右端・下端・本文幅の境界を検査。個性・外見の画面で重なり解消を目視。証跡はLogs/CatDetailsFix/editor-result.txt、editor-0～4-1280/1024.png。画面はfixture／Canvas描画であり、物理マウス入力や自然プレイ全件の確認ではない。

指定スクリプト・固定Latestでビルドを試みたが、並行モモ画像27枚（有効cbe0…パック内）の既存texture policy不一致で停止。今回のUIコードのコンパイルとEditor検証は成功。ガードを無効化せず、画像meta・PNGは変更していない。直前の成功Latestを保持しPendingは削除される共有処理を使用。今回修正の配布版反映は未完了。旧ユキ・アキ54metaの解決済み残件とは別件。

再現検証入口：CatCafePreview.Editor.CatDetailsOverlapVerification.VerifyAndBuild。保存済み配置の読取確認はVerifySaved。通常のビルド手順はAGENTS.mdの固定Latest／Pending／Runsに従う。

## Editorと配布版の見た目

UnityのGameビューのAspect・解像度・Scaleは表示領域と拡大率を変える。比較時は同じ解像度へ揃える。制作ToolはPythonの制作画面で、Unityゲームとは別UI。さらに、Editorのシーン変更はビルドし直すまで既存exeへ反映されない。今回の重なりは上記のシーン配置不整合が直接原因で、単なる表示倍率差ではない。

Unity検証は既存環境で成功実績のある2021.3.45f1を使用。ProjectVersion.txtは作業前のf2表記に戻す。ライセンスIPCへ接続できないsandbox内の試行は停止し、通常権限環境で検証した。

終了照合：保存済みシーンのVerifySavedも成功。容量bytes：{"before": {"Logs": 20107470715, "Latest": 5506516854, "Library": 7892776753}, "after": {"Logs": 20109581458, "Latest": 5506516854, "Library": 7891753073}, "delta": {"Logs": 2110743, "Latest": 0, "Library": -1023680}}。Latest増減0、成功物1組、Pendingなし。Libraryの減少はf2→f1検証時にUnity自身がBeeキャッシュを更新した影響で、手動の容量整理は行っていない。
