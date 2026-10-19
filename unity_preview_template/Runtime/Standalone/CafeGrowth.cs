using System;
using System.Collections.Generic;
using System.Linq;

namespace CatCafePreview.Standalone
{
    [Serializable] public class GrowthRules
    {
        public int threshold, mastery_threshold, type_mastery_threshold, second_mastery_threshold, second_type_mastery_threshold;
        public double service_xp, rest_xp, dispatch_xp, service_stamina_refund, rest_recovery_bonus, dispatch_reward_multiplier, mastery_engagement_multiplier, type_mastery_engagement_multiplier;
    }
    // Separate leaf type keeps Unity JsonUtility from dropping recursive fields.
    [Serializable] public class GrowthTypePractice
    {
        public int started_day, selected_day; public int[] actions = new int[8]; public string type_mastery;
        public virtual void Normalize() { if (type_mastery == "") type_mastery = null; }
    }
    [Serializable] public class GrowthPractice : GrowthTypePractice
    {
        public GrowthTypePractice second;
        public override void Normalize() { base.Normalize(); if (second != null && second.started_day == 0) second = null; second?.Normalize(); }
    }
    [Serializable] public class ThirdGrowthPractice
    {
        public int started_day, selected_day; public int[] groups = new int[3]; public string mastery; public GrowthPractice individual;
        public void Normalize() { if (mastery == "") mastery = null; if (individual != null && individual.started_day == 0) individual = null; individual?.Normalize(); }
    }
    [Serializable] public class CafeGrowth
    {
        public string cat_id, specialization, mastery, second_mastery, type_mastery, second_type_mastery;
        public int selected_day, mastery_selected_day, second_mastery_selected_day, type_mastery_selected_day, second_type_mastery_selected_day;
        public double service, rest, dispatch;
        public int[] type_actions = new int[8], groups = new int[3], mastery_groups = new int[3], second_mastery_groups = new int[3], type_mastery_actions = new int[8], second_type_mastery_actions = new int[8];
        public GrowthPractice second_group_type_practice; public ThirdGrowthPractice third_group_practice;
        public void Normalize()
        {
            if (specialization == "") specialization = null; if (mastery == "") mastery = null; if (second_mastery == "") second_mastery = null;
            if (type_mastery == "") type_mastery = null; if (second_type_mastery == "") second_type_mastery = null;
            if (second_group_type_practice != null && second_group_type_practice.started_day == 0) second_group_type_practice = null;
            if (third_group_practice != null && third_group_practice.started_day == 0) third_group_practice = null;
            second_group_type_practice?.Normalize(); third_group_practice?.Normalize();
        }
    }
    [Serializable] public class CafeGrowthOperation
    {
        public string kind, cat_id, customer_id, choice, decision_reason; public int stamina_policy_version; public int day, tick; public int[] actions = new int[8]; public double affinity_delta, stamina_spent;
    }
    public sealed partial class CafeSimulation
    {
        public static readonly string[] GrowthTypes = { "teaser", "ball", "plush", "tunnel", "pet", "brush", "voice", "presence" };
        public static readonly string[] GrowthGroups = { "play", "contact", "quiet" };
        public static string GrowthGroup(string type) { int i = Array.IndexOf(GrowthTypes, type); return i < 0 ? null : GrowthGroups[i < 4 ? 0 : i < 6 ? 1 : 2]; }
        public CafeGrowth GrowthCat(string id) => State.growth.Single(r => r.cat_id == id);
        private void EnsureGrowthCat(string id) { EnsurePlayerCat(id);if (!State.growth.Any(r => r.cat_id == id)) State.growth.Add(new CafeGrowth { cat_id = id }); }
        public static string[] LearnedGroups(CafeGrowth r) => new[] { r.mastery, r.second_mastery, r.third_group_practice?.mastery }.Where(v => v != null).ToArray();
        public static string[] LearnedTypes(CafeGrowth r) => new[] { r.type_mastery, r.second_type_mastery, r.second_group_type_practice?.type_mastery, r.second_group_type_practice?.second?.type_mastery, r.third_group_practice?.individual?.type_mastery, r.third_group_practice?.individual?.second?.type_mastery }.Where(v => v != null).ToArray();
        internal bool LegacyGrowthReplay;
        public bool GrowthWaiting => (LegacyGrowthReplay || State.phase != "open") && State.growth.Any(r => GrowthPendingKind(r) != null);
        public string GrowthPendingKind(CafeGrowth r)
        {
            if (r.specialization == null && r.service + r.rest + r.dispatch >= Rules.growth.threshold) return "resolve_growth";
            if (MasteryChoices(r).Length > 0) return "resolve_growth_mastery";
            return TypeMasteryChoices(r).Length > 0 ? "resolve_growth_type_mastery" : null;
        }
        public string[] MasteryChoices(CafeGrowth r)
        {
            if (r.second_mastery != null)
                return r.third_group_practice?.mastery == null && r.third_group_practice != null && r.third_group_practice.groups.Sum() >= Rules.growth.second_mastery_threshold
                    ? GrowthGroups.Where((g,i) => !LearnedGroups(r).Contains(g) && r.third_group_practice.groups[i] >= Rules.growth.second_mastery_threshold).ToArray() : new string[0];
            if (r.mastery != null) return GrowthGroups.Where((g,i) => g != r.mastery && r.second_mastery_groups[i] >= Rules.growth.second_mastery_threshold).ToArray();
            return r.specialization == "service" && r.mastery_groups.Sum() >= Rules.growth.mastery_threshold ? GrowthGroups.Where((g,i) => r.mastery_groups[i] > 0).ToArray() : new string[0];
        }
        private GrowthTypePractice TypePractice(CafeGrowth r, out string group, out string stage)
        {
            group = r.mastery; stage = "first";
            if (r.mastery != null && r.type_mastery == null && r.type_mastery_actions.Sum() >= Rules.growth.type_mastery_threshold) return new GrowthPractice { actions = r.type_mastery_actions };
            stage = "second";
            if (r.type_mastery != null && r.second_type_mastery == null && r.second_type_mastery_actions.Sum() >= Rules.growth.second_type_mastery_threshold) return new GrowthPractice { actions = r.second_type_mastery_actions };
            var practices = new GrowthTypePractice[] { r.second_group_type_practice, r.second_group_type_practice?.second, r.third_group_practice?.individual, r.third_group_practice?.individual?.second };
            string[] stages = { "second_group", "second_group_second", "third_group", "third_group_second" };
            for (int i = 0; i < practices.Length; i++)
                if (practices[i] != null && practices[i].type_mastery == null && practices[i].actions.Sum() >= (i % 2 == 0 ? Rules.growth.type_mastery_threshold : Rules.growth.second_type_mastery_threshold))
                { group = i < 2 ? r.second_mastery : r.third_group_practice.mastery; stage = stages[i]; return practices[i]; }
            return null;
        }
        public int TypeMasteryExperience(CafeGrowth r) => TypePractice(r, out _, out _)?.actions.Sum() ?? 0;
        public int GrowthThreshold(CafeGrowth r, string kind)
        {
            if (kind == "resolve_growth") return Rules.growth.threshold;
            if (kind == "resolve_growth_mastery") return r.mastery == null ? Rules.growth.mastery_threshold : Rules.growth.second_mastery_threshold;
            TypePractice(r, out _, out string stage); return stage.EndsWith("second", StringComparison.Ordinal) ? Rules.growth.second_type_mastery_threshold : Rules.growth.type_mastery_threshold;
        }
        public string[] TypeMasteryChoices(CafeGrowth r)
        {
            var p = TypePractice(r, out string group, out _);
            return p == null ? new string[0] : GrowthTypes.Where((t,i) => GrowthGroup(t) == group && !LearnedTypes(r).Contains(t) && p.actions[i] > 0).ToArray();
        }
        public string[] GrowthPracticeChoices(CafeGrowth r)
        {
            var choices = new List<string>();
            if (r.second_mastery != null && r.second_group_type_practice == null) choices.Add("begin_second_group_type_practice");
            if (r.second_group_type_practice?.type_mastery != null && r.second_group_type_practice.second == null) choices.Add("begin_second_group_second_type_practice");
            if (r.second_mastery != null && r.third_group_practice == null) choices.Add("begin_third_group_practice");
            if (r.third_group_practice?.mastery != null && r.third_group_practice.individual == null) choices.Add("begin_third_group_type_practice");
            if (r.third_group_practice?.individual?.type_mastery != null && r.third_group_practice.individual.second == null) choices.Add("begin_third_group_second_type_practice");
            return choices.ToArray();
        }
        private void RecordGrowth(CafeGrowthOperation op)
        {
            double before=GrowthExperience(GrowthCat(op.cat_id));bool upgrading=State.stamina_growth==null;
            ApplyGrowthOperation(GrowthCat(op.cat_id), op);
            EnableStaminaGrowth();
            if(upgrading && State.stamina_growth!=null)State.cats.Single(c=>c.id==op.cat_id).stamina_experience_origin=before;
            op.stamina_policy_version=LegacyStaminaReplay?0:1;State.growth_operations.Add(op); ApplyStaminaGrowth(op.cat_id);
        }
        private void ResolveGrowth(string kind, string id, string choice)
        {
            if (!LegacyGrowthReplay && State.phase == "open") throw new InvalidOperationException("成長は営業終了後に確定してください。");
            var row = State.growth.Find(r => r.cat_id == id);
            if (row == null) throw new ArgumentException("成長できる猫を選んでください。");
            RecordGrowth(new CafeGrowthOperation { kind = kind, cat_id = id, choice = choice, day = State.day, tick = State.tick }); State.growth_selections?.RemoveAll(x=>x.cat_id==id); State.revision++;
        }
        private void BeginGrowthPractice(string kind, string id)
        {
            if (GrowthWaiting) throw new InvalidOperationException("猫の成長・習熟を選択してください。");
            if (!State.growth.Any(r => r.cat_id == id)) throw new ArgumentException("実績を開始する猫を選んでください。");
            RecordGrowth(new CafeGrowthOperation { kind = kind, cat_id = id, day = State.day, tick = State.tick });
        }
        private void GrowthService(string id, CafeVisit v)
        {
            RecordGrowth(new CafeGrowthOperation { kind = "service", cat_id = id, customer_id = v.id, day = State.day, tick = State.tick, actions = (int[])v.type_actions.Clone(), affinity_delta = v.affinity_delta, stamina_spent = v.stamina_spent });
            if (GrowthCat(id).specialization == "service") State.cats.Single(c => c.id == id).stamina = Math.Min(MaxStamina(id), State.cats.Single(c => c.id == id).stamina + v.stamina_spent * Rules.growth.service_stamina_refund);
        }
        private void GrowthRestDay()
        {
            State.resting_cats = State.cats.Where(c => c.activity == "cafe" && !c.working).Select(c => c.id).ToList();
            foreach (string id in State.resting_cats) RecordGrowth(new CafeGrowthOperation { kind = "rest", cat_id = id, day = State.day, tick = State.tick });
        }
        internal void ApplyGrowthBoundary(CafeGrowthOperation op) => ApplyGrowthOperation(GrowthCat(op.cat_id), op);
        private void ApplyGrowthOperation(CafeGrowth r, CafeGrowthOperation op)
        {
            if (op.kind == "start") return;
            if(op.kind=="dispatch"){r.dispatch+=Rules.growth.dispatch_xp;return;}
            if (op.kind == "rest") { r.rest += Rules.growth.rest_xp; return; }
            if (op.kind == "service")
            {
                r.service += Rules.growth.service_xp;
                for (int i = 0; i < GrowthTypes.Length; i++)
                {
                    int count = op.actions[i], g = Array.IndexOf(GrowthGroups, GrowthGroup(GrowthTypes[i]));
                    r.type_actions[i] += count; r.groups[g] += count;
                    if (op.affinity_delta <= 0) continue;
                    r.mastery_groups[g] += count;
                    if (r.mastery != null && r.mastery != GrowthGroups[g]) r.second_mastery_groups[g] += count;
                    if (r.mastery == GrowthGroups[g])
                    { r.type_mastery_actions[i] += count; if (r.type_mastery != null && r.type_mastery != GrowthTypes[i]) r.second_type_mastery_actions[i] += count; }
                    if (r.third_group_practice != null && !new[] { r.mastery, r.second_mastery }.Contains(GrowthGroups[g])) r.third_group_practice.groups[g] += count;
                    foreach (var p in new[] { GrowthGroups[g] == r.second_mastery ? r.second_group_type_practice : null, GrowthGroups[g] == r.third_group_practice?.mastery ? r.third_group_practice?.individual : null })
                        if (p != null) { p.actions[i] += count; if (p.second != null && p.type_mastery != GrowthTypes[i]) p.second.actions[i] += count; }
                }
                return;
            }
            if (op.kind == "resolve_growth")
            {
                if (r.specialization != null || r.service + r.rest + r.dispatch < Rules.growth.threshold || !new[] { "service", "rest", "dispatch" }.Contains(op.choice)) throw new ArgumentException("成長できる猫と得意分野を選んでください。");
                r.specialization = op.choice; r.selected_day = op.day; return;
            }
            if (op.kind == "resolve_growth_mastery")
            {
                if (!MasteryChoices(r).Contains(op.choice)) throw new ArgumentException("熟練できる猫と接客分類を選んでください。");
                if (r.second_mastery != null) { r.third_group_practice.mastery = op.choice; r.third_group_practice.selected_day = op.day; }
                else if (r.mastery != null) { r.second_mastery = op.choice; r.second_mastery_selected_day = op.day; }
                else { r.mastery = op.choice; r.mastery_selected_day = op.day; } return;
            }
            if (op.kind == "resolve_growth_type_mastery")
            {
                if (!TypeMasteryChoices(r).Contains(op.choice)) throw new ArgumentException("熟練できる猫と個別行動を選んでください。");
                var p = TypePractice(r, out _, out string stage);
                if (stage == "first") { r.type_mastery = op.choice; r.type_mastery_selected_day = op.day; }
                else if (stage == "second") { r.second_type_mastery = op.choice; r.second_type_mastery_selected_day = op.day; }
                else { p.type_mastery = op.choice; p.selected_day = op.day; } return;
            }
            if (!GrowthPracticeChoices(r).Contains(op.kind)) throw new ArgumentException("習得済みの猫の追加実績を開始してください。");
            var practice = new GrowthPractice { started_day = op.day };
            switch (op.kind)
            {
                case "begin_second_group_type_practice": r.second_group_type_practice = practice; break;
                case "begin_second_group_second_type_practice": r.second_group_type_practice.second = new GrowthTypePractice { started_day = op.day }; break;
                case "begin_third_group_practice": r.third_group_practice = new ThirdGrowthPractice { started_day = op.day }; break;
                case "begin_third_group_type_practice": r.third_group_practice.individual = practice; break;
                case "begin_third_group_second_type_practice": r.third_group_practice.individual.second = new GrowthTypePractice { started_day = op.day }; break;
            }
        }
        private static bool SameGrowth(CafeGrowth a, CafeGrowth b)
        {
            bool P(GrowthTypePractice x, GrowthTypePractice y) => x == null ? y == null : y != null && x.started_day == y.started_day && x.selected_day == y.selected_day && x.type_mastery == y.type_mastery && x.actions.SequenceEqual(y.actions) && (!(x is GrowthPractice xp) || y is GrowthPractice yp && P(xp.second,yp.second));
            return a.cat_id == b.cat_id && a.service == b.service && a.rest == b.rest && a.dispatch == b.dispatch && a.specialization == b.specialization && a.selected_day == b.selected_day
                && a.mastery == b.mastery && a.mastery_selected_day == b.mastery_selected_day && a.second_mastery == b.second_mastery && a.second_mastery_selected_day == b.second_mastery_selected_day
                && a.type_mastery == b.type_mastery && a.type_mastery_selected_day == b.type_mastery_selected_day && a.second_type_mastery == b.second_type_mastery && a.second_type_mastery_selected_day == b.second_type_mastery_selected_day
                && a.type_actions.SequenceEqual(b.type_actions) && a.groups.SequenceEqual(b.groups) && a.mastery_groups.SequenceEqual(b.mastery_groups) && a.second_mastery_groups.SequenceEqual(b.second_mastery_groups)
                && a.type_mastery_actions.SequenceEqual(b.type_mastery_actions) && a.second_type_mastery_actions.SequenceEqual(b.second_type_mastery_actions) && P(a.second_group_type_practice,b.second_group_type_practice)
                && (a.third_group_practice == null ? b.third_group_practice == null : b.third_group_practice != null && a.third_group_practice.started_day == b.third_group_practice.started_day && a.third_group_practice.selected_day == b.third_group_practice.selected_day && a.third_group_practice.mastery == b.third_group_practice.mastery && a.third_group_practice.groups.SequenceEqual(b.third_group_practice.groups) && P(a.third_group_practice.individual,b.third_group_practice.individual));
        }
        private void ValidateGrowth(CafeSnapshot s)
        {
            if (s.growth == null || s.growth_operations == null || s.resting_cats == null || s.growth.Count != s.cats.Count || s.growth.Any(r => r == null) || s.growth.Select(r => r.cat_id).Distinct().Count() != s.cats.Count || s.growth.Any(r => !s.cats.Any(c => c.id == r.cat_id))) throw new ArgumentException("Invalid growth cats");
            var replay = s.growth.Select(r => new CafeGrowth { cat_id = r.cat_id }).ToList(); int previous = 0;
            foreach (var op in s.growth_operations)
            {
                var row = op == null ? null : replay.Find(r => r.cat_id == op.cat_id);
                if (row == null || op.day < 1 || op.day > s.day || op.tick < 0 || op.tick > Rules.opening_ticks || (op.day - 1) * (Rules.opening_ticks + 1) + op.tick < previous
                    || op.actions == null || op.actions.Length != 8 || op.actions.Any(v => v < 0) || !Range(op.affinity_delta, -100, 100) || !Range(op.stamina_spent, 0, double.MaxValue)) throw new ArgumentException("Invalid growth operation");
                int joined = JoinedDay(s,op.cat_id);
                if (op.day < joined) throw new ArgumentException("Growth predates cat admission");
                previous = (op.day - 1) * (Rules.opening_ticks + 1) + op.tick;
                if (op.kind == "start")
                {
                    var active = s.exchanges.SingleOrDefault(e => e.customer_id == op.customer_id && op.day == s.day);
                    if (active != null && (active.cat_id != op.cat_id || !LearnedGroups(row).SequenceEqual(active.growth_groups) || !LearnedTypes(row).SequenceEqual(active.growth_types))) throw new ArgumentException("Saved growth terms differ from assignment");
                }
                if (op.kind == "service")
                {
                    var record = op.day == s.day ? s.visits.Where(v => v.satisfaction != null).Select(v => new CafeCustomerRecord { customer_id = v.id, cat_id = v.cat_id, type_actions = v.type_actions, affinity_delta = v.affinity_delta, stamina_spent = v.stamina_spent }).SingleOrDefault(v => v.customer_id == op.customer_id)
                        : s.history[op.day - 1].customer_records.SingleOrDefault(v => v.customer_id == op.customer_id);
                    if (record == null || record.cat_id != op.cat_id || record.type_actions == null || !record.type_actions.SequenceEqual(op.actions) || record.affinity_delta != op.affinity_delta || record.stamina_spent != op.stamina_spent) throw new ArgumentException("Growth disagrees with service history");
                }
                ApplyGrowthOperation(row, op);
            }
            foreach (var row in replay) if (!SameGrowth(row, s.growth.Single(r => r.cat_id == row.cat_id))) throw new ArgumentException("Growth disagrees with replay");
            for (int day = 1; day <= s.day; day++)
            {
                int count = day == s.day ? s.visits.Count(v => v.satisfaction != null) : s.history[day - 1].completed;
                var services = s.growth_operations.Where(o => o.day == day && o.kind == "service").ToArray();
                if (services.Length != count || services.Select(o => o.customer_id).Distinct().Count() != count) throw new ArgumentException("Missing growth service");
                int assigned = day == s.day ? s.visits.Count(v => v.cat_id != null) : s.history[day - 1].customer_records.Count(v => v.cat_id != null);
                var starts = s.growth_operations.Where(o => o.day == day && o.kind == "start").ToArray();
                if (starts.Length != assigned || starts.Select(o => o.customer_id).Distinct().Count() != assigned) throw new ArgumentException("Missing growth assignment");
                var rest = day == s.day ? s.resting_cats : s.history[day - 1].resting_cats;
                var recorded = s.growth_operations.Where(o => o.day == day && o.kind == "rest").Select(o => o.cat_id).ToArray();
                if (rest == null || recorded.Length != rest.Count || recorded.Distinct().Count() != recorded.Length || recorded.Any(id => !rest.Contains(id))) throw new ArgumentException("Invalid growth rest history");
            }
            foreach (var e in s.exchanges)
                if (e.growth_groups == null || e.growth_types == null || e.type_actions == null || e.type_actions.Length != 8 || e.type_actions.Any(v => v < 0) || !Range(e.stamina_spent, 0, double.MaxValue)
                    || e.growth_groups.Any(g => !LearnedGroups(s.growth.Single(r => r.cat_id == e.cat_id)).Contains(g)) || e.growth_types.Any(t => !LearnedTypes(s.growth.Single(r => r.cat_id == e.cat_id)).Contains(t))) throw new ArgumentException("Invalid exchange growth terms");
        }
    }
}
