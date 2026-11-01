using System;
using System.Linq;
using CatCafePreview.Standalone;

namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        CafeOptionalEvent[] optionalEvents=new CafeOptionalEvent[0];
        public bool OptionalEventOpen {get;set;}
        public CafeOptionalEvent[] OptionalEvents {get{SyncConversations();return optionalEvents;}}
        public CafeOptionalEvent OptionalEvent=>OptionalEvents.FirstOrDefault();
        public bool OptionalEventListed=>OptionalEvent!=null?!OptionalEvent.progress.completed:CafeOptionalEvents.Eligible(Simulation.State);
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
            if(OptionalEvent==null)optionalEvents=new[]{CafeOptionalEvents.Start(Simulation.State,Simulation.Definition("cat-mike").name,Details.ContentId("cat-mike"),imageResource)};
            return true;
        }
        void RestoreOptionalEvents(CafeOptionalEvent[] rows)
        {
            optionalEvents=rows??new CafeOptionalEvent[0];OptionalEventOpen=false;
        }
    }
}
