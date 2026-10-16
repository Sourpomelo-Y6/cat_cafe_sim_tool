"""Register approved seven-cat preparation/execution pairs in the existing pack."""
import argparse
import copy
import hashlib
import json
import re
from pathlib import Path
from datetime import datetime

from content_images import ImageRegistry
from interaction_presentation import ACTION_TEXTS, read_png

PROTOTYPES = Path(__file__).resolve().parent / 'Docs/Results/CatExecutionImages/Prototypes'
MODES = ('teaser', 'ball', 'plush', 'tunnel', 'pet', 'brush', 'voice', 'presence')


def write_json(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    temp = path.with_suffix(path.suffix + '.tmp')
    newline = '\r\n' if path.exists() and b'\r\n' in path.read_bytes()[:4096] else '\n'
    text = json.dumps(data, ensure_ascii=False, indent=2) + '\n'
    temp.write_bytes(text.replace('\n', newline).encode('utf-8'))
    temp.replace(path)


def actions(mode):
    if mode == 'pause':
        return ('pause',)
    return ('any', 'direct', 'adapt') + (('intense',) if mode in ('teaser', 'ball', 'tunnel') else ()) + (('feint',) if mode in ('teaser', 'ball', 'plush', 'tunnel') else ())


def register(root, project, content_pack_id=None):
    inventory_root = PROTOTYPES / 'ExecutionImages-v1'
    rows = json.loads((inventory_root / 'Inventory.json').read_text(encoding='utf-8'))
    if len(rows) != 56 or len({(r['game_cat_id'], r['action']) for r in rows}) != 56:
        raise ValueError('Expected seven cats with eight distinct modes')
    registry = ImageRegistry(root / 'cat_cafe_sim/saves/content_images')
    before = registry.read()
    resources = project / 'Assets/Resources'
    catalog = json.loads((resources / 'CatCafeContent/catalog.json').read_text(encoding='utf-8-sig'))
    if content_pack_id is not None:
        if not re.fullmatch('[a-f0-9]{32}', content_pack_id):
            raise ValueError('Expected an existing content pack ID')
        base = 'CatCafeContent/Imports/' + content_pack_id + '/'
        catalog = dict(bundleResource=base + 'bundle', interactionResource=base + 'interaction_texts')
    bundle_path = resources / (catalog['bundleResource'] + '.json')
    texts_path = resources / (catalog['interactionResource'] + '.json')
    if bundle_path.parent != texts_path.parent:
        raise ValueError('Expected a shared existing content pack')
    bundle = json.loads(bundle_path.read_text(encoding='utf-8'))
    texts = json.loads(texts_path.read_text(encoding='utf-8'))
    old_bundle, old_texts = copy.deepcopy(bundle), copy.deepcopy(texts)
    if texts['version'] not in (2, 3):
        raise ValueError('Expected an existing v2 or v3 narrative catalog')
    # Older standard packs only described teaser/pet/pause. Extend their schema
    # using existing common action prose, retaining every authored text pool.
    texts['version'] = 3
    old_actions = {(a['mode'], a['action']) for a in texts['action_texts']}
    texts['action_texts'].extend(copy.deepcopy(a) for a in ACTION_TEXTS if (a['mode'], a['action']) not in old_actions)
    updated = copy.deepcopy(before)
    blobs, plan = {}, {}
    existing = {}
    for relative in sorted({p for scenes in before['cats'].values() for p in scenes.values()}):
        path = registry._resolve(relative)
        if path.is_file():
            existing.setdefault(hashlib.sha256(path.read_bytes()).hexdigest(), relative)
    for row in rows:
        game, display, mode = row['game_cat_id'], row['display_cat_id'], row['action']
        if mode not in MODES:
            raise ValueError('Unexpected mode')
        cat = next(c for c in bundle['cats'] if c['cat_id'] == display)
        binding = next((c for c in texts['cat_bindings'] if c['cat_id'] == game), None)
        if binding is None:
            binding = dict(cat_id=game, content_cat_id=display, subject_id=game, name=row['name'])
            texts['cat_bindings'].append(binding)
        if binding['content_cat_id'] != display:
            raise ValueError('Existing cat/content ID mapping differs')
        planned = plan.setdefault(game, dict(content_cat_id=display, scenes={}))['scenes']
        for scene, relative in row['keys'].items():
            source = (inventory_root / relative).resolve()
            if not source.is_relative_to(PROTOTYPES.resolve()):
                raise ValueError('Source escaped approved assets')
            data = read_png(source)
            digest = hashlib.sha256(data).hexdigest()
            image = 'images/' + digest + '.png'
            blobs[image] = data
            planned[scene] = dict(image=image, source=source.relative_to(PROTOTYPES).as_posix())
            for cat_id in {game, display}:
                updated['cats'].setdefault(cat_id, {})[scene] = existing.get(digest, image)
            item = next((s for s in cat['scenes'] if s['key'] == scene), None)
            if item is None:
                item = dict(key=scene)
                cat['scenes'].append(item)
            # Preserve existing authored scene text and labels.
            item['image'] = image
            if 'label' not in item:
                item['label'] = scene
                item['text'] = row['name'] + ('が交流を始める前の様子です。' if scene.endswith('__switch') else 'が通常の交流に応じています。')
    # Image bindings may exist without authored cat-specific prose: the runtime's
    # existing generic prose remains the fallback; do not fabricate text pools.
    for binding in texts['cat_bindings']:
        game, display = binding['cat_id'], binding['content_cat_id']
        cat_plan = plan.get(game, dict(scenes={}))
        cat = next(c for c in bundle['cats'] if c['cat_id'] == display)
        scenes = {s['key']: s for s in cat['scenes']}
        for mode in MODES + ('pause',):
            for action in actions(mode):
                key = mode if action in ('any', 'pause') else mode + '__' + action
                item = next((i for i in texts['image_bindings'] if i['cat_id'] == game and i['scene_key'] == key), None)
                if item is not None and key not in cat_plan['scenes']:
                    continue
                resolved = next((s for s in (key, mode, 'play' if mode == 'teaser' else 'normal', 'normal') if s in scenes and scenes[s].get('image')), '')
                image = scenes[resolved]['image'] if resolved else ''
                if item is None:
                    item = dict(cat_id=game, content_cat_id=display, scene_key=key)
                    texts['image_bindings'].append(item)
                item.update(image=image, resolved_scene_key=resolved)
    payload = {k: v for k, v in texts.items() if k not in ('pack_id', 'revision')}
    texts['revision'] = hashlib.sha256(json.dumps(payload, ensure_ascii=False, sort_keys=True, separators=(',', ':')).encode()).hexdigest()
    assert texts['entries'] == old_texts['entries']
    for cat_id, scenes in before['cats'].items():
        changed = next((v['scenes'] for game, v in plan.items() if cat_id in (game, v['content_cat_id'])), {})
        for key, image in scenes.items():
            if key not in changed:
                assert updated['cats'][cat_id][key] == image
    affected = {v['content_cat_id'] for v in plan.values()}
    assert [c for c in bundle['cats'] if c['cat_id'] not in affected] == [c for c in old_bundle['cats'] if c['cat_id'] not in affected]
    for old in old_bundle['cats']:
        if old['cat_id'] not in affected:
            continue
        changed_keys = next(p['scenes'] for p in plan.values() if p['content_cat_id'] == old['cat_id'])
        new = next(c for c in bundle['cats'] if c['cat_id'] == old['cat_id'])
        for scene in old['scenes']:
            if scene['key'] not in changed_keys:
                assert scene == next(s for s in new['scenes'] if s['key'] == scene['key'])
    backup = root / 'Logs/CatExecutionImages' / datetime.now().strftime('%Y%m%d-%H%M%S-%f')
    for name, data in [('registry-before.json', before), ('bundle-before.json', old_bundle), ('interaction-texts-before.json', old_texts)]:
        write_json(backup / name, data)
    added = []
    for image, data in blobs.items():
        destinations = [bundle_path.parent / image]
        if Path(image).stem not in existing:
            destinations.append(registry.directory / image)
        for path in destinations:
            path.parent.mkdir(parents=True, exist_ok=True)
            if not path.exists():
                path.write_bytes(data)
                added.append(dict(path=str(path), bytes=len(data)))
            elif path.read_bytes() != data:
                raise ValueError('Existing content hash does not match bytes')
    write_json(registry.path, updated)
    write_json(bundle_path, bundle)
    write_json(texts_path, texts)
    for game, cat_plan in plan.items():
        for key, row in cat_plan['scenes'].items():
            for cat_id in {game, cat_plan['content_cat_id']}:
                assert registry._resolve(updated['cats'][cat_id][key]).read_bytes() == blobs[row['image']]
            assert (bundle_path.parent / row['image']).read_bytes() == blobs[row['image']]
    report = dict(cats=plan, registered_keys=sum(len(v['scenes']) for v in plan.values()), active_pack=str(bundle_path.parent), backup=str(backup), added_files=added, revision=texts['revision'], authored_entries_unchanged=True, unrelated_scenes_unchanged=True)
    name = 'Registration.json' if content_pack_id is None else 'Registration-' + content_pack_id + '.json'
    write_json(Path(__file__).resolve().parent / 'Docs/Results/CatExecutionImages' / name, report)
    return report


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--project-path', type=Path, required=True)
    parser.add_argument('--content-pack-id', help='Update an existing pack without changing the active catalog')
    args = parser.parse_args()
    result = register(Path(__file__).resolve().parent.parent, args.project_path.resolve(), args.content_pack_id)
    print(json.dumps({k: result[k] for k in ('registered_keys', 'active_pack', 'backup', 'revision')}, indent=2))
