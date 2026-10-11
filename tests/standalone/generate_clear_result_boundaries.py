"""Short original-Python terminal/goal deadline traces, with explicit fixture overrides."""
import copy,json,sys,tempfile
from pathlib import Path
from generate_reference import ROOT,growth_answers
from generate_expansion_cases import expected
from export_standalone_rules import exported_rules
from cat_cafe_sim.cafe_new_game import create_game,starting_conditions

def generate():
 cases=[]
 for variant in ('funds','popularity','expired','deadline-cleared'):
  with tempfile.TemporaryDirectory(dir=ROOT/'Logs/Standalone') as temp:
   selected=starting_conditions('popularity');selected['store_events']['probability']=0
   rule=copy.deepcopy(exported_rules());rule['store_events']['probability']=0
   if variant=='funds':selected['management']['starting_funds']=1;rule['starting_funds']=1
   if variant=='popularity':
    for k,v in dict(starting_popularity=1,runaway_threshold=1,return_stress=0,popularity_loss=100,stress_per_service_tick=3).items():selected['management'][k]=v;rule[k]=v
   if variant in ('expired','deadline-cleared'):
    selected['goal']['days']=1;rule['goal_stages'][0]['days']=1
    target=600 if variant=='expired' else 101
    if variant=='expired':
     selected['goal']['stages']=[dict(days=20,target=625),dict(days=25,target=650)];rule['goal_stages'][1:]=copy.deepcopy(selected['goal']['stages'])
    selected['goal']['target']=target;rule['goal_stages'][0]['target']=target
   session=create_game(Path(temp),selected,seed=0);case=dict(variant=variant,rules=rule,objective='popularity',operations=[]);cases.append(case)
   def record(kind,cat_id=None,choice=None,working=None):
    before=expected(session);failed=False
    try:
     if kind=='set_shifts':session.set_shifts(working)
     elif kind.startswith('resolve_growth'):getattr(session.core,kind)(cat_id,choice)
     elif kind in ('next_day','day_off','continue_goal','advance_goal'):getattr(session,kind)()
     else:session.automatic_step()
    except ValueError:failed=True
    row=expected(session)
    if failed:assert before==row
    case['operations'].append(dict(kind=kind,cat_id=cat_id,choice=choice,working=working,source=None,seat_id=None,rejected=failed,expected=row))
   if variant=='funds':record('day_off')
   else:
    record('set_shifts',working=list(session.core.cats));record('start_business')
    while not session.core.closed:growth_answers(session.core,record);record('advance_business')
   core=session.core
   if variant in ('funds','popularity'):
    assert core.management['game_over']['reason']==variant
    for kind in ('next_day','continue_goal','start_business','advance_goal'):record(kind)
   else:
    assert core.goal['status']==('expired' if variant=='expired' else 'cleared')
    record('next_day');record('continue_goal');record('continue_goal');record('next_day')
   assert not core.clear_results # Only stage one, including a deadline-day success.
 return dict(cases=cases)
if __name__=='__main__':
 Path(sys.argv[1]).write_text(json.dumps(generate(),ensure_ascii=False),encoding='utf-8');print(sys.argv[1])
