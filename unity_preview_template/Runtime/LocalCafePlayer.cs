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
        private static string PlayerActionLabel(string action)=>Standalone.CafeSimulation.PlayerActionName(action);
        private PythonPlayerInteraction PlayerView()
        {
            var s=Simulation.State;var session=Simulation.ActivePlayer??s.player_sessions.LastOrDefault();if(session==null)return null;var e=session.exchange;bool active=e.end_reason==null;
            return new PythonPlayerInteraction {session_id=session.id,cat_id=session.cat_id,content_cat_id=session.cat_id=="cat-mike" && Narratives.Catalog!=null?Narratives.Catalog.ContentId(Details.ContentId(session.cat_id)):Details.ContentId(session.cat_id),narrative_image=Narratives.LatestPlayer(session.id)?.image_resource,name=Simulation.Definition(session.cat_id).name,active=active,can_finish=active && s.game_over==null,remaining_ticks=e.remaining,remaining_sets=Simulation.PlayerRemaining,mode=Standalone.CafeSimulation.PlayerTypeName(e.mode),scene_key=session.actions.LastOrDefault()?.action=="pause"?"pause":e.mode,image_key=session.actions.Count==0?e.mode:e.mode+"__"+session.actions.Last().action,event_image_key=Simulation.PlayerEventImageKey(session),affinity_before=(float)e.affinity_start,affinity_after=(float)Math.Max(0,Math.Min(100,e.affinity_start+e.affinity_pending)),affinity_delta=(float)(Math.Max(0,Math.Min(100,e.affinity_start+e.affinity_pending))-e.affinity_start),stamina_before=(float)session.initial_stamina,stamina=(float)e.stamina,max_stamina=(float)(session.max_stamina==0?Simulation.Rules.max_stamina:session.max_stamina),engagement=(float)e.engagement,tension=(float)e.tension,reason=Standalone.CafeSimulation.PlayerEndName(e.end_reason),history=(session.cat_id=="cat-mike"?Narratives.Read(session.id)+"\n\n── 数値履歴 ──\n\n":"")+Simulation.PlayerHistory(session),actions=Standalone.CafeSimulation.PlayerActions.Select(action=>new PythonEventChoice {choice=action,label=PlayerActionLabel(action),can_select=active && Simulation.PlayerChoiceReason(action)=="",reason=Simulation.PlayerChoiceReason(action)}).ToArray(),targets=Simulation.Rules.types.Where(type=>type.id!=e.mode).Select(type=>new PythonEventChoice {choice=type.id,label=Standalone.CafeSimulation.PlayerTypeName(type.id),can_select=active && Simulation.PlayerActionReason("switch",type.id)=="",reason=Simulation.PlayerActionReason("switch",type.id)}).ToArray()};
        }
    }
}
