"""Export confirmed prose through the normal exporter, preserving the live image scope.

The live Unity bundle includes concurrent adopted images. Never substitute a historic
committed bundle or include drafts. Run once per integration; reuse pack.txt thereafter.
"""
import copy
import hashlib
import json
from pathlib import Path
from content_images import ImageRegistry
from content_texts import TextStore
from interaction_subjects import SubjectRegistry
from interaction_texts import InteractionTextStore, key
from human_portraits import HumanPortraitStore
from unity_export import export_bundle

ROOT = Path(__file__).resolve().parents[1]
UNITY = Path('C:/Users/hi-wa/unity_workspase/CatCafeSim')
OUTPUT = ROOT / 'Logs/NarrativeIntegration'

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def main():
    OUTPUT.mkdir(exist_ok=True)
    if (OUTPUT / 'pack.txt').exists():
        raise RuntimeError('Reuse the recorded export; do not duplicate packs.')
    data = ROOT / 'cat_cafe_sim/saves/content_images'
    pointer = read(UNITY / 'Assets/Resources/CatCafeContent/catalog.json')
    active = UNITY / 'Assets/Resources' / (pointer['bundleResource'] + '.json')
    bundle, previous = read(active), read(active.parent / 'interaction_texts.json')
    allowed = {r['subject_id'] for r in previous['cat_bindings']}
    class CurrentImages(ImageRegistry):
        def read(self):
            return dict(version=1, cats={c['cat_id']: {s['key']: s['image'] for s in c['scenes'] if s['image']} for c in bundle['cats']})
        def _resolve(self, value):
            p = (active.parent / value).resolve()
            if active.parent.resolve() not in p.parents:
                raise ValueError('Image outside current pack')
            return p
    class CurrentSubjects(SubjectRegistry):
        def read(self):
            result = super().read()
            result['subjects'] = [r for r in result['subjects'] if r['kind'] == 'speech' or r['id'] in allowed]
            return result
    class CurrentDescriptions(TextStore):
        def read(self):
            return dict(version=1, cats={c['cat_id']: dict(introduction=c['introduction'], scenes={s['key']:s['text'] for s in c['scenes']}) for c in bundle['cats']})
    subjects = CurrentSubjects(data / 'interaction_subjects.json')
    store = InteractionTextStore(data / 'interaction_texts.json', published=True, subject_registry=subjects)
    adopted = store.read()['entries']
    before = {e['pool_key']:e for e in previous['entries']}
    pending = [e for e in adopted if key(e['condition']) not in before]
    assert len(adopted) == 435 and sum(bool(v['text']) for e in adopted for v in e['variants']) == 1123
    assert len(pending) == 176 and sum(bool(v['text']) for e in pending for v in e['variants']) == 362
    audit = read(Path(__file__).parent/'Docs/Results/MikePresenceNarratives/completion-audit.json')
    # Compare the exact audited pending candidate set, independent of document order.
    assert {key(e['condition']):e['missing_variants'] for e in audit['pending']} == {key(e['condition']):[v for v in e['variants'] if v['text']] for e in pending}
    portraits, _, warnings = HumanPortraitStore(data).export_data()
    current_people = read(UNITY/'Assets/Resources/CatCafeContent/human_portraits.json')
    assert portraits['people'] == current_people['people'], 'Portrait scope changed: review before exporting'
    profiles = {c['cat_id']:dict(appearance=c['appearance'],features=c['features']) for c in bundle['cats']}
    existing = list((OUTPUT/'packs').glob('CatCafeContent-*'))
    if len(existing) > 1:
        raise RuntimeError('Unexpected multiple integration packs')
    if existing:
        pack = existing[0]
        report = read(pack/'validation.json')
    else:
        pack, report = export_bundle({c['cat_id']:c['name'] for c in bundle['cats']}, CurrentImages(data),
            CurrentDescriptions(data/'texts.json'), OUTPUT/'packs', profiles=profiles, interaction_texts=store)
    exported = read(pack/'interaction_texts.json')
    entries = {e['pool_key']:e for e in exported['entries']}
    assert len(entries) == 435 and sum(len(e['variants']) for e in entries.values()) == 1123
    for k,e in before.items():
        assert entries[k] == e, k
    for e in adopted:
        assert entries[key(e['condition'])]['variants'] == [v for v in e['variants'] if v['text']], key(e['condition'])
    assert exported['cat_bindings'] == previous['cat_bindings']
    assert exported['image_bindings'] == previous['image_bindings']
    for p in previous['people']:
        assert p in exported['people']
    for old, new in zip(bundle['cats'], read(pack/'bundle.json')['cats']):
        assert all(old[k] == new[k] for k in ('cat_id','name','introduction','appearance','features'))
        # Normal exporter restores official labels/order on recently added image rows.
        assert {s['key']:(s['text'],s['image']) for s in old['scenes']} == {s['key']:(s['text'],s['image']) for s in new['scenes']}
    inputs = {str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in [data/'interaction_texts.json',data/'interaction_subjects.json',data/'manifest.json',data/'human_portraits.json',active,active.parent/'interaction_texts.json']}
    result = dict(source_bundle=str(active),pack=str(pack),adopted_conditions=435,adopted_texts=1123,
        previous_conditions=259,previous_texts=761,added_conditions=176,added_texts=362,
        pending=pending,cat_bindings=exported['cat_bindings'],people=exported['people'],inputs=inputs,
        images_preserved=True,portraits_preserved=True,previous_entries_preserved=True)
    (OUTPUT/'export-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    (OUTPUT/'pack.txt').write_text(str(pack),encoding='utf8')
    print(pack)
    print(f"435 conditions / 1123 texts; +176 / +362; {report['image_count']} images; current image bindings and portraits preserved")

if __name__ == '__main__':
    main()
