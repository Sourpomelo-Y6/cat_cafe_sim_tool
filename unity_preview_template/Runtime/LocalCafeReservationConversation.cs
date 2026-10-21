using System.Collections.Generic;
using System.Linq;
using CatCafePreview.Standalone;
namespace CatCafePreview {
 public sealed partial class LocalCafeBackend {
  List<CafeReservationConversation> reservationConversations=new List<CafeReservationConversation>();string reservationSelected;
  public bool ReservationConversationOpen {get;set;}
  public CafeReservationConversation[] ReservationConversations {get{SyncConversations();return reservationConversations.ToArray();}}
  public CafeReservationConversation ReservationConversation {get{SyncConversations();return reservationConversations.FirstOrDefault(r=>r.event_id==reservationSelected);}}
  public string ReservationConversationSelected=>ReservationConversation?.event_id;
  public bool ReservationResultPending=>ReservationConversations.Any(CafeReservationDialogue.Pending);
  public bool ReservationSafe=>Simulation.State.phase!="open" || Simulation.State.game_over!=null;
  public bool ReservationBlocksProgress=>ReservationSafe && (ReservationConversationOpen || ReservationResultPending);
  public string ReservationConversationReason=>ReservationResultPending?"特別予約の回答・接客結果は確定済みですが、結果会話が未完了です。イベント一覧の「特別予約」から再開し、会話を終えるまで進めてください。":Simulation.ReservationWaiting?"特別予約が回答待ちです。イベント一覧の「特別予約」から依頼を読み、確認画面で回答してください。":Simulation.ReservationVisitToday?"受諾した特別予約の来店当日は休業できません。営業を開始し、接客または未接客の結果を確定してください。":"";
  void SyncReservationConversations() {
   var e=Simulation.State.reservation;var row=reservationConversations.FirstOrDefault(r=>r.event_id==CafeReservationDialogue.EventId(e));
   if(row==null && (e.status=="waiting" || e.status=="accepted" && e.result==null)){
    var rule=Simulation.Rules.reservation;var view=ReservationView();
    row=new CafeReservationConversation{event_id=view.event_id,name=view.name,description=view.description,feature=view.feature_label,visit_day=e.visit_day,arrival_tick=rule.arrival_tick,open_up_count=rule.open_up_count,bonus=rule.bonus,popularity_bonus=rule.popularity_bonus,observe_result=true,response=e.status=="waiting"?CafeReservationDialogue.Start():null};reservationConversations.Add(row);
   }
   if(row!=null){CafeReservationDialogue.Sync(row,e);if(row.result!=null)row.popularity_settled=Simulation.State.phase!="open" || Simulation.State.day>e.visit_day;if(row.result!=null && string.IsNullOrEmpty(row.service_summary)){
    var v=Simulation.State.visits.FirstOrDefault(x=>x.id=="reservation-longhair" && Simulation.State.day==e.visit_day && x.satisfaction!=null);
    var h=Simulation.State.history.FirstOrDefault(x=>x.day==e.visit_day)?.customer_records.FirstOrDefault(x=>x.customer_id=="reservation-longhair" && x.satisfaction!=null);
    string cat=v?.cat_id??h?.cat_id;int count=v?.open_up_count??h?.open_up_count??0;
    row.service_summary=e.result=="unserved"?"来店日の接客完了記録がないため、未接客と確定した。":cat==null?"接客の詳細記録はありません。":"担当猫："+Simulation.Definition(cat).name+"。「心を開く」実績："+count+"回。特徴条件："+(Simulation.Definition(cat).features.Contains(Simulation.Rules.reservation.feature)?"一致":"不一致")+"。"+row.Conditions;
   }}
  }
  public bool BeginReservationConversation(string id){var row=ReservationConversations.FirstOrDefault(r=>r.event_id==id);if(!ReservationSafe || Simulation.State.game_over!=null && row?.Current?.branch_id=="introduction" || row?.Current==null || row.Current.completed)return false;reservationSelected=id;return true;}
  public void PrepareReservationConversation(){var rows=ReservationConversations;if(ReservationConversation?.Current?.completed==false && !(Simulation.State.game_over!=null && ReservationConversation.Current.branch_id=="introduction"))return;foreach(var row in rows.OrderBy(r=>Simulation.State.reservation.offered_day).ThenBy(r=>r.event_id,System.StringComparer.Ordinal))if(BeginReservationConversation(row.event_id))return;}
  void RestoreReservationConversations(CafeReservationConversation[] rows,string selected){reservationConversations=(rows??new CafeReservationConversation[0]).ToList();reservationSelected=selected;ReservationConversationOpen=false;}
 }
}
