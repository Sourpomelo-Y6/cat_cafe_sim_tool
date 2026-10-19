using System;
using System.Linq;
using System.Collections.Generic;
namespace CatCafePreview.Standalone
{
    [Serializable] public sealed class CafeOpeningProgress
    {
        public string mode, outcome;
        public string[] cat_ids, cat_names;
        public CafeConversationProgress progress;
    }
    public static class CafeOpening
    {
        public const string Id="opening.cafe.v1";
        public static readonly string[] Modes={"popularity","patron","bond","free"};
        public static CafeOpeningProgress Start(CafeSnapshot state,string mode,Func<string,string> name,bool play)
        {
            var cats=state.cats.Where(c=>c.activity=="cafe").ToArray();
            var row=new CafeOpeningProgress{mode=mode,outcome="",cat_ids=cats.Select(c=>c.id).ToArray(),cat_names=cats.Select(c=>name(c.id)).ToArray(),progress=new CafeConversationProgress{conversation_id=Id,branch_id=mode,viewed_branches=new string[0]}};
            if(!play)Finish(row,"debug_omitted");return row;
        }
        public static CafeConversationLine[] Lines(CafeOpeningProgress row)
        {
            var lines=new List<CafeConversationLine>{new CafeConversationLine("地の文","営業前の猫カフェ。\nまだお客さんのいない店内に、穏やかな時間が流れています。"),new CafeConversationLine("あなた","ここで、猫たちとお客さんを迎える毎日が始まるんだね。")};
            foreach(string name in row.cat_names){lines.Add(new CafeConversationLine("地の文",name+"がこちらへ顔を向けます。\nあなたはそっと目線を合わせ、声をかけました。"));lines.Add(new CafeConversationLine("あなた",name+"、今日はよろしくね。\n無理のないように、一緒に過ごしていこう。"));}
            if(row.cat_names.Length==0)lines.Add(new CafeConversationLine("地の文","あなたは静かな店内を見渡します。\n今いる猫の様子や予定を確かめながら、準備を進めましょう。"));
            string goal=row.mode=="popularity"?"目指すのは、人気のある猫カフェ。\n毎日の接客を重ねて、お店のことを知ってもらおう。":row.mode=="patron"?"目指すのは、有力者とのつながり。\nお客さんや訪問先との関わりを、少しずつ育てていこう。":row.mode=="bond"?"目指すのは、猫との親しい関係。\n日々のふれあいで、それぞれの猫との時間を大切にしよう。":"今回は自由営業。\n決まった目標に縛られず、猫たちの様子を見ながら店を育てていこう。";
            lines.Add(new CafeConversationLine("あなた",goal));lines.Add(new CafeConversationLine("地の文","まずは1日目の営業準備です。\n出勤する猫と休む猫を確認し、準備ができたら営業を始めましょう。\n営業準備では、休業を選ぶこともできます。"));return lines.ToArray();
        }
        public static void Finish(CafeOpeningProgress row,string outcome)
        {
            if(row.progress.completed)return;
            if(outcome!="viewed" && outcome!="skipped" && outcome!="debug_omitted")throw new ArgumentException("Invalid opening outcome");
            if(outcome=="viewed" && row.progress.node!=Lines(row).Length)throw new InvalidOperationException("本文を最後まで進めてください。");
            row.progress.node=Lines(row).Length;row.progress.page_offset=0;row.progress.completed=true;row.outcome=outcome;row.progress.viewed_branches=outcome=="viewed"?new[]{row.mode}:new string[0];
        }
        public static void Validate(CafeOpeningProgress row,CafeSnapshot state)
        {
            if(row==null)return;var p=row.progress;
            if(!Modes.Contains(row.mode) || row.cat_ids==null || row.cat_names==null || row.cat_ids.Length!=row.cat_names.Length || row.cat_ids.Length>64 || row.cat_ids.Distinct().Count()!=row.cat_ids.Length || row.cat_ids.Any(id=>!state.cats.Any(c=>c.id==id)) || row.cat_names.Any(n=>string.IsNullOrEmpty(n) || n.Length>256) || p==null || p.conversation_id!=Id || p.branch_id!=row.mode)throw new ArgumentException("オープニングの保存情報が不正です。");
            var lines=Lines(row);int max=p.node<lines.Length && p.node>=0?lines[p.node].text.Length:0;
            if(p.node<0 || p.node>lines.Length || p.page_offset<0 || p.page_offset>max || p.viewed_branches==null || !p.viewed_branches.SequenceEqual(row.outcome=="viewed"?new[]{row.mode}:new string[0]) || (p.completed?(p.node!=lines.Length || p.page_offset!=0 || !new[]{"viewed","skipped","debug_omitted"}.Contains(row.outcome)):( !string.IsNullOrEmpty(row.outcome) || state.day!=1 || state.tick!=0 || state.phase!="preparation")))throw new ArgumentException("オープニングの保存位置が不正です。");
        }
    }
}
