using System;
using System.Linq;
using System.Collections.Generic;
namespace CatCafePreview.Standalone
{
    [Serializable] public class DispatchIntroductionRule { public string destination;public IntakeRules candidate; }
    // Personality/traits/features are fixed by the save's immutable rules_id; identity and ordinal are fixed at departure.
    [Serializable] public class CafeDispatchIntroduction { public string cat_id,name,status="scheduled";public double cost;public int presented_day,resolved_day; }
    public sealed partial class CafeSimulation
    {
        private IEnumerable<CafeDispatch> DispatchIntroductions(CafeSnapshot s)=> (s.dispatches??new List<CafeDispatch>()).Where(e=>e!=null && e.introduction!=null);
        public bool DispatchIntroductionWaiting=>DispatchIntroductions(State).Any(e=>e.introduction.status=="waiting");
        public DispatchIntroductionRule DispatchIntroductionRule(string destination)=>Rules.dispatch_introductions?.SingleOrDefault(r=>r.destination==destination);
        private int DispatchJoinedCount(CafeSnapshot s,int day)=>DispatchIntroductions(s).Count(e=>e.introduction.status=="accepted" && e.introduction.resolved_day<=day);
        private int JoinedDay(CafeSnapshot s,string id)=>Rules.cats.Any(c=>c.id==id)?1:id==Rules.intake.cat.id?s.intake_request.resolved_day:id==Rules.regular_introduction.cat.id?s.regular_introduction.resolved_day:DispatchIntroductions(s).Single(e=>e.introduction.cat_id==id && e.introduction.status=="accepted").introduction.resolved_day;
        private CatDefinition DispatchCandidate(CafeDispatch e)
        {
            var c=DispatchIntroductionRule(e.destination).candidate.cat;var v=e.introduction;
            return new CatDefinition {id=v.cat_id,name=v.name,trait_id=c.trait_id,features=c.features,type_preferences=c.type_preferences,intensity_preferences=c.intensity_preferences,boredom_decay=c.boredom_decay,switch_affinity=c.switch_affinity,service_stress=c.service_stress,service_fatigue=c.service_fatigue,rest_fatigue=c.rest_fatigue,dispatch_reward=c.dispatch_reward,return_stress=c.return_stress};
        }
        private void AttachDispatchIntroduction(CafeDispatch e)
        {
            var rule=DispatchIntroductionRule(e.destination);if(rule==null || e.destination=="cat_photo_studio" && DispatchIntroductions(State).Any(x=>x.destination==e.destination))return;
            var used=new HashSet<string>(State.cats.Select(c=>c.id).Concat(DispatchIntroductions(State).Select(x=>x.introduction.cat_id)).Concat(new[]{Rules.intake.cat.id,Rules.regular_introduction.cat.id}));int index=1;while(used.Contains("dispatch-rescue-"+index))index++;
            e.introduction=new CafeDispatchIntroduction {cat_id="dispatch-rescue-"+index,name=rule.candidate.cat.name+"（派遣紹介"+(DispatchIntroductions(State).Count()+1)+"）",cost=rule.candidate.cost};
        }
        private void PresentDispatchIntroductions()
        {
            if(State.phase!="preparation" || State.game_over!=null)return;
            foreach(var e in DispatchIntroductions(State).Where(e=>e.status=="resolved" && e.introduction.status=="scheduled")){e.introduction.status="waiting";e.introduction.presented_day=State.day;}
        }
        public string DispatchIntroductionResponseReason(string id)
        {
            if(State.game_over!=null)return "経営終了後は回答できません。";
            if(DispatchWaiting || DispatchChoiceWaiting || DispatchTroubleWaiting)return "派遣の帰還結果を確認してください。";
            if(State.cats.Any(c=>c.activity=="missing" && c.missing_remaining==0 && !DispatchMissing(State,c.id)))return "帰還結果を確認してください。";
            if(GoalWaiting || PatronWaiting)return "目標の結果を確認してください。";
            if(ReservationWaiting)return "特別予約に回答してください。";
            if(CustomerTrustWaiting)return "顧客の信頼回復イベントに回答してください。";
            if(StoreEventWaiting)return "店舗イベントに回答してください。";
            if(GrowthWaiting)return "猫の成長・習熟を選択してください。";
            return State.phase=="preparation" && DispatchIntroductions(State).Any(e=>e.id==id && e.introduction.status=="waiting")?"":"回答待ちの猫紹介を営業準備中に確認してください。";
        }
        public string DispatchIntroductionAcceptReason(string id)
        {
            string reason=DispatchIntroductionResponseReason(id);if(reason!="")return reason;
            return State.cats.Count>=HousingCapacity(State,State.day)?"飼育スペースが満員です。":State.funds<=State.dispatches.Single(e=>e.id==id).introduction.cost?"受け入れ後に資金が残る必要があります。":"";
        }
        private void ResolveDispatchIntroduction(string id,string choice)
        {
            var e=DispatchIntroductions(State).SingleOrDefault(e=>e.id==id);if(e==null || choice!="accept" && choice!="decline")throw new ArgumentException("猫紹介と迎える／見送るの選択を確認してください。");
            var v=e.introduction;string status=choice=="accept"?"accepted":"declined";
            if(v.status=="accepted" || v.status=="declined"){if(v.status!=status)throw new InvalidOperationException("回答済みの猫紹介は変更できません。");return;}
            string reason=choice=="accept"?DispatchIntroductionAcceptReason(id):DispatchIntroductionResponseReason(id);if(reason!="")throw new InvalidOperationException(reason);
            if(choice=="accept")JoinCat(new IntakeRules {cat=DispatchCandidate(e),cost=v.cost});
            v.status=status;v.resolved_day=State.day;State.revision++;
        }
        private void ValidateDispatchIntroductions(CafeSnapshot s)
        {
            if(s.dispatches==null)throw new ArgumentException("Invalid dispatch introductions");
            var used=new HashSet<string>(Rules.cats.Select(c=>c.id).Concat(new[]{Rules.intake.cat.id,Rules.regular_introduction.cat.id}));int ordinal=0;bool photoSeen=false;
            foreach(var e in s.dispatches)
            {
                if(e==null)throw new ArgumentException("Invalid dispatch event");
                var r=DispatchIntroductionRule(e.destination);bool required=r!=null && (e.destination!="cat_photo_studio" || !photoSeen);
                if(required!=(e.introduction!=null))throw new ArgumentException("Missing or unexpected dispatch candidate");
                if(e.introduction==null)continue;
                if(e.destination=="cat_photo_studio")photoSeen=true;
                var v=e.introduction;int index=1;while(used.Contains("dispatch-rescue-"+index))index++;
                if(v.cat_id!="dispatch-rescue-"+index || !used.Add(v.cat_id) || v.name!=r.candidate.cat.name+"（派遣紹介"+(++ordinal)+"）" || v.cost!=r.candidate.cost)throw new ArgumentException("Dispatch candidate differs from departure");
                bool answered=v.status=="accepted" || v.status=="declined";
                if(v.status=="scheduled")
                {if(v.presented_day!=0 || v.resolved_day!=0 || e.status=="resolved" && (s.phase!="closed" || e.resolved_day!=s.day))throw new ArgumentException("Unpresented dispatch introduction");}
                else if((v.status!="waiting" && !answered) || e.status!="resolved" || v.presented_day<e.resolved_day || v.presented_day>Math.Min(s.day,e.resolved_day+1) || (answered?v.resolved_day!=v.presented_day:v.resolved_day!=0 || v.presented_day!=s.day || s.phase!="preparation"))throw new ArgumentException("Invalid dispatch introduction dates");
                if((v.status=="accepted")!=s.cats.Any(c=>c.id==v.cat_id))throw new ArgumentException("Dispatch introduction roster differs");
            }
        }
    }
}
