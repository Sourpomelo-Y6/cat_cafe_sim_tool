"""Instrument original AutoPlayer normal operations; never reimplement its decisions."""
import copy,json,sys,tempfile
from pathlib import Path
from dataclasses import replace
from generate_reference import ROOT
from generate_patron_cases import expected as patron_expected
from generate_expansion_cases import expected as goal_expected
from export_standalone_rules import exported_rules
from cat_cafe_sim.cafe_new_game import create_game,starting_conditions
from cat_cafe_sim.cafe_autoplay import AutoPlayer
from cat_cafe_sim.core import cafe_player,cafe_waiting_area
from cat_cafe_sim.unity_autoplay import start,advance

def generate(seed_filter=None):
 cases=[]
 for target in ('popularity','bond','patron'):
  for mode in ('basic','clear','fast'):
   for seed in ((0,42,100,101,102,103) if target=="popularity" else (0,42)):
    if seed_filter is not None and seed not in seed_filter:continue
    with tempfile.TemporaryDirectory(dir=ROOT/'Logs/Standalone') as temp:
     rules=exported_rules();selected=starting_conditions(target)
     rules['store_events']['probability']=selected['store_events']['probability']=0
     if seed==100:rules['starting_funds']=selected['management']['starting_funds']=20000
     if seed in (101,102):
      stages=[dict(days=20,target=k) for k in (101,200,300)];rules['goal_stages']=stages;selected['goal'].update(stages[0],stages=stages[1:])
     if seed==102:
      stages=[dict(days=1,target=k) for k in (600,625,650)];rules['goal_stages']=stages;selected['goal'].update(stages[0],stages=stages[1:])
     if seed==103:rules['starting_funds']=selected['management']['starting_funds']=1
     s=create_game(Path(temp),selected,seed=seed);c=s.core;ops=[];depth=[0]
     if seed==103:
      rules.update(opening_ticks=1,arrival_ticks=[0]);c.config=replace(c.config,opening_ticks=1,arrival_ticks=(0,))
     case=dict(variant=f'autoplay-{target}-{mode}-{seed}',rules=rules,objective=target,mode=mode,days=3,seed=seed,operations=ops);cases.append(case)
     def expected():
      nonlocal c
      c=s.core
      data=patron_expected(s);data.update(goal_expected(s))
      if c.goal.get('tracking_only'):data['goal']=[]
      p=cafe_player.state(c);data['player_cats']=[dict(cat_id=k,affinity=p['affinity'][k],today=p['today'][k],total=p['total'][k]) for k in c.cats]
      if c.bond_goal:data['bond_goal']={k:c.bond_goal[k] for k in ('status','continued','achieved_cats','started_day')};data['bond_goal']['resolved_day']=c.bond_goal['resolved_day'] or 0
      rest=c.rest_space or {};data['rest_space']=dict(purchased_day=rest.get('day',0),upgraded_day=rest.get('upgrade',{}).get('day',0),soundproofed_day=rest.get('soundproof',{}).get('day',0));data['rest_space_expenses']=c.summary().get('equipment_expenses',0)
      area=c.waiting_area;data['waiting_area']=dict(purchased_day=(area.get('purchase') or {}).get('day',0),upgraded_day=area.get('upgrade',{}).get('day',0));data['waiting_area_expenses']=cafe_waiting_area.expenses(c,c.day);data['waiting_area_cost']=cafe_waiting_area.daily_cost(c) if c.closed else 0
      return data
     def wrap(name):
      original=getattr(s,name)
      def execute(*args,**kwargs):
       nonlocal c
       c=s.core
       top=depth[0]==0;depth[0]+=1
       op=dict(kind=name,working=None,cat_id=None,choice=None,source=None,seat_id=None)
       if name=='automatic_step':op['kind']='start_business' if c.can_set_shifts else 'advance_business'
       elif name=='set_shifts':op['working']=sorted(args[0])
       elif name=='play_with_player':op.update(kind='player_begin',cat_id=args[0])
       elif name=='player_command':op.update(kind='player_finish' if kwargs.get('finish') else 'player_step',choice=args[0] if args else None,source=args[1] if len(args)>1 else None)
       elif name=='dispatch':op.update(cat_id=args[0],choice=args[1]['id'])
       elif name=='recruit_cat':op['cat_id']=args[0]
       elif name=='purchase_seat_equipment':op.update(seat_id=args[0],choice=args[1]['id'])
       try:result=original(*args,**kwargs)
       finally:depth[0]-=1
       if top:op['expected']=expected();ops.append(op)
       return result
      setattr(s,name,execute)
     for name in ('automatic_step','set_shifts','day_off','next_day','purchase_rest_space','upgrade_rest_space','soundproof_rest_space','expand_seats','purchase_waiting_area','upgrade_waiting_area','purchase_seat_equipment','play_with_player','player_command','open_recruitment','recruit_cat','dispatch'):wrap(name)
     run=start(s,target,mode,3,False)
     for _ in range(10000):
      advance(s,run,batch_size=1)
      if not run['running']:break
     else:raise RuntimeError(case['variant'])
     case['reason']=run['reason'];case['completed_days']=run['days'];case['operation_count']=run['operations'];case['expected']=expected()
 return dict(cases=cases)
if __name__=='__main__':
 path=Path(sys.argv[1]);path.parent.mkdir(parents=True,exist_ok=True);path.write_text(json.dumps(generate(),ensure_ascii=False),encoding='utf-8')
