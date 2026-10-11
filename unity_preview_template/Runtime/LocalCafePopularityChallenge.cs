using System.Linq;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private PythonObjectiveStart ObjectiveStartView()
        {
            var s=Simulation.State;bool started=s.goal.Count>0;
            return new PythonObjectiveStart {rows=new[]{
                new PythonStartObjective {choice="popularity",label="人気への挑戦",started=started,can_start=Simulation.ObjectiveStartReason("popularity")=="",reason=Simulation.ObjectiveStartReason("popularity"),summary=started?"開始済み":"未開始",details=string.Join(" → ",Simulation.Rules.goal_stages.Select(r=>"人気"+Number(r.target)+"（"+r.days+"日間）"))+"\n現在の店・猫・資金・他の目標を維持します。開始日を含む期限で、休業も日数に含みます。開始費用はなく、取消・再挑戦はできません。"+(started?"\n開始日："+s.goal[0].started_day+"日目":"")},
                IndependentStartRow("bond","猫との好感度目標",s.bond_goal!=null,s.bond_goal?.started_day??0,"好感度"+Number(Simulation.Rules.bond.affinity)+"以上の猫を"+Simulation.Rules.bond.target+"匹。既存の好感度を引き継ぎ、条件を満たしていれば開始時に達成します。"),
                IndependentStartRow("patron","有力者の満足度目標",s.patron!=null,s.patron?.started_day??0,"3人それぞれの満足度目標へ挑戦します。満足度は0から開始し、導入後の派遣帰還確認で加算します。既存の通常派遣と育成は維持します。")},notes="開始時のモードは変更しません。"};
        }
        private PythonStartObjective IndependentStartRow(string choice,string label,bool started,int day,string details)
        {
            string reason=Simulation.ObjectiveStartReason(choice);return new PythonStartObjective {choice=choice,label=label,started=started,can_start=reason=="",reason=reason,summary=started?"開始済み":"未開始",details=details+"\n期限なし。店・猫・資金・他の目標の進捗を維持します。開始費用はなく、取消・再開始はできません。"+(started?"\n開始日："+day+"日目":"")};
        }
    }
}
