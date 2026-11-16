"""Read-only comparison of the cat and optional-event parts of a Unity export."""
import json
import re
from collections import Counter
from pathlib import Path

from optional_events import OptionalEventStore, normalize_event, read_pack
from unity_export import build_cat_catalog

BUNDLE_RESOURCE = r'CatCafeContent/Imports/[a-f0-9]{32}/bundle'
EVENT_RESOURCE = r'CatCafeEvents/Imports/[a-f0-9]{32}/optional_events'


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
    if path.name == 'catalog.json' and path.parent.name == 'CatCafeContent':
        resources = path.parent.parent
        pointer = read_json(path)
        if not isinstance(pointer, dict):
            raise ValueError('Unityの素材参照が不正です。')
        bundle_path = resource_path(resources, pointer.get('bundleResource'), BUNDLE_RESOURCE)
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
    return cats, events


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


def compare_production(path, catalog, registry, texts, *, profiles=None, optional_events=None):
    old_cats, old_events = read_baseline(path)
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
    return {
        'changes': changes,
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
    lines = [f'猫 {old_cats} → {new_cats}匹 / 任意イベント {old_events} → {new_events}件', counts, '']
    if not report['changes']:
        lines.append('照合対象に差分はありません。')
    for row in report['changes']:
        lines.append(f"【{row['kind']}】{row['item']} / {row['field']}")
        if row['old'] is not None or row['new'] is not None:
            lines.extend([f"  本番：{display(row['old'])}", f"  書き出し候補：{display(row['new'])}"])
    return '\n'.join(lines)
