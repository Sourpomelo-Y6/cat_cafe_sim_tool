using System.Linq;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private PythonAutoPlay AutoplayView()
        {
            var s=Simulation.State;var r=s.autoplay;string reason=Simulation.AutoStartReason();bool running=r?.running==true;
            return new PythonAutoPlay {running=running,paused=r?.paused==true,can_start=!running && reason=="",reason=reason,notes="基礎営業・安定経営・積極経営 / 1〜10日。成長は営業後に猫に合う内容で自動確定します。猫適性の選択は目標達成を保証しません。専門確定後は変更できません。手動で育成する場合は、おまかせを中止して通常営業後に選んでください。他の回答待ち・目標結果で停止します。保存すると一時停止し、読み込み後は明示的に再開します。中止後は元の割り当て設定へ戻ります。",objectives=new[]{"popularity","bond","patron"}.Where(t=>Simulation.AutoTargetStatus(t)!=null).Select(t=>new PythonAutoPlayOption {choice=t,label=t=="popularity"?"人気":t=="bond"?"好感度":"有力者",status=Simulation.AutoTargetStatus(t),can_start=!running && reason=="" && Simulation.AutoTargetStatus(t)=="active"}).ToArray(),modes=new[]{new PythonAutoPlayOption {choice="basic",label="基礎営業"},new PythonAutoPlayOption {choice="clear",label="安定経営"},new PythonAutoPlayOption {choice="fast",label="積極経営"}},report=r==null?null:new PythonAutoPlayReport {version=r.version,run_id=r.run_id,running=r.running,objective=r.objective,mode=r.mode,limit=r.limit,days=r.days,operations=r.operations,reason=r.reason,message=r.message,summary=r.summary,lines=r.lines.ToArray(),decisions=r.decisions.Select(d=>new PythonAutoPlayDecision {day=d.day,target=d.target,choice=d.choice,reason=d.reason,subject=d.subject,status=d.status}).ToArray()}};
        }
    }
}
