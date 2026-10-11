"""Real business transitions under isolated collision/suspension fixtures.

Production coefficients are exported unchanged. Only these cases accelerate
warning generation and choose satisfaction thresholds to cover all outcomes.
"""
import copy
import json
import sys
import tempfile
from dataclasses import replace
from pathlib import Path
from generate_reference import ROOT, snapshot
from export_standalone_rules import exported_rules
from cat_cafe_sim.cafe_new_game import starting_conditions
from cat_cafe_sim.cafe_interaction import CafeInteractionSession
from cat_cafe_sim.core.config import Config
from cat_cafe_sim.storage.relationships import RelationshipStore


def generate():
    cases = []
    for outcome in ('ignore', 'recovered', 'departed', 'normal', 'unanswered', 'repeat'):
        selected = starting_conditions('free')
        rules = copy.deepcopy(exported_rules())
        rules.update(arrival_ticks=[0, 0], queue_capacity=1,
                     patterns=[dict(days=[0, 2]), dict(days=list(range(7)))])
        rules['discontent'].update(waiting_gain=100, suspension_days=1)
        rules['store_events']['probability'] = 0
        rules['satisfaction'].update(satisfied_score=1 if outcome in ('recovered', 'repeat') else 5,
                                     dissatisfied_score=4 if outcome=='departed' else 0)
        # Production profiles and interaction coefficients; no manual results.
        with tempfile.TemporaryDirectory(dir=ROOT / 'Logs/Standalone') as temp:
            store = RelationshipStore(Path(temp)/'relationships.json'); store._write(selected['profiles'])
            config = replace(Config.load(), arrival_ticks=(0, 0), queue_capacity=1)
            s = CafeInteractionSession(store=store, seat_count=2, seed=0, cafe_config=config); c=s.core
            c.initialize_weekdays(dict(start_weekday=0, patterns=[[0, 2], list(range(7))]))
            c.initialize_traits(selected['traits']); c.initialize_preferences(selected['features'], selected['preferences'])
            s.enable_management(selected['management']); c.initialize_operating_cost(selected['operating_cost'])
            c.initialize_customer_satisfaction(rules['satisfaction'])
            goal=dict(selected['goal']); goal.pop('stages', None); c.enable_goal(goal, tracking_only=True)
            c.initialize_customer_loyalty(selected['customer_loyalty']); c.initialize_customer_discontent(rules['discontent'])
            c.initialize_customer_trust(selected['customer_trust'])
            c.initialize_intake_request(selected['intake_request']); c.initialize_regular_introduction(selected['regular_introduction'])
            c.initialize_store_events(rules['store_events'])
            case=dict(name=outcome, rules=rules, operations=[]); cases.append(case)
            def record(kind, choice=None, source=None, working=None, cat_id=None):
                nonlocal c
                if kind=='resolve_customer_trust': c.resolve_customer_trust(source, choice)
                elif kind=='resolve_intake': s.resolve_intake_request(choice)
                elif kind=='resolve_regular_introduction': s.resolve_regular_introduction(choice)
                elif kind=='resolve_missing': s.resolve_missing(next(e['id'] for e in c.management['events'].values() if e['cat_id']==cat_id and e['status']=='waiting'))
                elif kind=='set_shifts': s.set_shifts(working)
                elif kind=='next_day': s.next_day()
                else: s.automatic_step()
                c=s.core
                case['operations'].append(dict(kind=kind, **({'choice':choice} if choice else {}), **({'source':source} if source else {}), **({'cat_id':cat_id} if cat_id else {}), **({'working':working} if working is not None else {}), expected=snapshot(s)))
            for day in range(1, 7 if outcome=='repeat' else 6):
                for e in list(c.management['events'].values()):
                    if e['status']=='waiting': record('resolve_missing', cat_id=e['cat_id'])
                pending=[e for e in c.customer_trust['events'].values() if e['status']=='waiting']
                if pending and outcome=='unanswered': break
                for e in pending:
                    record('resolve_customer_trust', 'ignore' if outcome=='ignore' else 'recover', e['id'])
                if c.intake_request['status']=='waiting': record('resolve_intake', 'decline')
                if c.regular_introduction['status']=='waiting': record('resolve_regular_introduction', 'decline')
                record('set_shifts', working=[key for i,key in enumerate(c.cats) if (i+day)%3!=0 and c.cats[key].health_status=='healthy' and c.activity(key)=='cafe'])
                record('start_business')
                while not c.closed: record('advance_business')
                if outcome in ('recovered','departed') and any(e['status']=='resolved' for e in c.customer_trust['events'].values()): break
                record('next_day')
            row=c.customer_trust['customers'].get('guest-2')
            expected='departed' if outcome in ('ignore','departed') else 'stable' if outcome in ('recovered','repeat') else 'recovery' if outcome=='normal' else 'waiting'
            assert row and (outcome=='repeat' or row['status']==expected), (outcome,row)
            if outcome=='repeat': assert len(c.customer_trust['events'])>=2, c.customer_trust
    return dict(scenarios=cases)


if __name__=='__main__':
    target=Path(sys.argv[1]); target.parent.mkdir(parents=True,exist_ok=True)
    target.write_text(json.dumps(generate(),ensure_ascii=False),encoding='utf-8'); print(target)
