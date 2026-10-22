using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview {
 public sealed class OutcomeConversationPanel:AcquisitionConversationPanel {
  GameObject host;string offered;CafeOutcome Row=>Backend.Outcome;
  public void ApplyOutcome(PythonCafeState value){
   bool ended=value.game_over!=null;bool eligible=ended || value.goal_result?.kind=="continue_goal" && value.goal_result.status=="expired";
   if(Backend.DispatchResultPending || Backend.EquipmentResultPending || Backend.ReservationResultPending || Backend.MissingResultPending || Backend.TrustResultPending || !eligible || !Backend.OutcomePending){if(host!=null)host.SetActive(false);return;}
   if(!ended && (value.growth_choices?.Length>0 || value.required_action!="goal_result" || GetComponents<AcquisitionConversationPanel>().Any(x=>x!=this && x.Showing)))return;
   if(host==null){host=new GameObject("Outcome Conversation",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));host.transform.SetParent(GetComponent<PythonGoalPanel>().panel.transform.parent,false);host.SetActive(false);}
   string key=value.instance_id+"/"+Row.cause;bool first=offered!=key;offered=key;
   Bind(value,host,GetComponent<PythonGoalPanel>().title.font);if(first)Open();
   if(Showing){GetComponent<PythonGoalPanel>().panel.SetActive(false);GetComponent<PythonGameOverPanel>().panel.SetActive(false);}
  }
  public static OutcomeConversationPanel Ensure(GameObject owner)=>owner.GetComponent<OutcomeConversationPanel>()??owner.AddComponent<OutcomeConversationPanel>();
  protected override CafeConversationProgress ResolveProgress()=>Row?.progress;
  protected override bool ThreeLinePages=>true;protected override bool Choosing=>false;
  protected override void SetOpen(bool value){}protected override bool AutoRunning=>false;
  protected override string Title=>Row.Title;protected override string SceneLabel=>Row.day+"日目 · 結果";
  protected override CafeConversationLine[] Lines(string branch)=>Row.Lines();
  protected override PythonIntakeState Request=>new PythonIntakeState{cat_id="",name="カフェの記録",personality="今回の結果",can_decline=true};
  protected override bool CanAnswer(bool answer)=>false;protected override void SendAnswer(bool answer){}
  protected override string AcceptEffect=>"";protected override string DeclineEffect=>"";protected override string HistoryPrefix=>"";
  protected override string Notice=>"閉じても会話の位置は保持されます。結果画面から再開できます。";
  protected override string EndText=>Row.cause=="expired"?"結果画面から、既存の継続営業を選べます。":"結果画面で保存・タイトル復帰を選べます。";
  protected override void Finish(){if(progress.completed)return;progress.completed=true;progress.viewed_branches=new[]{Row.cause};}
  protected override void AfterClose(){GetComponent<PythonGoalPanel>().Apply(Client.CurrentState);GetComponent<PythonGameOverPanel>().Apply(Client.CurrentState);GetComponent<ContentPreview>().RefreshPythonControls();}
  public override void Open(){if(Backend.DispatchResultPending){GetComponent<PythonGeneralDispatchPanel>().Open("");return;}if(Backend.EquipmentResultPending){GetComponent<PythonStoreEventPanel>().Dialogue.OpenPending();return;}if(Backend.ReservationResultPending){GetComponent<PythonReservationPanel>()?.Dialogue.OpenPending();return;}if(Backend.MissingResultPending){var e=Backend.Simulation.State.missing_cases.First(x=>x.result!=null && !x.result.completed);GetComponent<PythonMissingPanel>().Open(e.cat_id);return;}if(Backend.TrustResultPending){GetComponent<PythonCustomerTrustPanel>()?.Dialogue.OpenPending();return;}base.Open();}
  void LateUpdate(){if(Showing && !GameOptionsPanel.IsOpen && GetComponent<PythonSavePanel>()?.panel.activeSelf!=true)host.transform.SetAsLastSibling();}
 }
}
