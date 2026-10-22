"""Register Hana's approved daily production without replacing existing cats."""
import copy
import hashlib
import json
from pathlib import Path
from register_cat_execution_images import actions, write_json, MODES
from interaction_subjects import SubjectRegistry

ROOT = Path(__file__).resolve().parent.parent
SOURCE = Path(__file__).resolve().parent / 'Docs/Results/HanaImages'
PROJECT = Path('C:/Users/hi-wa/unity_workspase/CatCafeSim')

def register():
    profile = json.loads((SOURCE / 'profile.json').read_text(encoding='utf-8'))
    keys = ['normal', 'sleep', 'pause'] + list(MODES) + [m + '__' + r for m in MODES for r in ('open_up', 'simultaneous')]
    assert len(keys) == 27
    selected_files = {r['scene']:r['file'] for r in json.loads((SOURCE/'prompts.json').read_text(encoding='utf-8'))}
    blobs = {}
    scene_images = {}
    for key in keys:
        data = (SOURCE / selected_files[key]).read_bytes()
        assert data.startswith(b'\x89PNG\r\n\x1a\n'), key
        image = 'images/' + hashlib.sha256(data).hexdigest() + '.png'
        blobs[image] = data
        scene_images[key] = image
    # Basic interaction slots are preparation. Direct/adapt use actual action art.
    scenes = dict(normal=scene_images['normal'], sleep=scene_images['sleep'], play=scene_images['teaser'], pause=scene_images['pause'])
    for mode in MODES:
        scenes[mode] = scene_images['normal']
        scenes[mode + '__switch'] = scene_images['normal']
        for operation in ('direct', 'adapt'):
            scenes[mode + '__' + operation] = scene_images[mode]
        for reaction in ('open_up', 'simultaneous'):
            scenes[mode + '__' + reaction] = scene_images[mode + '__' + reaction]
    assert len(scenes) == 52
    registry_path = ROOT / 'cat_cafe_sim/saves/content_images/manifest.json'
    registry = json.loads(registry_path.read_text(encoding='utf-8'))
    old_registry = copy.deepcopy(registry)
    assert 'shop-3' not in registry['cats'] or registry['cats']['shop-3'] == scenes
    resources = PROJECT / 'Assets/Resources'
    catalog = json.loads((resources / 'CatCafeContent/catalog.json').read_text(encoding='utf-8-sig'))
    bundle_path = resources / (catalog['bundleResource'] + '.json')
    texts_path = resources / (catalog['interactionResource'] + '.json')
    bundle = json.loads(bundle_path.read_text(encoding='utf-8'))
    texts = json.loads(texts_path.read_text(encoding='utf-8'))
    assert texts['version'] == 3
    old_cats = copy.deepcopy([c for c in bundle['cats'] if c['cat_id'] != 'shop-3'])
    old_entries = copy.deepcopy(texts['entries'])
    backup = ROOT / 'Logs/CatDailyImages/20261020'
    backup.mkdir(parents=True, exist_ok=True)
    for name, value in [('registry-before.json', registry), ('bundle-before.json', bundle), ('interaction-before.json', texts)]:
        if not (backup / name).exists():
            write_json(backup / name, value)
    for image, data in blobs.items():
        for base in (registry_path.parent, bundle_path.parent):
            path = base / image
            path.parent.mkdir(parents=True, exist_ok=True)
            if path.exists():
                assert path.read_bytes() == data
            else:
                path.write_bytes(data)
    cat = dict(cat_id='shop-3', name='ハナ', introduction='白黒の短毛と銅色の目、白い尾先を持つ、遊び好きで繊細な猫。', features=profile['features'], appearance=profile['appearance'], scenes=[dict(key=k, image=v, label=k, text='ハナの様子です。') for k,v in scenes.items()])
    existing = next((c for c in bundle['cats'] if c['cat_id'] == 'shop-3'), None)
    if existing:
        assert existing == cat
    else:
        bundle['cats'].append(cat)
    if not any(c['cat_id'] == 'shop-3' for c in texts['cat_bindings']):
        texts['cat_bindings'].append(dict(cat_id='shop-3', content_cat_id='shop-3', subject_id='shop-3', name='ハナ'))
    for mode in MODES + ('pause',):
        for operation in actions(mode):
            key = mode if operation in ('any', 'pause') else mode + '__' + operation
            resolved = key if key in scenes else mode
            row = dict(cat_id='shop-3', content_cat_id='shop-3', scene_key=key, image=scenes[resolved], resolved_scene_key=resolved)
            existing = next((r for r in texts['image_bindings'] if r['cat_id'] == 'shop-3' and r['scene_key'] == key), None)
            if existing:
                assert existing == row
            else:
                texts['image_bindings'].append(row)
    payload = {k:v for k,v in texts.items() if k not in ('pack_id','revision')}
    texts['revision'] = hashlib.sha256(json.dumps(payload,ensure_ascii=False,sort_keys=True,separators=(',',':')).encode()).hexdigest()
    registry['cats']['shop-3'] = scenes
    assert texts['entries'] == old_entries
    assert [c for c in bundle['cats'] if c['cat_id'] != 'shop-3'] == old_cats
    assert {k:v for k,v in registry['cats'].items() if k != 'shop-3'} == {k:v for k,v in old_registry['cats'].items() if k != 'shop-3'}
    write_json(registry_path, registry)
    write_json(bundle_path, bundle)
    write_json(texts_path, texts)
    production_catalog = ROOT / 'cat_cafe_sim/config/cafe_playtest_cats.json'
    rows = json.loads(production_catalog.read_text(encoding='utf-8'))
    matching = next((r for r in rows if r['cat_id'] == 'shop-3'), None)
    if matching:
        assert matching == profile
    else:
        rows.append(profile)
        write_json(production_catalog, rows)
    subjects = SubjectRegistry(registry_path.parent / 'interaction_subjects.json')
    if not subjects.get('shop-3'):
        subjects.save(dict(id='shop-3',kind='cat_response',name='ハナ',game_id='shop-3',content_id='shop-3',notes='白黒・短毛、銅色の目、丸い顔、白い尾先。遊び好きで繊細な猫。',call='{cat}',enabled=True))
    write_json(SOURCE/'manifest.json',dict(cat_id='shop-3',name='ハナ',date='2026-10-20',tool='built-in image_gen',images=[dict(scene=k,file=selected_files[k]) for k in keys],scenes=scenes,active_pack=str(bundle_path.parent),authored_entries_unchanged=True,existing_cats_unchanged=True))
    print('Registered 27 PNGs, 52 scene keys, 32 narrative bindings; existing cats/text retained')

if __name__ == '__main__':
    register()
