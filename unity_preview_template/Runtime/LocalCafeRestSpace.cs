using System.Linq;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private PythonRestSpace RestSpaceView()
        {
            var s=Simulation.State;var r=s.rest_space;double fatigue=Simulation.RestSpaceRecovery,stress=Simulation.RestSpaceStressRecovery;
            return new PythonRestSpace {status=r.purchased_day==0?"未設置":r.upgraded_day>0?"強化済み":"設置済み",recovery_bonus=(float)fatigue,stress_recovery_bonus=(float)stress,
                details="購入日："+r.purchased_day+" / 強化日："+r.upgraded_day+" / 防音改修日："+r.soundproofed_day+" / 本日の設備費："+s.rest_space_expenses,
                notes="在店して休養する猫の日末回復に加算します。強化は人気第1段階、防音は第2段階の達成翌日から。各操作は1回限りです。",
                actions=new[]{"purchase_rest_space","upgrade_rest_space","soundproof_rest_space"}.Select(kind=>{var rule=Simulation.RestSpaceRule(kind);string reason=Simulation.RestSpaceReason(kind);return new PythonRestSpaceAction {
                    kind=kind,label=kind=="purchase_rest_space"?"休養スペースを購入・設置":kind=="upgrade_rest_space"?"休養スペースを強化":"休養スペースを防音改修",reason=reason,can_execute=reason=="",cost=(float)rule.cost,funds_after=(float)(s.funds-rule.cost),recovery_before=(float)fatigue,recovery_after=(float)(kind=="soundproof_rest_space"?fatigue:rule.recovery_bonus),stress_before=(float)stress,stress_after=(float)(kind=="soundproof_rest_space"?rule.stress_recovery_bonus:stress)};}).ToArray()};
        }
    }
}
