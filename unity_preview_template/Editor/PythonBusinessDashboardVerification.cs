using System;
using System.Reflection;
using UnityEngine;
namespace CatCafePreview.Editor
{
    public static class PythonBusinessDashboardVerification
    {
        static void Check(bool ok,string label){if(!ok)throw new Exception(label);}
        public static void BuildAndVerify()
        {
            PythonBusinessHeaderVerification.BuildAndVerify();PythonBusinessDashboardLayout.Build();
            var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="dashboard";state.required_action="";state.waiting_count=1;state.completed_interactions=3;
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});
            var header=preview.GetComponent<PythonBusinessHeader>();var service=preview.GetComponent<PythonServiceScreen>();apply();
            string unchanged=JsonUtility.ToJson(state);Check(service.serviceSummary.text.Contains("来店 4人") && service.serviceSummary.text.Contains("接客完了 3件"),"Business metrics");
            Capture(preview,"BusinessDashboardOpen");service.ShowCats();Check(service.serviceStatus.activeSelf && service.rosterPanel.GetComponent<RectTransform>().anchoredPosition.y==-262 && service.detailPanel.anchoredPosition.y==-262,"Cat view covered metrics");Check(preview.GetComponent<PythonCatVisibility>().rosterToggle.GetComponent<RectTransform>().anchoredPosition.y==-94,"Cat filter overlaps metrics");Capture(preview,"BusinessDashboardCats");
            Check(JsonUtility.ToJson(state)==unchanged && typeof(PythonStateClient).GetField("pending",flags).GetValue(client)==null,"Viewing mutated state");service.ShowSeats();
            header.auto.Toggle();Check(client.Automatic && header.hint.text.Contains("自動進行中") && !header.progress.interactable,"Automatic guards");header.auto.ChangeSpeed();Check(service.serviceSummary.text.Contains("0.25秒"),"Speed metrics");Capture(preview,"BusinessDashboardAutomatic");header.auto.Toggle();
            client.ProgressBusiness();Check(!header.progress.interactable && header.hint.text.Contains("送信"),"Pending operation hint");typeof(PythonStateClient).GetField("pending",flags).SetValue(client,null);apply();
            state.required_action="予約への回答を確認してください。";apply();Check(!header.progress.interactable && !header.auto.toggle.interactable && header.attention.interactable && header.hint.text.Contains("予約"),"Required action");header.attention.onClick.Invoke();Check(preview.GetComponent<PythonEventListPanel>().panel.activeSelf,"Event navigation");preview.GetComponent<PythonEventListPanel>().Close();
            state.required_action=string.Join("\n",new string[]{new string('長',100),new string('い',100),"回答を確認してください。"});apply();Canvas.ForceUpdateCanvases();Check(header.guidance.content.rect.height>header.guidance.viewport.rect.height,"Long reason cannot scroll");Capture(preview,"BusinessDashboardWaiting");
            CaptureSize(preview,"BusinessDashboardSmall",960,600);CaptureSize(preview,"BusinessDashboardWide",1440,810);
            state.required_action="";state.service_assignment=new PythonServiceAssignment{waiting=true,auto_assign=false,customers=new PythonAssignmentCustomer[0],cats=new PythonAssignmentCat[0],seats=new PythonAssignmentSeat[0]};apply();preview.GetComponent<PythonServiceAssignmentPanel>().Close();Check(header.hint.text.Contains("割り当て待ち") && !header.progress.interactable,"Assignment blocker");header.attention.onClick.Invoke();Check(preview.GetComponent<PythonServiceAssignmentPanel>().panel.activeSelf,"Assignment navigation");preview.GetComponent<PythonServiceAssignmentPanel>().Close();state.service_assignment=null;
            state.pending_results=new PythonPendingResults{count=1};apply();preview.GetComponent<PythonPersistPanel>().Close();Check(header.hint.text.Contains("保存") && !header.progress.interactable && !header.auto.toggle.interactable,"Persist blocker");header.attention.onClick.Invoke();Check(preview.GetComponent<PythonPersistPanel>().panel.activeSelf,"Persist navigation");preview.GetComponent<PythonPersistPanel>().Close();state.pending_results=null;
            state.closed=true;apply();preview.GetComponent<PythonClosingPanel>().Close();Check(header.progress.GetComponentInChildren<UnityEngine.UI.Text>().text=="翌日へ" && !header.auto.toggle.gameObject.activeSelf,"Closing controls");header.attention.onClick.Invoke();Check(preview.GetComponent<PythonClosingPanel>().panel.activeSelf,"Closing navigation");preview.GetComponent<PythonClosingPanel>().Close();Capture(preview,"BusinessDashboardClosed");
            state.closed=false;state.can_set_shifts=true;foreach(var cat in state.cats)cat.working=false;apply();preview.GetComponent<PythonPreparationPanel>().Close();Check(!header.progress.interactable && header.hint.text.Contains("出勤する猫がいません"),"No worker hint");
            typeof(PythonStateClient).GetField("online",flags).SetValue(client,false);preview.PythonConnectionError("test");Check(header.summary.text.Contains("未接続") && !header.progress.interactable && !header.attention.gameObject.activeSelf,"Offline status");
            Debug.Log("CAT_CAFE_BUSINESS_DASHBOARD_VERIFIED: metrics / both views / read only / automatic / speed / pending / required / long reason / event navigation / assignment / persist / closing / preparation / offline");
        }
        static void CaptureSize(ContentPreview p,string name,int width,int height){typeof(PythonBusinessHeaderVerification).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{p,name,width,height});}
        static void Capture(ContentPreview p,string name){Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{p,"Logs/"+name+".png"});}
    }
}
