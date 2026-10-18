using System;
using System.Collections.Generic;
using System.Linq;
namespace CatCafePreview.Standalone
{
    [Serializable] public class CafeAutoOperation
    {
        public string kind,cat_id,choice,source,seat_id;
        public string[] working;
    }
    [Serializable] public class CafeAutoDecision
    {
        public int day;public string target,choice,reason,subject,status="実行済み";
    }
    [Serializable] public class CafeAutoRun
    {
        public int version=1,limit,start_days,days,operations;
        public string run_id,objective,mode,reason="",message="",summary="";
        public bool running,paused,restore_auto_assignment;
        public double before_funds,before_popularity;
        public List<CafeAutoDecision> decisions=new List<CafeAutoDecision>();
        public List<string> lines=new List<string>();
    }
    public sealed partial class CafeSimulation
    {
        public void StartAutoplay(string objective,string mode,int days)=>TraceImported(new CafeReplayOperation {kind="start_autoplay",objective=objective,mode=mode,days=days},()=>StartAutoplayCore(objective,mode,days));
        public void PauseAutoplay()=>TraceImported(new CafeReplayOperation {kind="pause_autoplay"},PauseAutoplayCore);
        public void ResumeAutoplay()=>TraceImported(new CafeReplayOperation {kind="resume_autoplay"},ResumeAutoplayCore);
        public void CancelAutoplay()=>TraceImported(new CafeReplayOperation {kind="stop_autoplay"},CancelAutoplayCore);
        public CafeAutoOperation StepAutoplay()=>TraceImported(new CafeReplayOperation {kind="step_autoplay"},StepAutoplayCore);
        public string AutoTargetStatus(string target)=>target=="popularity"?State.goal.LastOrDefault()?.status:target=="bond"?State.bond_goal?.status:target=="patron"?State.patron?.status:null;
        public string AutoStartReason()
        {
            if(State.game_over!=null || GoalWaiting || BondWaiting || PatronWaiting)return "経営終了・目標結果を確認してください。";
            if(!new[]{"popularity","bond","patron"}.Any(t=>AutoTargetStatus(t)=="active"))return "挑戦中の目標が必要です。";
            if(PlayerActive || AdoptionWaiting || DispatchWaiting || DispatchChoiceWaiting || DispatchTroubleWaiting || DispatchIntroductionWaiting || ReservationWaiting || IntakeWaiting || RegularIntroductionWaiting || CustomerTrustWaiting || GrowthWaiting || StoreEventWaiting || State.cats.Any(c=>c.activity=="missing" && c.missing_remaining==0))return "回答待ちのイベント・交流を先に確認してください。";
            return "";
        }
        private void StartAutoplayCore(string objective,string mode,int days)
        {
            if(State.autoplay?.running==true)throw new InvalidOperationException("おまかせは実行中です。");
            string problem=AutoStartReason();if(problem!="")throw new InvalidOperationException(problem);
            if(AutoTargetStatus(objective)!="active" || !new[]{"basic","clear","fast"}.Contains(mode) || days<1 || days>10)throw new ArgumentException("挑戦中の目標、方針、1〜10日を指定してください。");
            State.autoplay=new CafeAutoRun {run_id=Guid.NewGuid().ToString("N"),objective=objective,mode=mode,limit=days,start_days=AutoDayCount(),running=true,restore_auto_assignment=State.auto_assignment,before_funds=State.funds,before_popularity=State.popularity};
            State.auto_assignment=true;UpdateAutoSummary();State.revision++;
        }
        private void UpdateAutoSummary(){var r=State.autoplay;r.summary="資金 "+r.before_funds+" → "+State.funds+" / 人気 "+r.before_popularity+" → "+State.popularity+"\n"+string.Join("\n",State.cats.Select(c=>Definition(c.id).name+"：疲労 "+c.fatigue+" / ストレス "+c.stress+" / "+c.health+" / "+c.activity));}
        private int AutoDayCount()=>State.history.Count+(State.phase=="closed"?1:0);
        private void PauseAutoplayCore(){if(State.autoplay?.running==true){State.autoplay.paused=true;State.revision++;}}
        private void ResumeAutoplayCore(){if(State.autoplay?.running!=true || !State.autoplay.paused)throw new InvalidOperationException("保存・一時停止したおまかせはありません。");if(GrowthWaiting)throw new InvalidOperationException("猫の成長を選択してからおまかせを再開してください。");State.autoplay.paused=false;State.auto_assignment=true;State.revision++;}
        private void StopAutoplay(string reason,string message="")
        {
            var run=State.autoplay;if(run?.running!=true)throw new InvalidOperationException("実行中のおまかせはありません。");
            run.running=false;run.paused=false;run.reason=reason;run.message=message;run.days=AutoDayCount()-run.start_days;State.auto_assignment=run.restore_auto_assignment;UpdateAutoSummary();State.revision++;
        }
        private void CancelAutoplayCore()=>StopAutoplay("cancelled");
        private CafeAutoOperation StepAutoplayCore()
        {
            var run=State.autoplay;if(run?.running!=true || run.paused)throw new InvalidOperationException("おまかせを開始・再開してください。");
            string terminal=State.game_over!=null?"game_over":State.goal.LastOrDefault()?.status=="expired" && (run.objective=="popularity" || !State.goal.Last().continued)?"expired":AutoTargetStatus(run.objective)=="cleared" && (run.objective!="popularity" || State.goal.Count==Rules.goal_stages.Length)?"completed":GoalWaiting || BondWaiting || PatronWaiting?"goal_cleared":AutoDayCount()-run.start_days>=run.limit?"day_limit":run.operations>=10000?"operation_limit":null;
            if(terminal!=null){StopAutoplay(terminal);return null;}
            // HTTP Python autoplay yields to the user for external pending answers.
            if(!PlayerActive && AutoStartReason()!=""){StopAutoplay("blocked",AutoStartReason());return null;}
            var op=PlanAutoplay(run.objective,run.mode);
            try{Execute(op.kind,op.working,op.cat_id,op.choice,op.source,op.seat_id);run.operations++;run.days=AutoDayCount()-run.start_days;run.lines.Add(State.day+":"+op.kind+":"+op.cat_id+":"+op.choice);UpdateAutoSummary();}
            catch(InvalidOperationException e){StopAutoplay("blocked",e.Message);}
            return op;
        }
        private CafeAutoOperation AutoOp(string kind,string cat=null,string choice=null,string source=null,string seat=null,string[] workers=null)=>new CafeAutoOperation {kind=kind,cat_id=cat,choice=choice,source=source,seat_id=seat,working=workers};
        private void AutoDecision(string subject,string choice,string reason)
        {
            var run=State.autoplay;if(run==null)return;var row=new CafeAutoDecision {day=State.day,target=subject,subject=subject,choice=choice,reason=reason};int i=run.decisions.FindIndex(r=>r.day==row.day && r.subject==subject);if(i<0)run.decisions.Add(row);else run.decisions[i]=row;
        }
        public CafeAutoOperation PlanAutoplay(string objective,string mode)
        {
            if(PlayerActive)return objective=="bond" && mode!="basic"?PlanAutoPlayer(mode=="clear"):AutoOp("player_finish");
            if(State.phase=="closed")return AutoOp("next_day");
            if(State.phase!="preparation")return AutoOp("advance_business");
            if(mode!="basic" && objective=="bond")
            {
                if(ActiveCatCount(State)<Rules.bond.target && ActiveCatCount(State)<HousingCapacity(State,State.day))
                {
                    if(State.recruitment==null)return AutoOp("open_recruitment");
                    var recruit=RecruitmentCandidates(State).OrderBy(r=>r.cat.id,StringComparer.Ordinal).FirstOrDefault(r=>RecruitReason(r.cat.id)=="" && State.funds-r.cost>=AutoReserve());if(recruit!=null)return AutoOp("recruit_cat",recruit.cat.id);
                }
                var eligible=State.player_cats.Where(c=>c.affinity<Rules.bond.affinity && PlayerBeginReason(c.cat_id)=="").OrderBy(c=>c.affinity).ThenBy(c=>c.cat_id,StringComparer.Ordinal).FirstOrDefault();if(eligible!=null)return AutoOp("player_begin",eligible.cat_id);
            }
            if(mode!="basic" && objective=="patron")
            {
                var dispatch=PlanAutoPatron(mode);if(dispatch!=null)return dispatch;
            }
            if(mode=="basic")
            {
                var workers=State.cats.Where(c=>c.activity=="cafe" && c.health=="healthy" && c.fatigue<60 && c.stress<60).Select(c=>c.id).OrderBy(id=>id,StringComparer.Ordinal).ToArray();return AutoStaff(workers);
            }
            var staffing=AutoStaffing(objective=="patron"?"clear":mode,objective=="patron",mode=="fast");
            if(objective!="patron")
            {
                double remaining=State.goal.Count==0?double.PositiveInfinity:State.goal.Last().started_day+Rules.goal_stages[State.goal.Count-1].days-State.day;
                double buffer=mode=="fast" && objective=="popularity"?AutoWaitingReserve(WaitingAreaDailyCost(State,State.day)):AutoReserve();
                if(remaining>1 && RestSpaceReason("purchase_rest_space")=="" && State.funds-Rules.rest_space.cost>=buffer)return AutoOp("purchase_rest_space");
                if(mode=="fast" && objective=="popularity" && remaining>1)
                {
                    var departures=State.history.LastOrDefault()?.customer_records;
                    if(staffing.arrivals>Math.Min(State.seat_count,staffing.workers.Length) || departures?.Any(r=>r.departure=="wait_timeout" || r.departure=="queue_full")==true)
                    {string kind=State.waiting_area.purchased_day>0?"upgrade_waiting_area":"purchase_waiting_area";var rule=WaitingAreaRule(kind);if(WaitingAreaReason(kind)=="" && State.funds-rule.cost>=AutoWaitingReserve(rule.daily_cost))return AutoOp(kind);}
                }
                bool expand=mode=="fast" && staffing.arrivals>State.seat_count && staffing.healthy>State.seat_count || staffing.workers.Length>State.seat_count && staffing.peak>State.seat_count;
                if(expand && remaining>1 && ExpansionReason()=="" && State.funds-Rules.expansion_costs[State.seat_count-2]>=buffer)return AutoOp("expand_seats");
                if(remaining>1 && RestSpaceReason("upgrade_rest_space")=="" && State.funds-Rules.rest_space_upgrade.cost>=buffer)return AutoOp("upgrade_rest_space");
                var toy=Rules.equipment.Single(r=>r.id=="toys");for(int seat=0;seat<State.seat_count;seat++)if(Installed(seat)==null && EquipmentReason()=="" && State.funds-toy.cost>=buffer)return AutoOp("purchase_seat_equipment",choice:"toys",seat:"seat-"+(seat+1));
                if(remaining>1 && RestSpaceReason("soundproof_rest_space")=="" && State.funds-Rules.rest_space_soundproof.cost>=buffer)return AutoOp("soundproof_rest_space");
            }
            var selected=staffing.workers;
            if(selected.Length==0 && State.reservation.status=="accepted" && State.reservation.visit_day==State.day && objective!="patron")selected=State.cats.Where(c=>c.activity=="cafe" && c.health=="healthy").OrderBy(c=>c.fatigue+c.stress).ThenBy(c=>c.id,StringComparer.Ordinal).Take(1).Select(c=>c.id).ToArray();
            return AutoStaff(selected);
        }
        private CafeAutoOperation AutoStaff(string[] workers)
        {
            foreach(var cat in State.cats)AutoDecision(cat.id,workers.Contains(cat.id)?"出勤":"休養", "疲労 "+cat.fatigue+" / ストレス "+cat.stress);
            if(workers.Length==0 && !(State.reservation.status=="accepted" && State.reservation.visit_day==State.day))return AutoOp("day_off");
            if(!State.cats.Where(c=>c.working).Select(c=>c.id).OrderBy(id=>id,StringComparer.Ordinal).SequenceEqual(workers.OrderBy(id=>id,StringComparer.Ordinal)))return AutoOp("set_shifts",workers:workers);
            return AutoOp("start_business");
        }
        private double AutoReserve()=>Math.Max(300,2*(Rules.base_cost+Rules.per_seat_cost*(State.seat_count+1)));
        private double AutoWaitingReserve(double cost)=>Math.Max(AutoReserve(),2*(Rules.base_cost+Rules.per_seat_cost*(State.seat_count+1)+cost));
        private sealed class AutoStaffPlan {public int arrivals,peak,healthy;public string[] workers;}
        private AutoStaffPlan AutoStaffing(string mode,bool patron,bool fast)
        {
            var arrivals=CustomerSchedule(State.day);int duration=Rules.exchange.ticks,workload=arrivals.Sum(a=>Math.Min(duration,Rules.opening_ticks-a.tick));int peak=arrivals.Select(a=>arrivals.Count(b=>b.tick<=a.tick && a.tick<Math.Min(Rules.opening_ticks,b.tick+duration))).DefaultIfEmpty(0).Max();
            string[] trainees,reserved;AutoPatronRoles(out trainees,out reserved);
            if(patron && fast)reserved=reserved.Concat(PatronDestinations().Where(d=>IsPatronMember(d.id) && State.patron.members.Single(s=>s.id==d.id).value<Rules.patron.members.Single(m=>m.id==d.id).target).Select(d=>AutoMatchingWait(d,trainees)).Where(id=>id!=null)).Distinct().ToArray();
            var healthy=State.cats.Where(c=>c.activity=="cafe" && c.health=="healthy" && c.stamina>0 && !c.cannot_continue).OrderBy(c=>patron && trainees.Contains(c.id)?0:1).ThenBy(c=>c.fatigue+c.stress).ThenBy(c=>c.id,StringComparer.Ordinal).ToArray();
            int desired=Math.Min(healthy.Length,Math.Max(peak,(int)Math.Ceiling(workload/20.0))),actions=Math.Max(20,(int)Math.Ceiling(Math.Min(workload,State.seat_count*Rules.opening_ticks)/(double)Math.Max(1,desired)));var workers=new List<string>();
            foreach(var cat in healthy)
            {
                int individual=mode=="clear"?Math.Max(20,Math.Min(Rules.opening_ticks,workload)):actions;
                if(mode=="fast")individual=Math.Max(actions,Math.Max(Math.Min(Rules.opening_ticks,arrivals.Where(a=>Matched(cat,a.customer_id)).Sum(a=>Math.Min(duration,Rules.opening_ticks-a.tick))),Math.Min(Rules.opening_ticks,State.history.LastOrDefault()?.legacy_service_ticks?.SingleOrDefault(r=>r.cat_id==cat.id)?.ticks??State.history.LastOrDefault()?.customer_records.Where(r=>r.cat_id==cat.id).Sum(r=>r.service_ticks)??0)));
                var definition=Definition(cat.id);double fatigue=Math.Min(Rules.max_fatigue,cat.fatigue+individual*Rules.fatigue_per_service_tick*definition.service_fatigue),stress=Math.Min(100,cat.stress+individual*Rules.stress_per_service_tick*definition.service_stress);
                AutoDecision(cat.id,"予測", "接客 "+individual+"手 / 疲労 "+fatigue+" / ストレス "+stress);
                if(!(patron && reserved.Contains(cat.id)) && fatigue<=(mode=="fast"?65:60) && stress<=60 && workers.Count<desired)workers.Add(cat.id);
            }
            return new AutoStaffPlan {arrivals=arrivals.Length,peak=peak,healthy=healthy.Length,workers=workers.ToArray()};
        }
        private void AutoPatronRoles(out string[] trainees,out string[] reserved)
        {
            trainees=reserved=new string[0];if(State.patron==null)return;var strict=Rules.patron.members.FirstOrDefault(r=>r.id=="patron_strict_visit");if(strict==null || State.patron.members.Single(r=>r.id==strict.id).value>=strict.target)return;
            var white=State.cats.Where(c=>c.activity!="adopted" && Definition(c.id).features.Contains("white")).OrderBy(c=>c.id,StringComparer.Ordinal).ToArray();var trained=white.Where(c=>GrowthCat(c.id).specialization=="service").OrderBy(c=>c.activity=="cafe"?0:1).ThenBy(c=>c.health=="healthy"?0:1).ThenBy(c=>c.fatigue+c.stress).ThenBy(c=>c.id,StringComparer.Ordinal).FirstOrDefault();
            if(trained!=null)reserved=new[]{trained.id};else trainees=white.Where(c=>GrowthCat(c.id).specialization==null).Take(1).Select(c=>c.id).ToArray();
        }
        private CafeAutoOperation PlanAutoPatron(string mode)
        {
            string[] trainees,reserved;AutoPatronRoles(out trainees,out reserved);
            if(mode=="fast" && ActiveCatCount(State)<HousingCapacity(State,State.day))
            {
                var needed=Rules.patron.members.Where(r=>State.patron.members.Single(s=>s.id==r.id).value<r.target).SelectMany(r=>r.conditions).Select(c=>c.feature).Where(f=>f!=null).Distinct().Except(State.cats.Where(c=>c.activity!="adopted").SelectMany(c=>Definition(c.id).features)).ToArray();
                if(needed.Length>0 && State.funds>AutoReserve()){if(State.recruitment==null)return AutoOp("open_recruitment");var recruit=RecruitmentCandidates(State).Where(r=>r.cat.features.Intersect(needed).Any() && RecruitReason(r.cat.id)=="" && State.funds-r.cost>=AutoReserve()).OrderBy(r=>r.cost).ThenBy(r=>r.cat.id,StringComparer.Ordinal).FirstOrDefault();if(recruit!=null)return AutoOp("recruit_cat",recruit.cat.id);}
            }
            if(State.dispatches.Any(e=>IsPatronDestination(e.destination) && e.status!="resolved"))return null;
            foreach(var destination in PatronDestinations())
            {
                double score=destination.id==Rules.patron.destination.id?State.patron.satisfaction:State.patron.members.Single(s=>s.id==destination.id).value,target=destination.id==Rules.patron.destination.id?Rules.patron.target:Rules.patron.members.Single(r=>r.id==destination.id).target;if(score>=target)continue;
                if(mode=="fast" && AutoMatchingWait(destination,trainees)!=null)continue;
                var cat=State.cats.Where(c=>DispatchReason(c.id,destination.id)=="" && !trainees.Contains(c.id) && c.stress<=60 && (PatronTerms(State,c.id,destination.id)?.gain??1)>0).OrderByDescending(c=>PatronTerms(State,c.id,destination.id)?.gain??0).ThenBy(c=>c.fatigue).ThenBy(c=>c.id,StringComparer.Ordinal).FirstOrDefault();if(cat!=null)return AutoOp("dispatch",cat.id,destination.id);
            }
            return null;
        }
        private string AutoMatchingWait(DispatchRules destination,string[] trainees)
        {
            var matching=State.cats.Where(c=>c.activity=="cafe" && !trainees.Contains(c.id) && PatronTerms(State,c.id,destination.id)?.matched==true && (string.IsNullOrEmpty(destination.required_trait) || Definition(c.id).trait_id==destination.required_trait)).ToArray();
            if(matching.Length==0 || matching.Any(c=>DispatchReason(c.id,destination.id)=="" && c.stress<=60))return null;
            return matching.OrderBy(c=>c.health=="healthy"?0:1).ThenBy(c=>c.fatigue).ThenBy(c=>c.stress).ThenBy(c=>c.id,StringComparer.Ordinal).First().id;
        }
        private void ValidateAutoplay(CafeSnapshot s)
        {
            var r=s.autoplay;if(r==null)return;
            if(r.version!=1 || string.IsNullOrEmpty(r.run_id) || !new[]{"basic","clear","fast"}.Contains(r.mode) || !new[]{"popularity","bond","patron"}.Contains(r.objective) || r.limit<1 || r.limit>10 || r.start_days<0 || r.start_days>s.history.Count+(s.phase=="closed"?1:0) || r.days<0 || r.running && r.days!=s.history.Count+(s.phase=="closed"?1:0)-r.start_days || r.days>r.limit || r.operations<0 || r.operations>10000 || !Finite(r.before_funds) || !Range(r.before_popularity,0,Rules.popularity.cap) || r.lines==null || r.lines.Count!=r.operations || r.decisions==null || r.decisions.Count>10000 || r.decisions.Any(d=>d==null || d.day<1 || d.day>s.day || d.reason==null || d.subject==null) || r.running && (!s.auto_assignment || r.reason!="") || !r.running && (r.paused || !new[]{"completed","expired","goal_cleared","game_over","day_limit","operation_limit","blocked","cancelled"}.Contains(r.reason)))throw new ArgumentException("Invalid autoplay state");
        }
    }
}
