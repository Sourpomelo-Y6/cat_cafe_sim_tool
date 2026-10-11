using System;
using System.Linq;
namespace CatCafePreview.Standalone
{
    [Serializable] public class DispatchUnlockRule {public string id;public int returns,stage;public double popularity;}
    [Serializable] public class CafeDispatchUnlock {public string id;public int day,returns;public double popularity;}
    [Serializable] public class DispatchEncounterOption {public string id,label;public double reward,fatigue,stress;}
    [Serializable] public class DispatchEncounterRule {public string id,destination,title;public DispatchEncounterOption[] choices;}
    [Serializable] public class CafeDispatchEncounter
    {
        public string id,status="scheduled",choice;public int occurred_day,resolved_day;
        public double fatigue_before,fatigue_after,stress_before,stress_after;
    }
    public sealed partial class CafeSimulation
    {
        public bool DispatchChoiceWaiting=>State.dispatches.Any(e=>e.encounter?.status=="waiting");
        public DispatchEncounterRule DispatchEncounterDefinition(string destination,int day)
        {var matches=(Rules.dispatch_encounters??new DispatchEncounterRule[0]).Where(r=>r.destination==destination).ToArray();return matches.Length==0?null:matches.Length==1?matches[0]:matches[day%matches.Length];}
        public double DispatchReceivedReward(CafeDispatch e)
        {var rule=DispatchEncounterDefinition(e.destination,e.started_day);return DispatchInterrupted(e)?0:Math.Max(0,e.reward+(e.encounter?.status=="resolved"?rule.choices.Single(c=>c.id==e.encounter.choice).reward:0));}
        private bool DispatchEligible(CafeSnapshot s,DispatchUnlockRule r)=>r.stage>0?s.goal.Count>=r.stage && s.goal[r.stage-1].status=="cleared":s.popularity>=r.popularity && s.dispatches.Count(e=>e.status=="resolved" && !DispatchInterrupted(e))>=r.returns;
        private void UpdateDispatchUnlocks()
        {
            if(State.game_over!=null)return;
            foreach(var r in Rules.dispatch_unlocks??new DispatchUnlockRule[0])if(!State.dispatch_unlocks.Any(u=>u.id==r.id) && DispatchEligible(State,r))State.dispatch_unlocks.Add(new CafeDispatchUnlock {id=r.id,day=State.day,popularity=State.popularity,returns=State.dispatches.Count(e=>e.status=="resolved" && !DispatchInterrupted(e))});
        }
        public string DispatchUnlockReason(string destination)
        {
            var r=(Rules.dispatch_unlocks??new DispatchUnlockRule[0]).SingleOrDefault(r=>r.id==destination);
            if(r==null || State.dispatch_unlocks.Any(u=>u.id==destination))return "";
            return r.stage>0?"未解放：人気第"+r.stage+"段階の達成が必要です。":"未解放：人気 "+State.popularity+" / "+r.popularity+"・派遣報酬受取 "+State.dispatches.Count(e=>e.status=="resolved" && !DispatchInterrupted(e))+" / "+r.returns+"回";
        }
        private void ValidateDispatchUnlocks(CafeSnapshot s)
        {
            var rules=Rules.dispatch_unlocks??new DispatchUnlockRule[0];
            if(s.dispatch_unlocks==null || s.dispatch_unlocks.Select(u=>u?.id).Distinct().Count()!=s.dispatch_unlocks.Count)throw new ArgumentException("Invalid dispatch unlocks");
            foreach(var u in s.dispatch_unlocks)
            {
                var r=u==null?null:rules.SingleOrDefault(r=>r.id==u.id);
                if(r==null || u.day<1 || u.day>s.day || !Range(u.popularity,r.popularity,Rules.popularity.cap) || u.returns<r.returns || u.returns>s.dispatches.Count(e=>e.status=="resolved" && !DispatchInterrupted(e) && e.resolved_day<=u.day) || r.stage>0 && (s.goal.Count<r.stage || s.goal[r.stage-1].status!="cleared" || u.day<s.goal[r.stage-1].resolved_day))throw new ArgumentException("Invalid unlock achievement");
            }
            if(s.game_over==null && rules.Any(r=>DispatchEligible(s,r) && !s.dispatch_unlocks.Any(u=>u.id==r.id)))throw new ArgumentException("Missing earned dispatch unlock");
            foreach(var e in s.dispatches)if(rules.Any(r=>r.id==e.destination) && !s.dispatch_unlocks.Any(u=>u.id==e.destination && u.day<=e.started_day))throw new ArgumentException("Departure before destination unlock");
        }
        public string DispatchChoiceReason(string id,string choice)
        {
            if(State.game_over!=null)return "経営終了後は回答できません。";
            var e=State.dispatches.SingleOrDefault(e=>e.id==id);var rule=e==null?null:DispatchEncounterDefinition(e.destination,e.started_day);
            if(rule==null || !rule.choices.Any(c=>c.id==choice))return "派遣イベントと選択肢を確認してください。";
            if(e.encounter.status=="resolved")return e.encounter.choice==choice?"":"回答済みの選択は変更できません。";
            return e.encounter.status=="waiting" && e.status=="travelling" && State.phase!="open"?"":"回答待ちの派遣イベントではありません。";
        }
        private void ResolveDispatchChoice(string id,string choice)
        {
            string reason=DispatchChoiceReason(id,choice);if(reason!="")throw new InvalidOperationException(reason);
            var e=State.dispatches.Single(e=>e.id==id);if(e.encounter.status=="resolved")return;
            var row=DispatchEncounterDefinition(e.destination,e.started_day).choices.Single(c=>c.id==choice);if(!Finite(e.reward+row.reward))throw new ArgumentException("Dispatch reward overflow");
            var c=State.cats.Single(c=>c.id==e.cat_id);var v=e.encounter;v.fatigue_before=c.fatigue;v.stress_before=c.stress;
            c.fatigue=Clamp(c.fatigue+row.fatigue,Rules.max_fatigue);c.stress=Clamp(c.stress+row.stress,100);
            v.fatigue_after=c.fatigue;v.stress_after=c.stress;v.status="resolved";v.resolved_day=State.day;v.choice=choice;State.revision++;
        }
        private void ValidateDispatchEncounter(CafeSnapshot s,CafeDispatch e)
        {
            var rule=DispatchEncounterDefinition(e.destination,e.started_day);var v=e.encounter;
            if(rule==null){if(v!=null)throw new ArgumentException("Unexpected dispatch encounter");return;}
            if(v==null || v.id!=rule.id)throw new ArgumentException("Missing dispatch encounter");
            int elapsed=s.day-e.started_day+(s.phase=="closed"?1:0);
            if(v.status=="scheduled" || v.status=="waiting")
            {
                if((v.status=="scheduled"?elapsed!=0 || v.occurred_day!=0:elapsed!=1 || v.occurred_day!=e.started_day || e.status!="travelling") || v.resolved_day!=0 || v.choice!=null || v.fatigue_before!=0 || v.fatigue_after!=0 || v.stress_before!=0 || v.stress_after!=0)throw new ArgumentException("Dispatch advanced before encounter response");return;
            }
            var row=rule.choices.SingleOrDefault(c=>c.id==v.choice);
            if(v.status!="resolved" || row==null || v.occurred_day!=e.started_day || v.resolved_day<e.started_day || v.resolved_day>Math.Min(s.day,e.started_day+1) || elapsed<1 || !Range(v.fatigue_before,0,Rules.max_fatigue) || !Range(v.stress_before,0,100) || v.fatigue_after!=Clamp(v.fatigue_before+row.fatigue,Rules.max_fatigue) || v.stress_after!=Clamp(v.stress_before+row.stress,100))throw new ArgumentException("Invalid dispatch encounter effects");
        }
    }
}
