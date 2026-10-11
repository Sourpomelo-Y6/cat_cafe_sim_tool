using System.Linq;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private PythonVisitingCat VisitingCatView()
        {
            var r=Simulation.Rules.visiting_cat;var s=Simulation.State;var v=s.visiting_cat;
            string reason=Simulation.VisitingResponseReason(),daily=Simulation.VisitingDailyReason(),admission=Simulation.VisitingAdmissionReason();
            return new PythonVisitingCat {cat_id=r.cat.id,content_cat_id=r.cat.id,name=r.cat.name,status=v.status,personality=r.personality,features=r.features_label,trait=r.trait,
                housing="在籍 "+s.cats.Count+" / 上限 "+Simulation.HousingCapacity(s,s.day),history=string.Join("\n",v.actions.Select(a=>a.day+"日目："+(a.choice=="interact"?"交流した":"今日は見送った"))),
                reason=reason,daily_reason=daily,accept_reason=admission,progress=Simulation.VisitingProgress,required=r.interactions_required,presented_day=v.presented_day,accepted_day=v.accepted_day,
                cost=(float)r.cost,funds_after=(float)(s.funds-r.cost),can_interact=daily=="" && v.status=="visiting",can_skip=daily=="",can_accept=admission==""};
        }
    }
}
