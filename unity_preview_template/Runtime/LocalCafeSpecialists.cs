using System.Linq;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private PythonReservation ReservationView()
        {
            if(Simulation.State.goal.Count==0)return null;
            var s=Simulation.State.reservation;var r=Simulation.Rules.reservation;string reason=Simulation.ReservationReason();
            var catalog=UnityEngine.JsonUtility.FromJson<CatCafePreview.Standalone.CafePresentationCatalog>(UnityEngine.Resources.Load<UnityEngine.TextAsset>("CatCafeStandalone/presentation").text);string feature=catalog.features.FirstOrDefault(x=>x.id==r.feature)?.label??r.feature;
            return new PythonReservation {event_id=s.offered_day>0?"reservation-"+s.offered_day:null,status=s.status,customer_id="reservation-longhair",name="長毛猫との時間を望む予約客",description="担当猫の特徴と、接客中の「心を開く」の回数を指定する予約です。",feature_label=feature,result=s.result,reason=reason,offered_day=s.offered_day,visit_day=s.visit_day,resolved_day=s.resolved_day,open_up_count=r.open_up_count,arrival_tick=r.arrival_tick,bonus=(float)r.bonus,popularity_bonus=(float)r.popularity_bonus,can_accept=reason=="",can_decline=reason==""};
        }
    }
}
