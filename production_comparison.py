"""Read-only comparison of selected production Unity content."""
import json
import re
from collections import Counter
from pathlib import Path

from interaction_presentation import build_interaction_catalog, condition_label
from interaction_subjects import SubjectRegistry, identifier
from interaction_texts import CONDITION_FIELDS, validate_condition
from named_dispatch import LEGACY_CUSTOMERS, SCENES, export_settings, validate_settings
from optional_events import OptionalEventStore, normalize_event, read_pack
from unity_export import build_cat_catalog

BUNDLE_RESOURCE = r'CatCafeContent/Imports/[a-f0-9]{32}/bundle'
EVENT_RESOURCE = r'CatCafeEvents/Imports/[a-f0-9]{32}/optional_events'
INTERACTION_RESOURCE = r'CatCafeContent/Imports/[a-f0-9]{32}/interaction_texts'


def read_json(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def resource_path(root, value, pattern):
    if not isinstance(value, str) or not re.fullmatch(pattern, value):
        raise ValueError('Unityの素材参照先が不正です。')
    path = (root / (value + '.json')).resolve()
    if not path.is_relative_to(root.resolve()):
        raise ValueError('Unityの素材参照先がResourcesの外にあります。')
    return path


def read_baseline(path):
    path = Path(path)
    interactions_path = None
    if path.name == 'catalog.json' and path.parent.name == 'CatCafeContent':
        resources = path.parent.parent
        pointer = read_json(path)
        if not isinstance(pointer, dict):
            raise ValueError('Unityの素材参照が不正です。')
        bundle_path = resource_path(resources, pointer.get('bundleResource'), BUNDLE_RESOURCE)
        interaction_resource = pointer.get('interactionResource')
        if interaction_resource:
            interactions_path = resource_path(resources, interaction_resource, INTERACTION_RESOURCE)
            if interaction_resource != pointer['bundleResource'].removesuffix('/bundle') + '/interaction_texts':
                raise ValueError('比較元の交流文と素材パックの参照先が一致しません。')
        event_pointer = resources / 'CatCafeEvents' / 'catalog.json'
        if event_pointer.exists():
            pointer = read_json(event_pointer)
            if not isinstance(pointer, dict):
                raise ValueError('Unityの任意イベント参照が不正です。')
            events_path = resource_path(resources, pointer.get('eventResource'), EVENT_RESOURCE)
        else:
            events_path = None
    elif path.name == 'bundle.json':
        bundle_path, events_path = path, None
        sibling = path.with_name('interaction_texts.json')
        if sibling.exists():
            interactions_path = sibling
    else:
        raise ValueError('CatCafeContentのcatalog.json、または書き出し済みのbundle.jsonを選んでください。')

    bundle = read_json(bundle_path)
    if not isinstance(bundle, dict) or bundle.get('version') != 1:
        raise ValueError('比較元の素材パックの形式が不正です。')
    cats = index_rows(bundle.get('cats'), 'cat_id')
    for cat in cats.values():
        index_rows(cat.get('scenes'), 'key')
    if events_path is None and bundle.get('optional_events'):
        if bundle['optional_events'] != 'optional_events.json':
            raise ValueError('比較元の任意イベント参照が不正です。')
        events_path = bundle_path.with_name('optional_events.json')
    events = read_pack(events_path.read_text(encoding='utf-8-sig'))['events'] if events_path else []
    interactions = read_json(interactions_path) if interactions_path else None
    if interactions_path and (not isinstance(interactions, dict) or interactions.get('version') not in (1, 2, 3, 4)
                              or interactions.get('pack_id') != bundle.get('pack_id')):
        raise ValueError('比較元の交流文カタログの形式またはパックIDが不正です。')
    return cats, events, bundle.get('named_dispatch') or [], interactions


def index_rows(rows, key):
    if not isinstance(rows, list):
        raise ValueError('比較元の素材一覧が不正です。')
    result = {}
    for row in rows:
        if (not isinstance(row, dict) or not isinstance(row.get(key), str)
                or not row[key] or row[key] in result):
            raise ValueError('比較元に空欄または重複した素材IDがあります。')
        result[row[key]] = row
    return result


def named_dispatch_rows(rows):
    by_customer = index_rows(rows, 'customer_id')
    for row in by_customer.values():
        scenes = index_rows(row.get('scenes'), 'id')
        validate_settings(dict(enabled=row.get('enabled'), scenes={key: scene.get('lines') for key, scene in scenes.items()}))
    return by_customer


def compare_named_dispatch(before, after, field):
    old_rows, new_rows = named_dispatch_rows(before), named_dispatch_rows(after)
    for customer in sorted(old_rows.keys() | new_rows.keys()):
        old, new = old_rows.get(customer, {}), new_rows.get(customer, {})
        item = f"指名派遣 / {(new or old).get('name', customer)}（{customer}）"
        options = [row.get('enabled', customer in LEGACY_CUSTOMERS) for row in (old, new)]
        field(item, '有効・無効', *('有効' if enabled else '無効' for enabled in options))
        old_scenes, new_scenes = ({scene['id']: scene['lines'] for scene in row.get('scenes', [])} for row in (old, new))
        for key, (label, count) in SCENES.items():
            old_lines = old_scenes.get(key, [''] * count)
            new_lines = new_scenes.get(key, [''] * count)
            for slot, (prior, candidate) in enumerate(zip(old_lines, new_lines), 1):
                field(item, f'{label} / 上書き{slot}行目', prior if prior.strip() else '', candidate if candidate.strip() else '')


def interaction_conditions(catalog):
    if catalog is None:
        return {}
    legacy = catalog.get('version') == 1
    cats = index_rows(catalog.get('cat_bindings'), 'cat_id' if legacy else 'subject_id')
    people = index_rows(catalog.get('people'), 'id')
    if (any(not identifier(row.get('cat_id')) for row in cats.values())
            or not legacy and any(not identifier(row.get('game_id')) for row in people.values())):
        raise ValueError('比較元の交流文に不正なゲームIDがあります。')
    labels = {
        'speech': {(key if legacy else row.get('game_id')): row.get('name', key) for key, row in people.items()},
        'cat_response': {row['cat_id']: row.get('name') or row['cat_id'] for row in cats.values()},
    }
    entries = catalog.get('entries')
    if not isinstance(entries, list):
        raise ValueError('比較元の交流文一覧が不正です。')
    result = {}
    for entry in entries:
        if not isinstance(entry, dict):
            raise ValueError('交流文の条件が不正です。')
        condition = {key: entry.get(key) for key in CONDITION_FIELDS}
        if condition['kind'] == 'speech':
            condition['target_cat'] = entry.get('target_cat') or 'cat-mike'
        condition['narrative_scene'] = entry.get('narrative_scene') or 'normal'
        validate_condition(condition, structural=True)
        subject = condition['subject_id']
        if condition['kind'] == 'speech' and subject in people:
            condition['subject_id'] = subject if legacy else people[subject].get('game_id')
            target = condition['target_cat']
            if target != '*' and target in cats:
                condition['target_cat'] = cats[target]['cat_id']
            elif target != '*':
                raise ValueError('交流文の対象猫が比較元の対応表にありません。')
        elif condition['kind'] == 'cat_response' and subject in cats:
            condition['subject_id'] = cats[subject]['cat_id']
        else:
            raise ValueError('交流文の制作対象が比較元の対応表にありません。')
        validate_condition(condition, structural=True)
        variants = entry.get('variants')
        if not isinstance(variants, list) or not variants:
            raise ValueError('交流文の候補が不正です。')
        lines = {}
        for variant in variants:
            slot = variant.get('slot') if isinstance(variant, dict) else None
            if type(slot) is not int or slot not in (1, 2, 3) or slot in lines or not isinstance(variant.get('text'), str):
                raise ValueError('交流文の候補が不正です。')
            lines[slot] = variant['text']
        key = tuple(condition.get(field, '') for field in (*CONDITION_FIELDS, 'target_cat', 'narrative_scene'))
        if key in result:
            raise ValueError('比較元の交流文の条件が重複しています。')
        label = condition_label(condition, labels)
        if 'target_cat' in condition:
            target = condition['target_cat']
            label += ' / 対象猫：' + ('共通' if target == '*' else labels['cat_response'][target] + f'（{target}）')
        result[key] = (f"交流文 / {label}（{condition['subject_id']}）", lines)
    return result


def compare_interactions(before, after, add, field):
    old_rows, new_rows = interaction_conditions(before), interaction_conditions(after)
    for key in sorted(old_rows.keys() | new_rows.keys()):
        prior, candidate = old_rows.get(key), new_rows.get(key)
        if prior is None or candidate is None:
            item = (candidate or prior)[0]
            add('追加' if prior is None else '欠落', item, '条件',
                prior[1] if prior else None, candidate[1] if candidate else None)
            continue
        for slot in sorted(prior[1].keys() | candidate[1].keys()):
            field(candidate[0], f'候補{slot}', prior[1].get(slot), candidate[1].get(slot))
    return len(old_rows), len(new_rows)


def compare_production(path, catalog, registry, texts, *, profiles=None, optional_events=None, interaction_texts=None):
    old_cats, old_events, old_dispatch, old_interactions = read_baseline(path)
    cats, _, _, defaults = build_cat_catalog(catalog, registry, texts, profiles=profiles, retain_images=False)
    store = optional_events or OptionalEventStore(registry.directory / 'optional_events.json')
    new_events = store.export_data(catalog)['events']
    new_cats = index_rows(cats, 'cat_id')
    changes = []

    def add(kind, item, field, old=None, new=None):
        changes.append(dict(kind=kind, item=item, field=field, old=old, new=new))

    def field(item, label, old, new, *, fallback=False):
        if old != new:
            kind = '欠落' if old and (fallback or not new) else '追加' if not old and new else '変更'
            add(kind, item, label, old, new)

    for cat_id in sorted(old_cats.keys() | new_cats.keys()):
        before, after = old_cats.get(cat_id), new_cats.get(cat_id)
        item = f"猫 / {(after or before).get('name', cat_id)}（{cat_id}）"
        if before is None or after is None:
            add('追加' if before is None else '欠落', item, '猫')
            continue
        field(item, '表示名', before.get('name'), after.get('name'))
        field(item, '紹介文', before.get('introduction'), after.get('introduction'), fallback=(cat_id, None) in defaults)
        old_scenes = index_rows(before['scenes'], 'key')
        new_scenes = index_rows(after['scenes'], 'key')
        shared = old_scenes.keys() & new_scenes.keys()
        field(item, '場面の順序', [key for key in old_scenes if key in shared],
              [key for key in new_scenes if key in shared])
        for key in sorted(old_scenes.keys() | new_scenes.keys()):
            prior, candidate = old_scenes.get(key), new_scenes.get(key)
            label = f"{item} / {(candidate or prior).get('label', key)}（{key}）"
            if prior is None or candidate is None:
                add('追加' if prior is None else '欠落', label, '場面')
                continue
            field(label, '表示名', prior.get('label'), candidate.get('label'))
            field(label, '説明文', prior.get('text'), candidate.get('text'), fallback=(cat_id, key) in defaults)
            field(label, '画像参照', prior.get('image') or '', candidate.get('image') or '')

    baseline_events = index_rows([normalize_event(e) for e in old_events], 'event_id')
    candidate_events = index_rows([normalize_event(e) for e in new_events], 'event_id')
    for event_id in sorted(baseline_events.keys() | candidate_events.keys()):
        before, after = baseline_events.get(event_id), candidate_events.get(event_id)
        item = f"任意イベント / {(after or before).get('title', event_id)}（{event_id}）"
        if before is None or after is None:
            add('追加' if before is None else '欠落', item, 'イベント')
        elif before != after:
            keys = sorted(key for key in before.keys() | after.keys()
                          if key != 'revision' and before.get(key) != after.get(key))
            if keys:
                add('変更', item, 'イベント定義',
                    {key: before.get(key) for key in keys}, {key: after.get(key) for key in keys})
    dispatch = export_settings(SubjectRegistry(registry.directory / 'interaction_subjects.json'))
    compare_named_dispatch(old_dispatch, dispatch, field)
    interactions, _, _ = build_interaction_catalog(catalog, registry, store=interaction_texts, text_only=True)
    interaction_counts = compare_interactions(old_interactions, interactions, add, field)
    return {
        'changes': changes,
        'interactions': interaction_counts,
        'counts': Counter(row['kind'] for row in changes),
        'cats': (len(old_cats), len(new_cats)),
        'events': (len(old_events), len(new_events)),
    }


def format_comparison(report):
    def display(value):
        if value in (None, ''):
            return '（なし）'
        return value if isinstance(value, str) else json.dumps(value, ensure_ascii=False)

    old_cats, new_cats = report['cats']
    old_events, new_events = report['events']
    counts = ' / '.join(f"{kind} {report['counts'].get(kind, 0)}件" for kind in ('追加', '変更', '欠落'))
    old_interactions, new_interactions = report['interactions']
    lines = [f'猫 {old_cats} → {new_cats}匹 / 任意イベント {old_events} → {new_events}件'
             f' / 採用済み交流文 {old_interactions} → {new_interactions}条件', counts, '']
    if not report['changes']:
        lines.append('照合対象に差分はありません。')
    for row in report['changes']:
        lines.append(f"【{row['kind']}】{row['item']} / {row['field']}")
        if row['old'] is not None or row['new'] is not None:
            lines.extend([f"  本番：{display(row['old'])}", f"  書き出し候補：{display(row['new'])}"])
    return '\n'.join(lines)
