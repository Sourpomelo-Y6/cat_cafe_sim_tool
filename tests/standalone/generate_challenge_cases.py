"""Original Python mid-game popularity challenge, with explicit short-stage fixtures."""
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
 for mode in ('free','bond','patron','popularity'):
  for variant in (('expired','cleared','gates') if mode!='popularity' else ('gates',)):
   with tempfile.TemporaryDirectory(dir=ROOT/'Logs/Standalone') as temp:
    rules=exported_rules();selected=starting_conditions(mode)
    rules['starting_funds']=selected['management']['starting_funds']=20000
    rules['store_events']['probability']=selected['store_events']['probability']=0
    if variant=='cleared':
     rules['goal_stages']=[dict(days=10,target=k) for k in (101,102,103)];rules.update(max_probability=0,stress_per_service_tick=0,fatigue_per_service_tick=0);selected['management']['stress_per_service_tick']=0
    if mode=='bond' and variant=='cleared':rules['bond']=selected['bond']=dict(target=1,affinity=.5)
    session=create_game(Path(temp),selected,seed=42);history=[]
    if variant=='cleared':
     from dataclasses import replace
     session.core.health_rules=replace(session.core.health_rules,max_probability=0);session.core.shift_rules=replace(session.core.shift_rules,fatigue_per_service_tick=0)
    case=dict(variant=mode+'-'+variant,rules=copy.deepcopy(rules),objective=mode,seed=42,operations=[]);cases.append(case)
    def expected():
     c=session.core;s=patron_expected(session);s.update(goal_expected(session));s['challenge_started_day']=c.goal.get('challenge_started_day',0)
     if c.goal.get('tracking_only'):s['goal']=[]
     p=cafe_player.state(c);s['player_cats']=[dict(cat_id=k,affinity=p['affinity'][k],today=p['today'][k],total=p['total'][k]) for k in c.cats];s['player_sessions']=copy.deepcopy(history)
     g=c.bond_goal;s['bond_goal']=None if g is None else dict(started_day=g['started_day'],status=g['status'],resolved_day=g['resolved_day'] or 0,continued=g['continued'],achieved_cats=g['achieved_cats'])
     return s
    def record(kind,cat_id=None,choice=None,source=None,working=None):
     before=expected();c=session.core;stress={k:c.management['stress'][k] for k in c.cats};failed=False
     try:
      if kind=='start_popularity_challenge':
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
      elif kind in ('day_off','next_day','advance_goal','continue_goal','continue_bond_goal'):getattr(session,kind)()
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
    def events():
     c=session.core
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
    if variant=='gates':
     record('player_begin',cat_id='cat-mugi');record('start_popularity_challenge');record('player_finish');business();record('start_popularity_challenge');record('next_day');events();record('start_popularity_challenge');record('start_popularity_challenge');continue
    for _ in range(2):events();record('day_off')
    record('start_popularity_challenge');record('start_popularity_challenge')
    if variant=='expired':
     for _ in range(rules['goal_stages'][0]['days']):events();record('day_off')
     assert session.core.goal['status']=='expired'
     record('day_off');record('continue_goal');record('continue_goal');events();record('player_begin',cat_id='cat-mugi');record('player_finish');record('start_popularity_challenge');continue
    if mode=='bond':
     record('player_begin',cat_id='cat-mugi');record('player_step',choice='direct');record('player_finish');record('advance_goal');record('start_popularity_challenge');record('continue_bond_goal')
    for stage in range(3):
     business();assert session.core.goal['status']=='cleared',(mode,stage,session.core.goal,case['operations'][-2]['rejected'])
     record('advance_goal' if stage<2 else 'continue_goal');record('next_day');events()
    if mode=='bond':
     record('player_begin',cat_id='cat-mugi');record('player_step',choice='direct');record('player_finish');record('advance_goal');record('day_off');record('continue_bond_goal')
     assert len(session.core.clear_results)==2
    events();business();record('next_day');events();record('start_popularity_challenge')
 return dict(cases=cases)
if __name__=='__main__':
 Path(sys.argv[1]).write_text(json.dumps(generate(),ensure_ascii=False),encoding='utf-8');print(sys.argv[1])
