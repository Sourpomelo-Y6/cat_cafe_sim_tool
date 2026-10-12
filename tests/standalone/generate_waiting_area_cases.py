"""Run Python waiting-area operations, crowding, deadlines and daily finance."""
import copy,json,sys,tempfile
from pathlib import Path
from dataclasses import replace
from generate_reference import ROOT,growth_answers
from generate_patron_cases import expected as patron_expected
from generate_expansion_cases import expected as goal_expected
from export_standalone_rules import exported_rules
from cat_cafe_sim.cafe_new_game import create_game,starting_conditions
from cat_cafe_sim.core import cafe_goal,cafe_waiting_area as area,cafe_operating_cost,cafe_popularity_challenge

def generate():
 cases=[]
 specs=[(m,'normal') for m in ('free','bond','patron','popularity')]+[(m,'challenge') for m in ('free','bond','patron')]+[('popularity',v) for v in ('dense-none','dense-purchase','dense-upgrade','one-cat','closing','same-day','day-off','combined','funds-end','expired')]
 for mode,variant in specs:
  with tempfile.TemporaryDirectory(dir=ROOT/'Logs/Standalone') as temp:
   rules=exported_rules();selected=starting_conditions(mode)
   rules['starting_funds']=selected['management']['starting_funds']=801 if variant=='funds-end' else 20000
   rules['store_events']['probability']=selected['store_events']['probability']=0
   rules['growth']['threshold']=selected['growth']['threshold']=1000
   rules['stress_per_service_tick']=selected['management']['stress_per_service_tick']=0;rules['max_probability']=0
   rules['goal_stages']=[dict(days=1 if variant=='expired' else 10,target=k) for k in ((200,250,300) if variant=='expired' else (101,102,103))]
   if mode=='popularity':selected['goal'].update(rules['goal_stages'][0],stages=rules['goal_stages'][1:])
   dense=variant.startswith('dense') or variant in ('one-cat','closing')
   if dense:
    rules['arrival_ticks']=[0]*8;rules['patterns']=[dict(days=list(range(7)))]*7;rules['exchange']['ticks']=10
   if variant=='one-cat':
    rules['preference_pool']=selected['preferences']['pool']=['brown_tabby'];rules['satisfaction']['satisfied_score']=selected['customer_satisfaction']['satisfied_score']=1;rules['satisfaction']['dissatisfied_score']=selected['customer_satisfaction']['dissatisfied_score']=0
   if variant=='closing':rules['opening_ticks']=12
   session=create_game(Path(temp),selected,seed=42);c=session.core;c.health_rules=replace(c.health_rules,max_probability=0)
   if dense:
    c.config=replace(c.config,arrival_ticks=(0,)*8,opening_ticks=rules['opening_ticks']);c.weekdays['patterns']=[list(range(7))]*7;session.interaction_config=replace(session.interaction_config,ticks=10)
   case=dict(variant=f'waiting-{mode}-{variant}',rules=copy.deepcopy(rules),objective=mode,seed=42,operations=[]);cases.append(case)
   def expected():
    s=patron_expected(session);s.update(goal_expected(session));s['challenge_started_day']=c.goal.get('challenge_started_day',0)
    if c.goal.get('tracking_only'):s['goal']=[]
    data=c.waiting_area;s['waiting_area']=dict(purchased_day=(data.get('purchase') or {}).get('day',0),upgraded_day=data.get('upgrade',{}).get('day',0))
    s['waiting_area_expenses']=area.expenses(c,c.day);s['waiting_area_cost']=area.daily_cost(c) if c.closed else 0
    data=c.rest_space or {};s['rest_space']=dict(purchased_day=data.get('day',0),upgraded_day=data.get('upgrade',{}).get('day',0),soundproofed_day=data.get('soundproof',{}).get('day',0));s['rest_space_expenses']=c.summary().get('equipment_expenses',0)
    for row,original in zip(s['history'],c.day_results):
     row.update(waiting_area_expenses=original['summary'].get('waiting_area_expenses',0),waiting_area_cost=area.daily_cost(c,original['day']),rest_space_expenses=original['summary'].get('equipment_expenses',0))
    return s
   def limits():return dict(capacity=area.queue_capacity(c),wait=area.max_wait_ticks(c),daily=area.daily_cost(c),operating=cafe_operating_cost.estimate(c))
   def record(kind,working=None,cat_id=None,choice=None,source=None):
    nonlocal c
    if len(case['operations'])>1200:raise RuntimeError((variant,kind,c.day,c.tick))
    before=expected();failed=False;boundaries=[]
    if kind in ('purchase_waiting_area','upgrade_waiting_area'):
     config=c.waiting_area['rules'] if kind=='purchase_waiting_area' else area.upgrade_rules();original=c.funds
     for amount in (config['cost']-1,config['cost'],config['cost']+1):
      c.funds=amount;boundaries.append(dict(funds=amount,can_execute=not(area.reason(c) if kind=='purchase_waiting_area' else area.upgrade_reason(c,config))))
     c.funds=original
    try:
     if kind=='set_shifts':session.set_shifts(working)
     elif kind=='start_popularity_challenge':
      chosen=cafe_popularity_challenge.rules(c);chosen['goal'].update(rules['goal_stages'][0],stages=rules['goal_stages'][1:]);session.start_popularity_challenge(chosen)
     elif kind.startswith('resolve_growth'):getattr(c,kind)(cat_id,choice)
     elif kind=='resolve_missing':session.resolve_missing(source)
     elif kind=='resolve_customer_trust':c.resolve_customer_trust(source,choice)
     elif kind=='resolve_intake':session.resolve_intake_request(choice)
     elif kind=='resolve_regular_introduction':session.resolve_regular_introduction(choice)
     elif kind=='resolve_reservation':session.resolve_reservation(choice)
     elif kind=='next_day':session.next_day()
     elif kind in ('start_business','advance_business'):session.automatic_step()
     else:getattr(session,kind)()
    except ValueError:failed=True
    c=session.core;case['operations'].append(dict(kind=kind,working=working,cat_id=cat_id,choice=choice,source=source,rejected=failed,waiting_boundaries=boundaries,waiting_limits=limits(),expected=expected()))
    if failed:assert before==expected(),kind
    return not failed
   def events():
    for e in list(c.management['events'].values()):
     if e['status']=='waiting':record('resolve_missing',source=e['id'],cat_id=e['cat_id'])
    for e in list(c.customer_trust['events'].values()):
     if e['status']=='waiting':record('resolve_customer_trust',source=e['id'],choice='recover')
    growth_answers(c,record)
    if c.intake_request['status']=='waiting':record('resolve_intake',choice='decline')
    if c.regular_introduction['status']=='waiting':record('resolve_regular_introduction',choice='decline')
    if c.reservation and c.reservation['request'] and c.reservation['request']['status']=='waiting':record('resolve_reservation',choice='decline')
    if cafe_goal.pending(c):
     record('purchase_waiting_area');record('upgrade_waiting_area');record('advance_goal' if cafe_goal.next_rules(c.goal) else 'continue_goal')
   record('purchase_waiting_area');record('upgrade_waiting_area')
   if variant=='challenge':record('start_popularity_challenge')
   for day in range(20 if variant=="funds-end" else 5):
    if c.management['game_over']:break
    if day>0:
     if c.closed:record('next_day')
     events()
    if variant!='dense-none' and not(variant=='same-day' and day<2):record('purchase_waiting_area')
    if variant!='dense-purchase':record('upgrade_waiting_area')
    if variant=='combined' and day==2:record('purchase_housing');record('upgrade_housing');record('purchase_rest_space');record('upgrade_rest_space');record('soundproof_rest_space');record('expand_seats')
    if variant in ('day-off','funds-end') and day>0:
     record('day_off');events()
     if c.closed:break
     # day_off itself enters the next preparation; avoid advancing twice.
     continue
    record('set_shifts',working=['cat-mugi'] if variant=='one-cat' else list(c.cats))
    record('start_business');record('purchase_waiting_area');record('upgrade_waiting_area')
    while not c.closed:record('advance_business');events()
    events();record('purchase_waiting_area');record('upgrade_waiting_area')
   if variant in ('dense-upgrade','one-cat','same-day','combined'):assert area.purchased(c) and 'upgrade' in c.waiting_area
   if variant=='funds-end':assert c.management['game_over']['reason']=='funds'
   if dense:assert any(o['expected']['visits'] and any(v['departure']=='queue_full' for v in o['expected']['visits']) for o in case['operations'])
 return dict(cases=cases)

if __name__=='__main__':
 data=generate();Path(sys.argv[1]).write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8');print(len(data['cases']),'waiting area scenarios')
