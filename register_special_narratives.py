"""代表8欄だけを既存Toolの下書き→確認→登録経路で制作する。"""
import json
import hashlib
import tkinter as tk
from pathlib import Path
from interaction_subjects import SubjectRegistry
from interaction_texts import InteractionTextStore, DEFAULT_DRAFT_PATH, condition, key
from interaction_text_editor_gui import InteractionTextEditorWindow
from content_images import ImageRegistry

ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Logs/SpecialInteraction'
TEXTS={
 'switch': ('今度はねこじゃらし。穂先の動き、気になるかな。','{cat}は新しく出てきたねこじゃらしへ、そっと視線を移した。'),
 'connect': ('{cat_call}、今だね。この間合い、つかめた気がする。','{cat}は相手へ注意を向け、その働きかけをまっすぐ受け止めた。'),
 'open_up': ('{cat_call}、さっきより近く感じるね。急がず遊ぼう。','{cat}は警戒をゆるめ、ねこじゃらしの向こうの相手へ穏やかなまなざしを向けた。'),
 'simultaneous': ('目が合ったね、{cat_call}。今、気持ちが通じたかな。','{cat}は働きかけに応えると、安心したように相手へ親しみのこもった目を向けた。'),
}

def capacity():
 return {name:sum(p.stat().st_size for p in folder.rglob('*') if p.is_file()) for name,folder in {
  'Logs':ROOT/'Logs','generated':ROOT/'cat_cafe_sim_tool/generated',
  'content_images':ROOT/'cat_cafe_sim/saves/content_images','Latest':ROOT/'Logs/StandaloneBuild/Latest',
  'Library':Path('C:/Users/hi-wa/unity_workspase/CatCafeSim/Library')}.items()}

def main():
 OUT.mkdir(exist_ok=True)
 data=ROOT/'cat_cafe_sim/saves/content_images'
 subjects=SubjectRegistry(data/'interaction_subjects.json')
 store=InteractionTextStore(data/'interaction_texts.json',published=True,subject_registry=subjects)
 drafts=InteractionTextStore(DEFAULT_DRAFT_PATH,subject_registry=subjects)
 before=store.read();old_draft=drafts.read()
 protected={str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in [data/'interaction_subjects.json',data/'manifest.json']}
 start=capacity()
 root=tk.Tk();root.withdraw()
 editor=InteractionTextEditorWindow(root,drafts,store,subject_registry=subjects,image_registry=ImageRegistry(data),catalog_provider=lambda:subjects.image_catalog({}))
 entries=[]
 try:
  for scene,texts in TEXTS.items():
   for kind,subject,text in [('speech','player',texts[0]),('cat_response','cat-mike',texts[1])]:
    value=condition(kind,subject,action='any' if scene=='open_up' else 'switch' if scene=='switch' else 'connect',subjects=subjects.labels(),target_cat='cat-mike' if kind=='speech' else None,narrative_scene=scene)
    assert not store.get(value),'Already registered: review before authoring again'
    assert editor.load_condition(value)
    editor.fields[0].insert('1.0',text);root.update()
    assert editor.save_draft();editor.preview();assert editor.register()
    assert editor.reload() and editor.values()==store.get(value)
    entries.append(editor.values())
  editor.open_preview();root.update()
  for scene in TEXTS:
   editor.load_condition(next(e['condition'] for e in entries if e['condition']['kind']=='speech' and e['condition']['narrative_scene']==scene))
   editor.open_preview();root.update()
  assert all(e in store.read()['entries'] for e in before['entries'])
  assert all(e in drafts.read()['entries'] for e in old_draft['entries'])
  assert all(hashlib.sha256(Path(p).read_bytes()).hexdigest()==h for p,h in protected.items())
  (OUT/'registration.json').write_text(json.dumps(dict(entries=entries,adopted_profiles=[subjects.get('player'),subjects.get('cat-mike')],before_count=len(before['entries']),after_count=len(store.read()['entries']),protected=protected,capacity_before=start,capacity_after=capacity(),route='Tk editor save_draft / preview / register / reload; manual writing, no model request'),ensure_ascii=False,indent=2),encoding='utf8')
  print('PASS: 4 scenes / 8 fields; existing prose and profiles preserved; previews rendered')
 finally:root.destroy()

if __name__=='__main__':main()
