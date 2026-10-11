using System.Linq;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private PythonObjectiveStart ObjectiveStartView()
        {
            var s=Simulation.State;bool started=s.goal.Count>0;
            return new PythonObjectiveStart {rows=new[]{new PythonStartObjective {choice="popularity",label="人気への挑戦",started=started,can_start=Simulation.PopularityChallengeReason()=="",reason=Simulation.PopularityChallengeReason(),summary=started?"開始済み":"未開始",details=string.Join(" → ",Simulation.Rules.goal_stages.Select(r=>"人気"+Number(r.target)+"（"+r.days+"日間）"))+"\n現在の店・猫・資金・他の目標を維持します。開始日を含む期限で、休業も日数に含みます。開始費用はなく、取消・再挑戦はできません。"+(started?"\n開始日："+s.goal[0].started_day+"日目":"")}},notes="開始時のモードは変更しません。"};
        }
    }
}
