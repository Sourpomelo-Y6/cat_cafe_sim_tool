using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview
{
    public class PythonHousingPanel : MonoBehaviour
    {
        public GameObject panel;
        public Text details, notice;
        public Button open, expand, close;
        public void Open() { panel.SetActive(true); panel.transform.SetAsLastSibling(); Apply(GetComponent<PythonStateClient>().CurrentState); }
        public void Close() { panel.SetActive(false); }
        public void Expand() { GetComponent<PythonStateClient>().ExpandHousing(); }
        public void Apply(PythonCafeState state)
        {
            var housing = state == null ? null : state.housing;
            if (housing == null) { details.text="飼育スペースの情報を取得してください。"; RefreshControls(); return; }
            details.text = "在籍：" + housing.count + "匹";
            if (!housing.configured) details.text += "\n上限なし（従来ルール）";
            else
            {
                details.text += "\n上限：" + housing.capacity + "匹 / 空き：" + housing.free + "匹\n拡張段階：" + housing.stage + " / 2"
                    + "\n現在の飼育維持費：" + housing.daily_cost + " / 日";
                if (!string.IsNullOrEmpty(housing.kind))
                    details.text += "\n\n次の拡張：" + housing.capacity + " → " + housing.next_capacity + "匹"
                        + "\n拡張費用：" + housing.cost + "\n所持金：" + state.funds + " → 拡張後：" + housing.funds_after
                        + "\n飼育維持費：" + housing.daily_cost + " → " + housing.next_daily_cost + " / 日"
                        + "\n日次運営費：" + housing.operating_cost + " → " + housing.next_operating_cost;
            }
            details.text += "\n\n維持費は拡張当日から、休業日も精算します。\n派遣中・家出中の猫も在籍に含み、譲渡済みは除きます。\n接客席数は変わりません。";
            notice.text = housing.can_expand ? "費用と維持費を確認して拡張してください。各段階は1回ずつ利用できます。" : housing.reason;
            expand.GetComponentInChildren<Text>().text = housing.stage == 0 ? "初回拡張する" : "2段階目へ拡張";
            RefreshControls();
        }
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();
            open.interactable=client.CanManageSaves;
            expand.interactable=client.CanExpandHousing;
            close.interactable=true;
        }
        public void Disconnect()
        {
            open.interactable=expand.interactable=false;
            if(panel.activeSelf) notice.text="Python未接続です。再接続後に拡張できます。";
        }
    }
}
