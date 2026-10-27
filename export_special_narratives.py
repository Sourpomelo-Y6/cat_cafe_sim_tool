"""通常export_bundleを使用。取り込み待ちの高橋本文と制作中の猫画像は採用しない。"""
import json
import subprocess
from pathlib import Path
from content_images import ImageRegistry
from content_texts import TextStore
from interaction_subjects import SubjectRegistry
from interaction_texts import InteractionTextStore
from unity_export import export_bundle

ROOT=Path(__file__).resolve().parents[1]
UNITY=Path('C:/Users/hi-wa/unity_workspase/CatCafeSim')

def main():
 data=ROOT/'cat_cafe_sim/saves/content_images'
 pointer=json.loads((UNITY/'Assets/Resources/CatCafeContent/catalog.json').read_text(encoding='utf8'))
 active=UNITY/'Assets/Resources'/pointer['bundleResource']
 # The active checkout also contains concurrent image work. Freeze the committed
 # bundle and authoring identities, never their uncommitted replacements.
 original='Assets/Resources/CatCafeContent/Imports/51b88e2f3f974a83a197ff6a3cc5af8e/'
 bundle=json.loads(subprocess.check_output(['git','-C',str(UNITY),'show','f0f4dc7:'+original+'bundle.json']))
 previous=json.loads(subprocess.check_output(['git','-C',str(UNITY),'show','f0f4dc7:'+original+'interaction_texts.json']))
 active=UNITY/original/'bundle'
 class ActiveImages(ImageRegistry):
  def read(self):return dict(version=1,cats={c['cat_id']:{s['key']:s['image'] for s in c['scenes'] if s['image']} for c in bundle['cats']})
  def _resolve(self,value):
   p=(active.parent/value).resolve()
   if active.parent.resolve() not in p.parents:raise ValueError('Snapshot image outside active pack')
   return p
 allowed={r['subject_id'] for r in previous['cat_bindings']}|{p['id'] for p in previous['people']}
 class AdoptedSubjects(SubjectRegistry):
  def read(self):
   result=super().read();result['subjects']=[r for r in result['subjects'] if r['id'] in allowed];return result
 subjects=AdoptedSubjects(data/'interaction_subjects.json')
 class SelectedTexts(InteractionTextStore):
  def labels(self,*,include_disabled=True):return SubjectRegistry(data/'interaction_subjects.json').labels(include_disabled=include_disabled)
  def read(self):
   result=super().read()
   result['entries']=[e for e in result['entries'] if e['condition']['subject_id']!='person-guest-3']
   return result
 store=SelectedTexts(data/'interaction_texts.json',published=True,subject_registry=subjects)
 pack,report=export_bundle({c['cat_id']:c['name'] for c in bundle['cats']},ActiveImages(data),TextStore(data/'texts.json'),ROOT/'Logs/SpecialInteraction/packs',interaction_texts=store)
 exported=json.loads((pack/'interaction_texts.json').read_text(encoding='utf8'))
 for old in previous['entries']:
  current=next(e for e in exported['entries'] if e['pool_key']==old['pool_key'])
  assert all(current[k]==v for k,v in old.items()),old['pool_key']
 assert not [e for e in exported['entries'] if e['subject_id']=='person-guest-3']
 (ROOT/'Logs/SpecialInteraction/pack.txt').write_text(str(pack),encoding='utf8')
 (ROOT/'Logs/SpecialInteraction/export-scope.json').write_text(json.dumps(dict(source_bundle=str(active),excluded_subject='person-guest-3',special_fields=sum(e.get('narrative_scene')!='normal' for e in exported['entries']),version=exported['version'],entries=len(exported['entries']),images='existing active bundle snapshot through export_bundle',pack=str(pack)),ensure_ascii=False,indent=2),encoding='utf8')
 print(pack)

if __name__=='__main__':main()
