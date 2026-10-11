namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private PythonReservation ReservationView()
        {
            if(Simulation.State.objective!="popularity")return null;
            var s=Simulation.State.reservation;var r=Simulation.Rules.reservation;string reason=Simulation.ReservationReason();
            return new PythonReservation {event_id=s.offered_day>0?"reservation-"+s.offered_day:null,status=s.status,customer_id="reservation-longhair",name="長毛猫との時間を望む予約客",description="人気第2段階達成の翌日に一度だけ依頼。長毛猫で心を開くを"+r.open_up_count+"回以上。引き受けた来店日は休業できません。",result=s.result,reason=reason,offered_day=s.offered_day,visit_day=s.visit_day,resolved_day=s.resolved_day,open_up_count=r.open_up_count,arrival_tick=r.arrival_tick,bonus=(float)r.bonus,popularity_bonus=(float)r.popularity_bonus,can_accept=reason=="",can_decline=reason==""};
        }
    }
}
