using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CatCafePreview.Standalone
{
    public static class CafeInteractionStatisticsPresentation
    {
        public static string Describe(CafeInteractionStatistics stats,Func<string,string> catName,Func<string,string> personName,string cat=null,string person=null,string channel=null,int page=0)
        {
            string connect=CatCafePreview.CafeDisplayNames.Label("reactions","connect","心をつかむ"),opened=CatCafePreview.CafeDisplayNames.Label("reactions","open_up","心を開く"),simultaneous=CatCafePreview.CafeDisplayNames.Label("reactions","simultaneous","同時成立");
            var data=stats.Data;var rows=stats.Select(cat,person,channel).ToArray();var normal=rows.Where(r=>r.Normal).ToArray();
            Func<IEnumerable<CafeInteractionCount>,long> count=items=>items.Sum(r=>r.count);
            var text=new StringBuilder();
            text.AppendLine(data.complete?"このセーブの開始からの記録":"記録開始以降の回数（それ以前の回数は含みません）");
            text.AppendLine("記録開始："+data.started_day+"日目 / "+data.started_tick+"tick（途中の交流は開始後の行動のみ）");
            text.AppendLine("対象："+(cat==null?"全猫":catName(cat))+" / "+(person==null?"全人物":personName(person))+" / "+(channel==null?"接客＋プレイヤー交流":channel=="service"?"接客":"プレイヤー交流"));
            text.AppendLine("\n通常交流行動："+count(normal)+"回");
            text.AppendLine("補助操作："+count(rows.Where(r=>r.action=="pause" || r.action=="switch"))+"回 / 特別な働きかけ："+count(rows.Where(r=>r.action=="connect"))+"回");
            text.AppendLine("全確定操作："+count(rows)+"回（通常・補助・特別な働きかけの合計）");
            text.AppendLine("同じ行動を双方の視点で表示しています。猫側・人物側の回数は足しません。");
            text.AppendLine("\n"+(cat!=null?"受けた通常行動":person!=null?"行った通常行動":"通常行動の動作別内訳"));
            AppendGroups(text,normal,r=>r.mode,r=>CafeSimulation.PlayerTypeName(r.mode));
            text.AppendLine("\n"+(cat!=null?"猫が示した通常反応":person!=null?"人物が受けた猫の通常反応":"通常行動に対する猫の反応"));
            AppendGroups(text,normal,r=>r.reaction,r=>CafeSimulation.PlayerReactionName(r.reaction));
            text.AppendLine("\n特別成立（通常反応と併存する内訳）");
            text.AppendLine("人物→猫："+connect+" "+count(rows.Where(r=>(r.special&1)!=0))+"回");
            text.AppendLine("猫→人物："+opened+" "+count(rows.Where(r=>(r.special&2)!=0))+"回");
            text.AppendLine(simultaneous+" "+count(rows.Where(r=>r.special==3))+"回（上記それぞれの内数）");
            text.AppendLine("特別成立した手数 "+count(rows.Where(r=>r.special!=0))+"回（同時成立も1手）");
            text.AppendLine("人物自身の独立した感情反応：対象となる記録なし");
            text.AppendLine("\n相手別の通常交流行動（多い順・上位20件）");
            AppendGroups(text,normal,r=>cat!=null?r.person_id:person!=null?r.cat_id:r.cat_id.Length+":"+r.cat_id+r.person_id,r=>cat!=null?personName(r.person_id):person!=null?catName(r.cat_id):catName(r.cat_id)+" × "+personName(r.person_id));
            var detail=rows.OrderBy(r=>r.cat_id,StringComparer.Ordinal).ThenBy(r=>r.person_id,StringComparer.Ordinal).ThenBy(r=>r.mode,StringComparer.Ordinal).ThenBy(r=>r.action,StringComparer.Ordinal).ThenBy(r=>r.reaction,StringComparer.Ordinal).ThenBy(r=>r.special).ThenBy(r=>r.target,StringComparer.Ordinal).ThenBy(r=>r.channel,StringComparer.Ordinal).ToArray();
            int pages=Math.Max(1,(detail.Length+29)/30);page=Math.Max(0,Math.Min(page,pages-1));
            text.AppendLine("\n操作・反応の詳細："+(page+1)+" / "+pages+"ページ（1ページ30項目）");
            foreach(var r in detail.Skip(page*30).Take(30))text.AppendLine(catName(r.cat_id)+" × "+personName(r.person_id)+" / "+(r.channel=="service"?"接客":"プレイヤー")+"\n"+CafeSimulation.PlayerTypeName(r.mode)+" / "+CafeSimulation.PlayerActionName(r.action)+(r.action=="switch"?" → "+CafeSimulation.PlayerTypeName(r.target):"")+" / "+(string.IsNullOrEmpty(r.reaction)?"通常反応なし":CafeSimulation.PlayerReactionName(r.reaction))+(r.Normal?"":"（補助・特別操作の結果）")+" / 特別："+(r.special==3?simultaneous:r.special==2?opened:r.special==1?connect:"なし")+" / "+r.count+"回\n");
            if(detail.Length==0)text.AppendLine(data.complete?"まだ記録された行動はありません。":"記録開始以降の行動はありません。以前の回数は不明です。");
            return text.ToString();
        }
        static void AppendGroups(StringBuilder text,IEnumerable<CafeInteractionCount> rows,Func<CafeInteractionCount,string> key,Func<CafeInteractionCount,string> label)
        {
            var groups=rows.GroupBy(key).Select(g=>new{label=label(g.First()),count=g.Sum(r=>r.count)}).OrderByDescending(g=>g.count).ThenBy(g=>g.label,StringComparer.Ordinal).ToArray();
            foreach(var group in groups.Take(20))text.AppendLine(group.label+"："+group.count+"回");if(groups.Length==0)text.AppendLine("記録なし");if(groups.Length>20)text.AppendLine("ほか "+(groups.Length-20)+"項目（対象を絞ると詳細を確認できます）");
        }
    }
}
