"""Register Chiro's approved daily production without replacing existing cats."""
import copy
import hashlib
import json
from pathlib import Path
from register_cat_execution_images import actions, write_json, MODES
from interaction_subjects import SubjectRegistry

ROOT = Path(__file__).resolve().parent.parent
SOURCE = Path(__file__).resolve().parent / 'Docs/Results/ChiroImages'
PROJECT = Path('C:/Users/hi-wa/unity_workspase/CatCafeSim')

def register():
    profile = json.loads((SOURCE / 'profile.json').read_text(encoding='utf-8'))
    keys = ['normal', 'sleep', 'pause'] + list(MODES) + [m + '__' + r for m in MODES for r in ('open_up', 'simultaneous')]
    assert len(keys) == 27
    blobs = {}
    scene_images = {}
    for key in keys:
        data = (SOURCE / ('chiro-' + key + '.png')).read_bytes()
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
    assert 'shop-1' not in registry['cats'] or registry['cats']['shop-1'] == scenes
    resources = PROJECT / 'Assets/Resources'
    catalog = json.loads((resources / 'CatCafeContent/catalog.json').read_text(encoding='utf-8-sig'))
    bundle_path = resources / (catalog['bundleResource'] + '.json')
    texts_path = resources / (catalog['interactionResource'] + '.json')
    bundle = json.loads(bundle_path.read_text(encoding='utf-8'))
    texts = json.loads(texts_path.read_text(encoding='utf-8'))
    assert texts['version'] == 3
    old_cats = copy.deepcopy([c for c in bundle['cats'] if c['cat_id'] != 'shop-1'])
    old_entries = copy.deepcopy(texts['entries'])
    backup = ROOT / 'Logs/CatDailyImages/20261018'
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
    cat = dict(cat_id='shop-1', name='チロ', introduction='白い長毛と金色の目を持つ、丸みのある中立の猫。', features=profile['features'], appearance=profile['appearance'], scenes=[dict(key=k, image=v, label=k, text='チロの様子です。') for k,v in scenes.items()])
    existing = next((c for c in bundle['cats'] if c['cat_id'] == 'shop-1'), None)
    if existing:
        assert existing == cat
    else:
        bundle['cats'].append(cat)
    if not any(c['cat_id'] == 'shop-1' for c in texts['cat_bindings']):
        texts['cat_bindings'].append(dict(cat_id='shop-1', content_cat_id='shop-1', subject_id='shop-1', name='チロ'))
    for mode in MODES + ('pause',):
        for operation in actions(mode):
            key = mode if operation in ('any', 'pause') else mode + '__' + operation
            resolved = key if key in scenes else mode
            row = dict(cat_id='shop-1', content_cat_id='shop-1', scene_key=key, image=scenes[resolved], resolved_scene_key=resolved)
            existing = next((r for r in texts['image_bindings'] if r['cat_id'] == 'shop-1' and r['scene_key'] == key), None)
            if existing:
                assert existing == row
            else:
                texts['image_bindings'].append(row)
    payload = {k:v for k,v in texts.items() if k not in ('pack_id','revision')}
    texts['revision'] = hashlib.sha256(json.dumps(payload,ensure_ascii=False,sort_keys=True,separators=(',',':')).encode()).hexdigest()
    registry['cats']['shop-1'] = scenes
    assert texts['entries'] == old_entries
    assert [c for c in bundle['cats'] if c['cat_id'] != 'shop-1'] == old_cats
    assert {k:v for k,v in registry['cats'].items() if k != 'shop-1'} == {k:v for k,v in old_registry['cats'].items() if k != 'shop-1'}
    write_json(registry_path, registry)
    write_json(bundle_path, bundle)
    write_json(texts_path, texts)
    production_catalog = ROOT / 'cat_cafe_sim/config/cafe_playtest_cats.json'
    rows = json.loads(production_catalog.read_text(encoding='utf-8'))
    matching = next((r for r in rows if r['cat_id'] == 'shop-1'), None)
    if matching:
        assert matching == profile
    else:
        rows.append(profile)
        write_json(production_catalog, rows)
    subjects = SubjectRegistry(registry_path.parent / 'interaction_subjects.json')
    if not subjects.get('shop-1'):
        subjects.save(dict(id='shop-1',kind='cat_response',name='チロ',game_id='shop-1',content_id='shop-1',notes='白い長毛、金色の目、丸い顔とがっしりした体。中立。',call='{cat}',enabled=True))
    write_json(SOURCE/'manifest.json',dict(cat_id='shop-1',name='チロ',date='2026-10-18',tool='built-in image_gen',images=[dict(scene=k,file='chiro-'+k+'.png') for k in keys],scenes=scenes,active_pack=str(bundle_path.parent),authored_entries_unchanged=True,existing_cats_unchanged=True))
    print('Registered 27 PNGs, 52 scene keys, 32 narrative bindings; existing cats/text retained')

if __name__ == '__main__':
    register()
