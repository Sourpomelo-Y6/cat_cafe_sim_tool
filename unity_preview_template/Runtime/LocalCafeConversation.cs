using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private CafeConversationProgress conversation,regularConversation;
        private string conversationEpoch;
        public bool ConversationOpen {get;set;}
        public bool RegularConversationOpen {get;set;}
        static bool UnfinishedResult(CafeConversationProgress p)=>p!=null && !p.completed && p.branch_id!="introduction";
        public bool ConversationBlocksProgress=>EquipmentBlocksProgress || ReservationBlocksProgress || MissingBlocksProgress || TrustBlocksProgress || AdoptionConversationOpen || AdoptionResultPending || LodgeConversationOpen || LodgeResultPending || OutcomePending || OpeningPending || PatronEndingPending || BondEndingPending || PopularityEndingPending || ConversationOpen || RegularConversationOpen || DispatchConversationOpen || UnfinishedResult(Conversation) || UnfinishedResult(RegularConversation) || DispatchResultPending || EncounterConversationOpen || EncounterResultPending || DispatchIntroductionConversationOpen || DispatchIntroductionResultPending;
        public bool IntakeResultPending=>UnfinishedResult(Conversation);
        public bool RegularResultPending=>UnfinishedResult(RegularConversation);
        public string ConversationProgressReason
        {
            get
            {
                string reason=OpeningPending?"オープニングを最後まで読むか、スキップして営業準備へ進んでください。":"";
                if(IntakeResultPending)reason+="保護猫の受け入れ依頼は"+(Simulation.State.intake_request.status=="declined"?"見送り済み":"受け入れ済み")+"ですが、結果会話が未完了です。イベント一覧の「回答・会話待ち」から再開し、「会話を終える」まで進めてください。\n";
                if(RegularResultPending)reason+="常連からの猫紹介は"+(Simulation.State.regular_introduction.status=="declined"?"見送り済み":"受け入れ済み")+"ですが、結果会話が未完了です。イベント一覧の「回答・会話待ち」から再開し、「会話を終える」まで進めてください。\n";
                if(reason=="" && (ConversationOpen || RegularConversationOpen))reason="依頼・紹介の会話中です。回答後の会話を最後まで進めてください。閉じた会話はイベント一覧から再開できます。";
                if(DispatchConversationReason!="")reason+="\n"+DispatchConversationReason;
                if(AdoptionConversationReason!="")reason+="\n"+AdoptionConversationReason;
                if((TrustBlocksProgress || Simulation.CustomerTrustWaiting) && TrustConversationReason!="")reason+="\n"+TrustConversationReason;
                if(EquipmentConversationReason!="")reason+="\n"+EquipmentConversationReason;
                if(ReservationConversationReason!="")reason+="\n"+ReservationConversationReason;
                if(MissingConversationReason!="")reason+="\n"+MissingConversationReason;
                if(LodgeConversationReason!="")reason+="\n"+LodgeConversationReason;
                if(EncounterConversationReason!="")reason+="\n"+EncounterConversationReason;
                if(DispatchIntroductionConversationReason!="")reason+="\n"+DispatchIntroductionConversationReason;
                return reason.Trim();
            }
        }
        void SyncConversations()
        {
            var s=Simulation.State;
            if(conversationEpoch!=s.instance_id){conversation=regularConversation=null;missingSelected=missingPhase=null;MissingConversationOpen=false;RestoreDispatchConversations(null,null);RestoreEncounterConversations(null,null);RestoreLodgeConversations(null,null);RestoreAdoptionConversations(null,null);RestoreTrustConversations(null,null);RestoreCocoConversations(null);RestoreEquipmentConversations(null,null);RestoreReservationConversations(null,null);RestoreDispatchIntroductionConversations(null,null);ConversationOpen=RegularConversationOpen=false;conversationEpoch=s.instance_id;}
            CafeIntakeConversation.SyncAnswer(conversation,s.intake_request.status);
            CafeRegularConversation.SyncAnswer(regularConversation,s.regular_introduction.status);
            SyncDispatchConversations();SyncEncounterConversations();SyncLodgeConversations();SyncAdoptionConversations();SyncTrustConversations();SyncCocoConversations();SyncEquipmentConversations();SyncReservationConversations();SyncMissingConversations();SyncDispatchIntroductionConversations();
        }
        public CafeConversationProgress Conversation {get{SyncConversations();return conversation;}}
        public CafeConversationProgress RegularConversation {get{SyncConversations();return regularConversation;}}
        public void BeginIntakeConversation(){var current=Conversation;if(current==null && Simulation.IntakeWaiting)conversation=CafeIntakeConversation.Start();}
        public void BeginRegularConversation(){var current=RegularConversation;if(current==null && Simulation.RegularIntroductionWaiting)regularConversation=CafeRegularConversation.Start();}
        private void RestoreConversation(CafeConversationProgress value,CafeConversationProgress regular){conversation=value;regularConversation=regular;conversationEpoch=Simulation.State.instance_id;ConversationOpen=RegularConversationOpen=false;}
    }
}
