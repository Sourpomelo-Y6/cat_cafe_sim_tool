using System;
using System.Collections.Generic;
using System.Linq;
namespace CatCafePreview.Standalone
{
    public sealed partial class CafeSimulation
    {
        public bool RegularIntroductionWaiting => State.regular_introduction.status == "waiting";
        private static CafeLoyalty FirstRegular(IEnumerable<CafeLoyalty> customers) => customers.Where(c => c.regular_day > 0).OrderBy(c => c.regular_day).ThenBy(c => c.customer_id, StringComparer.Ordinal).FirstOrDefault();
        internal void PresentRegularIntroduction()
        {
            var r = State.regular_introduction; var first = FirstRegular(State.loyalty);
            if (r.status == "untriggered" && first != null && first.regular_day < State.day && State.phase == "preparation" && State.game_over == null)
            {
                r.status = "waiting"; r.customer_id = first.customer_id; r.regular_day = first.regular_day; r.presented_day = State.day;
            }
        }
        public string RegularIntroductionAcceptReason => State.cats.Count >= HousingCapacity(State, State.day) ? "飼育スペースが満員です。" : State.funds <= Rules.regular_introduction.cost ? "受け入れ後に資金が残る必要があります。" : "";
        private void ResolveRegularIntroduction(string choice)
        {
            if (choice != "accept" && choice != "decline") throw new ArgumentException("常連からの猫紹介で迎える／見送るを選んでください。");
            var r = State.regular_introduction; string status = choice == "accept" ? "accepted" : "declined";
            if (r.status == "accepted" || r.status == "declined")
            { if (r.status != status) throw new InvalidOperationException("回答済みの猫紹介は変更できません。"); return; }
            if (State.cats.Any(c => c.activity == "missing" && c.missing_remaining == 0 && !DispatchMissing(State,c.id))) throw new InvalidOperationException("帰還結果を確認してください。");
            if (CustomerTrustWaiting) throw new InvalidOperationException("顧客の信頼回復イベントに回答してください。");
            if (GrowthWaiting) throw new InvalidOperationException("猫の成長・習熟を選択してください。");
            if (StoreEventWaiting) throw new InvalidOperationException("店舗イベントに回答してください。");
            if (GoalWaiting) throw new InvalidOperationException("人気目標の結果を確認してください。");
            Require("preparation");
            if (!RegularIntroductionWaiting) throw new InvalidOperationException("常連からの猫紹介への回答は営業準備中に行ってください。");
            if (choice == "accept")
            {
                if (RegularIntroductionAcceptReason != "") throw new InvalidOperationException(RegularIntroductionAcceptReason);
                JoinCat(Rules.regular_introduction);
            }
            r.status = status; r.resolved_day = State.day; State.revision++;
        }
        private void ValidateRegularIntroduction(CafeSnapshot s)
        {
            var r = s.regular_introduction; var rule = Rules.regular_introduction;
            if (r == null || rule == null || rule.cat == null || Rules.cats.Any(c => c.id == rule.cat.id) || rule.cat.id == Rules.intake.cat.id
                || !new[] { "untriggered", "waiting", "accepted", "declined" }.Contains(r.status)) throw new ArgumentException("Invalid regular introduction state");
            var first = FirstRegular(s.loyalty);
            if (r.status == "untriggered")
            {
                if (r.customer_id != null || r.regular_day != 0 || r.presented_day != 0 || r.resolved_day != 0 || first != null && first.regular_day < s.day && s.game_over == null) throw new ArgumentException("Unpresented regular introduction");
                return;
            }
            if (first == null || r.customer_id != first.customer_id || r.regular_day != first.regular_day || r.presented_day != r.regular_day + 1 || r.presented_day > s.day
                || (r.status == "waiting" ? r.resolved_day != 0 || r.presented_day != s.day || s.phase != "preparation" : r.resolved_day != r.presented_day)) throw new ArgumentException("Regular introduction does not match loyalty history");
        }
    }
}
