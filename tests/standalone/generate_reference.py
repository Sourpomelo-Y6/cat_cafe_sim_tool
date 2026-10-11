"""Generate comparison traces by executing original Python cores (no HTTP)."""
import json
import copy
import random
import sys
import tempfile
from dataclasses import asdict, replace
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
sys.path[:0] = [str(ROOT / 'cat_cafe_sim'), str(ROOT / 'cat_cafe_sim_tool')]
from cat_cafe_sim.cafe_new_game import starting_conditions
from cat_cafe_sim.cafe_interaction import CafeInteractionSession
from cat_cafe_sim.core.config import Config
from cat_cafe_sim.core.cafe_customer_satisfaction import evaluate as evaluate_satisfaction
from cat_cafe_sim.core.human_cat_relationship import RelationshipConfig, RelationshipInteraction
from cat_cafe_sim.storage.relationships import RelationshipStore
from export_standalone_rules import exported_rules



GROWTH_TYPES=('teaser','ball','plush','tunnel','pet','brush','voice','presence')
GROWTH_GROUPS=('play','contact','quiet')
def growth_row(row):
    result={}
    for key,value in row.items():
        if isinstance(value,dict):
            if key.endswith('actions') or key=='type_actions': result[key]=[value[k] for k in GROWTH_TYPES]
            elif key.endswith('groups') or key=='groups': result[key]=[value[k] for k in GROWTH_GROUPS]
            else: result[key]=growth_row(value)
        else: result[key]=(value or 0) if key.endswith('day') else value
    return result

def growth_answers(core, record, specialization='service', practice=False):
    from cat_cafe_sim.core import cafe_growth as g
    while True:
        pending=g.pending(core)
        if pending: record('resolve_growth',cat_id=pending[0],choice=specialization); continue
        pending=g.mastery_pending(core)
        if pending: record('resolve_growth_mastery',cat_id=pending[0],choice=g.mastery_choices(core,pending[0])[0]); continue
        pending=g.type_mastery_pending(core)
        if pending: record('resolve_growth_type_mastery',cat_id=pending[0],choice=g.type_mastery_choices(core,pending[0])[0]); continue
        break
    if not practice: return
    try: core.require_events_resolved()
    except ValueError: return
    for id,row in core.growth['cats'].items():
        kinds=[]
        if row.get('second_mastery') and 'second_group_type_practice' not in row: kinds.append('begin_second_group_type_practice')
        if row.get('second_group_type_practice',{}).get('type_mastery') and 'second' not in row['second_group_type_practice']: kinds.append('begin_second_group_second_type_practice')
        if row.get('second_mastery') and 'third_group_practice' not in row: kinds.append('begin_third_group_practice')
        if row.get('third_group_practice',{}).get('mastery') and 'individual' not in row['third_group_practice']: kinds.append('begin_third_group_type_practice')
        if row.get('third_group_practice',{}).get('individual',{}).get('type_mastery') and 'second' not in row['third_group_practice']['individual']: kinds.append('begin_third_group_second_type_practice')
        for kind in kinds: record(kind,cat_id=id)


def exchange(s, seat=0):
    return dict(service_group="contact" if s.config.contact_service else "play" if s.config.play_service else "quiet" if s.config.quiet_service else "",equipment_group=s.config.equipment_group,equipment_engagement=s.config.equipment_engagement_multiplier,equipment_tension=s.config.equipment_tension_multiplier,type_actions=[s.summary()['type_actions'][k] for k in GROWTH_TYPES], stamina_spent=s.summary()['stamina_spent'], growth_groups=[v for v in (s.config.mastery_group,s.config.second_mastery_group,s.config.third_mastery_group) if v], growth_types=[v for v in (s.config.type_mastery,s.config.second_type_mastery,s.config.second_group_type_mastery,s.config.second_group_second_type_mastery,s.config.third_group_type_mastery,s.config.third_group_second_type_mastery) if v], customer_id=s.customer_id, cat_id=s.cat_id, seat=seat, mode=s.state['mode'],
                last_group=s.state['last_interaction_group'], previous_action=s.state['previous_action'],
                previous_reaction=s.state['previous_reaction'], end_reason=s.state['end_reason'],
                remaining=s.state['remaining_ticks'], streak=s.state['interaction_streak'],
                **{k: s.state[k] for k in ('stamina', 'engagement', 'tension', 'connect_count', 'open_up_count', 'simultaneous_count', 'affinity_start', 'affinity_pending', 'pause_after_interaction', 'connect_pending', 'open_up_pending')},
                bonus=s.state['bonus_funds'], tension_multiplier=s.config.customer_tension_multiplier)


def specialist_snapshot(core, result):
    from cat_cafe_sim.core.cafe_advanced_customers import evaluate as advanced
    from cat_cafe_sim.core.cafe_reservation import evaluate as reservation
    from cat_cafe_sim.core.cafe_quiet_customer import evaluate as quiet
    from cat_cafe_sim.core.cafe_play_customer import evaluate as play
    from cat_cafe_sim.core.cafe_contact_customer import evaluate as contact
    value=(advanced(core,result) or reservation(core,result) or quiet(core,result) or play(core,result) or contact(core,result)) if result else None
    return dict(specialist_result=('success' if value['success'] else 'failure') if value else None,specialist_bonus=value['bonus'] if value else 0,service_equipment=bool(result and any(result.get(k,False) for k in ('quiet_equipment','play_equipment','contact_equipment'))),service_count=next((value[k] for k in ('quiet_count','play_count','contact_count') if value and k in value),0))


def snapshot(session):
    c = session.core
    from cat_cafe_sim.core.cafe_advanced_customers import evaluate as evaluate_advanced
    from cat_cafe_sim.core.cafe_checkpoint import outcome_result
    results = [outcome_result(r) for r in list(c.outcomes.values())[c.day_outcome_offset:]]
    summary = c.summary()
    intake = c.intake_request
    extra = dict(intake_request=dict(status=intake['status'], presented_day=intake['presented_day'] or 0, resolved_day=intake['resolved_day'] or 0), recruitment_expenses=summary.get('recruitment_expenses', 0)) if intake else {}
    regular = c.regular_introduction
    if regular: extra['regular_introduction'] = dict(status=regular['status'], customer_id=regular['customer_id'], **{key:regular[key] or 0 for key in ('regular_day','presented_day','resolved_day')})
    if c.customer_trust:
        extra['trust_customers'] = [dict(customer_id=key, **{k:(value or 0) if k=='departed_day' else value for k,value in row.items()}) for key,row in c.customer_trust['customers'].items()]
        extra['trust_events'] = [dict(row, resolved_day=row['resolved_day'] or 0) for row in c.customer_trust['events'].values()]
    if c.item_purchases or c.item_sales or c.item_uses:
        extra['item_purchases']=[dict(source=p['source'],day=p['day'],cost=p['cost'],item_id=p['item']['id']) for p in c.item_purchases]
        extra['item_sales']=copy.deepcopy(c.item_sales)
        from cat_cafe_sim.core.cafe_items import reward_for_source,stat
        def item_use(u):
            item=reward_for_source(c,u['source'])['item'];mode=stat(item)
            before=u['before'];after=u['after']
            return dict(source=u['source'],cat_id=u['cat_id'],day=u['day'],before=before.get('stress',0) if mode=='both' else before if mode=='stress' else 0,after=after.get('stress',0) if mode=='both' else after if mode=='stress' else 0,
                        before_fatigue=before.get('fatigue',0) if mode=='both' else before if mode=='fatigue' else 0,after_fatigue=after.get('fatigue',0) if mode=='both' else after if mode=='fatigue' else 0)
        extra['item_uses']=[item_use(u) for u in c.item_uses]
    if c.reservation:
        r=c.reservation['request'];extra['reservation']={key:(r[key] or 0) if key.endswith('day') else r[key] for key in ('status','choice','result','offered_day','visit_day','resolved_day')} if r else dict(status='untriggered',choice=None,result=None,offered_day=0,visit_day=0,resolved_day=0)
    extra['equipment_purchases']=[dict(id=p['id'],kind=p['rules']['id'],day=p['day']) for p in (c.equipment_store or {}).get('purchases',[])]
    extra['equipment_placements']=[dict(seat=int(k.split('-')[-1])-1,item=v.equipment) for k,v in c.seats.items() if v.equipment]
    extra['equipment_expenses']=summary.get('seat_equipment_expenses',0)
    extra['seat_count']=len(c.seats);extra['expansion_expenses']=summary.get('expansion_expenses',0)
    if c.expansion: extra['expansion']=copy.deepcopy(c.expansion['purchases'])
    extra['item_expenses']=summary.get('item_expenses',0);extra['item_sales_income']=summary.get('item_sales_income',0)
    if c.housing:
        extra['housing'] = dict(purchased_day=(c.housing['purchase'] or {}).get('day',0), upgraded_day=c.housing.get('upgrade',{}).get('day',0))
        extra['housing_expenses'] = summary.get('housing_expenses',0)
        extra['history'] = [dict(day=row['day'],equipment_expenses=row['summary'].get('seat_equipment_expenses',0),seat_count=row['summary'].get('seat_count',2),expansion_expenses=row['summary'].get('expansion_expenses',0), housing_expenses=row['summary'].get('housing_expenses',0), item_expenses=row['summary'].get('item_expenses',0),item_sales_income=row['summary'].get('item_sales_income',0),housing_cost=next(charge['housing_cost'] for charge in c.operating_cost['charges'] if charge['day']==row['day'])) for row in c.day_results]
        from cat_cafe_sim.core.cafe_housing import daily_cost
        extra['housing_cost'] = daily_cost(c) if c.closed else 0
    if c.growth: extra['growth']=[dict(cat_id=id,**growth_row(row)) for id,row in c.growth['cats'].items()]
    return dict(**extra, seed=c.seed, day=c.day, tick=c.tick, phase='closed' if c.closed else 'preparation' if c.can_set_shifts else 'open',
                funds=c.funds, opening_funds=summary['opening_funds'], revenue=summary['revenue'], expenses=summary['total_expenses'],
                popularity=c.management['popularity'], completed=summary['completed_interactions'], game_over=(c.management['game_over'] or {}).get('reason'),
                cats=[dict(id=cat.id, stamina=cat.stamina, fatigue=cat.fatigue, stress=c.management['stress'][cat.id], working=cat.id in c.working_cats,
                           health=cat.health_status, activity=c.activity(cat.id), service_ticks=c.cat_service_ticks[cat.id],
                           recovery_remaining=cat.recovery_days_remaining, cannot_continue=cat.cannot_continue,
                           missing_remaining=next((e['remaining'] for e in c.management['events'].values() if e['cat_id']==cat.id and e['status']!='resolved'),0),
                           kitten=next((e['kitten'] for e in reversed(list(c.management['events'].values())) if e['cat_id']==cat.id),False)) for cat in c.cats.values()],
                visits=[dict(id=v.id, cat_id=next((e.cat_id for e in c.interactions.values() if e.customer_id == v.id),
                                              next((r['cat_id'] for r in results if r['customer_id'] == v.id), None)),
                             **specialist_snapshot(c,next((r for r in results if r["customer_id"]==v.id),None)), arrival=v.arrival_tick, seated_at=v.seated_at if v.seated_at is not None else -1, seated_ticks=v.seated_ticks, departure=v.departure_reason, bill=v.bill,
                             type_actions=next(([r['type_actions'][k] for k in GROWTH_TYPES] for r in results if r['customer_id']==v.id),[0]*8),connect_count=next((r["connect_count"] for r in results if r["customer_id"]==v.id),0),open_up_count=next((r["open_up_count"] for r in results if r["customer_id"]==v.id),0),
                             satisfaction=next((evaluate_satisfaction(c, r, evaluate_advanced(c,r)) for r in results if r['customer_id']==v.id), None)) for v in c.visits.values()],
                loyalty=[dict(customer_id=key, score=row['score'], regular_day=row['regular_day'] or 0) for key,row in c.customer_loyalty['customers'].items()],
                discontent=[dict(customer_id=key, **copy.deepcopy(row)) for key,row in c.customer_discontent['customers'].items()],
                store_events=copy.deepcopy(c.store_events['days']),
                popularity_gain=next((r['gain'] for r in c.goal['days'] if r['day']==c.day), 0),
                queue=list(c.queue), exchanges=[exchange(e, int(key.split('-')[-1])-1) for key, e in sorted(c.interactions.items())])


def generate():
    data = dict(draws=[], scenarios=[], exchanges=[])
    for seed in (0, 1, 42):
        print(f"Generating seed {seed}", flush=True)
        for day in (1, 3, 28, 200):
            for cat in ('cat-mike', 'cat-tama', 'cat-kohaku'):
                key = json.dumps(['cafe-health-v1', seed, day, cat], ensure_ascii=True)
                data['draws'].append(dict(key=key, value=random.Random(key).random()))
        with tempfile.TemporaryDirectory(dir=ROOT / 'Logs/Standalone') as temp:
            selected = starting_conditions('free')
            store = RelationshipStore(Path(temp) / 'relationships.json'); store._write(selected['profiles'])
            session = CafeInteractionSession(store=store, seat_count=2, seed=seed)
            core = session.core
            core.initialize_weekdays(selected['weekdays']); core.initialize_traits(selected['traits'])
            core.initialize_preferences(selected['features'], selected['preferences'])
            session.enable_management(selected['management']); core.initialize_operating_cost(selected['operating_cost'])
            core.initialize_customer_satisfaction(selected['customer_satisfaction'])
            goal = dict(selected['goal']); goal.pop('stages', None); core.enable_goal(goal, tracking_only=True)
            core.initialize_customer_loyalty(selected['customer_loyalty']); core.initialize_customer_discontent(selected['customer_discontent'])
            core.initialize_growth(selected['growth'])
            core.initialize_customer_trust(selected['customer_trust'])
            core.initialize_intake_request(selected['intake_request'])
            core.initialize_regular_introduction(selected['regular_introduction'])
            core.initialize_housing(selected['housing'])
            core.initialize_store_events(selected['store_events'])
            # Only modules implemented in this first slice are enabled on BOTH sides.
            scenario = dict(seed=seed, operations=[]); data['scenarios'].append(scenario)
            def record(kind, working=None, cat_id=None, choice=None, source=None):
                nonlocal core
                if kind == 'resolve_store_event':
                    if choice == 'close': session.day_off()
                    else: session.resolve_store_event(choice)
                elif kind == 'resolve_customer_trust': core.resolve_customer_trust(source, choice)
                elif kind.startswith('resolve_growth') or kind.startswith('begin_'): getattr(core,kind)(cat_id,choice) if kind.startswith('resolve_') else getattr(core,kind)(cat_id)
                elif kind == 'resolve_regular_introduction': session.resolve_regular_introduction(choice)
                elif kind == 'resolve_intake': session.resolve_intake_request(choice)
                elif kind == 'use_item': core.use_item(source, cat_id)
                elif kind == 'purchase_item':
                    from cat_cafe_sim.core.cafe_item_shop import catalog
                    session.purchase_item(next(r for r in catalog() if r['item']['id']==choice))
                elif kind == 'sell_item': session.sell_item(source)
                elif kind in ('purchase_housing','upgrade_housing','expand_seats'): getattr(core,kind)()
                elif kind == 'set_shifts': session.set_shifts(working)
                elif kind == 'next_day': session.next_day()
                elif kind == 'day_off': session.day_off()
                elif kind == 'resolve_missing': session.resolve_missing(next(e['id'] for e in core.management['events'].values() if e['cat_id']==cat_id and e['status']=='waiting'))
                else: session.automatic_step()
                core = session.core  # Joining replaces the Python core atomically.
                scenario['operations'].append(dict(kind=kind, **({'working': working} if working is not None else {}), **({'cat_id': cat_id} if cat_id else {}), **({'choice': choice} if choice else {}), **({'source': source} if source else {}), expected=snapshot(session)))
            for day in range(1, 29):
                if day % 7 == 0: print(f"seed={seed} day={day}", flush=True)
                for missing in list(core.management['events'].values()):
                    if missing['status']=='waiting': record('resolve_missing', cat_id=missing['cat_id'])
                for trust in list(core.customer_trust['events'].values()):
                    if trust['status']=='waiting': record('resolve_customer_trust', choice='ignore' if seed==1 else 'recover', source=trust['id'])
                growth_answers(core,record,'rest' if seed==1 else 'service')
                event = core.store_events['days'][-1]
                if event.get('status') == 'waiting':
                    options = ('repair','patch','close') if event['type']=='trouble' else ('full','small','decline')
                    choice = options[(seed+day) % 3]
                    if choice == 'close' and (core.intake_request['status'] == 'waiting' or core.regular_introduction['status'] == 'waiting'): choice = 'patch'
                    record('resolve_store_event', choice=choice)
                    if core.management['game_over']: break
                    if choice == 'close': continue
                if day >= 2 and len(core.seats)==2 and core.funds>500: record('expand_seats')
                if day >= 2 and not core.housing['purchase'] and core.funds > core.housing['rules']['cost']: record('purchase_housing')
                if day >= 10 and core.housing['purchase'] and 'upgrade' not in core.housing and core.funds > 1000: record('upgrade_housing')
                if core.intake_request['status'] == 'waiting': record('resolve_intake', choice='decline' if seed == 1 else 'accept')
                if core.regular_introduction['status'] == 'waiting': record('resolve_regular_introduction', choice='accept')
                from cat_cafe_sim.core.cafe_items import inventory,unavailable_reason
                from cat_cafe_sim.core.cafe_item_shop import catalog
                if day%4==2:
                    offer=catalog()[((day-2)//4)%3]
                    if core.funds>offer['cost']: record('purchase_item',choice=offer['item']['id'])
                supplies=inventory(core)
                if supplies and day%3==0: record('sell_item',source=next(iter(supplies)))
                for source in list(inventory(core)):
                    target=next((key for key in core.cats if not unavailable_reason(core,source,key)),None)
                    if target: record('use_item',cat_id=target,source=source)
                # Alternating work/rest preserves the existing fatigue/health rules.
                working = [key for i, key in enumerate(core.cats) if core.cats[key].health_status == 'healthy' and core.activity(key)=='cafe' and (i + day) % 3 != 0]
                growth_answers(core,record,'rest' if seed==1 else 'service',practice=True)
                record('set_shifts', working)
                if not working: record('day_off'); continue
                record('start_business')
                while not core.closed:
                    growth_answers(core,record,'rest' if seed==1 else 'service',practice=True)
                    record('advance_business')
                growth_answers(core,record,'rest' if seed==1 else 'service')
                if core.management['game_over']: break
                for event in list(core.management['events'].values()):
                    if event['status']=='waiting': record('resolve_missing', cat_id=event['cat_id'])
                record('next_day')
    # Exercise every base action, optional/forced specials, negative reactions and exhaustion.
    selected = starting_conditions('free')
    from cat_cafe_sim.core.human_cat_types import load_presets
    presets = load_presets()
    rows = list(selected['profiles']['cats'].items())
    for i, (key, row) in enumerate(rows):
        for initial in ((100, 0, 0), (18, 100, 100), (5, 0, 0)):
            conf = replace(RelationshipConfig.load(), personality=presets[row['preset']] if 'preset' in row else presets[list(presets)[0]])
            # Use the exact exported personality instead of relying on storage field names.
            from cat_cafe_sim.core.human_cat_types import Personality
            p = exported_rules()['cats'][i]
            conf = replace(conf, personality=Personality(tuple(p['type_preferences']), tuple(p['intensity_preferences']), p['boredom_decay'], p['switch_affinity']))
            e = RelationshipInteraction(conf, cat_id=key, customer_id='guest-test', stamina=initial[0], tension=initial[1], engagement=initial[2])
            case = dict(cat_index=i, initial=exchange(e), steps=[]); data['exchanges'].append(case)
            actions = [('direct', None), ('pause', None), ('feint', None), ('adapt', None), ('switch', 'pet'), ('direct', None), ('switch', 'ball'), ('intense', None)] * 3
            for action, target in actions:
                if e.state['end_reason']: break
                if e.state['connect_pending']: action, target = 'connect', None
                elif action not in e.valid_actions(): action, target = 'direct', None
                if action == 'switch' and target == e.state['mode']: target = 'voice'
                e.step(action, target)
                case['steps'].append(dict(action=action, target=target, expected=exchange(e)))
    from cat_cafe_sim.core.cafe_preferences import preference_for
    data['satisfaction'] = []
    cat_id = next(iter(core.cats))
    preferred = preference_for(core.seed, 'guest-test', core.customer_preferences['rules']['pool'])
    for bits in range(64):
        matched = bool(bits & 8)
        core.cat_features[cat_id] = [preferred] if matched else []
        start = 100 if bits & 32 else 0
        pending = 1 if bits & 1 else 0
        initial = dict(affinity_start=start, affinity_pending=pending, connect_count=int(bool(bits & 2)), open_up_count=0, tension=50 if bits & 4 else 49.999, engagement=50, end_reason='exhausted' if bits & 16 else 'time_limit')
        result = dict(initial, customer_id='guest-test', cat_id=cat_id, affinity_delta=min(100,start+pending)-start)
        data['satisfaction'].append(dict(initial=initial, matched=matched, expected=evaluate_satisfaction(core,result)))
    return data


if __name__ == '__main__':
    target = Path(sys.argv[1]); target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(generate(), ensure_ascii=False), encoding='utf-8')
    print(target)
