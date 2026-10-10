"""Generate comparison traces by executing original Python cores (no HTTP)."""
import json
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
from cat_cafe_sim.core.human_cat_relationship import RelationshipConfig, RelationshipInteraction
from cat_cafe_sim.storage.relationships import RelationshipStore
from export_standalone_rules import exported_rules


def exchange(s, seat=0):
    return dict(customer_id=s.customer_id, cat_id=s.cat_id, seat=seat, mode=s.state['mode'],
                last_group=s.state['last_interaction_group'], previous_action=s.state['previous_action'],
                previous_reaction=s.state['previous_reaction'], end_reason=s.state['end_reason'],
                remaining=s.state['remaining_ticks'], streak=s.state['interaction_streak'],
                **{k: s.state[k] for k in ('stamina', 'engagement', 'tension', 'connect_count', 'open_up_count', 'simultaneous_count', 'affinity_start', 'affinity_pending', 'pause_after_interaction', 'connect_pending', 'open_up_pending')},
                bonus=s.state['bonus_funds'], tension_multiplier=s.config.customer_tension_multiplier)


def snapshot(session):
    c = session.core
    from cat_cafe_sim.core.cafe_checkpoint import outcome_result
    results = [outcome_result(r) for r in list(c.outcomes.values())[c.day_outcome_offset:]]
    summary = c.summary()
    return dict(seed=c.seed, day=c.day, tick=c.tick, phase='closed' if c.closed else 'preparation' if c.can_set_shifts else 'open',
                funds=c.funds, opening_funds=summary['opening_funds'], revenue=summary['revenue'], expenses=summary['total_expenses'],
                popularity=c.management['popularity'], completed=summary['completed_interactions'], game_over=(c.management['game_over'] or {}).get('reason'),
                cats=[dict(id=cat.id, stamina=cat.stamina, fatigue=cat.fatigue, stress=c.management['stress'][cat.id], working=cat.id in c.working_cats,
                           health=cat.health_status, activity=c.activity(cat.id), service_ticks=c.cat_service_ticks[cat.id],
                           recovery_remaining=cat.recovery_days_remaining, cannot_continue=cat.cannot_continue,
                           missing_remaining=next((e['remaining'] for e in c.management['events'].values() if e['cat_id']==cat.id and e['status']!='resolved'),0),
                           kitten=next((e['kitten'] for e in reversed(list(c.management['events'].values())) if e['cat_id']==cat.id),False)) for cat in c.cats.values()],
                visits=[dict(id=v.id, cat_id=next((e.cat_id for e in c.interactions.values() if e.customer_id == v.id),
                                              next((r['cat_id'] for r in results if r['customer_id'] == v.id), None)),
                             arrival=v.arrival_tick, seated_at=v.seated_at if v.seated_at is not None else -1, seated_ticks=v.seated_ticks, departure=v.departure_reason, bill=v.bill) for v in c.visits.values()],
                queue=list(c.queue), exchanges=[exchange(e, int(key.split('-')[-1])-1) for key, e in sorted(c.interactions.items())])


def generate():
    data = dict(draws=[], scenarios=[], exchanges=[])
    for seed in (0, 1, 42):
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
            # Only modules implemented in this first slice are enabled on BOTH sides.
            scenario = dict(seed=seed, operations=[]); data['scenarios'].append(scenario)
            def record(kind, working=None, cat_id=None):
                if kind == 'set_shifts': session.set_shifts(working)
                elif kind == 'next_day': session.next_day()
                elif kind == 'day_off': session.day_off()
                elif kind == 'resolve_missing': session.resolve_missing(next(e['id'] for e in core.management['events'].values() if e['cat_id']==cat_id and e['status']=='waiting'))
                else: session.automatic_step()
                scenario['operations'].append(dict(kind=kind, **({'working': working} if working is not None else {}), **({'cat_id': cat_id} if cat_id else {}), expected=snapshot(session)))
            for day in range(1, 29):
                # Alternating work/rest preserves the existing fatigue/health rules.
                working = [key for i, key in enumerate(core.cats) if core.cats[key].health_status == 'healthy' and core.activity(key)=='cafe' and (i + day) % 3 != 0]
                record('set_shifts', working)
                if not working: record('day_off'); continue
                record('start_business')
                while not core.closed: record('advance_business')
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
    return data


if __name__ == '__main__':
    target = Path(sys.argv[1]); target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(json.dumps(generate(), ensure_ascii=False), encoding='utf-8')
    print(target)
