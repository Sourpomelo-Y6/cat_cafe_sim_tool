"""Cover every store-event type and answer using the original Python core."""
import json
import sys
import tempfile
from pathlib import Path
from generate_reference import ROOT, snapshot
from cat_cafe_sim.cafe_new_game import starting_conditions
from cat_cafe_sim.cafe_interaction import CafeInteractionSession
from cat_cafe_sim.storage.relationships import RelationshipStore
from cat_cafe_sim.core.cafe_store_events import draw, event_types
from cat_cafe_sim.core.cafe_items import inventory


def generate():
    selected = starting_conditions('free')
    rules = selected['store_events']
    seeds = {}
    for seed in range(1000):
        kind = event_types(rules)[int(draw(seed, 1, 'type') * 5)] if draw(seed, 1, 'occur') < rules['probability'] else None
        seeds.setdefault(kind, seed)
        if len(seeds) == 6:
            break
    cases = []
    for kind, seed in seeds.items():
        choices = ('repair', 'patch', 'close') if kind == 'trouble' else ('full', 'small', 'decline') if kind == 'support' else (None,)
        for choice in choices:
            with tempfile.TemporaryDirectory(dir=ROOT / 'Logs/Standalone') as temp:
                store = RelationshipStore(Path(temp) / 'relationships.json'); store._write(selected['profiles'])
                session = CafeInteractionSession(store=store, seat_count=2, seed=seed); c = session.core
                c.initialize_weekdays(selected['weekdays']); c.initialize_traits(selected['traits'])
                c.initialize_preferences(selected['features'], selected['preferences']); session.enable_management(selected['management'])
                c.initialize_operating_cost(selected['operating_cost']); c.initialize_customer_satisfaction(selected['customer_satisfaction'])
                goal = dict(selected['goal']); goal.pop('stages', None); c.enable_goal(goal, tracking_only=True)
                c.initialize_store_events(rules)
                case = dict(seed=seed, event=kind, answer=choice, initial=snapshot(session), operations=[]); cases.append(case)

                def record(kind, **arguments):
                    if kind == 'resolve_store_event':
                        if arguments['choice'] == 'close': session.day_off()
                        else: session.resolve_store_event(arguments['choice'])
                    elif kind == 'next_day': session.next_day()
                    elif kind == 'use_item': session.use_item(arguments['source'], arguments['cat_id'])
                    else: session.automatic_step()
                    case['operations'].append(dict(kind=kind, **arguments, expected=snapshot(session)))

                if choice:
                    record('resolve_store_event', choice=choice)
                    if choice == 'close': continue
                record('start_business')
                while not c.closed: record('advance_business')
                record('next_day')
                if kind == 'supplies':
                    if c.store_events['days'][-1].get('status') == 'waiting':
                        record('resolve_store_event', choice='repair' if c.store_events['days'][-1]['type']=='trouble' else 'decline')
                    cat = next(key for key in c.cats if c.management['stress'][key] > 0)
                    record('use_item', source=next(iter(inventory(c))), cat_id=cat)
    return dict(scenarios=cases, draws=[], satisfaction=[], exchanges=[])


if __name__ == '__main__':
    path = Path(sys.argv[1]); path.write_text(json.dumps(generate(), ensure_ascii=False), encoding='utf-8'); print(path)
