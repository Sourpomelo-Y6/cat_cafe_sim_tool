using System;
using System.Linq;
namespace CatCafePreview.Standalone
{
    // Provisional balance, copied into each save so later tuning never rewrites history.
    [Serializable] public class CafeStaminaRules
    {
        public int version;
        public double initial, increment, cap, experience_per_stage;
        public static bool Missing(CafeStaminaRules r)=>r==null || r.version==0 && r.initial==0 && r.cap==0 && r.increment==0 && r.experience_per_stage==0;
        public static CafeStaminaRules Provisional()=>new CafeStaminaRules {version=1,initial=100,increment=10,cap=300,experience_per_stage=20};
    }
    public sealed partial class CafeSimulation
    {
        internal bool LegacyStaminaReplay; internal CafeStaminaRules ReplayStaminaRules;
        public double MaxStamina(string id)=>MaxStamina(State,id);
        public double MaxStamina(CafeSnapshot state,string id)
        {
            var rules=state.stamina_growth;
            return rules==null?Rules.max_stamina:Math.Min(rules.cap,rules.initial+state.cats.Single(c=>c.id==id).stamina_stage*rules.increment);
        }
        private static double GrowthExperience(CafeGrowth g)=>g.service+g.rest+g.dispatch;
        private void EnableStaminaGrowth()
        {
            if(State.stamina_growth!=null || LegacyStaminaReplay)return;
            var configured=ReplayStaminaRules??Rules.stamina_growth;
            State.stamina_growth=CopyStaminaRules(CafeStaminaRules.Missing(configured)?CafeStaminaRules.Provisional():configured);
            foreach(var c in State.cats)c.stamina_experience_origin=GrowthExperience(GrowthCat(c.id));
        }
        private static CafeStaminaRules CopyStaminaRules(CafeStaminaRules r)=>new CafeStaminaRules {version=r.version,initial=r.initial,increment=r.increment,cap=r.cap,experience_per_stage=r.experience_per_stage};
        private static int StaminaStage(CafeStaminaRules r,double experience)=>r.increment==0?0:(int)Math.Min(Math.Ceiling((r.cap-r.initial)/r.increment),Math.Floor(experience/r.experience_per_stage));
        private void ApplyStaminaGrowth(string id)
        {
            var r=State.stamina_growth;if(r==null)return;
            var c=State.cats.Single(c=>c.id==id);double before=MaxStamina(id);
            c.stamina_stage=StaminaStage(r,GrowthExperience(GrowthCat(id))-c.stamina_experience_origin);
            double delta=MaxStamina(id)-before;
            if(delta>0){c.stamina=Math.Min(MaxStamina(id),c.stamina+delta);if(c.health=="healthy")c.cannot_continue=false;ReplayActions.Add("最大体力の成長："+id+" / "+before+" → "+MaxStamina(id)+" / 現在体力＋"+delta);}
        }
        private void ValidateStaminaGrowth(CafeSnapshot s)
        {
            var r=s.stamina_growth;
            if(r!=null && (r.version!=1 || !Range(r.initial,1,10000) || !Range(r.cap,r.initial,10000) || !Range(r.increment,0,10000) || !Range(r.experience_per_stage,1,1000000)))throw new ArgumentException("Invalid stamina growth rules");
            foreach(var c in s.cats)
            {
                var g=s.growth.SingleOrDefault(g=>g.cat_id==c.id);if(g==null)throw new ArgumentException("Missing growth cat");
                if(r==null?c.stamina_stage!=0 || c.stamina_experience_origin!=0:!Range(c.stamina_experience_origin,0,GrowthExperience(g)) || c.stamina_stage!=StaminaStage(r,GrowthExperience(g)-c.stamina_experience_origin))throw new ArgumentException("Stamina growth disagrees with experience");
            }
        }
    }
}
