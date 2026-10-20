using System;
using System.Linq;
using System.Globalization;
namespace CatCafePreview.Standalone {
 [Serializable] public sealed class CafeOutcome {
  public const string Id="ending.outcome.v1";
  public string cause;public int day,stage,deadline,prior_expired_day;public double funds,popularity,target,revenue;public int cats;public bool legacy;
  public CafeConversationProgress progress;
  public static string Cause(CafeSnapshot s)=>s.game_over??(s.goal.LastOrDefault()?.status=="expired" && !s.goal.Last().continued?"expired":null);
  public static CafeOutcome Capture(CafeSnapshot s,CafeRules rules,bool legacy=false){var cause=Cause(s);if(cause==null)return null;var g=s.goal.LastOrDefault();int stage=s.goal.Count;
   return new CafeOutcome{cause=cause,prior_expired_day=cause!="expired" && g?.status=="expired"?g.resolved_day:0,day=s.day,funds=s.funds,popularity=s.popularity,revenue=s.revenue,cats=s.cats.Count(c=>c.activity!="adopted"),stage=stage,deadline=stage==0?0:g.started_day+rules.goal_stages[stage-1].days-1,target=stage==0?0:rules.goal_stages[stage-1].target,legacy=legacy,progress=new CafeConversationProgress{conversation_id=Id,branch_id=cause,viewed_branches=new string[0]}};
  }
  public string Title=>cause=="expired"?"未達の結末 · ひとつの区切り":cause=="funds"?"経営終了 · 資金不足":"経営終了 · 人気低下";
  static string N(double x)=>x.ToString("G6",CultureInfo.InvariantCulture);
  public CafeConversationLine[] Lines()=>new[]{
   new CafeConversationLine("地の文","あなたはカフェの記録を開き、今回の結果を静かに確かめました。"),
   new CafeConversationLine("営業の記録",cause=="expired"?"人気目標の第"+stage+"段階は、期限の"+deadline+"日目までに目標へ届かず、未達となりました。":cause=="funds"?"資金が0以下になり、経営終了が確定しました。":"人気が0以下になり、経営終了が確定しました。"),
   new CafeConversationLine("営業の記録",(legacy?"旧セーブから確認できる記録です。\n":"結果確定時の記録です。\n")+day+"日目 ／ 資金 "+N(funds)+" ／ 人気 "+N(popularity)+"。\n在籍猫 "+cats+"匹 ／ 当日の接客売上 "+N(revenue)+"。"+(stage>0?"\n第"+stage+"段階の目標人気 "+N(target)+" ／ 期限 "+deadline+"日目。":"")+(prior_expired_day>0?"\n人気目標は"+prior_expired_day+"日目に期限内未達と記録されています。":"")),
   new CafeConversationLine("あなた","思い描いた結果には届かなかったね。\nそれでも、ここで向き合ってきた時間まで、なくなるわけではない。"),
   new CafeConversationLine("地の文",cause=="expired"?"今回の挑戦は、ここでひと区切りです。\n結果を確認したあとは、猫や資金を引き継いで営業を続けられます。":"今回の経営は、ここで終わりを迎えました。\n今はこの記録を、ゆっくり受け止めましょう。")};
  public static void Validate(CafeOutcome row,CafeSnapshot s){if(row==null)return;var p=row.progress;int n=row.Lines().Length;
   if(!new[]{"expired","funds","popularity"}.Contains(row.cause) || (s.game_over!=null && row.cause!=s.game_over) || row.cause=="expired" && !s.goal.Any(g=>g.status=="expired") || row.prior_expired_day<0 || row.prior_expired_day>row.day || row.day<1 || row.day>s.day || row.stage<0 || row.stage>s.goal.Count || row.cats<0 || row.cats>s.cats.Count || new[]{row.funds,row.popularity,row.target,row.revenue}.Any(x=>double.IsNaN(x) || double.IsInfinity(x)) || double.IsNaN(row.funds) || double.IsInfinity(row.funds) || p==null || p.conversation_id!=Id || p.branch_id!=row.cause || p.node<0 || p.node>n || p.page_offset<0 || p.page_offset>1000000 || p.completed && p.node!=n || p.viewed_branches==null || !p.viewed_branches.SequenceEqual(p.completed?new[]{row.cause}:new string[0]))throw new ArgumentException("結末の保存情報が不正です。");
  }
 }
}
