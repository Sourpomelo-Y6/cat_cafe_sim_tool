using System.Collections.Generic;
using System.Linq;
using CatCafePreview.Standalone;
namespace CatCafePreview {
 public sealed partial class LocalCafeBackend {
  List<CafeTrustConversation> trustConversations=new List<CafeTrustConversation>();string trustSelected;
  public bool TrustConversationOpen {get;set;}
  public CafeTrustConversation[] TrustConversations {get{SyncConversations();return trustConversations.ToArray();}}
  public CafeTrustConversation TrustConversation {get{SyncConversations();return trustConversations.FirstOrDefault(r=>r.event_id==trustSelected);}}
  public string TrustConversationSelected=>TrustConversation?.event_id;
  public bool TrustResultPending=>TrustConversations.Any(CafeTrustDialogue.Pending);
  public bool TrustSafe=>Simulation.State.phase!="open" || Simulation.State.game_over!=null;
  public bool TrustBlocksProgress=>TrustSafe && (TrustConversationOpen || TrustResultPending);
  public string TrustConversationReason=>TrustResultPending?"信頼の回答・接客結果は確定済みですが、結果会話が未完了です。イベント一覧の「顧客の信頼回復」から再開してください。":Simulation.CustomerTrustWaiting?"信頼警告が回答待ちです。イベント一覧の「顧客の信頼回復」から再開してください。":Simulation.State.trust_customers.Any(r=>r.status=="recovery")?"信頼回復中：来店停止期限は変わりません。営業・休業で日程を進め、来店後の接客結果を待ちます。状況はイベント一覧の「顧客の信頼回復」で確認できます。":"";
  void SyncTrustConversations() {
   foreach(var e in Simulation.State.trust_events){var row=trustConversations.FirstOrDefault(r=>r.event_id==e.id);
    if(row==null && (e.status=="waiting" || e.status=="recovery")){row=new CafeTrustConversation{event_id=e.id,observe_result=true,response=e.status=="waiting"?CafeTrustDialogue.Start():null};trustConversations.Add(row);}
    if(row!=null)CafeTrustDialogue.Sync(row,e);
   }
  }
  public bool BeginTrustConversation(string id){var row=TrustConversations.FirstOrDefault(r=>r.event_id==id);if(!TrustSafe || Simulation.State.game_over!=null && row?.Current?.branch_id=="introduction" || row?.Current==null || row.Current.completed)return false;trustSelected=id;return true;}
  public void PrepareTrustConversation(){var rows=TrustConversations;if(TrustConversation?.Current?.completed==false && !(Simulation.State.game_over!=null && TrustConversation.Current.branch_id=="introduction"))return;foreach(var row in rows.OrderBy(r=>Simulation.State.trust_events.Find(e=>e.id==r.event_id).day).ThenBy(r=>r.event_id,System.StringComparer.Ordinal))if(BeginTrustConversation(row.event_id))return;}
  void RestoreTrustConversations(CafeTrustConversation[] rows,string selected){trustConversations=(rows??new CafeTrustConversation[0]).ToList();trustSelected=selected;TrustConversationOpen=false;}
 }
}
