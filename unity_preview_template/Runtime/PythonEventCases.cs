using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview {
 public partial class PythonEventListPanel {
  public sealed class PendingCase {
   public int group; public string id,title,subject,reason; public bool result; public Action resume;
   public string Caption=>(result?"【回答済み・結果会話未完了】":"【回答・確認待ち】")+"\n"+title+" / "+subject+" / "+id+"\n"+(string.IsNullOrEmpty(reason)?"続きへ（選択した案件を開く）":"先に必要："+reason);
  }
  public bool HoldConversations {get;set;}
  readonly List<Button> caseButtons=new List<Button>(); Button continueButton;
  public void ConversationReturned(){if(current==null)return;Apply(GetComponent<PythonStateClient>().CurrentState);group=0;OpenList(false);}
  public List<PendingCase> PendingCases(){
   var rows=new List<PendingCase>();var b=CafeBackends.Local as LocalCafeBackend;if(current==null || b==null)return rows;
   Action<int,string,string,string,bool,string,Action> add=(g,id,t,subject,result,reason,action)=>rows.Add(new PendingCase{group=g,id=id,title=t,subject=subject,result=result,reason=reason,resume=action});
   Func<string,string> cat=id=>current.cats?.FirstOrDefault(c=>c.cat_id==id)?.name ?? id;
   string priority=b.EquipmentResultPending?"店舗イベントの結果会話を完了してください。":"";
   string safe=current.phase=="open"?"営業を閉店まで進めてください。":priority;
   if(current.intake_request?.status=="waiting" || b.IntakeResultPending)add(1,"intake", "保護猫の受け入れ",current.intake_request.name,b.IntakeResultPending,priority,()=>Show(1));
   if(current.regular_introduction?.status=="waiting" || b.RegularResultPending)add(7,"regular", "常連からの猫紹介",current.regular_introduction.name,b.RegularResultPending,priority,()=>Show(7));
   foreach(var r in b.EquipmentConversations.Where(r=>!r.progress.completed)){
    var row=r;bool result=r.progress.branch_id!="introduction";
    add(2,r.event_id,CafeEquipmentDialogue.Title(r),r.day+"日目",result,!result && b.EquipmentResultPending?"先の店舗イベントの結果会話を完了してください。":!result && !string.IsNullOrEmpty(current.required_action) && current.required_action!="store_event"?ActionLabel(current.required_action)+"を先に確認してください。":"",()=>{b.SelectEquipmentConversation(row.event_id);GetComponent<PythonStoreEventPanel>().Dialogue.OpenPending();});
   }
   if(current.store_event!=null && !CafeEquipmentDialogue.Supports(current.store_event.kind))add(2,"store-"+current.store_event.day,current.store_event.title,current.store_event.day+"日目",false,"",()=>Show(2));
   foreach(var e in current.growth_choices ?? new PythonGrowthChoice[0]){var key=PythonEventPresentation.GrowthKey(e);add(3,key,"猫の成長",e.name+" / "+e.title,false,priority!=""?priority:e.choices?.Length>0 && e.choices.All(c=>!c.can_select)?e.choices.First().reason:"",()=>{GetComponent<PythonStateClient>().GrowthRowIndex=Array.FindIndex(current.growth_choices,g=>PythonEventPresentation.GrowthKey(g)==key);GetComponent<PythonGrowthPanel>().Apply(current);Show(3);});}
   foreach(var e in b.Simulation.State.missing_cases){
    bool result=e.result!=null && !e.result.completed;var dto=current.missing_cats?.FirstOrDefault(c=>c.cat_id==e.cat_id && c.status=="waiting");
    if(!result && (e.resolved_day>0 || dto==null))continue;var id=e.id;
    add(4,id,"猫の家出・帰還",cat(e.cat_id),result,safe,()=>{GetComponent<PythonMissingPanel>().OpenCase(id);});
   }
   foreach(var e in current.adoption?.events ?? new PythonAdoptionEvent[0]){
    bool result=b.AdoptionConversations.Any(r=>r.event_id==e.event_id && CafeAdoptionDialogue.ResultPending(r));if(e.status!="waiting" && !result)continue;var id=e.event_id;
    add(5,id,"譲渡の申し出",e.day+"日目 / "+e.name+" / "+e.customer_name,result,priority,()=>GetComponent<PythonAdoptionPanel>().Dialogue.OpenEvent(id));
   }
   foreach(var e in current.customer_trust?.events ?? new PythonTrustEvent[0]){
    bool result=b.TrustConversations.Any(r=>r.event_id==e.event_id && CafeTrustDialogue.Pending(r));if(e.status!="waiting" && !result)continue;var id=e.event_id;
    add(9,id,"顧客の信頼回復",e.day+"日目 / "+e.name,result,safe!=""?safe:!result && !e.can_respond?e.reason:"",()=>GetComponent<PythonCustomerTrustPanel>().Dialogue.OpenEvent(id));
   }
   if(current.reservation?.status=="waiting" || b.ReservationResultPending)add(8,"reservation","特別予約",current.reservation.visit_day+"日目の来店",b.ReservationResultPending,safe,()=>Show(8));
   foreach(var e in b.Simulation.State.dispatches){
    var id=e.id;var subject=e.started_day+"日目発 / "+cat(e.cat_id)+" / "+CafeDispatchDialogue.Title(e.destination);
    var dispatch=b.DispatchConversations.FirstOrDefault(r=>r.DispatchId==id && r.Destination==e.destination);bool result=dispatch!=null && CafeDispatchDialogue.ResultPending(dispatch);
    if(!CafeSimulation.DispatchInterrupted(e) && (e.status=="waiting" || result))add(CafeDispatchDialogue.Patron(e.destination)?6:10,id,"派遣・帰還",subject,result,priority,()=>GetComponent<PythonGeneralDispatchPanel>().Dialogue.OpenEvent(id));
    var encounter=b.EncounterConversations.FirstOrDefault(r=>r.event_id==id);result=encounter!=null && CafeEncounterDialogue.ResultPending(encounter);
    if(e.encounter?.status=="waiting" || result)add(11,id,"派遣中の選択",subject,result,safe,()=>GetComponent<PythonDispatchChoicePanel>().Dialogue.OpenEvent(id));
    var lodge=b.LodgeConversations.FirstOrDefault(r=>r.event_id==id);result=lodge!=null && CafeLodgeDialogue.ResultPending(lodge);
    if(CafeLodgeDialogue.Supports(e) && (e.trouble.status=="waiting" || e.status=="waiting" || result))add(12,id,"宿のトラブル・帰還",subject,result,safe,()=>GetComponent<PythonDispatchTroublePanel>().Dialogue.OpenEvent(id));
    var intro=b.DispatchIntroductionConversations.FirstOrDefault(r=>r.event_id==id);result=intro!=null && CafeDispatchIntroductionDialogue.ResultPending(intro);
    if(e.introduction?.status=="waiting" || result)add(13,id,"派遣先からの猫紹介",subject+" / "+e.introduction.name,result,priority,()=>GetComponent<PythonDispatchIntroductionPanel>().Dialogue.OpenEvent(id));
   }
   var keys=new Dictionary<int,string>{{1,"intake_request"},{2,"store_event"},{3,"growth"},{4,"missing_return"},{5,"adoption"},{6,"dispatch_return"},{7,"regular_introduction"},{8,"reservation"},{9,"customer_trust"},{10,"dispatch_return"},{11,"dispatch_choice"},{12,"dispatch_trouble"},{13,"dispatch_introduction"}};
   foreach(var row in rows)if(!row.result && string.IsNullOrEmpty(row.reason) && !string.IsNullOrEmpty(current.required_action) && keys[row.group]!=current.required_action && !(row.group==12 && current.required_action=="dispatch_return"))row.reason=ActionLabel(current.required_action)+"を先に確認してください。";
   return rows;
  }
  static string ActionLabel(string action){switch(action){case "adoption":return "譲渡の申し出";case "player":return "プレイヤー交流";case "dispatch_trouble":return "宿のトラブル";case "dispatch_choice":return "派遣中の選択";case "dispatch_return":return "派遣の帰還";case "missing_return":return "猫の帰還";case "reservation":return "特別予約";case "dispatch_introduction":return "派遣先からの猫紹介";case "intake_request":return "保護猫の受け入れ";case "regular_introduction":return "常連からの猫紹介";case "customer_trust":return "顧客の信頼回復";case "store_event":return "店舗イベント";case "growth":return "猫の成長";case "service_assignment":return "接客の割り当て";default:return "目標・営業の結果";}}
  void RefreshCaseRows(){
   if(!CafeBackends.Standalone || content==null)return;var rows=PendingCases();
   if(group==0)foreach(var button in entries)button.gameObject.SetActive(false);
   for(int i=0;i<rows.Count;i++){
    if(i>=caseButtons.Count){var copy=Instantiate(entries[1],content);copy.name="Pending Case "+i;copy.onClick=new Button.ButtonClickedEvent();caseButtons.Add(copy);}
    var button=caseButtons[i];var row=rows[i];button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>ResumeCase(row));button.gameObject.SetActive(group==0);
    var rect=button.GetComponent<RectTransform>();rect.anchoredPosition=new Vector2(4,-i*112);rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,104);
    var label=button.GetComponentInChildren<Text>();label.text=row.Caption;label.fontSize=16;label.resizeTextForBestFit=false;label.horizontalOverflow=HorizontalWrapMode.Wrap;label.verticalOverflow=VerticalWrapMode.Truncate;
    button.interactable=online && GetComponent<PythonStateClient>().CanManageSaves && !GetComponent<PythonStateClient>().AutoPlayExecuting && string.IsNullOrEmpty(row.reason);
   }
   for(int i=rows.Count;i<caseButtons.Count;i++)caseButtons[i].gameObject.SetActive(false);
   if(group==0)content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(342,rows.Count*112));
   if(continueButton==null){continueButton=Instantiate(close,close.transform.parent);continueButton.name="Continue Business";continueButton.onClick=new Button.ButtonClickedEvent();var rect=continueButton.GetComponent<RectTransform>();rect.anchoredPosition=new Vector2(24,rect.anchoredPosition.y);rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,436);continueButton.onClick.AddListener(ContinueBusiness);}
   var client=GetComponent<PythonStateClient>();string caption=client.AutoPlayRunning?client.AutoPlayPaused?"おまかせを再開":"おまかせ実行中":current.closed?"翌日へ":current.can_set_shifts?"営業開始":"自動進行を再開";
   continueButton.GetComponentInChildren<Text>().text=caption;continueButton.interactable=CanContinue;
   notice.text=rows.Count+"件（回答・確認待ち "+rows.Count(r=>!r.result)+" / 結果会話 "+rows.Count(r=>r.result)+"）。"+RunStatus+"\n"+(CanContinue?"下の「"+caption+"」から進められます。":!string.IsNullOrEmpty(current.required_action)?"先に必要："+ActionLabel(current.required_action):rows.Count>0?"選んだ案件から続きを確認してください。":"営業準備・出勤・接客の割り当て、または目標結果を確認してください。");
  }
  string RunStatus {get{var c=GetComponent<PythonStateClient>();return c.AutoPlayRunning?c.AutoPlayPaused?"おまかせ：一時停止中":"おまかせ：実行中":c.Automatic?"通常自動進行：実行中":current.autoplay?.report?.reason=="cancelled"?"おまかせ：中止済み":current.autoplay?.report!=null?"おまかせ：終了（"+(current.autoplay.report.reason=="completed" || current.autoplay.report.reason=="day_limit"?"予定完了":current.autoplay.report.reason=="blocked"?"回答待ちで終了":current.autoplay.report.message)+"）":"通常進行：停止中";}}
  bool CanContinue {get{var c=GetComponent<PythonStateClient>();return online && current.game_over==null && (c.AutoPlayRunning?c.AutoPlayPaused && c.CanManageSaves && string.IsNullOrEmpty(current.required_action) && !(CafeBackends.Local as LocalCafeBackend).ConversationBlocksProgress:c.CanProgressBusiness || c.CanStartAutomatic);}}
  void ContinueBusiness(){if(!CanContinue)return;var c=GetComponent<PythonStateClient>();ReturnToBusiness();if(c.AutoPlayRunning)c.ResumeAutoPlay();else if(current.closed || current.can_set_shifts)c.ProgressBusiness();else c.ToggleAutomatic();}
  void ResumeCase(PendingCase row){if(!online || !GetComponent<PythonStateClient>().CanManageSaves)return;var fresh=PendingCases().FirstOrDefault(r=>r.group==row.group && r.id==row.id);if(fresh==null || !string.IsNullOrEmpty(fresh.reason))return;Close();fresh.resume();}
 }
}
