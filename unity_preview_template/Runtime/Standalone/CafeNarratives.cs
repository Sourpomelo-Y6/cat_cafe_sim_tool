using System;
using System.Collections.Generic;
using System.Linq;
namespace CatCafePreview.Standalone
{
    [Serializable] public class CafeNarrativeRow
    {
        public int version=1,day,turn,seat;
        public string session,cat_id,customer_id,mode,action,reaction,text;
        public string speech,human_action,cat_response,narration;
        public string catalog_revision,speech_id,cat_text_id,speech_pool,cat_pool,image_resource,image_scene_key;
    }
    // Presentation receipts: generated only after a real action, never while reading.
    public sealed class CafeNarratives
    {
        private readonly CafeSimulation sim;private string epoch;
        public List<CafeNarrativeRow> Rows=new List<CafeNarrativeRow>();
        public Func<string,string> CustomerName=id=>id;
        private CafeNarrativeCatalog catalog;
        public CafeNarrativeCatalog Catalog=>catalog;
        public void SetCatalog(CafeNarrativeCatalog value){value?.Validate();catalog=value;}
        public CafeNarratives(CafeSimulation simulation){sim=simulation;epoch=sim.State.instance_id;sim.ExchangeObserved=Observe;}
        public void Sync(){if(epoch!=sim.State.instance_id){epoch=sim.State.instance_id;Rows.Clear();}}
        public void Restore(CafeNarrativeRow[] rows){epoch=sim.State.instance_id;Rows=(rows??new CafeNarrativeRow[0]).ToList();}
        private void Observe(CafeExchange e,string mode,string action,double before,int opened)
        {
            Sync();if(e.cat_id!="cat-mike" && !(catalog?.SupportsCat(e.cat_id)??false))return;
            bool player=e.customer_id=="player";var p=player?sim.ActivePlayer:null;
            string session=player?p?.id:"service-"+sim.State.day+"-"+e.customer_id+"-"+e.cat_id+"-"+sim.State.visits.Single(v=>v.id==e.customer_id).seated_at;
            if(session==null)return;
            var row=Compose(player?"プレイヤー":CustomerName(e.customer_id),mode,action,e.previous_reaction,before,e.stamina,e.open_up_count>opened,action=="connect",sim.Definition(e.cat_id).name);
            row.cat_id=e.cat_id;
            if(catalog!=null)ApplyCatalog(row,player?"player":e.customer_id,player?"プレイヤー":CustomerName(e.customer_id),before,e.stamina,e.open_up_count>opened);
            row.day=sim.State.day;row.session=session;row.cat_id=e.cat_id;row.customer_id=e.customer_id;row.seat=player?-1:e.seat;row.turn=player?p.actions.Count+1:sim.State.visits.Single(v=>v.id==e.customer_id).seated_ticks+1;
            Rows.Add(row);
        }
        public static CafeNarrativeRow Compose(string human,string mode,string action,string reaction,double before,double after,bool opened=false,bool connect=false,string catName="ミケ")
        {
            string speech,move,cat,scene;
            if(action=="pause"){speech="少し休もうか。";move="は手を止め、ミケが落ち着くのを待った。";cat=before<=20?"ミケは体を丸め、ゆっくり息を整えた。":"ミケは前足をそろえ、静かに座った。";scene=after>before?"ひと息つく間に、体力が少し戻った。":"急がず、静かな時間を過ごした。";}
            else if(action=="switch"){speech="今度はこれにしよう。";move="は交流を「"+CafeSimulation.PlayerTypeName(mode)+"」へ切り替えた。";cat="ミケは人の手元を見つめた。";scene="次の働きかけまで、少し間を置いた。";}
            else if(connect){speech="ミケ、こっちを見て。";move="はミケの様子を見ながら、特別な働きかけをした。";cat="ミケはその働きかけを受け止めた。";scene="二人の間合いが、少し近づいた。";}
            else {
                speech=mode=="teaser"?"ミケ、遊ぼうか。":mode=="pet"?"ミケ、なでてもいいかな。":"ミケ、付き合ってくれるかな。";
                string strength=action=="intense"?"強く":action=="adapt"?"ミケの様子に合わせて":action=="feint"?"緩急をつけて":"素直な動きで";
                move=mode=="teaser"?"は"+strength+"ねこじゃらしを揺らした。":mode=="pet"?"は"+strength+"ミケをなでた。":"は「"+CafeSimulation.PlayerTypeName(mode)+"」で、"+strength+"働きかけた。";
                var actualMove=CafeNarrativeCatalog.DefaultActionText(mode,action);
                if(actualMove!=null)move=actualMove.Replace("{human}","").Replace("{cat}","ミケ");
                cat=reaction=="turn_away"?"ミケは顔を背け、手の届かない方へ身を引いた。":reaction=="listless"?"ミケはまぶたを重そうに下げ、動こうとしなかった。":reaction=="confused"?"ミケは耳を動かし、どう応じるか迷っているようだった。":reaction=="enthusiastic"?(mode=="teaser"?"ミケは穂先を追い、勢いよく前足を伸ばした。":(mode=="pet" || mode=="brush")?"ミケは目を細め、手に頬を寄せた。":"ミケは身を乗り出し、熱心に応じた。"):reaction=="favorable"?(mode=="teaser"?"ミケは穂先を見つめ、前足でそっと触れた。":(mode=="pet" || mode=="brush")?"ミケは力を抜き、気持ちよさそうに目を細めた。":"ミケは穏やかに応じた。"):"ミケは様子を見ながら、静かに応じた。";
                scene=reaction=="turn_away"?"距離を置きたい気持ちが伝わってきた。":reaction=="listless"?"今は休む時間が必要そうだ。":reaction=="confused"?"働きかけと反応の間に、小さな間が空いた。":"穏やかな交流の時間が流れた。";
            }
            if(!connect && action!="pause" && action!="switch" && (mode=="voice" || mode=="presence"))
            {
                if(reaction=="enthusiastic")cat=mode=="voice"?"ミケは相手へ耳を向け、声に熱心に応じた。":"ミケは落ち着いた様子で、相手の気配を熱心にうかがった。";
                if(reaction=="favorable")cat="ミケは穏やかな様子で、相手へ注意を向けた。";
            }
            if(opened)cat+=" ミケは心を開いた様子を見せた。";
            if(after==0)scene+=" 体力が尽き、交流を切り上げた。";
            speech=speech.Replace("ミケ",catName);move=move.Replace("ミケ",catName);cat=cat.Replace("ミケ",catName);
            var row=new CafeNarrativeRow{mode=mode,action=action,reaction=reaction??"received",speech=speech,human_action=human+move,cat_response=cat,narration=scene};
            row.text=Format(row,human,before,after);return row;
        }
        private static string Format(CafeNarrativeRow row,string human,double before,double after)=>human+"「"+row.speech+"」\n"+row.human_action+"\n\n"+row.cat_response+"\n\n"+row.narration+"\n体力 "+CafeDetailPresentation.Number(before)+" → "+CafeDetailPresentation.Number(after);
        private void ApplyCatalog(CafeNarrativeRow row,string person,string human,double before,double after,bool opened)
        {
            string mode=row.action=="pause"?"pause":row.mode;
            if(!CafeNarrativeCatalog.Modes.Contains(mode) || row.action=="any" || !CafeNarrativeCatalog.Actions(mode).Contains(row.action))return;
            string catName=sim.Definition(row.cat_id).name;
            row.catalog_revision=catalog.revision;
            var speech=catalog.Find("speech",person,mode,row.action,"any",row.cat_id);
            var cat=catalog.Find("cat_response",row.cat_id,mode,row.action,row.reaction,row.cat_id);
            if(speech!=null)
            {
                var previous=Rows.LastOrDefault(r=>r.speech_pool==speech.Key());var chosen=Next(speech,previous?.speech_id,Rows.LastOrDefault(r=>r.customer_id==person)?.speech);
                row.speech=catalog.Expand(chosen.text,person,catName);row.speech_id=chosen.id;row.speech_pool=speech.Key();
            }
            if(cat!=null)
            {
                var previous=Rows.LastOrDefault(r=>r.cat_pool==cat.Key());var chosen=Next(cat,previous?.cat_text_id,Rows.LastOrDefault(r=>r.cat_id==row.cat_id)?.cat_response);
                row.cat_response=catalog.Expand(chosen.text,person,catName)+(opened?" "+catName+"は心を開いた様子を見せた。":"");row.cat_text_id=chosen.id;row.cat_pool=cat.Key();
            }
            var move=catalog.action_texts.FirstOrDefault(r=>r.mode==mode && r.action==row.action);
            if(move!=null)row.human_action=move.text.Replace("{human}",human).Replace("{cat}",catName);
            string scene=mode=="pause"?catalog.rest_narration:catalog.narration_texts.FirstOrDefault(r=>r.reaction==row.reaction)?.text;
            if(scene!=null)row.narration=scene+(mode=="pause" && after>before?" 体力が少し戻った。":"")+(after==0?" 体力が尽き、交流を切り上げた。":"");
            string imageScene=CafeNarrativeCatalog.UsePreparationImage(row.cat_id,row.action,row.reaction)?mode:CafeNarrativeCatalog.Scene(mode,row.action);
            var image=catalog.image_bindings.FirstOrDefault(i=>i.cat_id==row.cat_id && i.scene_key==imageScene);
            // Existing special-event images keep priority when this action also opens up.
            if(image!=null && image.image!="" && !opened)
            {row.image_resource="CatCafeContent/Imports/"+catalog.pack_id+"/"+image.image.Substring(0,image.image.Length-4);row.image_scene_key=image.resolved_scene_key;}
            row.text=Format(row,human,before,after);
        }
        private static NarrativeVariant Next(NarrativeEntry entry,string previous,string lastText)
        {int i=(Array.FindIndex(entry.variants,v=>v.id==previous)+1)%entry.variants.Length;if(entry.variants.Length>1 && entry.variants[i].text==lastText)i=(i+1)%entry.variants.Length;return entry.variants[i];}
        public CafeNarrativeRow LatestPlayer(string session){Sync();return Rows.LastOrDefault(r=>r.session==session);}
        public CafeNarrativeRow LatestSeat(CafeExchange e)
        {if(e==null)return null;Sync();var v=sim.State.visits.Single(r=>r.id==e.customer_id);string id="service-"+sim.State.day+"-"+e.customer_id+"-"+e.cat_id+"-"+v.seated_at;return Rows.LastOrDefault(r=>r.session==id);}
        public string Read(string session){Sync();var rows=Rows.Where(r=>r.session==session).ToArray();if(rows.Length==0)return "文章の記録はまだありません。";return "最新のやり取り（"+rows.Last().turn+"手目）\n\n"+rows.Last().text+"\n\n── 履歴（新しい順） ──\n\n"+string.Join("\n\n",rows.Reverse().Skip(1).Select(r=>r.turn+"手目\n"+r.text));}
        public string Seat(CafeExchange e){if(e==null)return "空席です。お客さんを待っています。";Sync();var v=sim.State.visits.Single(r=>r.id==e.customer_id);return Read("service-"+sim.State.day+"-"+e.customer_id+"-"+e.cat_id+"-"+v.seated_at);}
        public static void Validate(CafeSaveEnvelope save)
        {
            var rows=save.narratives??new CafeNarrativeRow[0];if(rows.Length>100000)throw new ArgumentException("Too many narrative receipts");var keys=new HashSet<string>();
            foreach(var r in rows)
            {
                if(r==null || r.version!=1 || r.day<1 || r.day>save.state.day || !CafeNarrativeCatalog.Id(r.cat_id) || string.IsNullOrEmpty(r.session) || r.turn<1 || string.IsNullOrEmpty(r.text) || r.text.Length>3000 || !keys.Add(r.session+"/"+r.turn))throw new ArgumentException("Invalid narrative receipt");
                if(!string.IsNullOrEmpty(r.catalog_revision) && !CafeNarrativeCatalog.Hex(r.catalog_revision,64)
                    || !string.IsNullOrEmpty(r.image_resource) && !CafeNarrativeCatalog.Resource(r.image_resource))throw new ArgumentException("Invalid saved narrative catalog/image");
                foreach(string id in new[]{r.speech_id,r.cat_text_id})if(!string.IsNullOrEmpty(id) && (!CafeNarrativeCatalog.Hex(id,32) || string.IsNullOrEmpty(r.catalog_revision)))throw new ArgumentException("Invalid saved narrative candidate");
                if(string.IsNullOrEmpty(r.speech_id)!=string.IsNullOrEmpty(r.speech_pool) || string.IsNullOrEmpty(r.cat_text_id)!=string.IsNullOrEmpty(r.cat_pool)
                    || (r.speech_pool?.Length??0)>400 || (r.cat_pool?.Length??0)>400
                    || new[]{r.speech,r.human_action,r.cat_response,r.narration}.Any(s=>(s?.Length??0)>1200))throw new ArgumentException("Invalid saved narrative fields");
            }
        }
    }
    public sealed partial class CafeSimulation {public Action<CafeExchange,string,string,double,int> ExchangeObserved;}
}
