"""Exercise original growth functions independently of business/balance fixtures."""
import json
import sys
from pathlib import Path
from types import SimpleNamespace
from generate_reference import growth_row, GROWTH_TYPES
from cat_cafe_sim.core import cafe_growth as g


def generate():
    cases=[]
    for specialization in ('service','rest','dispatch'):
        core=SimpleNamespace(day=1, growth=dict(rules=g.rules(),cats={'cat-kohaku':g.blank(g.rules())}),
                             cats={'cat-kohaku':SimpleNamespace(stamina=100)},config=SimpleNamespace(max_stamina=100),
                             require_running=lambda:None, require_events_resolved=lambda:None, _emit=lambda *a,**k:None, _record=lambda *a,**k:None,
                             activity=lambda id:'cafe',working_cats=set())
        case=dict(specialization=specialization,operations=[]);cases.append(case)
        def record(kind, choice=None, actions=None, affinity=0):
            op=dict(kind=kind,cat_id='cat-kohaku',day=core.day,tick=0,actions=[(actions or {}).get(t,0) for t in GROWTH_TYPES],affinity_delta=affinity,stamina_spent=0,choice=choice)
            if kind=='service':g.service(core,dict(cat_id='cat-kohaku',type_actions=actions,affinity_delta=affinity,stamina_spent=0))
            elif kind=='rest':g.rest_day(core)
            else:getattr(g,kind.replace('resolve_growth_type_mastery','resolve_type_mastery').replace('resolve_growth_mastery','resolve_mastery').replace('resolve_growth','resolve'))(core,'cat-kohaku',choice) if kind.startswith('resolve') else getattr(g,kind)(core,'cat-kohaku')
            case['operations'].append(dict(operation=op,expected=dict(cat_id='cat-kohaku',**growth_row(core.growth['cats']['cat-kohaku']))))
        def resolve():
            while True:
                if g.pending(core):record('resolve_growth',specialization);continue
                if g.mastery_pending(core):record('resolve_growth_mastery',g.mastery_choices(core,'cat-kohaku')[0]);continue
                if g.type_mastery_pending(core):record('resolve_growth_type_mastery',g.type_mastery_choices(core,'cat-kohaku')[0]);continue
                break
        for i in range(50):
            core.day=i+1
            record('rest')
            record('service',actions={t:5 for t in GROWTH_TYPES},affinity=0 if i%9==0 else -1 if i%13==0 else 1)
            resolve()
            row=core.growth['cats']['cat-kohaku']
            if row.get('second_mastery') and 'second_group_type_practice' not in row:record('begin_second_group_type_practice')
            if row.get('second_group_type_practice',{}).get('type_mastery') and 'second' not in row['second_group_type_practice']:record('begin_second_group_second_type_practice')
            if row.get('second_mastery') and 'third_group_practice' not in row:record('begin_third_group_practice')
            if row.get('third_group_practice',{}).get('mastery') and 'individual' not in row['third_group_practice']:record('begin_third_group_type_practice')
            if row.get('third_group_practice',{}).get('individual',{}).get('type_mastery') and 'second' not in row['third_group_practice']['individual']:record('begin_third_group_second_type_practice')
        if specialization=='service':assert len(g.learned_groups(row))==3 and len(g.learned_types(row))==6,row
    return dict(cases=cases)


if __name__=='__main__':
    target=Path(sys.argv[1]);target.write_text(json.dumps(generate(),ensure_ascii=False),encoding='utf-8');print(target)
