using System.Linq;
using CatCafePreview.Standalone;
namespace CatCafePreview {
 public sealed partial class LocalCafeBackend {
  string missingSelected,missingPhase;public bool MissingConversationOpen {get;set;}
  public bool MissingSafe=>Simulation.State.phase!="open" || Simulation.State.game_over!=null;
  public bool MissingResultPending {get{SyncConversations();return Simulation.State.missing_cases.Any(e=>e.result!=null && !e.result.completed);}}
  public bool MissingBlocksProgress=>MissingSafe && MissingResultPending;
  public string MissingConversationReason=>MissingBlocksProgress?"猫の帰還は確定済みですが結果会話が未完了です。イベント一覧の「猫の家出・帰還」から対象案件を選び、会話を最後まで進めてください。":MissingWaiting?"帰還待ちの猫がいます。イベント一覧の「猫の家出・帰還」で対象を選び、会話を閉じて費用と残金を確認し、「帰還と費用を確認する」を押してください。":"";
  public CafeMissingCase MissingCase=>Simulation.State.missing_cases.FirstOrDefault(e=>e.id==missingSelected);
  public string MissingPhase=>missingPhase;
  public CafeConversationProgress MissingProgress=>MissingCase==null?null:CafeMissingDialogue.Progress(MissingCase,missingPhase);
  void SyncMissingConversations(){
   var s=Simulation.State;if(s.missing_cases==null)s.missing_cases=new System.Collections.Generic.List<CafeMissingCase>();
   foreach(var c in s.cats.Where(c=>c.activity=="missing" && !Simulation.DispatchMissing(s,c.id))){var e=s.missing_cases.LastOrDefault(x=>x.cat_id==c.id && x.resolved_day==0);if(e==null){e=new CafeMissingCase{id="legacy-missing-"+s.day+"-"+c.id,cat_id=c.id};s.missing_cases.Add(e);}if(e.notice==null)e.notice=CafeMissingDialogue.Start("notice","departed");if(c.missing_remaining==0 && e.confirmation==null)e.confirmation=CafeMissingDialogue.Start("confirmation","waiting_return");}
   foreach(var e in s.missing_cases.Where(e=>e.resolved_day>0 && e.result==null))e.result=CafeMissingDialogue.Start("result",e.kitten?"kitten":"alone");
  }
  public bool OpenPendingMissing(){SyncConversations();var e=Simulation.State.missing_cases.FirstOrDefault(x=>x.result!=null && !x.result.completed);return e!=null && BeginMissingConversation(e.id);}
  public bool BeginMissingConversation(string id){SyncConversations();var e=Simulation.State.missing_cases.FirstOrDefault(x=>x.id==id);if(e==null || !MissingSafe)return false;missingSelected=id;missingPhase=e.result!=null?"result":e.confirmation!=null?"confirmation":e.notice?.completed==false?"notice":"waiting";if(missingPhase=="waiting" && (e.waiting==null || e.waiting.completed))e.waiting=CafeMissingDialogue.Start("waiting","waiting");return MissingProgress!=null && !MissingProgress.completed;}
 }
}
