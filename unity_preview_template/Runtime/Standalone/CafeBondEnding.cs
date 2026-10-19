using System.Linq;
using System.Globalization;
namespace CatCafePreview.Standalone
{
    // Uses the same progress, completion, rendering and replay infrastructure as popularity.
    public static class CafeBondEnding
    {
        public const string Id="ending.bond.v1";
        public static CafeClearRecord Record(CafeSnapshot s)=>s.clear_results.FirstOrDefault(r=>r.mode=="bond");
        public static bool Waiting(CafeSnapshot s)=>s.game_over==null && Record(s)!=null && s.bond_goal?.status=="cleared" && !s.bond_goal.continued;
        public static void Validate(CafeEndingProgress row,CafeSnapshot s)
        {
            CafePopularityEnding.Validate(row,s,Record(s),Id);
            if(row!=null && !string.IsNullOrEmpty(row.cat_id) && !s.bond_goal.achieved_cats.Contains(row.cat_id))throw new System.ArgumentException("達成記録にない猫です。");
        }
        static string N(double v)=>v.ToString("G6",CultureInfo.InvariantCulture);
        public static CafeConversationLine[] Lines(CafeClearRecord r,string name)
        {
            if(r==null)return new CafeConversationLine[0];
            return new[]{new CafeConversationLine("地の文","あなたはカフェで、猫たちとの関係を記した記録を開きました。\n数字の向こうに、それぞれの猫と向き合う時間があります。"),
            new CafeConversationLine("あなた","好感度の目標を達成できたんだね。\n猫たちとの距離が、少しずつ近づいてきたんだ。"),
            new CafeConversationLine("地の文",string.IsNullOrEmpty(name)?"達成した猫たちの記録を、ゆっくり読み返します。": "達成した猫の記録には、"+name+"の名前もあります。\nあなたはその名前に、そっと目を留めました。"),
            new CafeConversationLine("あなた","一緒に過ごす時間を、これからも大切にしたい。\nその子の様子を見て、無理をせずに向き合っていこう。"),
            new CafeConversationLine("営業の記録","達成したのは"+r.day+"日目。\n目標の猫数 "+N(r.target)+"匹に対して、達成した猫は "+N(r.value)+"匹。\n達成時の在籍猫 "+r.cats+"匹 ／ 所持金 "+N(r.funds)+" ／ 累計接客売上 "+N(r.revenue)+"。\nこれは目標達成時に記録した成果です。"),
            new CafeConversationLine("あなた","目標は、ひとつの区切り。\nこれからの毎日も、猫たちの気持ちを大切にしていこう。"),
            new CafeConversationLine("地の文","猫たちとあなたの間に育った関係。\nそのつながりを大切にしながら、カフェの日々は続いていきます。")};
        }
    }
}
