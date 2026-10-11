using System;
using System.Linq;
namespace CatCafePreview.Standalone
{
    [Serializable] public class SpecialistRules { public string feature; public int connect_count,open_up_count,arrival_tick; public double bonus,popularity_bonus; }
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
            if(State.objective!="popularity")return;
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
        private string SpecialistResult(CafeSnapshot s,string id,string cat,int connect,int open)
        {
            if(s.objective!="popularity" || id!="advanced-white" && id!="reservation-longhair")return null;
            var r=id=="advanced-white"?Rules.advanced_customer:Rules.reservation;
            bool matched=Definitions(s).Single(d=>d.id==cat).features.Contains(r.feature);
            return matched && (id=="advanced-white"?connect>=r.connect_count:open>=r.open_up_count)?"success":"failure";
        }
        private double SpecialistBonus(string id,string result) => result!="success"?0:id=="advanced-white"?Rules.advanced_customer.bonus:Rules.reservation.bonus;
        private void SetSpecialistResult(CafeVisit v)
        {
            v.specialist_result=SpecialistResult(State,v.id,v.cat_id,v.connect_count,v.open_up_count);v.specialist_bonus=SpecialistBonus(v.id,v.specialist_result);
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
                if(s.objective=="popularity" && s.goal!=null && s.goal.Count>=2 && s.goal[1].status=="cleared" && s.day>s.goal[1].resolved_day)throw new ArgumentException("Missing reservation offer");
            }
            else
            {
                if(s.objective!="popularity" || s.goal==null || s.goal.Count<2 || s.goal[1].status!="cleared" || r.offered_day!=s.goal[1].resolved_day+1 || r.offered_day>s.day || r.visit_day!=r.offered_day+1)throw new ArgumentException("Invalid reservation dates");
                if(r.status=="waiting"?(r.offered_day!=s.day || s.phase!="preparation" || r.resolved_day!=0 || r.choice!=null || r.result!=null):r.resolved_day!=r.offered_day || r.choice!=(r.status=="accepted"?"accept":"decline"))throw new ArgumentException("Invalid reservation response");
                if(r.status=="declined" && r.result!=null || r.status=="accepted" && (r.result!=null && !new[]{"success","failure","unserved"}.Contains(r.result) || r.result==null && s.day>r.visit_day || r.result=="unserved" && s.day<=r.visit_day))throw new ArgumentException("Invalid reservation result");
            }
            int served=0;string reservationResult=null;
            Action<int,string,string,int,int,string,double,bool> validate=(day,id,cat,connect,open,result,bonus,completed)=>
            {
                if(connect<0 || open<0)throw new ArgumentException("Invalid special counts");
                string expected=completed?SpecialistResult(s,id,cat,connect,open):null;
                if(result!=expected || bonus!=SpecialistBonus(id,expected))throw new ArgumentException("Specialist result differs from service");
                if(id=="advanced-white" && (s.objective!="popularity" || s.goal==null || s.goal.Count<1 || s.goal[0].status!="cleared" || day<=s.goal[0].resolved_day))throw new ArgumentException("Locked specialist arrival");
                if(id=="reservation-longhair")
                {
                    if(r.status!="accepted" || day!=r.visit_day)throw new ArgumentException("Unexpected reservation arrival");
                    if(completed){served++;reservationResult=result;}
                }
            };
            foreach(var h in s.history)foreach(var v in h.customer_records)validate(h.day,v.customer_id,v.cat_id,v.connect_count,v.open_up_count,v.specialist_result,v.specialist_bonus,v.satisfaction!=null);
            foreach(var v in s.visits)validate(s.day,v.id,v.cat_id,v.connect_count,v.open_up_count,v.specialist_result,v.specialist_bonus,v.satisfaction!=null);
            if(served>1 || (served==1 ? r.result!=reservationResult : r.result=="success" || r.result=="failure"))throw new ArgumentException("Reservation differs from service history");
        }
    }
}
