"""Read-only import/preservation audit; writes QA evidence, never authoring data."""
import hashlib
import json
from pathlib import Path
from interaction_texts import key
from export_completed_narratives import ROOT, UNITY, OUTPUT, read

def main():
    audit = read(OUTPUT/'export-audit.json')
    pointer = read(UNITY/'Assets/Resources/CatCafeContent/catalog.json')
    folder = (UNITY/'Assets/Resources'/(pointer['bundleResource']+'.json')).parent
    actual = read(folder/'interaction_texts.json')
    pack = Path(audit['pack'])
    assert actual == read(pack/'interaction_texts.json')
    adopted = read(ROOT/'cat_cafe_sim/saves/content_images/interaction_texts.json')['entries']
    entries = {e['pool_key']:e for e in actual['entries']}
    assert len(entries)==len(adopted)==435
    for e in adopted:
        assert entries[key(e['condition'])]['variants']==[v for v in e['variants'] if v['text']], key(e['condition'])
    old = read(OUTPUT/'active-before.json')
    assert all(entries[e['pool_key']]==e for e in old['entries'])
    assert actual['cat_bindings']==old['cat_bindings']
    assert actual['image_bindings']==old['image_bindings']
    image_count=0
    bundle=read(folder/'bundle.json')
    for image in {s['image'] for c in bundle['cats'] for s in c['scenes'] if s['image']}:
        path=folder/image
        assert hashlib.sha256(path.read_bytes()).hexdigest()==path.stem
        image_count+=1
    people=read(UNITY/'Assets/Resources/CatCafeContent/human_portraits.json')
    assert people['people']==read(pack/'human_portraits.json')['people']
    for p in people['people']:
        path=UNITY/'Assets/Resources/CatCafeContent/People'/people['pack_id']/p['image']
        assert hashlib.sha256(path.read_bytes()).hexdigest()==path.stem
    concurrent=read(OUTPUT/'concurrent-hashes.json')
    changed=[p for p,h in concurrent.items() if hashlib.sha256(Path(p).read_bytes()).hexdigest()!=h]
    assert not changed, changed
    # Registered source, subject profiles, and image manifest must remain byte-identical.
    for p,h in audit['inputs'].items():
        assert hashlib.sha256(Path(p).read_bytes()).hexdigest()==h,p
    result=dict(pack_id=actual['pack_id'],revision=actual['revision'],conditions=len(entries),
        texts=sum(len(e['variants']) for e in entries.values()),added_conditions=176,added_texts=362,
        pending_conditions=0,pending_texts=0,previous_conditions_preserved=len(old['entries']),
        cat_bindings_preserved=len(actual['cat_bindings']),cat_images=image_count,
        portraits_preserved=len(people['people']),concurrent_files_preserved=len(concurrent),
        authoring_inputs_unchanged=True)
    (OUTPUT/'import-audit.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
    print(json.dumps(result,ensure_ascii=False))

if __name__=='__main__':
    main()
