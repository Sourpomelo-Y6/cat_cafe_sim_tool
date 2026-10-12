"""Original recruitment batches, cyclic candidates, admission and persistence."""
import copy,json,sys,tempfile
from pathlib import Path
from dataclasses import replace
from generate_reference import ROOT,growth_answers,exchange
from generate_patron_cases import expected as patron_expected
from generate_expansion_cases import expected as goal_expected
from export_standalone_rules import exported_rules
from cat_cafe_sim.cafe_new_game import create_game,starting_conditions
from cat_cafe_sim.core import cafe_player,cafe_popularity_challenge
from cat_cafe_sim.core.human_cat_relationship import verify_relationship

def generate():
 cases=[]
 specs=[(m,'normal',20000) for m in ('free','bond','patron','popularity')]+[('free','funds',v) for v in (199,200,201)]+[('free',v,20000) for v in ('batches','gates','growth')]+[('popularity','clear',20000)]
 for mode,variant,funds in specs:
  with tempfile.TemporaryDirectory(dir=ROOT/'Logs/Standalone') as temp:
   rules=exported_rules();selected=starting_conditions(mode)
   rules['starting_funds']=selected['management']['starting_funds']=funds
   rules['store_events']['probability']=selected['store_events']['probability']=0
   rules['growth']['threshold']=selected['growth']['threshold']=1 if variant=='growth' else 1000
   if variant=='clear':
    rules['goal_stages']=[dict(days=30,target=k) for k in (101,102,103)];selected['goal'].update(rules['goal_stages'][0],stages=rules['goal_stages'][1:])
   if variant in ('growth','clear'):
    rules['max_probability']=rules['fatigue_per_service_tick']=rules['stress_per_service_tick']=0;selected['management']['stress_per_service_tick']=0
   session=create_game(Path(temp),selected,seed=42);history=[]
   if variant in ('growth','clear'):session.core.health_rules=replace(session.core.health_rules,max_probability=0);session.core.shift_rules=replace(session.core.shift_rules,fatigue_per_service_tick=0)
   case=dict(variant='recruit-'+mode+'-'+variant+'-'+str(funds),rules=copy.deepcopy(rules),objective=mode,seed=42,operations=[]);cases.append(case)
   def expected():
    c=session.core;s=patron_expected(session);s.update(goal_expected(session));s['challenge_started_day']=c.goal.get('challenge_started_day',0)
    if c.goal.get('tracking_only'):s['goal']=[]
    v=c.visiting_cat;s['visiting_cat']=dict(status=v['status'],presented_day=v['presented_day'] or 0,accepted_day=v['accepted_day'] or 0,actions=copy.deepcopy(v['actions']))
    p=cafe_player.state(c);s['player_cats']=[dict(cat_id=k,affinity=p['affinity'][k],today=p['today'][k],total=p['total'][k]) for k in c.cats];s['player_sessions']=copy.deepcopy(history)
    g=c.bond_goal;s['bond_goal']=None if g is None else dict(started_day=g['started_day'],status=g['status'],resolved_day=g['resolved_day'] or 0,continued=g['continued'],achieved_cats=g['achieved_cats'])
    s['shop_purchases']=[dict(cat_id=k,day=d) for k,d in c.pet_shop['accepted'].items()];s['pet_shop_expenses']=c.summary()['pet_shop_expenses']
    for target,day in zip(s['history'],c.day_results):target.update(pet_shop_expenses=day['summary']['pet_shop_expenses'],recruitment_expenses=day['summary']['recruitment_expenses'],expenses=day['summary']['total_expenses'])
    data=c.recruitment;s['recruitment']=None if data is None else dict(id='recruitment',days=sorted(set(data.get('presented_days',{}).values())) or [data['opened_day']],accepted=[dict(cat_id=k,day=d) for k,d in data['accepted'].items()])
    return s
   def record(kind,cat_id=None,choice=None,source=None,working=None):
    if len(case['operations'])>1500:raise RuntimeError((variant,kind,session.core.day,session.core.goal))
    before=expected();c=session.core;stress={k:c.management['stress'][k] for k in c.cats};failed=False
    try:
     if kind=='open_recruitment':session.open_recruitment()
     elif kind=='recruit_cat':session.recruit_cat(cat_id)
     elif kind=='purchase_cat':session.purchase_cat(cat_id)
     elif kind=='resolve_visiting_cat':session.resolve_visiting_cat(choice)
     elif kind=='start_popularity_challenge':
      chosen=cafe_popularity_challenge.rules(c);chosen['goal'].update(rules['goal_stages'][0],stages=rules['goal_stages'][1:]);session.start_popularity_challenge(chosen)
     elif kind=='player_begin':session.play_with_player(cat_id)
     elif kind=='player_step':session.player_command(choice,source)
     elif kind=='player_finish':session.player_command(finish=True)
     elif kind.startswith('resolve_growth'):getattr(c,kind)(cat_id,choice)
     elif kind=='resolve_intake':session.resolve_intake_request(choice)
     elif kind=='resolve_regular_introduction':session.resolve_regular_introduction(choice)
     elif kind=='resolve_reservation':session.resolve_reservation(choice)
     elif kind=='resolve_customer_trust':c.resolve_customer_trust(source,choice)
     elif kind=='resolve_missing':session.resolve_missing(source)
     elif kind=='set_shifts':session.set_shifts(working)
     elif kind=='dispatch':session.dispatch(cat_id,json.loads((ROOT/'cat_cafe_sim/config/cafe_dispatch.json').read_text()))
     elif kind=='resolve_activity':session.resolve_activity(source)
     elif kind in ('day_off','next_day','advance_goal','continue_goal','purchase_housing','upgrade_housing'):getattr(session,kind)()
     else:session.automatic_step()
    except ValueError:failed=True
    c=session.core
    if not failed and kind in ('player_begin','player_step','player_finish'):
     log=cafe_player.active(c) or (c.player_bond or {}).get('last')
     if log:
      i=verify_relationship(log);a=log['initial_relationship'];key=a['cat_id'];active=i.state['end_reason'] is None
      row=dict(id=a['session_id'],cat_id=key,day=int(a['session_id'].split('-')[1]),ordinal=int(a['session_id'].split('-')[2]),initial_revision=a['revision'],initial_affinity=a['affinity'],initial_stamina=log['initial_stamina'],manual=i.state['end_reason']=='manual',exchange=exchange(i),actions=[dict(action=r['action'],target=r['target_type']) for r in log['records']],stress_before=0 if active else stress[key],stress_after=0 if active else c.management['stress'][key])
      if history and history[-1]['id']==row['id']:history[-1]=row
      else:history.append(row)
    after=expected()
    if failed:assert before==after,(mode,variant,kind)
    candidates=[]
    for key,row in (c.recruitment or {}).get('candidates',{}).items():
     p=row['personality'];candidates.append(dict(cost=row['cost'],cat=dict(id=key,name=row['name'],features=row.get('features',[]),trait_id=row.get('trait',{}).get('id',''),type_preferences=[p['type_preferences'][t['id']] for t in rules['types']],intensity_preferences=[p['intensity_preferences'][k] for k in ('gentle','standard','active')])))
    case['operations'].append(dict(recruitment_candidates=candidates,kind=kind,cat_id=cat_id,choice=choice,source=source,working=working,rejected=failed,expected=after))
   def events():
    c=session.core;growth_answers(c,record)
    if c.intake_request['status']=='waiting':record('resolve_intake',choice='decline')
    if c.regular_introduction['status']=='waiting':record('resolve_regular_introduction',choice='decline')
    if c.reservation and c.reservation['request'] and c.reservation['request']['status']=='waiting':record('resolve_reservation',choice='decline')
    for e in list(c.customer_trust['events'].values()):
     if e['status']=='waiting':record('resolve_customer_trust',source=e['id'],choice='recover')
    for e in list(c.management['events'].values()):
     if e['status']=='waiting':record('resolve_missing',source=e['id'])
   def business():
    events();record('start_business')
    while not session.core.closed:events();record('advance_business')
    events()
   record('recruit_cat',cat_id='rescue-1')
   if variant=='gates':record('player_begin',cat_id='cat-mugi');record('open_recruitment');record('player_finish')
   if variant=='batches':record('day_off');events()
   record('open_recruitment');record('open_recruitment');record('recruit_cat',cat_id='unknown')
   if variant=='funds':
    record('recruit_cat',cat_id='rescue-1');record('recruit_cat',cat_id='rescue-1');assert ('rescue-1' in session.core.recruitment['accepted'])==(funds>200);continue
   record('recruit_cat',cat_id='rescue-1');record('recruit_cat',cat_id='rescue-1');record('recruit_cat',cat_id='rescue-2');record('purchase_housing');record('purchase_cat',cat_id='shop-1');record('recruit_cat',cat_id='rescue-2');record('recruit_cat',cat_id='rescue-3')
   assert len(session.core.cats)==9
   if variant=='batches':
    for batch in range(1,8):
     due=session.core.recruitment.get('last_added_day',session.core.recruitment['opened_day'])+3
     while session.core.day<due:record('open_recruitment');record('day_off');events()
     if batch==2:
      for _ in range(4):record('day_off');events()
     record('open_recruitment');record('open_recruitment');assert len(session.core.recruitment['candidates'])==(batch+1)*3
    assert session.core.recruitment['candidates']['rescue-19']['name']=='ハル（紹介7）';record('recruit_cat',cat_id='rescue-19');record('recruit_cat',cat_id='rescue-20');record('upgrade_housing');record('recruit_cat',cat_id='rescue-20');record('recruit_cat',cat_id='rescue-19');continue
   if variant=='gates':
    record('player_begin',cat_id='cat-mugi');record('open_recruitment');record('recruit_cat',cat_id='rescue-2');record('player_finish')
   record('player_begin',cat_id='rescue-1');record('player_step',choice='direct');record('player_finish');record('set_shifts',working=['rescue-1','rescue-2','rescue-3']);business();record('open_recruitment');record('recruit_cat',cat_id='rescue-2')
   if variant=='clear':record('advance_goal')
   record('next_day');events()
   if variant=='clear':
    for stage in (1,2):
     record('set_shifts',working=list(session.core.cats));business();record('advance_goal' if stage==1 else 'continue_goal');record('next_day');events()
    assert session.core.clear_results['popularity']['cats']==9
   else:
    record('day_off');events();record('day_off');record('open_recruitment');events();record('open_recruitment')
   record('dispatch',cat_id='rescue-1',choice='neighborhood_visit');record('day_off');events()
   for e in list(session.core.activities['events'].values()):
    if e['status']=='waiting':record('resolve_activity',source=e['id'],choice='receive')
 return dict(cases=cases)
if __name__=='__main__':
 Path(sys.argv[1]).write_text(json.dumps(generate(),ensure_ascii=False),encoding='utf-8');print(sys.argv[1])
