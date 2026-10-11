"""Execute original patron progression; compare every operation and save boundary."""
import copy,json,sys,tempfile
from pathlib import Path
from generate_reference import ROOT,growth_answers
from generate_dispatch_cases import expected as dispatch_expected
from export_standalone_rules import exported_rules
from cat_cafe_sim.cafe_new_game import create_game,starting_conditions
from cat_cafe_sim.core.cafe_patron import destinations

def expected(session):
 s=dispatch_expected(session);p=session.core.patron
 s['patron']=None if p is None else dict(started_day=p['started_day'],status=p['status'],resolved_day=p['resolved_day'] or 0,continued=p['continued'],satisfaction=p['satisfaction'],members=[dict(id=k,value=v) for k,v in p['members'].items()])
 for row,e in zip(s['dispatches'],(session.core.activities or {}).get('events',{}).values()):
  if 'patron_match' in e:
   match=copy.deepcopy(e['patron_match']);match['condition'].setdefault('feature',None);match['condition'].setdefault('specialization',None);row['patron_match']=match
 s['clear_results']=[dict(mode=k,**v) for k,v in session.core.clear_results.items()]
 return s

def generate():
 cases=[]
 for variant in ('default','natural-growth','closed','dispatch-specialist','unmatched','no-goal','funds-end'):
  with tempfile.TemporaryDirectory(dir=ROOT/'Logs/Standalone') as temp:
   mode='free' if variant=='no-goal' else 'patron';rules=exported_rules();selected=starting_conditions(mode)
   rules['store_events']['probability']=selected['store_events']['probability']=0
   rules['starting_funds']=selected['management']['starting_funds']=1 if variant=='funds-end' else 1000 if variant=='natural-growth' else 20000
   # Accelerate development only; default targets, rewards, days and conditions stay intact.
   if variant!='natural-growth':rules['growth']['threshold']=selected['growth']['threshold']=1
   if variant=='closed':
    rules.update(max_probability=0,stress_per_service_tick=0,fatigue_per_service_tick=0);selected['management']['stress_per_service_tick']=0
   session=create_game(Path(temp),selected,seed=42)
   if variant=='closed':
    from dataclasses import replace
    session.core.health_rules=replace(session.core.health_rules,max_probability=0);session.core.shift_rules=replace(session.core.shift_rules,fatigue_per_service_tick=0)
   case=dict(variant=variant,rules=copy.deepcopy(rules),objective=mode,seed=42,operations=[]);cases.append(case)
   def record(kind,cat_id=None,choice=None,source=None,working=None):
    if len(case['operations'])>5000:raise RuntimeError((variant,kind,session.core.day,session.core.summary(),session.core.patron))
    before=expected(session);failed=False
    try:
     if kind=='dispatch':session.dispatch(cat_id,next(r for r in destinations(session.core) if r['id']==choice) if session.core.patron else selected['patron']['destination'])
     elif kind=='resolve_activity':session.resolve_activity(source)
     elif kind.startswith('resolve_growth'):getattr(session.core,kind)(cat_id,choice)
     elif kind=='resolve_missing':session.resolve_missing(source)
     elif kind=='resolve_customer_trust':session.core.resolve_customer_trust(source,choice)
     elif kind=='set_shifts':session.set_shifts(working)
     elif kind=='resolve_intake':session.resolve_intake_request(choice)
     elif kind=='resolve_regular_introduction':session.resolve_regular_introduction(choice)
     elif kind in ('day_off','next_day','continue_patron'):getattr(session,kind)()
     else:session.automatic_step()
    except ValueError:failed=True
    after=expected(session)
    if failed:assert before==after,(variant,kind)
    case['operations'].append(dict(kind=kind,cat_id=cat_id,choice=choice,source=source,working=working,rejected=failed,expected=after))
   def events():
    if session.core.management['game_over']:return
    for e in list(session.core.management['events'].values()):
     if e['status']=='waiting':record('resolve_missing',cat_id=e['cat_id'],source=e['id'])
    for e in list(session.core.customer_trust['events'].values()):
     if e['status']=='waiting':record('resolve_customer_trust',source=e['id'],choice='recover')
    growth_answers(session.core,record,specialization='dispatch' if variant=='dispatch-specialist' else 'service')
    if session.core.intake_request['status']=='waiting':record('resolve_intake',choice='decline')
    if session.core.regular_introduction['status']=='waiting':record('resolve_regular_introduction',choice='decline')
   def close():
    if session.core.closed:record('next_day');events()
    if variant!='closed':record('day_off');return
    working=[key for key,cat in session.core.cats.items() if session.core.activity(key)=='cafe' and cat.health_status=='healthy']
    if not working:record('day_off');return
    record('set_shifts',working=working);record('start_business')
    while not session.core.closed:
     events();record('advance_business')
     if case['operations'][-1]['rejected']:raise RuntimeError((variant,session.core.day,session.core.summary()))
   record('continue_patron')
   record('dispatch',cat_id='cat-sora',choice='patron_visit')
   if variant=='no-goal':continue
   record('resolve_activity',source='dispatch-1-cat-sora',choice='receive')
   for _ in range(2):events();close()
   if session.core.management['game_over']:
    record('resolve_activity',source='dispatch-1-cat-sora',choice='receive');record('continue_patron');continue
   record('resolve_activity',source='dispatch-1-cat-sora',choice='receive');record('resolve_activity',source='dispatch-1-cat-sora',choice='receive');events()
   if session.core.closed:record('next_day');events()
   # Exercise feature-only, specialization-only and unmatched conditions before full completion.
   order=['patron_strict_visit']+['patron_moody_visit']*4
   for dest in order:
    cat='cat-mike' if variant=='unmatched' else 'cat-sora'
    eid=f'dispatch-{session.core.day}-{cat}';record('dispatch',cat_id=cat,choice=dest)
    if dest!='patron_visit':record('dispatch',cat_id='cat-mugi',choice=dest)
    for _ in range(2):events();close()
    record('resolve_activity',source=eid,choice='receive');events()
    if session.core.closed:record('next_day');events()
   if variant in ('unmatched','dispatch-specialist'):continue
   # Use developed white cat to achieve all three default targets (black wish intentionally unmatched).
   guard=0
   while session.core.patron['status']=='active':
    guard+=1
    if guard>100:raise RuntimeError((variant,session.core.day,session.core.patron))
    p=session.core.patron;dest='patron_visit' if p['satisfaction']<100 else 'patron_strict_visit' if p['members']['patron_strict_visit']<100 else 'patron_moody_visit'
    eid=f'dispatch-{session.core.day}-cat-sora';record('dispatch',cat_id='cat-sora',choice=dest)
    for _ in range(2):events();close()
    record('resolve_activity',source=eid,choice='receive')
    if p['status']=='cleared':break
    events()
    if session.core.closed:record('next_day');events()
   record('day_off');record('continue_patron');record('continue_patron');events()
   if session.core.closed:record('next_day');events()
   close();events() # Fixed clear result must survive further play.
 matrix=[]
 with tempfile.TemporaryDirectory(dir=ROOT/'Logs/Standalone') as temp:
  session=create_game(Path(temp),starting_conditions('patron'));c=session.core
  from cat_cafe_sim.core.cafe_patron_members import terms
  from cat_cafe_sim.core.cafe_dispatch_match import terms as welcome
  from cat_cafe_sim.core.cafe_growth import dispatch_multiplier
  from cat_cafe_sim.core.cafe_activities import reward
  for features in ([],['white'],['black'],['long_hair']):
   for specialization in (None,'service','dispatch'):
    for index in range(3):
     c.cat_features['cat-sora']=features;c.growth['cats']['cat-sora']['specialization']=specialization;c.activities=dict(events={str(i):dict(destination=dict(id='patron_moody_visit'),status='resolved') for i in range(index)})
     base=c.patron['rules']['destination'];w=welcome(c,'cat-sora',base);e=dict(destination=base,cat_id='cat-sora',growth_multiplier=dispatch_multiplier(c,'cat-sora'),welcome_match=w)
     matches=[terms(c,'cat-sora',member['id']) for member in c.patron['rules']['members']]
     for match in matches:match['condition'].setdefault('feature',None);match['condition'].setdefault('specialization',None)
     matrix.append(dict(features=features,specialization=specialization,index=index,matches=matches,reward=reward(c,e),gain=c.patron['rules']['gain']+w['satisfaction_bonus']))
 return dict(cases=cases,patron_matrix=matrix,patron_matrix_rules=exported_rules())

if __name__=='__main__':
 Path(sys.argv[1]).write_text(json.dumps(generate(),ensure_ascii=False),encoding='utf-8');print(sys.argv[1])
