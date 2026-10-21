using System;
using System.Linq;
namespace CatCafePreview.Standalone {
 [Serializable] public sealed class CafeMissingCase {
  public string id,cat_id,reason; public int departed_day,resolved_day;
  public bool kitten; public double cost,funds_after,stamina,stress;
  public CafeConversationProgress notice,waiting,confirmation,result;
 }
 public static class CafeMissingDialogue {
  public const string Id="cafe.missing.v1";
  public static CafeConversationProgress Start(string phase,string branch)=>new CafeConversationProgress{conversation_id=Id+"."+phase,branch_id=branch,viewed_branches=new string[0]};
  public static CafeConversationLine[] Lines(CafeMissingCase e,string phase,string name) {
   if(phase=="notice")return new[]{new CafeConversationLine("",name+"が店から家出した。"),new CafeConversationLine("あなた",e.reason=="stress"?"接客後のストレスが家出の基準に達した記録があります。今は帰還を待ちましょう。":"家出日や原因の記録はありません。今分かる状態を確認しましょう。"),new CafeConversationLine("","家出中は接客・交流・派遣に参加できない。営業か休業で日程を進められる。居場所や帰還結果は、まだ確認できていない。")};
   if(phase=="waiting")return new[]{new CafeConversationLine("あなた",name+"は、まだ家出中です。帰還までの残り閉店回数は記録一覧で確認できます。"),new CafeConversationLine("","この状況確認は任意。会話を閉じて営業や休業を進めよう。出勤できる猫がいなくても、営業準備の休業を利用できる。")};
   if(phase=="confirmation")return new[]{new CafeConversationLine("",name+"が帰還待ちになった。"),new CafeConversationLine("あなた","子猫の有無と費用は、帰還の記録に公開されています。一覧で内容と確認後の資金を確かめてから、帰還を確定しましょう。"),new CafeConversationLine("","この会話だけでは帰還・費用は確定しない。会話を閉じると記録一覧へ戻る。帰還を確定するまで営業・休業は進められない。")};
   return new[]{new CafeConversationLine("",name+"の帰還が確定し、店に戻った。"),new CafeConversationLine("あなた",e.kitten?"子猫の引き渡し費用は "+e.cost.ToString("0.#")+"。子猫は在籍猫に追加されません。":"子猫の引き渡しはありません。費用は0です。"),new CafeConversationLine("","精算直後の資金："+e.funds_after.ToString("0.#")+"。帰還時の体力："+e.stamina.ToString("0.#")+"、ストレス："+e.stress.ToString("0.#")+"。体調・疲労は引き継ぎ、休養予定。猫一覧で状態と出勤設定を確認しよう。")};
  }
  public static void Finish(CafeConversationProgress p){p.completed=true;p.page_offset=0;p.viewed_branches=new[]{p.branch_id};}
  public static void Validate(CafeSnapshot s){
   if(s.missing_cases==null)s.missing_cases=new System.Collections.Generic.List<CafeMissingCase>();
   if(s.missing_cases.Count>10000 || s.missing_cases.Any(e=>e==null || string.IsNullOrEmpty(e.id) || !s.cats.Any(c=>c.id==e.cat_id) || e.departed_day<0 || e.departed_day>s.day || e.resolved_day<0 || e.resolved_day>s.day || double.IsNaN(e.cost) || double.IsInfinity(e.cost) || e.cost<0 || double.IsNaN(e.funds_after) || double.IsInfinity(e.funds_after)) || s.missing_cases.Select(e=>e.id).Distinct().Count()!=s.missing_cases.Count)throw new ArgumentException("家出案件の記録が不正です。");
   foreach(var e in s.missing_cases)foreach(var phase in new[]{"notice","waiting","confirmation","result"}){var p=Progress(e,phase);if(p==null)continue;int count=Lines(e,phase,"").Length;if(p.branch_id!=(phase=="notice"?"departed":phase=="waiting"?"waiting":phase=="confirmation"?"waiting_return":e.kitten?"kitten":"alone") || p.conversation_id!=Id+"."+phase || p.node<0 || p.node>count || p.page_offset<0 || p.page_offset>10000 || p.viewed_branches==null || p.completed && (p.node!=count || p.viewed_branches.Length!=1 || p.viewed_branches[0]!=p.branch_id) || !p.completed && p.viewed_branches.Length!=0 || phase=="result" && e.resolved_day==0)throw new ArgumentException("家出会話の進行が不正です。");}
  }
  public static CafeConversationProgress Progress(CafeMissingCase e,string phase)=>phase=="notice"?e.notice:phase=="waiting"?e.waiting:phase=="confirmation"?e.confirmation:e.result;
 }
}
