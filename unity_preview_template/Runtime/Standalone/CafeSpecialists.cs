using System;
using System.Linq;
namespace CatCafePreview.Standalone
{
    [Serializable] public class SpecialistRules { public string feature; public int connect_count,open_up_count,simultaneous_count,arrival_tick; public double bonus,popularity_bonus; }
    [Serializable] public class LonghairRules { public double probability,bonus,popularity_bonus; public int contact_count; }
    [Serializable] public class ServiceCustomerRules { public string equipment; public int quiet_count,play_count,contact_count; public double bonus,popularity_bonus; public int[] weekdays,quiet_weekdays; }
    [Serializable] public class CafeReservation
    {
        public string status="untriggered",choice,result;
        public int offered_day,visit_day,resolved_day;
    }
    public sealed partial class CafeSimulation
    {
        public bool ReservationWaiting => State.reservation.status=="waiting";
        public bool ReservationVisitToday => State.reservation.status=="accepted" && State.reservation.visit_day==State.day;
        private void PresentReservation()
        {
            if(State.goal.Count==0)return;
            var r=State.reservation;
            if(r.status=="accepted" && r.visit_day<State.day && r.result==null)r.result="unserved";
            if(r.status=="untriggered" && ClearedGoalDay(2)>0 && State.day>ClearedGoalDay(2))
            {r.offered_day=State.day;r.visit_day=State.day+1;r.status="waiting";}
        }
        public string ReservationReason()
        {
            if(State.game_over!=null)return "経営終了後は回答できません。";
            if(!ReservationWaiting)return "回答待ちの予約はありません。";
            return State.phase=="preparation"?"":"特別予約は営業準備中に回答してください。";
        }
        private void ResolveReservation(string choice)
        {
            string reason=ReservationReason();if(reason!="")throw new InvalidOperationException(reason);
            if(choice!="accept" && choice!="decline")throw new ArgumentException("予約の回答を選んでください。");
            var r=State.reservation;r.choice=choice;r.status=choice=="accept"?"accepted":"declined";r.resolved_day=State.day;State.revision++;
        }
        private ServiceCustomerRules ServiceRule(string id) => id=="advanced-quiet"?Rules.quiet_customer:id=="advanced-play"?Rules.play_customer:id=="advanced-contact"?Rules.contact_customer:null;
        public static double LonghairDraw(int seed,int day) => PythonRandom.Hash64("longhair-customer:"+seed+":"+day)/18446744073709551616.0;
        private bool LonghairScheduled(int day) => State.objective=="popularity" && ClearedGoalDay(2)>0 && day>ClearedGoalDay(2) && LonghairDraw(State.seed,day)<Rules.longhair_customer.probability;
        private string SpecialistLabel(string id) => id=="advanced-longhair"?"長毛触れ合い条件":id=="vip-calico"?"VIP条件":id=="advanced-white"?"高難度条件":id=="reservation-longhair"?"予約条件":id=="advanced-quiet"?"静かな交流条件":id=="advanced-play"?"遊び条件":"触れ合い条件";
        private double SpecialistPopularity(string id,string result) => result!="success"?0:id=="advanced-longhair"?Rules.longhair_customer.popularity_bonus:id=="vip-calico"?Rules.vip_customer.popularity_bonus:id=="reservation-longhair"?Rules.reservation.popularity_bonus:ServiceRule(id)?.popularity_bonus??0;
        private string SpecialistResult(CafeSnapshot s,string id,string cat,int connect,int open,bool equipment=false,int count=0,int simultaneous=0)
        {
            if(s.goal.Count>0 && id=="advanced-longhair")return Definitions(s).Single(d=>d.id==cat).features.Contains("long_hair") && count>=Rules.longhair_customer.contact_count?"success":"failure";
            if(s.goal.Count>0 && id=="vip-calico")return Definitions(s).Single(d=>d.id==cat).features.Contains(Rules.vip_customer.feature) && connect>=Rules.vip_customer.connect_count && open>=Rules.vip_customer.open_up_count && simultaneous>=Rules.vip_customer.simultaneous_count?"success":"failure";
            if(s.goal.Count>0 && ServiceRule(id)!=null)
            {var rule=ServiceRule(id);return equipment && count>=rule.quiet_count+rule.play_count+rule.contact_count?"success":"failure";}
            if(s.goal.Count==0 || id!="advanced-white" && id!="reservation-longhair")return null;
            var r=id=="advanced-white"?Rules.advanced_customer:Rules.reservation;
            bool matched=Definitions(s).Single(d=>d.id==cat).features.Contains(r.feature);
            return matched && (id=="advanced-white"?connect>=r.connect_count:open>=r.open_up_count)?"success":"failure";
        }
        private double SpecialistBonus(string id,string result) => result!="success"?0:id=="advanced-longhair"?Rules.longhair_customer.bonus:id=="vip-calico"?Rules.vip_customer.bonus:id=="advanced-white"?Rules.advanced_customer.bonus:id=="reservation-longhair"?Rules.reservation.bonus:ServiceRule(id)?.bonus??0;
        private void SetSpecialistResult(CafeVisit v)
        {
            v.specialist_result=SpecialistResult(State,v.id,v.cat_id,v.connect_count,v.open_up_count,v.service_equipment,v.service_count,v.simultaneous_count);v.specialist_bonus=SpecialistBonus(v.id,v.specialist_result);
            if(v.id=="reservation-longhair" && v.specialist_result!=null)
            {
                if(State.reservation.result!=null)throw new InvalidOperationException("予約の結果は確定済みです。");
                State.reservation.result=v.specialist_result;
            }
        }
        private void ValidateSpecialists(CafeSnapshot s)
        {
            var r=s.reservation;
            if(r==null || !new[]{"untriggered","waiting","accepted","declined"}.Contains(r.status))throw new ArgumentException("Invalid reservation");
            if(r.status=="untriggered")
            {
                if(r.offered_day!=0 || r.visit_day!=0 || r.resolved_day!=0 || r.choice!=null || r.result!=null)throw new ArgumentException("Invalid empty reservation");
                if(s.goal.Count>0 && s.goal!=null && s.goal.Count>=2 && s.goal[1].status=="cleared" && s.day>s.goal[1].resolved_day)throw new ArgumentException("Missing reservation offer");
            }
            else
            {
                if(s.goal.Count==0 || s.goal==null || s.goal.Count<2 || s.goal[1].status!="cleared" || r.offered_day!=s.goal[1].resolved_day+1 || r.offered_day>s.day || r.visit_day!=r.offered_day+1)throw new ArgumentException("Invalid reservation dates");
                if(r.status=="waiting"?(r.offered_day!=s.day || s.phase!="preparation" || r.resolved_day!=0 || r.choice!=null || r.result!=null):r.resolved_day!=r.offered_day || r.choice!=(r.status=="accepted"?"accept":"decline"))throw new ArgumentException("Invalid reservation response");
                if(r.status=="declined" && r.result!=null || r.status=="accepted" && (r.result!=null && !new[]{"success","failure","unserved"}.Contains(r.result) || r.result==null && s.day>r.visit_day || r.result=="unserved" && s.day<=r.visit_day))throw new ArgumentException("Invalid reservation result");
            }
            foreach(var e in s.exchanges)if(e.connect_count<0 || e.open_up_count<0 || e.simultaneous_count<0 || e.simultaneous_count>Math.Min(e.connect_count,e.open_up_count))throw new ArgumentException("Invalid active special counts");
            int served=0;string reservationResult=null;
            Action<int,string,string,int,int,string,double,bool,bool,int,int[],int,int> validate=(day,id,cat,connect,open,result,bonus,completed,equipment,count,actions,seat,simultaneous)=>
            {
                if(connect<0 || open<0 || simultaneous<0 || simultaneous>Math.Min(connect,open) || !completed && (connect!=0 || open!=0 || simultaneous!=0))throw new ArgumentException("Invalid special counts");
                string expected=completed?SpecialistResult(s,id,cat,connect,open,equipment,count,simultaneous):null;
                string group=ServiceGroup(id);
                if(group=="" && seat!=-1 || completed && group!="" && (seat<0 || seat>=2+s.expansion.Count(p=>p.day<=day) || equipment!=ServiceEquipmentAt(s,day,seat,group)))throw new ArgumentException("Service equipment differs from journal");
                if(!completed && (equipment || count!=0 || seat!=-1) || actions==null || actions.Length!=Rules.types.Length || actions.Any(n=>n<0) || count<0 || group=="" && (equipment || count!=0) || completed && group!="" && (actions==null || actions.Length!=Rules.types.Length || count!=Rules.types.Select((t,i)=>t.group==group?actions[i]:0).Sum()))throw new ArgumentException("Invalid service count");
                if(group!="" && (s.goal.Count==0 || s.goal==null || s.goal.Count<(id=="advanced-longhair"?2:1) || s.goal[id=="advanced-longhair"?1:0].status!="cleared" || day<=s.goal[id=="advanced-longhair"?1:0].resolved_day))throw new ArgumentException("Locked service customer");
                if(id=="advanced-longhair" && s.objective!="popularity")throw new ArgumentException("Longhair customer not introduced by mid-game challenge");
                if(id=="advanced-longhair" && LonghairDraw(s.seed,day)>=Rules.longhair_customer.probability && !s.loyalty.Any(l=>l.customer_id==id && l.regular_day>0 && day>l.regular_day && ExtraWeekday(id)==(Rules.start_weekday+day-1)%7))throw new ArgumentException("Longhair arrival differs from draw or loyalty");
                if(id=="vip-calico" && (s.goal.Count==0 || s.goal==null || s.goal.Count!=3 || s.goal[2].status!="cleared" || day<=s.goal[2].resolved_day))throw new ArgumentException("Locked VIP arrival");
                if(result!=expected || bonus!=SpecialistBonus(id,expected))throw new ArgumentException("Specialist result differs from service");
                if(id=="advanced-white" && (s.goal.Count==0 || s.goal==null || s.goal.Count<1 || s.goal[0].status!="cleared" || day<=s.goal[0].resolved_day))throw new ArgumentException("Locked specialist arrival");
                if(id=="reservation-longhair")
                {
                    if(r.status!="accepted" || day!=r.visit_day)throw new ArgumentException("Unexpected reservation arrival");
                    if(completed){served++;reservationResult=result;}
                }
            };
            foreach(var h in s.history)foreach(var v in h.customer_records)validate(h.day,v.customer_id,v.cat_id,v.connect_count,v.open_up_count,v.specialist_result,v.specialist_bonus,v.satisfaction!=null,v.service_equipment,v.service_count,v.type_actions,v.service_seat,v.simultaneous_count);
            foreach(var v in s.visits)validate(s.day,v.id,v.cat_id,v.connect_count,v.open_up_count,v.specialist_result,v.specialist_bonus,v.satisfaction!=null,v.service_equipment,v.service_count,v.type_actions,v.service_seat,v.simultaneous_count);
            if(served>1 || (served==1 ? r.result!=reservationResult : r.result=="success" || r.result=="failure"))throw new ArgumentException("Reservation differs from service history");
        }
    }
}
