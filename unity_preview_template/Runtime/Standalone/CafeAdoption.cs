using System;
using System.Linq;
using System.Collections.Generic;
namespace CatCafePreview.Standalone
{
    [Serializable] public class CafeAdoption {public string id;public bool enabled;public List<CafeAdoptionEvent> events=new List<CafeAdoptionEvent>();}
    [Serializable] public class CafeAdoptionEvent {public string id,cat_id,customer_id,source,status="waiting",choice;public int day,tick,resolved_day,resolved_tick;public double guest_affinity,player_affinity,threshold;}
    public sealed partial class CafeSimulation
    {
        public bool AdoptionWaiting=>State.adoption!=null && State.adoption.events.Any(e=>e.status=="waiting");
        public int ActiveCatCount(CafeSnapshot s)=>s.cats.Count(c=>c.activity!="adopted");
        private int AdoptedBy(CafeSnapshot s,int day,int tick)=>(s.adoption?.events ?? new List<CafeAdoptionEvent>()).Count(e=>e.status=="resolved" && e.choice=="accept" && (e.resolved_day<day || e.resolved_day==day && e.resolved_tick<=tick));
        public string AdoptionConfigureReason()=>ItemsReason();
        private void ConfigureAdoption(string choice)
        {if(choice!="on" && choice!="off")throw new ArgumentException("譲渡イベントをON/OFFで指定してください。");string reason=AdoptionConfigureReason();if(reason!="")throw new InvalidOperationException(reason);if(State.adoption==null)State.adoption=new CafeAdoption {id="adoption"};State.adoption.enabled=choice=="on";State.revision++;}
        private void ConsiderAdoption(string cat,CafeVisit visit,double affinity)
        {
            var a=State.adoption;if(a==null || !a.enabled || State.cats.Single(c=>c.id==cat).activity!="cafe")return;string id="adoption-"+State.day+"-"+cat;double player=PlayerCat(cat).affinity;
            if(affinity<Rules.adoption_threshold || affinity<=player || a.events.Any(e=>e.id==id))return;
            a.events.Add(new CafeAdoptionEvent {id=id,cat_id=cat,customer_id=visit.id,source="service-"+State.day+"-"+cat+"-"+visit.id,day=State.day,tick=State.tick,guest_affinity=affinity,player_affinity=player,threshold=Rules.adoption_threshold});
        }
        public string AdoptionResponseReason(string id)
        {
            if(State.game_over!=null)return "経営終了後は回答できません。";var e=State.adoption?.events.SingleOrDefault(e=>e.id==id);if(e==null || e.status!="waiting")return "回答待ちの譲渡申し出を選んでください。";
            return PlayerActive || State.cats.Single(c=>c.id==e.cat_id).activity!="cafe" || State.exchanges.Any(x=>x.cat_id==e.cat_id)?"この猫の譲渡は現在確定できません。":"";
        }
        private void ResolveAdoption(string id,string choice)
        {
            var e=State.adoption?.events.SingleOrDefault(e=>e.id==id);if(e==null || choice!="accept" && choice!="decline")throw new ArgumentException("譲渡の申し出と選択肢を確認してください。");
            if(e.status=="resolved"){if(e.choice!=choice)throw new InvalidOperationException("解決済みの申し出の選択は変更できません。");return;}
            string reason=AdoptionResponseReason(id);if(reason!="")throw new InvalidOperationException(reason);
            if(choice=="accept"){var c=State.cats.Single(c=>c.id==e.cat_id);c.activity="adopted";c.working=false;}
            e.status="resolved";e.choice=choice;e.resolved_day=State.day;e.resolved_tick=State.tick;State.revision++;
        }
        private void ValidateAdoption(CafeSnapshot s)
        {
            var a=s.adoption;if(a==null){if(s.cats.Any(c=>c.activity=="adopted"))throw new ArgumentException("Missing adoption record");return;}
            if(a.id!="adoption" || a.events==null || a.events.Any(e=>e==null) || a.events.Count>0 && s.player_sessions==null || a.events.Select(e=>e.id).Distinct().Count()!=a.events.Count)throw new ArgumentException("Invalid adoption history");
            var adopted=new HashSet<string>();
            foreach(var e in a.events)
            {
                var cat=s.cats.SingleOrDefault(c=>c.id==e.cat_id);if(cat==null || e.day<1 || e.day>s.day || e.tick<0 || e.tick>=Rules.opening_ticks || e.day==s.day && e.tick>s.tick || e.id!="adoption-"+e.day+"-"+e.cat_id || e.source!="service-"+e.day+"-"+e.cat_id+"-"+e.customer_id || e.threshold!=Rules.adoption_threshold || !Range(e.guest_affinity,e.threshold,100) || !Range(e.player_affinity,0,100) || e.guest_affinity<=e.player_affinity)throw new ArgumentException("Invalid adoption conditions");
                var record=e.day==s.day?s.visits.Where(v=>v.id==e.customer_id && v.cat_id==e.cat_id && v.satisfaction!=null).Select(v=>new CafeCustomerRecord {cat_id=v.cat_id,customer_id=v.id,completed_tick=v.completed_tick,affinity_delta=v.affinity_delta}).SingleOrDefault():s.history[e.day-1].customer_records.SingleOrDefault(v=>v.customer_id==e.customer_id && v.cat_id==e.cat_id && v.satisfaction!=null);
                double affinity=s.history.Where(h=>h.day<=e.day).SelectMany(h=>h.customer_records.Where(v=>v.customer_id==e.customer_id && v.cat_id==e.cat_id)).Sum(v=>v.affinity_delta)+(e.day==s.day?s.visits.Where(v=>v.id==e.customer_id && v.cat_id==e.cat_id).Sum(v=>v.affinity_delta):0);
                double player=s.player_sessions.Where(p=>p.cat_id==e.cat_id && p.day<=e.day && p.exchange.end_reason!=null).Select(p=>Math.Max(0,Math.Min(100,p.initial_affinity+p.exchange.affinity_pending))).LastOrDefault();
                if(record==null || record.completed_tick!=e.tick || Math.Abs(affinity-e.guest_affinity)>1e-8 || player!=e.player_affinity)throw new ArgumentException("Adoption differs from service or player history");
                if(e.status=="waiting"){if(e.choice!=null || e.resolved_day!=0 || e.resolved_tick!=0 || !a.enabled || e.day!=s.day || cat.activity!="cafe")throw new ArgumentException("Invalid pending adoption");}
                else if(e.status!="resolved" || e.choice!="accept" && e.choice!="decline" || e.resolved_day!=e.day || e.resolved_tick<e.tick || e.resolved_tick>Rules.opening_ticks || e.day==s.day && e.resolved_tick>s.tick)throw new ArgumentException("Invalid adoption answer");
                if(e.status=="resolved" && e.choice=="accept" && (!adopted.Add(e.cat_id) || cat.activity!="adopted" || cat.working))throw new ArgumentException("Invalid adopted cat");
            }
            if(!adopted.SetEquals(s.cats.Where(c=>c.activity=="adopted").Select(c=>c.id)))throw new ArgumentException("Adoption roster mismatch");
        }
    }
}
