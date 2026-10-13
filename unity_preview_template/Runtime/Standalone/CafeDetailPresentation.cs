using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
namespace CatCafePreview.Standalone
{
    [Serializable] public class DisplayLabel {public string id,label;}
    [Serializable] public class DisplayProfile {public string id,name,personality,appearance;}
    [Serializable] public class CafePresentationCatalog
    {
        public int version;public string rules_id;public DisplayProfile[] profiles;public DisplayLabel[] traits,features,types,special_names,content_ids,growth_labels;public string[] stages,names,extra_names;
    }
    // Read-only projections; missing historical values are never reconstructed from current state.
    public sealed class CafeDetailPresentation
    {
        private readonly CafeSimulation sim;private readonly CafePresentationCatalog catalog;
        public CafeDetailPresentation(CafeSimulation simulation,CafePresentationCatalog data){sim=simulation;catalog=data;if(data==null || data.version!=1 || data.rules_id!=sim.Rules.rules_id)throw new ArgumentException("表示定義とゲームルールが一致しません。");}
        private CafeSnapshot S=>sim.State;
        public static string Number(double value)=>value.ToString("G6",CultureInfo.InvariantCulture).Replace("E","e");
        private string Label(DisplayLabel[] rows,string id,string fallback)=>rows.FirstOrDefault(r=>r.id==id)?.label ?? fallback;
        public string ContentId(string id)=>Label(catalog.content_ids,id,id);
        public string CustomerName(string id)
        {
            if(string.IsNullOrEmpty(id))return "";var special=catalog.special_names.FirstOrDefault(r=>r.id==id);if(special!=null)return special.label;
            string surname;var match=Regex.Match(id,@"\Aguest-([1-9][0-9]*)\z");
            if(match.Success){var digits=match.Groups[1].Value;int small;if(digits.Length<=2 && int.TryParse(digits,out small) && small<=catalog.names.Length)surname=catalog.names[small-1];else{int mod=0;foreach(char c in digits)mod=(mod*10+c-'0')%catalog.extra_names.Length;surname=catalog.extra_names[(mod-catalog.names.Length-1+2*catalog.extra_names.Length)%catalog.extra_names.Length];}}
            else{int mod=0;using(var sha=SHA256.Create())foreach(byte b in sha.ComputeHash(Encoding.UTF8.GetBytes(id)))mod=(mod*256+b)%catalog.extra_names.Length;surname=catalog.extra_names[mod];}
            return surname+"さん";
        }
        private string ProfileKey(string id)
        {
            var candidates=sim.RecruitmentCandidates(S);int index=Array.FindIndex(candidates,r=>r.cat.id==id);if(index>=0)return "catalog-"+(index%sim.Rules.recruitment_catalog.Length+1);
            var intro=S.dispatches.FirstOrDefault(e=>e.introduction?.cat_id==id && e.introduction.status=="accepted");return intro==null?id:"intro-"+intro.destination;
        }
        public string Profile(string id)
        {
            var d=sim.Definition(id);var row=catalog.profiles.FirstOrDefault(r=>r.id==ProfileKey(id));var traits=new List<string>{"特性："+Label(catalog.traits,d.trait_id,"なし")};
            double[] values={d.service_stress,d.service_fatigue,d.rest_fatigue,d.dispatch_reward};string[] labels={"接客ストレス","接客疲労","休養時の疲労回復","派遣報酬"};
            for(int i=0;i<values.Length;i++)if(values[i]!=1)traits.Add(labels[i]+"：通常の"+Number(values[i])+"倍");if(d.return_stress!=0)traits.Add("派遣帰還時のストレス：＋"+Number(d.return_stress)+"（経営ルール有効時）");
            var preferences=new List<string>();for(int i=0;i<sim.Rules.types.Length;i++)preferences.Add("好み："+Label(catalog.types,sim.Rules.types[i].id,sim.Rules.types[i].id)+"："+Number(d.type_preferences[i]));
            string[] intensities={"穏やか","普通","活発"};for(int i=0;i<3;i++)preferences.Add("強さの好み："+intensities[i]+"："+Number(d.intensity_preferences[i]));preferences.Add("飽きやすさ："+Number(d.boredom_decay));preferences.Add("種類切り替えへの反応："+Number(d.switch_affinity));
            return "プロフィール\n名前："+d.name+"\n猫ID："+id+"\n個性："+(row?.personality ?? "記録なし")+"\n\n特性と効果\n"+string.Join("\n",traits)+"\n\n特徴・外見\n特徴："+(d.features.Length==0?"未設定":string.Join("・",d.features.Select(f=>Label(catalog.features,f,f))))+"\n"+(row?.appearance ?? "外見の記録なし")+"\n\n交流の好み\n"+string.Join("\n",preferences);
        }
        private int JoinedDay(string id)
        {
            if(sim.Rules.cats.Any(c=>c.id==id))return 1;var shop=S.shop_purchases.FirstOrDefault(p=>p.cat_id==id);if(shop!=null)return shop.day;var rescue=S.recruitment?.accepted.FirstOrDefault(p=>p.cat_id==id);if(rescue!=null)return rescue.day;
            if(id==sim.Rules.intake.cat.id)return S.intake_request.resolved_day;if(id==sim.Rules.regular_introduction.cat.id)return S.regular_introduction.resolved_day;if(id==sim.Rules.visiting_cat?.cat.id)return S.visiting_cat.accepted_day;return S.dispatches.Single(e=>e.introduction?.cat_id==id && e.introduction.status=="accepted").introduction.resolved_day;
        }
        private int? PreviousTicks(string id)
        {
            var previous=S.history.LastOrDefault();if(previous==null || previous.day<JoinedDay(id))return null;var legacy=previous.legacy_service_ticks?.FirstOrDefault(r=>r.cat_id==id);if(legacy!=null)return legacy.ticks;
            if(previous.customer_records.Any(r=>r.service_count>1))return null;
            return previous.customer_records.Where(r=>r.cat_id==id).Sum(r=>r.service_ticks);
        }
        private double Probability(double fatigue)=>Math.Min(sim.Rules.max_probability,Math.Max(0,fatigue-sim.Rules.safe_fatigue)*sim.Rules.probability_per_fatigue);
        private string Estimate(double fatigue,bool sick)=>Number(fatigue)+" / "+(sick?"判定なし":(Probability(fatigue)*100).ToString("F1",CultureInfo.InvariantCulture)+"%");
        private static double Clamp(double value,double maximum)=>Math.Max(0,Math.Min(maximum,value));
        public string Forecast(string id)
        {
            var cat=S.cats.Single(c=>c.id==id);var d=sim.Definition(id);bool sick=cat.health=="sick",atCafe=cat.activity=="cafe";int? actions=PreviousTicks(id);
            double restFatigue=Clamp(cat.fatigue-sim.Rules.rest_day_recovery*d.rest_fatigue-sim.RestSpaceRecovery-(sim.GrowthCat(id).specialization=="rest"?sim.Rules.growth.rest_recovery_bonus:0),sim.Rules.max_fatigue);
            string work=!atCafe || sick?"出勤不可":actions.HasValue?Estimate(Clamp(cat.fatigue+actions.Value*sim.Rules.fatigue_per_service_tick*d.service_fatigue,sim.Rules.max_fatigue),false):"—";
            string workStress=!atCafe || sick?"出勤不可":actions.HasValue?Number(Clamp(cat.stress+actions.Value*sim.Rules.stress_per_service_tick*d.service_stress,100)):"—";
            string note=!atCafe?"在店していない猫は出勤・休養の対象外です。":sick?(cat.recovery_remaining<=1?"療養中は再発症判定を行いません。丸1日休むと閉店時に回復し、翌日から出勤を選べます。":"療養中は再発症判定を行いません。丸1日休むと残り療養"+(cat.recovery_remaining-1)+"日になります。"):!actions.HasValue?"前日の接客行動数の記録がないため、出勤時の予測は表示していません。":"前日の接客"+actions+"行動を基準にしています。当日の担当回数や交流時間によって、実際の疲労・発症確率・ストレスは変わります。";
            return "前日接客："+(actions.HasValue?actions+"行動":"記録なし")+"\n出勤：疲労 / 発症 "+work+" / ストレス "+workStress+"\n休養：疲労 / 発症 "+(atCafe?Estimate(restFatigue,sick):"在店していません")+" / ストレス "+(atCafe?Number(Clamp(cat.stress-sim.Rules.rest_recovery-sim.RestSpaceStressRecovery,100)):"在店していません")+"\n"+note;
        }
        public string Relationships(string id)
        {
            var known=S.relationships.Select(r=>r.customer_id).Concat(S.visits.Select(v=>v.id)).Concat(S.history.SelectMany(h=>h.customer_records.Select(r=>r.customer_id))).Concat(S.loyalty.Select(r=>r.customer_id)).Distinct().OrderBy(x=>x,StringComparer.Ordinal);
            var rows=known.Select(customer=>{var r=S.relationships.FirstOrDefault(p=>p.cat_id==id && p.customer_id==customer);double value=r?.affinity??0;int stage=(value>=10?1:0)+(value>=30?1:0)+(value>=60?1:0);return CustomerName(customer)+"（"+customer+"）\n確定済み親しみ："+Number(value)+" / "+catalog.stages[stage]+" / "+(r==null?"未交流":S.import_history!=null?"交流経験の記録なし":"交流済み");}).ToArray();
            return "関係データに保存済みの値です。交流中・未保存の変化は含みません。\n\n"+(rows.Length==0?"お客との関係記録はありません。":string.Join("\n\n",rows));
        }
        private string importBase;private int importDay;
        private int ImportedDay{get{if(S.import_history==null){importBase=null;importDay=0;return 0;}if(importBase!=S.import_history.base_json){importBase=S.import_history.base_json;importDay=CafeLegacyImport.DecodeBase(importBase).day;}return importDay;}}
        public Func<int,string,string> SavedNetStamina;
        public Func<int,string,CafeDailyRecord> SavedDailyRecord;
        public string ClosedNetStamina(string id)
        {
            var cat=S.cats.Single(c=>c.id==id);return (S.import_history==null || S.day>ImportedDay) && S.phase=="closed" && cat.activity=="cafe" && !S.dispatches.Any(e=>e.cat_id==id && (e.occurred_day==S.day || e.resolved_day==S.day))?Number(sim.Rules.max_stamina-cat.stamina):null;
        }
        public string History(string id)
        {
            var days=S.history.Select(h=>new {h.day,records=h.customer_records,resting=h.resting_cats,ticks=h.legacy_service_ticks}).ToList();
            if(S.phase=="closed")days.Add(new {S.day,records=S.visits.Select(v=>new CafeCustomerRecord{cat_id=v.cat_id,customer_id=v.id,service_ticks=v.seated_ticks,service_count=v.service_count,affinity_delta=v.affinity_delta,stamina_spent=v.stamina_spent}).ToList(),resting=S.resting_cats,ticks=(List<CafeLegacyServiceTotal>)null});
            var rows=new List<string>();foreach(var day in days)
            {
                var operations=S.growth_operations.Where(o=>o.day==day.day && o.cat_id==id).ToArray();if(day.day<JoinedDay(id))continue;
                var saved=SavedDailyRecord?.Invoke(day.day,id);if(saved!=null){rows.Add(saved.Text());continue;}
                var services=operations.Where(o=>o.kind=="service").ToArray();bool exact=S.import_history==null || day.day>ImportedDay; // Imported compact receipts cannot prove all older per-cat totals.
                string count=exact?services.Length.ToString():"記録なし";var legacy=day.ticks?.FirstOrDefault(r=>r.cat_id==id);string ticks=legacy!=null?legacy.ticks.ToString():day.records.Any(r=>r.service_count>1)?"記録なし":day.records.Where(r=>r.cat_id==id).Sum(r=>r.service_ticks).ToString();
                var current=S.cats.Single(c=>c.id==id);bool currentClosed=day.day==S.day && S.phase=="closed";string spent=(currentClosed?ClosedNetStamina(id):null) ?? SavedNetStamina?.Invoke(day.day,id) ?? "記録なし";
                string xp=exact?"接客＋"+Number(services.Length*sim.Rules.growth.service_xp)+"・休養＋"+Number(operations.Count(o=>o.kind=="rest")*sim.Rules.growth.rest_xp)+"・派遣＋"+Number(operations.Count(o=>o.kind=="dispatch")*sim.Rules.growth.dispatch_xp):"記録なし";
                rows.Add("日目："+day.day+"\n営業区分："+(operations.Any(o=>o.kind=="start")?"営業":operations.Any(o=>o.kind=="rest" && o.tick==0)?"休業":"記録なし")+"\n予定："+(currentClosed?(current.activity=="cafe"?current.working?"出勤":"休養":current.activity=="dispatched"?"派遣中":current.activity=="adopted"?"譲渡済み":"家出中"):day.resting.Contains(id)?"休養":operations.Any(o=>o.kind=="start")?"出勤":"記録なし")+"\n接客件数："+count+"\n接客行動数："+ticks+"\n体力消耗："+spent+"\n疲労変化：記録なし\n体調変化：記録なし\n獲得経験："+xp+"\n親しみ増減合計："+(exact?Number(services.Sum(o=>o.affinity_delta)):"記録なし"));
            }
            return "閉店済みの日次実績です。個々の行動履歴ではありません。体力消耗は回復を差し引いた値です。\n保存済みの日次記録を優先します。開始値がない日や旧セーブの欠落項目は補いません。\n\n"+(rows.Count==0?"閉店済みの日次実績はありません。":string.Join("\n\n",rows));
        }
        private string ItemName(string source){var purchase=S.item_purchases.FirstOrDefault(p=>p.source==source);if(purchase!=null)return sim.Rules.item_shop.Single(r=>r.item.id==purchase.item_id).item.name;var dispatch=S.dispatches.FirstOrDefault(e=>e.id==source);return dispatch!=null?sim.DispatchDestination(dispatch.destination).item_reward.name:sim.Rules.store_events.supply.name;}
        public string Events(string id)
        {
            var admission=new List<string>();var events=new List<Tuple<int,int,string>>();int order=0;
            Action<int,string,string,string> add=(day,kind,target,result)=>events.Add(Tuple.Create(day,order++,(day==0?"日付の記録なし":day+"日目")+" · "+kind+" · "+target+"\n"+result));
            Action<int,string,double> join=(day,source,cost)=>{admission.Add("加入経路："+source);admission.Add("加入日："+day+"日目");add(day,"加入",source,(source=="ペットショップ"?"購入費 ":"初期費用 ")+Number(cost));};
            var shop=S.shop_purchases.FirstOrDefault(p=>p.cat_id==id);if(shop!=null)join(shop.day,"ペットショップ",sim.Rules.pet_shop.Single(r=>r.cat.id==id).cost);
            var rescue=S.recruitment?.accepted.FirstOrDefault(p=>p.cat_id==id);if(rescue!=null)join(rescue.day,"保護猫の受け入れ",sim.RecruitmentCandidates(S).Single(r=>r.cat.id==id).cost);
            if(id==sim.Rules.intake.cat.id && S.intake_request.status=="accepted")join(S.intake_request.resolved_day,"保護猫の受け入れ依頼",sim.Rules.intake.cost);
            if(id==sim.Rules.regular_introduction.cat.id && S.regular_introduction.status=="accepted"){join(S.regular_introduction.resolved_day,"常連からの紹介",sim.Rules.regular_introduction.cost);admission.Add("紹介者："+CustomerName(S.regular_introduction.customer_id));}
            if(id==sim.Rules.visiting_cat?.cat.id && S.visiting_cat.status=="accepted"){foreach(var a in S.visiting_cat.actions)add(a.day,"店先での交流","店先",a.choice=="interact"?"交流した":"今日は見送った");join(S.visiting_cat.accepted_day,"店先に通う猫",sim.Rules.visiting_cat.cost);}
            foreach(var e in S.dispatches)
            {
                string place=sim.DispatchDestination(e.destination).name;var intro=e.introduction;if(intro?.cat_id==id && intro.status=="accepted")join(intro.resolved_day,"派遣先からの紹介",intro.cost);
                if(e.cat_id!=id)continue;add(e.started_day,"派遣出発",place,"予定期間 "+sim.DispatchDestination(e.destination).days+"日");
                if(intro!=null && intro.presented_day>0)add(intro.presented_day,"猫の紹介",place,intro.name+" / "+(intro.status=="waiting"?"回答待ち":intro.status=="accepted"?"迎えた":"見送り"));
                if(e.trouble?.missing_day>0){add(e.trouble.missing_day,"派遣中の家出",place,"派遣中断・報酬0");if(!string.IsNullOrEmpty(e.trouble.choice))add(e.trouble.choice_day,"家出トラブルへの対応",place,(e.trouble.choice=="search"?"捜索して連れ戻す":"帰還を待つ")+" / 捜索費 "+Number(e.trouble.cost));}
                if(e.encounter!=null && e.encounter.status!="scheduled"){var rule=sim.DispatchEncounterDefinition(e.destination,e.started_day);add(e.encounter.occurred_day,"派遣中の出来事",place,rule.title+(e.encounter.status=="waiting"?"（回答待ち）":"（回答済み）"));if(e.encounter.status=="resolved"){var choice=rule.choices.Single(c=>c.id==e.encounter.choice);add(e.encounter.resolved_day,"派遣イベントへの回答",place,choice.label+" / 帰還報酬の増減 "+choice.reward.ToString("+0.######;-0.######;+0",CultureInfo.InvariantCulture)+" / 疲労 "+Number(e.encounter.fatigue_before)+" → "+Number(e.encounter.fatigue_after)+" / ストレス "+Number(e.encounter.stress_before)+" → "+Number(e.encounter.stress_after));}}
                if(e.status=="waiting" || e.status=="resolved")add(e.occurred_day,"派遣帰還",place,e.status=="waiting"?"報酬受取待ち":"報酬受取済み");if(e.status=="resolved")add(e.resolved_day,"帰還報酬の受取",place,"報酬 "+Number(sim.DispatchReceivedReward(e))+" / 出発時に固定した帰還ストレス "+Number(e.return_stress));
            }
            foreach(var e in S.adoption?.events ?? new List<CafeAdoptionEvent>())if(e.cat_id==id){add(e.day,"譲渡の申し出",CustomerName(e.customer_id),e.status=="waiting"?"回答待ち":"回答済み");if(e.status=="resolved"){add(e.resolved_day,"譲渡への回答",CustomerName(e.customer_id),e.choice=="accept"?"譲渡成立":"見送り");if(e.choice=="accept"){admission.Add("譲渡先："+CustomerName(e.customer_id));admission.Add("譲渡成立日："+e.resolved_day+"日目");}}}
            foreach(var use in S.item_uses.Where(u=>u.cat_id==id))add(use.day,"用品の使用",ItemName(use.source),"1個使用 / ストレス "+Number(use.before)+" → "+Number(use.after)+" / 疲労 "+Number(use.before_fatigue)+" → "+Number(use.after_fatigue));
            var growth=sim.GrowthCat(id);
            Action<int,string,string> learned=(day,title,value)=>{if(!string.IsNullOrEmpty(value))add(day,title,"成長",Label(catalog.growth_labels,value,value));};
            learned(growth.selected_day,"得意分野の選択",growth.specialization);learned(growth.mastery_selected_day,"得意な交流の選択",growth.mastery);learned(growth.second_mastery_selected_day,"2つ目の得意な交流の選択",growth.second_mastery);
            learned(growth.type_mastery_selected_day,"得意な行動の選択",growth.type_mastery);learned(growth.second_type_mastery_selected_day,"2つ目の得意な行動の選択",growth.second_type_mastery);
            if(growth.third_group_practice!=null){var third=growth.third_group_practice;learned(third.selected_day,"3つ目の得意な交流の選択",third.mastery);if(third.individual!=null){learned(third.individual.selected_day,"3つ目の分類の得意な行動の選択",third.individual.type_mastery);if(third.individual.second!=null)learned(third.individual.second.selected_day,"3つ目の分類の追加の得意な行動の選択",third.individual.second.type_mastery);}}
            if(growth.second_group_type_practice!=null){var second=growth.second_group_type_practice;learned(second.selected_day,"2つ目の分類の得意な行動の選択",second.type_mastery);if(second.second!=null)learned(second.second.selected_day,"2つ目の分類の追加の得意な行動の選択",second.second.type_mastery);}
            var rows=events.OrderBy(e=>e.Item1==0?int.MaxValue:e.Item1).ThenBy(e=>e.Item2).Select(e=>e.Item3).ToArray();
            return "加入・譲渡の記録\n"+(admission.Count==0?"加入経路・加入日の記録はありません。":string.Join("\n",admission))+"\n\n保存済みの出来事を日付順に表示します。同日の行順は発生順を示しません。記録のない過去は補いません。\n家出の過去の発生日・対応は単独版の保存に含まれないため補いません。\n\n"+(rows.Length==0?"出来事の記録はありません。":string.Join("\n\n",rows));
        }
    }
}
