using System.Collections.Generic;

namespace CatCafePreview.Standalone
{
    public static class CafeSatisfactionEvaluator
    {
        public static CafeSatisfaction Evaluate(SatisfactionRules rules, CafeExchange exchange, bool matched)
        {
            var reasons = new List<string>();
            bool affinity = System.Math.Max(0, System.Math.Min(100, exchange.affinity_start + exchange.affinity_pending)) > exchange.affinity_start;
            bool special = exchange.connect_count + exchange.open_up_count > 0;
            bool gauges = exchange.tension >= rules.gauge_target && exchange.engagement >= rules.gauge_target;
            if (affinity) reasons.Add("親しみ上昇");
            if (special) reasons.Add("特別行動");
            if (gauges) reasons.Add("関心・テンション");
            if (matched) reasons.Add("好み一致");
            int score = reasons.Count;
            if (exchange.end_reason == "exhausted") { score -= 2; reasons.Add("体力切れ"); }
            string level = score >= rules.satisfied_score ? "satisfied" : score <= rules.dissatisfied_score ? "dissatisfied" : "normal";
            return new CafeSatisfaction { level = level, label = level == "satisfied" ? "満足" : level == "normal" ? "普通" : "不満", score = score, reasons = reasons.ToArray(), matched = matched, special = special, gauges = gauges, bonus = level == "satisfied" ? rules.satisfied_bonus : 0 };
        }
    }
}
