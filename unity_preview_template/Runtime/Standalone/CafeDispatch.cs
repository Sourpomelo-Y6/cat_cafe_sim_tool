using System;
using System.Linq;
namespace CatCafePreview.Standalone
{
    [Serializable] public class DispatchWelcome { public string feature,specialization;public double reward_bonus,satisfaction_bonus; }
    [Serializable] public class DispatchRules { public string id,name;public int days;public double reward,max_fatigue;public DispatchWelcome welcome;public CafeSupply item_reward; }
    [Serializable] public class CafeDispatch
    {
        public string id,cat_id,destination,status="travelling",choice;
        public int started_day,remaining,occurred_day,resolved_day,growth_boundary;
        public bool feature_matched,specialization_matched;
        public double growth_multiplier,reward_bonus,reward,return_stress;
    }
    public sealed partial class CafeSimulation
    {
        public bool DispatchWaiting=>State.dispatches.Any(e=>e.status=="waiting");
        private CafeDispatch DispatchTerms(CafeSnapshot s,string id,int day,int boundary=-1)
        {
            var definition=Definitions(s).Single(c=>c.id==id);var g=s.growth.Single(c=>c.cat_id==id);var r=Rules.dispatch;
            string specialization=boundary<0?g.specialization:s.growth_operations.Take(boundary).LastOrDefault(o=>o.cat_id==id && o.kind=="resolve_growth")?.choice;
            bool specialized=specialization=="dispatch",feature=definition.features.Contains(r.welcome.feature),welcome=specialization==r.welcome.specialization;
            double multiplier=specialized?Rules.growth.dispatch_reward_multiplier:1,bonus=((feature?1:0)+(welcome?1:0))*r.welcome.reward_bonus;
            return new CafeDispatch {id="dispatch-"+day+"-"+id,cat_id=id,destination=r.id,growth_boundary=boundary<0?s.growth_operations.Count:boundary,started_day=day,remaining=r.days,feature_matched=feature,specialization_matched=welcome,growth_multiplier=multiplier,reward_bonus=bonus,reward=r.reward*definition.dispatch_reward*multiplier+bonus,return_stress=definition.return_stress};
        }
        public double DispatchReward(string id)=>DispatchTerms(State,id,State.day).reward;
        public string DispatchReason(string id,string destination)
        {
            string reason=ItemsReason();if(reason!="")return reason;
            if(destination!=Rules.dispatch.id)return "対応する派遣先を選んでください。";
            var c=State.cats.SingleOrDefault(c=>c.id==id);
            if(c==null)return "参加している猫を選んでください。";
            if(c.activity!="cafe")return "在店していません。";
            if(c.health!="healthy")return "健康な猫が対象です。";
            if(c.cannot_continue || c.stamina<=0)return "接客を担当できる体力・状態が必要です。";
            if(c.fatigue>Rules.dispatch.max_fatigue)return "疲労"+Rules.dispatch.max_fatigue+"以下が必要です。";
            if(State.cats.Count(c=>c.activity=="cafe" && c.health=="healthy" && !c.cannot_continue && c.stamina>0)<=State.seat_count)return "店内の席数を超える担当可能な猫が必要です。";
            if(State.dispatches.Any(e=>e.id=="dispatch-"+State.day+"-"+id))return "この猫は本日すでに派遣されています。";
            return "";
        }
        private void Dispatch(string id,string destination)
        {
            string reason=DispatchReason(id,destination);if(reason!="")throw new InvalidOperationException(reason);
            var e=DispatchTerms(State,id,State.day);if(!Finite(e.reward))throw new ArgumentException("派遣報酬が大きすぎます。");
            State.dispatches.Add(e);var c=State.cats.Single(c=>c.id==id);c.activity="dispatched";c.working=false;
        }
        private void DispatchCloseDay()
        {foreach(var e in State.dispatches.Where(e=>e.status=="travelling")){e.remaining--;if(e.remaining==0){e.status="waiting";e.occurred_day=State.day;}}}
        public string DispatchReceiveReason(string id)
        {
            if(State.game_over!=null)return "経営終了後は帰還を確認できません。";
            var e=State.dispatches.SingleOrDefault(e=>e.id==id);return e==null?"帰還イベントを選んでください。":e.status=="travelling"?"この派遣はまだ帰還していません。":!Finite(State.funds+e.reward)?"派遣報酬と所持金の合計が大きすぎます。":"";
        }
        private void ReceiveDispatch(string id,string choice)
        {
            if(choice!="receive")throw new ArgumentException("イベントと選択肢を確認してください。");
            string reason=DispatchReceiveReason(id);if(reason!="")throw new InvalidOperationException(reason);var e=State.dispatches.Single(e=>e.id==id);if(e.status=="resolved")return;
            e.status="resolved";e.choice=choice;e.resolved_day=State.day;var c=State.cats.Single(c=>c.id==e.cat_id);c.activity="cafe";c.stress=Math.Min(100,c.stress+e.return_stress);
            State.funds+=e.reward;State.dispatch_income+=e.reward;
            RecordGrowth(new CafeGrowthOperation {kind="dispatch",cat_id=c.id,customer_id=e.id,day=State.day,tick=State.tick});State.revision++;
        }
        private void ValidateDispatch(CafeSnapshot s)
        {
            if(s.dispatches==null || s.dispatches.Select(e=>e?.id).Distinct().Count()!=s.dispatches.Count)throw new ArgumentException("Invalid dispatch history");
            var busy=new System.Collections.Generic.HashSet<string>();
            foreach(var e in s.dispatches)
            {
                var c=e==null?null:s.cats.SingleOrDefault(c=>c.id==e.cat_id);
                if(c==null || e.started_day<1 || e.started_day>s.day || e.destination!=Rules.dispatch.id || e.growth_boundary<0 || e.growth_boundary>s.growth_operations.Count || s.growth_operations.Take(e.growth_boundary).Any(o=>o.day>e.started_day))throw new ArgumentException("Invalid dispatch destination or cat");
                var terms=DispatchTerms(s,c.id,e.started_day,e.growth_boundary);int elapsed=s.day-e.started_day+(s.phase=="closed"?1:0);
                if(e.id!=terms.id || e.remaining!=Math.Max(0,Rules.dispatch.days-elapsed) || e.reward!=terms.reward || e.reward_bonus!=terms.reward_bonus || e.return_stress!=terms.return_stress || e.growth_multiplier!=terms.growth_multiplier || e.feature_matched!=terms.feature_matched || e.specialization_matched!=terms.specialization_matched)throw new ArgumentException("Dispatch terms differ from departure");
                if(e.status=="travelling"?(e.remaining<=0 || e.occurred_day!=0 || e.resolved_day!=0 || e.choice!=null):e.status=="waiting"?(e.remaining!=0 || e.occurred_day!=e.started_day+Rules.dispatch.days-1 || e.resolved_day!=0 || e.choice!=null):e.status=="resolved"?(e.remaining!=0 || e.occurred_day!=e.started_day+Rules.dispatch.days-1 || e.resolved_day<e.occurred_day || e.resolved_day>s.day || e.choice!="receive"):true)throw new ArgumentException("Invalid dispatch phase");
                if(e.status!="resolved" && (!busy.Add(c.id) || c.activity!="dispatched" || c.working))throw new ArgumentException("Overlapping dispatches");
                var ops=s.growth_operations.Where(o=>o.kind=="dispatch" && o.customer_id==e.id).ToArray();
                if(ops.Length!=(e.status=="resolved"?1:0) || ops.Any(o=>o.cat_id!=e.cat_id || o.day!=e.resolved_day))throw new ArgumentException("Dispatch experience differs from return");
            }
            if(!busy.SetEquals(s.cats.Where(c=>c.activity=="dispatched").Select(c=>c.id)) || s.growth_operations.Any(o=>o.kind=="dispatch" && !s.dispatches.Any(e=>e.id==o.customer_id && e.status=="resolved")))throw new ArgumentException("Dispatch activity differs from events");
            foreach(var g in s.dispatches.GroupBy(e=>e.cat_id)){CafeDispatch previous=null;foreach(var e in g.OrderBy(e=>e.started_day)){if(previous!=null && (previous.status!="resolved" || e.started_day<previous.resolved_day))throw new ArgumentException("Overlapping dispatch dates");previous=e;}}
            for(int day=1;day<=s.day;day++)if(s.dispatches.Where(e=>e.status=="resolved" && e.resolved_day==day).Sum(e=>e.reward)!=(day==s.day?s.dispatch_income:s.history[day-1].dispatch_income))throw new ArgumentException("Dispatch income differs from returns");
        }
    }
}
