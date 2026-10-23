using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview {
 public sealed class TrustConversationPanel:AcquisitionConversationPanel {
  GameObject host;PythonCustomerTrustPanel owner;
  CafeTrustConversation Row=>Backend.TrustConversation;
  CafeTrustEvent Event=>Backend.Simulation.State.trust_events.FirstOrDefault(e=>e.id==Row?.event_id);
  bool Result=>ReferenceEquals(progress,Row?.result);
  public GameObject Panel=>host;
  protected override Texture2D PortraitTexture(string id)=>Resources.Load<Texture2D>("CatCafeStandalone/customer_icon");
  protected override bool Ready=>base.Ready && !owner.Confirming;
  public void Apply(PythonCafeState value,PythonCustomerTrustPanel original) {
   owner=original;if(!Backend.TrustSafe){if(host!=null)host.SetActive(false);Backend.TrustConversationOpen=false;return;}
   Backend.PrepareTrustConversation();
   if(host==null){host=new GameObject("Trust Conversation",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));host.transform.SetParent(owner.panel.transform.parent,false);host.SetActive(false);}
   Bind(value,host,owner.title.font);if(Showing){owner.panel.SetActive(owner.Confirming);GetComponent<PythonGameOverPanel>()?.panel.SetActive(false);}
   if(owner.Confirming){owner.panel.SetActive(true);owner.panel.transform.SetAsLastSibling();owner.confirmation.transform.SetAsLastSibling();}
  }
  public bool OpenEvent(string id){if(!Backend.BeginTrustConversation(id))return false;Apply(Client.CurrentState,GetComponent<PythonCustomerTrustPanel>());Open();return Showing;}
  public bool OpenPending(){Backend.PrepareTrustConversation();if(Row?.Current==null || Row.Current.completed || !Backend.TrustSafe)return false;Apply(Client.CurrentState,GetComponent<PythonCustomerTrustPanel>());Open();return true;}
  protected override CafeConversationProgress ResolveProgress()=>Row?.Current;
  protected override bool ThreeLinePages=>true;
  protected override bool AutoRunning=>false;
  protected override void SetOpen(bool open){Backend.TrustConversationOpen=open;}
  protected override bool Choosing=>!Result && base.Choosing;
  protected override CafeConversationLine[] Lines(string branch)=>CafeTrustDialogue.Lines(Result,branch,Backend.Details.CustomerName(Event.customer_id),Backend.Simulation.Rules.trust.popularity_loss);
  protected override PythonIntakeState Request=>new PythonIntakeState{cat_id="",name=Backend.Details.CustomerName(Event.customer_id),personality=Result?"接客結果 · "+Event.resolved_day+"日目":"信頼警告 · 来店停止累計 "+Event.suspensions+"回",can_decline=true};
  protected override bool CanAnswer(bool yes)=>Event?.status=="waiting" && Client.CanRespondTrust(Event.id);
  protected override string Title=>"顧客の信頼回復";
  protected override string SceneLabel=>Event.day+"日目の警告 · "+Backend.Details.CustomerName(Event.customer_id)+" · "+(Result?Event.outcome=="recovered"?"回復済み":"永久離脱":Event.status=="waiting"?"回答待ち":Event.choice=="recover"?"回復中":"永久離脱");
  protected override string ChoicePrompt=>"信頼回復を試みますか。結果を確認して選んでください。";
  protected override string AcceptCaption=>"回復を試みる";
  protected override string DeclineCaption=>"見送る（最終確認へ）";
  protected override string AcceptEffect=>"即時回復・費用・人気変化なし。来店停止期限は短縮しません。\n来店後の接客：満足で回復、普通で継続、不満で永久離脱。\n結果会話を終えたら、営業・休業で日程を進められます。";
  protected override string DeclineEffect=>"確定すると永久離脱し、今後は来店しません。\n人気 −"+Backend.Simulation.Rules.trust.popularity_loss.ToString("0.#")+"（下限0）。経営終了になる場合があります。\n費用なし。最終確認で取消した場合は回答しません。";
  protected override string HistoryPrefix=>Result || progress.branch_id=="introduction"?"":string.Join("\n\n",CafeTrustDialogue.Lines(false,"introduction",Request.name,Backend.Simulation.Rules.trust.popularity_loss).Select(l=>(l.speaker==""?"地の文":l.speaker)+"\n"+l.text))+"\n\n";
  protected override string Notice=>Backend.TrustConversationReason;
  protected override string EndText=>"この結果は記録済みです。会話を終えて、残るイベントと店の状態を確認しましょう。";
  protected override void SendAnswer(bool yes){if(!CanAnswer(yes))return;if(yes)Client.RespondTrust(Event.id,"recover");else owner.BeginConversationIgnore(Event.id);}
  protected override void Finish(){CafeTrustDialogue.Finish(Row);}
  public override void Open(){base.Open();if(Showing)owner.panel.SetActive(false);}
  public override void Close(){if(owner.Confirming)return;base.Close();}
  protected override void AfterClose(){owner.Apply(Client.CurrentState);GetComponent<PythonGrowthPanel>()?.Apply(Client.CurrentState);GetComponent<PythonGoalPanel>()?.Apply(Client.CurrentState);GetComponent<PythonGameOverPanel>()?.Apply(Client.CurrentState);base.AfterClose();}
  void LateUpdate(){if(Showing && !owner.Confirming && !GameOptionsPanel.IsOpen && GetComponent<PythonSavePanel>()?.panel.activeSelf!=true)host.transform.SetAsLastSibling();}
 }
}
