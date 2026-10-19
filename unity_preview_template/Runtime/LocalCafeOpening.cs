using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        CafeOpeningProgress opening,newGameOpening;string openingEpoch;
        public CafeOpeningProgress Opening=>openingEpoch==Simulation.State.instance_id?opening:null;
        public bool OpeningPending=>Opening!=null && !Opening.progress.completed;
        void RestoreOpening(CafeOpeningProgress row){opening=row;openingEpoch=Simulation.State.instance_id;}
        public void FinishOpening(bool skip){if(!OpeningPending)return;CafeOpening.Finish(Opening,skip?"skipped":"viewed");RememberViewedConversations();}
    }
}
