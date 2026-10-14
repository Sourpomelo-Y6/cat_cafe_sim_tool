using System;
using System.Linq;
using System.Collections.Generic;
namespace CatCafePreview.Standalone
{
    public sealed partial class CafeSimulation
    {
        private sealed class AutoNode {public CafeExchange exchange;public List<CafePlayerAction> path=new List<CafePlayerAction>();}
        private readonly Dictionary<string,List<CafePlayerAction>> autoPlayerPlans=new Dictionary<string,List<CafePlayerAction>>();
        private static CafeExchange AutoClone(CafeExchange e)
        {
            var clone=new CafeExchange();foreach(var field in typeof(CafeExchange).GetFields()){var value=field.GetValue(e);field.SetValue(clone,value is Array a?a.Clone():value);}return clone;
        }
        private IEnumerable<CafePlayerAction> AutoPlayerChoices(CafeExchange e,bool low=true)
        {
            if(e.end_reason!=null)yield break;
            if(e.connect_pending || low && e.stamina<=Rules.exchange.low_stamina){yield return new CafePlayerAction {action=e.connect_pending || e.tension>=Rules.exchange.optional_threshold?"connect":"pause"};yield break;}
            foreach(string action in Rules.types.Single(t=>t.id==e.mode).actions.Concat(new[]{"pause"}))yield return new CafePlayerAction {action=action};
            if(e.tension>=Rules.exchange.optional_threshold)yield return new CafePlayerAction {action="connect"};
            foreach(var type in Rules.types)if(type.id!=e.mode)yield return new CafePlayerAction {action="switch",target=type.id};
        }
        private CafeExchange AutoTrial(CafeExchange e,CatDefinition cat,CafePlayerAction a){var next=AutoClone(e);StepExchange(next,cat,a.action,a.target);return next;}
        private CafePlayerAction AutoShort(CafeExchange e,CatDefinition cat)
        {
            if(e.connect_pending || e.tension>=Rules.exchange.optional_threshold)return new CafePlayerAction {action="connect"};if(e.stamina<=20)return new CafePlayerAction {action="pause"};
            return AutoPlayerChoices(e,false).Select(a=>new {action=a,futures=AutoTrial(e,cat,a)}).Select(row=>new {row.action,score=row.futures.end_reason!=null?new[]{row.futures}:AutoPlayerChoices(row.futures,false).Where(a=>a.action!="switch").Select(a=>AutoTrial(row.futures,cat,a)).ToArray()}).Select(row=>new {row.action,best=row.score.OrderByDescending(f=>f.affinity_pending-e.affinity_pending).ThenByDescending(f=>f.stamina).First()}).OrderByDescending(row=>row.best.affinity_pending-e.affinity_pending).ThenByDescending(row=>row.best.stamina).First().action;
        }
        private double AutoPotential(CafeExchange e,double reserve)
        {
            double opened=Math.Min(Rules.exchange.optional_threshold,e.engagement)/Rules.exchange.optional_threshold,connected=Math.Min(Rules.exchange.optional_threshold,e.tension)/Rules.exchange.optional_threshold;
            return e.affinity_pending+.2*Math.Min(reserve,e.stamina)+(e.end_reason!=null?0:Rules.exchange.affinity_open_up*opened+Rules.exchange.affinity_connect*connected+Rules.exchange.affinity_simultaneous*Math.Min(opened,connected));
        }
        private List<CafePlayerAction> AutoBeam(CafeExchange initial,CatDefinition cat,double reserve)
        {
            var baseline=new AutoNode {exchange=AutoClone(initial)};while(baseline.exchange.end_reason==null){var a=AutoShort(baseline.exchange,cat);baseline.path.Add(a);baseline.exchange=AutoTrial(baseline.exchange,cat,a);}
            var nodes=new List<AutoNode>{new AutoNode {exchange=AutoClone(initial)}};
            while(nodes.Any(n=>n.exchange.end_reason==null))
            {
                var candidates=new List<AutoNode>();foreach(var n in nodes){if(n.exchange.end_reason!=null){candidates.Add(n);continue;}foreach(var a in AutoPlayerChoices(n.exchange))candidates.Add(new AutoNode {exchange=AutoTrial(n.exchange,cat,a),path=n.path.Concat(new[]{a}).ToList()});}
                nodes=candidates.OrderByDescending(n=>AutoPotential(n.exchange,reserve)).ThenByDescending(n=>n.exchange.stamina).ThenByDescending(n=>n.exchange.affinity_pending).Take(128).ToList();
            }
            nodes.Add(baseline);if(reserve>0){var gain=new AutoNode {exchange=AutoClone(initial),path=AutoBeam(initial,cat,0)};foreach(var a in gain.path)gain.exchange=AutoTrial(gain.exchange,cat,a);nodes.Add(gain);}
            return nodes.OrderByDescending(n=>n.exchange.affinity_pending+.2*Math.Min(reserve,n.exchange.stamina)).ThenByDescending(n=>n.exchange.affinity_pending).ThenByDescending(n=>n.exchange.stamina).First().path;
        }
        private CafeAutoOperation PlanAutoPlayer(bool keepStamina)
        {
            var session=ActivePlayer;var cat=Definition(session.cat_id);string key=PlayerSessionTicks(session)+":"+session.cat_id+":"+session.initial_stamina.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+":"+keepStamina;
            if(!autoPlayerPlans.TryGetValue(key,out var path)){var initial=new CafeExchange {cat_id=session.cat_id,remaining=PlayerSessionTicks(session),stamina=session.initial_stamina};path=AutoBeam(initial,cat,keepStamina?Rules.exchange.max_stamina*.6:0);if(autoPlayerPlans.Count>=128)autoPlayerPlans.Clear();autoPlayerPlans[key]=path;}
            bool matches=session.actions.Count<path.Count && session.actions.Select((a,i)=>a.action==path[i].action && a.target==path[i].target).All(v=>v);var action=matches?path[session.actions.Count]:AutoBeam(session.exchange,cat,keepStamina?Rules.exchange.max_stamina*.6:0).First();return AutoOp("player_step",choice:action.action,source:action.target);
        }
    }
}
