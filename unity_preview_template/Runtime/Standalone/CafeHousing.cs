using System;
using System.Linq;
namespace CatCafePreview.Standalone
{
    [Serializable] public class HousingRules { public int initial_capacity, capacity_bonus; public double cost, daily_cost; }
    [Serializable] public class CafeHousingExtension { public int stage, day; }
    [Serializable] public class CafeHousing { public int purchased_day, upgraded_day; public CafeHousingExtension[] extensions; }
    public sealed partial class CafeSimulation
    {
        public int HousingStage(CafeSnapshot s, int day) => (s.housing.purchased_day > 0 && s.housing.purchased_day <= day ? 1 : 0)
            + (s.housing.upgraded_day > 0 && s.housing.upgraded_day <= day ? 1 : 0)
            + (s.housing.extensions ?? new CafeHousingExtension[0]).Count(e => e.day <= day);
        public int HousingMaxStage => 2 + (Rules.housing_extensions?.Length ?? 0);
        public HousingRules NextHousingRules => HousingStage(State,State.day)==0 ? Rules.housing : HousingStage(State,State.day)==1 ? Rules.housing_upgrade
            : HousingStage(State,State.day)<HousingMaxStage ? Rules.housing_extensions[HousingStage(State,State.day)-2] : null;
        public string NextHousingKind => HousingStage(State,State.day)==0 ? "purchase_housing" : HousingStage(State,State.day)==1 ? "upgrade_housing"
            : HousingStage(State,State.day)<HousingMaxStage ? "extend_housing" : null;
        public int HousingCapacity(CafeSnapshot s, int day) => Rules.housing.initial_capacity
            + (s.housing.purchased_day > 0 && s.housing.purchased_day <= day ? Rules.housing.capacity_bonus : 0)
            + (s.housing.upgraded_day > 0 && s.housing.upgraded_day <= day ? Rules.housing_upgrade.capacity_bonus : 0)
            + (s.housing.extensions ?? new CafeHousingExtension[0]).Where(e=>e.day<=day).Sum(e=>Rules.housing_extensions[e.stage-3].capacity_bonus);
        public double HousingDailyCost(CafeSnapshot s, int day) => HousingStage(s,day)>2 ? Rules.housing_extensions[HousingStage(s,day)-3].daily_cost
            : s.housing.upgraded_day > 0 && s.housing.upgraded_day <= day ? Rules.housing_upgrade.daily_cost
            : s.housing.purchased_day > 0 && s.housing.purchased_day <= day ? Rules.housing.daily_cost : 0;
        private double HousingExpenses(CafeSnapshot s, int day) => (s.housing.purchased_day == day ? Rules.housing.cost : 0) + (s.housing.upgraded_day == day ? Rules.housing_upgrade.cost : 0)
            + (s.housing.extensions ?? new CafeHousingExtension[0]).Where(e=>e.day==day).Sum(e=>Rules.housing_extensions[e.stage-3].cost);
        public string HousingReason(string kind)
        {
            if(AdoptionWaiting)return "譲渡の申し出に回答してください。";
            if (State.game_over != null) return "経営終了後は拡張できません。";
            if (State.cats.Any(c => c.activity == "missing" && c.missing_remaining == 0 && !DispatchMissing(State,c.id))) return "帰還結果を確認してください。";
            if (GoalWaiting || PatronWaiting || BondWaiting) return "目標の結果を確認してください。";
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
            else if (kind == "extend_housing")
            {
                if (HousingStage(State,State.day)<2) return "先に飼育スペースを2段階目まで拡張してください。";
                if (NextHousingRules==null) return "飼育スペースは最大まで拡張済みです。";
                if (State.funds<=NextHousingRules.cost) return "追加拡張後に資金が残る必要があります。";
            }
            else return "拡張操作が不正です。";
            return "";
        }
        private void ExpandHousing(string kind)
        {
            string reason = HousingReason(kind); if (reason != "") throw new InvalidOperationException(reason);
            double cost = kind == "purchase_housing" ? Rules.housing.cost : kind == "upgrade_housing" ? Rules.housing_upgrade.cost : NextHousingRules.cost;
            if (kind == "purchase_housing") State.housing.purchased_day = State.day;
            else if(kind == "upgrade_housing") State.housing.upgraded_day = State.day;
            else State.housing.extensions=(State.housing.extensions ?? new CafeHousingExtension[0]).Concat(new[]{new CafeHousingExtension{stage=HousingStage(State,State.day)+1,day=State.day}}).ToArray();
            State.funds -= cost; State.expenses += cost; State.housing_expenses += cost; State.revision++;
        }
        private void ValidateHousing(CafeSnapshot s)
        {
            var h = s.housing; var r = Rules.housing; var u = Rules.housing_upgrade;
            ValidateHousingExtensions(s);
            if (h == null || r == null || u == null || r.initial_capacity < 6 || r.capacity_bonus < 1 || u.capacity_bonus < 1
                || !Range(r.cost,double.Epsilon,double.MaxValue) || !Range(u.cost,double.Epsilon,double.MaxValue) || !Range(r.daily_cost,double.Epsilon,double.MaxValue) || !Range(u.daily_cost,r.daily_cost+double.Epsilon,double.MaxValue) || u.daily_cost <= r.daily_cost
                || h.purchased_day < 0 || h.purchased_day > s.day || h.upgraded_day < 0 || h.upgraded_day > s.day
                || h.upgraded_day > 0 && (h.purchased_day == 0 || h.upgraded_day < h.purchased_day)
                || ActiveCatCount(s) > HousingCapacity(s,s.day) || s.housing_expenses != HousingExpenses(s,s.day)
                || s.housing_cost != (s.phase == "closed" ? HousingDailyCost(s,s.day) : 0)
                || s.expenses < s.housing_expenses + s.housing_cost) throw new ArgumentException("Invalid housing state or expenses");
            foreach (var row in s.history)
            {
                int count = Rules.cats.Length + (s.intake_request.status == "accepted" && s.intake_request.resolved_day <= row.day ? 1 : 0)
                    + (s.regular_introduction.status == "accepted" && s.regular_introduction.resolved_day <= row.day ? 1 : 0)+DispatchJoinedCount(s,row.day)+VisitingJoinedCount(s,row.day)+ShopJoinedCount(s,row.day)+RecruitmentJoinedCount(s,row.day);
                count -= AdoptedBy(s,row.day,Rules.opening_ticks);
                if (count > HousingCapacity(s,row.day) || row.housing_expenses != HousingExpenses(s,row.day) || row.housing_cost != HousingDailyCost(s,row.day)
                    || row.expenses < row.housing_expenses + row.housing_cost) throw new ArgumentException("Housing disagrees with daily history");
            }
        }
        private void ValidateHousingExtensions(CafeSnapshot s)
        {
            if(s.housing==null)throw new ArgumentException("Missing housing state");
            var rules=Rules.housing_extensions ?? new HousingRules[0];
            double daily=Rules.housing_upgrade?.daily_cost ?? 0;
            foreach(var r in rules){if(r==null || r.capacity_bonus<1 || !Range(r.cost,double.Epsilon,double.MaxValue) || !Range(r.daily_cost,daily+double.Epsilon,double.MaxValue) || r.daily_cost<=daily)throw new ArgumentException("Invalid housing extension rules");daily=r.daily_cost;}
            var rows=s.housing.extensions ?? new CafeHousingExtension[0];
            if(rows.Length>rules.Length)throw new ArgumentException("Too many housing extensions");
            int previous=s.housing.upgraded_day;
            for(int i=0;i<rows.Length;i++){var e=rows[i];if(e==null || e.stage!=i+3 || previous<1 || e.day<previous || e.day>s.day)throw new ArgumentException("Invalid housing extension purchase");previous=e.day;}
        }
    }
}
