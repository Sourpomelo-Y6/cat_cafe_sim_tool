using System.Linq;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private PythonAdoption AdoptionView(){var a=Simulation.State.adoption;string reason=Simulation.AdoptionConfigureReason();return new PythonAdoption {enabled=a?.enabled??false,can_configure=reason=="",reason=reason,events=(a?.events ?? new System.Collections.Generic.List<Standalone.CafeAdoptionEvent>()).Select(e=>{string problem=Simulation.AdoptionResponseReason(e.id);return new PythonAdoptionEvent {event_id=e.id,cat_id=e.cat_id,name=Simulation.Definition(e.cat_id).name,customer_id=e.customer_id,customer_name=Details.CustomerName(e.customer_id),status=e.status,choice=e.choice??"",day=e.day,guest_affinity=(float)e.guest_affinity,player_affinity=(float)e.player_affinity,threshold=(float)e.threshold,can_accept=problem=="",can_decline=problem=="",accept_reason=problem,decline_reason=problem};}).ToArray()};}
    }
}
