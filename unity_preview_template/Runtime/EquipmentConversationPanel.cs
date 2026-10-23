using UnityEngine;
using CatCafePreview.Standalone;
namespace CatCafePreview {
 public sealed class EquipmentConversationPanel:AcquisitionConversationPanel {
  GameObject host;PythonStoreEventPanel owner;
  CafeEquipmentConversation Row=>Backend.EquipmentConversation;
  bool Support=>Row!=null && CafeEquipmentDialogue.IsSupport(Row);
  public void Apply(PythonCafeState value,PythonStoreEventPanel original){owner=original;Backend.PrepareEquipmentConversation();if(host==null){host=new GameObject("Equipment Conversation",typeof(RectTransform),typeof(UnityEngine.UI.Image));host.transform.SetParent(owner.panel.transform.parent,false);host.SetActive(false);}Bind(value,host,owner.title.font);if(Showing)owner.panel.SetActive(false);}
  public bool OpenPending(){Apply(Client.CurrentState,GetComponent<PythonStoreEventPanel>());if(Row==null || Row.progress.completed)return false;Open();return true;}
  protected override CafeConversationProgress ResolveProgress()=>Row?.progress;
  protected override bool Available=>base.Available && (Row.progress.branch_id!="introduction" || string.IsNullOrEmpty(state.required_action) || state.required_action=="store_event");
  protected override bool ThreeLinePages=>true;
  protected override bool AutoRunning=>false;
  protected override bool Ready=>base.Ready && GetComponent<PythonDayOffPanel>()?.panel.activeSelf!=true;
  protected override void SetOpen(bool open){Backend.EquipmentConversationOpen=open;}
  protected override CafeConversationLine[] Lines(string branch)=>CafeEquipmentDialogue.Lines(Row,branch);
  protected override PythonIntakeState Request=>new PythonIntakeState{cat_id="",name=CafeEquipmentDialogue.Title(Row),personality=Row.day+"日目の案件",can_decline=true};
  protected override string Title=>CafeEquipmentDialogue.Title(Row);
  protected override string SceneLabel=>Row.event_id+" · "+(progress.branch_id=="introduction"?"回答待ち":"対応確定・結果会話");
  protected override string ChoicePrompt=>Support?"支援額・支出後の資金・閉店時の人気加算を確認してください。選択後に最終確認します。":"費用・停止席・休業時の変化を確認して対応を選んでください。休業は確認画面を経て確定します。";
  PythonEventChoice Option(int index)=>state.store_event?.kind==(Support?"support":"trouble") && state.store_event.day==Row.day?state.store_event.choices[index]:null;
  string Effect(int index){var o=Option(index);return o==null?"回答済みです。":o.detail+(o.can_select?"":"\n選べない理由："+o.reason);}
  protected override bool CanAnswer(bool yes)=>Client.CanRespondStore(yes?0:1) && Option(yes?0:1)!=null;
  protected override string AcceptCaption=>Support?"支援":"修理";
  protected override string DeclineCaption=>Support?"少額支援":"応急処置";
  protected override string AcceptEffect=>Effect(0);
  protected override string DeclineEffect=>Effect(1);
  protected override bool HasThirdChoice=>true;
  protected override string ThirdCaption=>Support?"見送り":"休業（費用と猫の変化を確認）";
  protected override string ThirdEffect=>Effect(2);
  protected override bool CanThird=>Client.CanRespondStore(2) && Option(2)!=null;
  protected override void SendThird(){if(Support)ConfirmSupport(2);else {Close();GetComponent<PythonDayOffPanel>().Open();}}
  protected override void SendAnswer(bool yes){if(CanAnswer(yes)){if(Support)ConfirmSupport(yes?0:1);else Client.RespondStore(yes?0:1);}}
  void ConfirmSupport(int index){var option=Option(index);if(option==null || !Client.CanRespondStore(index))return;AskConfirmation(option.label+"で回答しますか？\n"+option.detail,()=>{if(Client.CanRespondStore(index) && Option(index)!=null)Client.RespondStore(index);});}
  protected override void Finish()=>CafeEquipmentDialogue.Finish(Row);
  protected override string HistoryPrefix=>progress.branch_id=="introduction"?"":string.Join("\n\n",System.Array.ConvertAll(CafeEquipmentDialogue.Lines(Row,"introduction"),l=>l.text))+"\n\n";
  protected override string Notice=>Backend.EquipmentConversationReason;
  protected override string EndText=>"対応は記録済みです。会話を終えると、残る確認待ちへ戻ります。";
  protected override void AfterClose(){owner.Apply(Client.CurrentState);GetComponent<PythonGrowthPanel>()?.Apply(Client.CurrentState);GetComponent<PythonGoalPanel>()?.Apply(Client.CurrentState);GetComponent<PythonGameOverPanel>()?.Apply(Client.CurrentState);base.AfterClose();}
 }
}
