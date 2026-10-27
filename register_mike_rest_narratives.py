import copy, json, hashlib, tkinter as tk
from pathlib import Path
from datetime import datetime, timezone
from content_images import ImageRegistry
from interaction_subjects import SubjectRegistry
from interaction_texts import InteractionTextStore, DEFAULT_DRAFT_PATH, condition, key
from interaction_text_editor_gui import InteractionTextEditorWindow
from interaction_preview_gui import InteractionPreviewWindow
from interaction_presentation import select_entry, resolve_image
ROOT=Path(__file__).resolve().parents[1]; BASE=ROOT/'cat_cafe_sim/saves/content_images'; OUT=Path(__file__).parent/'Docs/Results/MikeRestNarratives'; OUT.mkdir(parents=True,exist_ok=True)
s=SubjectRegistry(BASE/'interaction_subjects.json'); p=InteractionTextStore(BASE/'interaction_texts.json',published=True,subject_registry=s); d=InteractionTextStore(DEFAULT_DRAFT_PATH,subject_registry=s); images=ImageRegistry(BASE)
c=condition('cat_response','cat-mike','pause','pause','listless',subjects=s.labels()); before=p.read(); dbefore=d.read(); old=d.get(c)
assert old and p.get(c) is None
files=list((ROOT/'cat_cafe_sim_tool/generated/interaction_texts').rglob('*.json'))
matches=[]
for f in files:
 data=json.loads(f.read_text(encoding='utf-8'))
 if data.get('condition')==c: matches.append(str(f))
protected=[BASE/'interaction_subjects.json',BASE/'manifest.json']+list(BASE.glob('*bindings*.json'))+files
hashes={str(x):hashlib.sha256(x.read_bytes()).hexdigest() for x in protected}
root=tk.Tk(); root.withdraw(); editor=InteractionTextEditorWindow(root,d,p,subject_registry=s,image_registry=images,catalog_provider=lambda:s.image_catalog({}))
assert editor.load_condition(c) and editor.values()==old
texts=['ミケは疲れた様子で、静かに佇んだ。',old['variants'][1]['text'],'ミケは動きを控え、ゆったりと休んでいた。']
for w,t in zip(editor.fields,texts): w.delete('1.0','end'); w.insert('1.0',t)
root.update(); assert editor.save_draft(); editor.preview(); entry=editor.values(); assert editor.reviewed==entry
assert [v['id'] for v in old['variants']]==[v['id'] for v in entry['variants']]
preview=InteractionPreviewWindow(root,s.image_catalog({}),images,p,editing_entry=entry,drafts=d); root.update()
resolved=resolve_image(images,'playtest-mike','pause','pause'); assert resolved['resolved_scene_key']=='pause' and preview.image is not None
previews=[]
for i,t in enumerate(texts,1):
 preview.variables['cat'].set('案'+str(i)); preview.render(); root.update(); body=preview.body.get('1.0','end-1c'); assert t in body and '{' not in body and '}' not in body
 previews.append(dict(slot=i,body=body,source=preview.source_notice.get(),image_notice=preview.image_notice.get()))
assert editor.register(),editor.notice.get(); assert editor.reload() and editor.values()==entry
new=InteractionTextStore(p.path,published=True,subject_registry=s); assert new.get(c)==entry
editor2=InteractionTextEditorWindow(root,d,new,subject_registry=s); assert editor2.load_condition(c) and editor2.values()==entry
post=p.read(); dpost=d.read(); assert len(post['entries'])==len(before['entries'])+1
assert all(e in post['entries'] for e in before['entries']); assert all(e in dpost['entries'] for e in dbefore['entries'] if key(e['condition'])!=key(c)); assert len(dpost['entries'])==len(dbefore['entries'])
assert all(hashlib.sha256(Path(f).read_bytes()).hexdigest()==h for f,h in hashes.items())
found=select_entry(post['entries'],c); prior=select_entry(before['entries'],c); assert [v['id'] for v in found['candidates']]==[v['id'] for v in entry['variants']] and not prior['candidates']
assert len(set(texts))==3 and all('{' not in t and '}' not in t for t in texts)
record=dict(registered_at=datetime.now(timezone.utc).isoformat(),condition=c,condition_key=key(c),before=old,after=entry,previous_published=None,published_counts=[len(before['entries']),len(post['entries'])],draft_counts=[len(dbefore['entries']),len(dpost['entries'])],generated_files=len(files),matching_generated=matches,profile=s.get('cat-mike'),image=str(resolved['source_path']),image_sha256=hashlib.sha256(resolved['data']).hexdigest(),previews=previews,selection_before=prior,selection_after=found,unrelated_entries_drafts_and_protected_hashes_unchanged=True,unity_imported=False)
(OUT/'registration.json').write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n',encoding='utf-8'); print('PASS',key(c),len(files),len(matches)); root.destroy()
