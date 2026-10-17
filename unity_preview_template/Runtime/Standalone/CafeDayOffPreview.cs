using System;
namespace CatCafePreview.Standalone
{
    public sealed partial class CafeSimulation
    {
        public CafeSnapshot PreviewDayOff(Func<CafeSnapshot,CafeSnapshot> clone)
        {
            var isolated=clone(State);
            if(isolated==null || ReferenceEquals(isolated,State))throw new ArgumentException("休業予測には独立した状態が必要です。");
            var preview=new CafeSimulation(Rules);preview.State=isolated;
            preview.Execute("day_off");return preview.State;
        }
    }
}
