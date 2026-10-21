using System;
using System.Linq;
namespace CatCafePreview.Standalone {
 [Serializable] public sealed class CafeCocoConversation {
  public int attempt;public string result;public CafeConversationProgress progress;
 }
 public static class CafeCocoDialogue {
  public const string MeetingId="coco.meeting.v1",AdmissionId="coco.admission.v1";
  public static CafeCocoConversation Start(bool meeting,int attempt=0)=>new CafeCocoConversation{attempt=attempt,progress=new CafeConversationProgress{conversation_id=meeting?MeetingId:AdmissionId,branch_id=meeting?"meeting":"introduction",viewed_branches=new string[0]}};
  public static CafeConversationLine[] Lines(CafeCocoConversation r,string branch)=>branch=="meeting"?new[]{
   new CafeConversationLine("","店先に、ココが姿を見せた。少し距離を置いて、こちらを見ている。"),
   new CafeConversationLine("あなた","ココ。今はここから、こんにちは。近づきすぎず、少しずつ慣れてもらいましょう。"),
   new CafeConversationLine("","ココは距離を大切にする、のんびり屋の猫。店先での交流は無料で、準備中に1日1回。必要な回数は店先の交流画面で確認できる。プレイヤー交流のセット数とは別の記録だ。交流せず営業や休業へ進んでも、これまでの進捗は残る。")}:branch=="introduction"?new[]{
   new CafeConversationLine("","交流を重ね、ココはこの店先に十分慣れている。迎えるための準備を確かめよう。"),
   new CafeConversationLine("あなた","ココのペースは、これからも大切にしたい。費用と飼育スペースを確認して、迎えるか決めましょう。"),
   new CafeConversationLine("","今は迎えなくても、候補と交流の記録は残ります。資金や飼育枠が足りなければ、閉じて通常の準備に戻れます。")}:branch=="accepted"?new[]{
   new CafeConversationLine("あなた","ココ、ようこそ。ここでも、少しずつ慣れていこうね。"),
   new CafeConversationLine("",r.result),
   new CafeConversationLine("","店先で重ねた交流は、加入の記録として残った。出勤するか休養するかは、営業準備の猫一覧から選べる。")}:new[]{
   new CafeConversationLine("あなた","今回は、まだ迎えずにおきましょう。また準備ができたときに。"),
   new CafeConversationLine("","ココは引き続き店先の候補です。交流進捗・資金・飼育枠は変わりません。今日の交流や見送りの回数も消費しません。店先の画面から、いつでも迎える相談を再開できます。")};
  public static void Finish(CafeCocoConversation r){var p=r.progress;if(p.branch_id=="introduction" || p.node<Lines(r,p.branch_id).Length)return;p.completed=true;p.page_offset=0;p.viewed_branches=new[]{p.branch_id};}
  public static void Validate(CafeCocoConversation[] rows,CafeSnapshot s){
   rows=rows??new CafeCocoConversation[0];if(rows.Any(r=>r==null) || rows.Count(r=>r.attempt==0)>1 || rows.Select(r=>r.attempt).Distinct().Count()!=rows.Length)throw new ArgumentException("ココの会話一覧が不正です。");
   foreach(var r in rows){var p=r.progress;bool meeting=r.attempt==0;
    if(r.attempt<0 || p==null || p.conversation_id!=(meeting?MeetingId:AdmissionId) || !(meeting?new[]{"meeting"}:new[]{"introduction","accepted","later"}).Contains(p.branch_id) || s.visiting_cat.status=="untriggered" || !meeting && s.visiting_cat.status!="ready" && s.visiting_cat.status!="accepted" || p.branch_id=="accepted" && (s.visiting_cat.status!="accepted" || string.IsNullOrEmpty(r.result)) || s.visiting_cat.status=="accepted" && !meeting && p.branch_id=="introduction")throw new ArgumentException("ココの会話と来訪状態が一致しません。");
    var lines=Lines(r,p.branch_id);if(p.node<0 || p.node>lines.Length || p.page_offset<0 || p.node==lines.Length && p.page_offset!=0 || p.node<lines.Length && p.page_offset>lines[p.node].text.Length+256 || p.viewed_branches==null || (p.completed?(p.branch_id=="introduction" || p.node!=lines.Length || p.viewed_branches.Length!=1 || p.viewed_branches[0]!=p.branch_id):p.viewed_branches.Length!=0))throw new ArgumentException("ココの会話位置が不正です。");
   }
  }
 }
}
