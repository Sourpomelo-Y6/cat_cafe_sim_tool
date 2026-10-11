"""Compare original Python item operations and rejection boundaries."""
import json,sys,tempfile,copy
from pathlib import Path
from generate_reference import ROOT,snapshot
from export_standalone_rules import exported_rules
from cat_cafe_sim.cafe_new_game import starting_conditions
from cat_cafe_sim.cafe_interaction import CafeInteractionSession
from cat_cafe_sim.storage.relationships import RelationshipStore
from cat_cafe_sim.core.cafe_item_shop import catalog

def generate():
    cases=[]
    for funds in (99,100,150,200,2000):
        seed=0
        with tempfile.TemporaryDirectory(dir=ROOT/'Logs/Standalone') as temp:
            selected = starting_conditions('free')
            selected['management']['starting_funds'] = funds
            selected['store_events']['probability']=0
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
            rule=copy.deepcopy(exported_rules());rule['starting_funds']=funds;rule['store_events']['probability']=0
            case=dict(rules=rule,operations=[]);cases.append(case)
            def record(kind,choice=None,source=None,cat_id=None):
                nonlocal core
                before=snapshot(session);failed=False
                try:
                    if kind=='purchase_item':session.purchase_item(next(r for r in catalog() if r['item']['id']==choice))
                    elif kind=='sell_item':session.sell_item(source)
                    elif kind=='use_item':session.use_item(source,cat_id)
                    elif kind=='next_day':session.next_day()
                    else:session.automatic_step()
                except ValueError:failed=True
                core=session.core
                case['operations'].append(dict(kind=kind,rejected=failed,choice=choice,source=source,cat_id=cat_id,expected=snapshot(session)))
                if failed:assert before==snapshot(session)
            cat=next(iter(core.cats))
            record('use_item',source='absent',cat_id=cat)
            record('purchase_item',choice='nutrition_snack' if funds==150 else 'special_care_set' if funds==200 else 'care_supplies')
            record('use_item',source='item-purchase-1',cat_id=cat)
            record('sell_item',source='item-purchase-1');record('sell_item',source='item-purchase-1');record('use_item',source='item-purchase-1',cat_id=cat)
            if funds==2000:
                record('purchase_item',choice='nutrition_snack');record('purchase_item',choice='special_care_set')
                record('use_item',source='item-purchase-2',cat_id=cat)
                record('start_business');record('purchase_item',choice='care_supplies');record('sell_item',source='item-purchase-2')
                while not core.closed:record('advance_business')
                record('next_day');record('use_item',source='item-purchase-2',cat_id='unknown')
                record('use_item',source='item-purchase-2',cat_id=cat);record('use_item',source='item-purchase-3',cat_id=cat)
                record('sell_item',source='item-purchase-2');record('use_item',source='item-purchase-3',cat_id=cat)
                record('purchase_item',choice='care_supplies');record('use_item',source='item-purchase-4',cat_id=cat)
    return dict(cases=cases)
if __name__=='__main__':
    Path(sys.argv[1]).write_text(json.dumps(generate(),ensure_ascii=False),encoding='utf-8');print(sys.argv[1])
