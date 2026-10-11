using System;
using System.Linq;

namespace CatCafePreview.Standalone
{
    // First migration slice: base multi-seat service, shifts, health, management.
    // No UI, filesystem, clock, Unity or HTTP dependency.
    public sealed partial class CafeSimulation
    {
        public readonly CafeRules Rules;
        public CafeSnapshot State { get; private set; }
        public CafeSimulation(CafeRules rules, int seed = 0)
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            if (rules.version != 1 || rules.cats == null || rules.cats.Length == 0 || seed < 0) throw new ArgumentException("Invalid initial configuration");
            State = new CafeSnapshot { seed = seed, rules_id = rules.rules_id, instance_id = Guid.NewGuid().ToString("N"), funds = rules.starting_funds, opening_funds = rules.starting_funds, popularity = rules.starting_popularity };
            foreach (var cat in rules.cats.OrderBy(c => c.id, StringComparer.Ordinal)) State.cats.Add(new CafeCat { id = cat.id, stamina = rules.max_stamina });
            PresentStoreEvent();
        }
        public void Restore(CafeSnapshot value)
        {
            Validate(value);
            foreach (var e in value.store_events) e.draw = StoreDraw(value.seed, e.day, "occur");
            State = value;
            // A load starts a new command epoch. Old queued buttons cannot mutate it.
            State.instance_id = Guid.NewGuid().ToString("N"); State.revision = 0;
        }
        public void Validate(CafeSnapshot s)
        {
            if (s == null || s.version != 1 || s.rules_id != Rules.rules_id || s.seed < 0 || s.day < 1 || s.tick < 0 || s.tick > Rules.opening_ticks
                || !new[] { "preparation", "open", "closed" }.Contains(s.phase)
                || s.cats == null || s.cats.Count != Rules.cats.Length || s.visits == null || s.exchanges == null || s.queue == null || s.relationships == null || s.history == null
                || s.cats.Select(c => c.id).Distinct().Count() != s.cats.Count || s.revision < 0
                || !Finite(s.funds) || !Finite(s.opening_funds) || !Finite(s.revenue) || !Finite(s.expenses)
                || s.revenue < 0 || s.expenses < 0 || Math.Abs(s.funds - (s.opening_funds + s.revenue - s.expenses)) > 1e-7
                || s.history.Count != s.day - 1 || s.completed < 0
                || !Range(s.popularity, 0, Rules.popularity.cap)) throw new ArgumentException("Invalid standalone save");
            ValidateStoreEvents(s);
            foreach (var c in s.cats)
                if (!Rules.cats.Any(d => d.id == c.id) || !Range(c.stamina, 0, Rules.max_stamina) || !Range(c.fatigue, 0, Rules.max_fatigue)
                    || !Range(c.stress, 0, 100) || !new[] { "healthy", "sick" }.Contains(c.health) || !new[] { "cafe", "missing" }.Contains(c.activity)
                    || c.service_ticks < 0 || c.recovery_remaining < 0 || c.missing_remaining < 0) throw new ArgumentException("Invalid saved cat");
            if (s.phase == "preparation" && (s.tick != 0 || s.visits.Count != 0 || s.exchanges.Count != 0)
                || (s.phase == "closed") != (s.tick == Rules.opening_ticks)
                || s.visits.Select(v => v.id).Distinct().Count() != s.visits.Count
                || s.queue.Distinct().Count() != s.queue.Count
                || s.exchanges.Select(e => e.seat).Distinct().Count() != s.exchanges.Count
                || s.exchanges.Select(e => e.cat_id).Distinct().Count() != s.exchanges.Count
                || s.exchanges.Select(e => e.customer_id).Distinct().Count() != s.exchanges.Count
                || s.relationships.Select(r => r.cat_id + "/" + r.customer_id).Distinct().Count() != s.relationships.Count) throw new ArgumentException("Invalid saved phase or IDs");
            foreach (var id in s.queue) if (!s.visits.Any(v => v.id == id && v.departure == null && v.seated_at == -1)) throw new ArgumentException("Invalid saved queue");
            foreach (var v in s.visits) if (!Finite(v.bill) || v.seated_ticks < 0 || v.arrival < 0 || v.arrival >= s.tick) throw new ArgumentException("Invalid saved visit");
            if (!Range(s.popularity_gain, 0, Rules.popularity.cap) || s.phase != "closed" && s.popularity_gain != 0
                || s.visits.Count(v => v.satisfaction != null) != s.completed) throw new ArgumentException("Invalid satisfaction settlement");
            foreach (var v in s.visits.Where(v => v.satisfaction != null))
            {
                var a = v.satisfaction;
                string expected = a.score >= Rules.satisfaction.satisfied_score ? "satisfied" : a.score <= Rules.satisfaction.dissatisfied_score ? "dissatisfied" : "normal";
                if (v.departure == null || v.cat_id == null || a.level != expected || a.score < -2 || a.score > 4 || a.reasons == null
                    || a.bonus != (expected == "satisfied" ? Rules.satisfaction.satisfied_bonus : 0)) throw new ArgumentException("Invalid saved satisfaction");
            }
            foreach (var e in s.exchanges)
                if (s.phase != "open" || e.seat < 0 || e.seat > 1 || !s.cats.Any(c => c.id == e.cat_id)
                    || !s.visits.Any(v => v.id == e.customer_id && v.cat_id == e.cat_id && v.departure == null && v.seated_at >= 0)
                    || !Rules.types.Any(t => t.id == e.mode) || e.remaining < 1 || e.end_reason != null
                    || !Range(e.stamina, 0, Rules.max_stamina) || !Range(e.tension, 0, Rules.exchange.target) || !Range(e.engagement, 0, Rules.exchange.target)
                    || !Finite(e.bonus) || !Finite(e.affinity_pending) || !Range(e.affinity_start, 0, 100)) throw new ArgumentException("Invalid saved exchange");
            foreach (var r in s.relationships) if (!s.cats.Any(c => c.id == r.cat_id) || string.IsNullOrEmpty(r.customer_id) || !Range(r.affinity, 0, 100)) throw new ArgumentException("Invalid relationship");
            for (int i = 0; i < s.history.Count; i++)
            {
                var h = s.history[i];
                if (h.day != i + 1 || !Finite(h.closing_funds) || !Range(h.revenue, 0, double.MaxValue) || !Range(h.expenses, 0, double.MaxValue)
                    || Math.Abs(h.closing_funds - (h.opening_funds + h.revenue - h.expenses)) > 1e-7
                    || Math.Abs(h.opening_funds - (i == 0 ? Rules.starting_funds : s.history[i - 1].closing_funds)) > 1e-7) throw new ArgumentException("Invalid finance history");
            }
        }
        private static bool Finite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
        private static bool Range(double v, double min, double max) => Finite(v) && v >= min && v <= max;
        private static double Clamp(double v, double max) => Math.Max(0, Math.Min(max, v));
        public void Execute(string kind, string[] working = null, string catId = null, string choice = null, string source = null)
        {
            var s = State;
            if (s.game_over != null) throw new InvalidOperationException("経営終了後は保存・閲覧のみ利用できます。");
            if (kind != "resolve_missing" && s.cats.Any(c => c.activity == "missing" && c.missing_remaining == 0)) throw new InvalidOperationException("帰還結果を確認してください。");
            if (StoreEventWaiting && kind != "resolve_store_event" && kind != "resolve_missing" && !(kind == "day_off" && CurrentStoreEvent.type == "trouble")) throw new InvalidOperationException("店舗イベントに回答してください。");
            switch (kind)
            {
                case "resolve_store_event": ResolveStoreEvent(choice); return;
                case "use_item": UseSupply(source, catId); break;
                case "set_shifts":
                    if (s.phase != "preparation" || working == null || working.Distinct().Count() != working.Length
                        || working.Any(id => !s.cats.Any(c => c.id == id && c.health == "healthy" && c.activity == "cafe"))) throw new ArgumentException("営業準備中に健康な猫の出勤を設定してください。");
                    foreach (var c in s.cats) c.working = working.Contains(c.id);
                    break;
                case "start_business":
                    Require("preparation");
                    if (!s.cats.Any(c => c.working)) throw new InvalidOperationException("出勤猫を選んでください。");
                    s.phase = "open"; Advance(); break;
                case "advance_business": Require("open"); Advance(); break;
                case "next_day":
                    Require("closed");
                    AdvanceToNextDay();
                    break;
                case "day_off":
                    Require("preparation");
                    if (StoreEventWaiting) { CurrentStoreEvent.status = "resolved"; CurrentStoreEvent.choice = "close"; }
                    var planned = s.cats.Where(c => c.working).Select(c => c.id).ToArray();
                    foreach (var c in s.cats) c.working = false;
                    Close(); s.tick = Rules.opening_ticks;
                    foreach (var c in s.cats) c.working = planned.Contains(c.id) && c.health == "healthy" && c.activity == "cafe";
                    if (s.game_over == null) AdvanceToNextDay();
                    s.revision++;
                    return;
                case "resolve_missing":
                    var cat = s.cats.Find(c => c.id == catId && c.activity == "missing" && c.missing_remaining == 0);
                    if (cat == null) throw new ArgumentException("帰還待ちの猫を選んでください。");
                    cat.activity = "cafe"; cat.stamina = Rules.max_stamina; cat.cannot_continue = cat.health != "healthy"; cat.stress = Rules.return_stress;
                    double cost = cat.kitten ? Rules.kitten_cost : 0; s.funds -= cost; s.expenses += cost; CheckEnd(); break;
                default: throw new NotSupportedException("単独版の第1段階では未対応の操作です：" + kind);
            }
            s.revision++;
        }
        private void Require(string phase) { if (State.phase != phase) throw new InvalidOperationException("現在の営業段階では実行できません。"); }
        private void AdvanceToNextDay()
        {
            var s = State;
            s.history.Add(new CafeDayResult { day = s.day, arrivals = s.visits.Count, completed = s.completed, opening_funds = s.opening_funds, revenue = s.revenue, expenses = s.expenses, closing_funds = s.funds, popularity_gain = s.popularity_gain, satisfied = s.visits.Count(v => v.satisfaction != null && v.satisfaction.level == "satisfied"), normal = s.visits.Count(v => v.satisfaction != null && v.satisfaction.level == "normal"), dissatisfied = s.visits.Count(v => v.satisfaction != null && v.satisfaction.level == "dissatisfied") });
            s.day++; s.tick = 0; s.phase = "preparation"; s.opening_funds = s.funds; s.revenue = s.expenses = 0; s.completed = 0;
            s.visits.Clear(); s.queue.Clear(); s.exchanges.Clear(); s.popularity_gain = 0;
            foreach (var c in s.cats) { c.service_ticks = 0; if (c.activity == "cafe") { c.stamina = Rules.max_stamina; c.cannot_continue = c.health != "healthy"; } c.working &= c.health == "healthy" && c.activity == "cafe"; }
            PresentStoreEvent();
        }
        private string Preference(string customer) => Rules.preference_pool[(int)(PythonRandom.Hash64(State.seed + ":" + customer) % (ulong)Rules.preference_pool.Length)];
        private bool Matched(CafeCat c, string customer) => Array.IndexOf(Rules.cats.First(d => d.id == c.id).features, Preference(customer)) >= 0;
        private void Advance()
        {
            var s = State;
            // Assignment precedes arrival, matching CafeInteractionSession.automatic_step.
            for (int seat = 0; seat < 2 && s.queue.Count > 0; seat++)
            {
                if (DisabledSeat(seat) || s.exchanges.Any(e => e.seat == seat)) continue;
                string customer = s.queue[0];
                var cat = s.cats.Where(c => c.working && c.health == "healthy" && c.activity == "cafe" && !c.cannot_continue && c.stamina > 0 && !s.exchanges.Any(e => e.cat_id == c.id))
                    .OrderByDescending(c => Matched(c, customer)).ThenByDescending(c => c.stamina).ThenBy(c => c.id, StringComparer.Ordinal).FirstOrDefault();
                if (cat == null) break;
                s.queue.RemoveAt(0); var visit = s.visits.Find(v => v.id == customer); visit.cat_id = cat.id; visit.seated_at = s.tick;
                var relationship = s.relationships.Find(r => r.cat_id == cat.id && r.customer_id == customer);
                s.exchanges.Add(new CafeExchange { seat = seat, customer_id = customer, cat_id = cat.id, stamina = cat.stamina, remaining = Math.Min(Rules.exchange.ticks, Rules.opening_ticks - s.tick), affinity_start = relationship?.affinity ?? 0, tension_multiplier = Matched(cat, customer) ? Rules.tension_multiplier : 1 });
            }
            s.exchanges.Sort((a, b) => a.seat.CompareTo(b.seat));
            int weekday = (Rules.start_weekday + s.day - 1) % 7;
            int lastNormal = Enumerable.Range(0, Rules.arrival_ticks.Length).Where(i => Rules.patterns[i % Rules.patterns.Length].days.Contains(weekday)).DefaultIfEmpty(-1).Max();
            for (int i = 0; i < Rules.arrival_ticks.Length; i++)
            {
                if (Rules.arrival_ticks[i] != s.tick || !Rules.patterns[i % Rules.patterns.Length].days.Contains(weekday)) continue;
                if (CurrentStoreEvent.type == "rain" && i == lastNormal) continue;
                Arrive("guest-" + (i + 1));
            }
            if (CurrentStoreEvent.type == "festival" && s.tick == Rules.store_events.festival_customer_tick) Arrive("event-guest-" + s.day);
            foreach (var e in s.exchanges.OrderBy(e => e.seat).ToArray())
            {
                var cat = s.cats.Find(c => c.id == e.cat_id); var definition = Rules.cats.First(d => d.id == cat.id);
                Choose(e, out string action, out string target); StepExchange(e, definition, action, target);
                cat.stamina = e.stamina; cat.service_ticks++; var visit = s.visits.Find(v => v.id == e.customer_id); visit.seated_ticks++;
                if (e.end_reason != null)
                {
                    Depart(visit, e.end_reason == "exhausted" ? "interaction_exhausted" : s.tick + 1 >= Rules.opening_ticks ? "closing" : "interaction_time_limit");
                    visit.satisfaction = CafeSatisfactionEvaluator.Evaluate(Rules.satisfaction, e, Matched(cat, e.customer_id));
                    double bonus = e.bonus + visit.satisfaction.bonus;
                    visit.bill += bonus; s.funds += bonus; s.revenue += bonus;
                    var relationship = s.relationships.Find(r => r.cat_id == e.cat_id && r.customer_id == e.customer_id);
                    if (relationship == null) { relationship = new CafeRelationship { cat_id = e.cat_id, customer_id = e.customer_id }; s.relationships.Add(relationship); }
                    relationship.affinity = Clamp(e.affinity_start + e.affinity_pending, 100);
                    s.completed++; s.exchanges.Remove(e); if (cat.stamina == 0) cat.cannot_continue = true;
                }
            }
            foreach (string id in s.queue.ToArray()) { var v = s.visits.Find(r => r.id == id); if (s.tick - v.arrival + 1 >= Rules.max_wait_ticks) Depart(v, "wait_timeout"); }
            if (s.tick + 1 >= Rules.opening_ticks) { foreach (var v in s.visits.Where(v => v.departure == null).ToArray()) Depart(v, "closing"); Close(); }
            s.tick++;
        }
        private void Depart(CafeVisit visit, string reason)
        {
            if (visit.departure != null) return;
            visit.departure = reason; visit.bill = visit.seated_ticks * Rules.time_price;
            State.funds += visit.bill; State.revenue += visit.bill; State.queue.Remove(visit.id);
        }
        private void Arrive(string id)
        {
            var visit = new CafeVisit { id = id, arrival = State.tick }; State.visits.Add(visit);
            if (State.queue.Count >= Rules.queue_capacity) Depart(visit, "queue_full"); else State.queue.Add(id);
        }
        public void Choose(CafeExchange e, out string action, out string target)
        {
            target = null;
            if (e.connect_pending || e.tension >= Rules.exchange.optional_threshold) action = "connect";
            else if (e.stamina <= 20) action = "pause";
            else if (e.previous_reaction == "turn_away" || e.previous_reaction == "confused") { action = "switch"; target = Rules.types[(Array.FindIndex(Rules.types, t => t.id == e.mode) + 1) % Rules.types.Length].id; }
            else action = "direct";
        }
        public void StepExchange(CafeExchange e, CatDefinition p, string action, string target = null)
        {
            var c = Rules.exchange; var t = Rules.types.First(r => r.id == e.mode);
            bool exchange = new[] { "direct", "adapt", "intense", "feint" }.Contains(action);
            if (e.end_reason != null || e.connect_pending && action != "connect" || !(t.actions.Contains(action) || action == "pause" || action == "switch" || action == "connect" && e.tension >= c.optional_threshold)
                || action == "switch" && (target == e.mode || !Rules.types.Any(r => r.id == target)) || action != "switch" && target != null) throw new ArgumentException("Invalid exchange action");
            bool connect = action == "connect", opened = e.open_up_pending || e.engagement >= c.optional_threshold && (e.tension >= c.optional_threshold || e.remaining == 1 || e.stamina <= c.low_stamina);
            e.tension -= connect ? c.gauge_cost : 0; e.engagement -= opened ? c.gauge_cost : 0;
            string reaction = null;
            if (!connect)
            {
                double score = 0, cost = 0;
                reaction = e.stamina <= c.low_stamina ? "listless" : "neutral";
                if (exchange)
                {
                    double gain; int strength = action == "intense" ? 2 : 1;
                    switch (action) { case "adapt": gain = c.adapt_gain; cost = c.adapt_cost; break; case "intense": gain = c.intense_gain; cost = c.intense_cost; break; case "feint": gain = c.feint_gain; cost = c.feint_cost; break; default: gain = c.direct_gain; cost = c.direct_cost; break; }
                    if (action == "adapt" && new[] { "turn_away", "listless", "confused" }.Contains(e.previous_reaction)) { gain = c.gentle_gain; cost = c.gentle_cost; strength = 0; }
                    if (action == "feint" && e.previous_action == "pause" && e.pause_after_interaction) gain = c.combo_gain;
                    bool switched = e.last_group != null && e.last_group != t.group;
                    e.streak = e.last_group == t.group ? e.streak + 1 : 1; e.last_group = t.group;
                    double preference = p.type_preferences[Array.IndexOf(Rules.types, t)], intensity = p.intensity_preferences[strength];
                    score = gain * t.gain * preference * intensity * Math.Max(c.boredom_floor, 1 - p.boredom_decay * (e.streak - 1)) * (switched ? 1 + p.switch_affinity : 1);
                    cost *= t.cost;
                    reaction = preference == 0 || intensity == 0 ? "turn_away" : e.stamina <= c.low_stamina ? "listless" : score < c.confused_threshold ? "confused" : score >= c.enthusiastic_threshold ? "enthusiastic" : score >= c.favorable_threshold ? "favorable" : "neutral";
                }
                e.engagement = Math.Min(c.target, e.engagement + score * (reaction == "turn_away" || reaction == "listless" ? 0 : reaction == "confused" ? c.confused_multiplier : 1));
                e.stamina = Math.Max(0, e.stamina - cost);
                if (action == "pause") e.stamina = Math.Min(c.max_stamina, e.stamina + c.pause_recovery);
                if (action == "switch") e.mode = target;
                e.pause_after_interaction = action == "pause" && new[] { "direct", "adapt", "intense", "feint" }.Contains(e.previous_action);
            }
            else e.pause_after_interaction = false;
            double delta = opened ? c.open_up_tension : !exchange ? 0 : reaction == "enthusiastic" ? c.tension_enthusiastic : reaction == "favorable" ? c.tension_favorable : reaction == "neutral" ? c.tension_neutral : reaction == "confused" ? -c.tension_confused_loss : reaction == "listless" ? -c.tension_listless_loss : -c.tension_turn_away_loss;
            if (delta > 0 && !opened) delta *= e.tension_multiplier;
            e.tension = Clamp(e.tension + delta, c.target);
            e.bonus += (connect ? c.connect_bonus : 0) + (opened ? c.open_up_bonus : 0) + (connect && opened ? c.simultaneous_bonus : 0);
            e.affinity_pending += (exchange ? reaction == "enthusiastic" ? c.affinity_enthusiastic : reaction == "favorable" ? c.affinity_favorable : reaction == "turn_away" ? -c.affinity_turn_away_loss : 0 : 0)
                + (connect ? c.affinity_connect : 0) + (opened ? c.affinity_open_up : 0) + (connect && opened ? c.affinity_simultaneous : 0);
            if (connect) e.connect_count++; if (opened) e.open_up_count++; if (connect && opened) e.simultaneous_count++;
            e.previous_action = action; e.previous_reaction = reaction; e.remaining--;
            e.end_reason = e.stamina == 0 ? "exhausted" : e.remaining == 0 ? "time_limit" : null;
            if (e.end_reason == "exhausted") e.affinity_pending -= c.affinity_exhausted_loss;
            e.connect_pending = e.end_reason == null && e.tension >= c.forced_threshold;
            e.open_up_pending = e.end_reason == null && e.engagement >= c.forced_threshold;
        }
        private void Close()
        {
            var s = State;
            double after = Math.Min(Rules.popularity.cap, s.popularity + s.visits.Count(v => v.satisfaction != null && v.satisfaction.level == "satisfied") * Rules.popularity.gain_per_success + CurrentStoreEvent.popularity);
            s.popularity_gain = after - s.popularity; s.popularity = after;
            foreach (var cat in s.cats.Where(cat => cat.activity == "missing")) if (cat.missing_remaining > 0) cat.missing_remaining--;
            foreach (var cat in s.cats.Where(cat => cat.activity == "cafe"))
            {
                var p = Rules.cats.First(d => d.id == cat.id);
                cat.fatigue = Clamp(cat.fatigue + (cat.working ? cat.service_ticks * Rules.fatigue_per_service_tick * p.service_fatigue : -Rules.rest_day_recovery * p.rest_fatigue), Rules.max_fatigue);
                if (cat.health == "sick") { cat.recovery_remaining = Math.Max(0, cat.recovery_remaining - 1); if (cat.recovery_remaining == 0) cat.health = "healthy"; }
                else
                {
                    double chance = Math.Min(Rules.max_probability, Math.Max(0, cat.fatigue - Rules.safe_fatigue) * Rules.probability_per_fatigue);
                    double draw = PythonRandom.Draw("[\"cafe-health-v1\", " + s.seed + ", " + s.day + ", \"" + cat.id + "\"]");
                    if (draw < chance) { cat.health = "sick"; cat.recovery_remaining = Rules.recovery_days; }
                }
                if (cat.health == "sick") cat.cannot_continue = true;
                cat.stress = Clamp(cat.stress + (cat.working ? cat.service_ticks * Rules.stress_per_service_tick * p.service_stress : -Rules.rest_recovery), 100);
                if (cat.service_ticks > 0 && cat.stress >= Rules.runaway_threshold)
                {
                    cat.activity = "missing"; cat.working = false; cat.missing_remaining = Rules.missing_days;
                    cat.kitten = PythonRandom.Hash64("missing-kitten:" + s.seed + ":missing-" + s.day + "-" + cat.id) / 18446744073709551616.0 < Rules.kitten_probability;
                    s.popularity = Math.Max(0, s.popularity - Rules.popularity_loss);
                }
            }
            double expenses = Rules.base_cost + Rules.per_seat_cost * 2 + (CurrentStoreEvent.choice == "repair" ? Rules.store_events.trouble_cost : 0);
            s.expenses += expenses; s.funds -= expenses; s.phase = "closed"; CheckEnd();
        }
        private void CheckEnd() { if (State.game_over == null) State.game_over = State.funds <= 0 ? "funds" : State.popularity <= 0 ? "popularity" : null; }
    }
}
