using System;
using System.Linq;
namespace CatCafePreview.Standalone
{
    public sealed partial class CafeSimulation
    {
        private int VisitingJoinedCount(CafeSnapshot s,int day) => s.visiting_cat?.status=="accepted" && s.visiting_cat.accepted_day<=day ? 1 : 0;
        private int VisitingUnlockDay(CafeSnapshot s) => s.goal.Count>0 && s.goal[0].status=="cleared" ? s.goal[0].resolved_day : 0;
        private void PresentVisitingCat()
        {
            var r=State.visiting_cat;int day=VisitingUnlockDay(State);
            if(r!=null && r.status=="untriggered" && day>0 && day<State.day && State.phase=="preparation" && State.game_over==null){r.status="visiting";r.presented_day=State.day;}
        }
        public int VisitingProgress => (State.visiting_cat?.actions ?? new System.Collections.Generic.List<CafeVisitingAction>()).Count(a=>a.choice=="interact");
        public string VisitingResponseReason()
        {
            string reason=ItemsReason();if(reason!="")return reason;
            return State.visiting_cat?.status=="visiting" || State.visiting_cat?.status=="ready" ? "" : "交流できる店先の猫はいません。";
        }
        public string VisitingDailyReason()
        {
            string reason=VisitingResponseReason();if(reason!="")return reason;
            return State.visiting_cat.actions.Any(a=>a.day==State.day)?"今日は交流または見送り済みです。次の準備日に会えます。":"";
        }
        public string VisitingAdmissionReason()
        {
            string reason=VisitingResponseReason();if(reason!="")return reason;
            if(State.visiting_cat.status!="ready")return "迎えるには店先で"+Rules.visiting_cat.interactions_required+"回の交流が必要です。";
            if(State.cats.Count>=HousingCapacity(State,State.day))return "飼育スペースが満員です。";
            return State.funds<=Rules.visiting_cat.cost?"受け入れ後に資金が残る必要があります。":"";
        }
        private void ResolveVisitingCat(string choice)
        {
            if(choice!="interact" && choice!="skip" && choice!="accept")throw new ArgumentException("店先の猫と交流する／今日は見送る／迎えるを選んでください。");
            var r=State.visiting_cat;if(r==null)throw new InvalidOperationException("交流できる店先の猫はいません。");if(choice=="accept" && r.status=="accepted")return;
            string reason=choice=="accept"?VisitingAdmissionReason():VisitingDailyReason();if(reason!="")throw new InvalidOperationException(reason);
            if(choice=="interact" && r.status=="ready")throw new InvalidOperationException("十分に慣れています。迎えるか、今日は見送ることができます。");
            if(choice=="accept"){JoinCat(Rules.visiting_cat);r.status="accepted";r.accepted_day=State.day;}
            else {r.actions.Add(new CafeVisitingAction {day=State.day,choice=choice});if(VisitingProgress>=Rules.visiting_cat.interactions_required)r.status="ready";}
            State.revision++;
        }
        private void ValidateVisitingCat(CafeSnapshot s)
        {
            var r=s.visiting_cat;var rule=Rules.visiting_cat;
            if(rule==null){if(r!=null)throw new ArgumentException("Unexpected visiting cat state");return;}
            if(r==null || rule?.cat==null || rule.interactions_required<1 || !Range(rule.cost,0,double.MaxValue) || r.actions==null
                || Rules.cats.Any(c=>c.id==rule.cat.id) || rule.cat.id==Rules.intake.cat.id || rule.cat.id==Rules.regular_introduction.cat.id
                || Rules.dispatch_introductions.Any(c=>c.candidate.cat.id==rule.cat.id))throw new ArgumentException("Invalid visiting cat configuration");
            int unlocked=VisitingUnlockDay(s);
            if(r.status=="untriggered")
            {
                if(r.presented_day!=0 || r.accepted_day!=0 || r.actions.Count!=0 || unlocked>0 && unlocked<s.day && s.game_over==null)throw new ArgumentException("Unpresented visiting cat");return;
            }
            if(!new[]{"visiting","ready","accepted"}.Contains(r.status) || unlocked==0 || r.presented_day!=unlocked+1 || r.presented_day>s.day)throw new ArgumentException("Invalid visiting cat appearance");
            int previous=r.presented_day-1,count=0;
            foreach(var a in r.actions)
            {
                if(a==null || a.day<=previous || a.day>s.day || (a.choice!="interact" && a.choice!="skip") || a.choice=="interact" && count>=rule.interactions_required || r.status=="accepted" && a.day>r.accepted_day)throw new ArgumentException("Invalid daily visiting action");
                previous=a.day;if(a.choice=="interact")count++;
            }
            if((r.status=="visiting")!=(count<rule.interactions_required) || (r.status=="accepted" ? r.accepted_day<Math.Max(r.presented_day,previous) || r.accepted_day>s.day : r.accepted_day!=0))throw new ArgumentException("Invalid visiting progress or admission");
        }
    }
}
