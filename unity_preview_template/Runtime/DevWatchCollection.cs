#if CATCAFE_DEV_ACQUISITION
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public sealed partial class DevWatchSession
    {
        DevCollectionRoute[] watchRoutes;int routeIndex;bool nextRoutePending;GameObject collectionDetail;string playerCat;
        readonly List<DevAcquisitionResult> routeResults=new List<DevAcquisitionResult>();
        DevCollectionRoute CurrentRoute=>watchRoutes==null?null:watchRoutes[routeIndex];
        void PrepareRoutes()
        {
            if(!plan.collection){watchRoutes=new[]{new DevCollectionRoute{id="demo",objective=plan.objective}};return;}
            var rules=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);
            var all=CafeDevAcquisition.CreateRoutes(rules,new DevAcquisitionPlan{collection_routes=true});
            var ids=plan.route_ids??new string[0];if(ids.Distinct().Count()!=ids.Length || ids.Any(id=>!all.Any(r=>r.id==id)))throw new ArgumentException("Unknown or duplicate watch collection route");
            watchRoutes=ids.Length==0?all:ids.Select(id=>all.Single(r=>r.id==id)).ToArray();
            if(routeIndex<0 || routeIndex>=watchRoutes.Length)throw new ArgumentException("Invalid watch route checkpoint");
            if(resume)for(int i=0;i<routeIndex;i++)routeResults.Add(JsonUtility.FromJson<DevAcquisitionResult>(File.ReadAllText(Path.Combine(root,"routes",watchRoutes[i].id,"watch-result.json"))));
        }
        int AssignedAcquired()=>backend==null || CurrentRoute==null?0:CafeDevAcquisition.TargetsFor(backend.Simulation,CurrentRoute).Count(t=>t.assigned && t.status=="acquired");
        bool Wants(string id)=>CurrentRoute.targets.Length==0 || CurrentRoute.targets.Contains("cat/"+id);
        string[] CollectionAnswers(AcquisitionConversationPanel d)
        {
            bool accept=true;var rules=backend.Simulation.Rules;
            if(d is IntakeConversationPanel)accept=Wants(rules.intake.cat.id);
            if(d is RegularConversationPanel)accept=Wants(rules.regular_introduction.cat.id);
            if(d is CocoConversationPanel)accept=Wants(rules.visiting_cat.cat.id);
            if(d is DispatchIntroductionConversationPanel){var e=backend.DispatchIntroductionConversation;accept=e!=null && Wants("intro-"+backend.Simulation.State.dispatches.Single(x=>x.id==e.event_id).destination);}
            if(d is EncounterConversationPanel || d is AdoptionConversationPanel)accept=false;
            if(d is LodgeConversationPanel)accept=d.PlaybackAcceptCaption=="帰還を確認する";
            if(d is EquipmentConversationPanel)return new[]{"見送り","修理","応急処置"};
            return accept?new[]{d.PlaybackAcceptCaption,d.PlaybackDeclineCaption}:new[]{d.PlaybackDeclineCaption};
        }
        void FinishCollectionRoute()
        {
            CaptureRoute();completeAfterSave=false;
            if(routeIndex+1<watchRoutes.Length){nextRoutePending=true;paused=false;pending=null;next="担当対象を獲得して保存しました。次のルートへ移ります。";Publish();}
            else {complete=true;paused=true;next="選んだ収集ルートを完走し、結果を保存しました。";Publish();WriteCollectionSummary();if(exitOnComplete)Application.Quit(0);}
        }
        void CaptureRoute()
        {
            if(routeResults.Any(r=>r.route_id==CurrentRoute.id))return;
            var targets=CafeDevAcquisition.TargetsFor(backend.Simulation,CurrentRoute);var viewed=new HashSet<string>(backend.Replays.Load().entries.Select(e=>e.Key));
            foreach(var t in targets.Where(t=>t.category=="replay")){t.status=viewed.Contains(t.id)?"acquired":"unreached";t.reason=viewed.Contains(t.id)?"normal_ui_dialogue_reading":"branch_not_observed";}
            foreach(var t in targets.Where(t=>t.category=="image"))t.reason="aggregate_image_coverage_not_claimed; observed_keys_in_watch_status";
            var result=new DevAcquisitionResult{layer="ui-handlers",route_id=CurrentRoute.id,objective=CurrentRoute.objective,status="acquired",reason="watch_route_completed",completion_scope=CurrentRoute.targets.Length>0?"assigned_collection_targets":"selected_objective_or_free_duration",assigned_targets=CurrentRoute.targets,seed=backend.Simulation.State.seed,day=backend.Simulation.State.day,operations=commands,state_digest=CafeReplayJournal.Digest(backend.Simulation.State),targets=targets};
            routeResults.Add(result);Write(Path.Combine("routes",CurrentRoute.id,"watch-result.json"),JsonUtility.ToJson(result,true));WriteCollectionSummary();
        }
        void WriteCollectionSummary()
        {
            var union=routeResults.SelectMany(r=>r.targets).GroupBy(t=>t.category+"/"+t.id).Select(g=>
            {
                var best=g.FirstOrDefault(t=>t.status=="acquired")??g.FirstOrDefault(t=>t.status!="excluded")??g.First();
                return new DevAcquisitionTarget{category=best.category,id=best.id,status=best.status,reason=best.reason,assigned=g.Any(t=>t.assigned),evidence_routes=routeResults.Where(r=>r.targets.Any(t=>t.category==best.category && t.id==best.id && t.status=="acquired")).Select(r=>r.route_id).ToArray()};
            }).ToArray();
            Write("watch-collection.json",JsonUtility.ToJson(new DevAcquisitionSummary{layer="ui-handlers",status=complete?"completed":"running",routes=routeResults.ToArray(),union=union,pending_objectives=watchRoutes.Select(r=>r.id).Except(routeResults.Select(r=>r.route_id)).ToArray()},true));
        }
        void AdvanceWatchRoute()
        {
            CaptureRoute();backend.CommandObserved-=Observe;routeIndex++;preparedDay=0;playerCat=null;collectionDetail=null;nextRoutePending=false;started=false;loaded=false;resume=false;completeAfterSave=false;
            backend=new LocalCafeBackend(SaveDirectory,playOpening:true);backend.CommandObserved+=Observe;CafeBackends.UseLocal(backend);SceneManager.LoadScene("CatCafeTitle");lastChange=Time.realtimeSinceStartup;
        }
        bool CollectionRequired(string kind)
        {
            var c=Client;var list=c.GetComponent<PythonEventListPanel>();
            if(kind=="player"){ApplyCollectionOperation(CafeDevAcquisition.ProposeOperation(backend.Simulation,CurrentRoute));return true;}
            if(kind=="missing_return")
            {var p=c.GetComponent<PythonMissingPanel>();if(p.panel.activeInHierarchy && p.confirm.IsInteractable()){Click(p.confirm,"帰還した猫と費用を確認します。");return true;}}
            int route=kind=="dispatch_return"?(backend.Simulation.State.dispatches.Any(e=>e.status=="waiting" && backend.Simulation.IsPatronDestination(e.destination))?6:10):kind=="dispatch_choice"?11:kind=="dispatch_trouble"?12:kind=="dispatch_introduction"?13:-1;
            if(route>=0){Schedule("派遣の出来事・帰還を開いて確認します。",()=>list.ShowMenu(route));return true;}
            return false;
        }
        bool CollectionPendingResult()
        {
            if(!backend.ConversationBlocksProgress)return false;
            var list=Client.GetComponent<PythonEventListPanel>();
            var row=list.PendingCases().FirstOrDefault(r=>!r.optional && r.group!=3 && string.IsNullOrEmpty(r.reason));
            if(row==null)return false;
            Schedule("先に確認できる出来事・結果会話を読みます："+row.title,()=>{list.Close();row.resume();});return true;
        }
        void ChooseCollectionOperation()
        {
            var sim=backend.Simulation;var s=sim.State;bool done=CurrentRoute.targets.Length>0?AssignedAcquired()==CurrentRoute.targets.Length:CurrentRoute.objective=="free"?s.day>30:sim.AutoTargetStatus(CurrentRoute.objective)=="cleared" && (CurrentRoute.objective!="popularity" || s.goal.Count==sim.Rules.goal_stages.Length);
            if(done && !backend.ConversationBlocksProgress){completeAfterSave=true;RequestSave(false);return;}
            if(s.day>150){Fail("収集ルートが150日の上限に達しました："+CurrentRoute.id);return;}
            if(backend.ConversationBlocksProgress && Client.GetComponent<PythonEventListPanel>().RequiredPendingCount>0)
            {var list=Client.GetComponent<PythonEventListPanel>();Schedule("残っている結果会話を開きます。",()=>{list.Open();var b=list.entries.FirstOrDefault(x=>x.gameObject.activeInHierarchy && x.IsInteractable());if(b!=null)b.onClick.Invoke();});return;}
            ApplyCollectionOperation(CafeDevAcquisition.ProposeOperation(sim,CurrentRoute));
        }
        void OpenCollectionPanel(GameObject panel,Action open,string reason)
        {Schedule(reason,()=>{CloseCollectionDetails();open();collectionDetail=panel;});}
        void CloseCollectionDetails()
        {var c=Client;c.GetComponent<PythonPlayerPanel>().Close();c.GetComponent<PythonVisitingCatPanel>().Close();c.GetComponent<PythonMenuPanel>().CloseDetails();c.GetComponent<PythonMenuPanel>().Close();c.GetComponent<PythonEventListPanel>().ReturnToBusiness();collectionDetail=null;}
        bool Panel(GameObject panel,Action open,string reason)
        {if(!panel.activeInHierarchy){OpenCollectionPanel(panel,open,reason);return false;}collectionDetail=panel;return true;}
        void ConfirmPanel(GameObject panel,GameObject confirmation,Action open,Button choose,Button confirm,string reason)
        {if(!Panel(panel,open,reason))return;Click(confirmation.activeInHierarchy?confirm:choose,confirmation.activeInHierarchy?"表示した費用・対象を確認して確定します。":reason);}
        void ApplyCollectionOperation(CafeAutoOperation op)
        {
            if(op==null){Fail("画面へ渡す操作が見つかりません。");return;}var c=Client;var state=c.CurrentState;
            if(op.kind=="dispatch")op.kind=backend.Simulation.IsPatronDestination(op.choice)?"dispatch_patron":"dispatch_general";
            switch(op.kind)
            {
                case "purchase_housing":case "upgrade_housing":
                    var h=c.GetComponent<PythonHousingPanel>();ConfirmPanel(h.panel,h.confirmation,h.Open,h.expand,h.confirm,"担当猫の枠を確保するため、飼育スペースの拡張内容を開きます。");return;
                case "expand_seats":
                    var e=c.GetComponent<PythonExpansionPanel>();ConfirmPanel(e.panel,e.confirmation,e.Open,e.proceed,e.confirm,"人気目標で解放した席を増設します。");return;
                case "purchase_rest_space":case "upgrade_rest_space":case "soundproof_rest_space":
                    var r=c.GetComponent<PythonRestSpacePanel>();ConfirmPanel(r.panel,r.confirmation,r.Open,op.kind=="purchase_rest_space"?r.purchase:op.kind=="upgrade_rest_space"?r.upgrade:r.soundproof,r.confirm,"猫を休める設備の費用と効果を確認します。");return;
                case "purchase_waiting_area":case "upgrade_waiting_area":
                    var w=c.GetComponent<PythonWaitingAreaPanel>();ConfirmPanel(w.panel,w.confirmation,w.Open,op.kind=="purchase_waiting_area"?w.purchase:w.upgrade,w.confirm,"待合スペースの費用・維持費を確認します。");return;
                case "purchase_cat":
                    var shop=c.GetComponent<PythonPetShopPanel>();if(!Panel(shop.panel,shop.Open,"ペットショップで担当する猫を確認します。"))return;
                    if(shop.Row?.cat_id!=op.cat_id){Click(shop.next,"迎える猫の紹介を順番に見ます。");return;}Click(shop.confirmation.activeInHierarchy?shop.confirm:shop.purchase,"この猫を迎える費用と空き枠を確認します。");return;
                case "open_recruitment":case "recruit_cat":
                    var recruit=c.GetComponent<PythonRecruitmentPanel>();if(!Panel(recruit.panel,recruit.Open,"保護猫の紹介候補を確認します。"))return;
                    if(op.kind=="open_recruitment"){Schedule("次の紹介候補へ巡回します。",recruit.Open);return;}
                    if(recruit.Row?.cat_id!=op.cat_id){Click(recruit.next,"担当する系統の猫まで候補を見ていきます。");return;}Click(recruit.confirmation.activeInHierarchy?recruit.confirm:recruit.purchase,"紹介猫を迎える費用と空き枠を確認します。");return;
                case "purchase_item":
                    var items=c.GetComponent<PythonItemsPanel>();if(!Panel(items.panel,items.Open,"用品の購入画面を開きます。"))return;
                    if(!items.buy.gameObject.activeInHierarchy){Schedule("購入できる用品を表示します。",items.ShowPurchase);return;}
                    int item=Array.FindIndex(state.items.shop,i=>i.choice==op.choice);if(items.shop.value!=item){Schedule("目的の用品を選びます。",()=>{items.shop.value=item;items.SelectShop(item);});return;}
                    Click(items.confirmation.activeInHierarchy?items.confirm:items.buy,"用品の価格と購入後の資金を確認します。");return;
                case "purchase_seat_equipment":
                    var eq=c.GetComponent<PythonSeatEquipmentPanel>();if(!Panel(eq.panel,eq.Open,"接客設備の購入・設置画面を開きます。"))return;
                    if(eq.SelectedSeatId!=op.seat_id){Click(eq.seatSelector,"設置する席を選びます。");return;}if(eq.SelectedKindId!=op.choice){Click(eq.kindSelector,"購入する設備を選びます。");return;}
                    Click(eq.confirmation.activeInHierarchy?eq.confirm:eq.purchase,"接客設備の費用と設置先を確認します。");return;
                case "resolve_visiting_cat":
                    var visiting=c.GetComponent<PythonVisitingCatPanel>();if(!Panel(visiting.panel,visiting.Open,"店先に来る猫の様子を確認します。"))return;
                    Click(visiting.confirmation.activeInHierarchy?visiting.confirm:op.choice=="accept"?visiting.accept:visiting.interact,"店先の猫と交流し、迎える準備を進めます。");return;
                case "dispatch_general":case "dispatch_patron":
                    if(op.kind=="dispatch_general") {var p=c.GetComponent<PythonGeneralDispatchPanel>();if(!Panel(p.panel,()=>p.OpenOption(op.cat_id,op.choice),"派遣する猫と行き先を画面で確認します。"))return;if(p.SelectedCatId!=op.cat_id || p.SelectedDestination!=op.choice){Schedule("条件に合う猫と行き先を選びます。",()=>p.OpenOption(op.cat_id,op.choice));return;}Click(p.send,"出発内容を確認して、派遣の会話へ進みます。");return;}
                    else {var p=c.GetComponent<PythonDispatchPanel>();if(!Panel(p.panel,()=>p.OpenOption(op.cat_id,op.choice),"有力者へ派遣する猫と条件を確認します。"))return;if(p.SelectedCatId!=op.cat_id || p.SelectedDestination!=op.choice){Schedule("希望に合う猫と行き先を選びます。",()=>p.OpenOption(op.cat_id,op.choice));return;}Click(p.send,"出発の会話で条件を確認します。");return;}
                case "player_begin":
                    var detail=c.GetComponent<PythonCatDetailsPanel>();if(!detail.panel.activeInHierarchy || playerCat!=op.cat_id){OpenCollectionPanel(detail.panel,()=>{playerCat=op.cat_id;detail.Open(op.cat_id);},"好感度を育てる猫の詳細を開きます。");return;}Click(detail.interact,"この猫との個別交流を始めます。");return;
                case "player_step":case "player_finish":
                    var player=c.GetComponent<PythonPlayerPanel>();if(!Panel(player.panel,player.Open,"進行中の猫との交流を表示します。"))return;
                    if(op.kind=="player_finish"){Click(player.finish,"交流の結果を確認します。");return;}
                    int target=op.choice=="switch"?Array.FindIndex(state.player_interaction.targets,t=>t.choice==op.source)+1:0;
                    if(player.targets.value!=target){Schedule("次に使う交流方法を選びます。",()=>{player.targets.value=target;player.TargetChanged(target);});return;}
                    int action=Array.FindIndex(state.player_interaction.actions,a=>a.choice==op.choice);if(action<0){Fail("交流の選択肢がありません："+op.choice);return;}Click(player.actions[action],"猫の反応と体力を見て「"+state.player_interaction.actions[action].label+"」を選びます。");return;
            }
            bool details=collectionDetail!=null && collectionDetail.activeInHierarchy || c.GetComponent<PythonCatDetailsPanel>().panel.activeInHierarchy || c.GetComponent<PythonPlayerPanel>().panel.activeInHierarchy;
            if(details){Schedule("確認を終え、営業画面へ戻ります。",CloseCollectionDetails);return;}
            var prep=c.GetComponent<PythonPreparationPanel>();
            if(op.kind=="set_shifts")
            {
                if(!prep.panel.activeInHierarchy){Schedule("出勤と休養を設定する画面を開きます。",prep.Open);return;}
                if(prep.HasProposal){Schedule("現在の案を取り消し、担当する猫を選びます。",prep.CancelProposal);return;}
                var change=state.cats.FirstOrDefault(cat=>cat.working!=op.working.Contains(cat.cat_id));if(change!=null)Schedule(change.name+"を"+(op.working.Contains(change.cat_id)?"出勤":"休養")+"にします。",()=>prep.ShiftCat(change.cat_id,op.working.Contains(change.cat_id)));return;
            }
            if(op.kind=="start_business"){if(!prep.panel.activeInHierarchy){Schedule("営業開始の準備を確認します。",prep.Open);return;}Click(prep.start,"出勤を確認し、営業を始めます。");return;}
            if(op.kind=="advance_business"){Click(c.GetComponent<ContentPreview>().GetSceneButton(2),"接客を1刻進め、猫とお客さんの反応を見ます。");return;}
            if(op.kind=="next_day"){var close=c.GetComponent<PythonClosingPanel>();if(!close.panel.activeInHierarchy){Schedule("閉店結果を確認します。",close.Open);return;}Click(close.next,"売上と猫の状態を確認し、翌日へ進みます。");return;}
            if(op.kind=="day_off"){Schedule("今日は猫を休ませるため休業します。",c.DayOff);return;}
            if(op.kind=="set_auto_assignment"){Schedule("通常の自動割り当てを有効にします。",c.ToggleAutoAssignment);return;}
            Fail("未対応の画面操作："+op.kind);
        }
    }
}
#endif
