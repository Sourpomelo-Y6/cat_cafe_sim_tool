using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonFinanceDay { public int day; public string label,overview,details; }
    [Serializable] public class PythonFinanceHistory { public string totals,notes; public PythonFinanceDay[] rows; }
    public class PythonFinanceHistoryPanel : MonoBehaviour
    {
        public GameObject panel;
        public Text title,details,notice;
        public Button previous,next,close;
        private PythonCafeState current;
        private string instance;
        private int selected;
        private bool online, business;
        private PythonFinanceHistory History=>business ? current?.business_history : current?.finance_history;
        private PythonFinanceDay[] Rows => History?.rows ?? new PythonFinanceDay[0];
        public void Apply(PythonCafeState state)
        {
            if(instance!=state.instance_id){instance=state.instance_id;selected=0;Close();}
            current=state;online=true;
            if(Array.Find(Rows,row=>row.day==selected)==null)selected=Rows.Length==0 ? 0 : Rows[Rows.Length-1].day;
            Render();
        }
        public void Open(){business=false;Show();}
        public void OpenBusiness(){business=true;GetComponent<PythonMenuPanel>()?.Close();GetComponent<PythonEventListPanel>()?.Close();Show();}
        private void Show(){if(!GetComponent<PythonStateClient>().CanManageSaves || current==null)return;if(Array.Find(Rows,row=>row.day==selected)==null)selected=Rows.Length==0 ? 0 : Rows[Rows.Length-1].day;panel.SetActive(true);panel.transform.SetAsLastSibling();Render();ResetScroll();}
        public void Close(){panel.SetActive(false);}
        public void Previous(){Move(-1);}
        public void Next(){Move(1);}
        private void Move(int delta){if(!GetComponent<PythonStateClient>().CanManageSaves || !online)return;int i=Array.FindIndex(Rows,row=>row.day==selected)+delta;if(i<0 || i>=Rows.Length)return;selected=Rows[i].day;Render();ResetScroll();}
        private void ResetScroll(){Canvas.ForceUpdateCanvases();details.GetComponentInParent<ScrollRect>().verticalNormalizedPosition=1;var notesScroll=notice.GetComponentInParent<ScrollRect>();if(notesScroll!=null)notesScroll.verticalNormalizedPosition=1;}
        private void Render()
        {
            var row=Array.Find(Rows,item=>item.day==selected);
            title.text=business ? "営業成果・日次比較" : "収支の内訳・日次比較";
            details.text=(History?.totals ?? "収支情報はありません。")+"\n\n"+(row==null ? "まだ閉店した日の記録はありません。" : row.label+"\n"+row.overview+"\n\n"+row.details);
            RefreshControls();
        }
        public void RefreshControls()
        {
            int i=Array.FindIndex(Rows,row=>row.day==selected);bool ready=online && GetComponent<PythonStateClient>().CanManageSaves;
            previous.interactable=ready && i>0;next.interactable=ready && i>=0 && i<Rows.Length-1;close.interactable=true;
            previous.GetComponentInChildren<Text>().text="前の記録";next.GetComponentInChildren<Text>().text="次の記録";
            notice.text=!online ? "未接続です。表示は最後に取得した記録です。" : !GetComponent<PythonStateClient>().CanLeaveResult ? "操作を送信しています。完了までお待ちください。" : History?.notes ?? "";
        }
        public void Focus(){if(business && panel.activeSelf)panel.transform.SetAsLastSibling();}
        public void Disconnect(){online=false;RefreshControls();}
    }
}
