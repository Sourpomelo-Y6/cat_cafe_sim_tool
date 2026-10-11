"""Compare first-regular ordering and presentation boundaries with original Python."""
import copy
import json
import sys
from pathlib import Path
from types import SimpleNamespace
from generate_reference import ROOT
from cat_cafe_sim.core.cafe_regular_introduction import rules, present

def generate():
    cases = []
    for rows in ([], [('guest-2', 3), ('guest-10', 3)], [('guest-1', 5), ('guest-2', 2)]):
        for day, preparation, over in ((3, True, False), (4, True, False), (4, False, False), (4, True, True), (6, True, False)):
            data = dict(rules=rules(), status='untriggered', customer_id=None, regular_day=None, presented_day=None, resolved_day=None)
            core = SimpleNamespace(day=day, can_set_shifts=preparation, management=dict(game_over={'reason':'funds'} if over else None),
                customer_loyalty=dict(customers={key:dict(score=100, regular_day=regular_day) for key,regular_day in rows}), regular_introduction=data, _emit=lambda *args, **kwargs: None)
            present(core)
            expected = dict(status=data['status'], customer_id=data['customer_id'], **{key:data[key] or 0 for key in ('regular_day','presented_day','resolved_day')})
            cases.append(dict(day=day, preparation=preparation, game_over=over, loyalty=[dict(customer_id=key, score=100, regular_day=regular_day) for key,regular_day in rows], expected=copy.deepcopy(expected)))
    return dict(cases=cases)

if __name__ == '__main__':
    target = Path(sys.argv[1]); target.write_text(json.dumps(generate(), ensure_ascii=False, indent=2), encoding='utf-8'); print(target)
