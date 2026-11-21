using System;
using System.Linq;
using System.Collections.Generic;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed class OptionalEventCandidate
    {
        public string cat_id,event_id,revision,title,cat_name,content_cat_id;
        public bool resume, affinity_milestone;
        public int milestone;
    }
    public sealed partial class LocalCafeBackend
    {
        CafeOptionalEvent[] optionalEvents=new CafeOptionalEvent[0];
        CafeOptionalEventCatalog optionalCatalog;
        CafeOptionalEvent selectedOptional;
        public CafeOptionalDefinition OptionalDefinition=>optionalCatalog?.events.FirstOrDefault();
        public string CurrentOptionalConversationId=>optionalCatalog==null?CafeOptionalEvents.MikeId+".v1":OptionalDefinition==null?null:OptionalDefinition.event_id+".v"+OptionalDefinition.revision;
        public bool IsCurrentOptionalConversation(string id)=>optionalCatalog==null?id==CafeOptionalEvents.MikeId+".v1":optionalCatalog.events.Any(d=>id==d.event_id+".v"+d.revision);
        public string OptionalTitle=>OptionalEvent?.title??OptionalDefinition?.title??CafeOptionalEvents.MikeTitle;
        public string OptionalContentId=>OptionalEvent?.content_cat_id??OptionalDefinition?.content_cat_id??Details.ContentId("cat-mike");
        public void SetOptionalCatalog(CafeOptionalEventCatalog catalog){catalog?.Validate();optionalCatalog=catalog?.Copy();SyncAffinityMilestones();}
        public CafeReplayDefinition[] OptionalReplayDefinitions=>CafeReplayCatalog.Definitions(Replays.Load(),optionalCatalog);
        void LoadOptionalCatalog()
        {
            try{SetOptionalCatalog(CafeOptionalEventResources.Load());}
            catch(Exception ex){SetOptionalCatalog(new CafeOptionalEventCatalog{pack_id=new string('0',32),events=new CafeOptionalDefinition[0]});UnityEngine.Debug.LogWarning("任意イベントの新規開始を停止しました："+ex.Message);}
        }
        public bool OptionalEventOpen {get;set;}
        public CafeOptionalEvent[] OptionalEvents {get{SyncConversations();return optionalEvents;}}
        public CafeOptionalEvent OptionalEvent=>selectedOptional??optionalEvents.FirstOrDefault(r=>!r.progress.completed)??optionalEvents.FirstOrDefault();
        IEnumerable<CafeOptionalDefinition> DefinitionsFor(string cat)
        {
            if(optionalCatalog==null || optionalCatalog.events.Length==0)return Enumerable.Empty<CafeOptionalDefinition>();
            string content=Details.ContentId(cat);
            return optionalCatalog.events.Where(d=>d.Matches(cat,content));
        }
        CafeOptionalDefinition DefinitionFor(string cat,string id)=>DefinitionsFor(cat).FirstOrDefault(d=>d.event_id==id);
        CafeOptionalEvent ReceiptFor(string cat,string id,int milestone)=>optionalEvents.FirstOrDefault(
            r=>r.cat_id==cat && (r.event_id==id || milestone>0 && r.Milestone()==milestone));
        void SyncAffinityMilestones()
        {
            var caps=new List<CafeAffinityCap>();
            var state=Simulation.State;
            var historical=state.player_sessions.GroupBy(s=>s.cat_id).ToDictionary(g=>g.Key,g=>g.Max(s=>s.initial_affinity));
            bool canStart=optionalEvents.Length<CafeOptionalEvents.MaxRecords;
            foreach(var cat in state.cats.Where(c=>c.activity!="adopted"))
            {
                double current=Simulation.PlayerCat(cat.id).affinity;
                double reached=historical.TryGetValue(cat.id,out var prior)?Math.Max(prior,current):current;
                int limit=0;
                foreach(var d in DefinitionsFor(cat.id).Where(d=>d.Milestone()>0 && d.Milestone()<100
                    && (d.milestone>0 || cat.id==CafeOptionalEvents.MikeCatId)).OrderBy(d=>d.Milestone()))
                {
                    var receipt=ReceiptFor(cat.id,d.event_id,d.Milestone());
                    if(reached<=d.Milestone() && (receipt!=null?!receipt.progress.completed:canStart)){limit=d.Milestone();break;}
                }
                if(optionalCatalog==null && cat.id==CafeOptionalEvents.MikeCatId && reached<=20)
                {
                    var receipt=ReceiptFor(cat.id,CafeOptionalEvents.MikeId,20);
                    if(receipt!=null?!receipt.progress.completed:canStart)limit=20;
                }
                if(limit>0)caps.Add(new CafeAffinityCap{cat_id=cat.id,affinity=limit});
            }
            Simulation.AffinityCaps=caps.ToArray();
        }
        public OptionalEventCandidate[] OptionalCandidates
        {
            get
            {
                var rows=new List<OptionalEventCandidate>();
                foreach(var r in optionalEvents.Where(r=>!r.progress.completed))
                    rows.Add(new OptionalEventCandidate{cat_id=r.cat_id,event_id=r.event_id,revision=r.revision,title=r.title,cat_name=r.cat_name,content_cat_id=r.content_cat_id,resume=true,milestone=r.Milestone(),affinity_milestone=AtCap(r.cat_id,r.Milestone())});
                foreach(var c in Simulation.State.cats)
                {
                    foreach(var d in DefinitionsFor(c.id).OrderBy(d=>d.Milestone()))
                    {
                        bool reached=d.Milestone()>0 && c.activity!="adopted" && Simulation.PlayerCat(c.id).affinity>=d.Milestone();
                        if((!reached && !d.Eligible(Simulation.State,c.id)) || ReceiptFor(c.id,d.event_id,d.Milestone())!=null)continue;
                        rows.Add(new OptionalEventCandidate{cat_id=c.id,event_id=d.event_id,revision=d.revision,title=d.title,cat_name=Simulation.Definition(c.id).name,content_cat_id=d.content_cat_id,milestone=d.Milestone(),affinity_milestone=AtCap(c.id,d.Milestone())});
                    }
                    if(optionalCatalog==null && c.id==CafeOptionalEvents.MikeCatId && c.activity!="adopted"
                        && Simulation.PlayerCat(c.id).affinity>=20 && ReceiptFor(c.id,CafeOptionalEvents.MikeId,20)==null)
                        rows.Add(new OptionalEventCandidate{cat_id=c.id,event_id=CafeOptionalEvents.MikeId,revision="1",title=CafeOptionalEvents.MikeTitle,cat_name=Simulation.Definition(c.id).name,content_cat_id=Details.ContentId(c.id),milestone=20,affinity_milestone=AtCap(c.id,20)});
                }
                return rows.ToArray();
            }
        }
        bool AtCap(string cat,int milestone)=>Simulation.AffinityCaps.Any(cap=>cap.cat_id==cat && cap.affinity==milestone)
            && Simulation.PlayerAtAffinityMilestone(cat);
        public OptionalEventCandidate OptionalForCat(string cat)=>OptionalCandidates.FirstOrDefault(r=>r.cat_id==cat);
        public bool OptionalEventListed=>OptionalCandidates.Length!=0;
        public string OptionalEventReason=>OptionalCandidates.Length==0?"開始条件を満たしていません。":OptionalReason(OptionalCandidates[0]);
        public string OptionalReason(OptionalEventCandidate candidate)=>OptionalReason(candidate,false);
        string OptionalReason(OptionalEventCandidate candidate,bool automatic)
        {
            if(candidate==null)return "開始条件を満たしていません。";
            if(Simulation.State.phase!="preparation")return "営業準備で確認してください。";
            if(!string.IsNullOrEmpty(Simulation.State.game_over))return "営業を終了しています。";
            if(!candidate.resume && candidate.milestone>0)
            {
                int day = DefinitionFor(candidate.cat_id,candidate.event_id)?.min_day ?? 2;
                if(Simulation.State.day < day)return day+"日目以降に読めます。";
                if(!Simulation.State.cats.Any(c=>c.id==candidate.cat_id && c.activity=="cafe" && c.health=="healthy"))
                    return "猫が健康で在店しているときに読めます。";
            }
            if(Simulation.PlayerActive || (!automatic && Simulation.State.autoplay?.running==true && !Simulation.State.autoplay.paused))return "交流・自動進行を止めてから確認してください。";
            if(OpeningPending || OutcomePending || PatronEndingPending || BondEndingPending || PopularityEndingPending
                || EquipmentResultPending || IntakeResultPending || RegularResultPending || DispatchResultPending || EncounterResultPending
                || AdoptionResultPending || TrustBlocksProgress || ReservationBlocksProgress || MissingBlocksProgress || LodgeResultPending || DispatchIntroductionResultPending
                || (Simulation.AdoptionWaiting || Simulation.PlayerActive || Simulation.BondWaiting || Simulation.DispatchTroubleWaiting || Simulation.DispatchChoiceWaiting || Simulation.DispatchWaiting || MissingWaiting || Simulation.ReservationWaiting || Simulation.PatronWaiting || Simulation.GoalWaiting || Simulation.DispatchIntroductionWaiting || Simulation.IntakeWaiting || Simulation.RegularIntroductionWaiting || Simulation.CustomerTrustWaiting || Simulation.StoreEventWaiting || Simulation.GrowthWaiting || Simulation.AssignmentWaiting))return "必須の回答・結果会話を先に確認してください。";
            if(!candidate.resume && candidate.milestone==0 && optionalEvents.Any(r=>r.Milestone()==0 && !r.progress.completed))return "進行中の個別イベントを先に完了してください。";
            if(!candidate.resume && optionalEvents.Length>=CafeOptionalEvents.MaxRecords)return "個別イベントの保存上限512件に達しています。";
            return "";
        }
        public bool BeginOptionalEvent(string instance,int revision,string imageResource)
        {var c=OptionalCandidates.FirstOrDefault();return c!=null && BeginOptionalEvent(c.cat_id,c.event_id,c.revision,instance,revision,imageResource);}
        public bool BeginOptionalEvent(string cat,string eventId,string contentRevision,string instance,int revision,string imageResource,bool automatic=false)
        {
            if(instance!=Simulation.State.instance_id || revision!=Simulation.State.revision)return false;
            var candidate=OptionalCandidates.FirstOrDefault(c=>c.cat_id==cat && c.event_id==eventId && c.revision==contentRevision);
            if(candidate==null || OptionalReason(candidate,automatic)!="")return false;
            var existing=optionalEvents.FirstOrDefault(r=>r.cat_id==cat && r.event_id==eventId);
            if(existing!=null){selectedOptional=existing;return !existing.progress.completed;}
            var d=DefinitionFor(cat,eventId);
            var created=d==null?CafeOptionalEvents.Start(Simulation.State,Simulation.Definition(cat).name,Details.ContentId(cat),imageResource):d.Start(Simulation.State,cat,Simulation.Definition(cat).name,imageResource);
            if(created.artwork==null && created.event_id==CafeOptionalEvents.MikeId && created.cat_id==CafeOptionalEvents.MikeCatId
                && (d==null || d.target_kind!="content"))
                created.scene_art=CafeOptionalEvents.MikeWindowArt;
            var next=optionalEvents.Concat(new[]{created}).ToArray();CafeOptionalEvents.Validate(next);optionalEvents=next;selectedOptional=created;return true;
        }
        void CompleteAutoplayMilestones()
        {
            if(Simulation.PlayerActive || Simulation.State.phase!="preparation")return;
            foreach(var candidate in OptionalCandidates.Where(c=>c.milestone>0))
            {
                if(OptionalReason(candidate,true)!="")continue;
                var s=Simulation.State;
                if(!BeginOptionalEvent(candidate.cat_id,candidate.event_id,candidate.revision,s.instance_id,s.revision,OptionalEventConversationPanel.NormalImage(candidate.content_cat_id),true))continue;
                var row=OptionalEvent;
                var p=row.progress;
                if(p.branch_id=="introduction"){
                    p.node=row.introduction.Length;
                    CafeOptionalEvents.Choose(row,true);
                }
                p.node=CafeOptionalEvents.Lines(row,p.branch_id).Length;
                FinishOptionalEvent(row);
            }
        }
        void RestoreOptionalEvents(CafeOptionalEvent[] rows)
        {CafeOptionalEvents.Validate(rows);optionalEvents=rows??new CafeOptionalEvent[0];selectedOptional=null;OptionalEventOpen=false;SyncAffinityMilestones();}
        public void FinishOptionalEvent(CafeOptionalEvent row)
        {
            CafeOptionalEvents.Finish(row);
            SyncAffinityMilestones();
        }
    }
}
