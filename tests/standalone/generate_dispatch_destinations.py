"""Original Python traces for multiple destinations; fixture trait/goal overrides are explicit."""
import copy,json,sys,tempfile
from pathlib import Path
from generate_dispatch_cases import expected
from generate_reference import ROOT,growth_answers
from export_standalone_rules import exported_rules
from cat_cafe_sim.cafe_new_game import create_game,starting_conditions
from cat_cafe_sim.core.cafe_traits import definitions

def unlock_boundaries():
 from types import SimpleNamespace
 from cat_cafe_sim.core.cafe_dispatch_unlocks import rules,update
 rows=[]
 for popularity in (119.999,120,149.999,150):
  for returns in (0,1,2,3):
   for stages in (1,2):
    core=SimpleNamespace(day=3,management=dict(popularity=popularity,game_over=None),activities=dict(events={str(i):dict(status='resolved') for i in range(returns)}),goal=dict(history=[dict(status='cleared',resolved_day=1)] if stages==2 else [],status='cleared',resolved_day=stages),dispatch_unlocks=dict(rules=rules(),unlocked={}),_emit=lambda *a,**kw:None)
    update(core);expected=[dict(id=key,**value) for key,value in core.dispatch_unlocks['unlocked'].items()]
    core.management['popularity']=1;update(core)
    rows.append(dict(popularity=popularity,returns=returns,stages=stages,expected=expected,after_drop=[dict(id=key,**value) for key,value in core.dispatch_unlocks['unlocked'].items()]))
 return rows

def generate():
 cases=[]
 targets={'out_of_town_visit':('cat-tama','outgoing'),'quiet_reading_salon':('cat-kohaku','relaxed'),'cat_exercise_class':('cat-mugi','hardy'),'cat_product_trial':('cat-mugi','hardworking'),'small_art_museum':('cat-mugi','easygoing')}
 for destination,(target,trait) in targets.items():
  for answer in ('first','second'):
   for parity in (0,1) if destination=='cat_product_trial' else (0,):
    with tempfile.TemporaryDirectory(dir=ROOT/'Logs/Standalone') as temp:
     mode='popularity' if destination=='small_art_museum' else 'free'
     selected=starting_conditions(mode);selected['store_events']['probability']=0;selected['management'].update(starting_funds=20000)
     rule=exported_rules();rule['starting_funds']=20000;rule['store_events']['probability']=0
     selected['traits'][target]=definitions()[trait]
     cat=next(c for c in rule['cats'] if c['id']==target);cat['trait_id']=trait
     cat.update({k:definitions()[trait].get(k,1 if k!='return_stress' else 0) for k in ('service_stress','service_fatigue','rest_fatigue','dispatch_reward','return_stress')})
     for u in selected['dispatch_unlocks'].values():
      if 'popularity' in u:u['popularity']=100
     for u in rule['dispatch_unlocks']:
      if u['stage']==0:u['popularity']=100
     if mode=='popularity':
      selected['goal'].update(target=101,stages=[dict(days=20,target=102),dict(days=25,target=650)])
      for r,value in zip(rule['goal_stages'],(101,102,650)):r['target']=value
     session=create_game(Path(temp),selected,seed=0);case=dict(variant=destination+'-'+answer+'-'+str(parity),rules=copy.deepcopy(rule),objective=mode,operations=[]);cases.append(case)
     destinations={r['id']:{k:v for k,v in r.items() if k not in ('item_reward','unsupported_reason')} for r in rule['dispatch_destinations']}
     def record(kind,cat_id=None,choice=None,source=None,working=None):
      before=expected(session);failed=False
      try:
       if kind=='dispatch':session.dispatch(cat_id,destinations[choice])
       elif kind=='resolve_activity':session.resolve_activity(source)
       elif kind=='resolve_dispatch_choice':session.resolve_dispatch_choice(source,choice)
       elif kind=='set_shifts':session.set_shifts(working)
       elif kind.startswith('resolve_growth'):getattr(session.core,kind)(cat_id,choice)
       elif kind=='resolve_intake':session.resolve_intake_request(choice)
       elif kind=='resolve_regular_introduction':session.resolve_regular_introduction(choice)
       elif kind=='resolve_customer_trust':session.core.resolve_customer_trust(source,choice)
       elif kind=='resolve_reservation':session.resolve_reservation(choice)
       elif kind=='resolve_missing':session.resolve_missing(source)
       elif kind=='sell_item':session.sell_item(source)
       elif kind=='use_item':session.use_item(source,cat_id)
       elif kind in ('day_off','next_day','continue_goal','advance_goal'):getattr(session,kind)()
       else:session.automatic_step()
      except ValueError:failed=True
      after=expected(session)
      if failed:assert before==after,(case['variant'],kind)
      case['operations'].append(dict(kind=kind,cat_id=cat_id,choice=choice,source=source,working=working,rejected=failed,expected=after))
     def events():
      growth_answers(session.core,record)
      if session.core.intake_request['status']=='waiting':record('resolve_intake',choice='decline')
      if session.core.regular_introduction['status']=='waiting':record('resolve_regular_introduction',choice='decline')
      for e in session.core.customer_trust['events'].values():
       if e['status']=='waiting':record('resolve_customer_trust',source=e['id'],choice='recover')
      for e in session.core.management['events'].values():
       if e['status']=='waiting':record('resolve_missing',cat_id=e['cat_id'],source=e['id'])
      if session.core.reservation and session.core.reservation.get('request') and session.core.reservation['request']['status']=='waiting':record('resolve_reservation',choice='decline')
      if session.core.goal and session.core.goal['status']!='active' and not session.core.goal.get('continued'):record('continue_goal')
     record('dispatch',cat_id=target,choice=destination) # Locked before any return.
     for i in range(3):
      record('dispatch',cat_id='cat-sora',choice='neighborhood_visit');eid=f'dispatch-{session.core.day}-cat-sora';record('day_off');record('resolve_activity',source=eid,choice='receive');events()
     if mode=='popularity':
      for stage in range(2):
       record('set_shifts',working=list(session.core.cats));record('start_business')
       while not session.core.closed:events();record('advance_business')
       events();record('advance_goal');record('next_day');events()
     if (session.core.day%2)!=parity and destination=='cat_product_trial':record('day_off');events()
     record('dispatch',cat_id='cat-sora',choice=destination) # Trait mismatch.
     record('dispatch',cat_id=target,choice=destination);eid=f'dispatch-{session.core.day}-{target}'
     record('resolve_dispatch_choice',source=eid,choice='invalid');record('resolve_dispatch_choice',source=eid,choice='accept')
     # A second destination travels concurrently; return of the 1-day trip cannot skip the other encounter.
     record('dispatch',cat_id='cat-sora',choice='neighborhood_visit');second=f'dispatch-{session.core.day}-cat-sora'
     record('day_off');record('day_off');record('resolve_activity',source=second,choice='receive')
     e=session.core.activities['events'][eid];choices=e['encounter']['rules']['choices'];choice=choices[0 if answer=='first' else 1]['id']
     record('resolve_dispatch_choice',source=eid,choice=choice);record('resolve_dispatch_choice',source=eid,choice=choice);record('resolve_dispatch_choice',source=eid,choice=choices[1 if answer=='first' else 0]['id']);events()
     while e['status']=='travelling':record('day_off');events()
     record('day_off');record('resolve_activity',source=eid,choice='receive');record('resolve_activity',source=eid,choice='receive');events()
     if 'item_reward' in e:record('sell_item',source=eid);record('sell_item',source=eid)
     assert e['status']=='resolved',case['variant']
 return dict(cases=cases,unlock_rules=exported_rules(),unlock_boundaries=unlock_boundaries())
if __name__=='__main__':
 Path(sys.argv[1]).write_text(json.dumps(generate(),ensure_ascii=False),encoding='utf-8');print(sys.argv[1])
