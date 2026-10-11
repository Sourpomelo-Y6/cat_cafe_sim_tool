using System;
using System.Linq;
namespace CatCafePreview
{
    [Serializable] public class PythonBondGoal {public string status,details,reason;public bool pending,can_continue;}
    public sealed partial class LocalCafeBackend
    {
        private PythonBondGoal BondGoalView()
        {
            var s=Simulation.State;var goal=s.bond_goal;if(goal==null)return null;var r=Simulation.Rules.bond;
            return new PythonBondGoal {status=goal.status,pending=Simulation.BondWaiting,can_continue=Simulation.BondContinueReason()=="",reason=Simulation.BondContinueReason(),details="好感度 "+Number(r.affinity)+"以上："+Simulation.BondQualifying(s).Length+" / "+r.target+"匹\n"+(goal.continued?"クリア・継続中":goal.status=="cleared"?"クリア・結果確認待ち":"挑戦中（期限なし）")};
        }
        private static string PlayerActionLabel(string action)=>action=="direct"?"交流する":action=="adapt"?"反応に合わせる":action=="intense"?"積極的に交流":action=="feint"?"誘いを変える":action=="pause"?"少し休憩":action=="connect"?"心を通わせる":"交流の種類を変える";
        private PythonPlayerInteraction PlayerView()
        {
            var s=Simulation.State;var session=Simulation.ActivePlayer??s.player_sessions.LastOrDefault();if(session==null)return null;var e=session.exchange;bool active=e.end_reason==null;
            return new PythonPlayerInteraction {session_id=session.id,cat_id=session.cat_id,content_cat_id=session.cat_id,name=Simulation.Definition(session.cat_id).name,active=active,can_finish=active && s.game_over==null,remaining_ticks=e.remaining,remaining_sets=Simulation.PlayerRemaining,mode=e.mode,affinity_before=(float)e.affinity_start,affinity_after=(float)Math.Max(0,Math.Min(100,e.affinity_start+e.affinity_pending)),affinity_delta=(float)(Math.Max(0,Math.Min(100,e.affinity_start+e.affinity_pending))-e.affinity_start),stamina_before=(float)session.initial_stamina,stamina=(float)e.stamina,engagement=(float)e.engagement,tension=(float)e.tension,reason=e.end_reason,history=string.Join(" / ",session.actions.Select(a=>PlayerActionLabel(a.action)+(a.target==null?"":"（"+a.target+"）"))),actions=new[]{"direct","adapt","intense","feint","pause","connect"}.Select(action=>new PythonEventChoice {choice=action,label=PlayerActionLabel(action),can_select=active && Simulation.PlayerActionReason(action)=="",reason=Simulation.PlayerActionReason(action)}).ToArray(),targets=Simulation.Rules.types.Select(type=>new PythonEventChoice {choice=type.id,label=type.id=="teaser"?"猫じゃらし":type.id=="ball"?"ボール":type.id=="plush"?"ぬいぐるみ":type.id=="tunnel"?"トンネル":type.id=="pet"?"撫でる":type.id=="brush"?"ブラシ":type.id=="voice"?"声をかける":"そばにいる",can_select=active && Simulation.PlayerActionReason("switch",type.id)=="",reason=Simulation.PlayerActionReason("switch",type.id)}).ToArray()};
        }
    }
}
