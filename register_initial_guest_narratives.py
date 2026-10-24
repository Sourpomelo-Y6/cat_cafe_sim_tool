import sys,json,hashlib
from pathlib import Path
root=Path(__file__).resolve().parents[1];sys.path.insert(0,str(root/'cat_cafe_sim_tool'))
from interaction_texts import InteractionTextStore,condition,make_entry,key,DEFAULT_DRAFT_PATH
from interaction_subjects import SubjectRegistry,expand
base=root/'cat_cafe_sim/saves/content_images';out=root/'Logs/InitialGuestNarratives'
registry=SubjectRegistry(base/'interaction_subjects.json')
store=InteractionTextStore(base/'interaction_texts.json',published=True,subject_registry=registry)
drafts=InteractionTextStore(DEFAULT_DRAFT_PATH,subject_registry=registry)
before=store.read(); olddraft=drafts.read(); labels=registry.labels()
texts={
'guest-1':{
'ball':['ボールを少し動かしてみてもいいですか。','{cat_call}、ボールはいかがですか。','よかったら、このボールで遊びませんか。'],
'plush':['ぬいぐるみで遊んでみませんか。','{cat_call}、ぬいぐるみはいかがですか。','このぬいぐるみを動かしてみますね。'],
'tunnel':['トンネルで遊んでみてもいいですか。','{cat_call}、トンネルはいかがですか。','気が向いたら、トンネルで遊びませんか。'],
'brush':['ブラシを使ってみてもいいですか。','{cat_call}、ブラッシングはいかがですか。','無理のない範囲で、ブラシをかけてみますね。'],
'voice':['{cat_call}、少しお話ししてもいいですか。','お返事は気にせず、聞いていてくださいね。','少しだけ、声をかけてみますね。'],
'presence':['そっと、ここで様子を見ていましょう。','{cat_call}のペースを大切にしたいですね。','急がず、静かに見守っていましょう。']},
'person-952c631405e649fdb50c390881804977':{
'ball':['{cat_call}、ボールで遊んでみませんか。','このボール、少し動かしてみますね。','気が向いたら、ボール遊びもいいですね。'],
'plush':['{cat_call}、ぬいぐるみで遊びませんか。','このぬいぐるみ、動かしてみましょうか。','ぬいぐるみ遊びも、よかったらどうですか。'],
'tunnel':['{cat_call}、トンネルで遊んでみませんか。','トンネル遊びも、よかったらどうですか。','気が向いたときに、トンネルで遊びましょう。'],
'brush':['{cat_call}、ブラシをかけてもいいですか。','ブラッシングを少し試してみましょうか。','ブラシはどうですか。無理せずいきましょう。'],
'voice':['{cat_call}、ちょっとお話ししましょうか。','返事はなくても大丈夫ですよ。','少し声をかけてみますね。気楽にどうぞ。'],
'presence':['ここでのんびり、様子を見ていましょう。','{cat_call}のペースで過ごせるといいですね。','急がずに、静かな時間を大切にしましょう。']}}
entries=[make_entry(condition('speech',person,mode,subjects=labels,target_cat='*'),lines) for person,modes in texts.items() for mode,lines in modes.items()]
for e in entries:
 assert store.get(e['condition']) is None, 'already published'
 assert drafts.get(e['condition']) is None, 'draft exists: review first'
 for v in e['variants']:
  for cat in ['ミケ','タマ']:expand(v['text'],cat,next(s['call'] for s in registry.read()['subjects'] if s['id']==e['condition']['subject_id']))
assert len(before['entries'])==239
out.mkdir(exist_ok=True)
(out/'before.json').write_text(json.dumps(before,ensure_ascii=False,indent=2),encoding='utf-8')
(out/'draft-before.json').write_text(json.dumps(olddraft,ensure_ascii=False,indent=2),encoding='utf-8')
expected={key(e['condition']):None for e in entries}
drafts.save_many(entries,expected=expected);store.save_many(entries,expected=expected)
after=store.read();assert all(e in after['entries'] for e in before['entries'])
assert len(after['entries'])==251
result={'before_conditions':len(before['entries']),'after_conditions':len(after['entries']),'old_entries_unchanged':True,'old_drafts_unchanged':all(e in drafts.read()['entries'] for e in olddraft['entries']),'new_entries':entries}
(out/'registration.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print('PASS 239/717 -> 251/753; 12 new conditions; old entries and drafts retained')
