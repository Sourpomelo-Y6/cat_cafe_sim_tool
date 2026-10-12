"""Original adoption offers, threshold/player comparison and permanent transfers."""
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
 specs=[(m,'accept',80,False) for m in ('free','bond','patron','popularity')]+[('free',v,g,p) for v,g,p in [('decline',80,False),('off',80,False),('below',79,False),('equal',80,True),('higher',81,True),('multiple',80,False),('simultaneous',80,False),('closing',80,False),('growth',80,False),('return',80,False)]]
 specs += [('popularity','closing_goal',80,False),('bond','bond_record',80,True)]
 for mode,variant,gain,play in specs:
  with tempfile.TemporaryDirectory(dir=ROOT/'Logs/Standalone') as temp:
   rules=exported_rules();selected=starting_conditions(mode);answer='decline' if variant in ('decline','closing_goal') else 'accept'
   rules['starting_funds']=selected['management']['starting_funds']=20000;rules['store_events']['probability']=selected['store_events']['probability']=0;rules['growth']['threshold']=selected['growth']['threshold']=1000
   rules['exchange']['ticks']=1;rules['exchange']['affinity_enthusiastic']=rules['exchange']['affinity_favorable']=gain
   if variant.startswith('closing') or variant in ('bond_record','return'):rules['opening_ticks']=2;rules['arrival_ticks']=[0];rules['patterns']=[dict(days=list(range(7)))]*7
   if variant=='closing_goal':
    rules['goal_stages']=[dict(days=10,target=k) for k in (101,102,103)];selected['goal'].update(rules['goal_stages'][0],stages=rules['goal_stages'][1:]);rules['satisfaction']['satisfied_score']=selected['customer_satisfaction']['satisfied_score']=1;rules['satisfaction']['dissatisfied_score']=selected['customer_satisfaction']['dissatisfied_score']=0;rules['preference_pool']=selected['preferences']['pool']=['brown_tabby']
   if variant=='bond_record':rules['bond']=selected['bond']=dict(target=1,affinity=80)
   if variant=='simultaneous':rules['arrival_ticks']=[0,0];rules['patterns']=[dict(days=list(range(7)))]*7
   if variant=='growth':rules['growth']['threshold']=selected['growth']['threshold']=1
   session=create_game(Path(temp),selected,seed=42);history=[]
   if variant=='simultaneous':session.core.config=replace(session.core.config,arrival_ticks=(0,0));session.core.weekdays['patterns']=[list(range(7))]*7
   session.interaction_config=replace(session.interaction_config,ticks=1,affinity_enthusiastic=gain,affinity_favorable=gain)
   if variant.startswith('closing') or variant in ('bond_record','return'):
    session.core.config=replace(session.core.config,opening_ticks=2,arrival_ticks=(0,));session.core.weekdays['patterns']=[list(range(7))]*7
   case=dict(variant='adoption-'+mode+'-'+variant,rules=copy.deepcopy(rules),objective=mode,seed=42,operations=[]);cases.append(case)
   def expected():
    c=session.core;s=patron_expected(session);s.update(goal_expected(session));s['challenge_started_day']=c.goal.get('challenge_started_day',0)
    if c.goal.get('tracking_only'):s['goal']=[]
    v=c.visiting_cat;s['visiting_cat']=dict(status=v['status'],presented_day=v['presented_day'] or 0,accepted_day=v['accepted_day'] or 0,actions=copy.deepcopy(v['actions']))
    p=cafe_player.state(c);s['player_cats']=[dict(cat_id=k,affinity=p['affinity'][k],today=p['today'][k],total=p['total'][k]) for k in c.cats];s['player_sessions']=copy.deepcopy(history)
    g=c.bond_goal;s['bond_goal']=None if g is None else dict(started_day=g['started_day'],status=g['status'],resolved_day=g['resolved_day'] or 0,continued=g['continued'],achieved_cats=g['achieved_cats'])
    s['shop_purchases']=[dict(cat_id=k,day=d) for k,d in c.pet_shop['accepted'].items()];s['pet_shop_expenses']=c.summary()['pet_shop_expenses']
    for target,day in zip(s['history'],c.day_results):target.update(pet_shop_expenses=day['summary']['pet_shop_expenses'],recruitment_expenses=day['summary']['recruitment_expenses'],expenses=day['summary']['total_expenses'])
    data=c.recruitment;s['recruitment']=None if data is None else dict(id='recruitment',days=sorted(set(data.get('presented_days',{}).values())) or [data['opened_day']],accepted=[dict(cat_id=k,day=d) for k,d in data['accepted'].items()])
    data=c.adoption;s['adoption']=None if data is None else dict(id='adoption',enabled=data['enabled'],events=[dict(e,source='service-'+str(e['day'])+'-'+e['cat_id']+'-'+e['customer_id'],resolved_day=e['resolved_day'] or 0,resolved_tick=e['resolved_tick'] or 0) for e in data['events'].values()])
    return s
   def record(kind,cat_id=None,choice=None,source=None,working=None):
    if len(case['operations'])>1500:raise RuntimeError((variant,kind,session.core.day,session.core.goal))
    before=expected();c=session.core;stress={k:c.management['stress'][k] for k in c.cats};failed=False
    try:
     if kind=='configure_adoption':session.configure_adoption(choice=='on') if choice in ('on','off') else (_ for _ in ()).throw(ValueError())
     elif kind=='resolve_adoption':session.resolve_adoption(source,choice)
     elif kind=='open_recruitment':session.open_recruitment()
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
     elif kind in ('day_off','next_day','advance_goal','continue_goal','purchase_housing','upgrade_housing','continue_bond_goal'):getattr(session,kind)()
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
    c=session.core
    for e in list((c.adoption or {}).get('events',{}).values()):
     if e['status']=='waiting':record('resolve_adoption',source=e['id'],cat_id=e['cat_id'],choice=answer)
    growth_answers(c,record)
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
   record('configure_adoption',choice='invalid');record('resolve_adoption',source='unknown',cat_id='cat-mugi',choice='accept')
   if play:
    record('player_begin',cat_id='cat-mugi');record('player_step',choice='direct');record('player_finish')
   if variant=='higher':record('player_begin',cat_id='cat-mugi');record('player_step',choice='direct');record('player_finish')
   if variant=='bond_record':record('continue_bond_goal')
   record('set_shifts',working=list(session.core.cats) if variant in ('multiple','simultaneous') else ['cat-mugi'])
   if variant!='off':record('configure_adoption',choice='on')
   if variant=='return':record('dispatch',cat_id='cat-sora',choice='neighborhood_visit')
   record('start_business');record('configure_adoption',choice='off')
   saw=False
   while not session.core.closed:
    record('advance_business')
    waiting=[e for e in (session.core.adoption or {}).get('events',{}).values() if e['status']=='waiting']
    if waiting:
     if variant=='growth':growth_answers(session.core,record)
     if variant=='return':
      for dispatch in session.core.activities['events'].values():
       if dispatch['status']=='waiting':record('resolve_activity',source=dispatch['id'],choice='receive')
     saw=True;record('advance_business');record('day_off');record('configure_adoption',choice='off');record('player_begin',cat_id='cat-sora')
     if variant=='simultaneous':assert len(waiting)==2
     for index,e in enumerate(waiting):
      chosen='decline' if variant=='simultaneous' and index==1 else answer
      record('resolve_adoption',source=e['id'],cat_id=e['cat_id'],choice='invalid');record('resolve_adoption',source=e['id'],cat_id=e['cat_id'],choice=chosen);record('resolve_adoption',source=e['id'],cat_id=e['cat_id'],choice=chosen);record('resolve_adoption',source=e['id'],cat_id=e['cat_id'],choice='decline' if chosen=='accept' else 'accept')
    events()
   events()
   if variant not in ('off','below','equal','higher','bond_record'):assert saw,(variant,session.core.adoption)
   if variant in ('off','below','equal','higher'):assert not (session.core.adoption or {}).get('events'),(variant,session.core.adoption)
   if variant=='closing_goal':
    assert session.core.goal['status']=='cleared';record('advance_goal')
    for stage in (1,2):
     if stage==2:answer='accept'
     record('next_day');events();record('start_business')
     while not session.core.closed:record('advance_business');events()
     assert session.core.goal['status']=='cleared';record('advance_goal' if stage==1 else 'continue_goal')
   record('next_day');events()
   if variant=='bond_record':
    record('set_shifts',working=['cat-mugi']);record('start_business')
    while not session.core.closed:record('advance_business');events()
    assert session.core.activity('cat-mugi')=='adopted',(session.core.adoption,session.core.bond_goal,[(o['kind'],o['rejected']) for o in case['operations'][-20:]]);record('next_day');events()
   record('configure_adoption',choice='off');record('configure_adoption',choice='on')
   if saw and answer=='accept':
    for e in session.core.adoption['events'].values():
     if e['choice']=='accept':record('set_shifts',working=[e['cat_id']]);record('player_begin',cat_id=e['cat_id']);record('dispatch',cat_id=e['cat_id'],choice='neighborhood_visit')
    record('purchase_cat',cat_id='shop-1');record('purchase_cat',cat_id='shop-2');record('day_off');events()
 return dict(cases=cases)
if __name__=='__main__':
 Path(sys.argv[1]).write_text(json.dumps(generate(),ensure_ascii=False),encoding='utf-8');print(sys.argv[1])
