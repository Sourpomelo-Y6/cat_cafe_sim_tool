using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview {
 public sealed class ReservationConversationPanel:AcquisitionConversationPanel {
  GameObject host;PythonReservationPanel owner;
  CafeReservationConversation Row=>Backend.ReservationConversation;
  CafeReservation Event=>Backend.Simulation.State.reservation;
  bool Result=>ReferenceEquals(progress,Row?.result);
  public GameObject Panel=>host;
  protected override Texture2D PortraitTexture(string id)=>HumanPortraitResources.Resolve("reservation-longhair");
  protected override bool Ready=>base.Ready && !owner.Confirming;
  public void Apply(PythonCafeState value,PythonReservationPanel original) {
   owner=original;if(!Backend.ReservationSafe){if(host!=null)host.SetActive(false);Backend.ReservationConversationOpen=false;return;}
   Backend.PrepareReservationConversation();
   if(host==null){host=new GameObject("Reservation Conversation",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));host.transform.SetParent(owner.panel.transform.parent,false);host.SetActive(false);}
   Bind(value,host,owner.title.font);if(Showing){owner.panel.SetActive(owner.Confirming);GetComponent<PythonGameOverPanel>()?.panel.SetActive(false);}
   if(owner.Confirming){owner.panel.SetActive(true);owner.panel.transform.SetAsLastSibling();owner.confirmation.transform.SetAsLastSibling();}
  }
  public bool OpenPending(){Backend.PrepareReservationConversation();if(Row?.Current==null || Row.Current.completed || !Backend.ReservationSafe)return false;Apply(Client.CurrentState,GetComponent<PythonReservationPanel>());Open();return true;}
  protected override CafeConversationProgress ResolveProgress()=>Row?.Current;
  protected override bool ThreeLinePages=>true;
  protected override bool AutoRunning=>false;
  protected override void SetOpen(bool open){Backend.ReservationConversationOpen=open;}
  protected override bool Choosing=>!Result && base.Choosing;
  protected override CafeConversationLine[] Lines(string branch)=>CafeReservationDialogue.Lines(Row,Result,branch);
  protected override PythonIntakeState Request=>new PythonIntakeState{cat_id="",name=Row.name,personality=Result?"接客結果 · "+Row.visit_day+"日目":"来店予定 · "+Row.visit_day+"日目",can_decline=true};
  protected override bool CanAnswer(bool yes)=>Event?.status=="waiting" && Client.CanRespondReservation(yes?"accept":"decline");
  protected override string Title=>"特別予約";
  protected override string SceneLabel=>Row.event_id+" · "+(Result?"接客結果確定":Event.status=="waiting"?"回答待ち":Event.status=="declined"?"辞退済み":"受諾済み");
  protected override string ChoicePrompt=>"予約条件を確認して、引き受けるか辞退するかを選んでください。";
  protected override string AcceptCaption=>"引き受ける（確認へ）";
  protected override string DeclineCaption=>"辞退する（確認へ）";
  string AvailableCats=>Backend.Simulation.State.cats.Any(c=>c.activity=="cafe" && c.health=="healthy" && Backend.Simulation.Definition(c.id).features.Contains(Backend.Simulation.Rules.reservation.feature))?"現在在店の健康な猫に条件の特徴があります。出勤・体力も猫一覧で確認してください。":"現在在店の健康な猫に条件の特徴がありません。受諾しても条件達成は保証されません。猫一覧で確認してください。";
  protected override string AcceptEffect=>"来店当日は休業不可。回答時の資金・人気変化なし。\n"+Row.Schedule+"\n"+Row.Conditions+"\n"+Row.Rewards+"\n条件未達・未接客では追加報酬なし。\n"+AvailableCats;
  protected override string DeclineEffect=>"辞退のペナルティなし。資金・人気変化なし。\nこの予約による来店・追加報酬なし。依頼は一度限り。\n確認画面で取り消すと回答は確定しません。";
  protected override string HistoryPrefix=>Result || progress.branch_id=="introduction"?"":string.Join("\n\n",CafeReservationDialogue.Lines(Row,false,"introduction").Select(l=>(l.speaker==""?"地の文":l.speaker)+"\n"+l.text))+"\n\n";
  protected override string Notice=>Backend.ReservationConversationReason;
  protected override string EndText=>"この結果は記録済みです。会話を終えて、残るイベントと店の状態を確認しましょう。";
  protected override void SendAnswer(bool yes){if(CanAnswer(yes))owner.BeginConversationAnswer(yes?"accept":"decline");}
  protected override void Finish(){CafeReservationDialogue.Finish(Row);}
  public override void Open(){base.Open();if(Showing)owner.panel.SetActive(false);}
  public override void Close(){if(owner.Confirming)return;base.Close();}
  protected override void AfterClose(){owner.Apply(Client.CurrentState);GetComponent<PythonGrowthPanel>()?.Apply(Client.CurrentState);GetComponent<PythonGoalPanel>()?.Apply(Client.CurrentState);GetComponent<PythonGameOverPanel>()?.Apply(Client.CurrentState);base.AfterClose();}
  void LateUpdate(){if(Showing && !owner.Confirming && !GameOptionsPanel.IsOpen && GetComponent<PythonSavePanel>()?.panel.activeSelf!=true)host.transform.SetAsLastSibling();}
 }
}
