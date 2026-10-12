"""Compare native Python replay actions/reactions and gauge transitions."""
import sys,json,tempfile,copy
from pathlib import Path
from generate_reference import ROOT
from generate_patron_cases import expected as patron_expected
from generate_expansion_cases import expected as goal_expected
from export_standalone_rules import exported_rules
from cat_cafe_sim.cafe_new_game import create_game,starting_conditions
from cat_cafe_sim.core.cafe_interaction import verify_cafe_interaction

def generate():
 cases=[]
 for mode in ('free','popularity','bond','patron'):
  with tempfile.TemporaryDirectory(dir=ROOT/'Logs/Standalone') as temp:
   selected=starting_conditions(mode);rules=exported_rules();selected['store_events']['probability']=rules['store_events']['probability']=0;selected['growth']['threshold']=rules['growth']['threshold']=1000
   s=create_game(Path(temp),selected,seed=42);ops=[]
   for _ in range(rules['opening_ticks']):
    before=len(s.core.operations);kind='start_business' if s.core.can_set_shifts else 'advance_business';s.automatic_step();c=s.core
    audit=[]
    for row in c.operations[before:]:
     for event in row['events']:
      if event['kind']=='human_cat_action':
       record=event['record'];audit.append({k:record[k] for k in ('action','reaction','target_type','before','after')})
    expected=patron_expected(s);expected.update(goal_expected(s))
    if c.goal.get('tracking_only'):expected['goal']=[]
    ops.append(dict(kind=kind,working=None,cat_id=None,choice=None,source=None,rejected=False,expected=expected,audit=audit))
   verify_cafe_interaction(s.core.log())
   cases.append(dict(variant='audit-'+mode,rules=rules,objective=mode,seed=42,operations=ops))
 return dict(cases=cases)
if __name__=='__main__':Path(sys.argv[1]).write_text(json.dumps(generate(),ensure_ascii=False),encoding='utf-8')
