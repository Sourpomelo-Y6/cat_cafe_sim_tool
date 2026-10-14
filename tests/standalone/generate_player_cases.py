"""Original ten-action player interactions and six-cat goal, without a Python server."""
import copy,json,sys,tempfile
from pathlib import Path
from generate_reference import ROOT,growth_answers,exchange
from generate_dispatch_cases import expected as dispatch_expected
from export_standalone_rules import exported_rules
from cat_cafe_sim.cafe_new_game import create_game,starting_conditions
from cat_cafe_sim.core import cafe_player
from cat_cafe_sim.core.human_cat_relationship import verify_relationship
from cat_cafe_sim.cafe_autoplay_player import _short_action
from cat_cafe_sim.unity_state_server import player_interaction_view

def generate(variants=None):
 cases=[]
 for variant in variants or ('actions','daily','exhaustion','store-gate','funds-end','clear','natural'):
  with tempfile.TemporaryDirectory(dir=ROOT/'Logs/Standalone') as temp:
   mode='bond' if variant in ('clear','natural','funds-end') else 'free';rules=exported_rules();selected=starting_conditions(mode)
   rules['store_events']['probability']=selected['store_events']['probability']=1 if variant=='store-gate' else 0
   rules['starting_funds']=selected['management']['starting_funds']=1 if variant=='funds-end' else 1000 if variant=='natural' else 20000
   session=create_game(Path(temp),selected,seed=42);c=session.core;history=[];case=dict(variant=variant,rules=copy.deepcopy(rules),objective=mode,seed=42,operations=[]);cases.append(case)
   def expected():
    s=dispatch_expected(session);p=cafe_player.state(c)
    s['player_cats']=[dict(cat_id=key,affinity=p['affinity'][key],today=p['today'][key],total=p['total'][key]) for key in c.cats]
    s['player_sessions']=copy.deepcopy(history);g=c.bond_goal
    s['bond_goal']=None if g is None else dict(started_day=g['started_day'],status=g['status'],resolved_day=g['resolved_day'] or 0,continued=g['continued'],achieved_cats=g['achieved_cats'])
    s['clear_results']=[dict(mode=k,**v) for k,v in c.clear_results.items()]
    return s
   def record(kind,cat_id=None,choice=None,source=None,working=None):
    nonlocal c
    if len(case['operations'])>4000:raise RuntimeError((variant,c.day,c.bond_goal))
    before=expected();failed=False;stress={key:c.management['stress'][key] for key in c.cats}
    try:
     if kind=='player_begin':session.play_with_player(cat_id)
     elif kind=='player_step':session.player_command(choice,source)
     elif kind=='player_finish':session.player_command(finish=True)
     elif kind=='dispatch':session.dispatch(cat_id)
     elif kind=='resolve_activity':session.resolve_activity(source)
     elif kind=='purchase_housing':session.purchase_housing()
     elif kind=='set_shifts':session.set_shifts(working)
     elif kind.startswith('resolve_growth'):getattr(c,kind)(cat_id,choice)
     elif kind=='resolve_intake':session.resolve_intake_request(choice)
     elif kind=='resolve_regular_introduction':session.resolve_regular_introduction(choice)
     elif kind=='resolve_store_event':session.resolve_store_event(choice)
     elif kind=='resolve_missing':session.resolve_missing(source)
     elif kind=='resolve_customer_trust':c.resolve_customer_trust(source,choice)
     elif kind in ('day_off','next_day','continue_bond_goal'):getattr(session,kind)()
     else:session.automatic_step()
    except ValueError:failed=True
    c=session.core
    if not failed and kind in ('player_begin','player_step','player_finish'):
     log=cafe_player.active(c) or (c.player_bond or {}).get('last')
     if log:
      interaction=verify_relationship(log);initial=log['initial_relationship'];key=initial['cat_id'];active=interaction.state['end_reason'] is None
      row=dict(id=initial['session_id'],cat_id=key,day=int(initial['session_id'].split('-')[1]),ordinal=int(initial['session_id'].split('-')[2]),initial_revision=initial['revision'],initial_affinity=initial['affinity'],initial_stamina=log['initial_stamina'],manual=interaction.state['end_reason']=='manual',exchange=exchange(interaction),actions=[dict(action=a['action'],target=a['target_type']) for a in log['records']],stress_before=0 if active else stress[key],stress_after=0 if active else c.management['stress'][key])
      if history and history[-1]['id']==row['id']:
       if history[-1]['exchange']['end_reason'] is not None:row['stress_before']=history[-1]['stress_before'];row['stress_after']=history[-1]['stress_after']
       history[-1]=row
      else:history.append(row)
    after=expected()
    if failed:assert before==after,(variant,kind)
    case['operations'].append(dict(kind=kind,cat_id=cat_id,choice=choice,source=source,working=working,rejected=failed,expected=after,presentation=player_interaction_view(session)))
   def events(accept=True):
    if c.management['game_over']:return
    for e in list((c.activities or {}).get('events',{}).values()):
     if e['status']=='waiting':record('resolve_activity',source=e['id'],choice='receive')
    for e in list(c.management['events'].values()):
     if e['status']=='waiting':record('resolve_missing',cat_id=e['cat_id'],source=e['id'])
    for e in list(c.customer_trust['events'].values()):
     if e['status']=='waiting':record('resolve_customer_trust',source=e['id'],choice='recover')
    growth_answers(c,record)
    if c.intake_request['status']=='waiting':
     if accept and c.housing['purchase'] is None:record('purchase_housing')
     record('resolve_intake',choice='accept' if accept else 'decline')
    if c.regular_introduction['status']=='waiting':record('resolve_regular_introduction',choice='decline')
   def complete():
    while cafe_player.active(c):
     action,target=_short_action(cafe_player.current(c));record('player_step',choice=action,source=target)
   record('player_step',choice='direct');record('player_finish');record('continue_bond_goal')
   if variant=='store-gate':
    record('player_begin',cat_id='cat-mike');record('resolve_store_event',choice='repair');record('resolve_store_event',choice='receive');record('resolve_store_event',choice='participate');continue
   if variant in ('actions','daily','exhaustion'):
    for index,key in enumerate(c.cats):
     if cafe_player.remaining(c)==0:events(False);record('day_off');events(False)
     record('player_begin',cat_id=key);record('player_begin',cat_id=key);record('day_off');record('set_shifts',working=list(c.cats));record('player_step',choice='invalid');record('player_step',choice='switch',source='invalid');record('player_step',choice='direct',source='ball')
     if variant=='daily':record('player_finish');record('player_finish');continue
     if variant=='exhaustion':
      while cafe_player.active(c):
       valid=cafe_player.current(c).valid_actions();record('player_step',choice='connect' if valid==['connect'] or 'intense' not in valid else 'intense')
     else:
      for action in ('connect','switch','direct','adapt','intense','feint','pause'):
       if cafe_player.active(c):record('player_step',choice=action,source='pet' if action=='switch' else None)
      if cafe_player.active(c):record('player_finish')
     record('player_step',choice='direct');record('player_finish')
    while cafe_player.remaining(c)>0:record('player_begin',cat_id='cat-mike');record('player_finish')
    record('player_begin',cat_id='cat-mike');events(False);record('day_off');events(False);record('player_begin',cat_id='cat-mike');record('player_finish');continue
   if variant=='funds-end':record('player_begin',cat_id='cat-mike');complete();record('day_off');record('player_begin',cat_id='cat-mike');record('player_finish');continue
   # Default six-cat target and affinity threshold; protect the budget through ordinary neighborhood visits.
   days=0
   while c.bond_goal['status']=='active':
    days+=1
    if days>150:raise RuntimeError((variant,c.day,c.funds,c.bond_goal,[(op['kind'],op['rejected']) for op in case['operations'][-5:]]))
    events()
    while cafe_player.remaining(c)>0 and c.bond_goal['status']=='active':
     eligible=[key for key in c.cats if cafe_player.state(c)['affinity'][key]<80 and not cafe_player.unavailable_reason(c,key)]
     if not eligible:break
     key=min(eligible,key=lambda key:(cafe_player.state(c)['affinity'][key],key));record('player_begin',cat_id=key);complete()
    if c.bond_goal['status']=='cleared':break
    if variant=='natural':
     eligible=[key for key,cat in c.cats.items() if cat.stamina>0 and cat.fatigue<=40 and c.activity(key)=='cafe']
     if eligible:record('dispatch',cat_id=eligible[-1],choice='neighborhood_visit')
    record('day_off')
   record('day_off');record('player_begin',cat_id='cat-mike');record('continue_bond_goal');record('continue_bond_goal');events()
   record('day_off');events();record('player_begin',cat_id='cat-mike');record('player_step',choice='direct');record('player_finish')
 return dict(cases=cases)

if __name__=='__main__':
 Path(sys.argv[1]).write_text(json.dumps(generate(),ensure_ascii=False),encoding='utf8');print(sys.argv[1])
