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
  protected override bool Choosing=>false;
  protected override bool AutoRunning=>Client.AutoPlayExecuting;
  protected override void SetOpen(bool open){Backend.MissingConversationOpen=open;}
  protected override CafeConversationLine[] Lines(string branch)=>CafeMissingDialogue.Lines(Backend.MissingCase,Backend.MissingPhase,Request.name);
  protected override PythonIntakeState Request=>new PythonIntakeState{cat_id=Backend.Details.ContentId(Backend.MissingCase.cat_id),name=Backend.Simulation.Definition(Backend.MissingCase.cat_id).name,personality="店からの家出・帰還",can_decline=true};
  protected override string Title=>"猫の家出・帰還";
  protected override string SceneLabel=>(Backend.MissingCase.departed_day>0?Backend.MissingCase.departed_day+"日目の家出":"家出日不明")+" · "+(Backend.MissingPhase=="result"?"帰還確定":Backend.MissingPhase=="confirmation"?"帰還待ち":"家出中");
  protected override string AcceptEffect=>"";
  protected override string DeclineEffect=>"";
  protected override string HistoryPrefix=>"";
  protected override string Notice=>Backend.MissingConversationReason;
  protected override string EndText=>"会話を終えて、家出・帰還の記録と店の状態を確認しましょう。";
  protected override void Finish(){CafeMissingDialogue.Finish(progress);}
  protected override void AfterClose(){owner.ShowRecords();GetComponent<PythonGrowthPanel>()?.Apply(Client.CurrentState);GetComponent<PythonGoalPanel>()?.Apply(Client.CurrentState);GetComponent<PythonGameOverPanel>()?.Apply(Client.CurrentState);base.AfterClose();}
  void LateUpdate(){if(Showing && !GameOptionsPanel.IsOpen && GetComponent<PythonSavePanel>()?.panel.activeSelf!=true)host.transform.SetAsLastSibling();}
  public override void Open(){base.Open();if(Showing)owner.panel.SetActive(false);}
 }
}
