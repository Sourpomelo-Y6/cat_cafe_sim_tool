using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        private CafeConversationProgress conversation;
        private string conversationEpoch;
        public bool ConversationOpen {get;set;}
        public bool ConversationBlocksProgress=>ConversationOpen || Conversation!=null && !Conversation.completed && Conversation.branch_id!="introduction";
        public CafeConversationProgress Conversation
        {
            get
            {
                var s=Simulation.State;
                if(conversationEpoch!=s.instance_id){conversation=null;ConversationOpen=false;conversationEpoch=s.instance_id;}
                CafeIntakeConversation.SyncAnswer(conversation,s.intake_request.status);
                return conversation;
            }
        }
        public void BeginIntakeConversation(){var current=Conversation;if(current==null && Simulation.IntakeWaiting)conversation=CafeIntakeConversation.Start();}
        private void RestoreConversation(CafeConversationProgress value){conversation=value;conversationEpoch=Simulation.State.instance_id;ConversationOpen=false;}
    }
}
