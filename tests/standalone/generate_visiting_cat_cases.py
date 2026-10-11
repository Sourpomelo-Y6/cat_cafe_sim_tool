"""Execute Python visiting-cat rules and compare every command boundary with C#."""
import copy,json,sys,tempfile
from pathlib import Path
from generate_reference import ROOT,growth_answers,exchange
from generate_patron_cases import expected as patron_expected
from generate_expansion_cases import expected as goal_expected
from export_standalone_rules import exported_rules
from cat_cafe_sim.cafe_new_game import create_game,starting_conditions
from cat_cafe_sim.core import cafe_player,cafe_popularity_challenge
from cat_cafe_sim.core.human_cat_relationship import verify_relationship

def generate():
 cases=[]
 for mode,variant in [(m,'normal') for m in ('free','bond','patron','popularity')]+[('popularity',v) for v in ('full','funds200','funds201','gates','growth')]:
  with tempfile.TemporaryDirectory(dir=ROOT/'Logs/Standalone') as temp:
   rules=exported_rules();selected=starting_conditions(mode)
   rules['starting_funds']=selected['management']['starting_funds']=20000
   rules['store_events']['probability']=selected['store_events']['probability']=0
   rules['growth']['threshold']=selected['growth']['threshold']=1000
   rules['goal_stages']=[dict(days=30,target=k) for k in (101,250,300)];selected['goal'].update(rules['goal_stages'][0],stages=rules['goal_stages'][1:])
   if variant.startswith('funds'):
    rules['base_cost']=selected['operating_cost']['base_cost']=2000
    rules['starting_funds']=selected['management']['starting_funds']=int(variant[5:])+3*(rules['base_cost']+2*rules['per_seat_cost'])-first_revenue
   if variant=='growth':rules['growth']['threshold']=selected['growth']['threshold']=1
   session=create_game(Path(temp),selected,seed=42);history=[]
   case=dict(variant=mode+'-'+variant,rules=copy.deepcopy(rules),objective=mode,seed=42,operations=[]);cases.append(case)
   def expected():
    c=session.core;s=patron_expected(session);s.update(goal_expected(session));s['challenge_started_day']=c.goal.get('challenge_started_day',0)
    if c.goal.get('tracking_only'):s['goal']=[]
    v=c.visiting_cat;s['visiting_cat']=dict(status=v['status'],presented_day=v['presented_day'] or 0,accepted_day=v['accepted_day'] or 0,actions=copy.deepcopy(v['actions']))
    p=cafe_player.state(c);s['player_cats']=[dict(cat_id=k,affinity=p['affinity'][k],today=p['today'][k],total=p['total'][k]) for k in c.cats];s['player_sessions']=copy.deepcopy(history)
    g=c.bond_goal;s['bond_goal']=None if g is None else dict(started_day=g['started_day'],status=g['status'],resolved_day=g['resolved_day'] or 0,continued=g['continued'],achieved_cats=g['achieved_cats'])
    return s
   def record(kind,cat_id=None,choice=None,source=None,working=None):
    before=expected();c=session.core;stress={k:c.management['stress'][k] for k in c.cats};failed=False
    try:
     if kind=='resolve_visiting_cat':session.resolve_visiting_cat(choice)
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
     elif kind in ('day_off','next_day','advance_goal','continue_goal','purchase_housing'):getattr(session,kind)()
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
    case['operations'].append(dict(kind=kind,cat_id=cat_id,choice=choice,source=source,working=working,rejected=failed,expected=after))
   def visit(choice):record('resolve_visiting_cat',cat_id='visiting-cat-1',choice=choice)
   def events():
    c=session.core;growth_answers(c,record)
    if c.intake_request['status']=='waiting':record('resolve_intake',choice='accept' if variant=='full' else 'decline')
    if c.regular_introduction['status']=='waiting':record('resolve_regular_introduction',choice='decline')
    if c.reservation and c.reservation['request'] and c.reservation['request']['status']=='waiting':record('resolve_reservation',choice='decline')
    for e in list(c.customer_trust['events'].values()):
     if e['status']=='waiting':record('resolve_customer_trust',source=e['id'],choice='recover')
    for e in list(c.management['events'].values()):
     if e['status']=='waiting':record('resolve_missing',source=e['id'])
   visit('interact');visit('accept');visit('invalid')
   if mode!='popularity':record('start_popularity_challenge')
   record('start_business')
   while not session.core.closed:events();record('advance_business')
   if mode=='popularity' and variant=='normal':first_revenue=session.core.summary()['revenue']
   assert session.core.goal['status']=='cleared'
   visit('interact');events();record('advance_goal');record('next_day')
   assert session.core.visiting_cat['status']=='visiting'
   events()
   if variant in ('normal','gates','full'):
    visit('skip');visit('interact');visit('accept');record('day_off');events()
   if variant=='gates':
    record('player_begin',cat_id='cat-mugi');visit('interact');record('player_finish')
   for i in range(3):
    visit('accept');visit('interact');visit('interact');visit('skip')
    if i<2:
     record('day_off')
     if variant=='full' and session.core.intake_request['status']=='waiting':visit('interact')
     events()
   assert session.core.visiting_cat['status']=='ready'
   if variant=='full':
    assert len(session.core.cats)==6;visit('accept');record('purchase_housing')
   visit('accept')
   if variant=='funds200':
    assert session.core.funds==200 and session.core.visiting_cat['status']=='ready';continue
   assert session.core.visiting_cat['status']=='accepted',(mode,variant,session.core.funds)
   visit('accept');visit('skip');visit('interact')
   if variant=='funds201':assert session.core.funds==1;continue
   record('player_begin',cat_id='visiting-cat-1');record('player_step',choice='direct');record('player_finish')
   record('set_shifts',working=['visiting-cat-1']);record('start_business');visit('accept')
   while not session.core.closed:events();record('advance_business')
   visit('interact');events();record('next_day');events()
   record('dispatch',cat_id='visiting-cat-1',choice='neighborhood_visit');record('day_off');events()
   for e in list(session.core.activities['events'].values()):
    if e['status']=='waiting':record('resolve_activity',source=e['id'],choice='receive')
 return dict(cases=cases)
if __name__=='__main__':
 Path(sys.argv[1]).write_text(json.dumps(generate(),ensure_ascii=False),encoding='utf-8');print(sys.argv[1])
