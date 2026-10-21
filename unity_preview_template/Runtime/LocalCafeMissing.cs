using System;
using System.Linq;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private bool MissingWaiting => Simulation.State.cats.Any(c=>c.activity=="missing" && c.missing_remaining==0 && !Simulation.DispatchMissing(Simulation.State,c.id));
        private PythonMissingCat[] MissingView()
        {
            SyncConversations();var s=Simulation.State;
            return s.missing_cases.Select(e=>
            {
                var c=s.cats.Single(x=>x.id==e.cat_id);bool resolved=e.resolved_day>0;bool waiting=!resolved && c.missing_remaining==0;double cost=resolved?e.cost:waiting && c.kitten?Simulation.Rules.kitten_cost:0;
                string reason="";
                if(waiting)
                {
                    var probe=new CafeSimulation(Simulation.Rules,s.seed,s.objective);var copy=UnityEngine.JsonUtility.FromJson<CafeSnapshot>(UnityEngine.JsonUtility.ToJson(s));copy.NormalizeJsonStrings();CafeDoublePrecision.Restore(copy,CafeDoublePrecision.Capture(s));probe.Restore(copy);
                    try{probe.Execute("resolve_missing",catId:c.id);}catch(Exception ex){reason=ex.Message;}
                }
                // Older snapshots do not store the departure date. Do not invent it.
                return new PythonMissingCat{choice=e.id,cat_id=c.id,name=Simulation.Definition(c.id).name,status=resolved?"resolved":waiting?"waiting":"missing",departed_day=e.departed_day,resolved_day=e.resolved_day,remaining=resolved?0:c.missing_remaining,kitten=resolved?e.kitten:waiting && c.kitten,cost=(float)cost,funds_after=(float)(resolved?e.funds_after:s.funds-cost),return_stress=(float)(resolved?e.stress:Simulation.Rules.return_stress),max_stamina=(float)(resolved?e.stamina:Simulation.MaxStamina(c.id)),can_resolve=waiting && reason=="",reason=reason};
            }).ToArray();
        }
    }
}
