using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{

    public class PythonGeneralDispatchPanel : MonoBehaviour
    {
        public GameObject panel;
        public Dropdown cats,destinations,returns;
        public Text title,details,progress,notice,summary;
        public Button send,receive,close,compare;
        private PythonCafeState state;
        private PythonDispatchOption[] options=new PythonDispatchOption[0];
        private PythonDispatchEvent[] events=new PythonDispatchEvent[0];
        private PythonCatState[] catRows=new PythonCatState[0];
        private string catId,instanceId,lastWaiting;
        private bool showing,focusWaiting,online,confirming;
        private int confirmedRevision;
        public string SelectedCatId=>catId;
        public string SelectedDestination=>destinations.value>=0 && destinations.value<options.Length?options[destinations.value].choice:null;
        public DispatchConversationPanel Dialogue=>GetComponent<DispatchConversationPanel>() ?? gameObject.AddComponent<DispatchConversationPanel>();
        public void HideForConversation(){confirming=false;showing=false;panel.SetActive(false);}
        public void Open(string id){if(CafeBackends.Standalone && string.IsNullOrEmpty(id)){var waiting=(CafeBackends.Local as LocalCafeBackend)?.Simulation.State.dispatches.FirstOrDefault(e=>CatCafePreview.Standalone.CafeDispatchDialogue.Supports(e.destination) && e.status=="waiting");if(waiting!=null){if(GetComponent<PythonDispatchTroublePanel>().Dialogue.OpenEvent(waiting.id))return;if(Dialogue.OpenEvent(waiting.id))return;}}if(CafeBackends.Standalone && (CafeBackends.Local as LocalCafeBackend)?.DispatchResultPending==true){Dialogue.Apply(GetComponent<PythonStateClient>().CurrentState,this);Dialogue.Open();return;}if(!GetComponent<PythonStateClient>().CanManageSaves)return;confirming=false;if(!string.IsNullOrEmpty(id))catId=id;showing=true;GetComponent<PythonCatDetailsPanel>().Close();Render();panel.transform.SetAsLastSibling();}
        public void OpenReturn(string eventId){if(CafeBackends.Standalone && GetComponent<PythonDispatchTroublePanel>().Dialogue.OpenEvent(eventId))return;if(CafeBackends.Standalone && Dialogue.OpenEvent(eventId))return;var ev=Array.Find(state?.general_dispatch?.events ?? new PythonDispatchEvent[0],r=>r.choice==eventId);Open(ev?.cat_id);int index=Array.FindIndex(events,r=>r.choice==eventId);if(index>=0){returns.SetValueWithoutNotify(index);RenderDetails();}}
        public void OpenOption(string id,string destination){Open(id);int index=Array.FindIndex(options,r=>r.cat_id==id && r.choice==destination);if(index>=0){destinations.SetValueWithoutNotify(index);RenderDetails();}}
        public void Close(){if(CafeBackends.Standalone && GetComponent<DispatchConversationPanel>()?.Showing==true)GetComponent<DispatchConversationPanel>().Close();confirming=false;showing=false;panel.SetActive(false);}
        public void ResetSelection(){Close();catId=null;options=new PythonDispatchOption[0];events=new PythonDispatchEvent[0];lastWaiting=null;focusWaiting=false;}
        public void SelectCat(int index){if(!online || !GetComponent<PythonStateClient>().CanManageSaves || index<0 || index>=catRows.Length)return;confirming=false;catId=catRows[index].cat_id;Render();ResetScroll();}
        public void Select(int _){confirming=false;RenderDetails();ResetScroll();}
        public void Compare(){if(confirming){confirming=false;RefreshControls();return;}if(!online || !GetComponent<PythonStateClient>().CanManageSaves || catId==null)return;var comparison=GetComponent<PythonDispatchComparisonPanel>();comparison.OpenOption(catId,"general",SelectedDestination);if(!comparison.panel.activeSelf)return;Close();GetComponent<PythonMenuPanel>()?.TransferReturn(panel,comparison.panel);}
        public void Send(){if(!online || !GetComponent<PythonStateClient>().CanManageSaves || destinations.value<0 || destinations.value>=options.Length || !options[destinations.value].can_select)return;if(CafeBackends.Standalone && CatCafePreview.Standalone.CafeDispatchDialogue.Supports(SelectedDestination)){Dialogue.Departure(catId,SelectedDestination);return;}if(!confirming){confirming=true;confirmedRevision=state.revision;RefreshControls();return;}confirming=false;GetComponent<PythonStateClient>().DispatchGeneral(options[destinations.value].cat_id,options[destinations.value].choice);RefreshControls();}
        public void Receive(){if(CafeBackends.Standalone && returns.value>=0 && returns.value<events.Length && GetComponent<PythonDispatchTroublePanel>().Dialogue.OpenEvent(events[returns.value].choice))return;if(!online || returns.value<0 || returns.value>=events.Length)return;if(CafeBackends.Standalone && Dialogue.OpenEvent(events[returns.value].choice))return;GetComponent<PythonStateClient>().ReceiveGeneral(events[returns.value].choice);}
        public void Apply(PythonCafeState value)
        {
            if(state==null || state.instance_id!=value.instance_id || (confirming && confirmedRevision!=value.revision))confirming=false;state=value;online=true;if(instanceId!=state.instance_id){instanceId=state.instance_id;ResetSelection();}
            var waiting=(state.general_dispatch?.events ?? new PythonDispatchEvent[0]).FirstOrDefault(r=>r.needs_response || r.status=="waiting" || r.status=="missing");
            if(waiting!=null && waiting.choice!=lastWaiting){lastWaiting=waiting.choice;catId=waiting.cat_id;showing=true;focusWaiting=true;GetComponent<PythonCatDetailsPanel>().Close();}if(waiting==null)lastWaiting=null;Render();if(CafeBackends.Standalone){Dialogue.Apply(value,this);if(GetComponent<LodgeConversationPanel>()?.Showing==true)HideForConversation();}
        }
        static void Fill(Dropdown dropdown,string[] captions,int index){dropdown.options.Clear();foreach(var caption in captions)dropdown.options.Add(new Dropdown.OptionData(caption));if(captions.Length==0)dropdown.options.Add(new Dropdown.OptionData("記録・選択肢なし"));dropdown.SetValueWithoutNotify(Math.Max(0,Math.Min(index,captions.Length-1)));dropdown.RefreshShownValue();}
        private void Render()
        {
            panel.SetActive(showing);if(!showing || state==null)return;
            catRows=state.cats ?? new PythonCatState[0];if(!Array.Exists(catRows,r=>r.cat_id==catId))catId=catRows.FirstOrDefault()?.cat_id;
            if(cats!=null)Fill(cats,catRows.Select(r=>r.name+" / "+PythonCatCard.Activity(r,false)).ToArray(),Array.FindIndex(catRows,r=>r.cat_id==catId));
            string oldDestination=SelectedDestination;var oldEvent=events.Length>returns.value?events[returns.value].choice:null;
            options=(state.general_dispatch?.options ?? new PythonDispatchOption[0]).Where(r=>r.cat_id==catId).ToArray();events=(state.general_dispatch?.events ?? new PythonDispatchEvent[0]).Where(r=>r.cat_id==catId).ToArray();
            Fill(destinations,options.Select(r=>r.label).ToArray(),Math.Max(0,Array.FindIndex(options,r=>r.choice==oldDestination)));int pending=Array.FindIndex(events,r=>r.needs_response || r.status=="waiting" || r.status=="missing");
            Fill(returns,events.Select(r=>r.label+"（"+PythonDispatchDisplay.Status(r)+"）").ToArray(),focusWaiting && pending>=0?pending:Array.Exists(events,r=>r.choice==oldEvent)?Array.FindIndex(events,r=>r.choice==oldEvent):Math.Max(0,pending));focusWaiting=false;
            title.text=(Array.Find(catRows,r=>r.cat_id==catId)?.name ?? "猫")+"の一般派遣・帰還";RenderDetails();
        }
        private void RenderDetails()
        {
            if(state==null)return;var option=destinations.value>=0 && destinations.value<options.Length?options[destinations.value]:null;
            details.text=PythonDispatchDisplay.Terms(state,catId,"general",option);if(summary!=null)summary.text=PythonDispatchDisplay.Summary(state,catId,"general",option);
            var ev=returns.value>=0 && returns.value<events.Length?events[returns.value]:null;
            progress.text=ev==null?"この猫の派遣記録はありません。":"状態："+PythonDispatchDisplay.Status(ev)+"\n"+(ev.status=="travelling"?"あと "+ev.remaining+"回の閉店で帰還":ev.status=="waiting"?"帰還しました。報酬を受け取ってください。":ev.status=="resolved"?"帰還・報酬の受け取り済み":"イベントへの対応待ち")+"\n報酬："+PythonDispatchDisplay.N(ev.reward)+"\n"+ev.detail+(string.IsNullOrEmpty(ev.reason)?"":"\n操作できない理由："+ev.reason);

            if(ev?.needs_response==true)progress.text+="\n\nイベント一覧の「派遣中の選択」から回答してください。";else if(ev?.status=="missing")progress.text+="\n\nイベント一覧の「宿のトラブル」から対応してください。";
            PythonDispatchDisplay.Size(details);PythonDispatchDisplay.Size(progress);RefreshControls();
        }
        private void ResetScroll(){foreach(var text in new[]{details,progress}){var scroll=text.GetComponentInParent<ScrollRect>(true);if(scroll!=null)scroll.verticalNormalizedPosition=1;}}
        public void RefreshControls()
        {
            bool ready=online && GetComponent<PythonStateClient>().CanManageSaves;destinations.interactable=ready && options.Length>0;returns.interactable=ready && events.Length>0;if(cats!=null)cats.interactable=ready && catRows.Length>0;if(compare!=null)compare.interactable=ready && GetComponent<PythonDispatchComparisonPanel>()!=null && state?.dispatch_comparison!=null && catId!=null;
            send.interactable=ready && destinations.value>=0 && destinations.value<options.Length && options[destinations.value].can_select;receive.interactable=ready && returns.value>=0 && returns.value<events.Length && events[returns.value].can_select;close.interactable=true;
            send.GetComponentInChildren<Text>().text=confirming?"この内容で出発する":"出発内容を確認";if(compare!=null)compare.GetComponentInChildren<Text>().text=confirming?"出発確認を取り消す":"派遣先を比較する";
            notice.text=!online?"Python未接続です。再接続後に派遣・帰還を確認できます。":!ready?"操作を送信しています。完了するまでお待ちください。":confirming?"出発の確認："+options[destinations.value].label+"\n"+PythonDispatchDisplay.Summary(state,catId,"general",options[destinations.value]).Split('\n')[1]:"左で出発条件、右で派遣中・帰還待ちの記録を確認できます。\n条件と報酬は出発時に固定します。帰還後は出勤設定を確認してください。";
        }
        public void Disconnect(){confirming=false;online=false;RefreshControls();}
    }
}
