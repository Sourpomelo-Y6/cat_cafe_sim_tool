using System;
using System.Collections.Generic;

namespace CatCafePreview.Standalone
{
    // Stable IDs and double precision belong to the model; UI DTOs are projections.
    [Serializable] public class CafeRules
    {
        public int version = 1, opening_ticks, queue_capacity, max_wait_ticks, start_weekday;
        public int popular_customer_count, second_popular_customer_count;
        public double adoption_threshold;
        public int service_assignment_version,autoplay_version;
        public WaitingAreaRules waiting_area,waiting_area_upgrade;
        public RestSpaceRules rest_space,rest_space_upgrade,rest_space_soundproof;
        public double max_stamina, time_price, starting_funds, starting_popularity;
        public double base_cost, per_seat_cost, max_fatigue, fatigue_per_service_tick, rest_day_recovery;
        public double safe_fatigue, probability_per_fatigue, max_probability;
        public int recovery_days;
        public double stress_per_service_tick, rest_recovery, runaway_threshold, popularity_loss, return_stress, kitten_probability, kitten_cost;
        public int missing_days;
        public double tension_multiplier;
        public string[] preference_pool;
        public int[] arrival_ticks;
        public WeekPattern[] patterns;
        public CatDefinition[] cats;
        public ExchangeRules exchange;
        public SatisfactionRules satisfaction;
        public PopularityRules popularity;
        public StoreEventRules store_events;
        public LoyaltyRules loyalty;
        public DiscontentRules discontent;
        public TrustRules trust;
        public GrowthRules growth;
        public HousingRules housing, housing_upgrade;
        public ItemShopRule[] item_shop;public ItemSaleRule[] item_sale_prices;
        public SpecialistRules advanced_customer, reservation, vip_customer; public LonghairRules longhair_customer;
        public ServiceCustomerRules quiet_customer,play_customer,contact_customer; public EquipmentRule[] equipment;
        public PlayerRules player;public BondRules bond;
        public PatronRules patron;
        public double[] expansion_costs; public GoalStageRule[] goal_stages;
        public ExchangeType[] types;
        public IntakeRules intake, regular_introduction; public VisitingCatRules visiting_cat; public IntakeRules[] pet_shop, recruitment_catalog; public int recruitment_refresh_days, recruitment_batch_size;
        public DispatchRules dispatch;public DispatchTroubleRules dispatch_trouble; public DispatchIntroductionRule[] dispatch_introductions;
        public DispatchRules[] dispatch_destinations;public DispatchUnlockRule[] dispatch_unlocks;public DispatchEncounterRule[] dispatch_encounters;
        public string rules_id;
    }
    [Serializable] public class IntakeRules { public int day, capacity; public double cost; public string personality, trait, features_label; public CatDefinition cat; }
    [Serializable] public class VisitingCatRules : IntakeRules { public int interactions_required; }
    [Serializable] public class CafeVisitingAction { public int day; public string choice; }
    [Serializable] public class CafeVisitingCat { public string status="untriggered"; public int presented_day, accepted_day; public List<CafeVisitingAction> actions=new List<CafeVisitingAction>(); }
    [Serializable] public class CafeIntake { public string status = "scheduled"; public int presented_day, resolved_day; }
    [Serializable] public class CafeRegularIntroduction { public string status = "untriggered", customer_id; public int regular_day, presented_day, resolved_day; }
    [Serializable] public class WeekPattern { public int[] days; }
    [Serializable] public class SatisfactionRules { public double satisfied_score, dissatisfied_score, gauge_target, satisfied_bonus, dissatisfied_discontent_gain; }
    [Serializable] public class PopularityRules { public double cap, gain_per_success; }
    [Serializable] public class CafeSupply { public string id, name; public double stress_relief, fatigue_relief; }
    [Serializable] public class StoreEventRules { public double probability, trouble_cost, support_full_cost, support_full_popularity, support_small_cost, support_small_popularity; public int festival_customer_tick, trouble_seat_loss; public CafeSupply supply; }
    [Serializable] public class CafeStoreEvent { public int day; public string type, status, choice; public double draw, cost, popularity; }
    [Serializable] public class CafeSupplyUse { public string source, cat_id; public int day; public double before, after, before_fatigue, after_fatigue; }
    [Serializable] public class LoyaltyRules { public double gain, threshold; }
    [Serializable] public class DiscontentRules { public double waiting_gain, advanced_failure_gain, good_service_recovery, threshold, return_score; public int suspension_days; }
    [Serializable] public class CafeLoyalty { public string customer_id; public double score; public int regular_day; }
    [Serializable] public class CafeSuspension { public int day, until; }
    [Serializable] public class CafeDiscontent { public string customer_id; public double score; public int score_day; public List<CafeSuspension> suspensions = new List<CafeSuspension>(); }
    [Serializable] public class TrustRules { public int warning_suspensions; public double recovery_score, popularity_loss; }
    [Serializable] public class CafeTrustCustomer { public string customer_id, status, event_id, reason; public int departed_day; }
    [Serializable] public class CafeTrustEvent { public string id, customer_id, status, choice, outcome; public int day, suspensions, resolved_day; }
    [Serializable] public class CafeArrival { public string customer_id; public int tick; }
    [Serializable] public class CafeCustomerRecord { public string customer_id, cat_id, departure, satisfaction; public int service_ticks; public int completed_tick; public int[] type_actions = new int[8]; public int connect_count, open_up_count, simultaneous_count; public string specialist_result; public double specialist_bonus; public bool service_equipment; public int service_count; public int service_seat=-1;
        public double affinity_delta, stamina_spent; }
    [Serializable] public class CafeSatisfaction
    {
        public string level, label;
        public int score;
        public string[] reasons;
        public bool matched, special, gauges;
        public double bonus;
    }
    [Serializable] public class CatDefinition
    {
        public string id, name;public string trait_id;
        public string[] features;
        public double[] type_preferences, intensity_preferences;
        public double boredom_decay, switch_affinity, service_stress = 1, service_fatigue = 1, rest_fatigue = 1, dispatch_reward = 1, return_stress;
    }
    [Serializable] public class ExchangeType { public string id, group; public double gain, cost; public string[] actions; }
    [Serializable] public class ExchangeRules
    {
        public int ticks;
        public double max_stamina, target, direct_gain, direct_cost, adapt_gain, adapt_cost, gentle_gain, gentle_cost;
        public double intense_gain, intense_cost, feint_gain, feint_cost, combo_gain, pause_recovery, boredom_floor, low_stamina;
        public double confused_threshold, favorable_threshold, enthusiastic_threshold, confused_multiplier;
        public double optional_threshold, forced_threshold, gauge_cost, connect_bonus, open_up_bonus, simultaneous_bonus;
        public double open_up_tension, tension_enthusiastic, tension_favorable, tension_neutral;
        public double tension_confused_loss, tension_listless_loss, tension_turn_away_loss;
        public double affinity_enthusiastic, affinity_favorable, affinity_turn_away_loss, affinity_connect, affinity_open_up, affinity_simultaneous, affinity_exhausted_loss;
    }
    [Serializable] public class CafeCat
    {
        public string id, health = "healthy", activity = "cafe";
        public double stamina, fatigue, stress;
        public bool working = true, cannot_continue;
        public int service_ticks, recovery_remaining, missing_remaining;
        public bool kitten;
    }
    [Serializable] public class CafeVisit
    {
        public string id, cat_id, departure;
        public int arrival, seated_at = -1, seated_ticks;
        public double bill;
        public int completed_tick; public int[] type_actions = new int[8]; public int connect_count, open_up_count, simultaneous_count; public string specialist_result; public double specialist_bonus; public bool service_equipment; public int service_count; public int service_seat=-1;
        public double affinity_delta, stamina_spent;
        public CafeSatisfaction satisfaction;
    }
    [Serializable] public class CafeExchange
    {
        public string customer_id, cat_id, mode = "teaser", last_group, previous_action, previous_reaction, end_reason;
        public string service_group="",equipment_group=""; public double equipment_engagement=1,equipment_tension=1;
        public int seat, remaining, streak, connect_count, open_up_count, simultaneous_count;
        public double stamina, engagement, tension, bonus, affinity_start, affinity_pending, tension_multiplier = 1;
        public int[] type_actions = new int[8]; public string[] growth_groups = new string[0], growth_types = new string[0]; public double stamina_spent;
        public bool pause_after_interaction, connect_pending, open_up_pending;
    }
    [Serializable] public class CafeRelationship { public string cat_id, customer_id; public double affinity; }
    [Serializable] public class CafeLegacyServiceTotal {public string cat_id;public int ticks;}
    [Serializable] public class CafeDayResult
    {
        public int day, arrivals, completed;public List<CafeLegacyServiceTotal> legacy_service_ticks;
        public int seat_count = 2;
        public double opening_funds, revenue, expenses, closing_funds;
        public double pet_shop_expenses, recruitment_expenses, housing_expenses, housing_cost, item_expenses, item_sales_income, expansion_expenses, goal_popularity, equipment_expenses, rest_space_expenses, waiting_area_expenses, waiting_area_cost;
        public double popularity_gain,dispatch_income,dispatch_trouble_expenses;
        public int satisfied, normal, dissatisfied;
        public List<string> resting_cats = new List<string>();
        public List<CafeCustomerRecord> customer_records = new List<CafeCustomerRecord>();
    }
    [Serializable] public class CafeSnapshot
    {
        public int version = 2, seed, day = 1, tick, revision;
        public string rules_id, instance_id, phase = "preparation", game_over;
        public bool auto_assignment=true;
        public CafeAutoRun autoplay;public CafeImportHistory import_history;
        public double funds, opening_funds, popularity, revenue, expenses;
        public int completed;
        public int seat_count = 2;
        public double pet_shop_expenses, recruitment_expenses, housing_expenses, housing_cost, item_expenses, item_sales_income, expansion_expenses, goal_popularity, equipment_expenses, rest_space_expenses, waiting_area_expenses, waiting_area_cost;
        public double popularity_gain,dispatch_income,dispatch_trouble_expenses;
        public List<CafeCat> cats = new List<CafeCat>();
        public List<CafeVisit> visits = new List<CafeVisit>();
        public List<string> queue = new List<string>();
        public List<CafeExchange> exchanges = new List<CafeExchange>();
        public List<CafeRelationship> relationships = new List<CafeRelationship>();
        public List<CafeDayResult> history = new List<CafeDayResult>();
        public List<CafeStoreEvent> store_events = new List<CafeStoreEvent>();
        public List<CafeItemPurchase> item_purchases = new List<CafeItemPurchase>();
        public List<CafeItemSale> item_sales = new List<CafeItemSale>();
        public List<CafeSupplyUse> item_uses = new List<CafeSupplyUse>();
        public List<CafeLoyalty> loyalty = new List<CafeLoyalty>();
        public List<CafeDiscontent> discontent = new List<CafeDiscontent>();
        public List<CafeGrowth> growth = new List<CafeGrowth>();
        public List<CafeGrowthOperation> growth_operations = new List<CafeGrowthOperation>();
        public List<string> resting_cats = new List<string>();
        public List<CafeTrustCustomer> trust_customers = new List<CafeTrustCustomer>();
        public List<CafeTrustEvent> trust_events = new List<CafeTrustEvent>();
        public List<CafePlayerCat> player_cats=new List<CafePlayerCat>();
        public List<CafePlayerSession> player_sessions=new List<CafePlayerSession>();public CafeBondGoal bond_goal;
        public CafePatron patron;
        public int challenge_started_day;
        public string objective = "free";
        public List<CafeDispatchUnlock> dispatch_unlocks=new List<CafeDispatchUnlock>();
        public List<CafeDispatch> dispatches=new List<CafeDispatch>();
        public List<CafeGoalStage> goal = new List<CafeGoalStage>();
        public List<CafeClearRecord> clear_results = new List<CafeClearRecord>();
        public List<CafeEquipmentPurchase> equipment_purchases=new List<CafeEquipmentPurchase>();
        public List<CafeEquipmentMove> equipment_moves=new List<CafeEquipmentMove>();
        public List<CafeEquipmentPlacement> equipment_placements=new List<CafeEquipmentPlacement>();
        public CafeReservation reservation = new CafeReservation();
        public List<CafeSeatPurchase> expansion = new List<CafeSeatPurchase>();
        public CafeHousing housing = new CafeHousing();
        public CafeRestSpace rest_space = new CafeRestSpace();
        public CafeWaitingArea waiting_area = new CafeWaitingArea();
        public CafeIntake intake_request = new CafeIntake();
        public CafeRegularIntroduction regular_introduction = new CafeRegularIntroduction();
        public CafeVisitingCat visiting_cat = new CafeVisitingCat();
        public List<CafeShopPurchase> shop_purchases = new List<CafeShopPurchase>();
        public CafeRecruitment recruitment; public CafeAdoption adoption;
        // Unity JsonUtility materializes JSON null strings as empty strings.
        public void NormalizeJsonStrings()
        {
            if(import_history!=null && string.IsNullOrEmpty(import_history.base_json))import_history=null;
            if(import_history?.operations!=null)foreach(var operation in import_history.operations)CafeLegacyImport.NormalizeOperation(operation);
            if(history!=null)foreach(var h in history)if(h.legacy_service_ticks!=null && h.legacy_service_ticks.Count==0)h.legacy_service_ticks=null;
            if(autoplay!=null && string.IsNullOrEmpty(autoplay.run_id))autoplay=null;
            if(autoplay!=null){if(autoplay.reason==null)autoplay.reason="";if(autoplay.message==null)autoplay.message="";}
            if(dispatches!=null)foreach(var e in dispatches)if(e.encounter!=null && string.IsNullOrEmpty(e.encounter.id) && string.IsNullOrEmpty(e.encounter.choice) && (string.IsNullOrEmpty(e.encounter.status) || e.encounter.status=="scheduled") && e.encounter.occurred_day==0 && e.encounter.resolved_day==0 && e.encounter.fatigue_before==0 && e.encounter.fatigue_after==0 && e.encounter.stress_before==0 && e.encounter.stress_after==0)e.encounter=null;
            if(dispatches!=null)foreach(var e in dispatches)if(e.encounter!=null && e.encounter.choice=="")e.encounter.choice=null;
            if(dispatches!=null)foreach(var e in dispatches)if(e.introduction!=null && string.IsNullOrEmpty(e.introduction.cat_id) && string.IsNullOrEmpty(e.introduction.name) && (string.IsNullOrEmpty(e.introduction.status) || e.introduction.status=="scheduled") && e.introduction.cost==0 && e.introduction.presented_day==0 && e.introduction.resolved_day==0)e.introduction=null;
            if(dispatches!=null)foreach(var e in dispatches)if(e.trouble!=null && string.IsNullOrEmpty(e.trouble.destination) && string.IsNullOrEmpty(e.trouble.choice) && (string.IsNullOrEmpty(e.trouble.status) || e.trouble.status=="scheduled") && e.trouble.stress==0 && e.trouble.probability==0 && e.trouble.draw==0 && e.trouble.cost==0 && e.trouble.missing_day==0 && e.trouble.choice_day==0)e.trouble=null;
            if(dispatches!=null)foreach(var e in dispatches)if(e.trouble!=null && e.trouble.choice=="")e.trouble.choice=null;
            if(bond_goal!=null && string.IsNullOrEmpty(bond_goal.id) && bond_goal.resolved_day==0 && !bond_goal.continued && (bond_goal.achieved_cats==null || bond_goal.achieved_cats.Count==0))bond_goal=null;
            if(player_sessions!=null)foreach(var session in player_sessions){foreach(var a in session.actions)if(a.target=="")a.target=null;var e=session.exchange;if(e!=null){if(e.end_reason=="")e.end_reason=null;if(e.last_group=="")e.last_group=null;if(e.previous_action=="")e.previous_action=null;if(e.previous_reaction=="")e.previous_reaction=null;}}
            if(patron!=null && string.IsNullOrEmpty(patron.id) && patron.satisfaction==0 && (patron.members==null || patron.members.Count==0) && patron.resolved_day==0 && !patron.continued)patron=null;
            if(dispatches!=null)foreach(var e in dispatches){if(e.patron_match!=null && string.IsNullOrEmpty(e.patron_match.member_id))e.patron_match=null;if(e.patron_match?.condition!=null){if(e.patron_match.condition.feature=="")e.patron_match.condition.feature=null;if(e.patron_match.condition.specialization=="")e.patron_match.condition.specialization=null;}}
            if(dispatches!=null)foreach(var e in dispatches)if(e.choice=="")e.choice=null;
            if (regular_introduction != null && regular_introduction.customer_id == "") regular_introduction.customer_id = null;
            if(adoption!=null && string.IsNullOrEmpty(adoption.id) && !adoption.enabled && (adoption.events==null || adoption.events.Count==0))adoption=null;
            if(adoption!=null && adoption.events!=null)foreach(var e in adoption.events)if(e.choice=="")e.choice=null;
            if(recruitment!=null && string.IsNullOrEmpty(recruitment.id) && (recruitment.days==null || recruitment.days.Count==0) && (recruitment.accepted==null || recruitment.accepted.Count==0))recruitment=null;
            if (growth != null) foreach (var row in growth) row.Normalize();
            if (reservation != null) { if (reservation.result == "") reservation.result = null; if (reservation.choice == "") reservation.choice = null; }
            if (game_over == "") game_over = null;
            if (trust_customers != null) foreach (var r in trust_customers) { if (r.event_id == "") r.event_id = null; if (r.reason == "") r.reason = null; }
            if (trust_events != null) foreach (var e in trust_events) { if (e.choice == "") e.choice = null; if (e.outcome == "") e.outcome = null; }
            if (store_events != null) foreach (var e in store_events) { if (e.type == "") e.type = null; if (e.status == "") e.status = null; if (e.choice == "") e.choice = null; }
            if (history != null) foreach (var h in history) if (h.customer_records != null) foreach (var r in h.customer_records) { if (r.specialist_result == "") r.specialist_result = null; if (r.satisfaction == "") r.satisfaction = null; if (r.cat_id == "") r.cat_id = null; }
            if (visits != null) foreach (var v in visits) { if (v.specialist_result == "") v.specialist_result = null; if (v.departure == "") v.departure = null; if (v.cat_id == "") v.cat_id = null; if (v.satisfaction != null && string.IsNullOrEmpty(v.satisfaction.level)) v.satisfaction = null; }
            if (exchanges != null) foreach (var e in exchanges)
            {
                if (e.end_reason == "") e.end_reason = null;
                if (e.last_group == "") e.last_group = null;
                if (e.previous_action == "") e.previous_action = null;
                if (e.previous_reaction == "") e.previous_reaction = null;
            }
        }
    }
}
