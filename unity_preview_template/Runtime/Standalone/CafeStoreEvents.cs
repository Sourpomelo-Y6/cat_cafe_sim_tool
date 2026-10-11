using System;
using System.Linq;

namespace CatCafePreview.Standalone
{
    public sealed partial class CafeSimulation
    {
        private static readonly string[] StoreTypes = { "festival", "rain", "supplies", "trouble", "support" };
        public CafeStoreEvent CurrentStoreEvent => State.store_events[State.day - 1];
        public bool StoreEventWaiting => CurrentStoreEvent.status == "waiting";
        public static double StoreDraw(int seed, int day, string label) => PythonRandom.Hash64("store-event:" + seed + ":" + day + ":" + label) / 18446744073709551616.0;
        private CafeStoreEvent DrawStoreEvent(int seed, int day)
        {
            double draw = StoreDraw(seed, day, "occur");
            string type = draw < Rules.store_events.probability ? StoreTypes[(int)(StoreDraw(seed, day, "type") * StoreTypes.Length)] : null;
            return new CafeStoreEvent { day = day, draw = draw, type = type, status = type == "trouble" || type == "support" ? "waiting" : null };
        }
        private void PresentStoreEvent() => State.store_events.Add(DrawStoreEvent(State.seed, State.day));
        public bool DisabledSeat(int seat) => CurrentStoreEvent.choice == "patch" && seat >= Math.Max(0, 2 - Rules.store_events.trouble_seat_loss);
        public string[] AvailableSupplies => State.store_events.Where(e => e.type == "supplies").Select(e => "store-event-" + e.day).Where(source => !State.item_uses.Any(u => u.source == source)).ToArray();
        private void ResolveStoreEvent(string choice)
        {
            Require("preparation"); var e = CurrentStoreEvent; var rules = Rules.store_events;
            if (!StoreEventWaiting || !(e.type == "trouble" ? new[] { "repair", "patch", "close" } : new[] { "full", "small", "decline" }).Contains(choice)) throw new ArgumentException("店舗イベントの選択が不正です。");
            if (choice == "close") { Execute("day_off"); return; }
            e.status = "resolved"; e.choice = choice;
            if (e.type == "support")
            {
                e.cost = choice == "full" ? rules.support_full_cost : choice == "small" ? rules.support_small_cost : 0;
                e.popularity = choice == "full" ? rules.support_full_popularity : choice == "small" ? rules.support_small_popularity : 0;
                State.expenses += e.cost; State.funds -= e.cost; CheckEnd();
            }
            State.revision++;
        }
        private void UseSupply(string source, string catId)
        {
            Require("preparation");
            var cat = State.cats.Find(c => c.id == catId && c.activity == "cafe");
            if (!AvailableSupplies.Contains(source) || cat == null || cat.stress <= 0) throw new ArgumentException("所持している用品とストレスのある在店猫を選んでください。");
            double before = cat.stress; cat.stress = Math.Max(0, before - Rules.store_events.supply.stress_relief);
            State.item_uses.Add(new CafeSupplyUse { source = source, cat_id = cat.id, day = State.day, before = before, after = cat.stress });
        }
        private void ValidateStoreEvents(CafeSnapshot s)
        {
            if (s.store_events == null || s.store_events.Count != s.day || s.item_uses == null) throw new ArgumentException("Invalid store event history");
            for (int i = 0; i < s.store_events.Count; i++)
            {
                var e = s.store_events[i]; var expected = DrawStoreEvent(s.seed, i + 1); var r = Rules.store_events;
                // Unity's JSON double parser can round the stored draw by one ULP.
                // The event kind is still checked against the deterministic seed.
                if (e == null || e.day != i + 1 || e.type != expected.type || !Finite(e.draw) || Math.Abs(e.draw - expected.draw) > 1e-15) throw new ArgumentException("Invalid store event draw");
                if (expected.status == null)
                {
                    if (e.status != null || e.choice != null || e.cost != 0 || e.popularity != 0) throw new ArgumentException("Invalid automatic event");
                    continue;
                }
                bool waiting = e.status == "waiting";
                string[] allowed = e.type == "trouble" ? new[] { "repair", "patch", "close" } : new[] { "full", "small", "decline" };
                double cost = e.type == "support" ? e.choice == "full" ? r.support_full_cost : e.choice == "small" ? r.support_small_cost : 0 : 0;
                double popularity = e.type == "support" ? e.choice == "full" ? r.support_full_popularity : e.choice == "small" ? r.support_small_popularity : 0 : 0;
                if (e.status != "waiting" && e.status != "resolved" || waiting != (e.choice == null) || !waiting && !allowed.Contains(e.choice)
                    || waiting && (e.day != s.day || s.phase != "preparation") || e.cost != cost || e.popularity != popularity
                    || e.choice == "close" && e.day == s.day && s.phase != "closed") throw new ArgumentException("Invalid store event answer");
            }
            if (s.item_uses.Select(u => u.source).Distinct().Count() != s.item_uses.Count) throw new ArgumentException("Duplicate supply consumption");
            int lastDay = 0;
            foreach (var u in s.item_uses)
            {
                var reward = s.store_events.Find(e => "store-event-" + e.day == u.source && e.type == "supplies");
                if (reward == null || u.day < Math.Max(lastDay, reward.day) || u.day > s.day || !s.cats.Any(c => c.id == u.cat_id)
                    || !Range(u.before, double.Epsilon, 100) || u.after != Math.Max(0, u.before - Rules.store_events.supply.stress_relief)) throw new ArgumentException("Invalid supply use");
                lastDay = u.day;
            }
        }
    }
}
