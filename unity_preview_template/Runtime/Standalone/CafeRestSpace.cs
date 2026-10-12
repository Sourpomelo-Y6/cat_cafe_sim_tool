using System;
using System.Linq;
namespace CatCafePreview.Standalone
{
    [Serializable] public class RestSpaceRules { public double cost,recovery_bonus,stress_recovery_bonus; }
    [Serializable] public class CafeRestSpace { public int purchased_day,upgraded_day,soundproofed_day; }
    public sealed partial class CafeSimulation
    {
        public double RestSpaceRecovery => State.rest_space.upgraded_day>0?Rules.rest_space_upgrade.recovery_bonus:State.rest_space.purchased_day>0?Rules.rest_space.recovery_bonus:0;
        public double RestSpaceStressRecovery => State.rest_space.soundproofed_day>0?Rules.rest_space_soundproof.stress_recovery_bonus:0;
        public string RestSpaceReason(string kind)
        {
            string reason=ItemsReason();if(reason!="")return reason.Replace("用品","休養設備");
            var r=State.rest_space;
            if(kind=="purchase_rest_space") {if(r.purchased_day>0)return "休養スペースは購入・設置済みです。";}
            else if(kind=="upgrade_rest_space")
            {
                if(r.purchased_day==0)return "先に休養スペースを購入・設置してください。";
                if(r.upgraded_day>0)return "休養スペースは強化済みです。";
                if(ClearedGoalDay(1)==0 || ClearedGoalDay(1)>=State.day)return "休養スペースの強化は人気第1段階達成翌日から解放されます。";
            }
            else if(kind=="soundproof_rest_space")
            {
                if(r.purchased_day==0)return "先に休養スペースを購入・設置してください。";
                if(r.soundproofed_day>0)return "防音改修は実施済みです。";
                if(ClearedGoalDay(2)==0 || ClearedGoalDay(2)>=State.day)return "防音改修は人気第2段階達成翌日から解放されます。";
            }
            else return "休養設備の操作が不正です。";
            return State.funds<=RestSpaceRule(kind).cost?"購入・改修後に資金が残る必要があります。":"";
        }
        public RestSpaceRules RestSpaceRule(string kind) => kind=="purchase_rest_space"?Rules.rest_space:kind=="upgrade_rest_space"?Rules.rest_space_upgrade:kind=="soundproof_rest_space"?Rules.rest_space_soundproof:throw new ArgumentException("Invalid rest space action");
        private void ChangeRestSpace(string kind)
        {
            string reason=RestSpaceReason(kind);if(reason!="")throw new InvalidOperationException(reason);
            double cost=RestSpaceRule(kind).cost;
            if(kind=="purchase_rest_space")State.rest_space.purchased_day=State.day;else if(kind=="upgrade_rest_space")State.rest_space.upgraded_day=State.day;else State.rest_space.soundproofed_day=State.day;
            State.funds-=cost;State.expenses+=cost;State.rest_space_expenses+=cost;
        }
        private double RestSpaceExpenses(CafeSnapshot s,int day) => (s.rest_space.purchased_day==day?Rules.rest_space.cost:0)+(s.rest_space.upgraded_day==day?Rules.rest_space_upgrade.cost:0)+(s.rest_space.soundproofed_day==day?Rules.rest_space_soundproof.cost:0);
        private void ValidateRestSpace(CafeSnapshot s)
        {
            var r=s.rest_space;var basic=Rules.rest_space;var upgrade=Rules.rest_space_upgrade;var sound=Rules.rest_space_soundproof;
            if(r==null || basic==null || upgrade==null || sound==null || !Range(basic.cost,double.Epsilon,double.MaxValue) || !Range(upgrade.cost,double.Epsilon,double.MaxValue) || !Range(sound.cost,double.Epsilon,double.MaxValue)
                || !Range(basic.recovery_bonus,double.Epsilon,100) || !Range(upgrade.recovery_bonus,double.Epsilon,100) || upgrade.recovery_bonus<=basic.recovery_bonus || !Range(sound.stress_recovery_bonus,double.Epsilon,100)
                || basic.stress_recovery_bonus!=0 || upgrade.stress_recovery_bonus!=0 || sound.recovery_bonus!=0
                || r.purchased_day<0 || r.purchased_day>s.day || r.upgraded_day<0 || r.upgraded_day>s.day || r.soundproofed_day<0 || r.soundproofed_day>s.day
                || r.upgraded_day>0 && (r.purchased_day==0 || r.upgraded_day<r.purchased_day || s.goal.Count<1 || s.goal[0].status!="cleared" || r.upgraded_day<=s.goal[0].resolved_day)
                || r.soundproofed_day>0 && (r.purchased_day==0 || r.soundproofed_day<r.purchased_day || s.goal.Count<2 || s.goal[1].status!="cleared" || r.soundproofed_day<=s.goal[1].resolved_day)
                || s.rest_space_expenses!=RestSpaceExpenses(s,s.day) || s.expenses<s.rest_space_expenses+s.equipment_expenses)throw new ArgumentException("Invalid rest space state or finance");
            foreach(var h in s.history)if(h.rest_space_expenses!=RestSpaceExpenses(s,h.day) || h.expenses<h.rest_space_expenses+h.equipment_expenses)throw new ArgumentException("Rest space finance differs from history");
        }
    }
}
