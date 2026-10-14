using System;
using System.Linq;
using System.Collections.Generic;
namespace CatCafePreview.Standalone
{
    [Serializable] public class PlayerRules {public int daily_turns,set_ticks;public double max_affinity,relief,stress;}
    [Serializable] public class BondRules {public int target;public double affinity;}
    [Serializable] public class CafePlayerCat {public string cat_id;public double affinity;public int today,total;}
    [Serializable] public class CafePlayerAction {public string action,target;}
    [Serializable] public class CafePlayerSession
    {
        public string id,cat_id;public int day,ordinal,initial_revision,set_ticks;
        public double max_stamina; public int stamina_rule_version;
        public double initial_stamina,initial_affinity,stress_before,stress_after;
        public bool manual;public CafeExchange exchange;public List<CafePlayerAction> actions=new List<CafePlayerAction>();
    }
    [Serializable] public class CafeBondGoal {public string id,status="active";public int started_day=1,resolved_day;public bool continued;public List<string> achieved_cats=new List<string>();}
    public sealed partial class CafeSimulation
    {
        // Save/replay policy v2: missing per-session limits are legacy ten-turn sessions.
        public const int PlayerSetTicks = 20;
        internal bool LegacyPlayerReplay;
        public int PlayerSessionTicks(CafePlayerSession session)
        {
            if(session.set_ticks!=0 && session.set_ticks!=10 && session.set_ticks!=PlayerSetTicks)throw new ArgumentException("Invalid player session limit");
            return session.set_ticks==0?Rules.player.set_ticks:session.set_ticks;
        }
        public static readonly string[] PlayerActions={"direct","adapt","intense","feint","pause","switch","connect"};
        public string PlayerChoiceReason(string action)
        {
            if(action!="switch")return PlayerActionReason(action);
            var reasons=Rules.types.Where(t=>ActivePlayer==null || t.id!=ActivePlayer.exchange.mode).Select(t=>PlayerActionReason("switch",t.id)).ToArray();
            return reasons.Contains("")?"":reasons.FirstOrDefault()??"切り替え先がありません。";
        }
        // Rebuild presentation from saved actions so older saves also retain every reaction.
        public string PlayerHistory(CafePlayerSession session)
        {
            var e=new CafeExchange {cat_id=session.cat_id,customer_id="player",max_stamina=session.max_stamina,remaining=PlayerSessionTicks(session),stamina=session.initial_stamina,affinity_start=session.initial_affinity};
            var rows=new List<string>();
            foreach(var a in session.actions)
            {
                double affinity=e.affinity_pending;int opened=e.open_up_count;
                StepExchange(e,Definition(session.cat_id),a.action,a.target);
                string reaction=e.open_up_count>opened?"open_up":a.action=="connect"?"received":e.previous_reaction;
                double delta=e.affinity_pending-affinity;
                rows.Add((rows.Count+1)+"："+PlayerActionName(a.action)+(a.target==null?"":" → "+PlayerTypeName(a.target))+" / "+PlayerReactionName(reaction)+"\n体力 "+CafeDetailPresentation.Number(e.stamina)+" / "+CafeDetailPresentation.Number(session.max_stamina==0?Rules.max_stamina:session.max_stamina)+" / 好感度 "+(delta>=0?"+":"")+CafeDetailPresentation.Number(delta));
            }
            return string.Join("\n\n",rows);
        }
        public string PlayerEventImageKey(CafePlayerSession session)
        {
            if(session.actions.Count==0)return "";
            var previous=new CafePlayerSession {max_stamina=session.max_stamina,stamina_rule_version=session.stamina_rule_version,set_ticks=session.set_ticks,cat_id=session.cat_id,initial_stamina=session.initial_stamina,initial_affinity=session.initial_affinity,actions=session.actions.Take(session.actions.Count-1).ToList()};
            var before=ReplayPlayer(State,previous);var after=ReplayPlayer(State,new CafePlayerSession {max_stamina=session.max_stamina,stamina_rule_version=session.stamina_rule_version,set_ticks=session.set_ticks,cat_id=session.cat_id,initial_stamina=session.initial_stamina,initial_affinity=session.initial_affinity,actions=session.actions});
            return after.open_up_count==before.open_up_count?"":after.mode+"__"+(after.connect_count>before.connect_count?"simultaneous":"open_up");
        }
        public static string PlayerActionName(string action)=>action=="direct"?"素直に動かす":action=="adapt"?"相手に合わせる":action=="intense"?"激しく動かす":action=="feint"?"フェイントを入れる":action=="pause"?"動きを止めて待つ":action=="connect"?"心をつかむ":"動作を切り替える";
        public static string PlayerTypeName(string mode)=>mode=="teaser"?"ねこじゃらし":mode=="ball"?"ボール遊び":mode=="plush"?"ぬいぐるみ遊び":mode=="tunnel"?"トンネル遊び":mode=="pet"?"なでる":mode=="brush"?"ブラッシング":mode=="voice"?"声をかける":"そばで見守る";
        public static string PlayerReactionName(string reaction)=>reaction=="turn_away"?"そっぽを向く":reaction=="listless"?"けだるそうにする":reaction=="confused"?"戸惑う":reaction=="enthusiastic"?"熱心に応じる":reaction=="favorable"?"好意的に応じる":reaction=="open_up"?"心を開く":reaction=="received"?"特別な働きかけを受け止める":"普通に応じる";
        public static string PlayerEndName(string reason)=>reason=="manual"?"自分で切り上げました":reason=="exhausted"?"体力がなくなり、交流終了":reason=="success"?"交流成功！":reason=="time_limit"?"交流時間が終了しました":reason=="timeout"?"時間になり、交流終了":"";
        public CafePlayerSession ActivePlayer=>State.player_sessions.LastOrDefault(e=>e.exchange.end_reason==null);
        public bool PlayerActive=>ActivePlayer!=null;
        public bool BondWaiting=>State.game_over==null && State.bond_goal?.status=="cleared" && !State.bond_goal.continued;
        public int PlayerRemaining=>Rules.player.daily_turns-State.player_cats.Sum(c=>c.today);
        public CafePlayerCat PlayerCat(string id)=>State.player_cats.Single(c=>c.cat_id==id);
        private void EnsurePlayerCat(string id){if(!State.player_cats.Any(c=>c.cat_id==id))State.player_cats.Add(new CafePlayerCat {cat_id=id});}
        public string PlayerBeginReason(string id)
        {
            if(AdoptionWaiting)return "譲渡の申し出に回答してください。";
            if(PlayerActive)return "進行中の交流を再開または終了してください。";
            if(BondWaiting)return "好感度目標の結果を確認してください。";
            string reason=ItemsReason();if(reason!="")return reason;
            var c=State.cats.SingleOrDefault(c=>c.id==id);if(c==null || c.activity!="cafe")return "在店している猫を選んでください。";
            if(c.health!="healthy" || c.cannot_continue || c.stamina<=0)return "療養中・体力切れの猫は遊べません。";
            return PlayerRemaining<=0?"本日の交流回数を使い切りました。":"";
        }
        private CafeExchange ReplayPlayer(CafeSnapshot s,CafePlayerSession session)
        {
            var e=new CafeExchange {cat_id=session.cat_id,customer_id="player",max_stamina=session.max_stamina,remaining=PlayerSessionTicks(session),stamina=session.initial_stamina,affinity_start=session.initial_affinity};
            foreach(var a in session.actions)StepExchange(e,Definitions(s).Single(c=>c.id==session.cat_id),a.action,a.target);
            if(session.manual){if(e.end_reason!=null)throw new ArgumentException("Manual finish after automatic end");e.end_reason="manual";e.connect_pending=e.open_up_pending=false;}
            return e;
        }
        public string PlayerActionReason(string action,string target=null)
        {
            if(State.game_over!=null)return "経営終了後は交流できません。";var session=ActivePlayer;if(session==null)return "交流を開始してください。";
            try{var e=ReplayPlayer(State,session);StepExchange(e,Definition(session.cat_id),action,target);return "";}catch(ArgumentException){return "現在選べる交流操作と対象を確認してください。";}
        }
        private void BeginPlayer(string id)
        {
            string reason=PlayerBeginReason(id);if(reason!="")throw new InvalidOperationException(reason);var c=State.cats.Single(c=>c.id==id);var bond=PlayerCat(id);
            var session=new CafePlayerSession {max_stamina=State.stamina_growth==null?0:MaxStamina(id),stamina_rule_version=State.stamina_growth==null?0:1,set_ticks=LegacyPlayerReplay?0:PlayerSetTicks,id="player-"+State.day+"-"+(State.player_cats.Sum(r=>r.today)+1),cat_id=id,day=State.day,ordinal=State.player_cats.Sum(r=>r.today)+1,initial_revision=bond.total,initial_affinity=bond.affinity,initial_stamina=c.stamina};
            session.exchange=ReplayPlayer(State,session);bond.today++;bond.total++;State.player_sessions.Add(session);State.revision++;
        }
        private void AdvancePlayer(string action,string target,bool finish)
        {
            var session=ActivePlayer;if(session==null){if(finish)return;throw new InvalidOperationException("交流を開始してください。");}
            var e=ReplayPlayer(State,session);string replayBefore=ReplayBefore(e);int replayOpened=e.open_up_count;
            if(finish){e.end_reason="manual";e.connect_pending=e.open_up_pending=false;}else StepExchange(e,Definition(session.cat_id),action,target);
            RecordReplayAction(e,finish?"finish":action,target,replayBefore,replayOpened);
            if(finish)session.manual=true;else session.actions.Add(new CafePlayerAction {action=action,target=target});session.exchange=e;
            var cat=State.cats.Single(c=>c.id==session.cat_id);cat.stamina=e.stamina;if(cat.stamina==0)cat.cannot_continue=true;
            if(e.end_reason!=null)
            {
                PlayerCat(cat.id).affinity=Clamp(e.affinity_start+e.affinity_pending,Rules.player.max_affinity);session.stress_before=cat.stress;
                cat.stress=Clamp(cat.stress+(e.affinity_pending>0?-Rules.player.relief:e.affinity_pending<0?Rules.player.stress:0),100);session.stress_after=cat.stress;EvaluateBond();
            }
            State.revision++;
        }
        public string[] BondQualifying(CafeSnapshot s)=>s.player_cats.Where(c=>s.cats.Any(cat=>cat.id==c.cat_id && cat.activity!="adopted") && c.affinity>=Rules.bond.affinity).Select(c=>c.cat_id).OrderBy(id=>id,StringComparer.Ordinal).ToArray();
        private void EvaluateBond()
        {
            var goal=State.bond_goal;if(goal==null || goal.status!="active" || State.game_over!=null)return;var cats=BondQualifying(State);if(cats.Length<Rules.bond.target)return;
            goal.status="cleared";goal.resolved_day=State.day;goal.achieved_cats=cats.ToList();State.clear_results.Add(new CafeClearRecord {mode="bond",day=State.day,target=Rules.bond.target,value=cats.Length,funds=State.funds,popularity=State.popularity,cats=ActiveCatCount(State),revenue=State.history.Sum(h=>h.revenue)+State.visits.Sum(v=>v.bill)});
        }
        public string BondContinueReason()
        {
            if(!BondWaiting || AdoptionWaiting || DispatchWaiting || DispatchChoiceWaiting || DispatchTroubleWaiting || DispatchIntroductionWaiting || CustomerTrustWaiting || ReservationWaiting || State.cats.Any(c=>c.activity=="missing" && c.missing_remaining==0 && !DispatchMissing(State,c.id)))return "確認待ちの好感度目標がないか、先に帰還・イベントの確認が必要です。";
            return "";
        }
        private void ContinueBond(){string reason=BondContinueReason();if(reason!="")throw new InvalidOperationException(reason);State.bond_goal.continued=true;State.revision++;}
        private void ValidatePlayer(CafeSnapshot s)
        {
            if(s.player_cats==null || s.player_sessions==null || !s.player_cats.Select(c=>c?.cat_id).OrderBy(id=>id).SequenceEqual(s.cats.Select(c=>c.id).OrderBy(id=>id)) || s.player_sessions.Select(e=>e?.id).Distinct().Count()!=s.player_sessions.Count)throw new ArgumentException("Invalid player roster/history");
            var scores=s.cats.ToDictionary(c=>c.id,c=>0.0);var totals=s.cats.ToDictionary(c=>c.id,c=>0);var today=s.cats.ToDictionary(c=>c.id,c=>0);int active=0,previous=0;
            foreach(var session in s.player_sessions)
            {
                if(session==null || (session.set_ticks!=0 && session.set_ticks!=10 && session.set_ticks!=PlayerSetTicks) || !scores.ContainsKey(session.cat_id) || session.day<JoinedDay(s,session.cat_id) || session.day>s.day || session.day<previous || session.ordinal!=s.player_sessions.TakeWhile(e=>e!=session).Count(e=>e.day==session.day)+1 || session.ordinal>Rules.player.daily_turns || session.id!="player-"+session.day+"-"+session.ordinal || session.initial_revision!=totals[session.cat_id] || session.initial_affinity!=scores[session.cat_id] || !Range(session.initial_stamina,double.Epsilon,session.max_stamina==0?Rules.max_stamina:session.max_stamina) || (session.stamina_rule_version==0?session.max_stamina!=0:session.stamina_rule_version!=1 || !Range(session.max_stamina,1,10000) || session.max_stamina>MaxStamina(s,session.cat_id)) || session.actions==null || session.exchange==null)throw new ArgumentException("Invalid player departure");
                previous=session.day;var expected=ReplayPlayer(s,session);var e=session.exchange;
                // Check the entire public exchange, including counters, pending reactions and disabled growth/equipment modifiers.
                foreach(var field in typeof(CafeExchange).GetFields())
                {
                    var actual=field.GetValue(e);var value=field.GetValue(expected);
                    if(actual is Array a && value is Array b){if(!a.Cast<object>().SequenceEqual(b.Cast<object>()))throw new ArgumentException("Player exchange differs from actions");}
                    else if(!object.Equals(actual,value))throw new ArgumentException("Player exchange differs from actions: "+field.Name);
                }
                totals[session.cat_id]++;if(session.day==s.day)today[session.cat_id]++;
                if(e.end_reason==null)
                {
                    active++;var c=s.cats.Single(c=>c.id==session.cat_id);
                    if(session!=s.player_sessions.Last() || s.phase!="preparation" || session.day!=s.day || c.activity!="cafe" || c.health!="healthy" || c.cannot_continue || c.stamina!=e.stamina || session.stress_before!=0 || session.stress_after!=0 || s.game_over!=null)throw new ArgumentException("Invalid active player exchange");
                }
                else
                {
                    scores[session.cat_id]=Clamp(e.affinity_start+e.affinity_pending,Rules.player.max_affinity);
                    if(!Range(session.stress_before,0,100) || session.stress_after!=Clamp(session.stress_before+(e.affinity_pending>0?-Rules.player.relief:e.affinity_pending<0?Rules.player.stress:0),100))throw new ArgumentException("Invalid player stress effect");
                }
            }
            if(active>1 || s.player_cats.Any(c=>c.affinity!=scores[c.cat_id] || c.total!=totals[c.cat_id] || c.today!=today[c.cat_id]))throw new ArgumentException("Player affinity differs from history");
            var goal=s.bond_goal;if(s.objective=="bond" && goal==null)throw new ArgumentException("Invalid bond mode");if(goal==null)return;
            if(goal.id!="bond" || goal.started_day<1 || goal.started_day>s.day || s.objective=="bond" && goal.started_day!=1 || goal.achieved_cats==null || goal.achieved_cats.Distinct().Count()!=goal.achieved_cats.Count || goal.achieved_cats.Any(id=>!scores.ContainsKey(id)))throw new ArgumentException("Invalid bond goal");
            if(goal.status=="active")
            {if(goal.resolved_day!=0 || goal.continued || goal.achieved_cats.Count!=0 || s.game_over==null && BondQualifying(s).Length>=Rules.bond.target)throw new ArgumentException("Invalid active bond goal");}
            else if(goal.status!="cleared" || goal.resolved_day<goal.started_day || goal.resolved_day>s.day || goal.achieved_cats.Count<Rules.bond.target || !goal.continued && s.game_over==null && (s.day!=goal.resolved_day || s.phase!="preparation" || !goal.achieved_cats.All(id=>scores[id]>=Rules.bond.affinity)))throw new ArgumentException("Invalid completed bond goal");
        }
    }
}
