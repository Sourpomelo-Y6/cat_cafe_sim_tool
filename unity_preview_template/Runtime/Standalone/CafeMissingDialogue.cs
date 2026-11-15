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
  public static string NamedCause(string reason)=>CafeSimulation.NamedMissingCustomer(reason)==null?null:reason.EndsWith("_refused",StringComparison.Ordinal)?"返還拒否":"連れ去り";
  public static CafeConversationProgress Start(string phase,string branch)=>new CafeConversationProgress{conversation_id=Id+"."+phase,branch_id=branch,viewed_branches=new string[0]};
  public static CafeConversationLine[] Lines(CafeMissingCase e,string phase,string name,
      CafeNamedDispatchSettings[] settings=null,Func<string,string> customerName=null)
  {
   string id=CafeSimulation.NamedMissingCustomer(e.reason);
   if(id==null)return DefaultLines(e,phase,name,null,null);
   string customer=customerName?.Invoke(id)??CafeSimulation.NamedCustomerName(id);
   string route=e.reason.EndsWith("_refused",StringComparison.Ordinal)?"refused":"expired";
   return CafeNamedDispatchDialogue.Apply(settings,id,route+"_"+phase,name,customer,DefaultLines(e,phase,name,id,customer));
  }
  private static CafeConversationLine[] DefaultLines(CafeMissingCase e,string phase,string name,string id,string customer)
  {
   if(id==CafeSimulation.Suzuki)
   {
    bool refused=e.reason=="suzuki_refused";
    if(phase=="notice")return new[]{new CafeConversationLine("",refused?"迎えに行くと、鈴木さんは"+name+"を返そうとしなかった。":"指名のお願いに返事をしないまま翌日を迎えた。鈴木さんが店を訪れ、そのあと"+name+"の姿が見えなくなった。"),new CafeConversationLine(refused?"鈴木さん":"あなた",refused?"さっきも自分から遊びに来たんです。もう少し、うちにいさせてください。":"鈴木さんに連絡して、迎えに行こう。")};
    if(phase=="waiting")return new[]{new CafeConversationLine("","鈴木さんに連絡を取り、"+name+"を迎えに行ける日を待っている。")};
    if(phase=="confirmation")return new[]{new CafeConversationLine("鈴木さん",refused?"遊びに来てくれたのがうれしくて、ここにいたいんだと思い込んでました。約束の時間に返すべきでした。":"会える時間を作りたくて、勝手に連れていってしまいました。返事がないからって、連れていっていいわけがないですよね。"),new CafeConversationLine("あなた","今日は一緒に店へ戻ろう、"+name+"。")};
    return new[]{new CafeConversationLine("",name+"は鈴木さんのところから店に戻った。"),new CafeConversationLine("あなた","おかえり。今日はゆっくり休もうね。")};
   }
   if(e.reason=="sato_expired" || e.reason=="sato_refused")
   {
    if(phase=="notice")return new[]{new CafeConversationLine("",e.reason=="sato_expired"?"返事を待っていた佐藤さんが店を訪ねた。そのあと、"+name+"の姿が見えなくなった。":"迎えに行くと、佐藤さんは"+name+"を返そうとしなかった。"),new CafeConversationLine("あなた","佐藤さんに連絡して、"+name+"を迎えに行こう。")};
    if(phase=="waiting")return new[]{new CafeConversationLine("","佐藤さんに連絡を取り、"+name+"を迎えに行ける日を待っている。")};
    if(phase=="confirmation")return new[]{new CafeConversationLine("佐藤さん","うちで休ませてあげたいと思ったんです。でも、私が勝手に決めていいことではありませんでした。"),new CafeConversationLine("あなた",name+"を連れて帰ります。お話はまた改めて。")};
    return new[]{new CafeConversationLine("",name+"は佐藤さんのところから店に戻った。"),new CafeConversationLine("あなた","おかえり。今日はゆっくり休もうね。")};
   }
   if(id!=null)
   {
    bool refused=e.reason.EndsWith("_refused",StringComparison.Ordinal);
    if(phase=="notice")return new[]{
        new CafeConversationLine("",refused?"迎えに行くと、"+customer+"は"+name+"を返そうとしなかった。":customer+"が店を訪れ、そのあと"+name+"の姿が見えなくなった。"),
        new CafeConversationLine("あなた",customer+"に連絡して、"+name+"を迎えに行こう。")};
    if(phase=="waiting")return new[]{new CafeConversationLine("",customer+"に連絡を取り、"+name+"を迎えに行ける日を待っている。")};
    if(phase=="confirmation")return new[]{
        new CafeConversationLine(customer,name+"と過ごしたい気持ちで、勝手に決めてしまいました。約束を守るべきでした。"),
        new CafeConversationLine("あなた",name+"を連れて店に帰ります。お話はまた改めて。")};
    return new[]{new CafeConversationLine("",name+"は"+customer+"のところから店に戻った。"),
        new CafeConversationLine("あなた",name+"、おかえり。今日はゆっくり休もうね。")};
   }
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
