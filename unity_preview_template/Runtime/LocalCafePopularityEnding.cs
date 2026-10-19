using System.Linq;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend
    {
        CafeEndingProgress popularityEnding,bondEnding;string endingEpoch,bondEpoch;
        public CafeEndingProgress BondEnding { get {var s=Simulation.State;if(bondEpoch!=s.instance_id){bondEnding=null;bondEpoch=s.instance_id;}if(bondEnding==null && CafeBondEnding.Waiting(s))bondEnding=new CafeEndingProgress{cat_id=s.bond_goal.achieved_cats.FirstOrDefault(id=>s.cats.Any(c=>c.id==id))??"",progress=new CafeConversationProgress{conversation_id=CafeBondEnding.Id,branch_id=CafePopularityEnding.Branch,viewed_branches=new string[0]}};return bondEnding;} }
        public bool BondEndingPending=>BondEnding!=null && !BondEnding.progress.completed;
        void RestoreBondEnding(CafeEndingProgress row){bondEnding=row;bondEpoch=Simulation.State.instance_id;}
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
