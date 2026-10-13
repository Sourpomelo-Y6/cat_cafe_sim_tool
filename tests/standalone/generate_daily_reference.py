"""Original Python results from fresh games; only temporary verification files are written."""
import json,sys,tempfile
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'cat_cafe_sim'))
from cat_cafe_sim.cafe_new_game import create_game,starting_conditions
from cat_cafe_sim.cafe_cat_details import cat_details
output=Path(sys.argv[1]);output.parent.mkdir(parents=True,exist_ok=True);cases=[]
with tempfile.TemporaryDirectory(dir=output.parent) as folder:
 for seed in range(8):
  for mode in ('work','rest','off','recover','recovering','risk','dispatch','received','item','purchase','runaway'):
   game=create_game(Path(folder)/f'{seed}-{mode}',starting_conditions('free'),seed=seed)
   # Standard initial preparation has no unanswered event for these seeds except rain (automatic).
   core=game.core
   from cat_cafe_sim.core.cafe_store_events import waiting
   event=waiting(core)
   choice=('repair' if event['type']=='trouble' else 'decline') if event else None
   if choice:game.resolve_store_event(choice)
   if mode in ('recover','recovering','risk','item'):
    for id,cat in core.cats.items():
     cat.fatigue=80;core.initial_fatigue[id]=80
     if mode not in ('risk','item'):
      cat.health_status='sick';cat.recovery_days_remaining=1 if mode=='recover' else 2
      core.initial_health[id]=dict(status='sick',remaining=cat.recovery_days_remaining)
   if mode=='runaway':
    for id in core.cats:core.management['stress'][id]=80
   if mode=='item':
    rules=json.loads((ROOT/'cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/rules.json').read_text())
    selected=next(r for r in rules['item_shop'] if r['item'].get('fatigue_relief',0)>0)
    game.purchase_item(dict(cost=selected['cost'],item=selected['item']));game.use_item('item-purchase-1','cat-mike')
   if mode in ('dispatch','received'):game.dispatch('cat-sora')
   if mode=='purchase':
    rules=json.loads((ROOT/'cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/rules.json').read_text());game.purchase_cat(rules['pet_shop'][0]['cat']['id']);core=game.core
   if mode=='rest':game.set_shifts(['cat-mike','cat-tama'])
   if mode in ('off','recover','recovering','risk'):
    try:game.day_off()
    except ValueError as exc:
     cases.append(dict(seed=seed,mode=mode,skip=str(exc)));continue
   else:
    for step in range(200):
     if core.closed:break
     game.automatic_step()
    assert core.closed,(seed,mode,"did not close")
   if mode=='received':
    event=next(iter(core.activities['events'].values()));game.resolve_activity(event['id'])
   cats=[dict(id=id,text='\n'.join(f'{label}：{value}' for label,value in zip(('日目','営業区分','予定','接客件数','接客行動数','体力消耗','疲労変化','体調変化','獲得経験','親しみ増減合計'),cat_details(game,id)['history'][0]))) for id in core.cats]
   cases.append(dict(seed=seed,mode=mode,choice=choice,cats=cats))
output.write_bytes(json.dumps(dict(cases=cases),ensure_ascii=False).encode('utf-8'))
print('Python daily cases',len(cases),'comparable',sum('cats' in c for c in cases))
