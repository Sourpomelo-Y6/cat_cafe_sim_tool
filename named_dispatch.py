"""Customer-specific named dispatch settings carried by the Unity content bundle."""

SCENES = {
    'affinity': ('親しみイベント', 2),
    'request': ('最初の依頼', 2),
    'request_high': ('最初の依頼・不満が高い', 2),
    'reminder': ('催促', 2),
    'reminder_high': ('催促・不満が高い', 2),
    'expired_notice': ('連れ去りの知らせ', 2),
    'refused_notice': ('返還拒否の知らせ', 2),
    'expired_waiting': ('連れ去り後・帰還待ち', 1),
    'refused_waiting': ('返還拒否後・帰還待ち', 1),
    'expired_confirmation': ('連れ去り後・迎え', 2),
    'refused_confirmation': ('返還拒否後・迎え', 2),
    'expired_result': ('連れ去り後・帰還結果', 2),
    'refused_result': ('返還拒否後・帰還結果', 2),
    'normal_return': ('通常の派遣帰還', 1),
    'adoption': ('譲渡の申し出（お客さんの台詞）', 1),
    'epilogue': ('譲渡後の便り', 3),
}
LEGACY_CUSTOMERS = {'guest-1', 'guest-2'}


def validate_settings(settings):
    if (not isinstance(settings, dict) or settings.keys() != {'enabled', 'scenes'}
            or type(settings['enabled']) is not bool or not isinstance(settings['scenes'], dict)):
        raise ValueError('指名派遣の設定が不正です。')
    for scene, lines in settings['scenes'].items():
        if scene not in SCENES or not isinstance(lines, list) or len(lines) != SCENES[scene][1]:
            raise ValueError('指名派遣の場面または行数が不正です。')
        for line in lines:
            if (not isinstance(line, str) or len(line.encode('utf-16-le', errors='surrogatepass')) > 1200
                    or any(c in line.replace('{cat}', '').replace('{customer}', '') for c in '{}')
                    or any(ord(c) < 32 and c not in '\n\t' for c in line)):
                raise ValueError('本文は各行600字までです。差し込みは {cat} と {customer} が使えます。')


def export_settings(registry):
    from interaction_subjects import game_subjects

    customers = game_subjects()['speech']
    result = []
    for row in registry.read()['subjects']:
        settings = row.get('named_dispatch')
        customer = row['game_id']
        if settings is None:
            continue
        if customer not in customers:
            raise ValueError(f'{customer} は指名派遣を設定できるゲーム内のお客さんではありません。')
        result.append(dict(
            customer_id=customer,
            name=customers[customer]['name'],
            enabled=settings['enabled'],
            scenes=[dict(id=scene, lines=lines) for scene, lines in settings['scenes'].items()
                    if any(line.strip() for line in lines)],
        ))
    return sorted(result, key=lambda row: row['customer_id'])
