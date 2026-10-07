"""交流画像の場面。既存の4場面は互換性のため維持する。"""
INTERACTION_SCENES = (('teaser', 'ねこじゃらし'), ('ball', 'ボール遊び'), ('plush', 'ぬいぐるみ遊び'), ('tunnel', 'トンネル遊び'), ('brush', 'ブラッシング'), ('voice', '声をかける'), ('presence', 'そばで見守る'), ('pause', '動きを止めて待つ'))
ACTION_PROMPTS = {'teaser': 'playing with a feather wand, no human face, no human body', 'ball': 'chasing a small ball, no human face, no human body', 'plush': 'playing with a plush toy, no human face, no human body', 'tunnel': 'playing inside a cat tunnel, no human face, no human body', 'brush': 'being gently brushed by one generic adult hand, no human face, no human body', 'voice': 'looking attentively toward an offscreen person, no human face, no human body', 'presence': 'relaxing beside an offscreen person, no human face, no human body', 'pause': 'watching a motionless toy calmly, no human face, no human body'}

# 実際に選べる組み合わせのみ。switch は切り替え後の動作を描く。
MODE_LABELS = dict(INTERACTION_SCENES[:-1], pet='なでる')
ACTION_LABELS = dict(direct='素直に動かす', adapt='相手に合わせる', intense='激しく動かす',
                     feint='フェイントを入れる', pause='動きを止めて待つ', switch='動作を切り替える', connect='心をつかむ')
MODE_ACTIONS = {mode: ('direct', 'adapt') +
                (('intense',) if mode in ('teaser', 'ball', 'tunnel') else ()) +
                (('feint',) if mode in ('teaser', 'ball', 'plush', 'tunnel') else ()) +
                ('pause', 'switch', 'connect') for mode in MODE_LABELS}
COMBINATION_SCENES = tuple((mode + '__' + action, label + ' × ' + ACTION_LABELS[action])
                           for mode, label in MODE_LABELS.items() for action in MODE_ACTIONS[mode])
MODIFIERS = dict(direct='steady straightforward movement', adapt='gentle movement adjusted to the cat, relaxed attentive cat',
                 intense='vigorous energetic movement, dynamic cat pose', feint='a sudden playful change of direction, alert surprised cat',
                 pause='the person stops moving and waits, cat watches calmly',
                 switch='beginning this new interaction, cat notices the new activity',
                 connect='a warm moment of connection, cat responds enthusiastically')
MODE_PROMPTS = dict(ACTION_PROMPTS, pet='being gently stroked on the head by one generic adult hand')
PAUSE_PROMPTS = dict(teaser='cat watching a motionless feather wand', ball='cat watching a stationary small ball',
                     plush='cat beside a plush toy', tunnel='cat resting at a cat tunnel entrance',
                     pet='cat beside a still adult hand', brush='cat beside a still grooming brush',
                     voice='cat looking toward a quiet offscreen person', presence='cat relaxing near an offscreen person')
ACTION_PROMPTS.update({mode + '__' + action: (PAUSE_PROMPTS[mode] if action == 'pause' else MODE_PROMPTS[mode]) + ', ' + MODIFIERS[action] + ', no human face, no human body'
                       for mode in MODE_LABELS for action in MODE_ACTIONS[mode]})
