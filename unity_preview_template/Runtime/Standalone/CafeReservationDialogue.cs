using System;
using System.Linq;
namespace CatCafePreview.Standalone {
 [Serializable] public sealed class CafeReservationConversation {
  public string event_id,name,description,feature,service_summary;
  public int visit_day,arrival_tick,open_up_count; public double bonus,popularity_bonus;
  public CafeConversationProgress response,result; public bool observe_result,popularity_settled;
  public CafeConversationProgress Current=>response?.completed==false?response:result;
  public string Conditions=>"担当猫の条件："+feature+"。接客中に「心を開く」"+open_up_count+"回以上。";
  public string Schedule=>"来店日："+visit_day+"日目、来店刻み："+arrival_tick+"。";
  public string Rewards=>"条件達成時の追加料金 ＋"+bonus.ToString("0.#")+"、人気 ＋"+popularity_bonus.ToString("0.#")+"。";
 }
 // Display metadata only. All answers and rewards remain owned by CafeSimulation.
 public static class CafeReservationDialogue {
  public const string Id="reservation.v1";
  public static CafeConversationProgress Start(bool result=false)=>new CafeConversationProgress{conversation_id=Id+(result?".result":".response"),branch_id="introduction",viewed_branches=new string[0]};
  public static string EventId(CafeReservation e)=>e.offered_day>0?"reservation-"+e.offered_day:null;
  public static CafeConversationLine[] Lines(CafeReservationConversation r,bool result,string branch) {
   if(result)return new[]{
    new CafeConversationLine("",r.name+"の予約は「"+(branch=="success"?"条件達成":branch=="failure"?"条件未達":"未接客")+"」と確定した。"),
    new CafeConversationLine("",r.service_summary??"接客の詳細記録はありません。"),
    new CafeConversationLine("あなた",branch=="success"?"追加料金 ＋"+r.bonus.ToString("0.#")+"は接客処理に反映済みです。人気加点 ＋"+r.popularity_bonus.ToString("0.#")+"は"+(r.popularity_settled?"閉店処理に反映済みです（人気上限まで）。":"閉店前のため未反映です。")+"今回の確定記録を確認しましょう。":"今回の追加料金と人気加点はありません。確定した結果を確認して、店の仕事へ戻りましょう。")};
   if(branch=="accept")return new[]{new CafeConversationLine("あなた",r.name+"の予約を引き受けました。"),new CafeConversationLine("",r.Schedule+r.Conditions),new CafeConversationLine("あなた","来店当日は休業できません。回答による資金・人気の変化はありません。この回答結果を読了したら、営業や日程を進めて準備しましょう。接客結果はまだ確定していません。")};
   if(branch=="decline")return new[]{new CafeConversationLine("あなた",r.name+"の予約を辞退しました。"),new CafeConversationLine("","辞退のペナルティはなく、資金・人気は変わらない。この予約による来店と追加報酬はない。"),new CafeConversationLine("あなた","回答を記録しました。会話を終えて営業準備へ戻りましょう。")};
   return new[]{new CafeConversationLine("",r.name+"から特別予約の依頼が届いた。"+r.description),new CafeConversationLine("あなた",r.Schedule+r.Conditions),new CafeConversationLine("",r.Rewards+"引き受けた来店当日は休業できない。辞退のペナルティはなく、回答だけでは資金・人気は変わらない。条件を満たせる猫がいない場合も、達成が保証されるわけではない。")};
  }
  public static bool Pending(CafeReservationConversation r)=>new[]{r.response,r.result}.Any(p=>p!=null && !p.completed && p.branch_id!="introduction");
  public static void Sync(CafeReservationConversation r,CafeReservation e){if(r.response?.branch_id=="introduction" && e.choice!=null){r.response.branch_id=e.choice;r.response.node=r.response.page_offset=0;}if(r.observe_result && e.result!=null && r.result==null){r.result=Start(true);r.result.branch_id=e.result;}}
  public static void Finish(CafeReservationConversation r){var p=r.Current;if(p==null || p.branch_id=="introduction" || p.node<3)return;p.completed=true;p.page_offset=0;p.viewed_branches=new[]{p.branch_id};}
  public static void Validate(CafeReservationConversation[] rows,string selected,CafeSnapshot s){
   rows=rows??new CafeReservationConversation[0];if(rows.Length>1 || rows.Any(r=>r==null) || !string.IsNullOrEmpty(selected) && !rows.Any(r=>r.event_id==selected))throw new ArgumentException("予約会話の一覧が不正です。");
   foreach(var r in rows){var e=s.reservation;if(r.event_id!=EventId(e) || e.status=="untriggered" || !r.observe_result || r.visit_day!=e.visit_day || string.IsNullOrEmpty(r.name) || string.IsNullOrEmpty(r.feature) || r.open_up_count<0 || r.bonus<0 || r.popularity_bonus<0 || double.IsNaN(r.bonus) || double.IsInfinity(r.bonus) || double.IsNaN(r.popularity_bonus) || double.IsInfinity(r.popularity_bonus) || r.response==null && e.status=="waiting" || r.result!=null && e.result==null || e.result!=null && r.result==null)throw new ArgumentException("予約会話の対象が不正です。");ValidatePhase(r,r.response,false,e.choice??"introduction");ValidatePhase(r,r.result,true,e.result);}
  }
  static void ValidatePhase(CafeReservationConversation r,CafeConversationProgress p,bool result,string branch){if(p==null)return;var lines=Lines(r,result,branch);if(p.conversation_id!=Id+(result?".result":".response") || p.branch_id!=branch || p.node<0 || p.node>lines.Length || p.page_offset<0 || p.node==lines.Length && p.page_offset!=0 || p.node<lines.Length && p.page_offset>lines[p.node].text.Length+256 || p.viewed_branches==null || (p.completed?(branch=="introduction" || p.node!=lines.Length || p.viewed_branches.Length!=1 || p.viewed_branches[0]!=branch):p.viewed_branches.Length!=0))throw new ArgumentException("予約会話の進行が不正です。");}
 }
}
