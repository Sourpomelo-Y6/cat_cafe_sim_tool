using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonStartObjective { public string choice,label,details,reason,summary; public bool started,can_start; }
    [Serializable] public class PythonObjectiveStart { public PythonStartObjective[] rows; public string notes; }
    public class PythonObjectiveStartPanel : MonoBehaviour
    {
        public GameObject panel,confirmation;
        public Text title,details,notice,confirmationText,summary,actionGuide;
        public Button[] tabs;
        public Button proceed,close,confirm,cancel;
        private PythonCafeState current;
        private string instance,identity,selected="popularity",selection;
        private bool online;
        private PythonStartObjective Row => Array.Find(current?.objective_start?.rows ?? new PythonStartObjective[0],r=>r.choice==selected);
        public void Open(){if(current?.objective_start==null || !GetComponent<PythonStateClient>().CanManageSaves)return;Clear();panel.SetActive(true);panel.transform.SetAsLastSibling();Render();ResetScroll();}
        public void Close(){Clear();panel.SetActive(false);}
        private void Clear(){selection=null;confirmation.SetActive(false);}
        public void Management(){Select("management");}public void Popularity(){Select("popularity");}public void Bond(){Select("bond");}public void Patron(){Select("patron");}
        private void Select(string choice){if(!online || selection!=null || !GetComponent<PythonStateClient>().CanManageSaves)return;selected=choice;Render();ResetScroll();}
        private void ResetScroll(){Canvas.ForceUpdateCanvases();details.transform.parent.parent.GetComponent<ScrollRect>().verticalNormalizedPosition=1;}
        public void Choose(){if(!online || !GetComponent<PythonStateClient>().CanStartObjective(selected))return;selection=selected;confirmation.SetActive(true);confirmation.transform.SetAsLastSibling();confirmationText.text=Row.label+"を開始しますか？\n\n"+Row.details;Canvas.ForceUpdateCanvases();confirmationText.GetComponentInParent<ScrollRect>().verticalNormalizedPosition=1;RefreshControls();}
        public void Confirm(){if(!online || selection==null || !GetComponent<PythonStateClient>().CanStartObjective(selection))return;var choice=selection;Clear();GetComponent<PythonStateClient>().StartObjective(choice);}
        public void Cancel(){Clear();RefreshControls();}
        public void Apply(PythonCafeState state)
        {
            if(instance!=state.instance_id){instance=state.instance_id;selected="popularity";Close();}
            current=state;online=true;string key=state.instance_id+"/"+state.revision+"/"+state.day;if(identity!=key){identity=key;Clear();}
            if(state.objective_start==null || state.game_over!=null)Close();Render();
        }
        private void Render(){if(summary!=null)summary.text=(Row?.label ?? "経営目標")+"\n\n"+(Row?.summary ?? (Row?.started==true ? "開始済み：詳細欄で進捗を確認できます。" : "未開始"));if(actionGuide!=null)actionGuide.text=Row?.started==true ? "開始済みです。結果が出たら結果画面で確認します。" : "開始前に条件を確認できます。開始には確定操作が必要です。";title.text="経営ルール・目標の開始・進捗";details.text=(Row?.label ?? "経営目標")+(Row?.started==true ? "（開始済み）" : "")+"\n\n"+(Row?.details ?? "情報はありません。");RefreshControls();}
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();bool ready=online && client.CanManageSaves && selection==null;
            foreach(var tab in tabs)tab.interactable=ready;proceed.interactable=ready && client.CanStartObjective(selected);proceed.GetComponentInChildren<Text>().text=Row?.started==true ? "開始済み" : "この目標を開始（確認）";confirm.interactable=online && selection!=null && client.CanStartObjective(selection);cancel.interactable=close.interactable=true;
            string[] keys={"popularity","bond","patron","management"};for(int i=0;i<tabs.Length;i++)tabs[i].GetComponent<Image>().color=keys[i]==selected ? new Color(.78f,.88f,.74f) : Color.white;
            notice.text=!online ? "未接続です。再接続後に目標を開始できます。" : !client.CanManageSaves ? "操作を送信しています。完了までお待ちください。" : (Row?.started==true ? "開始済みです。現在の進捗を確認できます。" : Row?.reason ?? "")+"\n"+(current?.objective_start?.notes ?? "");
        }
        public void Disconnect(){online=false;Clear();RefreshControls();}
    }
}
