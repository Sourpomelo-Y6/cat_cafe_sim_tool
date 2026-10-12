"""Python manual assignment and automatic selection traces, using real starts."""
import copy,json,sys,tempfile
from pathlib import Path
from dataclasses import replace
from generate_reference import ROOT,growth_answers
from generate_patron_cases import expected as patron_expected
from generate_expansion_cases import expected as goal_expected
from export_standalone_rules import exported_rules
from cat_cafe_sim.cafe_new_game import create_game,starting_conditions
from cat_cafe_sim.core import cafe_goal

def generate():
 cases=[]
 specs=[(m,'manual') for m in ('free','popularity','bond','patron')]+[('free',v) for v in ('automatic','toggle','nonfront','single-cat','resting','dispatch','growth','crowded','closing','normal-health','adoption','pending-goal','store-event')]+[('popularity','equipment')]
 for mode,variant in specs:
  with tempfile.TemporaryDirectory(dir=ROOT/'Logs/Standalone') as temp:
   rules=exported_rules();selected=starting_conditions(mode);auto=True
   rules['starting_funds']=selected['management']['starting_funds']=20000;rules['store_events']['probability']=selected['store_events']['probability']=1 if variant=='store-event' else 0;rules['growth']['threshold']=selected['growth']['threshold']=1 if variant=='growth' else 1000
   rules['arrival_ticks']=[0]* (8 if variant=='crowded' else 3);rules['patterns']=[dict(days=list(range(7)))]*7
   rules['opening_ticks']=12 if variant=='closing' else 40
   if variant!='normal-health':rules['max_probability']=0
   if variant=='pending-goal':
    mode='popularity';rules['goal_stages']=[dict(days=10,target=k) for k in (101,102,103)];selected=starting_conditions(mode);selected['management']['starting_funds']=20000;selected['store_events']['probability']=0;selected['growth']['threshold']=1000;selected['goal'].update(rules['goal_stages'][0],stages=rules['goal_stages'][1:])
   if variant=='adoption':rules['exchange']['affinity_favorable']=rules['exchange']['affinity_enthusiastic']=80
   session=create_game(Path(temp),selected,seed=42);c=session.core;c.config=replace(c.config,arrival_ticks=tuple(rules['arrival_ticks']),opening_ticks=rules['opening_ticks']);c.weekdays['patterns']=[list(range(7))]*7;c.health_rules=replace(c.health_rules,max_probability=rules['max_probability'])
   if variant=='adoption':session.interaction_config=replace(session.interaction_config,affinity_favorable=80,affinity_enthusiastic=80)
   case=dict(variant=f'assignment-{mode}-{variant}',rules=copy.deepcopy(rules),objective=mode,seed=42,operations=[]);cases.append(case)
   def expected():
    s=patron_expected(session);s.update(goal_expected(session));s['auto_assignment']=auto
    if variant=='adoption':
     data=c.adoption;s['adoption']=None if data is None else dict(id='adoption',enabled=data['enabled'],events=[dict(e,source='service-'+str(e['day'])+'-'+e['cat_id']+'-'+e['customer_id'],resolved_day=e['resolved_day'] or 0,resolved_tick=e['resolved_tick'] or 0) for e in data['events'].values()])
    return s
   def assignment_projection():
    from cat_cafe_sim.unity_state_server import service_assignment_view
    v=service_assignment_view(session,auto)
    return dict(waiting=v['waiting'],can_assign=v['can_assign'],available_cats=[r['cat_id'] for r in v['cats'] if r['available']],free_seats=[r['seat_id'] for r in v['seats'] if r['available']])
   def record(kind,working=None,cat_id=None,choice=None,source=None,seat_id=None,customer_id=None):
    nonlocal c,auto
    if len(case['operations'])>1800:raise RuntimeError((variant,kind,c.day,c.tick))
    before=expected();failed=False
    try:
     if kind=='set_auto_assignment':
      c.require_running()
      if choice not in ('automatic','manual'):raise ValueError()
      auto=choice=='automatic'
     elif kind=='assign_service':
      if auto or c.can_set_shifts or not customer_id or not cat_id or not seat_id:raise ValueError()
      # Python HTTP executes rejected starts on a candidate; preserve that atomicity.
      from cat_cafe_sim.core.cafe_auto_assignment import available_cats
      if customer_id not in c.queue or cat_id not in {r.id for r in available_cats(c)} or seat_id not in session.free_seats:raise ValueError()
      session.start(customer_id,cat_id,seat_id)
     elif kind=='configure_adoption':session.configure_adoption(choice=='on')
     elif kind=='resolve_adoption':session.resolve_adoption(source,choice)
     elif kind=='resolve_store_event':session.resolve_store_event(choice)
     elif kind=='dispatch':session.dispatch(cat_id,json.loads((ROOT/'cat_cafe_sim/config/cafe_dispatch.json').read_text(encoding='utf-8')))
     elif kind=='resolve_activity':session.resolve_activity(source)
     elif kind=='set_shifts':session.set_shifts(working)
     elif kind.startswith('resolve_growth'):getattr(c,kind)(cat_id,choice)
     elif kind=='resolve_missing':session.resolve_missing(source)
     elif kind=='resolve_customer_trust':c.resolve_customer_trust(source,choice)
     elif kind=='resolve_intake':session.resolve_intake_request(choice)
     elif kind=='resolve_regular_introduction':session.resolve_regular_introduction(choice)
     elif kind=='resolve_reservation':session.resolve_reservation(choice)
     elif kind=='purchase_seat_equipment':
      from cat_cafe_sim.core.cafe_seat_equipment import catalog
      session.purchase_seat_equipment(seat_id,next(r for r in catalog() if r['id']==choice))
     elif kind=='next_day':session.next_day()
     elif kind in ('start_business','advance_business'):
      if c.closed or kind=='start_business' and not c.can_set_shifts or kind=='advance_business' and c.can_set_shifts:raise ValueError()
      if not session.automatic_step(auto_assign=auto):raise ValueError()
     else:getattr(session,kind)()
    except ValueError:failed=True
    c=session.core;case['operations'].append(dict(kind=kind,working=working,cat_id=cat_id,choice=choice,source=source,seat_id=seat_id,customer_id=customer_id,rejected=failed,assignment_projection=assignment_projection(),expected=expected()))
    if failed:assert before==expected(),kind
    return not failed
   def events():
    for e in list((c.adoption or {}).get('events',{}).values()):
     if e['status']=='waiting':
      record('set_auto_assignment',choice='manual');record('assign_service',customer_id=c.queue[0] if c.queue else 'guest-1',cat_id=session.available_cats()[0].id if session.available_cats() else 'cat-mugi',seat_id=session.free_seats[0] if session.free_seats else 'seat-1');record('resolve_adoption',source=e['id'],choice='decline')
    for e in list(c.store_events['days']):
     if e['day']==c.day and e.get('status')=='waiting':record('set_auto_assignment',choice='manual');record('start_business');record('resolve_store_event',choice='patch' if e['type']=='trouble' else 'decline')
    for e in list((c.activities or {}).get('events',{}).values()):
     if e['status']=='waiting':record('resolve_activity',source=e['id'],choice='receive')
    for e in list(c.management['events'].values()):
     if e['status']=='waiting':record('resolve_missing',source=e['id'],cat_id=e['cat_id'])
    for e in list(c.customer_trust['events'].values()):
     if e['status']=='waiting':record('resolve_customer_trust',source=e['id'],choice='recover')
    growth_answers(c,record)
    if c.intake_request['status']=='waiting':record('resolve_intake',choice='decline')
    if c.regular_introduction['status']=='waiting':record('resolve_regular_introduction',choice='decline')
    if c.reservation and c.reservation['request'] and c.reservation['request']['status']=='waiting':record('resolve_reservation',choice='decline')
    if cafe_goal.pending(c):record('set_auto_assignment',choice='manual');record('assign_service',customer_id='guest-1',cat_id='cat-mugi',seat_id='seat-1');record('continue_goal')
   record('set_auto_assignment',choice='invalid');record('assign_service',customer_id='guest-1',cat_id='cat-mugi',seat_id='seat-1')
   if variant!='automatic':record('set_auto_assignment',choice='manual')
   if variant=='adoption':record('configure_adoption',choice='on')
   if variant=='dispatch':record('dispatch',cat_id='cat-sora',choice='neighborhood_visit')
   if variant in ('resting','single-cat'):record('set_shifts',working=['cat-mugi'])
   if variant=='equipment':record('purchase_seat_equipment',seat_id='seat-2',choice='cat_tower')
   for day in range(4):
    if day>0:record('next_day');events()
    events()
    if variant in ('resting','single-cat'):record('set_shifts',working=['cat-mugi'])
    else:record('set_shifts',working=[k for k in c.cats if c.activity(k)=='cafe' and c.cats[k].health_status=='healthy'])
    record('start_business')
    while not c.closed:
     events()
     if assignment_projection()['waiting']:
      record('advance_business');record('assign_service',customer_id='unknown',cat_id='cat-mugi',seat_id='seat-1')
      customer=c.queue[-1] if variant=='nonfront' else c.queue[0];cats=session.available_cats();seat=session.free_seats[-1]
      if variant=='resting':record('assign_service',customer_id=customer,cat_id='cat-kohaku',seat_id=seat)
      if variant=='dispatch':record('assign_service',customer_id=customer,cat_id='cat-sora',seat_id=seat)
      record('assign_service',customer_id=customer,cat_id=cats[0].id,seat_id='seat-99');record('assign_service',customer_id=customer,cat_id=cats[0].id,seat_id=seat)
      record('assign_service',customer_id=customer,cat_id=cats[0].id,seat_id=seat)
      if c.queue:
       record('assign_service',customer_id=c.queue[0],cat_id=cats[0].id,seat_id=seat)
      if variant=='toggle':record('set_auto_assignment',choice='automatic')
      continue
     record('advance_business')
    events();record('assign_service',customer_id='guest-1',cat_id='cat-mugi',seat_id='seat-1');record('set_auto_assignment',choice='automatic');record('set_auto_assignment',choice='manual' if variant!='automatic' else 'automatic')
 return dict(cases=cases)

if __name__=='__main__':
 data=generate();Path(sys.argv[1]).write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8');print(len(data['cases']),'service assignment scenarios')
