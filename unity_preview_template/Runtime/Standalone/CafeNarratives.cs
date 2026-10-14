using System;
using System.Collections.Generic;
using System.Linq;
namespace CatCafePreview.Standalone
{
    [Serializable] public class CafeNarrativeRow
    {
        public int version=1,day,turn,seat;
        public string session,cat_id,customer_id,mode,action,reaction,text;
    }
    // Presentation receipts: generated only after a real action, never while reading.
    public sealed class CafeNarratives
    {
        private readonly CafeSimulation sim;private string epoch;
        public List<CafeNarrativeRow> Rows=new List<CafeNarrativeRow>();
        public Func<string,string> CustomerName=id=>id;
        public CafeNarratives(CafeSimulation simulation){sim=simulation;epoch=sim.State.instance_id;sim.ExchangeObserved=Observe;}
        public void Sync(){if(epoch!=sim.State.instance_id){epoch=sim.State.instance_id;Rows.Clear();}}
        public void Restore(CafeNarrativeRow[] rows){epoch=sim.State.instance_id;Rows=(rows??new CafeNarrativeRow[0]).ToList();}
        private void Observe(CafeExchange e,string mode,string action,double before,int opened)
        {
            Sync();if(e.cat_id!="cat-mike")return;
            bool player=e.customer_id=="player";var p=player?sim.ActivePlayer:null;
            string session=player?p?.id:"service-"+sim.State.day+"-"+e.customer_id+"-"+e.cat_id+"-"+sim.State.visits.Single(v=>v.id==e.customer_id).seated_at;
            if(session==null)return;
            var row=Compose(player?"プレイヤー":CustomerName(e.customer_id),mode,action,e.previous_reaction,before,e.stamina,e.open_up_count>opened,action=="connect");
            row.day=sim.State.day;row.session=session;row.cat_id=e.cat_id;row.customer_id=e.customer_id;row.seat=player?-1:e.seat;row.turn=player?p.actions.Count+1:sim.State.visits.Single(v=>v.id==e.customer_id).seated_ticks+1;
            Rows.Add(row);
        }
        public static CafeNarrativeRow Compose(string human,string mode,string action,string reaction,double before,double after,bool opened=false,bool connect=false)
        {
            string speech,move,cat,scene;
            if(action=="pause"){speech="少し休もうか。";move="は手を止め、ミケが落ち着くのを待った。";cat=before<=20?"ミケは体を丸め、ゆっくり息を整えた。":"ミケは前足をそろえ、静かに座った。";scene=after>before?"ひと息つく間に、体力が少し戻った。":"急がず、静かな時間を過ごした。";}
            else if(action=="switch"){speech="今度はこれにしよう。";move="は交流を「"+CafeSimulation.PlayerTypeName(mode)+"」へ切り替えた。";cat="ミケは人の手元を見つめた。";scene="次の働きかけまで、少し間を置いた。";}
            else if(connect){speech="ミケ、こっちを見て。";move="はミケの様子を見ながら、特別な働きかけをした。";cat="ミケはその働きかけを受け止めた。";scene="二人の間合いが、少し近づいた。";}
            else {
                speech=mode=="teaser"?"ミケ、遊ぼうか。":mode=="pet"?"ミケ、なでてもいいかな。":"ミケ、付き合ってくれるかな。";
                string strength=action=="intense"?"強く":action=="adapt"?"ミケの様子に合わせて":action=="feint"?"緩急をつけて":"素直な動きで";
                move=mode=="teaser"?"は"+strength+"ねこじゃらしを揺らした。":mode=="pet"?"は"+strength+"ミケをなでた。":"は「"+CafeSimulation.PlayerTypeName(mode)+"」で、"+strength+"働きかけた。";
                cat=reaction=="turn_away"?"ミケは顔を背け、手の届かない方へ身を引いた。":reaction=="listless"?"ミケはまぶたを重そうに下げ、動こうとしなかった。":reaction=="confused"?"ミケは耳を動かし、どう応じるか迷っているようだった。":reaction=="enthusiastic"?(mode=="teaser"?"ミケは穂先を追い、勢いよく前足を伸ばした。":mode=="pet"?"ミケは目を細め、手に頬を寄せた。":"ミケは身を乗り出し、熱心に応じた。"):reaction=="favorable"?(mode=="teaser"?"ミケは穂先を見つめ、前足でそっと触れた。":mode=="pet"?"ミケは力を抜き、気持ちよさそうに目を細めた。":"ミケは穏やかに応じた。"):"ミケは様子を見ながら、静かに応じた。";
                scene=reaction=="turn_away"?"距離を置きたい気持ちが伝わってきた。":reaction=="listless"?"今は休む時間が必要そうだ。":reaction=="confused"?"働きかけと反応の間に、小さな間が空いた。":"穏やかな交流の時間が流れた。";
            }
            if(opened)cat+=" ミケは心を開いた様子を見せた。";
            if(after==0)scene+=" 体力が尽き、交流を切り上げた。";
            return new CafeNarrativeRow{mode=mode,action=action,reaction=reaction??"received",text=human+"「"+speech+"」\n"+human+move+"\n\n"+cat+"\n\n"+scene+"\n体力 "+CafeDetailPresentation.Number(before)+" → "+CafeDetailPresentation.Number(after)};
        }
        public string Read(string session){Sync();var rows=Rows.Where(r=>r.session==session).ToArray();if(rows.Length==0)return "文章の記録はまだありません。";return "最新のやり取り（"+rows.Last().turn+"手目）\n\n"+rows.Last().text+"\n\n── 履歴（新しい順） ──\n\n"+string.Join("\n\n",rows.Reverse().Skip(1).Select(r=>r.turn+"手目\n"+r.text));}
        public string Seat(CafeExchange e){if(e==null)return "空席です。お客さんを待っています。";Sync();var v=sim.State.visits.Single(r=>r.id==e.customer_id);return Read("service-"+sim.State.day+"-"+e.customer_id+"-"+e.cat_id+"-"+v.seated_at);}
        public static void Validate(CafeSaveEnvelope save){var rows=save.narratives??new CafeNarrativeRow[0];if(rows.Length>100000)throw new ArgumentException("Too many narrative receipts");var keys=new HashSet<string>();foreach(var r in rows)if(r==null || r.version!=1 || r.day<1 || r.day>save.state.day || r.cat_id!="cat-mike" || string.IsNullOrEmpty(r.session) || r.turn<1 || string.IsNullOrEmpty(r.text) || r.text.Length>3000 || !keys.Add(r.session+"/"+r.turn))throw new ArgumentException("Invalid narrative receipt");}
    }
    public sealed partial class CafeSimulation {public Action<CafeExchange,string,string,double,int> ExchangeObserved;}
}
