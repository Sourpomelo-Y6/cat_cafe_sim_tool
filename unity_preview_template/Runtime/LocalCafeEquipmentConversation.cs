using System.Collections.Generic;
using System.Linq;
using CatCafePreview.Standalone;
namespace CatCafePreview {
 public sealed partial class LocalCafeBackend {
  List<CafeEquipmentConversation> equipmentConversations=new List<CafeEquipmentConversation>();string equipmentSelected;
  public bool EquipmentConversationOpen {get;set;}
  public CafeEquipmentConversation[] EquipmentConversations {get{SyncConversations();return equipmentConversations.ToArray();}}
  public CafeEquipmentConversation EquipmentConversation {get{SyncConversations();return equipmentConversations.FirstOrDefault(r=>r.event_id==equipmentSelected);}}
  public string EquipmentConversationSelected=>EquipmentConversation?.event_id;
  public bool EquipmentResultPending=>EquipmentConversations.Any(r=>!r.progress.completed && r.progress.branch_id!="introduction");
  // An open introduction does not prevent the existing day-off confirmation from previewing.
  public bool EquipmentBlocksProgress=>EquipmentResultPending;
  public string EquipmentConversationReason=>EquipmentResultPending?"店舗イベントの回答は確定済みですが、結果会話が未完了です。イベント一覧の「店舗イベント」から前日の案件も再開し、「会話を終える」まで進めてください。":"";
  void SyncEquipmentConversations(){
   var e=Simulation.CurrentStoreEvent;
   if(CafeEquipmentDialogue.Supports(e.type) && e.status=="waiting" && !equipmentConversations.Any(r=>r.day==e.day)){
    var options=StoreEventView();var rest=e.type=="trouble"?DayOffView():null;
    equipmentConversations.Add(new CafeEquipmentConversation{day=e.day,event_id=CafeEquipmentDialogue.EventId(e.day,e.type),progress=CafeEquipmentDialogue.Start(e.type),repair_effect=options.choices[0].detail,patch_effect=options.choices[1].detail,explanation=string.Join("\n",options.choices.Where(c=>c.choice!="close").Select(c=>c.label+"："+c.detail))+(rest==null?"\n回答するまで営業・休業は進められません。閉じた会話はイベント一覧の「店舗イベント」から再開できます。":"\n休業："+(rest.can_select?rest.details:rest.reason))});
   }
   foreach(var r in equipmentConversations){var source=Simulation.State.store_events.First(x=>x.day==r.day);if(r.progress.branch_id=="introduction" && source.choice!=null){r.progress.branch_id=source.choice;r.progress.node=r.progress.page_offset=0;
    r.result=source.type=="support"?("回答時の支出："+source.cost.ToString("0.#")+"。支出後の資金："+Simulation.State.funds.ToString("0.#")+"。"+(Simulation.State.game_over!=null?"資金が尽き、経営終了となりました。閉店していないため、支援による人気加算は未反映です。":source.choice=="decline"?"支出・支援による人気加算はありません。":"支援による人気 +"+source.popularity.ToString("0.#")+" は閉店時の人気計算に加算します（上限 "+Simulation.Rules.popularity.cap.ToString("0.#")+"）。回答時には人気を変更しません。")):source.choice=="repair"?"席は通常どおり使用できます。"+r.repair_effect+"を運営費に加算します。回答時の支出はありません。":source.choice=="patch"?r.patch_effect+"。停止するのは末尾の席です。席を停止しても基本運営費は減りません。":"休業の精算を反映しました。資金："+Simulation.State.funds.ToString("0.#")+"。"+(Simulation.State.game_over!=null?"精算で経営終了となり、翌日には進みません。":Simulation.State.day>r.day?Simulation.State.day+"日目の準備へ進みました。":"成長などの確認待ちのため、閉店状態で日程の進行を待っています。")+"費用・猫の変化は休業前の確認内容に従って反映済みです。";
   }}
  }
  public void SelectEquipmentConversation(string id){if(EquipmentConversations.Any(r=>r.event_id==id && !r.progress.completed))equipmentSelected=id;}
  public void PrepareEquipmentConversation(){var rows=EquipmentConversations;if(rows.Any(r=>r.event_id==equipmentSelected && !r.progress.completed))return;var pending=rows.Where(r=>!r.progress.completed).OrderBy(r=>r.day).FirstOrDefault();equipmentSelected=pending?.event_id;}
  void RestoreEquipmentConversations(CafeEquipmentConversation[] rows,string selected){equipmentConversations=(rows??new CafeEquipmentConversation[0]).ToList();equipmentSelected=selected;EquipmentConversationOpen=false;}
 }
}
