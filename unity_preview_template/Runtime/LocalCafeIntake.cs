namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private PythonIntakeState IntakeView()
        {
            var r = Simulation.Rules.intake; var s = Simulation.State;
            bool ready = !Simulation.ReservationWaiting && !Simulation.GoalWaiting && CanPrepare && !Simulation.GrowthWaiting && !Simulation.CustomerTrustWaiting && !Simulation.StoreEventWaiting && Simulation.IntakeWaiting;
            return new PythonIntakeState { cat_id = r.cat.id, name = r.cat.name, status = s.intake_request.status, day = r.day, cost = (float)r.cost,
                personality = r.personality, features = r.features_label, trait = r.trait,
                housing = "在籍 " + s.cats.Count + " / 上限 " + Simulation.HousingCapacity(s,s.day),
                response_reason = ready ? "" : "営業準備中に、他の確認待ちを解決して回答してください。",
                accept_reason = Simulation.IntakeAcceptReason, can_decline = ready, can_accept = ready && Simulation.IntakeAcceptReason == "" };
        }
    }
}
