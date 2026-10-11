"""Locate the first difference from complete free mode using identical commands."""
import json
import sys
import tempfile
from pathlib import Path
from generate_reference import ROOT, snapshot
from cat_cafe_sim.cafe_new_game import create_game, starting_conditions


def differences(expected, actual, path='state'):
    if isinstance(expected, dict):
        return [r for key,value in expected.items() for r in differences(value, actual.get(key), path+'.'+key)]
    if isinstance(expected, list):
        if not isinstance(actual, list) or len(expected) != len(actual): return [dict(field=path+'.length', migrated=len(expected), full=len(actual) if isinstance(actual,list) else None)]
        return [r for i,value in enumerate(expected) for r in differences(value, actual[i], path+f'[{i}]')]
    return [] if expected == actual else [dict(field=path, migrated=expected, full=actual)]


def compare(reference):
    rows = []
    for scenario in reference['scenarios']:
        seed = scenario['seed']
        with tempfile.TemporaryDirectory(dir=ROOT / 'Logs/Standalone') as temp:
            full = create_game(Path(temp), starting_conditions('free'), seed=seed)
            report = dict(seed=seed, equal_operations=0, checked_through_day=1, first_difference=None)
            for operation in scenario['operations']:
                expected = operation['expected']
                if expected['day'] > 28: break
                kind = operation['kind']
                try:
                    if kind == 'set_shifts': full.set_shifts(operation['working'])
                    elif kind in ('purchase_housing','upgrade_housing'): getattr(full.core,kind)()
                    elif kind == 'next_day': full.next_day()
                    elif kind == 'day_off' or kind == 'resolve_store_event' and operation['choice']=='close': full.day_off()
                    elif kind == 'resolve_store_event': full.resolve_store_event(operation['choice'])
                    elif kind == 'resolve_customer_trust': full.core.resolve_customer_trust(operation['source'], operation['choice'])
                    elif kind.startswith('resolve_growth') or kind.startswith('begin_'): getattr(full.core,kind)(operation['cat_id'],operation['choice']) if kind.startswith('resolve_') else getattr(full.core,kind)(operation['cat_id'])
                    elif kind == 'resolve_regular_introduction': full.resolve_regular_introduction(operation['choice'])
                    elif kind == 'resolve_intake': full.resolve_intake_request(operation['choice'])
                    elif kind == 'use_item': full.use_item(operation['source'], operation['cat_id'])
                    elif kind == 'resolve_missing': full.resolve_missing(next(e['id'] for e in full.core.management['events'].values() if e['cat_id']==operation['cat_id'] and e['status']=='waiting'))
                    else: full.automatic_step()
                except ValueError as error:
                    report['first_difference'] = dict(day=full.core.day, tick=full.core.tick, command=kind, blocked=str(error))
                    break
                delta = differences(expected, snapshot(full))
                if delta:
                    report['first_difference'] = dict(day=full.core.day, tick=full.core.tick, command=kind, differences=delta[:12])
                    break
                report['equal_operations'] += 1; report['checked_through_day'] = full.core.day
            rows.append(report)
    return dict(note='Compare identical commands through day 28 or the first difference. Fixed intake requests and regular introductions are included. Customer trust is included. Growth and housing are included. Other acquisition events remain outside the migration.', rows=rows)


if __name__ == '__main__':
    source, destination = map(Path, sys.argv[1:3])
    destination.write_text(json.dumps(compare(json.loads(source.read_text(encoding='utf-8'))), ensure_ascii=False, indent=2), encoding='utf-8'); print(destination)
