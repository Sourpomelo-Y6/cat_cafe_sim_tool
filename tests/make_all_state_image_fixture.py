"""Isolated v1/v2 packs for integration QA; no real registrations are touched."""
import json
import sys
from pathlib import Path
from PIL import Image, ImageDraw
from state_images import StateImageStore, STATES, definition_text
from cat_outfits import CatOutfitStore
from content_images import ImageRegistry
from content_texts import TextStore
from unity_export import export_bundle

root = Path(sys.argv[1]).resolve(); root.mkdir(parents=True, exist_ok=True)
source = root / 'private-source'
catalog = {f'playtest-{name}': name for name in ('mike', 'sora', 'kohaku')}
store = StateImageStore(source)
for index, cid in enumerate(catalog):
    for number, state in enumerate(STATES):
        image = Image.new('RGBA', (120 + index * 20, 120 + number * 20), ('#d96464', '#69a6d9', '#73bd85')[number])
        ImageDraw.Draw(image).text((4, 4), cid + '\n' + state, fill='black')
        path = root / f'{cid}-{state}.png'; image.save(path)
        store.adopt(state, path, content_id=cid)
packs = {'all': str(store.export(root / 'packs', catalog))}
# A real v1 contract: fixed Mike individual, not just a v2 version number change.
legacy = StateImageStore(root / 'legacy-source')
for state in STATES: legacy.adopt(state, root / f'playtest-mike-{state}.png')
folder = legacy.export(root / 'packs', catalog)
data = json.loads((folder / 'state_images.json').read_text()); data['version'] = 1
for row in data['bindings']: row['cat_id'] = 'cat-mike'
(folder / 'state_images.json').write_text(definition_text(data), encoding='utf-8')
packs['v1'] = str(folder)
image = Image.new('RGBA', (160, 120), '#e8b6dc'); ImageDraw.Draw(image).text((4, 4), 'RIBBON TEST ONLY', fill='black')
image.save(root / 'ribbon.png'); CatOutfitStore(source).adopt('ribbon', root / 'ribbon.png')
packs['full'] = str(export_bundle(catalog, ImageRegistry(source), TextStore(root / 'texts.json'), root / 'packs', profiles={})[0])
store.adopt('stress_high', content_id='playtest-sora')
packs['partial'] = str(store.export(root / 'packs', catalog))
for cid in catalog:
    for state in STATES: store.adopt(state, content_id=cid)
packs['empty'] = str(store.export(root / 'packs', catalog))
(root / 'packs.json').write_text(json.dumps(packs, indent=2), encoding='utf-8')
print(json.dumps(packs))

# Identical serialized cases are checked by Python, the new C# parser, and a v1 snapshot.
import copy
from state_images import read_definition
v2 = json.loads((Path(packs['all']) / 'state_images.json').read_text())
v1 = json.loads((Path(packs['v1']) / 'state_images.json').read_text())
row = v2['bindings'][0]
rows = [dict(row, content_cat_id=f'cat-{i}', state=state) for i in range(64) for state in STATES]
cases = []
def add_case(name, data, valid, legacy=False):
    text = data if isinstance(data, str) else json.dumps(data)
    try: read_definition(text); actual = True
    except ValueError: actual = False
    assert actual == valid, name
    cases.append(dict(name=name, json=text, valid=valid, legacy_valid=legacy))
add_case('v1', v1, True, True); add_case('v2', v2, True)
add_case('empty-v1', dict(v1, bindings=[]), True, True); add_case('empty-v2', dict(v2, bindings=[]), True)
add_case('192', dict(v2, bindings=rows), True); add_case('193', dict(v2, bindings=rows + [row]), False)
add_case('65 IDs', dict(v2, bindings=[dict(row, content_cat_id=f'cat-{i}') for i in range(65)]), False)
for key, value in [('version', True), ('version', '2'), ('version', 3), ('pack_id', '../bad'), ('bindings', [row, row]), ('bindings', {})]:
    add_case('root-' + key, dict(v2, **{key: value}), False)
for key, value in [('cat_id', 'cat-mike'), ('state', 'unknown'), ('state', []), ('content_cat_id', 'A'), ('content_cat_id', 'a' * 65), ('image', '../image.png')]:
    add_case('row-' + key, dict(v2, bindings=[dict(row, **{key: value})]), False)
add_case('id64', dict(v2, bindings=[dict(row, content_cat_id='a' * 64)]), True)
bad = copy.deepcopy(v1); bad['bindings'][0]['cat_id'] = 'cat-sora'; add_case('v1 other cat', bad, False)
add_case('duplicate key', '{"version":2,"version":2,"pack_id":"' + 'a' * 32 + '","bindings":[]}', False)
text = json.dumps(dict(v2, bindings=[]))
add_case('JSON100000', text + ' ' * (100000 - len(text)), True)
add_case('JSON100001', text + ' ' * (100001 - len(text)), False)
(root / 'contract-cases.json').write_text(json.dumps(dict(cases=cases)), encoding='utf-8')
print(f'PASS Python contract fixtures: {len(cases)} cases')
