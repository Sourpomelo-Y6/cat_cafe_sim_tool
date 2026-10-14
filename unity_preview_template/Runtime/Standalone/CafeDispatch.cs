using System;
using System.Linq;
namespace CatCafePreview.Standalone
{
    [Serializable] public class DispatchWelcome { public string feature,specialization;public double reward_bonus,satisfaction_bonus; }
    [Serializable] public class DispatchRules { public string id,name,required_trait,required_trait_name,unsupported_reason;public int days;public double reward,max_fatigue;public DispatchWelcome welcome;public CafeSupply item_reward; }
    [Serializable] public class CafeDispatch
    {
        public string id,cat_id,destination,status="travelling",choice;
        public int started_day,remaining,occurred_day,resolved_day,growth_boundary;
        public bool feature_matched,specialization_matched;
        public double growth_multiplier,reward_bonus,reward,return_stress;public CafeDispatchEncounter encounter;public CafeDispatchIntroduction introduction;public CafeDispatchTrouble trouble;public CafePatronMatch patron_match;
    }
    public sealed partial class CafeSimulation
    {
        public bool DispatchWaiting=>State.dispatches.Any(e=>e.status=="waiting");
        private CafeDispatch DispatchTerms(CafeSnapshot s,string id,int day,int boundary=-1,string destination=null)
        {
            var definition=Definitions(s).Single(c=>c.id==id);var g=s.growth.Single(c=>c.cat_id==id);var r=DispatchDestination(destination??Rules.dispatch.id);
            string specialization=boundary<0?g.specialization:s.growth_operations.Take(boundary).LastOrDefault(o=>o.cat_id==id && o.kind=="resolve_growth")?.choice;
            bool specialized=specialization=="dispatch",feature=r.welcome!=null && definition.features.Contains(r.welcome.feature),welcome=r.welcome!=null && specialization==r.welcome.specialization;
            double multiplier=specialized?Rules.growth.dispatch_reward_multiplier:1,bonus=((feature?1:0)+(welcome?1:0))*(r.welcome?.reward_bonus??0);
            return new CafeDispatch {id="dispatch-"+day+"-"+id,cat_id=id,destination=r.id,growth_boundary=boundary<0?s.growth_operations.Count:boundary,started_day=day,remaining=r.days,feature_matched=feature,specialization_matched=welcome,growth_multiplier=multiplier,reward_bonus=bonus,reward=r.reward*definition.dispatch_reward*multiplier+bonus,return_stress=definition.return_stress};
        }
        public DispatchRules[] DispatchDestinations()=>(Rules.dispatch_destinations??new[]{Rules.dispatch}).Concat(State.patron==null?new DispatchRules[0]:PatronDestinations()).ToArray();
        public DispatchRules[] PatronDestinations()=>new[]{Rules.patron.destination}.Concat(Rules.patron.members.Select(r=>new DispatchRules {id=r.id,name=r.name+"への訪問",days=Rules.patron.destination.days,reward=Rules.patron.destination.reward,max_fatigue=Rules.patron.destination.max_fatigue})).ToArray();
        public DispatchRules DispatchDestination(string id)=>(Rules.dispatch_destinations??new[]{Rules.dispatch}).Concat(Rules.patron==null?new DispatchRules[0]:PatronDestinations()).SingleOrDefault(r=>r.id==id);
        public double DispatchReward(string id,string destination=null)=>DispatchTerms(State,id,State.day,destination:destination).reward;
        public string DispatchReason(string id,string destination)
        {
            string reason=ItemsReason();if(reason!="")return reason;
            var r=DispatchDestination(destination);if(r==null)return "対応する派遣先を選んでください。";
            if(!string.IsNullOrEmpty(r.unsupported_reason))return r.unsupported_reason;
            string locked=DispatchUnlockReason(destination);if(locked!="")return locked;
            if(IsPatronDestination(destination) && State.patron==null)return "先に有力者目標を開始してください。";
            if(IsPatronMember(destination) && State.dispatches.Any(e=>e.destination==destination && (e.status!="resolved" || e.started_day==State.day)))return "同じ有力者への訪問は同時1件・同日出発1件までです。";
            var c=State.cats.SingleOrDefault(c=>c.id==id);
            if(c==null)return "参加している猫を選んでください。";
            if(c.activity!="cafe")return "在店していません。";
            if(c.health!="healthy")return "健康な猫が対象です。";
            if(c.cannot_continue || c.stamina<=0)return "接客を担当できる体力・状態が必要です。";
            if(c.fatigue>r.max_fatigue)return "疲労"+r.max_fatigue+"以下が必要です。";
            if(!string.IsNullOrEmpty(r.required_trait) && Definition(id).trait_id!=r.required_trait)return "特性「"+r.required_trait_name+"」が必要です。";
            if(State.cats.Count(c=>c.activity=="cafe" && c.health=="healthy" && !c.cannot_continue && c.stamina>0)<=State.seat_count)return "店内の席数を超える担当可能な猫が必要です。";
            if(State.dispatches.Any(e=>e.id=="dispatch-"+State.day+"-"+id))return "この猫は本日すでに派遣されています。";
            return "";
        }
        private void Dispatch(string id,string destination)
        {
            string reason=DispatchReason(id,destination);if(reason!="")throw new InvalidOperationException(reason);
            var e=DispatchTerms(State,id,State.day,destination:destination);if(!Finite(e.reward))throw new ArgumentException("派遣報酬が大きすぎます。");
            var encounter=DispatchEncounterDefinition(destination,State.day);if(encounter!=null)e.encounter=new CafeDispatchEncounter {id=encounter.id};
            e.patron_match=PatronTerms(State,id,destination,State.day);AttachDispatchTrouble(e);AttachDispatchIntroduction(e);State.dispatches.Add(e);var c=State.cats.Single(c=>c.id==id);c.activity="dispatched";c.working=false;
        }
        private void DispatchCloseDay()
        {foreach(var e in State.dispatches.Where(e=>e.status=="travelling" || e.status=="missing")){if(DispatchTroubleClose(e))continue;e.remaining--;if(e.encounter!=null && e.encounter.status=="scheduled"){e.encounter.status="waiting";e.encounter.occurred_day=State.day;}if(e.remaining==0){e.status="waiting";e.occurred_day=State.day;}}}
        public string DispatchReceiveReason(string id)
        {
            if(State.game_over!=null)return "経営終了後は帰還を確認できません。";
            var e=State.dispatches.SingleOrDefault(e=>e.id==id);return e==null?"帰還イベントを選んでください。":e.status!="waiting" && e.status!="resolved"?"この派遣はまだ帰還していません。":!Finite(State.funds+DispatchReceivedReward(e))?"派遣報酬と所持金の合計が大きすぎます。":"";
        }
        private void ReceiveDispatch(string id,string choice)
        {
            if(choice!="receive")throw new ArgumentException("イベントと選択肢を確認してください。");
            string reason=DispatchReceiveReason(id);if(reason!="")throw new InvalidOperationException(reason);var e=State.dispatches.Single(e=>e.id==id);if(e.status=="resolved")return;
            e.status="resolved";e.choice=choice;e.resolved_day=State.day;var c=State.cats.Single(c=>c.id==e.cat_id);c.activity="cafe";if(DispatchInterrupted(e)){c.stress=Rules.dispatch_trouble.return_stress;c.stamina=MaxStamina(c.id);c.cannot_continue=c.health!="healthy";}else c.stress=Math.Min(100,c.stress+e.return_stress);
            State.funds+=DispatchReceivedReward(e);State.dispatch_income+=DispatchReceivedReward(e);
            ReceivePatron(e);
            if(!DispatchInterrupted(e))RecordGrowth(new CafeGrowthOperation {kind="dispatch",cat_id=c.id,customer_id=e.id,day=State.day,tick=State.tick});PresentDispatchIntroductions();State.revision++;
        }
        private void ValidateDispatch(CafeSnapshot s)
        {
            if(s.dispatches==null || s.dispatches.Select(e=>e?.id).Distinct().Count()!=s.dispatches.Count)throw new ArgumentException("Invalid dispatch history");
            var busy=new System.Collections.Generic.HashSet<string>();
            foreach(var e in s.dispatches)
            {
                var c=e==null?null:s.cats.SingleOrDefault(c=>c.id==e.cat_id);
                if(c==null || e.started_day<JoinedDay(s,c.id) || e.started_day>s.day || DispatchDestination(e.destination)==null || !string.IsNullOrEmpty(DispatchDestination(e.destination).unsupported_reason) || e.growth_boundary<0 || e.growth_boundary>s.growth_operations.Count || s.growth_operations.Take(e.growth_boundary).Any(o=>o.day>e.started_day))throw new ArgumentException("Invalid dispatch destination or cat");
                var terms=DispatchTerms(s,c.id,e.started_day,e.growth_boundary,e.destination);var r=DispatchDestination(e.destination);ValidateDispatchEncounter(s,e);int elapsed=s.day-e.started_day+(s.phase=="closed"?1:0);
                bool interrupted=ValidateDispatchTrouble(s,e);
                if(e.id!=terms.id || !interrupted && e.remaining!=Math.Max(0,r.days-elapsed) || e.reward!=terms.reward || e.reward_bonus!=terms.reward_bonus || e.return_stress!=terms.return_stress || e.growth_multiplier!=terms.growth_multiplier || e.feature_matched!=terms.feature_matched || e.specialization_matched!=terms.specialization_matched)throw new ArgumentException("Dispatch terms differ from departure");
                if(!interrupted && (e.status=="travelling"?(e.remaining<=0 || e.occurred_day!=0 || e.resolved_day!=0 || e.choice!=null):e.status=="waiting"?(e.remaining!=0 || e.occurred_day!=e.started_day+r.days-1 || e.resolved_day!=0 || e.choice!=null):e.status=="resolved"?(e.remaining!=0 || e.occurred_day!=e.started_day+r.days-1 || e.resolved_day<e.occurred_day || e.resolved_day>s.day || e.choice!="receive"):true))throw new ArgumentException("Invalid dispatch phase");
                if(e.status!="resolved" && (!busy.Add(c.id) || c.activity!=(interrupted?"missing":"dispatched") || c.working))throw new ArgumentException("Overlapping dispatches");
                var ops=s.growth_operations.Where(o=>o.kind=="dispatch" && o.customer_id==e.id).ToArray();
                if(ops.Length!=(e.status=="resolved" && !interrupted?1:0) || ops.Any(o=>o.cat_id!=e.cat_id || o.day!=e.resolved_day))throw new ArgumentException("Dispatch experience differs from return");
            }
            if(!busy.SetEquals(s.cats.Where(c=>c.activity=="dispatched" || DispatchMissing(s,c.id)).Select(c=>c.id)) || s.growth_operations.Any(o=>o.kind=="dispatch" && !s.dispatches.Any(e=>e.id==o.customer_id && e.status=="resolved")))throw new ArgumentException("Dispatch activity differs from events");
            foreach(var g in s.dispatches.GroupBy(e=>e.cat_id)){CafeDispatch previous=null;foreach(var e in g.OrderBy(e=>e.started_day)){if(previous!=null && (previous.status!="resolved" || e.started_day<previous.resolved_day))throw new ArgumentException("Overlapping dispatch dates");previous=e;}}
            ValidateDispatchTroubleExpenses(s);
            ValidateDispatchUnlocks(s);
            for(int day=1;day<=s.day;day++)if(s.dispatches.Where(e=>e.status=="resolved" && e.resolved_day==day).Sum(e=>DispatchReceivedReward(e))!=(day==s.day?s.dispatch_income:s.history[day-1].dispatch_income))throw new ArgumentException("Dispatch income differs from returns");
        }
    }
}
