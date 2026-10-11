using System;
using System.Linq;
namespace CatCafePreview.Standalone
{
    [Serializable] public class HousingRules { public int initial_capacity, capacity_bonus; public double cost, daily_cost; }
    [Serializable] public class CafeHousing { public int purchased_day, upgraded_day; }
    public sealed partial class CafeSimulation
    {
        public int HousingCapacity(CafeSnapshot s, int day) => Rules.housing.initial_capacity
            + (s.housing.purchased_day > 0 && s.housing.purchased_day <= day ? Rules.housing.capacity_bonus : 0)
            + (s.housing.upgraded_day > 0 && s.housing.upgraded_day <= day ? Rules.housing_upgrade.capacity_bonus : 0);
        public double HousingDailyCost(CafeSnapshot s, int day) => s.housing.upgraded_day > 0 && s.housing.upgraded_day <= day ? Rules.housing_upgrade.daily_cost
            : s.housing.purchased_day > 0 && s.housing.purchased_day <= day ? Rules.housing.daily_cost : 0;
        private double HousingExpenses(CafeSnapshot s, int day) => (s.housing.purchased_day == day ? Rules.housing.cost : 0) + (s.housing.upgraded_day == day ? Rules.housing_upgrade.cost : 0);
        public string HousingReason(string kind)
        {
            if (State.game_over != null) return "経営終了後は拡張できません。";
            if (State.cats.Any(c => c.activity == "missing" && c.missing_remaining == 0)) return "帰還結果を確認してください。";
            if (GoalWaiting) return "人気目標の結果を確認してください。";
            if (ReservationWaiting) return "特別予約に回答してください。";
            if (CustomerTrustWaiting) return "顧客の信頼回復イベントに回答してください。";
            if (GrowthWaiting) return "猫の成長・習熟を選択してください。";
            if (StoreEventWaiting) return "店舗イベントに回答してください。";
            if (State.phase != "preparation") return "飼育スペースの拡張は営業準備中に行ってください。";
            if (kind == "purchase_housing")
            {
                if (State.housing.purchased_day > 0) return "飼育スペースは拡張済みです。";
                if (State.funds <= Rules.housing.cost) return "購入後に資金が残る必要があります。";
            }
            else if (kind == "upgrade_housing")
            {
                if (State.housing.purchased_day == 0) return "先に飼育スペースを初回拡張してください。";
                if (State.housing.upgraded_day > 0) return "飼育スペースは2段階目まで拡張済みです。";
                if (State.funds <= Rules.housing_upgrade.cost) return "追加拡張後に資金が残る必要があります。";
            }
            else return "拡張操作が不正です。";
            return "";
        }
        private void ExpandHousing(string kind)
        {
            string reason = HousingReason(kind); if (reason != "") throw new InvalidOperationException(reason);
            double cost = kind == "purchase_housing" ? Rules.housing.cost : Rules.housing_upgrade.cost;
            if (kind == "purchase_housing") State.housing.purchased_day = State.day; else State.housing.upgraded_day = State.day;
            State.funds -= cost; State.expenses += cost; State.housing_expenses += cost; State.revision++;
        }
        private void ValidateHousing(CafeSnapshot s)
        {
            var h = s.housing; var r = Rules.housing; var u = Rules.housing_upgrade;
            if (h == null || r == null || u == null || r.initial_capacity < 6 || r.capacity_bonus < 1 || u.capacity_bonus < 1
                || !Range(r.cost,double.Epsilon,double.MaxValue) || !Range(u.cost,double.Epsilon,double.MaxValue) || !Range(r.daily_cost,double.Epsilon,double.MaxValue) || !Range(u.daily_cost,r.daily_cost+double.Epsilon,double.MaxValue) || u.daily_cost <= r.daily_cost
                || h.purchased_day < 0 || h.purchased_day > s.day || h.upgraded_day < 0 || h.upgraded_day > s.day
                || h.upgraded_day > 0 && (h.purchased_day == 0 || h.upgraded_day < h.purchased_day)
                || s.cats.Count > HousingCapacity(s,s.day) || s.housing_expenses != HousingExpenses(s,s.day)
                || s.housing_cost != (s.phase == "closed" ? HousingDailyCost(s,s.day) : 0)
                || s.expenses < s.housing_expenses + s.housing_cost) throw new ArgumentException("Invalid housing state or expenses");
            foreach (var row in s.history)
            {
                int count = Rules.cats.Length + (s.intake_request.status == "accepted" && s.intake_request.resolved_day <= row.day ? 1 : 0)
                    + (s.regular_introduction.status == "accepted" && s.regular_introduction.resolved_day <= row.day ? 1 : 0)+DispatchJoinedCount(s,row.day);
                if (count > HousingCapacity(s,row.day) || row.housing_expenses != HousingExpenses(s,row.day) || row.housing_cost != HousingDailyCost(s,row.day)
                    || row.expenses < row.housing_expenses + row.housing_cost) throw new ArgumentException("Housing disagrees with daily history");
            }
        }
    }
}
