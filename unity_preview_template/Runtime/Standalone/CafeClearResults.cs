using System;
using System.Linq;
namespace CatCafePreview.Standalone
{
    [Serializable] public class CafeClearRecord
    {
        public string mode="popularity";
        public int day,cats;
        public double target,value,funds,popularity,revenue;
    }
    public sealed partial class CafeSimulation
    {
        private void CaptureClearResult()
        {
            if(State.objective!="popularity" || State.goal.Count!=Rules.goal_stages.Length || State.goal.Last().status!="cleared" || State.clear_results.Count!=0)return;
            State.clear_results.Add(new CafeClearRecord {day=State.day,target=Rules.goal_stages.Last().target,value=State.popularity,funds=State.funds,popularity=State.popularity,cats=State.cats.Count,revenue=State.history.Sum(h=>h.revenue)+State.visits.Sum(v=>v.bill)});
        }
        private void ValidateClearResults(CafeSnapshot s)
        {
            string reason=s.funds<=0?"funds":s.popularity<=0?"popularity":null;
            if(s.game_over!=reason)throw new ArgumentException("Invalid ending reason");
            bool completed=s.objective=="popularity" && s.goal.Count==Rules.goal_stages.Length && s.goal.Last().status=="cleared";
            if(s.clear_results==null || s.clear_results.Count!=(completed?1:0))throw new ArgumentException("Clear records differ from completed goals");
            foreach(var row in s.clear_results)
            {
                if(row==null || row.mode!="popularity" || row.day!=s.goal.Last().resolved_day || row.cats<0 || row.cats>s.cats.Count || row.target!=Rules.goal_stages.Last().target || !Range(row.value,row.target,Rules.popularity.cap) || row.value!=row.popularity || !Finite(row.funds) || row.funds<=0 || !Range(row.revenue,0,double.MaxValue))throw new ArgumentException("Invalid fixed clear record");
                double closingPopularity=row.day==s.day?s.goal_popularity:s.history[row.day-1].goal_popularity;
                double revenue=s.history.Where(h=>h.day<=row.day).Sum(h=>h.revenue)+(row.day==s.day?s.visits.Sum(v=>v.bill):0);
                int cats=Rules.cats.Length+(s.intake_request.status=="accepted" && s.intake_request.resolved_day<=row.day?1:0)+(s.regular_introduction.status=="accepted" && s.regular_introduction.resolved_day<=row.day?1:0);
                if(row.value!=closingPopularity || row.revenue!=revenue || row.cats!=cats)throw new ArgumentException("Clear record differs from closing history");
            }
        }
    }
}
