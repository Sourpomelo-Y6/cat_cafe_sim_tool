"""Record the difference between complete free mode and the migrated subset."""
import json
import sys
import tempfile
from pathlib import Path
from generate_reference import ROOT
from cat_cafe_sim.cafe_new_game import create_game, starting_conditions
from cat_cafe_sim.cafe_interaction import CafeInteractionSession
from cat_cafe_sim.storage.relationships import RelationshipStore


def compare():
    rows = []
    for seed in (0, 1, 42):
        selected = starting_conditions('free')
        with tempfile.TemporaryDirectory(dir=ROOT / 'Logs/Standalone') as temp:
            full = create_game(Path(temp) / 'full', selected, seed=seed)
            store = RelationshipStore(Path(temp) / 'subset-relationships.json'); store._write(selected['profiles'])
            subset = CafeInteractionSession(store=store, seat_count=2, seed=seed)
            c = subset.core
            c.initialize_weekdays(selected['weekdays']); c.initialize_traits(selected['traits'])
            c.initialize_preferences(selected['features'], selected['preferences']); subset.enable_management(selected['management'])
            c.initialize_operating_cost(selected['operating_cost'])
            c.initialize_customer_satisfaction(selected['customer_satisfaction'])
            goal = dict(selected['goal']); goal.pop('stages', None); c.enable_goal(goal, tracking_only=True)
            c.initialize_customer_loyalty(selected['customer_loyalty']); c.initialize_customer_discontent(selected['customer_discontent'])
            c.initialize_store_events(selected['store_events'])
            for session in (full, subset):
                while not session.core.closed: session.automatic_step()
            def summary(session):
                s = session.core.summary()
                return {key: s[key] for key in ('funds', 'revenue', 'operating_cost', 'completed_interactions', 'cat_stamina', 'popularity', 'customer_satisfaction')}
            assert summary(full) == summary(subset), 'Unexplained first-day difference'
            rows.append(dict(seed=seed, store_event=full.core.store_events['days'][0], full_python=summary(full), migrated_subset=summary(subset)))
    return dict(note='The subset includes basic satisfaction and popularity tracking; store events; loyalty and discontent; trust and growth remain omitted. Equal subset traces do not establish complete free-mode parity.', days=1, rows=rows)


if __name__ == '__main__':
    target = Path(sys.argv[1]); target.write_text(json.dumps(compare(), ensure_ascii=False, indent=2), encoding='utf-8'); print(target)
