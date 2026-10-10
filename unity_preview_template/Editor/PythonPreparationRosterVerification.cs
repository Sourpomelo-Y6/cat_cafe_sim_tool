using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonPreparationRosterVerification
    {
        static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
        public static void BuildAndVerify()
        {
            PythonPreparationLayout.Build();
            var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();
            var client=preview.GetComponent<PythonStateClient>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var pending=typeof(PythonStateClient).GetField("pending",flags);
            var state=PythonServiceSetup.Sample();state.instance_id="preparation-roster";state.can_set_shifts=true;state.tick=0;state.required_action="";
            state.rest_proposal=new[]{state.cats[2].cat_id,state.cats[3].cat_id,state.cats[4].cat_id};
            foreach(var cat in state.cats)cat.shift_forecast="前日接客：6行動\n出勤：疲労 66 / 発症 12.0% / ストレス 8\n休養：疲労 40 / 発症 0.0% / ストレス 0\n当日の担当回数によって実際の値は変わります。";
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});apply();
            var panel=preview.GetComponent<PythonPreparationPanel>();var roster=preview.GetComponent<PythonPreparationRoster>();
            Check(panel.panel.activeSelf && roster.Count==5,"Initial roster missing");
            roster.SelectButton(2).onClick.Invoke();Check(panel.SelectedCatId==state.cats[2].cat_id,"Wrong selected cat");
            roster.Goal();Check(roster.goal.gameObject.activeSelf && !roster.forecast.gameObject.activeSelf,"Goal tab");
            roster.SelectButton(0).onClick.Invoke();Check(roster.forecast.gameObject.activeSelf,"Selection did not show forecast");
            panel.ProposeRest();Check(panel.HasProposal && !panel.start.interactable,"Draft did not guard start");
            roster.WorkButton(0).onClick.Invoke();roster.RestButton(2).onClick.Invoke();
            Check(pending.GetValue(client)==null && state.cats.All(c=>c.working) && panel.Working(state.cats[0]) && !panel.Working(state.cats[2]),"Draft changed server state");
            Capture(preview,"Logs/PreparationRosterDraft.png");
            panel.CancelProposal();Check(state.cats.All(c=>panel.Working(c)),"Cancel changed shifts");
            panel.ProposeRest();panel.ConfirmProposal();
            var command=(PythonShiftCommand)pending.GetValue(client);
            Check(command!=null && command.working_cats.SequenceEqual(state.rest_proposal.OrderBy(id=>id,StringComparer.Ordinal)) && !roster.RestButton(0).interactable && !panel.start.interactable,"Draft payload or busy guards");
            pending.SetValue(client,null);apply();roster.RestButton(1).onClick.Invoke();command=(PythonShiftCommand)pending.GetValue(client);
            Check(command!=null && !command.working_cats.Contains(state.cats[1].cat_id) && command.working_cats.Length==4,"Row command targeted wrong cat");
            pending.SetValue(client,null);apply();string selected=state.cats[3].cat_id;panel.SelectCatId(selected);Array.Reverse(state.cats);apply();
            Check(panel.SelectedCatId==selected && roster.CardId(0)==state.cats[0].cat_id,"Reordering lost selection");
            state.cats[0].working=false;state.cats[0].health_status="sick";state.cats[1].working=false;state.cats[1].activity="adopted";apply();
            Check(roster.Count==4 && !roster.WorkButton(0).interactable,"Sick or adopted filtering failed");
            preview.GetComponent<PythonCatVisibility>().Change(true);Check(roster.Count==5 && !roster.WorkButton(1).interactable,"Adopted work enabled");
            roster.SelectButton(1).onClick.Invoke();preview.GetComponent<PythonCatVisibility>().Change(false);Check(panel.SelectedCatId!=state.cats[1].cat_id,"Hidden cat remained selected");
            state.required_action="帰還を確認してください。";apply();Check(!panel.start.interactable && panel.notice.text.Contains("帰還") && !roster.RestButton(2).interactable,"Required action guard");
            state.required_action="";apply();panel.ProposeRest();panel.Disconnect();Check(!panel.HasProposal && !panel.start.interactable && !roster.RestButton(2).interactable,"Offline guard");
            apply();panel.ProposeRest();state.revision++;apply();Check(!panel.HasProposal,"Revision retained draft");
            foreach(var cat in state.cats){cat.activity="cafe";cat.health_status="healthy";cat.working=true;}apply();panel.Open();roster.SelectButton(0).onClick.Invoke();
            Canvas.ForceUpdateCanvases();Check(roster.content.rect.height>roster.scroll.viewport.rect.height,"Roster cannot scroll");
            roster.scroll.verticalNormalizedPosition=.4f;apply();Check(Math.Abs(roster.scroll.verticalNormalizedPosition-.4f)<.02f,"Polling reset scroll");
            roster.scroll.verticalNormalizedPosition=1;Capture(preview,"Logs/PreparationRoster.png");roster.Goal();Capture(preview,"Logs/PreparationRosterGoal.png");
            state.pending_results=new PythonPendingResults{count=1};apply();Check(!panel.start.interactable && !roster.RestButton(0).interactable && panel.notice.text.Contains("保存"),"Pending result guard");state.pending_results=null;
            state.autoplay=new PythonAutoPlay{running=true};apply();panel.Open();Check(!panel.panel.activeSelf && !roster.RestButton(0).interactable,"Autoplay covered business or enabled shifts");state.autoplay=null;apply();
            foreach(var cat in state.cats)cat.working=false;apply();Check(!panel.start.interactable && panel.notice.text.Contains("出勤する猫がいません"),"Empty shift start");foreach(var cat in state.cats)cat.working=true;apply();
            panel.StartBusiness();command=(PythonShiftCommand)pending.GetValue(client);Check(command?.kind=="start_business","Start command missing");
            pending.SetValue(client,null);state.can_set_shifts=false;apply();Check(!panel.panel.activeSelf,"Preparation remained during business");
            Debug.Log("CAT_CAFE_PREPARATION_ROSTER_VERIFIED: rows / selection / tabs / draft / cancel / confirm / payload / busy / reorder / sick / adopted / required / offline / revision / scroll / start");
        }
        static void Capture(ContentPreview preview,string path)
        {
            Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{preview,path});
        }
    }
}
