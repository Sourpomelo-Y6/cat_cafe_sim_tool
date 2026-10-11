using System.Linq;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private PythonPatronDispatch GeneralDispatchView()
        {
            var s=Simulation.State;var r=Simulation.Rules.dispatch;
            return new PythonPatronDispatch {options=s.cats.Select(c=>new PythonDispatchOption {cat_id=c.id,choice=r.id,label=Simulation.Definition(c.id).name+" / "+r.name,days=r.days,reward=(float)Simulation.DispatchReward(c.id),detail="疲労 "+r.max_fatigue+"以下 / 出発費用なし / 帰還時にケア用品。長毛・派遣得意は各＋"+r.welcome.reward_bonus,reason=Simulation.DispatchReason(c.id,r.id),can_select=Simulation.DispatchReason(c.id,r.id)==""}).ToArray(),events=s.dispatches.Select(e=>new PythonDispatchEvent {cat_id=e.cat_id,choice=e.id,label=Simulation.Definition(e.cat_id).name+" / "+r.name,status=e.status,remaining=e.remaining,reward=(float)e.reward,needs_response=e.status=="waiting",can_select=e.status=="waiting" && Simulation.DispatchReceiveReason(e.id)=="",reason=e.status=="resolved"?"受け取り済みです。":Simulation.DispatchReceiveReason(e.id),detail="出発 "+e.started_day+"日目 / 帰還 "+e.occurred_day+"日目 / 確認 "+e.resolved_day+"日目 / 固定報酬 "+Number(e.reward)+" / 帰還ストレス ＋"+Number(e.return_stress)+" / 経験値 ＋"+Number(Simulation.Rules.growth.dispatch_xp)}).ToArray()};
        }
    }
}
