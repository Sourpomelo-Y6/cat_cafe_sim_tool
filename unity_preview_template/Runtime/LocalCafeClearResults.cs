using System.Globalization;
using System.Linq;
namespace CatCafePreview
{
    [System.Serializable] public class PythonEnding { public string status,title,details,reason; public bool pending,can_continue; }
    public sealed partial class LocalCafeBackend
    {
        private static string Number(double value)=>value.ToString("G6",CultureInfo.InvariantCulture);
        private PythonClearResults ClearResultsView()
        {
            var s=Simulation.State;
            return new PythonClearResults {rows=s.clear_results.Select(r=>new PythonClearRecord {mode=r.mode,label=r.mode=="bond"?"好感度目標":r.mode=="patron"?"有力者目標":"人気目標",details="達成日："+r.day+"日目\n目標の達成値："+Number(r.value)+" / "+Number(r.target)+"\n所持金："+Number(r.funds)+"\n店の人気："+Number(r.popularity)+"\n在籍猫数："+r.cats+"匹\n累計接客売上："+Number(r.revenue),notes="達成した瞬間の記録です。継続営業後も数値は変わりません。"+((r.mode=="bond"?s.bond_goal.continued:r.mode=="patron"?s.patron.continued:s.goal.Last().continued)?" この目標の結果は確認済みです。":"")}).ToArray(),notes="達成した目標の結果を、ここで見返せます。"};
        }
        private PythonEnding EndingView()
        {
            var s=Simulation.State;
            if(s.game_over!=null)return new PythonEnding {status="game_over",title="経営終了",details=s.game_over=="funds"?"資金がなくなりました。結果の閲覧・保存ができます。":"店の人気がなくなりました。結果の閲覧・保存ができます。",reason=s.game_over,pending=true};
            if(Simulation.BondWaiting)return new PythonEnding {status="cleared",title="好感度目標をクリアしました",details="好感度80以上の猫を6匹集めました。達成時の成果を保存しました。結果を確認して継続営業できます。",pending=true,can_continue=Simulation.BondContinueReason()=="",reason=Simulation.BondContinueReason()};
            if(Simulation.PatronWaiting)return new PythonEnding {status="cleared",title="有力者目標をクリアしました",details="3人の満足度を達成しました。クリア時の成果を記録しました。結果を確認して継続営業できます。",pending=true,can_continue=Simulation.PatronContinueReason()=="",reason=Simulation.PatronContinueReason()};
            if(!Simulation.GoalWaiting)return null;
            var g=s.goal.Last();bool final=s.goal.Count==Simulation.Rules.goal_stages.Length;
            return new PythonEnding {status=g.status,title=g.status=="expired"?"人気目標の期限を迎えました":final?"人気目標をクリアしました":"人気目標を達成しました",details=g.status=="expired"?"期限内未達。結果を確認して継続営業できます。":final?"最終段階を達成しました。クリア時の成果を記録しました。猫や資金を引き継いで継続営業できます。":"結果を確認して継続営業、または次の段階へ挑戦できます。",pending=true,can_continue=Simulation.GoalResponseReason("continue_goal")=="",reason=Simulation.GoalResponseReason("continue_goal")};
        }
    }
}
