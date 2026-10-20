using System.Linq;
using System.Globalization;
namespace CatCafePreview.Standalone
{
    // Uses the same progress, completion, rendering and replay infrastructure as popularity.
    public static class CafePatronEnding
    {
        public const string Id="ending.patron.v1";
        public static CafeClearRecord Record(CafeSnapshot s)=>s.clear_results.FirstOrDefault(r=>r.mode=="patron");
        public static bool Waiting(CafeSnapshot s)=>s.game_over==null && Record(s)!=null && s.patron?.status=="cleared" && !s.patron.continued;
        public static void Validate(CafeEndingProgress row,CafeSnapshot s)
        {
            CafePopularityEnding.Validate(row,s,Record(s),Id);

        }
        static string N(double v)=>v.ToString("G6",CultureInfo.InvariantCulture);
        public static CafeConversationLine[] Lines(CafeClearRecord r,string name)
        {
            if(r==null)return new CafeConversationLine[0];
            return new[]{new CafeConversationLine("地の文","あなたはカフェで、有力者への派遣を重ねてきた記録を開きました。\n店の外へとつながった活動が、ひとつの成果になっています。"),
            new CafeConversationLine("あなた","有力者の目標を、最後まで達成できたんだね。\n派遣を通じて積み重ねた信頼が、ここまでつながったんだ。"),
            new CafeConversationLine("地の文","記録に残るのは、目標を達成した日の成果。\nあなたはその数字を、ゆっくりと確かめます。"),
            new CafeConversationLine("あなた","カフェの中で育ててきた力が、外での活動にもつながった。\nこれからも猫たちの様子を見て、無理のない歩みを大切にしよう。"),
            new CafeConversationLine("営業の記録","達成したのは"+r.day+"日目。\n目標の合計満足度 "+N(r.target)+" に対して、達成時の合計満足度は "+N(r.value)+"。\n達成時の在籍猫 "+r.cats+"匹 ／ 所持金 "+N(r.funds)+" ／ 累計接客売上 "+N(r.revenue)+"。\nこれは目標達成時に記録した成果です。"),
            new CafeConversationLine("あなた","信頼を育てることも、店の活動を広げることも、一度きりでは終わらない。\nこの区切りを、これからの営業につなげていこう。"),
            new CafeConversationLine("地の文","派遣を通じて育った信頼と、広がったカフェの活動。\nその積み重ねを大切にしながら、あなたはこれからの日々へ目を向けました。")};
        }
    }
}
