"""Original Python housing boundary cases, including rejected operations."""
import json,sys,tempfile
from pathlib import Path
from generate_reference import ROOT,snapshot
from cat_cafe_sim.cafe_new_game import starting_conditions
from cat_cafe_sim.cafe_interaction import CafeInteractionSession
from cat_cafe_sim.storage.relationships import RelationshipStore

def generate():
    cases=[]
    for funds in (500,501,1500,1501,2000):
        seed=0
        with tempfile.TemporaryDirectory(dir=ROOT/'Logs/Standalone') as temp:
            selected = starting_conditions('free')
            selected['management']['starting_funds'] = funds
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
            case=dict(funds=funds,operations=[]);cases.append(case)
            for kind in ('upgrade_housing','purchase_housing','purchase_housing','upgrade_housing','upgrade_housing','day_off','purchase_housing','upgrade_housing','start_business','purchase_housing'):
                before=snapshot(session); failed=False
                try:
                    if kind=='day_off':session.day_off()
                    elif kind=='start_business':session.automatic_step()
                    else:getattr(core,kind)()
                except ValueError:failed=True
                core=session.core
                case['operations'].append(dict(kind=kind,rejected=failed,expected=snapshot(session)))
                if failed:assert before==snapshot(session)
    return dict(cases=cases)
if __name__=='__main__':
    Path(sys.argv[1]).write_text(json.dumps(generate(),ensure_ascii=False),encoding='utf-8')
    print(sys.argv[1])
