using System.Linq;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        CafeEndingProgress popularityEnding;string endingEpoch;
        public CafeEndingProgress PopularityEnding
        {
            get
            {
                var s=Simulation.State;if(endingEpoch!=s.instance_id){popularityEnding=null;endingEpoch=s.instance_id;}
                if(popularityEnding==null && CafePopularityEnding.Waiting(s))popularityEnding=new CafeEndingProgress{cat_id=s.cats.FirstOrDefault(c=>c.activity=="cafe")?.id??"",progress=new CafeConversationProgress{conversation_id=CafePopularityEnding.Id,branch_id=CafePopularityEnding.Branch,viewed_branches=new string[0]}};
                return popularityEnding;
            }
        }
        public bool PopularityEndingPending=>PopularityEnding!=null && !PopularityEnding.progress.completed;
        void RestorePopularityEnding(CafeEndingProgress row){popularityEnding=row;endingEpoch=Simulation.State.instance_id;}
    }
}
