using System;
using System.Collections.Generic;

namespace CatCafePreview.Standalone
{
    // Stable IDs and double precision belong to the model; UI DTOs are projections.
    [Serializable] public class CafeRules
    {
        public int version = 1, opening_ticks, queue_capacity, max_wait_ticks, start_weekday;
        public int popular_customer_count, second_popular_customer_count;
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
        public ExchangeType[] types;
        public IntakeRules intake, regular_introduction;
        public string rules_id;
    }
    [Serializable] public class IntakeRules { public int day, capacity; public double cost; public string personality, trait, features_label; public CatDefinition cat; }
    [Serializable] public class CafeIntake { public string status = "scheduled"; public int presented_day, resolved_day; }
    [Serializable] public class CafeRegularIntroduction { public string status = "untriggered", customer_id; public int regular_day, presented_day, resolved_day; }
    [Serializable] public class WeekPattern { public int[] days; }
    [Serializable] public class SatisfactionRules { public double satisfied_score, dissatisfied_score, gauge_target, satisfied_bonus, dissatisfied_discontent_gain; }
    [Serializable] public class PopularityRules { public double cap, gain_per_success; }
    [Serializable] public class CafeSupply { public string id, name; public double stress_relief; }
    [Serializable] public class StoreEventRules { public double probability, trouble_cost, support_full_cost, support_full_popularity, support_small_cost, support_small_popularity; public int festival_customer_tick, trouble_seat_loss; public CafeSupply supply; }
    [Serializable] public class CafeStoreEvent { public int day; public string type, status, choice; public double draw, cost, popularity; }
    [Serializable] public class CafeSupplyUse { public string source, cat_id; public int day; public double before, after; }
    [Serializable] public class LoyaltyRules { public double gain, threshold; }
    [Serializable] public class DiscontentRules { public double waiting_gain, advanced_failure_gain, good_service_recovery, threshold, return_score; public int suspension_days; }
    [Serializable] public class CafeLoyalty { public string customer_id; public double score; public int regular_day; }
    [Serializable] public class CafeSuspension { public int day, until; }
    [Serializable] public class CafeDiscontent { public string customer_id; public double score; public int score_day; public List<CafeSuspension> suspensions = new List<CafeSuspension>(); }
    [Serializable] public class TrustRules { public int warning_suspensions; public double recovery_score, popularity_loss; }
    [Serializable] public class CafeTrustCustomer { public string customer_id, status, event_id, reason; public int departed_day; }
    [Serializable] public class CafeTrustEvent { public string id, customer_id, status, choice, outcome; public int day, suspensions, resolved_day; }
    [Serializable] public class CafeArrival { public string customer_id; public int tick; }
    [Serializable] public class CafeCustomerRecord { public string customer_id, cat_id, departure, satisfaction; public int[] type_actions = new int[8]; public double affinity_delta, stamina_spent; }
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
        public string id, name;
        public string[] features;
        public double[] type_preferences, intensity_preferences;
        public double boredom_decay, switch_affinity, service_stress = 1, service_fatigue = 1, rest_fatigue = 1;
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
        public int[] type_actions = new int[8]; public double affinity_delta, stamina_spent;
        public CafeSatisfaction satisfaction;
    }
    [Serializable] public class CafeExchange
    {
        public string customer_id, cat_id, mode = "teaser", last_group, previous_action, previous_reaction, end_reason;
        public int seat, remaining, streak, connect_count, open_up_count, simultaneous_count;
        public double stamina, engagement, tension, bonus, affinity_start, affinity_pending, tension_multiplier = 1;
        public int[] type_actions = new int[8]; public string[] growth_groups = new string[0], growth_types = new string[0]; public double stamina_spent;
        public bool pause_after_interaction, connect_pending, open_up_pending;
    }
    [Serializable] public class CafeRelationship { public string cat_id, customer_id; public double affinity; }
    [Serializable] public class CafeDayResult
    {
        public int day, arrivals, completed;
        public double opening_funds, revenue, expenses, closing_funds;
        public double recruitment_expenses, housing_expenses, housing_cost;
        public double popularity_gain;
        public int satisfied, normal, dissatisfied;
        public List<string> resting_cats = new List<string>();
        public List<CafeCustomerRecord> customer_records = new List<CafeCustomerRecord>();
    }
    [Serializable] public class CafeSnapshot
    {
        public int version = 1, seed, day = 1, tick, revision;
        public string rules_id, instance_id, phase = "preparation", game_over;
        public double funds, opening_funds, popularity, revenue, expenses;
        public int completed;
        public double recruitment_expenses, housing_expenses, housing_cost;
        public double popularity_gain;
        public List<CafeCat> cats = new List<CafeCat>();
        public List<CafeVisit> visits = new List<CafeVisit>();
        public List<string> queue = new List<string>();
        public List<CafeExchange> exchanges = new List<CafeExchange>();
        public List<CafeRelationship> relationships = new List<CafeRelationship>();
        public List<CafeDayResult> history = new List<CafeDayResult>();
        public List<CafeStoreEvent> store_events = new List<CafeStoreEvent>();
        public List<CafeSupplyUse> item_uses = new List<CafeSupplyUse>();
        public List<CafeLoyalty> loyalty = new List<CafeLoyalty>();
        public List<CafeDiscontent> discontent = new List<CafeDiscontent>();
        public List<CafeGrowth> growth = new List<CafeGrowth>();
        public List<CafeGrowthOperation> growth_operations = new List<CafeGrowthOperation>();
        public List<string> resting_cats = new List<string>();
        public List<CafeTrustCustomer> trust_customers = new List<CafeTrustCustomer>();
        public List<CafeTrustEvent> trust_events = new List<CafeTrustEvent>();
        public CafeHousing housing = new CafeHousing();
        public CafeIntake intake_request = new CafeIntake();
        public CafeRegularIntroduction regular_introduction = new CafeRegularIntroduction();
        // Unity JsonUtility materializes JSON null strings as empty strings.
        public void NormalizeJsonStrings()
        {
            if (regular_introduction != null && regular_introduction.customer_id == "") regular_introduction.customer_id = null;
            if (growth != null) foreach (var row in growth) row.Normalize();
            if (game_over == "") game_over = null;
            if (trust_customers != null) foreach (var r in trust_customers) { if (r.event_id == "") r.event_id = null; if (r.reason == "") r.reason = null; }
            if (trust_events != null) foreach (var e in trust_events) { if (e.choice == "") e.choice = null; if (e.outcome == "") e.outcome = null; }
            if (store_events != null) foreach (var e in store_events) { if (e.type == "") e.type = null; if (e.status == "") e.status = null; if (e.choice == "") e.choice = null; }
            if (history != null) foreach (var h in history) if (h.customer_records != null) foreach (var r in h.customer_records) { if (r.satisfaction == "") r.satisfaction = null; if (r.cat_id == "") r.cat_id = null; }
            if (visits != null) foreach (var v in visits) { if (v.departure == "") v.departure = null; if (v.cat_id == "") v.cat_id = null; if (v.satisfaction != null && string.IsNullOrEmpty(v.satisfaction.level)) v.satisfaction = null; }
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
