using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonPresentation : MonoBehaviour
    {
        public Text title, summary;
        public void Apply(PythonCafeState state)
        {
            title.text="猫カフェ　"+state.day+"日目";
            summary.text="資金 "+state.funds.ToString("0.#")+"　｜　"+(state.game_over!=null ? "経営終了" : state.closed ? "閉店" : state.can_set_shifts ? "営業準備" : "営業中　"+state.tick+" / "+state.opening_ticks);
        }
        public void Disconnect() { summary.text="未接続　｜　Pythonへの再接続を待っています"; }
    }
}
