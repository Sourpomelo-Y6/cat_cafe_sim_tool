"""Generate isolated authored packs; never touch real registrations."""
import json
import sys
from pathlib import Path
from PIL import Image, ImageDraw
from cat_outfits import CatOutfitStore
from state_images import StateImageStore, STATES
from content_images import ImageRegistry
from content_texts import TextStore
from unity_export import export_bundle

root = Path(sys.argv[1]).resolve()
root.mkdir(parents=True, exist_ok=True)
catalog = {'playtest-mike': 'ミケ', 'playtest-sora': 'ソラ'}
source = root / 'private-source'
store = CatOutfitStore(source)
image = Image.new('RGBA', (180, 120), '#e8b6dc')
ImageDraw.Draw(image).text((10, 10), 'F5 RIBBON TEST ONLY', fill='black')
image.save(root / 'ribbon.png')
store.adopt('ribbon', root / 'ribbon.png')
store.adopt('ribbon', root / 'ribbon.png', content_id='playtest-sora', label='ソラのリボン')
packs = {'all': str(store.export(root / 'packs', catalog))}
import uuid
legacy = json.loads((Path(packs['all']) / 'cat_outfits.json').read_text(encoding='utf-8'))
legacy['version'] = 1; legacy['pack_id'] = uuid.uuid4().hex; legacy['outfits'] = [dict(legacy['outfits'][0], cat_id='cat-mike')]
v1 = root / 'packs' / ('CatCafeOutfits-' + legacy['pack_id']); (v1 / 'images').mkdir(parents=True)
import shutil
for row in legacy['outfits']: shutil.copyfile(Path(packs['all']) / row['image'], v1 / row['image'])
(v1 / 'cat_outfits.json').write_text(json.dumps(legacy, ensure_ascii=False, indent=2), encoding='utf-8')
packs['v1'] = str(v1)
packs['full'] = str(export_bundle(catalog, ImageRegistry(source), TextStore(root / 'texts.json'), root / 'packs', profiles={})[0])
store.adopt('ribbon')
store.adopt('ribbon', content_id='playtest-sora')
packs['empty'] = str(store.export(root / 'packs', catalog))
states = StateImageStore(source)
for state, color in zip(STATES, ('#d96464', '#69a6d9', '#73bd85')):
    image = Image.new('RGBA', (128, 128), color)
    ImageDraw.Draw(image).text((4, 4), state, fill='black')
    image.save(root / (state + '.png'))
    states.adopt(state, root / (state + '.png'))
    states.adopt(state, root / (state + '.png'), content_id='playtest-sora')
packs['states'] = str(states.export(root / 'packs', catalog))
(root / 'packs.json').write_text(json.dumps(packs, indent=2), encoding='utf-8')
print(json.dumps(packs))

# Parse-only boundary cases are also consumed by the Unity verifier.
import copy
from cat_outfits import read_definition
base = json.loads((Path(packs['all']) / 'cat_outfits.json').read_text(encoding='utf-8'))
cases = []
def add_case(name, data):
    text = data if isinstance(data, str) else json.dumps(data, ensure_ascii=False)
    try:
        read_definition(text)
        valid = True
    except ValueError:
        valid = False
    cases.append(dict(name=name, json=text, valid=valid))
add_case('v2', base)
add_case('v1', legacy)
for name, value in [('bool', True), ('unknown', 3), ('float', 2.0)]:
    data = copy.deepcopy(base); data['version'] = value
    add_case('version-' + name, data)
for key, value in [('cat_id', 'cat-mike'), ('content_cat_id', 'A'), ('outfit_id', 'future-set'), ('label', ' '), ('label', []), ('image', '../image.png'), ('label', '\U0001f408' * 20), ('label', '\U0001f408' * 21)]:
    data = copy.deepcopy(base); data['outfits'][0][key] = value
    add_case(key + '-' + str(value)[:10], data)
data = copy.deepcopy(base)
data['outfits'] = [dict(base['outfits'][0], content_cat_id='c' + str(i)) for i in range(64)]
add_case('64', data)
data['outfits'].append(dict(data['outfits'][0], content_cat_id='extra'))
add_case('65', data)
data = copy.deepcopy(base); data['outfits'].append(data['outfits'][0]); add_case('duplicate', data)
data = copy.deepcopy(legacy); data['outfits'][0]['cat_id'] = 'rescue-1'; add_case('legacy-not-Mike', data)
data = copy.deepcopy(base); data['outfits'] = []; add_case('empty', data)
text = json.dumps(base, ensure_ascii=False)
units = len(text.encode('utf-16-le')) // 2
add_case('json100000', text + ' ' * (100000 - units))
add_case('json100001', text + ' ' * (100001 - units))
add_case('duplicate-root', text.replace('"version": 2', '"version": 2,"version":2'))
(root / 'contract-cases.json').write_text(json.dumps(dict(cases=cases), ensure_ascii=False), encoding='utf-8')
