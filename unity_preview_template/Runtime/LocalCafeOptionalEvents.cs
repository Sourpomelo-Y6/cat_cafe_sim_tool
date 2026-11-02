using System;
using System.Linq;
using CatCafePreview.Standalone;

namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        CafeOptionalEvent[] optionalEvents=new CafeOptionalEvent[0];
        CafeOptionalEventCatalog optionalCatalog;
        public CafeOptionalDefinition OptionalDefinition=>optionalCatalog?.events.FirstOrDefault();
        public string CurrentOptionalConversationId=>optionalCatalog==null?CafeOptionalEvents.MikeId+".v1":OptionalDefinition==null?null:OptionalDefinition.event_id+".v"+OptionalDefinition.revision;
        public string OptionalTitle=>OptionalEvent?.title??OptionalDefinition?.title??CafeOptionalEvents.MikeTitle;
        public string OptionalContentId=>OptionalEvent?.content_cat_id??OptionalDefinition?.content_cat_id??Details.ContentId("cat-mike");
        public void SetOptionalCatalog(CafeOptionalEventCatalog catalog){catalog?.Validate();optionalCatalog=catalog?.Copy();}
        public CafeReplayDefinition[] OptionalReplayDefinitions=>CafeReplayCatalog.Definitions(Replays.Load(),optionalCatalog);
        void LoadOptionalCatalog()
        {
            try{SetOptionalCatalog(CafeOptionalEventResources.Load());}
            catch(Exception ex){optionalCatalog=new CafeOptionalEventCatalog{pack_id=new string('0',32),events=new CafeOptionalDefinition[0]};UnityEngine.Debug.LogWarning("任意イベントの新規開始を停止しました："+ex.Message);}
        }
        public bool OptionalEventOpen {get;set;}
        public CafeOptionalEvent[] OptionalEvents {get{SyncConversations();return optionalEvents;}}
        public CafeOptionalEvent OptionalEvent=>OptionalEvents.FirstOrDefault();
        public bool OptionalEventListed=>OptionalEvent!=null?!OptionalEvent.progress.completed:optionalCatalog==null?CafeOptionalEvents.Eligible(Simulation.State):OptionalDefinition?.Eligible(Simulation.State)==true;
        public string OptionalEventReason
        {
            get
            {
                if(!OptionalEventListed)return "開始条件を満たしていません。";
                if(Simulation.State.phase!="preparation")return "営業準備で確認してください。";
                if(!string.IsNullOrEmpty(Simulation.State.game_over))return "営業を終了しています。";
                if(OpeningPending || OutcomePending || PatronEndingPending || BondEndingPending || PopularityEndingPending
                    || EquipmentResultPending || IntakeResultPending || RegularResultPending || DispatchResultPending || EncounterResultPending
                    || AdoptionResultPending || TrustBlocksProgress || ReservationBlocksProgress || MissingBlocksProgress || LodgeResultPending || DispatchIntroductionResultPending
                    || !string.IsNullOrEmpty(View().required_action))return "必須の回答・結果会話を先に確認してください。";
                return "";
            }
        }
        // No simulation command/reward: revalidate the current instance and revision at explicit start.
        public bool BeginOptionalEvent(string instance,int revision,string imageResource)
        {
            if(instance!=Simulation.State.instance_id || revision!=Simulation.State.revision || OptionalEventReason!="")return false;
            if(OptionalEvent==null)optionalEvents=new[]{OptionalDefinition==null?CafeOptionalEvents.Start(Simulation.State,Simulation.Definition("cat-mike").name,Details.ContentId("cat-mike"),imageResource):OptionalDefinition.Start(Simulation.State,Simulation.Definition("cat-mike").name,imageResource)};
            return true;
        }
        void RestoreOptionalEvents(CafeOptionalEvent[] rows)
        {
            optionalEvents=rows??new CafeOptionalEvent[0];OptionalEventOpen=false;
        }
    }
}
