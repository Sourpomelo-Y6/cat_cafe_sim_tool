using System;
using System.Linq;
namespace CatCafePreview.Standalone
{
    public sealed partial class CafeSimulation
    {
        public int[] AssignmentFreeSeats => Enumerable.Range(0,State.seat_count).Where(i=>!DisabledSeat(i) && !State.exchanges.Any(e=>e.seat==i)).ToArray();
        public string AssignmentCatReason(string id)
        {
            var c=State.cats.Find(r=>r.id==id);if(c==null)return "在籍する猫を選んでください。";
            if(State.exchanges.Any(e=>e.cat_id==id))return "別の席で接客中です。";
            if(c.activity!="cafe")return "在店していません。";
            if(!c.working)return "出勤していません。";
            if(c.health!="healthy")return "健康な猫が対象です。";
            return c.cannot_continue || c.stamina<=0?"接客を続けられる体力・状態が必要です。":"";
        }
        public CafeCat[] AssignmentCats => State.cats.Where(c=>AssignmentCatReason(c.id)=="").ToArray();
        public string AssignmentBlockReason()
        {
            if(State.game_over!=null)return "経営終了後は接客できません。";
            if(PlayerActive)return "進行中の交流を終了してください。";
            if(AdoptionWaiting)return "譲渡の申し出に回答してください。";
            if(DispatchWaiting || DispatchChoiceWaiting || DispatchTroubleWaiting)return "派遣の帰還結果を確認してください。";
            if(State.cats.Any(c=>c.activity=="missing" && c.missing_remaining==0 && !DispatchMissing(State,c.id)))return "帰還結果を確認してください。";
            if(GoalWaiting || PatronWaiting || BondWaiting)return "目標の結果を確認してください。";
            if(ReservationWaiting)return "特別予約に回答してください。";
            if(CustomerTrustWaiting)return "顧客の信頼回復イベントに回答してください。";
            if(GrowthWaiting)return "猫の成長・習熟を選択してください。";
            if(StoreEventWaiting)return "店舗イベントに回答してください。";
            if(IntakeWaiting || RegularIntroductionWaiting || DispatchIntroductionWaiting)return "加入イベントに回答してください。";
            return State.phase=="open"?"":State.phase=="closed"?"本日の営業は終了しています。":"先に営業を開始してください。";
        }
        public bool AssignmentWaiting => !State.auto_assignment && AssignmentBlockReason()=="" && State.queue.Count>0 && AssignmentFreeSeats.Length>0 && AssignmentCats.Length>0;
        public string AssignmentReason() {string reason=AssignmentBlockReason();return reason!=""?reason:State.auto_assignment?"自動割り当てをOFFにしてから、客・猫・空席を選んでください。":"";}
        public string AssignmentPreference(string customer) => Preference(customer);
        public bool AssignmentMatched(string cat,string customer) => Matched(State.cats.Single(c=>c.id==cat),customer);
        private void SetAutoAssignment(string choice)
        {
            if(choice!="automatic" && choice!="manual")throw new ArgumentException("自動割り当て設定が不正です。");
            State.auto_assignment=choice=="automatic";State.revision++;
        }
        private void AssignService(string customer,string cat,string seatId)
        {
            string reason=AssignmentReason();if(reason!="")throw new InvalidOperationException(reason);
            if(!State.queue.Contains(customer))throw new ArgumentException("待機中の客を選んでください。");
            reason=AssignmentCatReason(cat);if(reason!="")throw new InvalidOperationException(reason);
            int seat=EquipmentSeat(seatId);if(!AssignmentFreeSeats.Contains(seat))throw new InvalidOperationException("空いている席を選んでください。");
            StartService(customer,State.cats.Single(c=>c.id==cat),seat);State.revision++;
        }
        private void StartService(string customer,CafeCat cat,int seat)
        {
            var s=State;s.queue.Remove(customer);var visit=s.visits.Single(v=>v.id==customer);visit.cat_id=cat.id;visit.seated_at=s.tick;
            var relationship=s.relationships.Find(r=>r.cat_id==cat.id && r.customer_id==customer);var equipment=Installed(seat);
            s.exchanges.Add(new CafeExchange {service_group=s.goal.Count>0?ServiceGroup(customer):"",equipment_group=equipment?.group??"",equipment_engagement=equipment?.engagement??1,equipment_tension=equipment?.tension??1,max_stamina=s.stamina_growth==null?0:MaxStamina(cat.id),seat=seat,customer_id=customer,cat_id=cat.id,stamina=cat.stamina,remaining=Math.Min(Rules.exchange.ticks,Rules.opening_ticks-s.tick),affinity_start=relationship?.affinity??0,growth_groups=LearnedGroups(GrowthCat(cat.id)),growth_types=LearnedTypes(GrowthCat(cat.id)),tension_multiplier=Matched(cat,customer)?Rules.tension_multiplier:1});
            RecordGrowth(new CafeGrowthOperation {kind="start",cat_id=cat.id,customer_id=customer,day=s.day,tick=s.tick});
            foreach(string kind in GrowthPracticeChoices(GrowthCat(cat.id)))RecordGrowth(new CafeGrowthOperation {kind=kind,cat_id=cat.id,day=s.day,tick=s.tick});
            s.exchanges.Sort((a,b)=>a.seat.CompareTo(b.seat));
        }
    }
}
