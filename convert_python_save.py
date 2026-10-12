"""One-time, read-only Python save migration. The exported Player never runs Python."""
import argparse, copy, hashlib, json, subprocess, sys, tempfile, uuid
import os
from dataclasses import asdict
from datetime import datetime, timezone
from pathlib import Path

if __name__=='__main__' and sys.platform=='win32' and not sys.flags.utf8_mode:
    os.execv(sys.executable,[sys.executable,'-X','utf8',*sys.argv])

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT / 'cat_cafe_sim'))
from cat_cafe_sim.cafe_new_game import create_game, starting_conditions
from cat_cafe_sim.cafe_interaction import CafeInteractionSession
from cat_cafe_sim.storage.cafe_saves import MemoryRelationships, validate_progress
from cat_cafe_sim.storage.legacy_cafe_reader import read_game
from cat_cafe_sim.core.cafe_checkpoint import restore, outcome_result
from cat_cafe_sim.core.human_cat_relationship import RelationshipConfig, verify_relationship
from cat_cafe_sim.policies.human_cat import AutomaticInteractionPolicy

RULES = ROOT / 'cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/rules.json'
PROJECT = ROOT / 'cat_cafe_sim_tool/tests/standalone/StandaloneChecks.csproj'
TYPES = ('teaser','ball','plush','tunnel','pet','brush','voice','presence')
GROUPS = ('play','contact','quiet')

def read_json(path):
    def unique(rows):
        result = {}
        for k,v in rows:
            if k in result: raise ValueError('JSONに重複した項目があります。')
            result[k] = v
        return result
    return json.loads(Path(path).read_text(encoding='utf-8-sig'), object_pairs_hook=unique,
                      parse_constant=lambda value: (_ for _ in ()).throw(ValueError('有限でない数値があります。')))

def sha(path):
    h=hashlib.sha256()
    with Path(path).open('rb') as f:
        for block in iter(lambda:f.read(1024*1024),b''):h.update(block)
    return h.hexdigest()

def command(*args):
    result=subprocess.run(['dotnet','run','--project',str(PROJECT),'--',*map(str,args)],capture_output=True,text=True,encoding='utf-8')
    if result.returncode:raise ValueError('単独版との整合性確認で拒否しました。\n'+(result.stderr or result.stdout).strip())

def load_readonly(source, relationship=None):
    source=Path(source).resolve()
    if not source.is_file():raise ValueError('Python営業セーブが見つかりません。')
    source_hash=sha(source)
    data,legacy=read_game(source)
    fields={'kind','format_version','core','interaction_config','relationship_path','relationships','policy_version','auto_assign'}
    if set(data)!=fields or data['kind']!='cafe-save' or type(data['format_version']) is not int or data['format_version'] not in (1,2) or type(data['auto_assign']) is not bool or data['policy_version'] not in AutomaticInteractionPolicy.compatible_versions:raise ValueError('対応するPython営業セーブ形式1・2ではありません。')
    relation=Path(relationship or data['relationship_path'])
    if not relation.is_absolute() or not relation.is_file() or relation.resolve()==source:raise ValueError('対応する関係データJSONの絶対パスが必要です。移動した場合は --relationships で指定してください。')
    relation=relation.resolve();relation_hash=sha(relation);current=read_json(relation)
    core=legacy if legacy is not None else restore(data['core'])
    config=RelationshipConfig.from_dict(data['interaction_config']);store=MemoryRelationships(current)
    persisted=validate_progress(core,data['relationships'],store._read())
    if set(core.outcomes)-persisted:raise ValueError('未保存の交流結果があります。Python版で保存を完了してから変換してください。')
    session=CafeInteractionSession(core,store,config);session.persisted=persisted
    if sha(source)!=source_hash or sha(relation)!=relation_hash:raise ValueError('読取中に元データが更新されました。')
    session.legacy_hashes={source:source_hash,relation:relation_hash}
    return session,data,relation

def check_rules(session,scratch):
    from export_standalone_rules import exported_rules
    if exported_rules()!=read_json(RULES):raise ValueError('Python標準定義と単独版のルール全体が一致しません。対応する版で変換してください。')
    c=session.core;refs={}
    for mode in ('free','popularity','bond','patron'):
        refs[mode]=create_game(Path(scratch)/mode,starting_conditions(mode)).core
    if c.objective not in refs or c.roster_ids!=refs[c.objective].roster_ids or len(c.seats)<2:raise ValueError('標準の初期猫5匹・2席から始めた経営セーブが対象です。')
    ref=refs[c.objective]
    if c.config.to_dict()!=ref.config.to_dict() or asdict(c.start_state)!=asdict(ref.start_state) or asdict(c.shift_rules)!=asdict(ref.shift_rules) or asdict(c.health_rules)!=asdict(ref.health_rules) or session.interaction_config.to_dict()!=RelationshipConfig.load().to_dict():raise ValueError('営業・交流・健康・出勤ルールが単独版の標準設定と異なります。')
    required=('management','operating_cost','waiting_area','housing','store_events','growth','weekdays','customer_preferences','customer_satisfaction','customer_loyalty','customer_discontent','customer_trust','intake_request','regular_introduction','visiting_cat','pet_shop','dispatch_unlocks','dispatch_trouble','dispatch_introduction')
    for name in required:
        value,expected=getattr(c,name),getattr(ref,name)
        if value is None:raise ValueError(name+' の保存情報がありません。補完によるルール変更は行いません。')
        if isinstance(value,dict) and 'rules' in expected and value.get('rules')!=expected['rules']:raise ValueError(name+' のルールが標準設定と異なります。')
    for name in ('weekdays','dispatch_trouble','dispatch_introduction'):
        if getattr(c,name)!=getattr(ref,name):raise ValueError(name+' の設定が標準設定と異なります。')
    for name in ('advanced_customers','quiet_customer','play_customer','contact_customer','longhair_customer','vip_customer'):
        value=getattr(c,name)
        if value is not None and value!=getattr(refs['popularity'],name):raise ValueError(name+' の条件・効果が標準設定と異なります。')
        if getattr(ref,name) is not None and value is None:raise ValueError(name+' の設定が不足しています。')
    if c.reservation is not None and c.reservation['rules']!=refs['popularity'].reservation['rules']:raise ValueError('予約条件が標準設定と異なります。')
    if c.pet_shop['candidates']!=ref.pet_shop['candidates']:raise ValueError('購入候補の価格・個性が標準設定と異なります。')
    if c.goal is None:raise ValueError('人気集計の情報が必要です。')
    goalref=refs['free' if c.goal.get('tracking_only') else 'popularity'].goal
    if c.goal['rules']!=goalref['rules']:raise ValueError('人気目標のルールが標準設定と異なります。')
    if c.patron and c.patron['rules']!=refs['patron'].patron['rules']:raise ValueError('有力者目標のルールが異なります。')
    if c.bond_goal and c.bond_goal['rules']!=refs['bond'].bond_goal['rules']:raise ValueError('好感度目標のルールが異なります。')
    from cat_cafe_sim.core import cafe_equipment,cafe_waiting_area,cafe_seat_equipment
    if c.rest_space:
        r=c.rest_space
        if r['rules']!=cafe_equipment.rules():raise ValueError('休養設備の設定が異なります。')
        for key,expected in (('upgrade',cafe_equipment.upgrade_rules()),('soundproof',cafe_equipment.soundproof_rules())):
            if key in r and r[key]['rules']!=expected:raise ValueError('休養設備の強化設定が異なります。')
    for p in (c.equipment_store or {}).get('purchases',[]):
        if p['rules'] not in cafe_seat_equipment.catalog():raise ValueError('接客設備の設定が異なります。')
    if c.waiting_area.get('upgrade') and {k:c.waiting_area['upgrade'][k] for k in cafe_waiting_area.upgrade_rules()}!=cafe_waiting_area.upgrade_rules():raise ValueError('待合強化の設定が異なります。')
    from cat_cafe_sim.core import cafe_housing,cafe_recruitment,cafe_item_shop,cafe_activities,cafe_patron,cafe_dispatch_encounters
    if c.housing.get('upgrade') and c.housing['upgrade']['rules']!=cafe_housing.upgrade_rules():raise ValueError('飼育スペース強化の設定が異なります。')
    from cat_cafe_sim.core import cafe_expansion
    prices=cafe_expansion.rules();cost_keys=('cost','four_seat_cost','five_seat_cost','six_seat_cost','seven_seat_cost','eight_seat_cost')
    for purchase in cafe_expansion.purchases(c):
        if purchase['cost']!=prices[cost_keys[purchase['seats']-3]]:raise ValueError('座席増設の費用が標準設定と異なります。')
    for row in c.item_purchases:
        if dict(cost=row['cost'],item=row['item'])!=cafe_item_shop.rules():raise ValueError('購入アイテムの価格・効果が標準設定と異なります。')
    if c.recruitment:
        catalog=cafe_recruitment.catalog()
        for index,(key,row) in enumerate(c.recruitment['candidates'].items()):
            candidate=copy.deepcopy(catalog[index%len(catalog)]);batch=index//cafe_recruitment.BATCH_SIZE
            if batch:candidate['name']+='（紹介'+str(batch+1)+'）'
            if key!='rescue-'+str(index+1) or row!=candidate:raise ValueError('保護猫候補のID・順序・価格・個性が標準設定と異なります。')
    destinations=cafe_activities.destinations(c)+cafe_patron.destinations(c)
    for event in (c.activities or {}).get('events',{}).values():
        if event['destination'] not in destinations:raise ValueError('保存済み派遣の条件が標準設定と異なります。')
        if 'encounter' in event and event['encounter']['rules']!=cafe_dispatch_encounters.for_destination(event['destination'],event['started_day']):raise ValueError('派遣選択イベントの効果が標準設定と異なります。')

def exchange(interaction,seat=0):
    e=interaction.state;config=interaction.config;r=interaction.summary()
    return dict(customer_id=interaction.customer_id,cat_id=interaction.cat_id,seat=seat,mode=e['mode'],last_group=e['last_interaction_group'],previous_action=e['previous_action'],previous_reaction=e['previous_reaction'],end_reason=e['end_reason'],remaining=e['remaining_ticks'],streak=e['interaction_streak'],bonus=e['bonus_funds'],tension_multiplier=config.customer_tension_multiplier,service_group='contact' if config.contact_service else 'play' if config.play_service else 'quiet' if config.quiet_service else '',equipment_group=config.equipment_group,equipment_engagement=config.equipment_engagement_multiplier,equipment_tension=config.equipment_tension_multiplier,type_actions=[r['type_actions'][k] for k in TYPES],stamina_spent=r['stamina_spent'],growth_groups=[v for v in (config.mastery_group,config.second_mastery_group,config.third_mastery_group) if v],growth_types=[v for v in (config.type_mastery,config.second_type_mastery,config.second_group_type_mastery,config.second_group_second_type_mastery,config.third_group_type_mastery,config.third_group_second_type_mastery) if v],**{k:e[k] for k in ('stamina','engagement','tension','connect_count','open_up_count','simultaneous_count','affinity_start','affinity_pending','pause_after_interaction','connect_pending','open_up_pending')})

def growth_row(row):
    result={}
    for k,v in row.items():
        if isinstance(v,dict):
            if k.endswith('actions'):result[k]=[v[t] for t in TYPES]
            elif k.endswith('groups') or k=='groups':result[k]=[v[t] for t in GROUPS]
            else:result[k]=growth_row(v)
        else:result[k]=v or 0 if k.endswith('day') else v
    return result

def project(session,template,auto):
    c=session.core;s=copy.deepcopy(template);summary=c.summary();results=[outcome_result(v) for v in list(c.outcomes.values())[c.day_outcome_offset:]]
    from cat_cafe_sim.core import cafe_player,cafe_goal,cafe_housing,cafe_waiting_area,cafe_activities
    from cat_cafe_sim.core.cafe_customer_satisfaction import evaluate
    from cat_cafe_sim.core import cafe_advanced_customers,cafe_reservation,cafe_quiet_customer,cafe_play_customer,cafe_contact_customer,cafe_longhair_customer,cafe_vip_customer
    def specialist(r):
        value=next((v for v in (module.evaluate(c,r) for module in (cafe_advanced_customers,cafe_reservation,cafe_quiet_customer,cafe_play_customer,cafe_contact_customer,cafe_longhair_customer,cafe_vip_customer)) if v),None) if r else None
        return dict(specialist_result=('success' if value['success'] else 'failure') if value else None,specialist_bonus=value['bonus'] if value else 0,service_equipment=bool(r and any(r.get(k,False) for k in ('quiet_equipment','play_equipment','contact_equipment'))),service_count=next((value[k] for k in ('quiet_count','play_count','contact_count') if value and k in value),0))
    def record(v,r):
        return dict(id=v.id,cat_id=r['cat_id'] if r else next((i.cat_id for i in c.interactions.values() if i.customer_id==v.id),None),arrival=v.arrival_tick,seated_at=v.seated_at if v.seated_at is not None else -1,seated_ticks=v.seated_ticks,departure=v.departure_reason,bill=v.bill,completed_tick=next((e['tick'] for e in c.events if e['kind']=='interaction_completed' and e['result']['customer_id']==v.id),0),service_seat=next((int(e['seat_id'].split('-')[-1])-1 for e in c.events if e['kind']=='assigned' and e['customer_id']==v.id),-1),type_actions=[r['type_actions'][t] for t in TYPES] if r else [0]*8,affinity_delta=r.get('affinity_delta',0) if r else 0,stamina_spent=r.get('stamina_spent',0) if r else 0,**{k:r[k] if r else 0 for k in ('connect_count','open_up_count','simultaneous_count')},satisfaction=evaluate(c,r,cafe_advanced_customers.evaluate(c,r)) if r else None,**specialist(r))
    s.update(seed=c.seed,day=c.day,tick=c.tick,phase='closed' if c.closed else 'preparation' if c.can_set_shifts else 'open',auto_assignment=auto,autoplay=None,instance_id=uuid.uuid4().hex,revision=0,funds=c.funds,opening_funds=summary['opening_funds'],revenue=summary['revenue'],expenses=summary['total_expenses'],popularity=c.management['popularity'],completed=summary['completed_interactions'],game_over=(c.management['game_over'] or {}).get('reason'),seat_count=len(c.seats))
    s['cats']=[dict(id=cat.id,stamina=cat.stamina,fatigue=cat.fatigue,stress=c.management['stress'][cat.id],working=cat.id in c.working_cats,health=cat.health_status,activity=c.activity(cat.id),service_ticks=c.cat_service_ticks[cat.id],recovery_remaining=cat.recovery_days_remaining,cannot_continue=cat.cannot_continue,missing_remaining=next((e['remaining'] for e in c.management['events'].values() if e['cat_id']==cat.id and e['status']!='resolved'),0),kitten=next((e['kitten'] for e in reversed(list(c.management['events'].values())) if e['cat_id']==cat.id),False)) for cat in c.cats.values()]
    s['visits']=[record(v,next((r for r in results if r['customer_id']==v.id),None)) for v in c.visits.values()];s['queue']=list(c.queue);s['exchanges']=[exchange(i,int(key.split('-')[-1])-1) for key,i in sorted(c.interactions.items())]
    s['relationships']=[dict(cat_id=r['cat_id'],customer_id=r['customer_id'],affinity=r['affinity']) for r in session.store.list_relationships() if r['cat_id'] in c.cats and r['customer_id']!='player']
    s['growth']=[dict(cat_id=k,**growth_row(v)) for k,v in c.growth['cats'].items()];s['growth_operations']=[];s['resting_cats']=[k for k in c.cats if c.closed and c.activity(k)=='cafe' and k not in c.working_cats]
    p=cafe_player.state(c);s['player_cats']=[dict(cat_id=k,affinity=p['affinity'][k],today=p['today'][k],total=p['total'][k]) for k in c.cats];s['player_sessions']=[]
    if cafe_player.active(c):
        log=cafe_player.active(c);i=verify_relationship(log);initial=log['initial_relationship'];s['player_sessions']=[dict(id=initial['session_id'],cat_id=i.cat_id,day=c.day,ordinal=int(initial['session_id'].split('-')[-1]),initial_revision=initial['revision'],initial_affinity=initial['affinity'],initial_stamina=log['initial_stamina'],manual=False,exchange=exchange(i),actions=[dict(action=a['action'],target=a['target_type']) for a in log['records']],stress_before=0,stress_after=0)]
    s['bond_goal']=dict(id='bond',started_day=c.bond_goal['started_day'],status=c.bond_goal['status'],continued=c.bond_goal['continued'],resolved_day=c.bond_goal['resolved_day'] or 0,achieved_cats=c.bond_goal['achieved_cats']) if c.bond_goal else None
    s['goal']=[] if c.goal.get('tracking_only') else [dict(started_day=g['started_day'],status=g['status'],resolved_day=g['resolved_day'] or 0,continued=g['continued']) for g in c.goal.get('history',[])]+[dict(started_day=cafe_goal.current_start(c.goal),status=c.goal['status'],resolved_day=c.goal['resolved_day'] or 0,continued=c.goal['continued'])]
    s['challenge_started_day']=c.goal.get('challenge_started_day',0);s['popularity_gain']=next((d['gain'] for d in c.goal['days'] if d['day']==c.day),0);s['goal_popularity']=c.management['popularity'] if c.closed else 0
    s['patron']=dict(id='patron',started_day=c.patron['started_day'],status=c.patron['status'],resolved_day=c.patron['resolved_day'] or 0,continued=c.patron['continued'],satisfaction=c.patron['satisfaction'],members=[dict(id=k,value=v) for k,v in c.patron['members'].items()]) if c.patron else None
    s['clear_results']=[dict(mode=k,**v) for k,v in c.clear_results.items()]
    for name in ('intake_request','regular_introduction'):
        v=getattr(c,name);s[name]={k:(v[k] or 0) if k.endswith('day') else v[k] for k in s[name]}
    s['visiting_cat']={k:copy.deepcopy(c.visiting_cat[k]) for k in ('status','actions','presented_day','accepted_day')};s['visiting_cat']['presented_day']=s['visiting_cat']['presented_day'] or 0;s['visiting_cat']['accepted_day']=s['visiting_cat']['accepted_day'] or 0
    s['shop_purchases']=[dict(cat_id=k,day=v) for k,v in c.pet_shop['accepted'].items()]
    s['recruitment']=dict(id='recruitment',days=sorted(set(c.recruitment.get('presented_days',{}).values())) or [c.recruitment['opened_day']],accepted=[dict(cat_id=k,day=v) for k,v in c.recruitment['accepted'].items()]) if c.recruitment else None
    s['adoption']=dict(id='adoption',enabled=c.adoption['enabled'],events=[dict(e,source='service-'+str(e['day'])+'-'+e['cat_id']+'-'+e['customer_id'],resolved_day=e['resolved_day'] or 0,resolved_tick=e['resolved_tick'] or 0) for e in c.adoption['events'].values()]) if c.adoption else None
    s['store_events']=copy.deepcopy(c.store_events['days']);r=c.reservation['request'] if c.reservation else None;s['reservation']={k:(r[k] or 0) if k.endswith('day') else r[k] for k in s['reservation']} if r else s['reservation']
    for key in ('loyalty','discontent'):s[key]=[dict(customer_id=k,**{field:(value or 0) if field.endswith('day') else copy.deepcopy(value) for field,value in v.items()}) for k,v in getattr(c,'customer_'+key)['customers'].items()]
    s['trust_customers']=[dict(customer_id=k,**{field:(value or 0) if field.endswith('day') else value for field,value in v.items()}) for k,v in c.customer_trust['customers'].items()];s['trust_events']=[dict(v,resolved_day=v['resolved_day'] or 0) for v in c.customer_trust['events'].values()]
    s['housing']=dict(purchased_day=(c.housing['purchase'] or {}).get('day',0),upgraded_day=c.housing.get('upgrade',{}).get('day',0));r=c.rest_space or {};s['rest_space']=dict(purchased_day=r.get('day',0),upgraded_day=r.get('upgrade',{}).get('day',0),soundproofed_day=r.get('soundproof',{}).get('day',0));r=c.waiting_area;s['waiting_area']=dict(purchased_day=(r['purchase'] or {}).get('day',0),upgraded_day=r.get('upgrade',{}).get('day',0))
    from cat_cafe_sim.core import cafe_expansion
    s['expansion']=copy.deepcopy(cafe_expansion.purchases(c));s['equipment_purchases']=[dict(id=p['id'],kind=p['rules']['id'],day=p['day']) for p in (c.equipment_store or {}).get('purchases',[])];s['equipment_moves']=[];s['equipment_placements']=[dict(seat=int(k.split('-')[-1])-1,item=v.equipment) for k,v in c.seats.items() if v.equipment]
    s['item_purchases']=[dict(source=v['source'],day=v['day'],cost=v['cost'],item_id=v['item']['id']) for v in c.item_purchases];s['item_sales']=copy.deepcopy(c.item_sales)
    from cat_cafe_sim.core.cafe_items import reward_for_source,stat
    s['item_uses']=[]
    for u in c.item_uses:
        mode=stat(reward_for_source(c,u['source'])['item']);a,b=u['before'],u['after'];s['item_uses'].append(dict(source=u['source'],cat_id=u['cat_id'],day=u['day'],before=a.get('stress',0) if mode=='both' else a if mode=='stress' else 0,after=b.get('stress',0) if mode=='both' else b if mode=='stress' else 0,before_fatigue=a.get('fatigue',0) if mode=='both' else a if mode=='fatigue' else 0,after_fatigue=b.get('fatigue',0) if mode=='both' else b if mode=='fatigue' else 0))
    s['dispatch_unlocks']=[dict(id=k,**v) for k,v in c.dispatch_unlocks['unlocked'].items()];s['dispatches']=[]
    for e in (c.activities or {}).get('events',{}).values():
        w=e.get('welcome_match',{});r=dict(id=e['id'],cat_id=e['cat_id'],destination=e['destination']['id'],started_day=e['started_day'],remaining=e['remaining'],status=e['status'],occurred_day=e['occurred_day'] or 0,resolved_day=e['resolved_day'] or 0,choice=e['choice'],feature_matched=w.get('feature_matched',False),specialization_matched=w.get('specialization_matched',False),growth_multiplier=e.get('growth_multiplier',1),reward_bonus=w.get('reward_bonus',0),reward=cafe_activities.reward(c,{k:v for k,v in e.items() if k not in ('encounter','trouble')}),return_stress=(c.traits or {}).get(e['cat_id'],{}).get('return_stress',0))
        for name in ('trouble','introduction','encounter','patron_match'):
            if name not in e:continue
            v=e[name]
            if name=='trouble':r[name]=dict(destination=e['destination']['id'],**{k:(v[k] or 0) if k.endswith('day') else v[k] for k in ('stress','probability','draw','status','missing_day','choice','choice_day','cost')})
            elif name=='introduction':r[name]=dict(cat_id=v['cat_id'],name=v['candidate']['name'],cost=v['candidate']['cost'],status=v['status'],presented_day=v['presented_day'] or 0,resolved_day=v['resolved_day'] or 0)
            elif name=='encounter':r[name]=dict(id=v['rules']['id'],status=v['status'],choice=v['choice'],occurred_day=v['occurred_day'] or 0,resolved_day=v['resolved_day'] or 0,**{k:(v.get('changes') or {}).get(k,0) for k in ('fatigue_before','fatigue_after','stress_before','stress_after')})
            else:r[name]=copy.deepcopy(v)
        s['dispatches'].append(r)
    finance={'equipment_expenses':'seat_equipment_expenses','rest_space_expenses':'equipment_expenses'}
    for k in ('pet_shop_expenses','recruitment_expenses','housing_expenses','item_expenses','item_sales_income','expansion_expenses','equipment_expenses','rest_space_expenses','waiting_area_expenses','dispatch_income','dispatch_trouble_expenses'):s[k]=summary.get(finance.get(k,k),0)
    s['housing_cost']=cafe_housing.daily_cost(c) if c.closed else 0;s['waiting_area_cost']=cafe_waiting_area.daily_cost(c) if c.closed else 0
    s['history']=[]
    for row in c.day_results:
        v=row['summary'];counts=v['customer_satisfaction'];h=dict(day=row['day'],arrivals=v['arrivals'],completed=v['completed_interactions'],seat_count=v['seat_count'],opening_funds=v['opening_funds'],revenue=v['revenue'],expenses=v['total_expenses'],closing_funds=v['closing_funds'],popularity_gain=v['popularity_gain'],goal_popularity=v['popularity'],housing_cost=next(charge['housing_cost'] for charge in c.operating_cost['charges'] if charge['day']==row['day']),waiting_area_cost=cafe_waiting_area.daily_cost(c,row['day']),satisfied=counts['satisfied'],normal=counts['normal'],dissatisfied=counts['dissatisfied'],resting_cats=[k for k,r in row['cats'].items() if r.get('experience_gain',{}).get('rest',0)>0],customer_records=[],legacy_service_ticks=[dict(cat_id=k,ticks=r['service_ticks']) for k,r in row['cats'].items()])
        for k in finance:h[k]=v.get(finance[k],0)
        for k in ('pet_shop_expenses','recruitment_expenses','housing_expenses','item_expenses','item_sales_income','expansion_expenses','waiting_area_expenses','dispatch_income','dispatch_trouble_expenses'):h[k]=v.get(k,0)
        s['history'].append(h)
    return s

def definitions(session,rules):
    c=session.core;rows=[]
    for key in c.cats:
        p=session.profiles[key]['personality'];trait=(c.traits or {}).get(key,{})
        rows.append(dict(id=key,name=session.profiles[key]['name'],trait_id=trait.get('id'),features=c.cat_features[key],type_preferences=[p['type_preferences'][t['id']] for t in rules['types']],intensity_preferences=[p['intensity_preferences'][k] for k in ('gentle','standard','active')],boredom_decay=p['boredom_decay'],switch_affinity=p['switch_affinity'],**{k:trait.get(k,1) for k in ('service_stress','service_fatigue','rest_fatigue','dispatch_reward')},return_stress=trait.get('return_stress',0)))
    return rows

def convert(source,target,relationship=None):
    source,target=Path(source).resolve(),Path(target).resolve()
    if target.exists() or source==target:raise ValueError('変換先は存在しない別名を指定してください。')
    target.parent.mkdir(parents=True,exist_ok=True);session,data,relation=load_readonly(source,relationship)
    if target==relation:raise ValueError('関係データへの上書きはできません。')
    hashes=session.legacy_hashes
    with tempfile.TemporaryDirectory(prefix='legacy-convert-',dir=target.parent) as scratch:
        check_rules(session,scratch);templates=Path(scratch)/'templates.json';command('--legacy-template',RULES,templates);template=next(s for s in read_json(templates) if s['objective']==session.core.objective);rules=read_json(RULES)
        bundle=dict(kind='cat-cafe-unity-import',version=1,source_version=data['format_version'],rules_id=rules['rules_id'],source_path=str(source),source_hash=hashes[source],relationship_path=str(relation),relationship_hash=hashes[relation],converted_at=datetime.now(timezone.utc).isoformat(),state=project(session,template,data['auto_assign']),definitions=definitions(session,rules),notes=['移行前の操作履歴は再構成せず、現在状態を移行起点として保持します。','画像設定・Python営業ログ・おまかせ結果ファイルは移行対象外です。'])
        draft=Path(scratch)/'draft.json';draft.write_text(json.dumps(bundle,ensure_ascii=False,allow_nan=False),encoding='utf-8');verified=Path(scratch)/'verified.json';command('--finalize-import',RULES,draft,verified)
        if verified.stat().st_size>32*1024*1024:raise ValueError('変換済み取込JSONは32MB以下にしてください。出力していません。')
        if any(sha(p)!=h for p,h in hashes.items()):raise ValueError('変換中に元データが更新されました。出力していません。')
        import os
        temp=target.with_name(target.name+'.tmp-'+uuid.uuid4().hex)
        try:
            with temp.open('xb') as output:output.write(verified.read_bytes());output.flush();os.fsync(output.fileno())
            # Windows rename never overwrites an existing destination.
            if sys.platform!='win32':os.link(temp,target);temp.unlink()
            else:temp.rename(target)
        finally:
            if temp.exists():temp.unlink()
    return target

if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('source',type=Path);parser.add_argument('target',type=Path);parser.add_argument('--relationships',type=Path);args=parser.parse_args()
    try:print(convert(args.source,args.target,args.relationships))
    except (ValueError,OSError,KeyError,TypeError) as error:parser.exit(1,str(error)+'\n')
