using System;
using System.Linq;
namespace CatCafePreview.Standalone {
 [Serializable] public sealed class CafeEquipmentConversation {
  public int day;
  public string event_id,explanation,result,repair_effect,patch_effect;
  public CafeConversationProgress progress;
 }
 public static class CafeEquipmentDialogue {
  public const string Id="equipment.trouble.v1";
  public static string EventId(int day)=>"equipment-trouble-"+day;
  public static CafeConversationProgress Start()=>new CafeConversationProgress{conversation_id=Id,branch_id="introduction",viewed_branches=new string[0]};
  public static CafeConversationLine[] Lines(CafeEquipmentConversation r,string branch)=>branch=="introduction"?new[]{
   new CafeConversationLine("","営業準備中、設備トラブルへの対応が必要になった。"),
   new CafeConversationLine("あなた","修理するか、応急処置で営業するか。それとも今日は休業するか。費用と店の状態を確認しましょう。"),
   new CafeConversationLine("",r.explanation)}:new[]{
   new CafeConversationLine("あなた",r.day+"日目の設備トラブルは「"+(branch=="repair"?"修理":branch=="patch"?"応急処置":"休業")+"」で対応しました。"),
   new CafeConversationLine("",r.result),
   new CafeConversationLine("あなた","対応は記録済みです。この会話を終えて、残る帰還待ち・成長・目標結果と店の状態を確認しましょう。閉じてもイベント一覧の「店舗イベント」から再開できます。")};
  public static void Finish(CafeEquipmentConversation r){var p=r.progress;if(p.branch_id=="introduction" || p.node<Lines(r,p.branch_id).Length)return;p.completed=true;p.page_offset=0;p.viewed_branches=new[]{p.branch_id};}
  public static void Validate(CafeEquipmentConversation[] rows,string selected,CafeSnapshot s){
   rows=rows??new CafeEquipmentConversation[0];if(rows.Any(r=>r==null) || rows.Select(r=>r.event_id).Distinct().Count()!=rows.Length || !string.IsNullOrEmpty(selected) && !rows.Any(r=>r.event_id==selected))throw new ArgumentException("設備会話の一覧が不正です。");
   foreach(var r in rows){var e=s.store_events.FirstOrDefault(x=>x.day==r.day && x.type=="trouble");var p=r.progress;
    if(e==null || r.event_id!=EventId(r.day) || p==null || p.conversation_id!=Id || p.branch_id!=(e.choice??"introduction") || string.IsNullOrEmpty(r.repair_effect) || string.IsNullOrEmpty(r.patch_effect) || string.IsNullOrEmpty(r.explanation) || r.explanation.Length>10000 || e.choice!=null && string.IsNullOrEmpty(r.result))throw new ArgumentException("設備会話の案件が不正です。");
    var lines=Lines(r,p.branch_id);if(p.node<0 || p.node>lines.Length || p.page_offset<0 || p.node==lines.Length && p.page_offset!=0 || p.node<lines.Length && p.page_offset>lines[p.node].text.Length+256 || p.viewed_branches==null || (p.completed?(p.branch_id=="introduction" || p.node!=lines.Length || p.viewed_branches.Length!=1 || p.viewed_branches[0]!=p.branch_id):p.viewed_branches.Length!=0))throw new ArgumentException("設備会話の進行が不正です。");
   }
  }
 }
}
