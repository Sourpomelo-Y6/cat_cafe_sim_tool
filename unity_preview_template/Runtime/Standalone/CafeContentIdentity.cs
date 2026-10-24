using System;
using System.Linq;
namespace CatCafePreview.Standalone
{
    public sealed partial class CafeSimulation
    {
        // Image provenance follows saved admission data, never a fixed rescue ID or name.
        public string ContentImageId(string id)
        {
            var intro=State.dispatches.FirstOrDefault(e=>e.introduction?.cat_id==id);
            if(intro!=null)return "intro-"+intro.destination;
            var candidates=RecruitmentCandidates(State);int index=Array.FindIndex(candidates,r=>r.cat.id==id);
            return index<0?id:"catalog-"+(index%Rules.recruitment_catalog.Length+1);
        }
    }
}
