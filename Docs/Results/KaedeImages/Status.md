# カエデ画像制作・検証記録

2026-11-04完了。カエデ（catalog-11）はキジトラ・長毛、遊び好きで繊細な猫。外見6項目に琥珀色の目、濃い縞、丸みのある顔、房状の尾などを記録。募集費200と特性 outgoing を維持。

[画像一覧](index.html) / [外見](profile.json) / [場面対応](manifest.json) / [プロンプトと参照元](prompts.json) / [検証結果](Verification.json)

内蔵image_genで採用27枚。通常・睡眠・待機・8行動・特殊16反応を52場面と32交流画像枠に割り当て、準備・切替は通常画像を共用。全画像で模様、顔立ち、毛長、行動と小道具を目視確認。猫じゃらし初稿は足の形が曖昧だったため差し替え、[初稿記録](rejected-candidates.json)を保存。

制作カタログ、Python画像manifest、実Unityの有効パックへ登録。既存23匹と交流文435件を照合。募集4回目に実加入する rescue-11 と素材 catalog-11 の対応を確認。Python既存28件とUnity交流40ケースが成功。加入、通常・睡眠、8行動の準備と実行、保存再開を検証し、通常・睡眠・ボール・なでるの4画面を目視確認。ログは Logs/CatDailyImages/20261104 に保存。

採用PNG合計 62,798,835 bytes。制作資料、PythonとUnityの画像ストアにそれぞれ同量を保存。Standalone向け画像設定1024・BC7を確認。容量は作業前後の記録を参照。Latest 5,753,606,210 bytes、Runs 286,796,761 bytes、generated 3,277,372,585 bytes。StandaloneBuild直下は Latest, Runs。今回の検証はUnity Editorで実施。

追加16/23匹完了。次は11月5日20時のフク。全完了予定は11月11日。
