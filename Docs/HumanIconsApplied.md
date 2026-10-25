2026-10-25追補：小林さん（`person-guest-9` → `guest-9`）一案を制作・登録。既存9人を保持する10人人物パックで通常Unity取り込み・固定Latestへ反映。Player84項目・別プロセス再開5項目・1024実入力を確認。[採用プロフィール・画像・検証区分・容量](KobayashiIconApplied.md)。

2026-10-25追補：中村さん（`person-guest-8` → `guest-8`）のプロフィールと透過アイコン一案を制作・登録。既存8人を保持し9人人物パックで通常Unity取り込み。[採用設定・検証区分・容量](NakamuraIconApplied.md)。

2026-10-25追補：山本さん（`person-guest-7` → `guest-7`）一案を制作・登録。既存7人を保持し8人人物パックでUnity/固定Latestへ反映。解放条件境界・解放済みfixture着席/保存再開とPlayer71項目を確認。自然な人気達成・実入力とは区別。[採用プロフィール・画像・検証・容量](YamamotoIconApplied.md)。

2026-10-25後続：渡辺さん（guest-6）の一案を加え、採用は初期客6人＋森さんの7人。Tool登録・人物専用パック・通常Unity取り込み・固定Latest・保存再開を確認。[採用・Player62項目・容量](WatanabeIconApplied.md)。以下は最初の3人の採用時点の記録。

# 3人の小アイコンをゲームへ反映

2026-10-25。ユーザーの「ではゲーム内で反映してください」により、佐藤さんの簡略版と鈴木さん・森さんの案を採用し、Tool登録→人物専用書き出し→通常Unity取り込み→固定Latestビルド→配布Player確認まで実施した。[登録・表示仕様](HumanPortraits.md)。今回は新たな画像生成を行っていない。

| 名前 | 制作対象ID | ゲーム客ID | 採用元 | 人物画像hash |
| --- | --- | --- | --- | --- |
| 佐藤さん | guest-1 | guest-1 | 共有Docs/Prototypes/SatoPortrait-icon-v2/sato-icon-v2.png | 0c815814fbaa02bf25092e99e7bc57ed858a9270e52bb679d97bd8117400b7f7 |
| 鈴木さん | person-952c631405e649fdb50c390881804977 | guest-2 | 共有Docs/Prototypes/CustomerIcons-v1/suzuki-icon-v1.png | ce47440c0d108d859835f07a5cac37a660acf4024e4dd2ea5ac4c90a08550852 |
| 森さん | person-reservation-longhair（新規） | reservation-longhair | 共有Docs/Prototypes/CustomerIcons-v1/mori-icon-v1.png | deabee0dcdeaa1a80d03369f10dc0251f57f57ba1e24921919c22d3d32a53306 |

森さんはspeechの制作対象だけを追加し、既存ID・ルール・人物の口調や本文は変更しない。以前から未コミットだった人物台帳の行は開始前と同値を確認し、今回のコミットはHEADに森さんの一行だけ加えた内容とする。ゲーム保存へ画像フィールドは追加しない。画像は人物画像台帳の明示ID対応から解決する。

採用画像は全て1254×1254 RGBA、alpha 0〜255。人物Importerは1024上限・非圧縮・mipmap/read-write無効・alpha保持。猫用BC7は人物へ適用しない。佐藤さんの以前の詳細画像と旧パックは保存したまま、新しい現在の対応だけを切り替える。

人物パックは `de2ba2151ca506e0a30b384e81b3981c` の一組。`HumanPortraitStore.register/export` から書き出し、`ContentPreviewSetup.Import` の既存入口で取り込んだ。猫画像・猫catalogの再書き出しはしない。未登録・未知ID・旧Extra客IDなしは既存の人用記号へ戻る。

## 配布Playerの確認

指定build_standalone.ps1、既存Unityプロジェクト、固定Latestを使用。最終成功ログは `Logs/StandaloneBuild/Runs/20261025-120139-374-d01ad88fd02f4206a50f18745fcb16fd.log`。exit0とCAT_CAFE_STANDALONE_BUILD_OKを確認。

Latestを `--catcafe-human-icons <専用出力>` で起動し、[31項目成功](Results/HumanIconsApplied/result.txt)。1280×800と1024×768で3人それぞれの席アイコン、鈴木さんの信頼回復、森さんの予約Extraを配布Player上で描画した。席の客交代・未知ID代替・空席、保存→別backendで再開した客ID保持、旧Extraの名前からIDを推定しない代替、予約ADVの森さんID解決、描画・Extra前後の営業状態一致を確認。専用のview/event/replay fixtureを使う自動検証であり、自然発生・実入力・長期評価は行っていない。右側の来店客一覧を差し替えていないため、席を森さん/鈴木さんへ置換した描画fixtureの右欄は元の佐藤さんのままである。

[佐藤さんの席](Results/HumanIconsApplied/seat-guest-1-1024.png)／[鈴木さんの席](Results/HumanIconsApplied/seat-guest-2-1024.png)／[森さんの席](Results/HumanIconsApplied/seat-reservation-longhair-1024.png)／[鈴木さんの信頼回復](Results/HumanIconsApplied/trust-suzuki-1024.png)／[森さんの予約Extra](Results/HumanIconsApplied/reservation-extra-mori-1280.png)。席は既存34 logical pxで、1024画面では約27px。髪型・眼鏡の有無・茶髪/灰髪・衣服色という大きな違いを優先。細かな目や髪の線まで読めるとの保証にはしない。

## ビルド前提の整合と保護

初回は現行猫パックだけに既存CatTexturePolicyを適用したが、保持中の旧4762…パックのリン27画像でガードが拒否。終了済みEditorのロックが残り、実Editor不在とロックの非使用を確認してその一ファイルだけ除去した。その後、既存の明示的 `CatTexturePolicy.ApplyAll` を登録前提処理として実行し、通常のビルドガードを迂回せず成功した。元PNG・bundle/interaction JSONのhashは開始前と一致。変更した猫Importer metaは旧パックの27件のみで、他作業の未コミット素材一式へ保持し、人物コミットには混ぜない。[対象meta記録](Results/HumanIconsApplied/cat-meta-changed.json)。監査は2772対象すべて設定一致。ガード自身は設定を書き換えない。

新規検証入口はEditor/HumanIconsApplyVerification、Player検証はRuntime/StandaloneHumanIconsSmoke。通常起動では専用flagがないため作動しない。インストーラーに両方を登録。採用後の再ビルドは通常の既定入口で行える。

既存の猫制作日程・リンの画像/会話/コード差分・加入設定・他人物台帳行は保持。名簿の文字一覧・猫中心ADV・猫画像参照・会話本文・Extra解放条件・セーブ形式を変更していない。

## 容量

| 対象 | 開始 bytes | 終了 bytes | 増減 bytes |
| --- | ---: | ---: | ---: |
| Latest | 3011167836 | 3027962840 | 16795004 |
| Logs | 15059304696 | 15083123172 | 23818476 |
| Library | 5229555342 | 5294582507 | 65027165 |

Latestは156ファイルの成功ビルド一組のみ。子フォルダはLatest/Runsで、Pending/別Build/ZIP/追加Unity/Libraryなし。画像原本3枚の追加は合計3,845,616 bytes。旧詳細版と旧人物パックは保持。Latest増加約16.8MBは人物textureの追加収録とPlayerコード等。Logs増加約23.8MBにはLatest増加を含み、それ以外は人物専用パック・画像/ログ・監査記録。Library増加約65.0MBは既存プロジェクトの通常の再取り込み/ビルドキャッシュであり、複製は作っていない。
