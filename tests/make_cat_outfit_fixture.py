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
catalog = {'playtest-mike': 'ミケ'}
source = root / 'private-source'
store = CatOutfitStore(source)
image = Image.new('RGBA', (180, 120), '#e8b6dc')
ImageDraw.Draw(image).text((10, 10), 'F5 RIBBON TEST ONLY', fill='black')
image.save(root / 'ribbon.png')
store.adopt('ribbon', root / 'ribbon.png')
packs = {'all': str(store.export(root / 'packs', catalog))}
packs['full'] = str(export_bundle(catalog, ImageRegistry(source), TextStore(root / 'texts.json'), root / 'packs', profiles={})[0])
store.adopt('ribbon')
packs['empty'] = str(store.export(root / 'packs', catalog))
states = StateImageStore(source)
for state, color in zip(STATES, ('#d96464', '#69a6d9', '#73bd85')):
    image = Image.new('RGBA', (128, 128), color)
    ImageDraw.Draw(image).text((4, 4), state, fill='black')
    image.save(root / (state + '.png'))
    states.adopt(state, root / (state + '.png'))
packs['states'] = str(states.export(root / 'packs', catalog))
(root / 'packs.json').write_text(json.dumps(packs, indent=2), encoding='utf-8')
print(json.dumps(packs))
