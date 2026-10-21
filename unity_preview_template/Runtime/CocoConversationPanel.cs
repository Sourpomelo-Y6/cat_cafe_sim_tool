using UnityEngine;
using CatCafePreview.Standalone;
namespace CatCafePreview {
 public sealed class CocoConversationPanel:AcquisitionConversationPanel {
  bool ended;GameObject host;PythonVisitingCatPanel owner;CafeCocoConversation Row=>Backend.CocoConversation;
  public void Apply(PythonCafeState value,PythonVisitingCatPanel original){owner=original;if(host==null){host=new GameObject("Coco Conversation",typeof(RectTransform),typeof(UnityEngine.UI.Image));host.transform.SetParent(owner.panel.transform.parent,false);host.SetActive(false);}Bind(value,host,owner.title.font);if(Showing){owner.panel.SetActive(false);foreach(var graphic in host.GetComponentsInChildren<UnityEngine.UI.Graphic>())graphic.SetAllDirty();Canvas.ForceUpdateCanvases();}}
  public bool OpenPending(bool repeat){Backend.PrepareCocoConversation(repeat);Apply(Client.CurrentState,GetComponent<PythonVisitingCatPanel>());if(Row==null)return false;Open();return Showing;}
  protected override CafeConversationProgress ResolveProgress()=>Row?.progress;
  protected override bool ThreeLinePages=>true;
  protected override bool AutoRunning=>Client.AutoPlayExecuting;
  protected override bool Available=>base.Available && state.can_set_shifts && !state.closed && state.game_over==null && (state.required_action=="" || state.required_action==null) && !Backend.ConversationBlocksProgress;
  protected override void SetOpen(bool open){}
  protected override CafeConversationLine[] Lines(string branch)=>CafeCocoDialogue.Lines(Row,branch);
  protected override PythonIntakeState Request=>new PythonIntakeState{cat_id=state.visiting_cat.content_cat_id,name=state.visiting_cat.name,personality=state.visiting_cat.personality,can_decline=true};
  protected override string Title=>progress.conversation_id==CafeCocoDialogue.MeetingId?"ココとの初対面":"ココを迎える相談";
  protected override string SceneLabel=>"店先 · 任意の会話";
  protected override string ChoicePrompt=>"費用・残金・飼育枠と、加入後の状態を確認してください。今回は迎えなくても進捗は残ります。";
  protected override bool CanAnswer(bool yes)=>yes?Client.CanRespondVisiting("accept"):Ready && state.visiting_cat.status=="ready";
  protected override string AcceptEffect=>"費用 "+state.visiting_cat.cost+" / 所持金 "+state.funds+" → 残金 "+state.visiting_cat.funds_after+"\n"+state.visiting_cat.housing+"\n健康・体力全回復、疲労・ストレス・好感度0、休養予定で加入。"+(state.visiting_cat.can_accept?"":"\n選べない理由："+state.visiting_cat.accept_reason);
  protected override string DeclineCaption=>"今回は迎えない";
  protected override string DeclineEffect=>"候補・交流進捗を保持。支出なし。\n日次の「今日は見送る」とは別で、当日の回数を消費しません。";
  protected override void SendAnswer(bool yes){if(!CanAnswer(yes))return;if(yes)AskConfirmation("ココを迎えますか？\n"+AcceptEffect,()=>{if(Client.CanRespondVisiting("accept"))Client.RespondVisiting("accept");});else Backend.CocoLater();}
  protected override void Finish(){CafeCocoDialogue.Finish(Row);ended=true;}
  protected override string HistoryPrefix=>progress.branch_id=="accepted" || progress.branch_id=="later"?string.Join("\n\n",System.Array.ConvertAll(CafeCocoDialogue.Lines(Row,"introduction"),l=>l.text))+"\n\n":"";
  protected override string Notice=>"閉じても会話位置と交流進捗は残ります。店先から再開できます。";
  protected override string EndText=>"店先の画面に戻りましょう。営業・休業へ進むこともできます。";
  protected override void AfterClose(){owner.Apply(Client.CurrentState);if(ended && !Showing)owner.ShowDaily();ended=false;base.AfterClose();}
 }
}
