"""Isolated F1/F2 packs and cross-language contracts; no real registrations."""
import copy
import json
import sys
import uuid
from pathlib import Path
from optional_events import (OptionalEventStore, validate_event, default_event,
                             normalize_event, validate_pack, read_pack, definition_text)
from test_optional_events_v2 import authored
from content_images import ImageRegistry
from content_texts import TextStore
from unity_export import export_bundle

root = Path(sys.argv[1]).resolve(); root.mkdir(parents=True, exist_ok=True)
catalog = {'playtest-mike': 'ミケ', 'playtest-sora': 'ソラ'}
mike = normalize_event(validate_event(default_event(), require_revision=False))
sora = authored('playtest-sora')
sora.update(min_day=1, min_affinity=0)
sora = validate_event({k: v for k, v in sora.items() if k != 'revision'}, require_revision=False)
packs = {}
for name, version, rows in [('all', 2, [mike, sora]), ('v1', 1, [validate_event(default_event(), require_revision=False)]), ('empty', 2, [])]:
    folder = root / name; folder.mkdir(exist_ok=True)
    data = dict(version=version, pack_id=uuid.uuid4().hex, events=rows)
    (folder / 'optional_events.json').write_text(definition_text(validate_pack(data)), encoding='utf-8')
    packs[name] = str(folder)
changed = copy.deepcopy(sora); changed['introduction'][0]['text'] = '改稿された新しい時間。'
changed = validate_event({k: v for k, v in changed.items() if k != 'revision'}, require_revision=False)
folder = root / 'changed'; folder.mkdir(exist_ok=True)
(folder / 'optional_events.json').write_text(definition_text(dict(version=2, pack_id=uuid.uuid4().hex, events=[mike, changed])), encoding='utf-8')
packs['changed'] = str(folder)
store = OptionalEventStore(root / 'private-source/optional_events.json')
store.save_all(dict(version=2, entries=[dict(enabled=True, event=e) for e in [mike, sora]]))
packs['full'] = str(export_bundle(catalog, ImageRegistry(root / 'private-source'), TextStore(root / 'texts.json'), root / 'packs', profiles={})[0])
old_full = export_bundle(catalog, ImageRegistry(root / 'private-source'), TextStore(root / 'texts.json'), root / 'packs', profiles={})[0]
(old_full / 'optional_events.json').unlink()
bundle = json.loads((old_full / 'bundle.json').read_text(encoding='utf-8')); bundle['optional_events'] = ''
(old_full / 'bundle.json').write_text(json.dumps(bundle, ensure_ascii=False, indent=2), encoding='utf-8')
packs['old_full'] = str(old_full)
(root / 'packs.json').write_text(json.dumps(packs, indent=2), encoding='utf-8')
base = dict(version=2, pack_id='1' * 32, events=[mike, sora]); cases = []
def add(name, data):
    text = data if isinstance(data, str) else json.dumps(data, ensure_ascii=False)
    try: read_pack(text); valid = True
    except ValueError: valid = False
    cases.append(dict(name=name, json=text, valid=valid))
add('v2', base)
add('v1', dict(version=1, pack_id='1'*32, events=[validate_event(default_event(), require_revision=False)]))
for key, value in [('target_kind', 'script'), ('cat_id', 'cat-sora'), ('event_id', 'bad'), ('content_cat_id', 'A'), ('min_day', True), ('min_day', 2.0), ('min_affinity', 101), ('title', 'x'*31), ('accepted', []), ('revision', 'a'*64)]:
    bad = copy.deepcopy(base); bad['events'][1][key] = value; add(key, bad)
rows = []
for i in range(64):
    event = copy.deepcopy(sora); event['event_id'] = 'optional.c'+str(i)+'.story'; event['content_cat_id'] = 'c'+str(i)
    rows.append(validate_event({k:v for k,v in event.items() if k!='revision'}, require_revision=False))
add('64', dict(version=2, pack_id='1'*32, events=rows))
add('65', dict(version=2, pack_id='1'*32, events=rows+[sora]))
add('duplicate', dict(version=2, pack_id='1'*32, events=[sora,sora]))
text = json.dumps(base, ensure_ascii=False); units = len(text.encode('utf-16-le'))//2
add('json1000000', text+' '*(1000000-units)); add('json1000001', text+' '*(1000001-units))
add('duplicate-root', text.replace('"version": 2', '"version": 2,"version":2'))
(root / 'cases.json').write_text(json.dumps(dict(cases=cases), ensure_ascii=False), encoding='utf-8')
print(json.dumps(packs))
