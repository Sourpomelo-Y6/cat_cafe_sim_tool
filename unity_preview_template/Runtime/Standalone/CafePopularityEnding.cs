using System;
using System.Linq;
using System.Globalization;
namespace CatCafePreview.Standalone
{
    [Serializable] public sealed class CafeEndingProgress {public string cat_id;public CafeConversationProgress progress;}
    public static class CafePopularityEnding
    {
        public const string Id="ending.popularity.v1",Branch="cleared";
        public static CafeClearRecord Record(CafeSnapshot s)=>s.clear_results.FirstOrDefault(r=>r.mode=="popularity");
        public static bool Waiting(CafeSnapshot s)=>s.game_over==null && Record(s)!=null && s.goal.Count>0 && !s.goal.Last().continued;
        public static void Validate(CafeEndingProgress row,CafeSnapshot s)
        {
            if(row==null)return;var p=row.progress;int length=Lines(Record(s),"猫").Length;
            if(Record(s)==null || (!string.IsNullOrEmpty(row.cat_id) && !s.cats.Any(c=>c.id==row.cat_id)) || p==null || p.conversation_id!=Id || p.branch_id!=Branch || p.node<0 || p.node>length || p.page_offset<0 || p.page_offset>1000000 || p.completed && p.node!=length || p.viewed_branches==null || !p.viewed_branches.SequenceEqual(p.completed?new[]{Branch}:new string[0]))throw new ArgumentException("人気エンディングの保存位置が不正です。");
        }
        public static void Finish(CafeConversationProgress p){if(p.completed)return;p.completed=true;p.viewed_branches=new[]{Branch};}
        static string N(double v)=>v.ToString("G6",CultureInfo.InvariantCulture);
        public static CafeConversationLine[] Lines(CafeClearRecord r,string name)
        {
            if(r==null)return new CafeConversationLine[0];
            return new[]{new CafeConversationLine("地の文","一日の営業を終えたカフェに、静かな時間が戻ってきました。\nいつもの席も、窓辺も、今日は少し違って見えます。"),
            new CafeConversationLine("あなた","人気目標の最後の段階まで、たどり着けたんだね。"),
            new CafeConversationLine("地の文",string.IsNullOrEmpty(name)?"あなたは静かな店内を見渡しました。\nここで重ねてきた時間を、ゆっくりと思い返します。":name+"がそばに座り、こちらを見上げます。\nあなたは少し腰をかがめて、目線を合わせました。"),
            new CafeConversationLine("あなた","お店を開けて、猫たちの様子を見て、お客さんを迎えて。\nその積み重ねが、ここまでつながったんだね。"),
            new CafeConversationLine("営業の記録","達成したのは"+r.day+"日目。\n目標人気 "+N(r.target)+" に対して、達成時の人気は "+N(r.value)+"。\n在籍猫 "+r.cats+"匹 ／ 所持金 "+N(r.funds)+" ／ 累計接客売上 "+N(r.revenue)+"。\nこれは目標達成時に記録した成果です。"),
            new CafeConversationLine("あなた","ひとつの目標は達成できたけれど、このカフェでの日々はまだ続けられる。\nこれからも、みんなの様子を見ながら過ごしていこう。"),
            new CafeConversationLine("地の文","猫たちと育ててきた、あなたの猫カフェ。\nその扉の向こうには、これからの毎日が待っています。")};
        }
    }
}
