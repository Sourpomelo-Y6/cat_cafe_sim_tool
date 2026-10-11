using System.Linq;
using CatCafePreview.Standalone;

namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private static string GrowthLabel(string value)
        {
            switch (value) { case "service": return "接客"; case "rest": return "休養"; case "dispatch": return "派遣（派遣処理は後続移植）";
                case "play": return "遊び"; case "contact": return "触れ合い"; case "quiet": return "静かな交流";
                case "teaser": return "猫じゃらし"; case "ball": return "ボール"; case "plush": return "ぬいぐるみ"; case "tunnel": return "トンネル";
                case "pet": return "なでる"; case "brush": return "ブラッシング"; case "voice": return "声かけ"; case "presence": return "そばにいる"; default: return value ?? "未選択"; }
        }
        private string GrowthDetails(string id)
        {
            var r = Simulation.GrowthCat(id);
            return "経験：接客 " + r.service + " / 休養 " + r.rest + " / 派遣 " + r.dispatch + " / 得意分野 " + GrowthLabel(r.specialization)
                + " / 得意分類 " + string.Join("・", CafeSimulation.LearnedGroups(r).Select(GrowthLabel)) + " / 得意行動 " + string.Join("・", CafeSimulation.LearnedTypes(r).Select(GrowthLabel));
        }
        private PythonGrowthChoice[] GrowthChoices()
        {
            var rules = Simulation.Rules.growth;
            return Simulation.State.growth.Select(r =>
            {
                string kind = Simulation.GrowthPendingKind(r); if (kind == null) return null;
                string[] choices = kind == "resolve_growth" ? new[] { "service", "rest", "dispatch" } : kind == "resolve_growth_mastery" ? Simulation.MasteryChoices(r) : Simulation.TypeMasteryChoices(r);
                return new PythonGrowthChoice { kind = kind, cat_id = r.cat_id, name = Simulation.Definition(r.cat_id).name,
                    title = kind == "resolve_growth" ? "得意分野" : kind == "resolve_growth_mastery" ? "得意な交流分類" : "得意な個別行動",
                    experience = (float)(kind == "resolve_growth" ? r.service + r.rest + r.dispatch : kind == "resolve_growth_mastery" ? (r.mastery == null ? r.mastery_groups.Sum() : r.second_mastery == null ? r.second_mastery_groups.Sum() : r.third_group_practice.groups.Sum()) : Simulation.TypeMasteryExperience(r)),
                    threshold = Simulation.GrowthThreshold(r, kind), service = (float)r.service, rest = (float)r.rest, dispatch = (float)r.dispatch,
                    play = r.groups[0], contact = r.groups[1], quiet = r.groups[2], learned = string.Join("・", CafeSimulation.LearnedGroups(r).Concat(CafeSimulation.LearnedTypes(r)).Select(GrowthLabel)),
                    choices = choices.Select(c => new PythonEventChoice { choice = c, label = GrowthLabel(c), can_select = Simulation.State.game_over == null,
                        detail = kind == "resolve_growth" ? c == "service" ? "接客終了時の消費体力を" + rules.service_stamina_refund * 100 + "%回復" : c == "rest" ? "在店休養時の疲労回復＋" + rules.rest_recovery_bonus : "派遣報酬×" + rules.dispatch_reward_multiplier + "（派遣は未移植）"
                        : "以降に開始する接客の関心の通常増加×" + (kind == "resolve_growth_mastery" ? rules.mastery_engagement_multiplier : rules.type_mastery_engagement_multiplier) }).ToArray() };
            }).Where(r => r != null).ToArray();
        }
        private PythonGrowthPracticeChoice[] GrowthPracticeView()
            => Simulation.State.growth.SelectMany(r => Simulation.GrowthPracticeChoices(r).Select(kind => new PythonGrowthPracticeChoice { cat_id = r.cat_id, kind = kind,
                label = Simulation.Definition(r.cat_id).name + "：" + (kind == "begin_third_group_practice" ? "3つ目の分類の実績を開始" : kind.Contains("third") ? "3つ目の分類の" : "2つ目の分類の") + (kind == "begin_third_group_practice" ? "" : kind.Contains("second_type") ? "追加個別行動実績を開始" : "個別行動実績を開始"),
                can_select = Simulation.State.game_over == null && !Simulation.GrowthWaiting && !Simulation.CustomerTrustWaiting && !Simulation.IntakeWaiting && !Simulation.RegularIntroductionWaiting && !Simulation.StoreEventWaiting && !Simulation.State.cats.Any(c => c.activity == "missing" && c.missing_remaining == 0) })).ToArray();
    }
}
