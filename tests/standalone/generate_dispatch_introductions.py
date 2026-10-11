"""Compare default dispatch candidates using original Python; only unlock popularity/store events/funds are fixtures."""
import copy,json,sys,tempfile
from pathlib import Path
from generate_dispatch_cases import expected
from generate_reference import ROOT,growth_answers
from export_standalone_rules import exported_rules
from cat_cafe_sim.core.cafe_dispatch_introduction import admission_reason
from cat_cafe_sim.cafe_new_game import create_game,starting_conditions

def generate():
 cases=[]
 for destination in ('shopping_street_event','cat_photo_studio'):
  for answer in ('accept','decline'):
   for closed in (False,True):
    with tempfile.TemporaryDirectory(dir=ROOT/'Logs/Standalone') as temp:
     rule=exported_rules();rule['starting_funds']=20000;rule['store_events']['probability']=0
     selected=starting_conditions('free');selected['management']['starting_funds']=20000;selected['store_events']['probability']=0
     for row in selected['dispatch_unlocks'].values():
      if 'popularity' in row:row['popularity']=100
     for row in rule['dispatch_unlocks']:
      if row['stage']==0:row['popularity']=100
     session=create_game(Path(temp),selected,seed=0);case=dict(variant=destination+'-'+answer+'-'+str(closed),rules=copy.deepcopy(rule),objective='free',operations=[]);cases.append(case)
     destinations={r['id']:{k:v for k,v in r.items() if k not in ('item_reward','unsupported_reason')} for r in rule['dispatch_destinations']}
     def record(kind,cat_id=None,choice=None,source=None,working=None):
      before=expected(session);failed=False
      try:
       if kind=='dispatch':session.dispatch(cat_id,destinations[choice])
       elif kind=='resolve_activity':session.resolve_activity(source)
       elif kind=='resolve_dispatch_choice':session.resolve_dispatch_choice(source,choice)
       elif kind=='resolve_dispatch_introduction':session.resolve_dispatch_introduction(source,choice)
       elif kind=='set_shifts':session.set_shifts(working)
       elif kind.startswith('resolve_growth'):getattr(session.core,kind)(cat_id,choice)
       elif kind=='resolve_intake':session.resolve_intake_request(choice)
       elif kind=='resolve_regular_introduction':session.resolve_regular_introduction(choice)
       elif kind in ('day_off','next_day','purchase_housing'):getattr(session,kind)()
       else:session.automatic_step()
      except ValueError:failed=True
      after=expected(session)
      if failed:assert before==after,(case['variant'],kind)
      case['operations'].append(dict(kind=kind,cat_id=cat_id,choice=choice,source=source,working=working,rejected=failed,expected=after))
      if not failed and kind=='resolve_activity' and source in session.core.activities['events'] and session.core.activities['events'][source].get('introduction',{}).get('status')=='waiting':
       funds=session.core.funds;cost=session.core.activities['events'][source]['introduction']['candidate']['cost'];rows=[]
       for value in (cost-1,cost,cost+.001):
        session.core.funds=value;rows.append(dict(funds=value,can_accept=not admission_reason(session.core,source)))
       session.core.funds=funds;case['operations'][-1]['admission_boundaries']=rows
     def events():
      growth_answers(session.core,record)
      if session.core.intake_request['status']=='waiting':record('resolve_intake',choice='accept' if closed else 'decline')
      if session.core.regular_introduction['status']=='waiting':record('resolve_regular_introduction',choice='decline')
     record('dispatch',cat_id='cat-sora',choice='neighborhood_visit');eid='dispatch-1-cat-sora';record('day_off');record('resolve_activity',source=eid,choice='receive');events()
     for trip in range(2):
      target='dispatch-rescue-1' if trip==1 and answer=='accept' else 'cat-mike'
      record('dispatch',cat_id=target,choice=destination);eid=f'dispatch-{session.core.day}-{target}'
      record('resolve_dispatch_introduction',source=eid,choice='accept');record('day_off')
      record('resolve_dispatch_choice',source=eid,choice='decline' if closed else 'accept');events()
      if closed:
       record('set_shifts',working=[key for key in session.core.cats if session.core.activity(key)=='cafe']);record('start_business')
       while not session.core.closed:events();record('advance_business')
      else:record('day_off')
      if trip==1:record('resolve_dispatch_introduction',source=first_id,choice=answer) # Answer replay remains a no-op while another return is pending.
      record('resolve_activity',source=eid,choice='receive');events()
      if trip==0:first_id=eid
      intro=session.core.activities['events'][eid].get('introduction')
      if closed:
       if intro:assert intro['status']=='scheduled';record('resolve_dispatch_introduction',source=eid,choice='decline')
       record('next_day');events()
      if intro:
       assert intro['status']=='waiting'
       record('day_off');record('set_shifts',working=list(session.core.cats));record('resolve_dispatch_introduction',source=eid,choice='invalid')
       if answer=='accept' and len(session.core.cats)==6:
        record('resolve_dispatch_introduction',source=eid,choice=answer);assert case['operations'][-1]['rejected'];record('purchase_housing')
       record('resolve_dispatch_introduction',source=eid,choice=answer);assert session.core.activities['events'][eid]['introduction']['status']==('accepted' if answer=='accept' else 'declined'),case['variant']
       record('resolve_dispatch_introduction',source=eid,choice=answer);record('resolve_dispatch_introduction',source=eid,choice='decline' if answer=='accept' else 'accept')
      elif trip==1 and destination=='cat_photo_studio':record('resolve_dispatch_introduction',source=eid,choice='decline')
      else:raise AssertionError(case['variant'])
      # Joined candidates participate in service and growth under their exported personalities/traits.
      working=[key for key in session.core.cats if session.core.activity(key)=='cafe' and (answer!='accept' or key.startswith('dispatch-rescue-'))]
      record('set_shifts',working=working);record('start_business')
      while not session.core.closed:events();record('advance_business')
      if answer=='accept':assert session.core.growth['cats']['dispatch-rescue-1']['service']>0,case['variant']
      events();record('next_day');events()
 return dict(cases=cases)
if __name__=='__main__':
 Path(sys.argv[1]).write_text(json.dumps(generate(),ensure_ascii=False),encoding='utf-8');print(sys.argv[1])
