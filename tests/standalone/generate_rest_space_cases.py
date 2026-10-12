"""Run original Python rest equipment and day-end recovery without changing production rules."""
import copy,json,sys,tempfile
from pathlib import Path
from dataclasses import replace
from generate_reference import ROOT,growth_answers
from generate_patron_cases import expected as patron_expected
from generate_expansion_cases import expected as goal_expected
from export_standalone_rules import exported_rules
from cat_cafe_sim.cafe_new_game import create_game,starting_conditions
from cat_cafe_sim.core import cafe_goal,cafe_equipment

def generate():
 cases=[]
 specs=[(m,20000,False) for m in ('free','popularity','bond','patron')]+[('free',f,False) for f in (400,401,20001,20002,20003)]+[('popularity',20000,True),('popularity',1200,True),('popularity',1201,True),('popularity',2200,True),('popularity',2201,True),('popularity',2202,True)]
 for mode,funds,accelerated in specs:
  with tempfile.TemporaryDirectory(dir=ROOT/'Logs/Standalone') as temp:
   rules=exported_rules();selected=starting_conditions(mode)
   rules['starting_funds']=selected['management']['starting_funds']=funds
   rules['store_events']['probability']=selected['store_events']['probability']=0
   rules['growth']['threshold']=selected['growth']['threshold']=1 if funds==20003 else 1000
   if accelerated:
    rules['goal_stages']=[dict(days=10,target=k) for k in (101,102,103)];selected['goal'].update(rules['goal_stages'][0],stages=rules['goal_stages'][1:])
   if funds!=20001:rules["max_probability"]=0
   session=create_game(Path(temp),selected,seed=42);c=session.core;c.health_rules=replace(c.health_rules,max_probability=rules["max_probability"])
   case=dict(variant=f'rest-{mode}-{funds}-{accelerated}',rules=copy.deepcopy(rules),objective=mode,seed=42,operations=[]);cases.append(case)
   def expected():
    s=patron_expected(session);s.update(goal_expected(session));data=c.rest_space or {}
    s['rest_space']=dict(purchased_day=data.get('day',0),upgraded_day=data.get('upgrade',{}).get('day',0),soundproofed_day=data.get('soundproof',{}).get('day',0))
    s['rest_space_expenses']=cafe_equipment.expenses(c,c.day)
    for row,original in zip(s['history'],c.day_results):row['rest_space_expenses']=original['summary'].get('equipment_expenses',0)
    return s
   def record(kind,working=None,cat_id=None,choice=None,source=None):
    nonlocal c
    if len(case["operations"])>1500:raise RuntimeError((mode,funds,kind,c.day,c.tick,c.summary()))
    before=expected();failed=False;boundaries=[]
    if kind in ('purchase_rest_space','upgrade_rest_space','soundproof_rest_space'):
     rule,reason={'purchase_rest_space':(cafe_equipment.rules,cafe_equipment.reason),'upgrade_rest_space':(cafe_equipment.upgrade_rules,cafe_equipment.upgrade_reason),'soundproof_rest_space':(cafe_equipment.soundproof_rules,cafe_equipment.soundproof_reason)}[kind]
     config=rule();original=c.funds
     for amount in (config['cost']-1,config['cost'],config['cost']+1):
      c.funds=amount;boundaries.append(dict(funds=amount,can_execute=not reason(c,config)))
     c.funds=original
    try:
     if kind=='dispatch':session.dispatch(cat_id,json.loads((ROOT/'cat_cafe_sim/config/cafe_dispatch.json').read_text(encoding='utf-8')))
     elif kind=='resolve_activity':session.resolve_activity(source)
     elif kind=='set_shifts':session.set_shifts(working)
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
    c=session.core
    case['operations'].append(dict(kind=kind,working=working,cat_id=cat_id,choice=choice,source=source,rejected=failed,rest_boundaries=boundaries,expected=expected()))
    if failed:assert before==expected(),kind
    return not failed
   def events():
    for e in list((c.activities or {}).get("events",{}).values()):
     if e["status"]=="waiting":record("resolve_activity",source=e["id"],choice="receive")
    for e in list(c.management['events'].values()):
     if e['status']=='waiting':record('resolve_missing',source=e['id'],cat_id=e['cat_id'])
    for e in list(c.customer_trust['events'].values()):
     if e['status']=='waiting':record('resolve_customer_trust',source=e['id'],choice='recover')
    growth_answers(c,record,'rest')
    if c.intake_request['status']=='waiting':record('resolve_intake',choice='decline')
    if c.regular_introduction['status']=='waiting':record('resolve_regular_introduction',choice='decline')
    if c.reservation and c.reservation['request'] and c.reservation['request']['status']=='waiting':record('resolve_reservation',choice='decline')
    if cafe_goal.pending(c):record('purchase_rest_space');record('upgrade_rest_space');record('soundproof_rest_space');record('advance_goal' if cafe_goal.next_rules(c.goal) else 'continue_goal')
   record('upgrade_rest_space');record('soundproof_rest_space');record('purchase_rest_space');record('purchase_rest_space');record('upgrade_rest_space');record('soundproof_rest_space')
   if funds==20002:record('dispatch',cat_id='cat-sora',choice='neighborhood_visit')
   for day in range(6 if accelerated else 4):
    if c.management['game_over']:break
    if day>0:record('next_day');events()
    if accelerated:
     if funds!=2202 or (c.rest_space or {}).get('soundproof'):record('upgrade_rest_space')
     record('soundproof_rest_space')
     if funds==2202 and (c.rest_space or {}).get('soundproof'):record('upgrade_rest_space')
    # Working days accumulate real fatigue/stress; alternating rest days reveal bonus and clamp behavior.
    available=[k for k in c.cats if c.activity(k)=='cafe' and c.cats[k].health_status=='healthy']
    record('set_shifts',working=available[:1] if day%2 else available[:3])
    if not available:record('day_off');events();continue
    record('start_business');record('purchase_rest_space');record('upgrade_rest_space');record('soundproof_rest_space')
    while not c.closed:record('advance_business');events()
    events();record('purchase_rest_space');record('upgrade_rest_space');record('soundproof_rest_space')
   if c.rest_space:assert expected()['rest_space']['purchased_day']==1
 return dict(cases=cases)

if __name__=='__main__':
 data=generate();Path(sys.argv[1]).write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8');print(len(data['cases']),'rest space scenarios')
