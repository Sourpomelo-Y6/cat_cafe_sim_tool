"""Original Python seat/goal traces: real operations, reduced targets only in explicit fixtures."""
import json,sys,tempfile,copy
from pathlib import Path
from generate_reference import ROOT,snapshot,growth_answers
from export_standalone_rules import exported_rules
from cat_cafe_sim.cafe_new_game import starting_conditions
from cat_cafe_sim.cafe_interaction import CafeInteractionSession
from cat_cafe_sim.storage.relationships import RelationshipStore
from cat_cafe_sim.core import cafe_goal

def expected(session):
    s=snapshot(session);c=session.core
    if not c.goal.get('tracking_only'):
        s['goal']=[dict(started_day=g['started_day'],status=g['status'],resolved_day=g['resolved_day'] or 0,continued=g['continued']) for g in c.goal.get('history',[])]
        s['goal'].append(dict(started_day=cafe_goal.current_start(c.goal),status=c.goal['status'],resolved_day=c.goal['resolved_day'] or 0,continued=c.goal['continued']))
    return s

def generate():
    cases=[]
    for case_index,(funds,objective,accelerated,expired) in enumerate(((500,'free',False,False),(501,'free',False,False),(20000,'free',False,False),(20000,'popularity',True,False),(20000,'popularity',False,True),(1000,'popularity',False,False),(20000,'popularity',True,False),(20000,'popularity',True,False),(20000,'popularity',True,False),(20000,'popularity',True,False),(20000,'popularity',True,False),(20000,'popularity',True,False),(450,'free',False,False),(451,'free',False,False),(20000,'popularity',True,False),(20000,'popularity',True,False),(20000,'popularity',True,False))):
        seed=0
        with tempfile.TemporaryDirectory(dir=ROOT/'Logs/Standalone') as temp:
            selected = starting_conditions('free')
            from cat_cafe_sim.core.cafe_goal import progression_rules
            selected['goal']=progression_rules()
            selected['management']['starting_funds'] = funds
            selected['store_events']['probability']=0
            if case_index>=14:
                selected['management']['stress_per_service_tick']=0
            store = RelationshipStore(Path(temp) / 'relationships.json'); store._write(selected['profiles'])
            session = CafeInteractionSession(store=store, seat_count=2, seed=seed)
            core = session.core
            if case_index>=14:
                from dataclasses import replace
                core.shift_rules=replace(core.shift_rules,fatigue_per_service_tick=0);core.health_rules=replace(core.health_rules,max_probability=0)
            core.initialize_weekdays(selected['weekdays']); core.initialize_traits(selected['traits'])
            core.initialize_preferences(selected['features'], selected['preferences'])
            session.enable_management(selected['management']); core.initialize_operating_cost(selected['operating_cost'])
            core.initialize_customer_satisfaction(selected['customer_satisfaction'])
            goal = dict(selected['goal'])
            if objective=='free': goal.pop('stages',None);core.enable_goal(goal,tracking_only=True)
            else:
                if accelerated: goal.update(target=110,stages=[dict(days=20,target=120),dict(days=25,target=130)])
                if expired: goal.update(days=1,target=650);goal.pop('stages',None)
                core.enable_goal(goal)
            if objective=="popularity":
                core.initialize_advanced_customers()
                if not expired:
                    core.initialize_reservation();core.initialize_quiet_customer();core.initialize_play_customer();core.initialize_contact_customer();core.initialize_longhair_customer();core.initialize_vip_customer()
            if case_index==7:core.reservation["rules"]["open_up_count"]=999
            core.initialize_customer_loyalty(selected['customer_loyalty']); core.initialize_customer_discontent(selected['customer_discontent'])
            core.initialize_growth(selected['growth'])
            core.initialize_customer_trust(selected['customer_trust'])
            core.initialize_intake_request(selected['intake_request'])
            core.initialize_regular_introduction(selected['regular_introduction'])
            core.initialize_housing(selected['housing'])
            core.initialize_store_events(selected['store_events'])
            rule=copy.deepcopy(exported_rules());rule['starting_funds']=funds;rule['store_events']['probability']=0
            if accelerated: rule['goal_stages']=[dict(days=20,target=110),dict(days=20,target=120),dict(days=25,target=130)]
            if expired: rule['goal_stages']=[dict(days=1,target=650)]
            if case_index==7:rule["reservation"]["open_up_count"]=999
            if case_index>=14:rule.update(stress_per_service_tick=0,fatigue_per_service_tick=0,max_probability=0)
            if case_index==10:
                for key in ("quiet_customer","play_customer","contact_customer"):
                    for count in ("quiet_count","play_count","contact_count"):
                        if count in rule[key]:rule[key][count]=999;getattr(core,key)[count]=999
            case=dict(rules=rule,objective=objective,operations=[]);cases.append(case)
            def record(kind,working=None,cat_id=None,choice=None,source=None,seat_id=None):
                nonlocal core
                before=expected(session);failed=False
                try:
                    if kind=='set_shifts':session.set_shifts(working)
                    elif kind=='resolve_missing':session.resolve_missing(next(e['id'] for e in core.management['events'].values() if e['cat_id']==cat_id and e['status']=='waiting'))
                    elif kind=='resolve_customer_trust':core.resolve_customer_trust(source,choice)
                    elif kind.startswith('resolve_growth'):getattr(core,kind)(cat_id,choice)
                    elif kind=='resolve_intake':session.resolve_intake_request(choice)
                    elif kind=='resolve_regular_introduction':session.resolve_regular_introduction(choice)
                    elif kind=='purchase_seat_equipment':
                        from cat_cafe_sim.core.cafe_seat_equipment import catalog
                        session.purchase_seat_equipment(seat_id,next(r for r in catalog() if r['id']==choice))
                    elif kind=='equip_seat':session.equip_seat(seat_id,choice or None)
                    elif kind=='resolve_reservation':session.resolve_reservation(choice)
                    elif kind in ('expand_seats','continue_goal','advance_goal','purchase_housing'):getattr(core,kind)()
                    elif kind=='next_day':session.next_day()
                    elif kind=='day_off':session.day_off()
                    else:session.automatic_step()
                except ValueError:failed=True
                core=session.core
                case['operations'].append(dict(kind=kind,seat_id=seat_id,rejected=failed,working=working,choice=choice,source=source,cat_id=cat_id,expected=expected(session)))
                if failed:assert before==expected(session),(kind,'mutated')
                return not failed
            def events():
                for e in list(core.management['events'].values()):
                    if e['status']=='waiting':record('resolve_missing',cat_id=e['cat_id'])
                for e in list(core.customer_trust['events'].values()):
                    if e['status']=='waiting':record('resolve_customer_trust',source=e['id'],choice='recover')
                growth_answers(core,record)
                if core.reservation and core.reservation['request'] and core.reservation['request']['status']=='waiting':
                    record('start_business');record('day_off');record('expand_seats');record('resolve_reservation',choice='invalid')
                    record('resolve_reservation',choice='decline' if case_index==6 else 'accept');record('resolve_reservation',choice='accept')
                if cafe_goal.pending(core):
                    record('expand_seats');record('next_day');record('day_off');record('set_shifts',working=[])
                    record('continue_goal')
                    if cafe_goal.next_rules(core.goal):record('advance_goal')
                if core.intake_request['status']=='waiting':record('resolve_intake',choice='decline')
                if core.regular_introduction['status']=='waiting':record('resolve_regular_introduction',choice='decline')
            if case_index in (12,13):
                record('purchase_seat_equipment',seat_id='seat-1',choice='cat_tower');continue
            record('continue_goal');record('advance_goal');record('expand_seats');record('expand_seats')
            if case_index in (9,10,11):
                record('purchase_seat_equipment',seat_id='seat-99',choice='cat_tower');record('equip_seat',seat_id='seat-1',choice='equipment-99')
                for i,kind in enumerate(('quiet_space','cat_tower','grooming_brush','toys','cushion')):
                    record('purchase_seat_equipment',seat_id='seat-'+str(i%3+1),choice=kind)
                for i in range(3):record('equip_seat',seat_id='seat-'+str(i+1),choice='equipment-'+str(i+1))
                record('equip_seat',seat_id='seat-2',choice='equipment-1');record('equip_seat',seat_id='seat-1',choice='equipment-1');record('equip_seat',seat_id='seat-2',choice='equipment-2')
                record('equip_seat',seat_id='seat-3',choice='');record('equip_seat',seat_id='seat-3',choice='equipment-3')
                if case_index==11:
                    for i in range(3):record('equip_seat',seat_id='seat-'+str(i+1),choice='equipment-4')
            if funds<=501:continue
            days=12 if accelerated else 2 if expired else 60 if objective=='popularity' else 3
            for day in range(1,days+1):
                events()
                while len(core.seats)<8 and record('expand_seats'):pass
                if case_index>=14 and len(core.seats)==8 and not core.equipment_store:
                    for i in range(8):record('purchase_seat_equipment',seat_id='seat-'+str(i+1),choice=('quiet_space','cat_tower','grooming_brush')[case_index-14])
                if case_index in (9,10) and len(core.seats)>=5 and next(r for r in (core.equipment_store or {}).get('purchases',[]) if r['id']=='equipment-1') and core.seats['seat-3'].equipment!='equipment-1':
                    for i in range(3):record('equip_seat',seat_id='seat-'+str(i+3),choice='equipment-'+str(i+1))
                working=[id for i,id in enumerate(core.cats) if core.cats[id].health_status=='healthy' and core.activity(id)=='cafe' and (case_index>=14 or (i+day)%3!=0)]
                if case_index==8 and core.reservation and core.reservation['request'] and core.reservation['request']['visit_day']==day:working=[]
                if core.reservation and core.reservation['request'] and core.reservation['request']['status']=='accepted' and core.reservation['request']['visit_day']==day:record('day_off')
                record('set_shifts',working=working)
                if not working and not (core.reservation and core.reservation['request'] and core.reservation['request']['status']=='accepted' and core.reservation['request']['visit_day']==core.day):record('day_off');continue
                record('start_business');record('expand_seats')
                if case_index>=9:record('equip_seat',seat_id='seat-1',choice='');record('purchase_seat_equipment',seat_id='seat-1',choice='cat_tower')
                while not core.closed:
                    growth_answers(core,record);record('advance_business')
                if core.management['game_over']:break
                events();record('next_day')
            if case_index>=14:
                id=('advanced-quiet','advanced-play','advanced-contact')[case_index-14]
                assert any(v['id']==id and v['specialist_result']=='success' for op in case['operations'] for v in op['expected']['visits']),(case_index,'missing success')
            case['final_seats']=len(core.seats)
            case['goal_status']=core.goal['status']
            if accelerated:
                assert len(core.seats)==8
                assert all(any(v['id']==id for op in case['operations'] for v in op['expected']['visits']) for id in ('expanded-guest','expanded-guest-2','expanded-guest-3','expanded-guest-4','expanded-guest-5'))
    return dict(cases=cases)
if __name__=='__main__':
    Path(sys.argv[1]).write_text(json.dumps(generate(),ensure_ascii=False),encoding='utf-8');print(sys.argv[1])
