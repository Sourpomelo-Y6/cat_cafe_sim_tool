# フク画像制作・検証記録

2026-11-05完了。フク（catalog-12）は白黒・短毛、活発な探検家。募集費200、特性 hardy。外見6項目を制作設定として記録し、翡翠色の目、白い逆V字の鼻筋、背の黒い鞍状斑、白い尾先、右口元の小さい黒点を各画像の識別に使用。

[画像一覧](index.html) / [外見](profile.json) / [場面対応](manifest.json) / [プロンプトと参照元](prompts.json) / [検証結果](Verification.json)

内蔵image_genで27枚を制作。通常・睡眠・待機・8行動・特殊16反応を52場面と32交流画像枠に割り当て。準備と切替は通常画像を共用。基準画像を各生成に参照し、全27枚の模様、顔、短毛、四肢、行動、小道具を目視確認。採用PNGと生成原本を保存。

制作カタログ、Python画像manifest、実Unity有効パックへ登録。既存24匹と交流文435件、既存metadataを保全照合。募集4回目の rescue-12 と素材 catalog-12 が一致。Python既存テスト28件成功。Unity Editorの交流40ケース成功、実加入後の通常・睡眠・交流・保存再開を確認。通常・睡眠・ボール・なでるの4画面を目視確認。検証ログと画面は Logs/CatDailyImages/20261105 に保存。

採用PNG合計 64,887,297 bytes。制作資料とPython・Unityの画像ストアに同量を保存。Standalone向け画像設定1024・BC7を確認。作業前後のLatestは 5,810,353,034 bytes、Runsは 292,865,663 bytes、generatedは 3,277,372,585 bytes。作業前後の容量をVerification.jsonへ記録。StandaloneBuild直下は Latest, Runs。Unity Editorで検証し、exeへの今回分反映は次回ビルド時となる。

追加17/23匹完了、残り6匹。次は11月6日20時のナギ。全完了予定は11月11日。
