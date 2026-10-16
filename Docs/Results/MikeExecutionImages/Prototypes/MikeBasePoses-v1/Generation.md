# ミケ：切り替え時の基本ポーズ

2026-10-08。組み込み画像生成ツールで制作。基準画像：`../CatAppearance-v1/mike-normal.png`。8動作と待機の計9枚。全て背景込みPNG。

各動作の「動作の共通画像」。特殊反応を加えず、動作開始時の注意・準備を描く。通常の場面キーを使用し、`__switch` が未登録の場合も共通画像に戻る既存の仕組みで表示する。

2026-10-08、基本9枚と特殊反応16枚を制作ツールの `playtest-mike` と `cat-mike` に登録。Unityの既存5匹のパックを複製し、ミケの25場面を反映した新しいパックへ catalog を切り替えた。他の猫とミケの通常・遊ぶ・眠るの画像は保持。画像ハッシュ・ファイル存在・他の猫の設定保持を確認。実行中のUnity画面での表示確認は未実施。反映記録は `Registration.json`。

目視で顔の模様の左右、目の色、四肢、尻尾、場面の道具を確認。細かな斑や背景の位置には生成による差がある。

一覧：`index.html`。既存の特殊反応16枚は `../MikeReactions-v1/index.html`。

## ねこじゃらし

場面キー：`teaser`。ファイル：`mike-teaser-base.png`。

生成プロンプト：

```text
Use case: stylized-concept. Asset type: CatCafeSim SINGLE square background-inclusive BASIC MODE POSE, teaser, displayed immediately when switching to this interaction. Image 1 is CHARACTER IDENTITY, STYLE AND BACKGROUND REFERENCE. Same individual Mike calico small rounded shorthaired adult, same round face pink nose amber eyes. Anatomical RIGHT ear/eye orange (viewer left frontal), LEFT ear/eye charcoal (viewer right frontal), central white blaze muzzle white chest belly four paws, same broad orange charcoal body patches and medium patch tail charcoal tip. Preserve identity faithfully, no mirroring. Pose and props: seated naturally, both front paws on mat, looking with open amber eyes at a beige feather wand presented just ahead from right by one adult hand; ready to play but not reaching yet. This is neutral basic readiness, not a special emotional response: mouth closed, BOTH eyes OPEN, calm gentle natural feline expression, no ecstatic smiling, no affectionate leaning, no surprise. Same cream wall light wooden floor sage rectangular mat, warm diffuse light detailed delicate Japanese hand painted 2D illustration. Fixed eye level viewpoint same cat size about 65 percent canvas height, entire cat ears paws and tail inside generous margins. Natural four legs two ears one tail; never extra paws. Hand only if explicitly required above, single realistic hand. No text labels grids symbols hearts watermark extra cats furniture or extra toys.
```

## ボール遊び

場面キー：`ball`。ファイル：`mike-ball-base.png`。

生成プロンプト：

```text
Use case: stylized-concept. Asset type: CatCafeSim SINGLE square background-inclusive BASIC MODE POSE, ball, displayed immediately when switching to this interaction. Image 1 is CHARACTER IDENTITY, STYLE AND BACKGROUND REFERENCE. Same individual Mike calico small rounded shorthaired adult, same round face pink nose amber eyes. Anatomical RIGHT ear/eye orange (viewer left frontal), LEFT ear/eye charcoal (viewer right frontal), central white blaze muzzle white chest belly four paws, same broad orange charcoal body patches and medium patch tail charcoal tip. Preserve identity faithfully, no mirroring. Pose and props: standing naturally on four paws, calmly looking down at one small sage and cream ball placed in front; ready to play, no paw lifted, no human parts. This is neutral basic readiness, not a special emotional response: mouth closed, BOTH eyes OPEN, calm gentle natural feline expression, no ecstatic smiling, no affectionate leaning, no surprise. Same cream wall light wooden floor sage rectangular mat, warm diffuse light detailed delicate Japanese hand painted 2D illustration. Fixed eye level viewpoint same cat size about 65 percent canvas height, entire cat ears paws and tail inside generous margins. Natural four legs two ears one tail; never extra paws. Hand only if explicitly required above, single realistic hand. No text labels grids symbols hearts watermark extra cats furniture or extra toys.
```

## ぬいぐるみ遊び

場面キー：`plush`。ファイル：`mike-plush-base.png`。

生成プロンプト：

```text
Use case: stylized-concept. Asset type: CatCafeSim SINGLE square background-inclusive BASIC MODE POSE, plush, displayed immediately when switching to this interaction. Image 1 is CHARACTER IDENTITY, STYLE AND BACKGROUND REFERENCE. Same individual Mike calico small rounded shorthaired adult, same round face pink nose amber eyes. Anatomical RIGHT ear/eye orange (viewer left frontal), LEFT ear/eye charcoal (viewer right frontal), central white blaze muzzle white chest belly four paws, same broad orange charcoal body patches and medium patch tail charcoal tip. Preserve identity faithfully, no mirroring. Pose and props: seated naturally both front paws on mat, calmly inspecting a small plain gray plush mouse lying in front; not hugging or biting, no human parts. This is neutral basic readiness, not a special emotional response: mouth closed, BOTH eyes OPEN, calm gentle natural feline expression, no ecstatic smiling, no affectionate leaning, no surprise. Same cream wall light wooden floor sage rectangular mat, warm diffuse light detailed delicate Japanese hand painted 2D illustration. Fixed eye level viewpoint same cat size about 65 percent canvas height, entire cat ears paws and tail inside generous margins. Natural four legs two ears one tail; never extra paws. Hand only if explicitly required above, single realistic hand. No text labels grids symbols hearts watermark extra cats furniture or extra toys.
```

## トンネル遊び

場面キー：`tunnel`。ファイル：`mike-tunnel-base.png`。

生成プロンプト：

```text
Use case: stylized-concept. Asset type: CatCafeSim SINGLE square background-inclusive BASIC MODE POSE, tunnel, displayed immediately when switching to this interaction. Image 1 is CHARACTER IDENTITY, STYLE AND BACKGROUND REFERENCE. Same individual Mike calico small rounded shorthaired adult, same round face pink nose amber eyes. Anatomical RIGHT ear/eye orange (viewer left frontal), LEFT ear/eye charcoal (viewer right frontal), central white blaze muzzle white chest belly four paws, same broad orange charcoal body patches and medium patch tail charcoal tip. Preserve identity faithfully, no mirroring. Pose and props: standing on four paws just OUTSIDE a simple sage fabric cat tunnel entrance, cat fully visible in front with tail beside body, looking curiously toward entrance; tunnel slightly behind cat to right, no human parts. This is neutral basic readiness, not a special emotional response: mouth closed, BOTH eyes OPEN, calm gentle natural feline expression, no ecstatic smiling, no affectionate leaning, no surprise. Same cream wall light wooden floor sage rectangular mat, warm diffuse light detailed delicate Japanese hand painted 2D illustration. Fixed eye level viewpoint same cat size about 65 percent canvas height, entire cat ears paws and tail inside generous margins. Natural four legs two ears one tail; never extra paws. Hand only if explicitly required above, single realistic hand. No text labels grids symbols hearts watermark extra cats furniture or extra toys.
```

## ブラッシング

場面キー：`brush`。ファイル：`mike-brush-base.png`。

生成プロンプト：

```text
Use case: stylized-concept. Asset type: CatCafeSim SINGLE square background-inclusive BASIC MODE POSE, brush, displayed immediately when switching to this interaction. Image 1 is CHARACTER IDENTITY, STYLE AND BACKGROUND REFERENCE. Same individual Mike calico small rounded shorthaired adult, same round face pink nose amber eyes. Anatomical RIGHT ear/eye orange (viewer left frontal), LEFT ear/eye charcoal (viewer right frontal), central white blaze muzzle white chest belly four paws, same broad orange charcoal body patches and medium patch tail charcoal tip. Preserve identity faithfully, no mirroring. Pose and props: seated naturally both front paws on mat, open amber eyes calmly noticing a soft bristle grooming brush offered beside shoulder by one adult hand from right; brush not contacting yet. This is neutral basic readiness, not a special emotional response: mouth closed, BOTH eyes OPEN, calm gentle natural feline expression, no ecstatic smiling, no affectionate leaning, no surprise. Same cream wall light wooden floor sage rectangular mat, warm diffuse light detailed delicate Japanese hand painted 2D illustration. Fixed eye level viewpoint same cat size about 65 percent canvas height, entire cat ears paws and tail inside generous margins. Natural four legs two ears one tail; never extra paws. Hand only if explicitly required above, single realistic hand. No text labels grids symbols hearts watermark extra cats furniture or extra toys.
```

## 声をかける

場面キー：`voice`。ファイル：`mike-voice-base.png`。

生成プロンプト：

```text
Use case: stylized-concept. Asset type: CatCafeSim SINGLE square background-inclusive BASIC MODE POSE, voice, displayed immediately when switching to this interaction. Image 1 is CHARACTER IDENTITY, STYLE AND BACKGROUND REFERENCE. Same individual Mike calico small rounded shorthaired adult, same round face pink nose amber eyes. Anatomical RIGHT ear/eye orange (viewer left frontal), LEFT ear/eye charcoal (viewer right frontal), central white blaze muzzle white chest belly four paws, same broad orange charcoal body patches and medium patch tail charcoal tip. Preserve identity faithfully, no mirroring. Pose and props: seated upright both front paws on mat, head slightly raised and turned toward offscreen person to right, ears attentive open amber eyes; no visible humans hands arms props. This is neutral basic readiness, not a special emotional response: mouth closed, BOTH eyes OPEN, calm gentle natural feline expression, no ecstatic smiling, no affectionate leaning, no surprise. Same cream wall light wooden floor sage rectangular mat, warm diffuse light detailed delicate Japanese hand painted 2D illustration. Fixed eye level viewpoint same cat size about 65 percent canvas height, entire cat ears paws and tail inside generous margins. Natural four legs two ears one tail; never extra paws. Hand only if explicitly required above, single realistic hand. No text labels grids symbols hearts watermark extra cats furniture or extra toys.
```

## そばで見守る

場面キー：`presence`。ファイル：`mike-presence-base.png`。

生成プロンプト：

```text
Use case: stylized-concept. Asset type: CatCafeSim SINGLE square background-inclusive BASIC MODE POSE, presence, displayed immediately when switching to this interaction. Image 1 is CHARACTER IDENTITY, STYLE AND BACKGROUND REFERENCE. Same individual Mike calico small rounded shorthaired adult, same round face pink nose amber eyes. Anatomical RIGHT ear/eye orange (viewer left frontal), LEFT ear/eye charcoal (viewer right frontal), central white blaze muzzle white chest belly four paws, same broad orange charcoal body patches and medium patch tail charcoal tip. Preserve identity faithfully, no mirroring. Pose and props: lying comfortably in a natural sphinx pose with forepaws resting in front on mat, head upright, calm open amber eyes looking slightly forward; no humans hands arms or props. This is neutral basic readiness, not a special emotional response: mouth closed, BOTH eyes OPEN, calm gentle natural feline expression, no ecstatic smiling, no affectionate leaning, no surprise. Same cream wall light wooden floor sage rectangular mat, warm diffuse light detailed delicate Japanese hand painted 2D illustration. Fixed eye level viewpoint same cat size about 65 percent canvas height, entire cat ears paws and tail inside generous margins. Natural four legs two ears one tail; never extra paws. Hand only if explicitly required above, single realistic hand. No text labels grids symbols hearts watermark extra cats furniture or extra toys.
```

## なでる

場面キー：`pet`。ファイル：`mike-pet-base.png`。

生成プロンプト：

```text
Use case: stylized-concept. Asset type: CatCafeSim SINGLE square background-inclusive BASIC MODE POSE, pet, displayed immediately when switching to this interaction. Image 1 is CHARACTER IDENTITY, STYLE AND BACKGROUND REFERENCE. Same individual Mike calico small rounded shorthaired adult, same round face pink nose amber eyes. Anatomical RIGHT ear/eye orange (viewer left frontal), LEFT ear/eye charcoal (viewer right frontal), central white blaze muzzle white chest belly four paws, same broad orange charcoal body patches and medium patch tail charcoal tip. Preserve identity faithfully, no mirroring. Pose and props: seated naturally both front paws on mat, open amber eyes neutral gentle face; one adult hand from right hovering just above crown between ears about to stroke, not touching yet. This is neutral basic readiness, not a special emotional response: mouth closed, BOTH eyes OPEN, calm gentle natural feline expression, no ecstatic smiling, no affectionate leaning, no surprise. Same cream wall light wooden floor sage rectangular mat, warm diffuse light detailed delicate Japanese hand painted 2D illustration. Fixed eye level viewpoint same cat size about 65 percent canvas height, entire cat ears paws and tail inside generous margins. Natural four legs two ears one tail; never extra paws. Hand only if explicitly required above, single realistic hand. No text labels grids symbols hearts watermark extra cats furniture or extra toys.
```

## 動きを止めて待つ

場面キー：`pause`。ファイル：`mike-pause-base.png`。

生成プロンプト：

```text
Use case: stylized-concept. Asset type: CatCafeSim SINGLE square background-inclusive BASIC MODE POSE, pause, displayed immediately when switching to this interaction. Image 1 is CHARACTER IDENTITY, STYLE AND BACKGROUND REFERENCE. Same individual Mike calico small rounded shorthaired adult, same round face pink nose amber eyes. Anatomical RIGHT ear/eye orange (viewer left frontal), LEFT ear/eye charcoal (viewer right frontal), central white blaze muzzle white chest belly four paws, same broad orange charcoal body patches and medium patch tail charcoal tip. Preserve identity faithfully, no mirroring. Pose and props: seated naturally both front paws on mat, calmly watching a beige feather wand resting completely still on mat in front; no human hands or arms. This is neutral basic readiness, not a special emotional response: mouth closed, BOTH eyes OPEN, calm gentle natural feline expression, no ecstatic smiling, no affectionate leaning, no surprise. Same cream wall light wooden floor sage rectangular mat, warm diffuse light detailed delicate Japanese hand painted 2D illustration. Fixed eye level viewpoint same cat size about 65 percent canvas height, entire cat ears paws and tail inside generous margins. Natural four legs two ears one tail; never extra paws. Hand only if explicitly required above, single realistic hand. No text labels grids symbols hearts watermark extra cats furniture or extra toys.
```
