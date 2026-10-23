using System.Linq;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private static string PatronConditionLabel(PatronCondition c)
            => string.Join(" かつ ", new[]{PatronFeatureLabel(c.feature),c.specialization==null?"":GrowthLabel(c.specialization)+"専門"}.Where(x=>x!=""));
        private string PatronCurrentGuide(string destination)
        {
            var r=Simulation.Rules.patron;
            var member=r.members.SingleOrDefault(m=>m.id==destination);
            if(member==null){var w=r.destination.welcome;return r.name+"：基本加点＋"+Number(r.gain)+(w==null?"":" / 歓迎："+PatronFeatureLabel(w.feature)+"・"+GrowthLabel(w.specialization)+"専門（各一致で＋"+Number(w.satisfaction_bonus)+"）");}
            var probe=Simulation.State.cats.First();var terms=Simulation.PatronTerms(Simulation.State,probe.id,destination);
            return member.name+"：現在の希望「"+PatronConditionLabel(terms.condition)+"」 / 一致＋"+Number(member.gain)+"、不一致＋"+Number(member.unmatched_gain);
        }
        private string PatronGoalGuide()
            => "\n\n"+string.Join("\n",Simulation.PatronDestinations().Select(d=>PatronCurrentGuide(d.id)))+"\n希望は満足度の条件です。出発可否とは別です。健康・体力・疲労・店に残す猫などの出発条件は派遣画面で確認できます。\n気分が変わる有力者の現在の希望は帰還確認で変化します。派遣済みは出発時の希望・加点で固定されます。\n猫適性の自動成長は目標達成を保証しません。専門は確定後に変更できません。確定前に成長画面の候補を手動で選べます。";
        private string PatronGrowthGuide(CafeGrowth growth)
        {
            var s=Simulation.State;if(s.patron?.status!="active" || s.cats.Single(c=>c.id==growth.cat_id).activity=="adopted")return "";
            var lines=Simulation.Rules.patron.members.Where(m=>s.patron.members.Single(v=>v.id==m.id).value<m.target)
                .SelectMany(m=>m.conditions.Where(c=>c.specialization!=null && (c.feature==null || Simulation.Definition(growth.cat_id).features.Contains(c.feature)))
                .Select(c=>m.name+"の希望候補「"+PatronConditionLabel(c)+"」："+(growth.specialization==null?
                    "専門未確定。"+(Simulation.GrowthSelection(growth)?.choice==c.specialization?"選択中の専門は希望に一致。":"希望に合わせるなら「"+GrowthLabel(c.specialization)+"」を手動選択。"):
                    growth.specialization==c.specialization?"確定済みの専門は一致。":"この猫の専門は変更できません。"))).Distinct().ToArray();
            return lines.Length==0?"":"\n\n有力者目標との関係\n"+string.Join("\n",lines);
        }
    }
}
