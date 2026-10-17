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
        public bool ConversationBlocksProgress=>ConversationOpen || RegularConversationOpen || UnfinishedResult(Conversation) || UnfinishedResult(RegularConversation);
        void SyncConversations()
        {
            var s=Simulation.State;
            if(conversationEpoch!=s.instance_id){conversation=regularConversation=null;ConversationOpen=RegularConversationOpen=false;conversationEpoch=s.instance_id;}
            CafeIntakeConversation.SyncAnswer(conversation,s.intake_request.status);
            CafeRegularConversation.SyncAnswer(regularConversation,s.regular_introduction.status);
        }
        public CafeConversationProgress Conversation {get{SyncConversations();return conversation;}}
        public CafeConversationProgress RegularConversation {get{SyncConversations();return regularConversation;}}
        public void BeginIntakeConversation(){var current=Conversation;if(current==null && Simulation.IntakeWaiting)conversation=CafeIntakeConversation.Start();}
        public void BeginRegularConversation(){var current=RegularConversation;if(current==null && Simulation.RegularIntroductionWaiting)regularConversation=CafeRegularConversation.Start();}
        private void RestoreConversation(CafeConversationProgress value,CafeConversationProgress regular){conversation=value;regularConversation=regular;conversationEpoch=Simulation.State.instance_id;ConversationOpen=RegularConversationOpen=false;}
    }
}
