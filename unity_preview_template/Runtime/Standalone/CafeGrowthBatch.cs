using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
namespace CatCafePreview.Standalone
{
    [Serializable] public class CafeGrowthSelection
    { public string cat_id, key, choice, reason; }
    public sealed partial class CafeSimulation
    {
        public string GrowthSelectionKey(CafeGrowth r) => GrowthPendingKind(r)+"/"+r.specialization+"/"+string.Join(",",LearnedGroups(r))+"/"+string.Join(",",LearnedTypes(r));
        public string[] EligibleGrowthChoices(CafeGrowth r) => GrowthPendingKind(r)=="resolve_growth" ? new[]{"service","rest","dispatch"} : GrowthPendingKind(r)=="resolve_growth_mastery" ? MasteryChoices(r) : TypeMasteryChoices(r);
        public CafeGrowthSelection GrowthSelection(CafeGrowth r) => State.growth_selections?.SingleOrDefault(x=>x.cat_id==r.cat_id && x.key==GrowthSelectionKey(r));
        static double Preference(double[] values,int i) => values!=null && i>=0 && i<values.Length && !double.IsNaN(values[i]) && !double.IsInfinity(values[i]) ? values[i] : 1;
        // Pure ranking: no random draw, money, objective, appearance or current fatigue.
        public CafeGrowthSelection RecommendGrowth(CafeGrowth r)
        {
            var d=Definition(r.cat_id);string kind=GrowthPendingKind(r);
            Func<string,double> strength=t=>Preference(d.intensity_preferences,GrowthGroup(t)=="play"?2:GrowthGroup(t)=="contact"?1:0);
            Func<string,double> type=t=>Preference(d.type_preferences,Array.FindIndex(Rules.types,x=>x.id==t))*strength(t);
            Func<string,double> group=g=>GrowthTypes.Where(t=>GrowthGroup(t)==g).Average(type);
            Func<string,double> score=c=>kind=="resolve_growth" ? c=="service" ? GrowthGroups.Average(group)/Math.Max(.01,(d.service_fatigue+d.service_stress)/2) : c=="rest" ? d.rest_fatigue : d.dispatch_reward/Math.Max(.01,1+d.return_stress/100) : kind=="resolve_growth_mastery" ? group(c) : type(c);
            var ranked=EligibleGrowthChoices(r).Select((c,i)=>new {c,i,value=score(c)}).OrderByDescending(x=>x.value).ThenBy(x=>x.i).ToArray();
            if(ranked.Length==0)throw new InvalidOperationException("選択可能な成長候補がありません。");
            string basis=kind=="resolve_growth" ? "交流・対応する強さの好み（遊び=活発・触れ合い=普通・静か=穏やか）÷接客疲労/ストレス特性、休養回復特性、派遣報酬/帰還ストレス特性を比較" : kind=="resolve_growth_mastery" ? "分類内の交流の好み平均×対応する強さの好み（遊び=活発・触れ合い=普通・静か=穏やか）を比較" : "個別行動の好み×対応する強さの好み（遊び=活発・触れ合い=普通・静か=穏やか）を比較";
            string[] labels={"接客","休養","派遣","遊び","触れ合い","静かな交流","猫じゃらし","ボール","ぬいぐるみ","トンネル","なでる","ブラッシング","声かけ","そばにいる"};
            string[] ids=new[]{"service","rest","dispatch"}.Concat(GrowthGroups).Concat(GrowthTypes).ToArray();
            string values=string.Join(" / ",ranked.Select(x=>labels[Array.IndexOf(ids,x.c)]+"="+x.value.ToString("0.###",CultureInfo.InvariantCulture)));
            return new CafeGrowthSelection {cat_id=r.cat_id,key=GrowthSelectionKey(r),choice=ranked[0].c,reason=basis+"。"+values+"。同点は接客→休養→派遣／遊び→触れ合い→静か／猫じゃらし→ボール→ぬいぐるみ→トンネル→なでる→ブラシ→声→そば。好み情報なしは1。"};
        }
        private void GrowthBatchCommand(string kind,string id,string choice)
        {
            if(State.phase=="open")throw new InvalidOperationException("営業終了後に成長を確認してください。");
            var pending=State.growth.Where(r=>GrowthPendingKind(r)!=null).OrderBy(r=>r.cat_id,StringComparer.Ordinal).ToArray();
            if(pending.Length==0)throw new InvalidOperationException("確定待ちの成長はありません。");
            if(State.growth_selections==null)State.growth_selections=new List<CafeGrowthSelection>();
            if(kind=="resolve_growth_stage")
            {
                var r=pending.SingleOrDefault(x=>x.cat_id==id);if(r==null || !EligibleGrowthChoices(r).Contains(choice))throw new ArgumentException("選択可能な成長を選んでください。");
                State.growth_selections.RemoveAll(x=>x.cat_id==id);State.growth_selections.Add(new CafeGrowthSelection {cat_id=id,key=GrowthSelectionKey(r),choice=choice,reason="自分で選ぶ"});
            }
            else if(kind=="resolve_growth_auto")
            {
                foreach(var r in pending){if(GrowthSelection(r)?.reason=="自分で選ぶ")continue;State.growth_selections.RemoveAll(x=>x.cat_id==r.cat_id);State.growth_selections.Add(RecommendGrowth(r));}
            }
            else
            {
                // Validate the complete batch before applying any effect.
                var selected=pending.Select(r=>GrowthSelection(r)).ToArray();
                for(int i=0;i<pending.Length;i++)if(selected[i]==null || !EligibleGrowthChoices(pending[i]).Contains(selected[i].choice))throw new InvalidOperationException("一覧のすべての成長内容を選んでください。");
                for(int i=0;i<pending.Length;i++)
                {
                    string resolve=GrowthPendingKind(pending[i]);
                    RecordGrowth(new CafeGrowthOperation {kind=resolve,cat_id=pending[i].cat_id,choice=selected[i].choice,decision_reason=selected[i].reason,day=State.day,tick=State.tick});
                    if(State.autoplay?.running==true)
                    {
                        string subject=pending[i].cat_id+"/"+selected[i].key;
                        AutoDecision(subject,selected[i].choice,selected[i].reason);
                        var decision=State.autoplay.decisions.Single(x=>x.day==State.day && x.subject==subject);
                        decision.target=Definition(pending[i].cat_id).name+"："+(resolve=="resolve_growth"?"得意分野":resolve=="resolve_growth_mastery"?"得意な交流分類":"得意な個別行動");
                        string[] ids=new[]{"service","rest","dispatch"}.Concat(GrowthGroups).Concat(GrowthTypes).ToArray();
                        string[] labels={"接客","休養","派遣","遊び","触れ合い","静かな交流","猫じゃらし","ボール","ぬいぐるみ","トンネル","なでる","ブラッシング","声かけ","そばにいる"};
                        decision.choice=labels[Array.IndexOf(ids,selected[i].choice)];
                    }
                }
                State.growth_selections.Clear();
            }
            State.revision++;
        }
        private void ValidateGrowthSelections(CafeSnapshot s)
        {
            if(s.growth_selections==null)return; // Old saves reconstruct candidates from saved experience.
            if(s.growth_selections.Any(x=>x==null) || s.growth_selections.Select(x=>x.cat_id).Distinct().Count()!=s.growth_selections.Count)throw new ArgumentException("Invalid staged growth");
            foreach(var x in s.growth_selections)
            {
                var r=s.growth.SingleOrDefault(g=>g.cat_id==x.cat_id);
                if(r==null || x.key!=GrowthSelectionKey(r) || !EligibleGrowthChoices(r).Contains(x.choice) || string.IsNullOrEmpty(x.reason))throw new ArgumentException("Invalid staged growth choice");
            }
        }
    }
}
