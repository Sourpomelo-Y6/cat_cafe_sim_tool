using System;
using System.Linq;
using System.Collections.Generic;
namespace CatCafePreview.Standalone
{
    [Serializable] public class PatronCondition {public string feature,specialization;}
    [Serializable] public class PatronMemberRule {public string id,name;public double target,gain,unmatched_gain;public PatronCondition[] conditions;}
    [Serializable] public class PatronRules {public string name;public double target,gain;public DispatchRules destination;public PatronMemberRule[] members;}
    [Serializable] public class CafePatronScore {public string id;public double value;}
    [Serializable] public class CafePatron {public int started_day=1,resolved_day;public string id,status="active";public bool continued;public double satisfaction;public List<CafePatronScore> members=new List<CafePatronScore>();}
    [Serializable] public class CafePatronMatch {public string member_id;public int condition_index;public PatronCondition condition;public bool matched;public double gain;}
    public sealed partial class CafeSimulation
    {
        public bool IsPatronMember(string id)=>id=="patron_moody_visit" || id=="patron_strict_visit";
        public bool IsPatronDestination(string id)=>id=="patron_visit" || IsPatronMember(id);
        public bool PatronWaiting=>State.game_over==null && State.patron?.status=="cleared" && !State.patron.continued;
        public CafePatronMatch PatronTerms(CafeSnapshot s,string cat,string destination,int day=0,int boundary=-1)
        {
            var r=Rules.patron?.members.SingleOrDefault(row=>row.id==destination);if(r==null)return null;
            int count=s.dispatches.Count(e=>e.destination==destination && (day==0?e.status=="resolved":e.started_day<day));int index=count%r.conditions.Length;var condition=r.conditions[index];
            string specialization=boundary<0?s.growth.Single(g=>g.cat_id==cat).specialization:s.growth_operations.Take(boundary).LastOrDefault(o=>o.cat_id==cat && o.kind=="resolve_growth")?.choice;
            bool matched=(condition.feature==null || Definitions(s).Single(c=>c.id==cat).features.Contains(condition.feature)) && (condition.specialization==null || specialization==condition.specialization);
            return new CafePatronMatch {member_id=destination,condition_index=index,condition=new PatronCondition {feature=condition.feature,specialization=condition.specialization},matched=matched,gain=matched?r.gain:r.unmatched_gain};
        }
        public double PatronGain(string cat,string destination)
        {var match=PatronTerms(State,cat,destination);if(match!=null)return match.gain;var terms=DispatchTerms(State,cat,State.day,destination:destination);return Rules.patron.gain+((terms.feature_matched?1:0)+(terms.specialization_matched?1:0))*(Rules.patron.destination.welcome?.satisfaction_bonus??0);}
        private double PatronEventGain(CafeDispatch e)=>e.patron_match?.gain??(Rules.patron.gain+((e.feature_matched?1:0)+(e.specialization_matched?1:0))*(Rules.patron.destination.welcome?.satisfaction_bonus??0));
        private void ReceivePatron(CafeDispatch e)
        {
            var p=State.patron;if(p==null || !IsPatronDestination(e.destination))return;
            if(IsPatronMember(e.destination)){var score=p.members.Single(r=>r.id==e.destination);score.value=Math.Min(Rules.patron.members.Single(r=>r.id==e.destination).target,score.value+PatronEventGain(e));}
            else p.satisfaction=Math.Min(Rules.patron.target,p.satisfaction+PatronEventGain(e));
            if(p.status=="active" && p.satisfaction>=Rules.patron.target && p.members.All(score=>score.value>=Rules.patron.members.Single(r=>r.id==score.id).target))
            {
                p.status="cleared";p.resolved_day=State.day;double target=Rules.patron.target+Rules.patron.members.Sum(r=>r.target);
                State.clear_results.Add(new CafeClearRecord {mode="patron",day=State.day,target=target,value=target,funds=State.funds,popularity=State.popularity,cats=State.cats.Count,revenue=State.history.Sum(h=>h.revenue)+State.visits.Sum(v=>v.bill)});
            }
        }
        public string PatronContinueReason()
        {
            if(!PatronWaiting)return "確認待ちの有力者目標の結果はありません。";
            if(DispatchWaiting || DispatchChoiceWaiting || DispatchTroubleWaiting || DispatchIntroductionWaiting || ReservationWaiting || CustomerTrustWaiting || State.cats.Any(c=>c.activity=="missing" && c.missing_remaining==0 && !DispatchMissing(State,c.id)))return "先に帰還・イベントを確認してください。";
            return "";
        }
        private void ContinuePatron(){string reason=PatronContinueReason();if(reason!="")throw new InvalidOperationException(reason);State.patron.continued=true;State.revision++;}
        private void ValidatePatron(CafeSnapshot s)
        {
            var p=s.patron;if((s.objective=="patron")!=(p!=null))throw new ArgumentException("Invalid patron objective");
            foreach(var e in s.dispatches)
            {
                if(!IsPatronDestination(e.destination)){if(e.patron_match!=null)throw new ArgumentException("Unexpected patron match");continue;}
                if(p==null || e.started_day<p.started_day)throw new ArgumentException("Patron visit before objective");
                var expected=PatronTerms(s,e.cat_id,e.destination,e.started_day,e.growth_boundary);var match=e.patron_match;
                if(expected==null){if(match!=null)throw new ArgumentException("Unexpected base patron match");continue;}
                if(match==null || match.member_id!=expected.member_id || match.condition_index!=expected.condition_index || match.condition==null || match.condition.feature!=expected.condition.feature || match.condition.specialization!=expected.condition.specialization || match.matched!=expected.matched || match.gain!=expected.gain)throw new ArgumentException("Patron conditions differ from departure");
                if(s.dispatches.Any(other=>other.id!=e.id && other.destination==e.destination && (other.started_day==e.started_day || other.started_day<e.started_day && (other.status!="resolved" || other.resolved_day>e.started_day))))throw new ArgumentException("Overlapping patron visits");
            }
            if(p==null)return;
            if(p.id!="patron" || p.started_day!=1 || p.members==null || p.members.Count!=Rules.patron.members.Length || p.members.Select(r=>r?.id).Distinct().Count()!=p.members.Count || p.members.Any(r=>r==null || !Rules.patron.members.Any(rule=>rule.id==r.id)))throw new ArgumentException("Invalid patron members");
            double satisfaction=0;var scores=Rules.patron.members.ToDictionary(r=>r.id,r=>0.0);int resolved=0;
            foreach(var e in s.dispatches.Where(e=>e.status=="resolved" && IsPatronDestination(e.destination)).OrderBy(e=>e.resolved_day))
            {
                if(IsPatronMember(e.destination))scores[e.destination]=Math.Min(Rules.patron.members.Single(r=>r.id==e.destination).target,scores[e.destination]+PatronEventGain(e));else satisfaction=Math.Min(Rules.patron.target,satisfaction+PatronEventGain(e));
                if(resolved==0 && satisfaction>=Rules.patron.target && Rules.patron.members.All(r=>scores[r.id]>=r.target))resolved=e.resolved_day;
            }
            if(p.satisfaction!=satisfaction || p.members.Any(r=>r.value!=scores[r.id]) || p.status!=(resolved==0?"active":"cleared") || p.resolved_day!=resolved || resolved==0 && p.continued || resolved!=0 && !p.continued && (s.day!=resolved || s.phase=="open"))throw new ArgumentException("Patron progress differs from returns");
        }
    }
}
