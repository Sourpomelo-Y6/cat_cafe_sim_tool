"""Export only the implemented migration slice, without changing Python balance."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CONFIG = ROOT / 'cat_cafe_sim/config'
OUTPUT = ROOT / 'cat_cafe_sim_tool/unity_preview_template/Resources/CatCafeStandalone/rules.json'


def exported_rules():
    def read(name):
        return json.loads((CONFIG / (name + '.json')).read_text(encoding='utf-8'))
    base, new = read('default'), read('cafe_new_game')
    management, shifts, health = read('cafe_management'), read('cafe_shifts'), read('cafe_health')
    weekdays, preferences = read('cafe_weekdays'), read('cafe_customer_preferences')
    relationship, presets, traits = read('human_cat_relationship'), read('cat_personalities'), read('cafe_traits')
    result = dict(version=1, **{key: base[key] for key in ('opening_ticks', 'queue_capacity', 'max_wait_ticks', 'max_stamina', 'time_price', 'arrival_ticks')},
                  **{key: management[key] for key in ('starting_funds', 'starting_popularity', 'stress_per_service_tick', 'rest_recovery', 'runaway_threshold', 'popularity_loss', 'return_stress', 'kitten_probability', 'kitten_cost', 'missing_days')},
                  **shifts, **health, **read('cafe_operating_cost'), start_weekday=weekdays['start_weekday'],
                  patterns=[dict(days=days) for days in weekdays['patterns']], preference_pool=preferences['pool'],
                  tension_multiplier=preferences['tension_multiplier'], exchange=relationship['rules'], types=relationship['types'], cats=[])
    result['satisfaction'] = read('cafe_customer_satisfaction')
    result['popularity'] = {key: read('cafe_goal')[key] for key in ('cap', 'gain_per_success')}
    result['store_events'] = read('cafe_store_events')
    for row in new['cats']:
        p = presets[row['preset']]
        trait = traits.get(row.get('trait'), {})
        result['cats'].append(dict(id=row['cat_id'], name=row['name'], features=row.get('features', []),
                                   type_preferences=[p['type_preferences'][t['id']] for t in relationship['types']],
                                   intensity_preferences=[p['intensity_preferences'][key] for key in ('gentle', 'standard', 'active')],
                                   boredom_decay=p['boredom_decay'], switch_affinity=p['switch_affinity'],
                                   **{key: trait.get(key, 1) for key in ('service_stress', 'service_fatigue', 'rest_fatigue')}))
    result['rules_id'] = hashlib.sha256(json.dumps(result, sort_keys=True, ensure_ascii=True).encode()).hexdigest()
    return result


if __name__ == '__main__':
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_text(json.dumps(exported_rules(), ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(OUTPUT)
