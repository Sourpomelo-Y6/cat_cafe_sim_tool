"""Original Python lodging dispatch traces. Fixture overrides are stored beside each trace."""
import copy,json,sys,tempfile
from pathlib import Path
from generate_reference import ROOT,growth_answers
from generate_dispatch_cases import expected
from export_standalone_rules import exported_rules
from cat_cafe_sim.cafe_new_game import create_game,starting_conditions
from cat_cafe_sim.core.cafe_dispatch_trouble import draw

def generate():
 cases=[]
 natural_hit=next(seed for seed in range(100) if draw(seed,'dispatch-1-cat-tama')<.15)
 natural_miss=next(seed for seed in range(100) if draw(seed,'dispatch-1-cat-tama')>=.15)
 for variant,chance,seed in [('forced',1,0),('never',0,0),('natural-hit',None,natural_hit),('natural-miss',None,natural_miss),('low-funds',1,0),('funds-end',1,0),('draw-equality',draw(0,'dispatch-1-cat-tama'),0)]:
  for answer in ('search','wait'):
   for closed in (False,True):
    with tempfile.TemporaryDirectory(dir=ROOT/'Logs/Standalone') as temp:
     rules=exported_rules();selected=starting_conditions('free');rules['store_events']['probability']=selected['store_events']['probability']=0
     funds=20000 if variant not in ('low-funds','funds-end') else 100+rules['base_cost']+2*rules['per_seat_cost'] if variant=='low-funds' else 1
     rules['starting_funds']=selected['management']['starting_funds']=funds
     if chance is not None:
      for row in (rules['dispatch_trouble'],selected['dispatch_trouble']):row.update(base_probability=chance,stress_probability=0)
     session=create_game(Path(temp),selected,seed=seed);case=dict(variant=variant+'-'+answer+'-'+str(closed),rules=copy.deepcopy(rules),objective='free',seed=seed,operations=[]);cases.append(case)
     destinations={r['id']:{k:v for k,v in r.items() if k not in ('item_reward','unsupported_reason')} for r in rules['dispatch_destinations']}
     def record(kind,cat_id=None,choice=None,source=None,working=None):
      before=expected(session);failed=False
      try:
       if kind=='dispatch':session.dispatch(cat_id,destinations[choice])
       elif kind=='resolve_activity':session.resolve_activity(source)
       elif kind=='resolve_dispatch_trouble':session.resolve_dispatch_trouble(source,choice)
       elif kind=='resolve_missing':session.resolve_missing(source)
       elif kind=='set_shifts':session.set_shifts(working)
       elif kind.startswith('resolve_growth'):getattr(session.core,kind)(cat_id,choice)
       elif kind=='resolve_intake':session.resolve_intake_request(choice)
       elif kind=='resolve_regular_introduction':session.resolve_regular_introduction(choice)
       elif kind in ('day_off','next_day','continue_goal'):getattr(session,kind)()
       else:session.automatic_step()
      except ValueError:failed=True
      after=expected(session)
      if failed:assert before==after,(case['variant'],kind)
      case['operations'].append(dict(kind=kind,cat_id=cat_id,choice=choice,source=source,working=working,rejected=failed,expected=after))
     def events():
      growth_answers(session.core,record)
      if session.core.intake_request['status']=='waiting':record('resolve_intake',choice='decline')
      if session.core.regular_introduction['status']=='waiting':record('resolve_regular_introduction',choice='decline')
     def close():
      if not closed:record('day_off');return
      record('set_shifts',working=[key for key in session.core.cats if session.core.activity(key)=='cafe']);record('start_business')
      while not session.core.closed:events();record('advance_business')
     record('dispatch',cat_id='cat-mike',choice='mountain_lodge_visit') # Wrong trait.
     record('dispatch',cat_id='cat-tama',choice='mountain_lodge_visit');eid='dispatch-1-cat-tama'
     record('resolve_dispatch_trouble',source=eid,choice=answer);record('resolve_activity',source=eid,choice='receive')
     if variant not in ('low-funds','funds-end'):record('dispatch',cat_id='cat-sora',choice='neighborhood_visit')
     close()
     if session.core.management['game_over']:
      record('resolve_dispatch_trouble',source=eid,choice=answer);record('resolve_activity',source=eid,choice='receive');continue
     if variant not in ('low-funds','funds-end'):record('resolve_activity',source='dispatch-1-cat-sora',choice='receive')
     trip=session.core.activities['events'][eid]
     if trip['trouble']['status']=='waiting':
      record('day_off');record('set_shifts',working=list(session.core.cats));record('resolve_activity',source=eid,choice='receive');record('resolve_missing',source=eid,cat_id='cat-tama');record('resolve_dispatch_trouble',source=eid,choice='invalid')
      record('resolve_dispatch_trouble',source=eid,choice=answer)
      if case['operations'][-1]['rejected']:record('resolve_dispatch_trouble',source=eid,choice='wait');chosen='wait'
      else:chosen=answer
      record('resolve_dispatch_trouble',source=eid,choice=chosen);record('resolve_dispatch_trouble',source=eid,choice='wait' if chosen=='search' else 'search')
     else:record('resolve_dispatch_trouble',source=eid,choice=answer)
     while session.core.activities['events'][eid]['status'] not in ('waiting','resolved') and not session.core.management['game_over']:
      events()
      if session.core.closed:record('next_day')
      events();close()
     record('resolve_activity',source=eid,choice='receive');record('resolve_activity',source=eid,choice='receive')
     if session.core.management['game_over']:continue
     events()
     if session.core.closed:record('next_day');events()
     # Returned cat can work again; interrupted dispatches yield no experience or reward.
     record('set_shifts',working=['cat-tama']);record('start_business')
     while not session.core.closed:events();record('advance_business')
     events();record('next_day');events()
 from types import SimpleNamespace
 from cat_cafe_sim.core.cafe_dispatch_trouble import rules as trouble_rules,probability
 probability_rules=exported_rules();probabilities=[]
 for stress in (0,1,20,50,80,99,100):
  core=SimpleNamespace(dispatch_trouble=trouble_rules(),management=dict(stress={'cat-tama':stress}));probabilities.append(dict(stress=stress,value=probability(core,'cat-tama')))
 return dict(cases=cases,probability_rules=probability_rules,probabilities=probabilities,draws=[dict(seed=seed,id=f'dispatch-{day}-cat-tama',value=draw(seed,f'dispatch-{day}-cat-tama')) for seed in (0,1,42,100) for day in (1,2,5)])
if __name__=='__main__':
 Path(sys.argv[1]).write_text(json.dumps(generate(),ensure_ascii=False),encoding='utf-8');print(sys.argv[1])
