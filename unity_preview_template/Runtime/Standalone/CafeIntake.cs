using System;
using System.Linq;
using System.Collections.Generic;
namespace CatCafePreview.Standalone
{
    public sealed partial class CafeSimulation
    {
        public bool IntakeWaiting => State.intake_request.status == "waiting";
        private IEnumerable<CatDefinition> Definitions(CafeSnapshot s)
        {
            var cats = (IEnumerable<CatDefinition>)Rules.cats;
            if (s.intake_request != null && s.intake_request.status == "accepted") cats = cats.Concat(new[] { Rules.intake.cat });
            if (s.regular_introduction != null && s.regular_introduction.status == "accepted") cats = cats.Concat(new[] { Rules.regular_introduction.cat });
            if (s.visiting_cat != null && s.visiting_cat.status == "accepted") cats = cats.Concat(new[] { Rules.visiting_cat.cat });
            cats = cats.Concat(ShopDefinitions(s));
            return cats.Concat(DispatchIntroductions(s).Where(e=>e.introduction.status=="accepted").Select(e=>DispatchCandidate(e)));
        }
        private void JoinCat(IntakeRules rule, bool shop = false)
        {
            if (State.cats.Any(c => c.id == rule.cat.id)) throw new InvalidOperationException("この猫はすでに加入しています。");
            if (State.funds <= rule.cost) throw new InvalidOperationException("受け入れ後に資金が残る必要があります。");
            if (State.cats.Count >= HousingCapacity(State, State.day)) throw new InvalidOperationException("飼育スペースが満員です。");
            State.cats.Add(new CafeCat { id = rule.cat.id, stamina = Rules.max_stamina, working = false });
            EnsureGrowthCat(rule.cat.id);
            State.funds -= rule.cost; State.expenses += rule.cost; if(shop) State.pet_shop_expenses += rule.cost; else State.recruitment_expenses += rule.cost;
        }
        public CatDefinition Definition(string id) => Definitions(State).First(c => c.id == id);
        private void PresentIntake()
        {
            if (State.intake_request.status == "scheduled" && State.day == Rules.intake.day && State.game_over == null)
            { State.intake_request.status = "waiting"; State.intake_request.presented_day = State.day; }
        }
        public string IntakeAcceptReason => State.funds <= Rules.intake.cost ? "受け入れ後に資金が残る必要があります。" : State.cats.Count >= HousingCapacity(State, State.day) ? "飼育スペースが満員です。" : "";
        private void ResolveIntake(string choice)
        {
            if (choice != "accept" && choice != "decline") throw new ArgumentException("迎えるか見送るかを選んでください。");
            var r = State.intake_request; string status = choice == "accept" ? "accepted" : "declined";
            if (r.status == "accepted" || r.status == "declined")
            { if (r.status != status) throw new InvalidOperationException("回答済みの依頼は変更できません。"); return; }
            if (State.cats.Any(c => c.activity == "missing" && c.missing_remaining == 0 && !DispatchMissing(State,c.id))) throw new InvalidOperationException("帰還結果を確認してください。");
            if (CustomerTrustWaiting) throw new InvalidOperationException("顧客の信頼回復イベントに回答してください。");
            if (GrowthWaiting) throw new InvalidOperationException("猫の成長・習熟を選択してください。");
            if (StoreEventWaiting) throw new InvalidOperationException("店舗イベントに回答してください。");
            if (GoalWaiting || PatronWaiting || BondWaiting) throw new InvalidOperationException("目標の結果を確認してください。");
            Require("preparation");
            if (!IntakeWaiting) throw new InvalidOperationException("回答待ちの依頼はありません。");
            if (choice == "accept")
            {
                if (IntakeAcceptReason != "") throw new InvalidOperationException(IntakeAcceptReason);
                JoinCat(Rules.intake);
            }
            r.status = status; r.resolved_day = State.day; State.revision++;
        }
        private void ValidateIntake(CafeSnapshot s)
        {
            var r = s.intake_request; var rule = Rules.intake;
            if (r == null || rule == null || rule.cat == null || Rules.cats.Any(c => c.id == rule.cat.id)
                || s.cats.Count > HousingCapacity(s, s.day) || !new[] { "scheduled", "waiting", "accepted", "declined" }.Contains(r.status)) throw new ArgumentException("Invalid intake state");
            bool answered = r.status == "accepted" || r.status == "declined";
            if (r.status == "scheduled" ? s.day >= rule.day || r.presented_day != 0 || r.resolved_day != 0
                : s.day < rule.day || r.presented_day != rule.day || (answered ? r.resolved_day != rule.day : r.resolved_day != 0 || s.day != rule.day || s.phase != "preparation")) throw new ArgumentException("Invalid intake dates");
            if (Rules.cats.Any(d => !s.cats.Any(c => c.id == d.id)) || s.cats.Any(c => !Definitions(s).Any(d => d.id == c.id))) throw new ArgumentException("Invalid intake roster");
            for (int day = 1; day <= s.day; day++)
            {
                double expected = r.status == "accepted" && r.resolved_day == day ? rule.cost : 0;
                if (s.regular_introduction != null && s.regular_introduction.status == "accepted" && s.regular_introduction.resolved_day == day) expected += Rules.regular_introduction.cost;
                expected += DispatchIntroductions(s).Where(e=>e.introduction.status=="accepted" && e.introduction.resolved_day==day).Sum(e=>e.introduction.cost);
                expected += VisitingJoinedCount(s,day)>0 && s.visiting_cat.accepted_day==day ? Rules.visiting_cat.cost : 0;
                double cost = day == s.day ? s.recruitment_expenses : s.history[day - 1].recruitment_expenses;
                double total = day == s.day ? s.expenses : s.history[day - 1].expenses;
                if (cost != expected || total < cost) throw new ArgumentException("Invalid intake expense history");
            }
        }
    }
}
