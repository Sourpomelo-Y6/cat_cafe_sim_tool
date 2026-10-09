using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonPendingResult { public string session_id,cat_id,customer_id,details; }
    [Serializable] public class PythonPendingResults { public int count;public string reason,notes;public PythonPendingResult[] rows; }
    public class PythonPersistPanel : MonoBehaviour
    {
        public GameObject panel,activity;
        public Text title,details,notice,activityStatus;
        public Button retry,close,activityDetails;
        private PythonPendingResults view;
        private string instance;
        private bool online,wasPending;
        private PythonStateClient Client=>GetComponent<PythonStateClient>();
        public void Open(){if(!(view?.count>0))return;panel.SetActive(true);Render();ResetScroll();Focus();}
        public void Close(){panel.SetActive(false);}
        public void Retry(){if(online)Client.RetryPersist();}
        public void Apply(PythonCafeState state)
        {
            if(instance!=state.instance_id){instance=state.instance_id;wasPending=false;Close();}
            view=state.pending_results;online=true;bool pending=view?.count>0;
            if(pending && !wasPending)Open();wasPending=pending;Render();Focus();
        }
        private void Render()
        {
            title.text="未保存の交流結果";
            details.text=view?.count>0 ? "未保存："+view.count+"件\n\n"+(string.IsNullOrEmpty(view.reason) ? "保存が完了していません。元の失敗理由の記録はありません。" : "保存失敗の理由："+view.reason)+"\n\n"+string.Join("\n\n",Array.ConvertAll(view.rows ?? new PythonPendingResult[0],r=>r.details)) : "未保存の交流結果はありません。\n\n交流結果の確定後は、セーブで進捗を保存してください。";
            details.supportRichText=false;RefreshControls();
        }
        public void RefreshControls()
        {
            bool pending=view?.count>0;activity.SetActive(pending);retry.interactable=online && Client.CanRetryPersist;
            activityDetails.interactable=pending;close.interactable=true;
            activityStatus.text="未保存の交流結果："+(view?.count ?? 0)+"件 / 営業を一時停止";
            if(pending){var auto=GetComponent<PythonAutoPlayPanel>();if(auto?.activity!=null)auto.activity.SetActive(false);var service=GetComponent<PythonServiceScreen>();if(service?.heading!=null)service.heading.gameObject.SetActive(false);}
            notice.text=!online ? "接続が切れています。再接続後に保存を再試行できます。" : pending && !Client.CanRetryPersist ? "操作結果を確認しています。完了までお待ちください。" : view?.notes ?? "";
        }
        private void ResetScroll(){Canvas.ForceUpdateCanvases();details.GetComponentInParent<ScrollRect>(true).verticalNormalizedPosition=1;}
        public void Focus(){if(panel.activeSelf)panel.transform.SetAsLastSibling();}
        public void Notify(string message){if(!Client.HasPendingResults && panel.activeSelf && !string.IsNullOrEmpty(message))notice.text=message;}
        public void Disconnect(){online=false;RefreshControls();}
    }
}
