# CatCafeSim Lite 公開前の素材確認

2026-11-12。対象は軽量版コミット `307e4d6` と、その Windows x64／Mono 検証ビルド。

[画像照合表](CatCafeSimLiteImageRights.csv) に追跡対象の PNG 108ファイルの SHA-256 と一致先を記録した。軽量版の全件が収録一覧のハッシュおよびワークスペース内の制作ファイルまたは登録用コピーと一致した。照合表の asset は軽量版ルート、matched_sources は CatCafeSim ワークスペースルートからの相対パス。

| 対象 | ファイル数 | 照合結果 |
| --- | ---: | --- |
| ミケ・タマ・ソラ | 90 | 制作フォルダの姿・反応・実行画像と照合。ミケの初期参照画像に遡る入力の由来と制作時の条件が残件 |
| 来店客 | 12 | 採用肖像と照合。佐藤さんは現行 v2。旧Aの PNG は収録外で、旧Aからの参照の有無は別途確認 |
| 個別イベント | 2 | 窓辺とミケが近づく挿絵。制作ツールの登録用コピーと Unity の導入コミット `7697dec` を確認。生成元ジョブ・入力・適用条件の確認が残る |
| 共通 | 4 | タイトル背景、猫シルエット、共通客アイコン2配置。タイトル背景は制作フォルダ、ほかは登録用コピーと照合 |

過去に調査した旧 ComfyUI 猫画像8枚と旧佐藤さんAの識別ハッシュは、軽量版の108ファイルに一致しなかった。後続画像の制作に使った参照入力の確認は引き続き必要。既存の記録を参照する。 [出典監査](../../Docs/ThirdPartyAudit20261029.md) ／ [残件追補](../../Docs/ThirdPartyFollowup20261031.md) ／ [現行の人物登録](HumanIconsApplied.md)

## 同梱文書

軽量版にクレジット、M PLUS 1p の OFL 参照、Player の DLL と版を照合した Unity 14パッケージのライセンス・第三者表示原文23ファイルと Unity Companion License 1.4 を用意した。Windows Player 本体には Mono ランタイムとクラスライブラリもある。Unity 2021.3.45f2 Windows Mono 向けの Player 本体の第三者表示原文は、公式リリースページとローカルの Windows Build Support から特定できていない。手元に保管している f1 版の PDF は版が異なる。 [軽量版のクレジット](../../CatCafeSimLite/CREDITS.md) ／ [軽量版の第三者表示](../../CatCafeSimLite/THIRD_PARTY_NOTICES.md) ／ [Unity 2021.3.45f2](https://unity.com/releases/editor/whats-new/2021.3.45f2)

## 公開前の判断

### 制作記録の追加確認

| 対象 | 確認できた制作経路 | 確認が残る項目 |
| --- | --- | --- |
| ミケ | 10月3日の初期比較原画 `calico-actions-v1.png` は Python 側の制作記録に内蔵画像生成とある。10月4日にこの画像を ChatGPT Images の入力として4場面比較を作り、その比較を画風と背景の参照に通常ポーズを制作。10月8日の基本ポーズ9枚は `mike-normal.png` を参照 | 10月3日の生成ジョブ、初期原画に入力した画像の有無と由来、制作時の適用条件 |
| 佐藤さん v2 | 10月25日の試作記録は組み込み image_gen を使用し、既存A案の短髪・丸眼鏡・緑の服を引き継いだと記載。現行肖像は v2 の採用ファイルと SHA-256 一致 | v2 ジョブに添付した画像の実体、A案の比較シートを作った際の入力と由来、制作時の適用条件 |
| イベント挿絵2枚 | 制作ツールのコミット `4351a94` と Unity のコミット `7697dec` が同じ挿絵を登録。窓辺画像とミケが近づく画像の用途を記録 | 生成サービス、入力画像、生成ジョブ、適用条件 |

[初期原画の制作記録](../../cat_cafe_sim/Docs/CatActionPreview.md) ／ [ミケの4場面比較](../../Docs/Prototypes/MikeChatGPTImages-actions-v1.prompt.md) ／ [通常ポーズの制作記録](../../Docs/Prototypes/CatAppearance-v1/README.md) ／ [基本ポーズの制作記録](../../Docs/Prototypes/MikeBasePoses-v1/Generation.md) ／ [佐藤さん v2 の制作記録](../../Docs/Prototypes/SatoPortrait-icon-v2/README.md)

画像の公開方針案は、出典と利用範囲を確認できた画像をソースとゲームに収録し、コードは既存の MIT 表示を維持する。画像の改変・素材単体の再配布・商用利用については、権利者が認める範囲を画像用の案内に明記する。確認が残る画像には、記録を補うか、入力と適用条件を記録できる素材に差し替える選択肢がある。

Unity の 2021.3.45f2 公式リリースページに Windows Mono のインストーラーと第三者表示の案内がある。対象版の Player 用原文はページから特定できなかった。次に Unity サポートへ照会する場合の指定は「2021.3.45f2、changeset 88f88f591b2e、Windows x64、Mono Player」。 [Unity 公式リリース情報](https://unity.com/releases/editor/whats-new/2021.3.45f2)

1. **画像の利用条件**を決める。GitHub のソースに画像ファイルを入れる許諾と、ゲーム本体に組み込んで配布する許諾を確認する。利用者による画像の改変・素材単体での再配布・商用利用をどこまで認めるか決め、既存のコード用 MIT 表示と区別して明記する。
2. **生成時の条件と参照入力**を確認する。ミケの初期参照、佐藤さん v2 に至る制作経路、イベント挿絵の生成履歴を優先する。各制作時のサービス、アカウント区分、適用条件、参照画像を確認し、権利者と利用範囲を判断する。OpenAI の公開規約には出力に関する規定と入力に必要な権利に関する規定がある。個々の制作への適用は当時の契約と制作記録で照合する。 [個人向け利用規約](https://openai.com/policies/terms-of-use/) ／ [事業者向け契約](https://openai.com/policies/services-agreement/)
3. **Windows Player の表示**を仕上げる。配布するビルドと一致する f2 Windows Mono の第三者表示を取得して同梱し、Player 一式とクレジットを照合する。

確認が残る画像を差し替える場合は、公開する Git 履歴に当該画像の旧版が含まれるかも確認する。公開候補を決める時点で、配布用のファイル一覧と Git の履歴を再確認する。

### 収録判断に必要な確認

- 猫画像90枚のうちミケ33枚、タマ28枚、ソラ29枚。通常ポーズ制作記録ではミケの初期比較を全猫共通の画風・背景参照としている。初期入力の確認が難しく差し替えを選ぶ場合は、ミケの33枚に加え、タマ・ソラへの参照のつながりも確認して対象を決める。 [通常ポーズの制作記録](../../Docs/Prototypes/CatAppearance-v1/README.md)
- 佐藤さんの現行 v2 は来店客12枚のうち1枚。差し替える場合は `guest-1` の肖像参照を更新する。個別イベントの挿絵2枚について、採用記録からはイベント定義と表示用途を確認できた。差し替える場合は共用の窓辺背景とミケの分岐スチルを置き換え、会話と回想の両方で確認する。
- 現行画像を GitHub の公開リポジトリに置く場合は、GitHub 上での閲覧と fork を含む公開範囲を了承したうえで、画像の改変・商用利用・GitHub 外での再配布の条件を決める。画像を差し替える場合は、初回コミット `eff571e` にも現行素材が入っているため、確定した素材から公開用履歴を作る。 [GitHub のライセンス案内](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/licensing-a-repository)
- Windows 配布の照会文案。「Unity 2021.3.45f2（changeset 88f88f591b2e）でビルドした Windows x64 Standalone Mono Player をゲームに同梱する予定です。この版の Player に対応する第三者通知（Third Party Notices）の原文または正式な取得先を教えてください。」インストール済みファイルの確認では UnityReferenceAssemblies やライセンスクライアントの通知は見つかったが、対象の Windows Mono Player 用原文は特定できていない。 [Unity 公式リリース情報](https://unity.com/releases/editor/whats-new/2021.3.45f2)

### 2026-11-14 の進捗

現行画像108ファイルを使って準備を進める。制作環境は Illustrious または ChatGPT Images とのユーザーの認識を記録し、個々の画像への対応は既存の制作記録に従う。画像の改変・商用利用・素材単体の再配布の許諾は未指定。

GitHub 上の `Sourpomelo-Y6/CatCafeSimLite` は公開済み。ローカルの `main` と追跡先 `origin/main` はともに `4ca8b05`。ローカルの全参照からたどれる履歴を点検し、LFS ポインタ、LFS 属性ファイル、Unity のキャッシュ、Player 実行ファイル・配布アーカイブの混入は見つからなかった。最大ファイルは営業シーンの 6,217,550 バイト。 [GitHub リポジトリ](https://github.com/Sourpomelo-Y6/CatCafeSimLite)

Windows 検証ビルドの UnityPlayer・Mono・クラスライブラリ等は、既存のランタイム監査24ファイルとすべて SHA-256 が一致。対象版の Player 用第三者通知は引き続き未取得。 [ランタイム監査](../../Docs/ThirdPartyAudit20261031Runtime.csv)

Windows 配布時は `CatCafeStandalone.exe`、`CatCafeStandalone_Data`、`MonoBleedingEdge`、`UnityPlayer.dll`、`UnityCrashHandler64.exe` を一式として扱い、クレジット・第三者表示と参照先のライセンス原文（M PLUS 1p の OFL を含む）を添える。`CatCafeSimLite_BurstDebugInformation_DoNotShip` は配布対象から外す。利用者向け案内には「展開したフォルダ内で CatCafeStandalone.exe を起動してください。Windows x64 用です。軽量版は新規ゲームから開始してください」と記載する。
