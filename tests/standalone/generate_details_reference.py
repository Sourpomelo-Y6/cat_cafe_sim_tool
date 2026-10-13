"""Read original Python saves without writing them; export display comparisons."""
import json
import sys
import tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
sys.path[:0]=[str(ROOT/'cat_cafe_sim_tool'),str(ROOT/'cat_cafe_sim')]
from convert_python_save import load_readonly, sha
from cat_cafe_sim.unity_state_server import state_view
from cat_cafe_sim.cafe_cat_details import cat_details
from cat_cafe_sim.cafe_customers import customer_name
source=Path(sys.argv[1]);output=Path(sys.argv[2]);rows=[]
for entry in json.loads(source.read_text(encoding='utf-8'))['cases']:
    bundle=Path(entry['bundle']);data=json.loads(bundle.read_text(encoding='utf-8'));session,_,_=load_readonly(data['source_path']);before={str(p):sha(p) for p in session.legacy_hashes}
    view=state_view(session)
    cats=[]
    for cat in view['cats']:
        details=cat_details(session,cat['cat_id']);cats.append(dict(id=cat['cat_id'],profile=cat['profile_details'],forecast=cat['shift_forecast'],content_id=cat['content_cat_id'],relationships=[dict(id=r[0],affinity=r[1],stage=r[2]) for r in details['relationships']],history=[dict(day=r[0],ticks=r[4]) for r in details['history']]))
    assert before=={str(p):sha(p) for p in session.legacy_hashes}
    rows.append(dict(bundle=str(bundle.resolve()),cats=cats))
ids=['',*('guest-'+str(i) for i in range(1,101)),'guest-999999999999999999999999999','guest-0','guest-01','guest-1x','advanced-white','expanded-guest','advanced-longhair','reservation-longhair','vip-calico','advanced-play','advanced-contact','advanced-quiet','独自のお客さん','emoji-🐈','guest-1\n']
output.parent.mkdir(parents=True,exist_ok=True)
from cat_cafe_sim.cafe_new_game import create_game, starting_conditions
forward=[];probes=[]
with tempfile.TemporaryDirectory(dir=output.parent) as folder:
    game=create_game(Path(folder)/'game',starting_conditions('free'),seed=0)
    while not game.core.closed:game.automatic_step()
    for mode in ('closed','next'):
        if mode=='next':game.next_day()
        forward.append(dict(mode=mode,cats=[dict(id=c['cat_id'],profile=c['profile_details'],forecast=c['shift_forecast'],history=[dict(day=r[0],count=r[3],ticks=r[4],spent=r[5],xp=r[8],delta=r[9]) for r in cat_details(game,c['cat_id'])['history']]) for c in state_view(game)['cats']]))
    for fatigue,stress,sick,remaining in [(0,0,False,0),(50,50,False,0),(70.25,99.5,False,0),(100,100,False,0),(80,20,True,1),(80,20,True,2)]:
        cat=game.core.cats['cat-mike'];cat.fatigue=fatigue;cat.health_status='sick' if sick else 'healthy';cat.recovery_days_remaining=remaining;game.core.management['stress']['cat-mike']=stress
        probes.append(dict(fatigue=fatigue,stress=stress,sick=sick,remaining=remaining,forecast=next(c['shift_forecast'] for c in state_view(game)['cats'] if c['cat_id']=='cat-mike')))
output.write_bytes(json.dumps(dict(cases=rows,forward=forward,probes=probes,names=[dict(id=id,name=customer_name(id)) for id in ids]),ensure_ascii=False).encode('utf-8'))
print('Python display references:',len(rows),'cases',sum(len(r['cats']) for r in rows),'cats',len(ids),'customer names')
