using System.Linq;
namespace CatCafePreview.Standalone
{
    public sealed partial class CafeSimulation
    {
        // Presentation only. Read the live cat and the existing exchange threshold.
        public string PlayerStaminaGuidance(string id)
        {
            var cat=State.cats.Single(c=>c.id==id);
            string plan=cat.activity!="cafe"?"不在":cat.working?"出勤予定":"休養予定";
            string warning=cat.stamina<=0?"体力切れです。接客を続けられません。":cat.stamina<=Rules.exchange.low_stamina?"低体力です。接客中もけだるい反応になりやすい状態です。":"";
            return plan+(cat.working && warning!=""?"："+warning:"");
        }
    }
}
