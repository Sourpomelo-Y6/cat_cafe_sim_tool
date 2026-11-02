"""Small labeled fixture textures, never adopted into the real source store."""
import json
import sys
from pathlib import Path
from PIL import Image, ImageDraw
from state_images import StateImageStore, STATES
from content_images import ImageRegistry
from content_texts import TextStore
from unity_export import export_bundle

root = Path(sys.argv[1]).resolve()
root.mkdir(parents=True, exist_ok=True)
catalog = {'playtest-mike': 'ミケ'}
store = StateImageStore(root / 'private-source')
for state, color, size in zip(STATES, ('#d96464', '#69a6d9', '#73bd85'), ((160, 100), (80, 160), (128, 128))):
    path = root / (state + '.png')
    image = Image.new('RGBA', size, color)
    ImageDraw.Draw(image).text((4, 4), state, fill='black')
    image.save(path)
    store.adopt(state, path)
packs = {'all': str(store.export(root / 'packs', catalog))}
registry = ImageRegistry(root / 'private-source')
packs['full'] = str(export_bundle(catalog, registry, TextStore(root / 'texts.json'), root / 'packs', profiles={})[0])
store.adopt('stress_high')
packs['partial'] = str(store.export(root / 'packs', catalog))
for state in STATES:
    store.adopt(state)
packs['empty'] = str(store.export(root / 'packs', catalog))
(root / 'packs.json').write_text(json.dumps(packs, indent=2), encoding='utf-8')
print(json.dumps(packs))
