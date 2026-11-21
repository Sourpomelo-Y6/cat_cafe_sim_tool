using System.Linq;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        static void SkipResult(CafeConversationProgress p,int length)
        {
            p.node=length;
            p.page_offset=0;
            p.completed=true;
            p.viewed_branches=p.viewed_branches.Concat(new[]{p.branch_id}).Distinct().ToArray();
        }

        void CompleteAutoplayConversations()
        {
            SyncConversations();
            var s=Simulation.State;
            if(UnfinishedResult(conversation))SkipResult(conversation,CafeIntakeConversation.Lines(conversation.branch_id,"").Length);
            if(UnfinishedResult(regularConversation))SkipResult(regularConversation,CafeRegularConversation.Lines(regularConversation.branch_id,"").Length);
            foreach(var r in equipmentConversations.Where(r=>UnfinishedResult(r.progress)))
                SkipResult(r.progress,CafeEquipmentDialogue.Lines(r,r.progress.branch_id).Length);
            foreach(var r in adoptionConversations.Where(CafeAdoptionDialogue.ResultPending))
                SkipResult(r.progress,CafeAdoptionDialogue.Lines(r.progress.branch_id,"","",s.adoption.events.First(e=>e.id==r.event_id)).Length);
            foreach(var r in encounterConversations.Where(CafeEncounterDialogue.ResultPending))
                SkipResult(r.progress,CafeEncounterDialogue.Lines(r.progress.branch_id,"",r.progress.conversation_id).Length);
            foreach(var r in dispatchIntroductionConversations.Where(CafeDispatchIntroductionDialogue.ResultPending))
                SkipResult(r.progress,CafeDispatchIntroductionDialogue.Lines(r.progress.branch_id,"").Length);
            foreach(var r in dispatchConversations){
                if(UnfinishedResult(r.departure))SkipResult(r.departure,CafeDispatchDialogue.Lines("departure",r.departure.branch_id,"",r.Destination).Length);
                if(UnfinishedResult(r.returning))SkipResult(r.returning,CafeDispatchDialogue.Lines("return",r.returning.branch_id,"",r.Destination).Length);
            }
            foreach(var r in lodgeConversations){
                if(UnfinishedResult(r.response))SkipResult(r.response,CafeLodgeDialogue.Lines(false,r.response.branch_id,"").Length);
                if(UnfinishedResult(r.returning))SkipResult(r.returning,CafeLodgeDialogue.Lines(true,r.returning.branch_id,"").Length);
            }
            foreach(var r in reservationConversations){
                if(UnfinishedResult(r.response))SkipResult(r.response,CafeReservationDialogue.Lines(r,false,r.response.branch_id).Length);
                if(UnfinishedResult(r.result))SkipResult(r.result,CafeReservationDialogue.Lines(r,true,r.result.branch_id).Length);
            }
            foreach(var r in trustConversations){
                if(UnfinishedResult(r.response))SkipResult(r.response,CafeTrustDialogue.Lines(false,r.response.branch_id,"",0).Length);
                if(UnfinishedResult(r.result))SkipResult(r.result,CafeTrustDialogue.Lines(true,r.result.branch_id,"",0).Length);
            }
            foreach(var e in s.missing_cases.Where(e=>UnfinishedResult(e.result)))
                SkipResult(e.result,CafeMissingDialogue.Lines(e,"result","").Length);
            if(!CafeOptions.Current.skipAutoplayEnding)return;
            if(PopularityEndingPending)SkipResult(PopularityEnding.progress,CafePopularityEnding.Lines(CafePopularityEnding.Record(s),"").Length);
            if(BondEndingPending)SkipResult(BondEnding.progress,CafeBondEnding.Lines(CafeBondEnding.Record(s),"").Length);
            if(PatronEndingPending)SkipResult(PatronEnding.progress,CafePatronEnding.Lines(CafePatronEnding.Record(s),"").Length);
            if(OutcomePending)SkipResult(Outcome.progress,Outcome.Lines().Length);
        }
        private PythonAutoPlay AutoplayView()
        {
            var s=Simulation.State;var r=s.autoplay;string reason=Simulation.AutoStartReason();bool running=r?.running==true;
            return new PythonAutoPlay {running=running,paused=r?.paused==true,can_start=!running && reason=="",reason=reason,notes="基礎営業・安定経営・積極経営 / 1〜10日。成長は営業後に猫に合う内容で自動確定します。猫適性の選択は目標達成を保証しません。専門確定後は変更できません。手動で育成する場合は、おまかせを中止して通常営業後に選んでください。イベントは自動で回答します。好感度イベントでは最初の選択肢を選びます。保存すると一時停止し、読み込み後は明示的に再開します。中止後は元の割り当て設定へ戻ります。",objectives=new[]{"popularity","bond","patron"}.Where(t=>Simulation.AutoTargetStatus(t)!=null).Select(t=>new PythonAutoPlayOption {choice=t,label=t=="popularity"?"人気":t=="bond"?"好感度":"有力者",status=Simulation.AutoTargetStatus(t),can_start=!running && reason=="" && Simulation.AutoTargetStatus(t)=="active"}).ToArray(),modes=new[]{new PythonAutoPlayOption {choice="basic",label="基礎営業"},new PythonAutoPlayOption {choice="clear",label="安定経営"},new PythonAutoPlayOption {choice="fast",label="積極経営"}},report=r==null?null:new PythonAutoPlayReport {version=r.version,run_id=r.run_id,running=r.running,objective=r.objective,mode=r.mode,limit=r.limit,days=r.days,operations=r.operations,reason=r.reason,message=r.message,summary=r.summary,lines=r.lines.ToArray(),decisions=r.decisions.Select(d=>new PythonAutoPlayDecision {day=d.day,target=d.target,choice=d.choice,reason=d.reason,subject=d.subject,status=d.status}).ToArray()}};
        }
    }
}
