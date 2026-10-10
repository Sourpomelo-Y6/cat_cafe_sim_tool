using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonEventRecordPanel : MonoBehaviour
    {
        public GameObject panel;public Text title,details,notice;public Button cat;private string instance,id;
        private PythonStateClient Client=>GetComponent<PythonStateClient>();
        public void Open(){var state=Client.CurrentState;var q=state?.intake_request;if(!Client.CanManageSaves || q==null || (q.status!="accepted" && q.status!="declined"))return;instance=state.instance_id;id=q.cat_id;panel.SetActive(true);panel.transform.SetAsLastSibling();Refresh();Canvas.ForceUpdateCanvases();details.GetComponentInParent<ScrollRect>().verticalNormalizedPosition=1;}
        public void Close(){if(panel!=null)panel.SetActive(false);}
        public void Back(){Close();var list=GetComponent<PythonEventListPanel>();list.Open();list.SelectGroup(3);}
        public void Business(){Close();GetComponent<PythonEventListPanel>().ReturnToBusiness();}
        public void Cat(){if(cat.interactable)GetComponent<PythonCatDetailsPanel>().Open(id);}
        public void Refresh()
        {
            if(panel==null || !panel.activeSelf)return;var state=Client.CurrentState;var q=state?.intake_request;
            if(state==null || state.instance_id!=instance || q==null || q.cat_id!=id || (q.status!="accepted" && q.status!="declined")){Close();return;}
            title.text="保護猫の受け入れ・結果の記録";details.text=(q.status=="accepted" ? "受け入れ済み" : "見送り済み")+" / 閲覧のみ\n\n"+q.day+"日目に届いた依頼\n名前："+q.name+"\n個性："+(string.IsNullOrEmpty(q.personality) ? "記録なし" : q.personality)+"\n特徴："+(string.IsNullOrEmpty(q.features) ? "記録なし" : q.features)+"\n"+q.trait+"\n\n依頼時の初期費用："+q.cost.ToString("0.#")+"\n"+(q.status=="accepted" ? "この依頼は受け入れ済みです。再回答による加入や費用の支払いはありません。" : "この依頼は見送り済みです。再回答はできません。")+(string.IsNullOrEmpty(q.response_reason) ? "" : "\n\n"+q.response_reason);
            cat.gameObject.SetActive(q.status=="accepted");cat.interactable=Client.CanManageSaves && Array.Exists(state.cats ?? new PythonCatState[0],c=>c.cat_id==id);notice.text=!Client.CanLeaveResult ? (Client.HasPendingResults ? "接客結果が未保存です。記録は閲覧できます。" : "操作中です。完了後に猫の詳細を開けます。") : Client.CanManageSaves ? "結果を確認したら、イベント一覧や営業画面へ戻れます。" : "最後に取得した記録を表示しています。再接続後に猫の詳細を開けます。";
        }
    }
}
