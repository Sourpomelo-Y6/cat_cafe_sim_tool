using UnityEngine;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview {
 public sealed class MissingConversationPanel:AcquisitionConversationPanel {
  GameObject host;PythonMissingPanel owner;
  public GameObject Panel=>host;
  public void Apply(PythonCafeState value,PythonMissingPanel original){owner=original;if(!Backend.MissingSafe)return;if(host==null){host=new GameObject("Missing Conversation",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));host.transform.SetParent(owner.panel.transform.parent,false);host.SetActive(false);}Bind(value,host,owner.title.font);if(Showing){owner.panel.SetActive(false);GetComponent<PythonGameOverPanel>()?.panel.SetActive(false);}}
  public bool Begin(string id){if(!Backend.BeginMissingConversation(id))return false;Apply(Client.CurrentState,GetComponent<PythonMissingPanel>());Open();return true;}
  protected override CafeConversationProgress ResolveProgress()=>Backend.MissingProgress;
  protected override bool ThreeLinePages=>true;
  private string AccessCustomer=>CafeSimulation.NamedMissingCustomer(Backend.MissingCase?.reason);
  protected override bool Choosing=>Backend.MissingPhase=="result" && Backend.Simulation.NamedReturnAccessPending(AccessCustomer)
      && progress.node>=Lines(progress.branch_id).Length;
  protected override bool CanAnswer(bool answer)=>Choosing && Client.CurrentState?.game_over==null;
  protected override string ChoicePrompt=>"今後も"+CafeSimulation.NamedCustomerName(AccessCustomer)+"の来店を受け入れますか？";
  protected override string AcceptCaption=>"今後も来店を受け入れる";
  protected override string DeclineCaption=>"今後の来店を断る";
  protected override void SendAnswer(bool answer)
  {
      string customer=AccessCustomer;
      if(answer)Client.RespondNamedReturnAccess(customer,true);
      else AskConfirmation(CafeSimulation.NamedCustomerName(customer)+"の来店を断りますか？ このセーブでは取り消せません。",
          ()=>Client.RespondNamedReturnAccess(customer,false));
  }
  protected override bool AutoRunning=>Client.AutoPlayExecuting;
  protected override void SetOpen(bool open){Backend.MissingConversationOpen=open;}
  protected override CafeConversationLine[] Lines(string branch)=>CafeMissingDialogue.Lines(Backend.MissingCase,Backend.MissingPhase,Request.name);
  private bool NamedCase=>AccessCustomer!=null;
  private string AbsenceCause=>CafeMissingDialogue.NamedCause(Backend.MissingCase.reason);
  protected override bool ShowEventListAfterClose=>!NamedCase || Backend.MissingPhase=="result";
  protected override PythonIntakeState Request=>new PythonIntakeState{cat_id=Backend.Details.ContentId(Backend.MissingCase.cat_id),name=Backend.Simulation.Definition(Backend.MissingCase.cat_id).name,personality=NamedCase?CafeSimulation.NamedCustomerName(AccessCustomer)+" · "+AbsenceCause:"店からの家出・帰還",can_decline=true};
  protected override string Title=>NamedCase?"指名猫の帰還":"猫の家出・帰還";
  protected override string SceneLabel=>NamedCase
      ? Backend.MissingCase.departed_day+"日目の"+AbsenceCause+" · "+(Backend.MissingPhase=="result"?Backend.MissingCase.resolved_day+"日目に帰還":Backend.MissingPhase=="confirmation"?"帰還待ち":"迎え待ち")
      : (Backend.MissingCase.departed_day>0?Backend.MissingCase.departed_day+"日目の家出":"家出日不明")+" · "+(Backend.MissingPhase=="result"?"帰還確定":Backend.MissingPhase=="confirmation"?"帰還待ち":"家出中");
  protected override string AcceptEffect=>"累積不満を引き継ぎ、7日間空けてから信頼回復が必要かを確認します。";
  protected override string DeclineEffect=>"このセーブでは以後の来店、売上、新規イベントがなくなります。過去の記録は見返せます。";
  protected override string HistoryPrefix=>"";
  protected override string Notice=>Backend.MissingConversationReason;
  protected override string EndText=>!NamedCase?"会話を終えて、家出・帰還の記録と店の状態を確認しましょう。"
      : Backend.MissingPhase=="confirmation"?"会話を終えたら、記録の「帰還と費用を確認する」で迎えを確定しましょう。"
      : Backend.MissingPhase=="result"?"会話を終えて、指名猫の帰還記録と店の状態を確認しましょう。"
      : "営業または休業で日程を進めると、迎えに行けるようになります。残り日数は帰還記録で確認できます。";
  protected override void Finish(){CafeMissingDialogue.Finish(progress);}
  protected override void AfterClose(){owner.ShowRecords();GetComponent<PythonGrowthPanel>()?.Apply(Client.CurrentState);GetComponent<PythonGoalPanel>()?.Apply(Client.CurrentState);GetComponent<PythonGameOverPanel>()?.Apply(Client.CurrentState);base.AfterClose();}
  void LateUpdate(){if(Showing && !GameOptionsPanel.IsOpen && GetComponent<PythonSavePanel>()?.panel.activeSelf!=true)host.transform.SetAsLastSibling();}
  public override void Open(){base.Open();if(Showing)owner.panel.SetActive(false);}
 }
}
