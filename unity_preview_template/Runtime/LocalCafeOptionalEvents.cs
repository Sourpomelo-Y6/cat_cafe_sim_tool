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
        public void SetOptionalCatalog(CafeOptionalEventCatalog catalog){catalog?.Validate();optionalCatalog=catalog?.Copy();SyncMikeMilestone();}
        public CafeReplayDefinition[] OptionalReplayDefinitions=>CafeReplayCatalog.Definitions(Replays.Load(),optionalCatalog);
        void LoadOptionalCatalog()
        {
            try{SetOptionalCatalog(CafeOptionalEventResources.Load());}
            catch(Exception ex){SetOptionalCatalog(new CafeOptionalEventCatalog{pack_id=new string('0',32),events=new CafeOptionalDefinition[0]});UnityEngine.Debug.LogWarning("任意イベントの新規開始を停止しました："+ex.Message);}
        }
        public bool OptionalEventOpen {get;set;}
        public CafeOptionalEvent[] OptionalEvents {get{SyncConversations();return optionalEvents;}}
        public CafeOptionalEvent OptionalEvent=>selectedOptional??optionalEvents.FirstOrDefault(r=>!r.progress.completed)??optionalEvents.FirstOrDefault();
        CafeOptionalDefinition DefinitionFor(string cat)
        {if(optionalCatalog==null || optionalCatalog.events.Length==0)return null;string content=Details.ContentId(cat);return optionalCatalog.events.FirstOrDefault(d=>d.Matches(cat,content));}
        void SyncMikeMilestone()
        {
            // Only the existing Mike story participates in the prototype.
            var definition = optionalCatalog?.events.FirstOrDefault(d => d.event_id == CafeOptionalEvents.MikeId);
            bool enabled = optionalCatalog == null || (definition?.min_affinity == CafeOptionalEvents.MikeAffinityMilestone
                && definition.Matches(CafeOptionalEvents.MikeCatId, Details.ContentId(CafeOptionalEvents.MikeCatId)));
            var receipt = optionalEvents.FirstOrDefault(r => r.cat_id == CafeOptionalEvents.MikeCatId && r.event_id == CafeOptionalEvents.MikeId);
            Simulation.MikeMilestoneLocked = receipt != null ? !receipt.progress.completed
                : enabled && optionalEvents.Length < CafeOptionalEvents.MaxRecords;
        }
        public OptionalEventCandidate[] OptionalCandidates
        {
            get
            {
                var rows=new List<OptionalEventCandidate>();
                foreach(var r in optionalEvents.Where(r=>!r.progress.completed))rows.Add(new OptionalEventCandidate{cat_id=r.cat_id,event_id=r.event_id,revision=r.revision,title=r.title,cat_name=r.cat_name,content_cat_id=r.content_cat_id,resume=true,affinity_milestone=r.cat_id==CafeOptionalEvents.MikeCatId && r.event_id==CafeOptionalEvents.MikeId && Simulation.PlayerAtAffinityMilestone(r.cat_id)});
                foreach(var c in Simulation.State.cats)
                {
                    var d=DefinitionFor(c.id);string id=d?.event_id??CafeOptionalEvents.MikeId;
                    bool eligible=optionalCatalog==null?c.id=="cat-mike" && CafeOptionalEvents.Eligible(Simulation.State):d?.Eligible(Simulation.State,c.id)==true;
                    bool milestone = c.activity != "adopted" && id == CafeOptionalEvents.MikeId && Simulation.PlayerAtAffinityMilestone(c.id);
                    if((!eligible && !milestone) || optionalEvents.Any(r=>r.cat_id==c.id && r.event_id==id))continue;
                    rows.Add(new OptionalEventCandidate{cat_id=c.id,event_id=id,revision=d?.revision??"1",title=d?.title??CafeOptionalEvents.MikeTitle,cat_name=Simulation.Definition(c.id).name,content_cat_id=d?.content_cat_id??Details.ContentId(c.id),affinity_milestone=milestone});
                }
                return rows.ToArray();
            }
        }
        public OptionalEventCandidate OptionalForCat(string cat)=>OptionalCandidates.FirstOrDefault(r=>r.cat_id==cat);
        public bool OptionalEventListed=>OptionalCandidates.Length!=0;
        public string OptionalEventReason=>OptionalCandidates.Length==0?"開始条件を満たしていません。":OptionalReason(OptionalCandidates[0]);
        public string OptionalReason(OptionalEventCandidate candidate)
        {
            if(candidate==null)return "開始条件を満たしていません。";
            if(Simulation.State.phase!="preparation")return "営業準備で確認してください。";
            if(!string.IsNullOrEmpty(Simulation.State.game_over))return "営業を終了しています。";
            if(!candidate.resume && candidate.affinity_milestone)
            {
                int day = DefinitionFor(candidate.cat_id)?.min_day ?? 2;
                if(Simulation.State.day < day)return day+"日目以降に読めます。";
                if(!Simulation.State.cats.Any(c=>c.id==candidate.cat_id && c.activity=="cafe" && c.health=="healthy"))
                    return "ミケが健康で在店しているときに読めます。";
            }
            if(Simulation.PlayerActive || (Simulation.State.autoplay?.running==true && !Simulation.State.autoplay.paused))return "交流・自動進行を止めてから確認してください。";
            if(OpeningPending || OutcomePending || PatronEndingPending || BondEndingPending || PopularityEndingPending
                || EquipmentResultPending || IntakeResultPending || RegularResultPending || DispatchResultPending || EncounterResultPending
                || AdoptionResultPending || TrustBlocksProgress || ReservationBlocksProgress || MissingBlocksProgress || LodgeResultPending || DispatchIntroductionResultPending
                || (Simulation.AdoptionWaiting || Simulation.PlayerActive || Simulation.BondWaiting || Simulation.DispatchTroubleWaiting || Simulation.DispatchChoiceWaiting || Simulation.DispatchWaiting || MissingWaiting || Simulation.ReservationWaiting || Simulation.PatronWaiting || Simulation.GoalWaiting || Simulation.DispatchIntroductionWaiting || Simulation.IntakeWaiting || Simulation.RegularIntroductionWaiting || Simulation.CustomerTrustWaiting || Simulation.StoreEventWaiting || Simulation.GrowthWaiting || Simulation.AssignmentWaiting))return "必須の回答・結果会話を先に確認してください。";
            if(!candidate.resume && optionalEvents.Any(r=>!r.progress.completed))return "進行中の個別イベントを先に完了してください。";
            if(!candidate.resume && optionalEvents.Length>=CafeOptionalEvents.MaxRecords)return "個別イベントの保存上限512件に達しています。";
            return "";
        }
        public bool BeginOptionalEvent(string instance,int revision,string imageResource)
        {var c=OptionalCandidates.FirstOrDefault();return c!=null && BeginOptionalEvent(c.cat_id,c.event_id,c.revision,instance,revision,imageResource);}
        public bool BeginOptionalEvent(string cat,string eventId,string contentRevision,string instance,int revision,string imageResource)
        {
            if(instance!=Simulation.State.instance_id || revision!=Simulation.State.revision)return false;
            var candidate=OptionalCandidates.FirstOrDefault(c=>c.cat_id==cat && c.event_id==eventId && c.revision==contentRevision);
            if(candidate==null || OptionalReason(candidate)!="")return false;
            var existing=optionalEvents.FirstOrDefault(r=>r.cat_id==cat && r.event_id==eventId);
            if(existing!=null){selectedOptional=existing;return !existing.progress.completed;}
            var d=DefinitionFor(cat);
            var created=d==null?CafeOptionalEvents.Start(Simulation.State,Simulation.Definition(cat).name,Details.ContentId(cat),imageResource):d.Start(Simulation.State,cat,Simulation.Definition(cat).name,imageResource);
            if(created.artwork==null && created.event_id==CafeOptionalEvents.MikeId && created.cat_id==CafeOptionalEvents.MikeCatId
                && (d==null || d.target_kind!="content"))
                created.scene_art=CafeOptionalEvents.MikeWindowArt;
            var next=optionalEvents.Concat(new[]{created}).ToArray();CafeOptionalEvents.Validate(next);optionalEvents=next;selectedOptional=created;return true;
        }
        void RestoreOptionalEvents(CafeOptionalEvent[] rows)
        {CafeOptionalEvents.Validate(rows);optionalEvents=rows??new CafeOptionalEvent[0];selectedOptional=null;OptionalEventOpen=false;SyncMikeMilestone();}
        public void FinishOptionalEvent(CafeOptionalEvent row)
        {
            CafeOptionalEvents.Finish(row);
            SyncMikeMilestone();
        }
    }
}
