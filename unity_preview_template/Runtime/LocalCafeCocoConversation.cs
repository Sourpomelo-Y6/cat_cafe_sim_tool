using System.Collections.Generic;
using System.Linq;
using CatCafePreview.Standalone;
namespace CatCafePreview {
 public sealed partial class LocalCafeBackend {
  List<CafeCocoConversation> cocoConversations=new List<CafeCocoConversation>();
  public CafeCocoConversation[] CocoConversations {get{SyncConversations();return cocoConversations.ToArray();}}
  public CafeCocoConversation CocoConversation=>CocoConversations.FirstOrDefault(r=>!r.progress.completed);
  void RestoreCocoConversations(CafeCocoConversation[] rows){cocoConversations=(rows??new CafeCocoConversation[0]).ToList();}
  void SyncCocoConversations(){
   var v=Simulation.State.visiting_cat;
   // Old saves can view a new meeting while still visiting; accepted saves never invent the past.
   if((v.status=="visiting" || v.status=="ready") && !cocoConversations.Any(r=>r.attempt==0))cocoConversations.Insert(0,CafeCocoDialogue.Start(true));
   if(v.status=="ready" && !cocoConversations.Any(r=>r.attempt>0))cocoConversations.Add(CafeCocoDialogue.Start(false,1));
   var pending=cocoConversations.FirstOrDefault(r=>r.progress.branch_id=="introduction");
   if(v.status=="accepted" && pending!=null){pending.progress.branch_id="accepted";pending.progress.node=pending.progress.page_offset=0;pending.result="迎える費用 "+Simulation.Rules.visiting_cat.cost.ToString("0.#")+" を精算しました。精算後の資金："+Simulation.State.funds.ToString("0.#")+"。健康・体力全回復、疲労・ストレス・プレイヤー好感度0、休養予定で加入しました。";}
  }
  public void PrepareCocoConversation(bool repeat){var current=CocoConversation;if(current==null && repeat && Simulation.State.visiting_cat.status=="ready")cocoConversations.Add(CafeCocoDialogue.Start(false,cocoConversations.Max(r=>r.attempt)+1));}
  public void CocoLater(){var r=CocoConversation;if(r==null || r.progress.branch_id!="introduction" || Simulation.State.visiting_cat.status!="ready")return;r.progress.branch_id="later";r.progress.node=r.progress.page_offset=0;}
 }
}
