"""Export display-only standard profiles and labels without changing simulation rules."""
import json
import sys
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT / 'cat_cafe_sim'))
from cat_cafe_sim.core.cat_appearance import FIELDS, validate_appearance
from cat_cafe_sim.core.cafe_preferences import FEATURES
from cat_cafe_sim.cafe_customers import _NAMES, _EXTRA_NAMES, _SPECIAL_NAMES
from cat_cafe_sim.relationship_presentation import STAGES
from cat_cafe_sim.core.cafe_growth import LABELS, GROUP_LABELS
from cat_cafe_sim.unity_state_server import CONTENT_IDS

def exported_presentation():
    config=ROOT/'cat_cafe_sim/config'
    def read(name):return json.loads((config/(name+'.json')).read_text(encoding='utf-8'))
    rows=[(r['cat_id'],r) for r in read('cafe_new_game')['cats']]
    for name in ('cafe_intake_request','cafe_regular_introduction','cafe_visiting_cat'):
        row=read(name);rows.append((row['cat_id'],row))
    for name in ('cafe_dispatch_introduction','cafe_photo_introduction'):
        row=read(name);rows.append(('intro-'+row['destination'],row))
    for prefix,name in [('shop','cafe_pet_shop'),('catalog','cafe_recruitment')]:
        rows += [(f'{prefix}-{i}',r) for i,r in enumerate(read(name),1)]
    profiles=[]
    for key,row in rows:
        profiles.append(dict(id=key,name=row['name'],personality=row['preset'],appearance='\n'.join(label+'：'+(value or '未設定') for (field,label),value in zip(FIELDS.items(),validate_appearance(row.get('appearance',{})).values()))))
    assert len({r['id'] for r in profiles})==len(profiles),'Duplicate profile IDs'
    rules=json.loads((ROOT/'cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/rules.json').read_text(encoding='utf-8'))
    return dict(growth_labels=[dict(id=k,label=v) for k,v in {**LABELS,**GROUP_LABELS,**{r["id"]:r["name"] for r in read("human_cat_relationship")["types"]}}.items()],version=1,rules_id=rules['rules_id'],profiles=profiles,traits=[dict(id=k,label=v['name']) for k,v in read('cafe_traits').items()],features=[dict(id=k,label=v[0]) for k,v in FEATURES.items()],types=[dict(id=r['id'],label=r['name']) for r in read('human_cat_relationship')['types']],stages=list(STAGES),names=list(_NAMES),extra_names=list(_EXTRA_NAMES),special_names=[dict(id=k,label=v+'さん') for k,v in _SPECIAL_NAMES.items()],content_ids=[dict(id=k,label=v) for k,v in CONTENT_IDS.items()])
if __name__=='__main__':
    path=ROOT/'cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/presentation.json'
    path.write_bytes((json.dumps(exported_presentation(),ensure_ascii=False,indent=2)+'\n').encode('utf-8'))
    print(path)
