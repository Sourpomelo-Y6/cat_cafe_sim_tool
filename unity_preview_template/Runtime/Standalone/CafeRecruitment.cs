using System;
using System.Linq;
using System.Collections.Generic;
namespace CatCafePreview.Standalone
{
    [Serializable] public class CafeRecruitment {public string id;public List<int> days=new List<int>();public List<CafeShopPurchase> accepted=new List<CafeShopPurchase>();}
    public sealed partial class CafeSimulation
    {
        public IntakeRules[] RecruitmentCandidates(CafeSnapshot s)
        {
            if(s.recruitment==null)return new IntakeRules[0];var result=new List<IntakeRules>();
            var used=new HashSet<string>(Rules.cats.Select(c=>c.id).Concat(new[]{Rules.intake.cat.id,Rules.regular_introduction.cat.id,Rules.visiting_cat?.cat.id}).Concat(ShopRules.Select(r=>r.cat.id)).Concat(DispatchIntroductions(s).Select(e=>e.introduction.cat_id)));
            for(int batch=0;batch<s.recruitment.days.Count;batch++)for(int offset=0;offset<Rules.recruitment_batch_size;offset++)
            {
                var rule=Rules.recruitment_catalog[(batch*Rules.recruitment_batch_size+offset)%Rules.recruitment_catalog.Length];int index=1;while(used.Contains("rescue-"+index))index++;string id="rescue-"+index;used.Add(id);var c=rule.cat;
                result.Add(new IntakeRules {cost=rule.cost,personality=rule.personality,trait=rule.trait,features_label=rule.features_label,cat=new CatDefinition {id=id,name=c.name+(batch==0?"":"（紹介"+(batch+1)+"）"),trait_id=c.trait_id,features=c.features,type_preferences=c.type_preferences,intensity_preferences=c.intensity_preferences,boredom_decay=c.boredom_decay,switch_affinity=c.switch_affinity,service_stress=c.service_stress,service_fatigue=c.service_fatigue,rest_fatigue=c.rest_fatigue,dispatch_reward=c.dispatch_reward,return_stress=c.return_stress}});
            }
            return result.ToArray();
        }
        private int RecruitmentJoinedCount(CafeSnapshot s,int day)=>(s.recruitment?.accepted ?? new List<CafeShopPurchase>()).Count(p=>p!=null && p.day<=day);
        private IEnumerable<CatDefinition> RecruitmentDefinitions(CafeSnapshot s)=>RecruitmentCandidates(s).Where(r=>s.recruitment.accepted.Any(p=>p.cat_id==r.cat.id)).Select(r=>r.cat);
        private double RecruitmentExpenses(CafeSnapshot s,int day)=>s.recruitment==null?0:s.recruitment.accepted.Where(p=>p.day==day).Sum(p=>RecruitmentCandidates(s).Single(r=>r.cat.id==p.cat_id).cost);
        public int NextRecruitmentDay=>State.recruitment==null?State.day:State.recruitment.days.Last()+Rules.recruitment_refresh_days;
        public string RecruitmentRefreshReason(){string reason=ItemsReason();if(reason!="")return reason;return State.day<NextRecruitmentDay?"次の候補追加日までお待ちください。":"";}
        private void OpenRecruitment()
        {
            string reason=RecruitmentRefreshReason();if(State.recruitment!=null && reason!="")return;if(reason!="")throw new InvalidOperationException(reason);
            if(Rules.recruitment_catalog==null)throw new InvalidOperationException("募集設定がありません。");
            if(State.recruitment==null)State.recruitment=new CafeRecruitment {id="recruitment"};State.recruitment.days.Add(State.day);State.revision++;
        }
        public string RecruitReason(string id)
        {
            string reason=ItemsReason();if(reason!="")return reason;var r=RecruitmentCandidates(State).SingleOrDefault(r=>r.cat.id==id);if(r==null)return "受け入れる候補を選んでください。";
            if(State.recruitment.accepted.Any(p=>p.cat_id==id))return "この猫はすでに受け入れています。";
            if(State.funds<=r.cost)return "受け入れ後に資金が残る必要があります。";return State.cats.Count>=HousingCapacity(State,State.day)?"飼育スペースが満員です。":"";
        }
        private void RecruitCat(string id){string reason=RecruitReason(id);if(reason!="")throw new InvalidOperationException(reason);JoinCat(RecruitmentCandidates(State).Single(r=>r.cat.id==id));State.recruitment.accepted.Add(new CafeShopPurchase {cat_id=id,day=State.day});State.revision++;}
        private void ValidateRecruitment(CafeSnapshot s)
        {
            var r=s.recruitment;if(r==null)return;
            if(r.id!="recruitment" || r.days==null || r.days.Count==0 || r.accepted==null || Rules.recruitment_catalog==null || Rules.recruitment_batch_size<1 || Rules.recruitment_refresh_days<1 || Rules.recruitment_catalog.Length%Rules.recruitment_batch_size!=0 || Rules.recruitment_catalog.Length==0)throw new ArgumentException("Invalid recruitment state");
            int previous=0;foreach(int day in r.days){if(day<1 || day>s.day || previous>0 && day-previous<Rules.recruitment_refresh_days)throw new ArgumentException("Invalid recruitment dates");previous=day;}
            var candidates=RecruitmentCandidates(s);if(r.accepted.Select(p=>p?.cat_id).Distinct().Count()!=r.accepted.Count)throw new ArgumentException("Duplicate recruited cat");
            foreach(var p in r.accepted){int index=Array.FindIndex(candidates,c=>c.cat.id==p?.cat_id);if(p==null || index<0 || p.day<r.days[index/Rules.recruitment_batch_size] || p.day>s.day || !s.cats.Any(c=>c.id==p.cat_id))throw new ArgumentException("Invalid recruitment admission");}
        }
    }
}
