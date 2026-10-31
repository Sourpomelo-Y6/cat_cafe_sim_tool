# 小川さん（event-guest-41）の人物画像

2026-10-31。直近開発観戦patronルートの41日目・20件の会話本文で「小川さん」と customer_id=`event-guest-41` を照合した。根拠の元ファイル・SHA256・先頭末尾行は Results/OgawaIcon/identity-evidence.json。`guest-29` は名前候補表から得た同名別IDであり、画像を共用しない。祭りの日の来店IDが `event-guest-` + day になる現行 CafeCustomers を保持する。今回の画像はこのIDだけに登録し、別日の同名客へ自動展開しない。

## 採用素材と設定

[採用PNG](../../Docs/Prototypes/OgawaIcon-v1/ogawa-icon.png)、[実行プロンプト・生成元](../../Docs/Prototypes/OgawaIcon-v1/prompt.txt)。組み込みimage_genで一案生成、1254×1254 RGBA、alpha 0〜255。上田さんの採用原本を目視し画風だけを参照。短い波打つ暗褐色の髪、眼鏡なし、赤茶の上着と生成りのシャツ、開いた目と穏やかな閉じ口。画像を目視採用し加工なし。外見のみの新規制作設定であり、年齢・職業・来歴・趣味・固有口調は追加しない。既存会話・数値・来店条件は変更しない。

制作対象 `person-event-guest-41` → ゲーム客 `event-guest-41`。既存SubjectRegistry / HumanPortraitStore経由の保存・再読込・登録・書出し。既存24人の資料と画像行を保持した25人人物専用パック `fab64863a8098fefaf4fb0d3af2dbf82`。同内容の再出力は同じ場所。猫manifest・catalog・本文は保持。通常 ContentPreviewSetup.Import でUnityに取り込む。登録と検証はAPI/fixture経由で、実台帳GUI手入力とは区別する。

## 検証

登録記録、Unity照合、先行差分保持、容量記録は Results/OgawaIcon/。人物Store既存試験は5件成功、GUI/継承分6件skip。最初のsandbox一時フォルダはreplace拒否のため、workspace内TestTempを使って成功。製品コード変更なし。

Unity検証入口は OgawaPortraitVerification.ImportAndBuild、共有 StandaloneScreenSetup / StandaloneBuildStorage を使用。初回制限環境はライセンスexit199。通常環境で取り込み・固定Latestビルドを実施。プレイヤー検証は専用引数 `--catcafe-ogawa-portrait` のときだけ有効な隔離view/信頼会話fixture。自然発生・実クリック・長期評価とは区別する。

現行の来店客一覧と猫プロフィール内の交流相手一覧は文字表示で、人物専用詳細画像欄はない。今回は既存の画像表示先である席・信頼回復会話を検証する。実セーブは読書きせず、QA保存先は Logs/OgawaIcon/Player* 内に限定する。別作業のシロ素材・帰還会話修正を保持する。

## 完了結果

固定Latest最終ビルド成功、ログ `Logs/StandaloneBuild/Runs/20261031-094341-291-6390550749774620b0c239fff5892ebd.log`（CAT_CAFE_OGAWA_IMPORTED / CAT_CAFE_STANDALONE_BUILD_OK、exit0）。同パックの再取り込みでもパック増殖なし。

Player最終exit0、[26項目成功](Results/OgawaIcon/player-result.txt)。25人のdistinct画像解決、同名別ID guest-29/guest-49・別イベント客・未知IDの代替、実席UIの画像/交代/空席、来店客一覧の小川さん表示、席詳細の本人名と猫画像保持、信頼回復会話の本人画像、描画前後の営業・会話状態不変を確認。viewと信頼イベントは隔離fixtureであり、41日目の自然再到達・実クリック・本人の信頼イベント自然発生・保存再開の新規検証ではない。ユーザーセーブは不使用。

[席1024](Results/OgawaIcon/seat-1024.png)・[席1280](Results/OgawaIcon/seat-1280.png)、[詳細1024](Results/OgawaIcon/detail-1024.png)・[詳細1280](Results/OgawaIcon/detail-1280.png)、[会話1024](Results/OgawaIcon/trust-1024.png)・[会話1280](Results/OgawaIcon/trust-1280.png)。目視で茶髪と赤茶の衣服・顔の表示を確認。席は小さく、細かな表情の判読は大表示と区別する。席詳細の大画像は従来どおり担当猫であり、人物画像で上書きしない。

先行差分のhash照合：Tool44ファイル、Python3ファイル、Unity60ファイル。人物台帳は今回追加行以外が開始時と同値。それ以外の先行差分は全てhash一致。PythonコミットはHEAD台帳＋小川行だけをindexに作成し、猫プロフィール差分は作業ツリーへ残した。シロ作業のファイルを混在させない。

容量：Latest 5,591,845,329 → 5,696,644,413 bytes（+104,799,084）、Runs +3,628,068、既存Library 7,972,325,187 → 8,081,268,227（+108,943,040）、content_images +1,344,304。Latest増分は旧人物パックを保持した新25人パックの収録が主因、Libraryは同じUnityの取り込み・ビルドキャッシュ。StandaloneBuild直下はLatest/Runsのみ、Pendingなし。別Build・ZIP・Unityコピーなし。採用素材や旧パック・セーブの削除なし。

コミット：Python `6bf7538`、共有Docs `d68038b`。Unity `bc12943`、Toolは最終報告参照。push・公開なし。

初回1280の席/詳細キャプチャが黒いため証拠から除外し、起動後の解像度切替を追加して再ビルド・再撮影。最終Player-renderedは6枚とも非黒を機械確認し目視確認。実行時チェックは26件成功。
