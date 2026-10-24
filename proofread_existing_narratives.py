"""Bounded correction through the existing optimistic-concurrency store API."""
import copy, hashlib, json
from pathlib import Path
from interaction_texts import InteractionTextStore, DEFAULT_DRAFT_PATH, key, make_entry
from interaction_subjects import SubjectRegistry

root=Path(__file__).resolve().parents[1]
out=root/'Logs/NarrativeProofreading'; out.mkdir(exist_ok=True)
base=root/'cat_cafe_sim/saves/content_images'
registry=SubjectRegistry(base/'interaction_subjects.json')
store=InteractionTextStore(base/'interaction_texts.json',published=True,subject_registry=registry)
draft=InteractionTextStore(DEFAULT_DRAFT_PATH,subject_registry=registry)
before=store.read(); dbefore=draft.read()
changes={
 ('cat-52bb33a93ad94bb582a5304f64a75000','pause','listless'):{
 1:'{cat}は疲れた様子で、静かに休んでいた。',
 2:'{cat}は動きを控え、そっと休憩していた。',
 3:'{cat}はあまり動かず、自分のペースで休んでいた。'},
 ('cat-52bb33a93ad94bb582a5304f64a75000','pause','neutral'):{3:'{cat}はしばらくの間、その場で静かに待った。'},
 ('cat-mike','teaser','neutral'):{
 1:'ミケはねこじゃらしに目を向け、穏やかな様子で動きを見ていた。',
 3:'ミケはねこじゃらしの動きを、静かに目で追った。'}}
entries=[]; records=[]
for e in before['entries']:
 c=e['condition']; edits=changes.get((c['subject_id'],c['mode'],c['reaction']))
 if not edits or c['action'] not in ('any','pause'): continue
 texts=[edits.get(v['slot'],v['text']) for v in e['variants']]
 new=make_entry(c,texts); assert new!=e
 assert draft.get(c)==e, 'Existing draft differs: review first'
 entries.append(new); records.append({'before':e,'after':new})
assert len(entries)==3 and sum(a['text']!=b['text'] for r in records for a,b in zip(r['before']['variants'],r['after']['variants']))==6
for name,data in [('before',before),('draft-before',dbefore)]:
 (out/(name+'.json')).write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
expected={key(r['before']['condition']):r['before'] for r in records}
draft.save_many(entries,expected=expected); store.save_many(entries,expected=expected)
for original,current in [(before,store.read()),(dbefore,draft.read())]:
 changed=set(expected); assert len(original['entries'])==len(current['entries'])
 assert {key(e['condition']):e for e in original['entries'] if key(e['condition']) not in changed}=={key(e['condition']):e for e in current['entries'] if key(e['condition']) not in changed}
result={'changed_conditions':3,'changed_variants':6,'unaffected_entries_and_drafts_identical':True,'records':records}
(out/'registration.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print('PASS: 3 conditions / 6 variants; IDs and all other entries/drafts preserved')
