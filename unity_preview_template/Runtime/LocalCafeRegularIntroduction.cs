namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private PythonRegularIntroduction RegularIntroductionView()
        {
            var r = Simulation.Rules.regular_introduction; var s = Simulation.State; var data = s.regular_introduction;
            bool ready = !Simulation.DispatchTroubleWaiting && !Simulation.DispatchWaiting && !Simulation.DispatchChoiceWaiting && !Simulation.ReservationWaiting && !Simulation.GoalWaiting && !Simulation.PatronWaiting && !Simulation.BondWaiting && !Simulation.PlayerActive && CanPrepare && !Simulation.GrowthWaiting && !Simulation.CustomerTrustWaiting && !Simulation.StoreEventWaiting && Simulation.RegularIntroductionWaiting;
            return new PythonRegularIntroduction { cat_id = r.cat.id, content_cat_id = r.cat.id, name = r.cat.name, status = data.status,
                customer_id = data.customer_id, customer_name = Details.CustomerName(data.customer_id), presented_day = data.presented_day, resolved_day = data.resolved_day,
                cost = (float)r.cost, funds_after = (float)(s.funds - r.cost), threshold = (float)Simulation.Rules.loyalty.threshold,
                personality = r.personality, features = r.features_label, trait = r.trait, housing = "在籍 " + Simulation.ActiveCatCount(s) + " / 上限 " + Simulation.HousingCapacity(s,s.day),
                response_reason = ready ? "" : "営業準備中に、帰還・店舗イベントの確認待ちを解決してください。",
                accept_reason = Simulation.RegularIntroductionAcceptReason, can_decline = ready, can_accept = ready && Simulation.RegularIntroductionAcceptReason == "" };
        }
    }
}
