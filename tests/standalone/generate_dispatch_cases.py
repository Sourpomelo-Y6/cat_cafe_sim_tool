"""Run original Python neighborhood dispatch operations, including items and frozen departure terms."""
import copy,json,sys,tempfile
from pathlib import Path
from generate_reference import ROOT,snapshot,growth_answers
from export_standalone_rules import exported_rules
from cat_cafe_sim.cafe_new_game import create_game,starting_conditions
from cat_cafe_sim.core.cafe_activities import reward

def expected(session):
 s=snapshot(session);c=session.core
 s['dispatch_income']=c.summary().get('dispatch_income',0)
 s['dispatches']=[]
 for e in (c.activities or {}).get('events',{}).values():
  w=e.get('welcome_match',{})
  s['dispatches'].append(dict(id=e['id'],cat_id=e['cat_id'],destination=e['destination']['id'],started_day=e['started_day'],remaining=e['remaining'],status=e['status'],occurred_day=e['occurred_day'] or 0,resolved_day=e['resolved_day'] or 0,choice=e['choice'],feature_matched=w.get('feature_matched',False),specialization_matched=w.get('specialization_matched',False),growth_multiplier=e.get('growth_multiplier',1),reward_bonus=w.get('reward_bonus',0),reward=reward(c,{k:v for k,v in e.items() if k!='encounter'}),return_stress=(c.traits or {}).get(e['cat_id'],{}).get('return_stress',0)))
 for row,e in zip(s['dispatches'],(c.activities or {}).get('events',{}).values()):
  v=e.get('encounter')
  if v:
   changes=v.get('changes') or {}
   row['encounter']=dict(id=v['rules']['id'],status=v['status'],choice=v['choice'],occurred_day=v['occurred_day'] or 0,resolved_day=v['resolved_day'] or 0,**{key:changes.get(key,0) for key in ('fatigue_before','fatigue_after','stress_before','stress_after')})
 s['dispatch_unlocks']=[dict(id=key,**value) for key,value in (c.dispatch_unlocks or {}).get('unlocked',{}).items()]
 if 'history' in s:
  for row,orig in zip(s['history'],c.day_results):row['dispatch_income']=orig['summary'].get('dispatch_income',0)
 return s

def generate():
 cases=[]
 for variant in ('normal','holiday','multi-day','dispatch-growth','funds-end','population','outgoing','goal-return'):
  with tempfile.TemporaryDirectory(dir=ROOT/'Logs/Standalone') as temp:
   mode='popularity' if variant=='goal-return' else 'free'
   selected=starting_conditions(mode);selected['store_events']['probability']=0
   selected['management']['starting_funds']=20000 if variant!='funds-end' else 1
   rule=exported_rules();rule['store_events']['probability']=0;rule['starting_funds']=selected['management']['starting_funds']
   if variant=='goal-return':selected['goal']['days']=1;rule['goal_stages'][0]['days']=1
   if variant=='dispatch-growth':selected['growth']['threshold']=1;rule['growth']['threshold']=1
   if variant=='multi-day':
    rule['dispatch']['days']=2
    rule['dispatch_destinations'][0]['days']=2
   session=create_game(Path(temp),selected,seed=0);case=dict(variant=variant,rules=copy.deepcopy(rule),objective=mode,operations=[]);cases.append(case)
   destination=copy.deepcopy(rule['dispatch']);destination.pop('item_reward')
   def record(kind,cat_id=None,choice=None,source=None,working=None):
    before=expected(session);failed=False
    try:
     if kind=='dispatch':session.dispatch(cat_id,destination)
     elif kind=='resolve_activity':session.resolve_activity(source)
     elif kind=='set_shifts':session.set_shifts(working)
     elif kind.startswith('resolve_growth'):getattr(session.core,kind)(cat_id,choice)
     elif kind=='resolve_intake':session.resolve_intake_request(choice)
     elif kind=='resolve_regular_introduction':session.resolve_regular_introduction(choice)
     elif kind=='resolve_missing':session.resolve_missing(source)
     elif kind=='resolve_customer_trust':session.core.resolve_customer_trust(source,choice)
     elif kind=='use_item':session.use_item(source,cat_id)
     elif kind=='sell_item':session.sell_item(source)
     elif kind in ('next_day','day_off','expand_seats','continue_goal'):getattr(session,kind)()
     else:session.automatic_step()
    except ValueError:failed=True
    after=expected(session)
    if failed:assert before==after,(variant,kind)
    case['operations'].append(dict(kind=kind,cat_id=cat_id,choice=choice,source=source,working=working,seat_id=None,rejected=failed,expected=after))
   def events():
    for e in list(session.core.management['events'].values()):
     if e['status']=='waiting':record('resolve_missing',cat_id=e['cat_id'],source=e['id'])
    for e in list(session.core.customer_trust['events'].values()):
     if e['status']=='waiting':record('resolve_customer_trust',source=e['id'],choice='recover')
    def answers(core,callback):
     growth_answers(core,lambda kind,**kw:callback(kind,**dict(kw,choice='dispatch') if kind=='resolve_growth' else kw))
    answers(session.core,record)
    if session.core.intake_request['status']=='waiting':record('resolve_intake',choice='decline')
    if session.core.regular_introduction['status']=='waiting':record('resolve_regular_introduction',choice='decline')
   target='cat-tama' if variant=='outgoing' else 'cat-kohaku';day_limit=1 if variant=='goal-return' else 4 if variant=='dispatch-growth' else 3
   record('dispatch',cat_id='unknown',choice=destination['id']);record('resolve_activity',source='unknown',choice='receive')
   for day in range(1,day_limit+1):
    events()
    if variant=='population':
     for id in ('cat-kohaku','cat-sora','cat-mike','cat-tama'):record('dispatch',cat_id=id,choice=destination['id'])
    else:record('dispatch',cat_id=target,choice=destination['id'])
    id=f'dispatch-{session.core.day}-{target}'
    if any(e['id']==id for e in (session.core.activities or {}).get('events',{}).values()):
     record('dispatch',cat_id=target,choice=destination['id']);record('resolve_activity',source=id,choice='receive')
    working=[id for id in session.core.cats if session.core.activity(id)=='cafe' and session.core.cats[id].health_status=='healthy']
    record('set_shifts',working=working+([target] if target not in working else []));record('set_shifts',working=working)
    if variant in ('holiday','funds-end','dispatch-growth','goal-return'):record('day_off')
    else:
     record('start_business');record('dispatch',cat_id='cat-mike',choice=destination['id'])
     while not session.core.closed:events();record('advance_business')
    pending=[e for e in (session.core.activities or {}).get('events',{}).values() if e['status']=='waiting']
    if pending:
     if variant=='goal-return':record('continue_goal')
     record('next_day');record('day_off');record('dispatch',cat_id='cat-mike',choice=destination['id'])
     for e in pending:
      record('resolve_activity',source=e['id'],choice='receive');record('resolve_activity',source=e['id'],choice='receive')
    if variant=='goal-return':record('continue_goal');record('continue_goal')
    if session.core.management['game_over']:break
    events()
    if session.core.closed:record('next_day')
    from cat_cafe_sim.core.cafe_items import inventory as get_inventory
    inventory=get_inventory(session.core)
    if inventory:
     source=next(iter(inventory))
     if variant in ('holiday','outgoing'):record('use_item',source=source,cat_id=target)
     else:record('sell_item',source=source)
     record('sell_item',source=source)
   if variant=='dispatch-growth':assert any(e.get('growth_multiplier')==rule['growth']['dispatch_reward_multiplier'] for e in session.core.activities['events'].values())
 return dict(cases=cases)
if __name__=='__main__':
 Path(sys.argv[1]).write_text(json.dumps(generate(),ensure_ascii=False),encoding='utf-8');print(sys.argv[1])
