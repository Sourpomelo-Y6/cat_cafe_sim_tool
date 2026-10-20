using System;
using System.Linq;
namespace CatCafePreview.Standalone {
 [Serializable] public sealed class CafeTrustConversation {
  public string event_id; public CafeConversationProgress response,result; public bool observe_result;
  public CafeConversationProgress Current=>response?.completed==false?response:result;
 }
 // Viewing metadata only. CafeCustomerTrust owns all effects.
 public static class CafeTrustDialogue {
  public const string Id="customer.trust.v1";
  public static CafeConversationProgress Start(bool result=false)=>new CafeConversationProgress{conversation_id=Id+(result?".result":".response"),branch_id="introduction",viewed_branches=new string[0]};
  public static CafeConversationLine[] Lines(bool result,string branch,string name,double loss) {
   if(result)return branch=="recovered"?new[]{
    new CafeConversationLine("",name+"への接客は「満足」と記録された。信頼は回復済みになった。"),
    new CafeConversationLine("あなた","今回の接客結果を受け止めて、これからも一回ずつ丁寧に対応していきましょう。"),
    new CafeConversationLine("","不満の値は既定の回復値へ反映済み。来店停止の累計は残るため、次の準備日に再び信頼警告が出る場合がある。")}:new[]{
    new CafeConversationLine("",name+"への接客は「不満」と記録された。回復中だった信頼は、永久離脱という結果になった。"),
    new CafeConversationLine("あなた",name+"は今後の来店対象から外れます。この結果を確認しておきましょう。"),
    new CafeConversationLine("","永久離脱による人気低下（"+loss.ToString("0.#")+"、下限0）と経営終了判定は反映済み。この会話で再び差し引くことはない。")};
   if(branch=="recover")return new[]{
    new CafeConversationLine("あなた",name+"の信頼回復を試みます。"),
    new CafeConversationLine("","回答は確定した。費用と人気の即時変化はなく、信頼は回復中。来店停止期限は変わらず、再来店もまだ確定していない。"),
    new CafeConversationLine("あなた","会話を終えたら営業や休業で日程を進めましょう。来店後の接客が満足なら回復、普通なら回復中を継続、不満なら永久離脱になります。")};
   if(branch=="ignore")return new[]{
    new CafeConversationLine("あなた",name+"の信頼回復は見送ります。"),
    new CafeConversationLine("",name+"の永久離脱が確定した。今後の来店対象から外れ、人気低下（"+loss.ToString("0.#")+"、下限0）と経営終了判定は反映済み。"),
    new CafeConversationLine("あなた","この回答を記録しました。会話を終えて、店の状態を確認しましょう。")};
   return new[]{new CafeConversationLine("",name+"は来店停止を繰り返し、信頼警告の対象になっている。"),
    new CafeConversationLine("あなた",name+"への対応方針を決めましょう。回復を試みても、ここで直ちに信頼が戻るわけではありません。"),
    new CafeConversationLine("","回復を試みる場合は、来店停止期限を変えず、来店後の接客結果を待つ。見送る場合は永久離脱となる。選択の結果を確認して決めよう。")};
  }
  public static void Sync(CafeTrustConversation r,CafeTrustEvent e) {
   if(e==null)return;
   if(r.response?.branch_id=="introduction" && e.choice!=null){r.response.branch_id=e.choice;r.response.node=r.response.page_offset=0;}
   if(r.observe_result && e.choice=="recover" && e.status=="resolved" && r.result==null){r.result=Start(true);r.result.branch_id=e.outcome;}
  }
  public static bool Pending(CafeTrustConversation r)=>new[]{r.response,r.result}.Any(p=>p!=null && !p.completed && p.branch_id!="introduction");
  public static void Finish(CafeTrustConversation r){var p=r.Current;if(p==null || p.branch_id=="introduction" || p.node<3)return;p.completed=true;p.page_offset=0;p.viewed_branches=new[]{p.branch_id};}
  public static void Validate(CafeTrustConversation[] rows,string selected,CafeSnapshot s) {
   rows=rows??new CafeTrustConversation[0];
   if(rows.Length>10000 || rows.Any(r=>r==null) || rows.Select(r=>r.event_id).Distinct().Count()!=rows.Length || !string.IsNullOrEmpty(selected) && !rows.Any(r=>r.event_id==selected))throw new ArgumentException("信頼会話の一覧が不正です。");
   foreach(var r in rows){var e=s.trust_events.SingleOrDefault(x=>x.id==r.event_id);
    if(e==null || !r.observe_result || r.response==null && (e.status=="waiting" || e.choice=="ignore") || r.result!=null && (!r.observe_result || e.choice!="recover" || e.status!="resolved"))throw new ArgumentException("信頼会話の対象が不正です。");
    ValidatePhase(r.response,false,e.choice??"introduction");ValidatePhase(r.result,true,e.outcome);
    if(r.observe_result && e.choice=="recover" && e.status=="resolved" && r.result==null)throw new ArgumentException("信頼の結果会話がありません。");
   }
  }
  static void ValidatePhase(CafeConversationProgress p,bool result,string branch) {
   if(p==null)return;var lines=Lines(result,branch,"",10);
   if(p.conversation_id!=Id+(result?".result":".response") || p.branch_id!=branch || p.node<0 || p.node>lines.Length || p.page_offset<0 || p.node==lines.Length && p.page_offset!=0 || p.node<lines.Length && p.page_offset>lines[p.node].text.Length+256 || p.viewed_branches==null || (p.completed?(branch=="introduction" || p.node!=lines.Length || p.viewed_branches.Length!=1 || p.viewed_branches[0]!=branch):p.viewed_branches.Length!=0))throw new ArgumentException("信頼会話の進行が不正です。");
  }
 }
}
