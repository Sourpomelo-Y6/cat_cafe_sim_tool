using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace CatCafePreview.Standalone
{
    [Serializable] public sealed class CafeNamedDispatchScene
    {
        public string id;
        public string[] lines;
    }

    [Serializable] public sealed class CafeNamedDispatchSettings
    {
        public string customer_id, name;
        public bool enabled;
        public CafeNamedDispatchScene[] scenes;

        private static readonly Dictionary<string,int> SceneLengths = new Dictionary<string,int>
        {
            {"affinity",2}, {"request",2}, {"request_high",2}, {"reminder",2}, {"reminder_high",2},
            {"expired_notice",2}, {"refused_notice",2}, {"expired_waiting",1}, {"refused_waiting",1},
            {"expired_confirmation",2}, {"refused_confirmation",2}, {"expired_result",2}, {"refused_result",2},
            {"normal_return",1}, {"adoption",1}, {"epilogue",3}
        };

        internal static bool CustomerId(string id) => Regex.IsMatch(id??"",
            @"\A(?:guest-[1-9][0-9]{0,8}|expanded-guest(?:-[2-9][0-9]*)?|advanced-(?:white|quiet|play|contact|longhair)|reservation-longhair|vip-calico)\z");

        public static void Validate(CafeNamedDispatchSettings[] rows)
        {
            if(rows==null)return;
            var ids=new HashSet<string>(StringComparer.Ordinal);
            if(rows.Length>500)throw new ArgumentException("指名派遣のお客さんが多すぎます。");
            foreach(var row in rows)
            {
                if(row==null || !CustomerId(row.customer_id) || !ids.Add(row.customer_id)
                    || string.IsNullOrWhiteSpace(row.name) || row.name.Length>100 || row.scenes==null
                    || row.scenes.Length>SceneLengths.Count)
                    throw new ArgumentException("指名派遣のお客さんの設定が不正です。");
                var scenes=new HashSet<string>(StringComparer.Ordinal);
                foreach(var scene in row.scenes)
                {
                    if(scene==null || string.IsNullOrEmpty(scene.id) || !scenes.Add(scene.id)
                        || !SceneLengths.TryGetValue(scene.id,out int length) || scene.lines==null || scene.lines.Length!=length
                        || scene.lines.Any(line=>line==null || line.Length>600
                            || line.Replace("{cat}","").Replace("{customer}","").Any(ch=>ch=='{' || ch=='}')
                            || line.Any(ch=>ch<32 && ch!='\n' && ch!='\t')))
                        throw new ArgumentException("指名派遣の台詞設定が不正です。");
                }
            }
        }
    }

    public static class CafeNamedDispatchDialogue
    {
        public static CafeConversationLine[] Apply(CafeNamedDispatchSettings[] settings,string customerId,
            string scene,string cat,string customer,CafeConversationLine[] defaults)
        {
            var lines=settings?.FirstOrDefault(r=>r.customer_id==customerId)?.scenes?.FirstOrDefault(r=>r.id==scene)?.lines;
            if(lines==null)return defaults;
            return defaults.Select((line,i)=>string.IsNullOrWhiteSpace(lines[i])?line:
                new CafeConversationLine(line.speaker,Regex.Replace(lines[i],@"\{(cat|customer)\}",
                    token=>token.Groups[1].Value=="cat"?cat:customer))).ToArray();
        }

        public static CafeConversationLine[] Request(CafeNamedDispatchSettings[] settings,string id,string cat,string customer,
            bool isRequest,bool reminder,bool high)
        {
            string scene=isRequest?(reminder?"reminder":"request")+(high?"_high":""):"affinity";
            return Apply(settings,id,scene,cat,customer,RequestDefaults(id,cat,customer,isRequest,reminder,high));
        }

        private static CafeConversationLine[] RequestDefaults(string id,string cat,string customer,bool request,bool reminder,bool high)
        {
            if(!request)
            {
                if(id==CafeSimulation.Sato)return new[]{
                    new CafeConversationLine("",customer+"の声が聞こえると、"+cat+"は自分から近くへ歩いていった。"),
                    new CafeConversationLine(customer,"今日も隣に来てくれたのね。ここ、空けておいたよ。")};
                if(id==CafeSimulation.Suzuki)return new[]{
                    new CafeConversationLine("",cat+"が戻ってきて、片づけかけたねこじゃらしに前足を伸ばした。"),
                    new CafeConversationLine(customer,"もう一回ですか。じゃあ、ここで待ってますね。")};
                return new[]{
                    new CafeConversationLine("",cat+"は"+customer+"に気づき、自分から近くへ歩いていった。"),
                    new CafeConversationLine(customer,cat+"、今日も来てくれたんですね。")};
            }
            if(id==CafeSimulation.Sato)return new[]{
                new CafeConversationLine(customer,reminder?cat+"のこと、開店前にお返事をいただけますか。":"お店で待っても会えない日が続きました。"+cat+"を、うちに一日だけ呼べませんか。"),
                new CafeConversationLine(customer,high?"一日で迎えに来られても、あの子が落ち着いていたら、そのままうちにいてほしいんです。":"あの子が落ち着けるように、窓辺に場所を空けておきます。")};
            if(id==CafeSimulation.Suzuki)return new[]{
                new CafeConversationLine(customer,reminder?"昨日のお願い、開店前にお返事をもらえますか？":"この間も待って帰ったので、今度はゆっくり遊びたいんです。"+cat+"に、一日だけうちに来てもらえませんか？"),
                new CafeConversationLine(customer,high?"またお店で遊べなくなるなら、少し長く預かることも考えてほしいです。":"ねこじゃらしを用意して待ってます。")};
            return new[]{
                new CafeConversationLine(customer,reminder?cat+"のこと、開店前にお返事をいただけますか。":"お店でなかなか会えないので、"+cat+"に一日だけうちに来てもらえませんか。"),
                new CafeConversationLine(customer,high?cat+"が落ち着いていたら、もう少し長く預かることも考えてほしいです。":"落ち着いて過ごせる場所を用意しておきます。")};
        }

        public static CafeConversationLine[] Epilogue(CafeNamedDispatchSettings[] settings,string id,string cat,string customer)
        {
            var letter=new[]{
                new CafeConversationLine("",customer+"から、"+cat+"の近況を知らせる手紙が届いた。"),
                new CafeConversationLine(customer,cat+"が落ち着ける場所を用意しました。最近はそこでよく休んでいます。"),
                new CafeConversationLine(customer,"この家でも、"+cat+"のペースを大事にしていきます。")};
            if(id==CafeSimulation.Sato)
            {
                letter[1]=new CafeConversationLine(customer,"窓辺にクッションを置きました。昨日はそこで午後の日差しを浴びていました。");
                letter[2]=new CafeConversationLine(customer,"店で隣に来てくれたときも、私が呼ぶより先に、あの子が自分で来てくれたんですよね。ここでも、"+cat+"が自分で選べる場所を増やしていきます。");
            }
            if(id==CafeSimulation.Suzuki)
            {
                letter[1]=new CafeConversationLine(customer,"ねこじゃらしは棚に置いています。この前は"+cat+"が棚の下で待っていたので、そこに座って少し遊びました。");
                letter[2]=new CafeConversationLine(customer,"遊びたいときは、自分から来てくれますね。");
            }
            return Apply(settings,id,"epilogue",cat,customer,letter);
        }

        public static string NormalReturn(CafeNamedDispatchSettings[] settings,string id,string cat,string customer)
            => Apply(settings,id,"normal_return",cat,customer,new[]{
                new CafeConversationLine("",cat+"は"+customer+"のところから予定どおり店に戻った。")})[0].text;
    }
}
