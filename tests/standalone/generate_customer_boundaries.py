"""Reference boundary values from the original loyalty/discontent modules."""
import copy
import json
import sys
from pathlib import Path
from types import SimpleNamespace
from generate_reference import ROOT, exported_rules
from cat_cafe_sim.cafe_new_game import starting_conditions
from cat_cafe_sim.core import cafe_customer_loyalty as loyalty, cafe_customer_discontent as discontent


def generate():
    selected = starting_conditions('free')
    exported = exported_rules()
    core = SimpleNamespace(day=1, goal=None, config=SimpleNamespace(arrival_ticks=exported['arrival_ticks'], opening_ticks=exported['opening_ticks']), weekdays=selected['weekdays'],
                           customer_loyalty=dict(rules=selected['customer_loyalty'], customers={}),
                           customer_discontent=dict(rules=selected['customer_discontent'], customers={}),
                           customer_satisfaction=selected['customer_satisfaction'], _emit=lambda *args, **kwargs: None)
    operations = []
    for day in range(1, 5):
        operations.extend([(day, 'service', 'guest-1', 'satisfied'), (day, 'departure', 'guest-2', 'queue_full' if day == 2 else 'wait_timeout'), (day, 'service', 'event-guest-1', 'satisfied')])
    operations.extend([(5, 'service', 'guest-1', 'satisfied'), (11, 'query', 'guest-2', None), (12, 'query', 'guest-2', None),
                       (12, 'service', 'guest-2', 'satisfied'), (13, 'service', 'guest-2', 'normal'), (14, 'service', 'guest-2', 'dissatisfied'),
                       (15, 'departure', 'guest-2', 'queue_full'), (16, 'service', 'guest-2', 'dissatisfied'), (23, 'query', 'guest-2', None), (24, 'query', 'guest-2', None)])
    result = dict(steps=[], weekdays=[])
    for day, kind, customer, value in operations:
        core.day = day
        if kind == 'service':
            outcome = dict(customer_id=customer, affinity_delta=1)
            satisfaction = dict(level=value)
            loyalty.apply(core, outcome, satisfaction); discontent.apply_service(core, outcome, None, satisfaction)
        elif kind == 'departure': discontent.apply_departure(core, SimpleNamespace(id=customer, departure_reason=value))
        queries = [dict(customer_id=customer, day=d, score=discontent.row(core, customer, d)['score'], available=discontent.available(core, customer, d)) for customer in ('guest-1', 'guest-2', 'event-guest-1') for d in (day, day+1)]
        result['steps'].append(dict(day=day, kind=kind, customer_id=customer, value=value,
            loyalty=[dict(customer_id=k, score=v['score'], regular_day=v['regular_day'] or 0) for k,v in core.customer_loyalty['customers'].items()],
            discontent=[dict(customer_id=k, **copy.deepcopy(v)) for k,v in core.customer_discontent['customers'].items()],
            queries=queries, extra_schedule=[dict(customer_id=k, tick=t) for k,t in loyalty.extra_schedule(core, day).items()]))
    for customer in ('guest-1', 'guest-2', 'guest-7', 'guest-12', 'guest-01', 'guest-1000000', 'event-guest-1', '客-あ'):
        result['weekdays'].append(dict(customer_id=customer, weekday=loyalty.extra_weekday(core, customer)))
    return result


if __name__ == '__main__':
    path = Path(sys.argv[1]); path.write_text(json.dumps(generate(), ensure_ascii=False), encoding='utf-8'); print(path)
