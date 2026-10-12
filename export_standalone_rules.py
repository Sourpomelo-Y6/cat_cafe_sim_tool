"""Export only the implemented migration slice, without changing Python balance."""
import ast
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
    adoption_module=ast.parse((ROOT/'cat_cafe_sim/cat_cafe_sim/core/cafe_adoption.py').read_text(encoding='utf-8'))
    result['adoption_threshold']=next(ast.literal_eval(node.value) for node in adoption_module.body if isinstance(node,ast.Assign) and node.targets[0].id=='THRESHOLD')
    result['satisfaction'] = read('cafe_customer_satisfaction')
    result['popularity'] = {key: read('cafe_goal')[key] for key in ('cap', 'gain_per_success')}
    result['store_events'] = read('cafe_store_events')
    result['loyalty'] = read('cafe_customer_loyalty')
    result['discontent'] = read('cafe_customer_discontent')
    result['trust'] = read('cafe_customer_trust')
    result['expansion_costs'] = [read('cafe_expansion')[key] for key in ('cost','four_seat_cost','five_seat_cost','six_seat_cost','seven_seat_cost','eight_seat_cost')]
    result['goal_stages'] = [dict(days=read('cafe_goal')['days'], target=read('cafe_goal')['target'])] + read('cafe_goal_stages')
    result['advanced_customer'] = read('cafe_advanced_customers')
    result['reservation'] = read('cafe_reservation')
    result['longhair_customer']=read('cafe_longhair_customer')
    result['vip_customer']=read('cafe_vip_customer')
    for key in ('quiet_customer','play_customer','contact_customer'): result[key]=read('cafe_'+key)
    result['equipment']=read('cafe_seat_equipment')
    result['growth'] = read('cafe_growth')
    result['dispatch']=read('cafe_dispatch')
    result['dispatch']['item_reward']=read('cafe_item_rewards')[result['dispatch']['id']]
    unlocks=read('cafe_dispatch_unlocks')
    destinations=[read('cafe_dispatch')]+read('cafe_dispatch_destinations')+[row['destination'] for row in unlocks.values() if 'destination' in row]+[read('cafe_dispatch_trouble')['destination']]
    result['dispatch_destinations']=[]
    for destination in destinations:
        destination['item_reward']=read('cafe_item_rewards').get(destination['id'])
        destination['unsupported_reason']=''
        result['dispatch_destinations'].append(destination)
    result['dispatch_unlocks']=[dict(id=key,popularity=row.get('popularity',0),returns=row.get('returns',0),stage=row.get('stage',0)) for key,row in unlocks.items()]
    player_module=ast.parse((ROOT/'cat_cafe_sim/cat_cafe_sim/core/cafe_player.py').read_text(encoding='utf-8'))
    player_constants={node.targets[0].id:ast.literal_eval(node.value) for node in player_module.body if isinstance(node,ast.Assign) and isinstance(node.value,ast.Constant)}
    result['player']=dict(daily_turns=player_constants['DAILY_TURNS'],set_ticks=player_constants['SET_TICKS'],max_affinity=player_constants['MAX_AFFINITY'],relief=management['player_relief'],stress=management['player_stress'])
    result['bond']=read('cafe_bond_goal')
    result['patron']=read('cafe_patron')
    result['patron']['members']=read('cafe_patron_members')
    result['dispatch_trouble']=read('cafe_dispatch_trouble')
    result['dispatch_encounters']=read('cafe_dispatch_encounters')
    result['item_shop'] = [dict(**row, sale_price=read('cafe_item_sales')[row['item']['id']]) for row in (read('cafe_item_shop'), read('cafe_nutrition_snack'), read('cafe_special_care_set'))]
    result['item_sale_prices']=[dict(id=key,price=value) for key,value in read('cafe_item_sales').items()]
    result['housing'] = read('cafe_housing')
    result['housing_upgrade'] = read('cafe_housing_upgrade')
    result['popular_customer_count'] = new.get('popular_customer_count', weekdays.get('popular_customer_count', 0))
    result['second_popular_customer_count'] = new.get('second_popular_customer_count', weekdays.get('second_popular_customer_count', 0))
    for row in new['cats']:
        p = presets[row['preset']]
        trait = traits.get(row.get('trait'), {})
        result['cats'].append(dict(trait_id=row.get('trait',''), id=row['cat_id'], name=row['name'], features=row.get('features', []),
                                   type_preferences=[p['type_preferences'][t['id']] for t in relationship['types']],
                                   intensity_preferences=[p['intensity_preferences'][key] for key in ('gentle', 'standard', 'active')],
                                   boredom_decay=p['boredom_decay'], switch_affinity=p['switch_affinity'],
                                   **{key: trait.get(key, 1) for key in ('service_stress', 'service_fatigue', 'rest_fatigue')},dispatch_reward=trait.get('dispatch_reward',1),return_stress=trait.get('return_stress',0)))
    intake = read('cafe_intake_request')
    p = presets[intake['preset']]
    trait = traits[intake['trait']]
    result['intake'] = dict(
        day=intake['day'], cost=intake['cost'], capacity=read('cafe_housing')['initial_capacity'],
        personality=intake['preset'], trait=trait['name'],
        cat=dict(trait_id=intake['trait'], id=intake['cat_id'], name=intake['name'], features=intake['features'],
                 type_preferences=[p['type_preferences'][t['id']] for t in relationship['types']],
                 intensity_preferences=[p['intensity_preferences'][k] for k in ('gentle', 'standard', 'active')],
                 boredom_decay=p['boredom_decay'], switch_affinity=p['switch_affinity'],
                 **{k: trait.get(k, 1) for k in ('service_stress', 'service_fatigue', 'rest_fatigue')},dispatch_reward=trait.get('dispatch_reward',1),return_stress=trait.get('return_stress',0)))
    # Read the original display labels without importing the game package or copying its constants.
    module = ast.parse((ROOT / 'cat_cafe_sim/cat_cafe_sim/core/cafe_preferences.py').read_text(encoding='utf-8'))
    features = ast.literal_eval(next(node.value for node in module.body
                                    if isinstance(node, ast.Assign) and any(
                                        isinstance(target, ast.Name) and target.id == 'FEATURES'
                                        for target in node.targets)))
    result['intake']['features_label'] = '・'.join(features[key][0] for key in intake['features'])
    regular = read('cafe_regular_introduction')
    p = presets[regular['preset']]
    trait = traits[regular['trait']]
    result['regular_introduction'] = dict(
        cost=regular['cost'], personality=regular['preset'], trait=trait['name'],
        features_label='・'.join(features[key][0] for key in regular['features']),
        cat=dict(trait_id=regular['trait'], id=regular['cat_id'], name=regular['name'], features=regular['features'],
                 type_preferences=[p['type_preferences'][t['id']] for t in relationship['types']],
                 intensity_preferences=[p['intensity_preferences'][k] for k in ('gentle', 'standard', 'active')],
                 boredom_decay=p['boredom_decay'], switch_affinity=p['switch_affinity'],
                 **{k: trait.get(k, 1) for k in ('service_stress', 'service_fatigue', 'rest_fatigue')},dispatch_reward=trait.get('dispatch_reward',1),return_stress=trait.get('return_stress',0)))
    visiting = read('cafe_visiting_cat')
    p = presets[visiting['preset']]
    trait = traits[visiting['trait']]
    result['visiting_cat'] = dict(
        interactions_required=visiting['interactions_required'], cost=visiting['cost'],
        personality=visiting['preset'], trait=trait['name'],
        features_label='・'.join(features[key][0] for key in visiting['features']),
        cat=dict(trait_id=visiting['trait'], id=visiting['cat_id'], name=visiting['name'], features=visiting['features'],
                 type_preferences=[p['type_preferences'][t['id']] for t in relationship['types']],
                 intensity_preferences=[p['intensity_preferences'][k] for k in ('gentle', 'standard', 'active')],
                 boredom_decay=p['boredom_decay'], switch_affinity=p['switch_affinity'],
                 **{k: trait.get(k, 1) for k in ('service_stress', 'service_fatigue', 'rest_fatigue')},
                 dispatch_reward=trait.get('dispatch_reward',1), return_stress=trait.get('return_stress',0)))
    result['pet_shop'] = []
    for index, row in enumerate(read('cafe_pet_shop'), 1):
        p = presets[row['preset']]
        trait = traits[row['trait']]
        result['pet_shop'].append(dict(
            cost=row['cost'], personality=row['preset'], trait=trait['name'],
            features_label='・'.join(features[key][0] for key in row['features']),
            cat=dict(trait_id=row['trait'], id=f'shop-{index}', name=row['name'], features=row['features'],
                     type_preferences=[p['type_preferences'][t['id']] for t in relationship['types']],
                     intensity_preferences=[p['intensity_preferences'][k] for k in ('gentle', 'standard', 'active')],
                     boredom_decay=p['boredom_decay'], switch_affinity=p['switch_affinity'],
                     **{k: trait.get(k, 1) for k in ('service_stress', 'service_fatigue', 'rest_fatigue')},
                     dispatch_reward=trait.get('dispatch_reward',1), return_stress=trait.get('return_stress',0))))
    result['recruitment_catalog'] = []
    for index, row in enumerate(read('cafe_recruitment'), 1):
        p = presets[row['preset']]
        trait = traits[row['trait']]
        result['recruitment_catalog'].append(dict(
            cost=row['cost'], personality=row['preset'], trait=trait['name'],
            features_label='・'.join(features[key][0] for key in row['features']),
            cat=dict(trait_id=row['trait'], id=f'catalog-{index}', name=row['name'], features=row['features'],
                     type_preferences=[p['type_preferences'][t['id']] for t in relationship['types']],
                     intensity_preferences=[p['intensity_preferences'][k] for k in ('gentle', 'standard', 'active')],
                     boredom_decay=p['boredom_decay'], switch_affinity=p['switch_affinity'],
                     **{k: trait.get(k, 1) for k in ('service_stress', 'service_fatigue', 'rest_fatigue')},
                     dispatch_reward=trait.get('dispatch_reward',1), return_stress=trait.get('return_stress',0))))
    recruitment_module=ast.parse((ROOT/'cat_cafe_sim/cat_cafe_sim/core/cafe_recruitment.py').read_text(encoding='utf-8'))
    recruitment_constants={node.targets[0].id:ast.literal_eval(node.value) for node in recruitment_module.body if isinstance(node,ast.Assign) and isinstance(node.value,ast.Constant)}
    result['recruitment_refresh_days']=recruitment_constants['REFRESH_DAYS']
    result['recruitment_batch_size']=recruitment_constants['BATCH_SIZE']
    result['dispatch_introductions'] = []
    for filename in ('cafe_dispatch_introduction', 'cafe_photo_introduction'):
        row = read(filename); p = presets[row['preset']]; trait = traits[row['trait']]
        result['dispatch_introductions'].append(dict(destination=row['destination'], candidate=dict(
            cost=row['cost'], personality=row['preset'], trait=trait['name'],
            features_label='・'.join(features[key][0] for key in row['features']),
            cat=dict(id='', name=row['name'], trait_id=row['trait'], features=row['features'],
                     type_preferences=[p['type_preferences'][t['id']] for t in relationship['types']],
                     intensity_preferences=[p['intensity_preferences'][k] for k in ('gentle','standard','active')],
                     boredom_decay=p['boredom_decay'], switch_affinity=p['switch_affinity'],
                     **{k:trait.get(k,1) for k in ('service_stress','service_fatigue','rest_fatigue','dispatch_reward')},
                     return_stress=trait.get('return_stress',0)))))
    result['rules_id'] = hashlib.sha256(json.dumps(result, sort_keys=True, ensure_ascii=True).encode()).hexdigest()
    return result


if __name__ == '__main__':
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_text(json.dumps(exported_rules(), ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(OUTPUT)
