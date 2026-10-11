using System;
using System.Linq;
namespace CatCafePreview.Standalone
{
    [Serializable] public class DispatchTroubleRules {public DispatchRules destination;public double base_probability,stress_probability,search_cost,return_stress;public int missing_days;}
    [Serializable] public class CafeDispatchTrouble {public string destination,status="scheduled",choice;public double stress,probability,draw,cost;public int missing_day,choice_day;}
    public sealed partial class CafeSimulation
    {
        public bool DispatchTroubleWaiting=>State.dispatches.Any(e=>e.trouble?.status=="waiting");
        public bool DispatchMissing(CafeSnapshot s,string id)=>s.dispatches.Any(e=>e.cat_id==id && DispatchInterrupted(e) && (e.status=="missing" || e.status=="waiting"));
        public static bool DispatchInterrupted(CafeDispatch e)=>e.trouble!=null && e.trouble.missing_day>0;
        public static double DispatchTroubleDraw(int seed,string id)=>PythonRandom.Hash64("dispatch-trouble:"+seed+":"+id)/18446744073709551616.0;
        public double DispatchTroubleProbability(string cat)=>Rules.dispatch_trouble.base_probability+Rules.dispatch_trouble.stress_probability*State.cats.Single(c=>c.id==cat).stress/100;
        private void AttachDispatchTrouble(CafeDispatch e)
        {
            if(e.destination!="mountain_lodge_visit")return;
            if(Rules.dispatch_trouble==null)throw new InvalidOperationException("山あいの宿の派遣設定がありません。");
            e.trouble=new CafeDispatchTrouble {destination=e.destination,stress=State.cats.Single(c=>c.id==e.cat_id).stress,probability=DispatchTroubleProbability(e.cat_id),draw=DispatchTroubleDraw(State.seed,e.id)};
        }
        private bool DispatchTroubleClose(CafeDispatch e)
        {
            var v=e.trouble;if(v==null)return false;
            if(e.status=="missing"){if(State.day>v.missing_day && --e.remaining==0){e.status="waiting";e.occurred_day=State.day;}return true;}
            if(v.status!="scheduled")return false;
            if(v.draw>=v.probability){v.status="not_triggered";return false;}
            v.status="waiting";v.missing_day=State.day;e.status="missing";e.remaining=Rules.dispatch_trouble.missing_days;State.cats.Single(c=>c.id==e.cat_id).activity="missing";return true;
        }
        public string DispatchTroubleReason(string id,string choice)
        {
            if(State.game_over!=null)return "経営終了後は回答できません。";
            var e=State.dispatches.SingleOrDefault(e=>e.id==id);var v=e?.trouble;
            if(v==null || choice!="search" && choice!="wait")return "派遣トラブルと対応を確認してください。";
            if(v.status=="resolved")return v.choice==choice?"":"回答済みの派遣トラブルは変更できません。";
            if(v.status!="waiting" || e.status!="missing" || State.phase=="open")return "回答待ちの派遣トラブルではありません。";
            return State.funds<=(choice=="search"?Rules.dispatch_trouble.search_cost:0)?"捜索後に資金が残る必要があります。費用なしの帰還待ちを選べます。":"";
        }
        private void ResolveDispatchTrouble(string id,string choice)
        {
            string reason=DispatchTroubleReason(id,choice);if(reason!="")throw new InvalidOperationException(reason);
            var e=State.dispatches.Single(e=>e.id==id);var v=e.trouble;if(v.status=="resolved")return;
            v.status="resolved";v.choice=choice;v.choice_day=State.day;v.cost=choice=="search"?Rules.dispatch_trouble.search_cost:0;
            State.funds-=v.cost;State.expenses+=v.cost;State.dispatch_trouble_expenses+=v.cost;
            if(choice=="search"){e.status="waiting";e.remaining=0;e.occurred_day=State.day;}State.revision++;
        }
        private bool ValidateDispatchTrouble(CafeSnapshot s,CafeDispatch e)
        {
            var v=e.trouble;var r=Rules.dispatch_trouble;
            if(v==null){if(e.destination=="mountain_lodge_visit")throw new ArgumentException("Missing dispatch trouble draw");return false;}
            if(r==null || e.destination!="mountain_lodge_visit" || v.destination!=e.destination || e.encounter!=null || e.introduction!=null || !Range(v.stress,0,100) || v.probability!=r.base_probability+r.stress_probability*v.stress/100 || v.draw!=DispatchTroubleDraw(s.seed,e.id))throw new ArgumentException("Invalid dispatch trouble terms");
            int elapsed=s.day-e.started_day+(s.phase=="closed"?1:0);
            if(v.status=="scheduled" || v.status=="not_triggered")
            {if(v.missing_day!=0 || v.choice!=null || v.choice_day!=0 || v.cost!=0 || (v.status=="scheduled"?elapsed!=0:elapsed<1 || v.draw<v.probability))throw new ArgumentException("Invalid untriggered dispatch trouble");return false;}
            if(v.status!="waiting" && v.status!="resolved" || v.missing_day!=e.started_day || elapsed<1 || v.draw>=v.probability)throw new ArgumentException("Invalid dispatch missing day");
            if(v.status=="waiting")
            {if(elapsed!=1 || e.status!="missing" || v.choice!=null || v.choice_day!=0 || v.cost!=0)throw new ArgumentException("Dispatch advanced before trouble response");}
            else if(v.choice!="search" && v.choice!="wait" || v.choice_day<e.started_day || v.choice_day>Math.Min(s.day,e.started_day+1) || v.cost!=(v.choice=="search"?r.search_cost:0))throw new ArgumentException("Invalid search payment");
            int due=v.choice=="search"?v.choice_day:v.missing_day+r.missing_days,remaining=v.choice=="search"?0:Math.Max(0,r.missing_days-(elapsed-1));
            if(e.remaining!=remaining)throw new ArgumentException("Invalid dispatch missing countdown");
            if(e.status=="missing")
            {if(remaining==0 || e.occurred_day!=0 || e.resolved_day!=0 || e.choice!=null)throw new ArgumentException("Invalid missing dispatch");}
            else if(e.status!="waiting" && e.status!="resolved" || remaining!=0 || e.occurred_day!=due || (e.status=="waiting"?e.resolved_day!=0 || e.choice!=null:e.resolved_day<due || e.resolved_day>s.day || e.choice!="receive"))throw new ArgumentException("Invalid interrupted return");
            return true;
        }
        private void ValidateDispatchTroubleExpenses(CafeSnapshot s)
        {
            for(int day=1;day<=s.day;day++)
            {
                double cost=s.dispatches.Where(e=>e.trouble?.choice_day==day).Sum(e=>e.trouble.cost),saved=day==s.day?s.dispatch_trouble_expenses:s.history[day-1].dispatch_trouble_expenses,total=day==s.day?s.expenses:s.history[day-1].expenses;
                if(saved!=cost || total<cost)throw new ArgumentException("Search cost differs from finance history");
            }
        }
    }
}
