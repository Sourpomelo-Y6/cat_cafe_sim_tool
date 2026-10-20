using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using CatCafePreview.Standalone;

namespace CatCafePreview
{
    public sealed partial class LocalCafeBackend : ICafeBackend
    {
        public readonly CafeSimulation Simulation;
        public CafeImageGalleryStore Gallery {get;private set;}
        private readonly CafeSaveStore saves;
        private CafeSnapshot newGameSaveState;
        private string displayEpoch;private System.Collections.Generic.List<CafeDisplayReceipt> displayReceipts=new System.Collections.Generic.List<CafeDisplayReceipt>();
        public CafeNarratives Narratives {get;private set;}
        public CafeDailyRecords DailyRecords {get;private set;}
        public CafeDetailPresentation Details {get;private set;}
        public CafeReplayJournal Journal {get;private set;}
        // Cache the response AND payload: one ID must never name different commands.
        private readonly Dictionary<string, KeyValuePair<string, CafeResponse>> replies = new Dictionary<string, KeyValuePair<string, CafeResponse>>();
        readonly bool playOpening;
        public LocalCafeBackend(string saveDirectory = null, CafeRules rules = null, bool playOpening = false)
        {
            this.playOpening=playOpening;
            if (rules == null)
            {
                var asset = Resources.Load<TextAsset>("CatCafeStandalone/rules");
                if (asset == null) throw new InvalidOperationException("単独版の初期設定がありません。");
                rules = JsonUtility.FromJson<CafeRules>(asset.text);
                var staminaRules=Resources.Load<TextAsset>("CatCafeStandalone/stamina_growth");
                if(staminaRules!=null)rules.stamina_growth=JsonUtility.FromJson<CafeStaminaRules>(staminaRules.text);
            }
            CafeLegacyImport.DecodeBase=text=>DecodeSave(text).state;
            this.saveDirectory=saveDirectory??Path.Combine(Application.persistentDataPath,"standalone-v1");
            Gallery=new CafeImageGalleryStore(this.saveDirectory,value=>JsonUtility.ToJson(value,true),text=>JsonUtility.FromJson<CafeImageProfile>(text));
            Replays=new CafeReplayStore(this.saveDirectory,value=>JsonUtility.ToJson(value,true),text=>JsonUtility.FromJson<CafeReplayProfile>(text));
            Simulation = new CafeSimulation(rules);
            var presentation=Resources.Load<TextAsset>("CatCafeStandalone/presentation");if(presentation==null)throw new InvalidOperationException("単独版の表示定義がありません。");Details=new CafeDetailPresentation(Simulation,JsonUtility.FromJson<CafePresentationCatalog>(presentation.text));displayEpoch=Simulation.State.instance_id;Details.SavedNetStamina=(day,id)=>displayEpoch==Simulation.State.instance_id?displayReceipts.FirstOrDefault(r=>r.day==day && r.cat_id==id)?.net_stamina:null;
            Narratives=new CafeNarratives(Simulation){CustomerName=Details.CustomerName,SpecialImage=(cat,scene)=>CafeNarrativeResources.SceneResource(Narratives.Catalog,cat,scene)};
            try{Narratives.SetCatalog(CafeNarrativeResources.Load());}catch(Exception ex){Debug.LogWarning("交流文は試作文を使用します："+ex.Message);}
            DailyRecords=new CafeDailyRecords(Simulation,true);Details.SavedDailyRecord=DailyRecords.Find;
            ResetJournal();
            saves = new CafeSaveStore(this.saveDirectory, EncodeSaveWithDisplay, DecodeSave, Simulation.Validate);
        }
        private string EncodeSaveWithDisplay(CafeSaveEnvelope save){RememberViewedConversations();save.opening=ReferenceEquals(save.state,newGameSaveState)?newGameOpening:ReferenceEquals(save.state,Simulation.State)?Opening:null;save.patron_ending=ReferenceEquals(save.state,Simulation.State)?PatronEnding:null;save.bond_ending=ReferenceEquals(save.state,Simulation.State)?BondEnding:null;save.popularity_ending=ReferenceEquals(save.state,Simulation.State)?PopularityEnding:null;save.conversation=ReferenceEquals(save.state,Simulation.State)?Conversation:null;save.regular_conversation=ReferenceEquals(save.state,Simulation.State)?RegularConversation:null;save.dispatch_conversations=ReferenceEquals(save.state,Simulation.State)?DispatchConversations:null;save.dispatch_conversation_selected=ReferenceEquals(save.state,Simulation.State)?DispatchConversationSelected:null;save.encounter_conversations=ReferenceEquals(save.state,Simulation.State)?EncounterConversations:null;save.encounter_conversation_selected=ReferenceEquals(save.state,Simulation.State)?EncounterConversationSelected:null;save.dispatch_introduction_conversations=ReferenceEquals(save.state,Simulation.State)?DispatchIntroductionConversations:null;save.dispatch_introduction_conversation_selected=ReferenceEquals(save.state,Simulation.State)?DispatchIntroductionConversationSelected:null;Narratives.Sync();save.narratives=ReferenceEquals(save.state,Simulation.State)?Narratives.Rows.ToArray():null;DailyRecords.Sync();if(ReferenceEquals(save.state,Simulation.State)){save.daily_baselines=DailyRecords.Baselines.ToArray();save.daily_records=DailyRecords.Records.ToArray();}save.display_receipts=ReferenceEquals(save.state,Simulation.State) && displayEpoch==Simulation.State.instance_id?displayReceipts.ToArray():null;if(ReferenceEquals(save.state,newGameSaveState)){save.daily_baselines=save.state.cats.Select(c=>new CafeDailyBaseline{day=save.state.day,cat_id=c.id,fatigue=c.fatigue.ToString("R",System.Globalization.CultureInfo.InvariantCulture)}).ToArray();}return EncodeSave(save);}
        private void CaptureDisplay()
        {
            DailyRecords.Sync();var state=Simulation.State;if(displayEpoch!=state.instance_id){displayEpoch=state.instance_id;displayReceipts.Clear();}if(state.phase!="closed")return;
            foreach(var cat in state.cats){string value=Details.ClosedNetStamina(cat.id);if(value==null)continue;var row=displayReceipts.FirstOrDefault(r=>r.day==state.day && r.cat_id==cat.id);if(row==null)displayReceipts.Add(new CafeDisplayReceipt{day=state.day,cat_id=cat.id,net_stamina=value});else row.net_stamina=value;}
        }
        private void ValidateDisplay(CafeSaveEnvelope save)
        {
            var rows=save.display_receipts??new CafeDisplayReceipt[0];var keys=new System.Collections.Generic.HashSet<string>();if(rows.Length>100000)throw new ArgumentException("表示記録の件数が上限を超えています。");
            foreach(var row in rows){double value;if(row==null || row.day<1 || row.day>save.state.day || row.day==save.state.day && save.state.phase!="closed" || !save.state.cats.Any(c=>c.id==row.cat_id) || !keys.Add(row.day+":"+row.cat_id) || row.net_stamina==null || row.net_stamina.Length>32 || !double.TryParse(row.net_stamina,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out value) || double.IsNaN(value) || double.IsInfinity(value) || value<0 || value>Simulation.MaxStamina(save.state,row.cat_id))throw new ArgumentException("保存済みの表示記録が不正です。");}
        }
        private static string EncodeSave(CafeSaveEnvelope save) { save.double_bits = CafeDoublePrecision.Capture(save.state); return JsonUtility.ToJson(save, true); }
        private static CafeSaveEnvelope DecodeSave(string text) { var save = JsonUtility.FromJson<CafeSaveEnvelope>(text); if(save?.opening!=null && string.IsNullOrEmpty(save.opening.progress?.conversation_id))save.opening=null;if(save?.patron_ending!=null && string.IsNullOrEmpty(save.patron_ending.progress?.conversation_id))save.patron_ending=null;if(save?.bond_ending!=null && string.IsNullOrEmpty(save.bond_ending.progress?.conversation_id))save.bond_ending=null;if(save?.popularity_ending!=null && string.IsNullOrEmpty(save.popularity_ending.progress?.conversation_id))save.popularity_ending=null;if(save?.conversation!=null && string.IsNullOrEmpty(save.conversation.conversation_id))save.conversation=null;if(save?.regular_conversation!=null && string.IsNullOrEmpty(save.regular_conversation.conversation_id))save.regular_conversation=null;if(save?.dispatch_conversations!=null)foreach(var row in save.dispatch_conversations){if(row==null)continue;if(string.IsNullOrEmpty(row.departure?.conversation_id))row.departure=null;if(string.IsNullOrEmpty(row.travelling?.conversation_id))row.travelling=null;if(string.IsNullOrEmpty(row.returning?.conversation_id))row.returning=null;}save?.state?.NormalizeJsonStrings(); if (save?.state != null) CafeDoublePrecision.Restore(save.state, save.double_bits); return save; }
        public IEnumerator Request(string route, string commandJson, Action<CafeResponse> completed, int timeout = 3)
        {
            CafeResponse response;
            try
            {
                switch (route)
                {
                    case "/state": response = Success(JsonUtility.ToJson(View())); break;
                    case "/saves": response = Success(JsonUtility.ToJson(ListSaves())); break;
                    case "/commands": response = Command(commandJson); break;
                    default: if(route.StartsWith("/legacy-import",StringComparison.Ordinal)){response=Success(JsonUtility.ToJson(LegacyPreview(route)));break;}if(route.StartsWith("/business-log",StringComparison.Ordinal)){response=Success(JsonUtility.ToJson(BusinessLog(route)));break;}throw new NotSupportedException("単独版の第1段階では未対応です。");
                }
            }
            catch (Exception ex) { response = Failure(ex.Message); }
            completed(response); yield break;
        }
        private CafeResponse Command(string json)
        {
            var cmd = JsonUtility.FromJson<PythonShiftCommand>(json);
            if (cmd == null || string.IsNullOrEmpty(cmd.request_id)) throw new ArgumentException("操作IDがありません。");
            if (replies.TryGetValue(cmd.request_id, out var old)) return old.Key == json ? old.Value : Failure("同じ操作IDに異なる内容は指定できません。");
            CafeResponse response;
            try
            {
                var s = Simulation.State;
                if (cmd.instance_id != s.instance_id || cmd.expected_revision != s.revision) throw new InvalidOperationException("状態が更新されています。最新状態で再操作してください。");
                if(s.autoplay?.running==true && !s.autoplay.paused && !new[]{"step_autoplay","stop_autoplay","pause_autoplay","resume_autoplay","save_game","load_game"}.Contains(cmd.kind))throw new InvalidOperationException("おまかせを中止してから通常操作へ戻ってください。");
                if(s.autoplay?.running==true && s.autoplay.paused && new[]{"start_business","advance_business","next_day","day_off","rest_for_missing"}.Contains(cmd.kind))throw new InvalidOperationException("おまかせを再開するか中止してから営業を進めてください。");
                if(ConversationBlocksProgress && !(cmd.kind=="step_autoplay" && (PopularityEndingPending || BondEndingPending || PatronEndingPending)) && new[]{"start_business","advance_business","next_day","day_off","start_autoplay","resume_autoplay","step_autoplay"}.Contains(cmd.kind))throw new InvalidOperationException("会話を終えてから操作してください。結果会話はイベント一覧から再開できます。");
                if((PatronEndingPending && cmd.kind=="continue_patron") || (PopularityEndingPending && cmd.kind=="continue_goal") || (BondEndingPending && cmd.kind=="continue_bond_goal"))throw new InvalidOperationException("エンディングを最後まで読むか、結果画面から再開してください。");
                if(OpeningPending && !new[]{"save_game","load_game","new_game"}.Contains(cmd.kind))throw new InvalidOperationException("オープニングを最後まで読むか、スキップしてください。");
                var replayBefore=Journal.CaptureBefore(s);Simulation.ReplayActions.Clear();CafeAutoOperation replayAutomatic=null;
                switch (cmd.kind)
                {
                    case "import_legacy_save":ImportLegacy(cmd.save_id,cmd.save_slot);break;
                    case "start_autoplay": Simulation.StartAutoplay(cmd.autoplay_objective,cmd.autoplay_mode,cmd.autoplay_days);break;
                    case "step_autoplay": replayAutomatic=Simulation.StepAutoplay();break;
                    case "stop_autoplay": Simulation.CancelAutoplay();break;
                    case "pause_autoplay": Simulation.PauseAutoplay();break;
                    case "resume_autoplay": Simulation.ResumeAutoplay();break;
                    case "new_game":
                        if (cmd.choice != "free" && cmd.choice != "popularity" && cmd.choice != "patron" && cmd.choice != "bond") throw new NotSupportedException("単独版は自由営業・人気・有力者・好感度目標に対応しています。");
                        var initial = new CafeSimulation(Simulation.Rules, s.seed, cmd.choice).State;
                        newGameSaveState=initial;newGameOpening=CafeOpening.Start(initial,cmd.choice,id=>Simulation.Definition(id).name,playOpening);
                        var createdOpening=newGameOpening;try{saves.Save(1, initial);}finally{newGameSaveState=null;newGameOpening=null;} Simulation.Restore(initial);RestoreOpening(createdOpening);DailyRecords.Reset(true); break;
                    case "save_game":
                        bool oldPause=s.autoplay?.paused??false;int oldRevision=s.revision;int oldImportCount=s.import_history?.operations.Count??0;
                        try{Simulation.PauseAutoplay();saves.Save(cmd.save_slot == 0 ? 1 : cmd.save_slot, s);s.revision++;}
                        catch{if(s.autoplay!=null)s.autoplay.paused=oldPause;s.revision=oldRevision;if(s.import_history!=null)s.import_history.operations.RemoveRange(oldImportCount,s.import_history.operations.Count-oldImportCount);throw;}break;
                    case "load_game":
                        RememberViewedConversations();
                        if (!int.TryParse(cmd.save_id, out int slot)) throw new ArgumentException("保存スロットを選んでください。");
                        var loaded=saves.Load(slot);ValidateDisplay(loaded);CafeDailyRecords.Validate(loaded);CafeNarratives.Validate(loaded);CafeIntakeConversation.Validate(loaded.conversation,loaded.state.intake_request.status);CafeRegularConversation.Validate(loaded.regular_conversation,loaded.state.regular_introduction.status);CafeDispatchDialogue.Validate(loaded.dispatch_conversations,loaded.dispatch_conversation_selected,loaded.state);CafeEncounterDialogue.Validate(loaded.encounter_conversations,loaded.encounter_conversation_selected,loaded.state);CafeDispatchIntroductionDialogue.Validate(loaded.dispatch_introduction_conversations,loaded.dispatch_introduction_conversation_selected,loaded.state);CafePopularityEnding.Validate(loaded.popularity_ending,loaded.state);CafePatronEnding.Validate(loaded.patron_ending,loaded.state);CafeBondEnding.Validate(loaded.bond_ending,loaded.state);CafeOpening.Validate(loaded.opening,loaded.state);Simulation.Restore(loaded.state);RestoreOpening(loaded.opening);RestorePopularityEnding(loaded.popularity_ending);RestoreBondEnding(loaded.bond_ending);RestorePatronEnding(loaded.patron_ending);RestoreConversation(loaded.conversation,loaded.regular_conversation);RestoreDispatchConversations(loaded.dispatch_conversations,loaded.dispatch_conversation_selected);RestoreEncounterConversations(loaded.encounter_conversations,loaded.encounter_conversation_selected);RestoreDispatchIntroductionConversations(loaded.dispatch_introduction_conversations,loaded.dispatch_introduction_conversation_selected);displayReceipts=(loaded.display_receipts??new CafeDisplayReceipt[0]).ToList();displayEpoch=Simulation.State.instance_id;DailyRecords.Restore(loaded.daily_baselines,loaded.daily_records);Narratives.Restore(loaded.narratives);break;
                    case "player_step":Simulation.Execute(cmd.kind,choice:cmd.choice,source:string.IsNullOrEmpty(cmd.target_type)?null:cmd.target_type);break;
                    case "receive_patron":
                    case "receive_general":
                        var dispatch=s.dispatches.SingleOrDefault(e=>e.id==cmd.choice && e.cat_id==cmd.cat_id);
                        if(dispatch==null || dispatch.status!="waiting" || (cmd.kind=="receive_patron")!=Simulation.IsPatronDestination(dispatch.destination))throw new ArgumentException("未確認の帰還記録を選んでください。");
                        Simulation.Execute("resolve_activity",source:cmd.choice,choice:"receive");break;
                    case "resolve_dispatch_introduction":
                        if(!s.dispatches.Any(e=>e.id==cmd.event_id && e.introduction?.cat_id==cmd.cat_id && e.introduction.status=="waiting"))throw new ArgumentException("回答待ちの紹介猫を選んでください。");
                        Simulation.Execute(cmd.kind,source:cmd.event_id,choice:cmd.choice);break;
                    case "resolve_dispatch_trouble":
                        if(!s.dispatches.Any(e=>e.id==cmd.event_id && e.cat_id==cmd.cat_id && e.trouble?.status=="waiting"))throw new ArgumentException("回答待ちの派遣トラブルを選んでください。");
                        Simulation.Execute(cmd.kind,source:cmd.event_id,choice:cmd.choice);break;
                    case "resolve_dispatch_choice":
                        if(!s.dispatches.Any(e=>e.id==cmd.event_id && e.encounter?.status=="waiting"))throw new System.ArgumentException("回答待ちの派遣イベントを選んでください。");
                        Simulation.Execute(cmd.kind,source:cmd.event_id,choice:cmd.choice);break;
                    case "resolve_activity":Simulation.Execute(cmd.kind,source:cmd.event_id,choice:cmd.choice);break;
                    case "resolve_customer_trust": Simulation.Execute(cmd.kind, choice: cmd.choice, source: cmd.event_id); break;
                    case "set_auto_assignment":
                        if(cmd.working_cats!=null && cmd.working_cats.Length>0)throw new ArgumentException("自動割り当て設定には出勤猫を指定しないでください。");
                        Simulation.Execute(cmd.kind,choice:cmd.choice);break;
                    case "assign_service":
                        if(cmd.working_cats!=null && cmd.working_cats.Length>0 || string.IsNullOrEmpty(cmd.customer_id) || string.IsNullOrEmpty(cmd.cat_id) || string.IsNullOrEmpty(cmd.seat_id))throw new ArgumentException("待機中の客・猫・席を選んでください。");
                        Simulation.Execute(cmd.kind,catId:cmd.cat_id,seatId:cmd.seat_id,customerId:cmd.customer_id);break;
                    case "configure_adoption": case "resolve_adoption":
                        if(cmd.working_cats!=null && cmd.working_cats.Length>0)throw new ArgumentException("譲渡操作に出勤指定はできません。");
                        if(cmd.kind=="resolve_adoption" && (s.adoption==null || !s.adoption.events.Any(e=>e.id==cmd.event_id && e.cat_id==cmd.cat_id && e.status=="waiting")))throw new ArgumentException("回答待ちの譲渡申し出を選んでください。");
                        Simulation.Execute(cmd.kind,choice:cmd.choice,source:cmd.event_id);break;
                    case "open_recruitment": case "recruit_cat":
                        if(cmd.working_cats!=null && cmd.working_cats.Length>0)throw new ArgumentException("紹介操作に出勤指定はできません。");
                        Simulation.Execute(cmd.kind,catId:cmd.cat_id);break;
                    case "purchase_waiting_area": case "upgrade_waiting_area":
                        if(cmd.working_cats!=null && cmd.working_cats.Length>0 || !string.IsNullOrEmpty(cmd.cat_id) || !string.IsNullOrEmpty(cmd.choice) || !string.IsNullOrEmpty(cmd.event_id) || !string.IsNullOrEmpty(cmd.target_type))throw new ArgumentException("待合の購入・強化には猫や選択肢を指定しないでください。");
                        Simulation.Execute(cmd.kind);break;
                    case "purchase_rest_space": case "upgrade_rest_space": case "soundproof_rest_space":
                        if(cmd.working_cats!=null && cmd.working_cats.Length>0 || !string.IsNullOrEmpty(cmd.cat_id) || !string.IsNullOrEmpty(cmd.choice) || !string.IsNullOrEmpty(cmd.event_id) || !string.IsNullOrEmpty(cmd.target_type))throw new ArgumentException("休養設備には猫や選択肢を指定しないでください。");
                        Simulation.Execute(cmd.kind);break;
                    case "purchase_cat":
                        if(cmd.working_cats!=null && cmd.working_cats.Length>0)throw new ArgumentException("購入時に出勤指定はできません。");
                        Simulation.Execute(cmd.kind,catId:cmd.cat_id);break;
                    case "resolve_visiting_cat":
                        if(cmd.working_cats!=null && cmd.working_cats.Length>0 || cmd.cat_id!=Simulation.Rules.visiting_cat.cat.id || (s.visiting_cat.status!="visiting" && s.visiting_cat.status!="ready"))throw new ArgumentException("店先の猫を選んでください。");
                        Simulation.Execute(cmd.kind,choice:cmd.choice);break;
                    case "resolve_regular_introduction":
                        if (cmd.cat_id != Simulation.Rules.regular_introduction.cat.id) throw new ArgumentException("紹介された猫を選んでください。");
                        Simulation.Execute(cmd.kind, choice: cmd.choice); break;
                    default: Simulation.Execute(cmd.kind, cmd.working_cats, cmd.cat_id, cmd.choice, cmd.choice,cmd.seat_id); break;
                }
                CaptureDisplay();
                if(cmd.kind=="new_game" || cmd.kind=="load_game" || cmd.kind=="import_legacy_save")ResetJournal();else Journal.Append(replayBefore,Simulation,ReplayOperation(cmd),replayAutomatic);
                response = Success(JsonUtility.ToJson(new PythonCommandReply { state = View() }));
            }
            catch (Exception ex) { response = Failure(ex.Message); }
            // The cache is intentionally scoped to this runtime, not gameplay saves.
            replies.Add(cmd.request_id, new KeyValuePair<string, CafeResponse>(json, response));
            return response;
        }
        private static CafeResponse Success(string text) => new CafeResponse { result = UnityWebRequest.Result.Success, text = text };
        private CafeResponse Failure(string error) => new CafeResponse { result = UnityWebRequest.Result.ProtocolError, error = error, text = JsonUtility.ToJson(new PythonCommandReply { error = error, state = View() }) };
        public PythonSaveList ListSaves()
        {
            var list = new List<PythonSavedGame>();
            for (int slot = 1; slot <= 10; slot++)
            {
                try
                {
                    var save = saves.Load(slot); var s = save.state;
                    list.Add(new PythonSavedGame { slot = slot, save_id = slot.ToString(), label = slot + "：" + s.day + "日目 / " + s.phase, created_at = save.created_at, objective_label = s.objective=="bond"?"猫6匹との好感度":s.objective=="patron"?"有力者の満足度":s.objective=="popularity"?"人気目標":"自由営業", phase = s.phase });
                }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
                catch (Exception ex) { list.Add(new PythonSavedGame { slot = slot, save_id = slot.ToString(), label = slot + "：読み込み不可 / " + ex.Message, phase = "invalid" }); }
            }
            return new PythonSaveList { saves = list.ToArray() };
        }
        private string[] RestProposal()
        {
            var healthy=Simulation.State.cats.Where(c=>c.health=="healthy" && c.activity=="cafe").ToArray();
            var resting=new System.Collections.Generic.HashSet<string>(healthy.OrderByDescending(c=>c.fatigue).ThenBy(c=>c.id,StringComparer.Ordinal).Take(2).Select(c=>c.id));
            return healthy.Where(c=>!resting.Contains(c.id)).OrderBy(c=>c.id,StringComparer.Ordinal).Select(c=>c.id).ToArray();
        }
        public PythonCafeState View()
        {
            var s = Simulation.State; var rules = Simulation.Rules;
            return new PythonCafeState
            {
                day_off=DayOffView(), autoplay=AutoplayView(), version = 1, instance_id = s.instance_id, revision = s.revision, objective = s.objective, phase = s.phase, day = s.day, tick = s.tick,
                can_set_shifts = s.phase == "preparation", closed = s.phase == "closed", opening_ticks = rules.opening_ticks,
                funds = (float)s.funds, revenue = (float)s.revenue, waiting_count = s.queue.Count, completed_interactions = s.completed,
                popularity = (float)s.popularity, popularity_gain = (float)s.popularity_gain,
                customer_satisfaction = new PythonSatisfactionCounts { satisfied = s.visits.Count(v => v.satisfaction != null && v.satisfaction.level == "satisfied"), normal = s.visits.Count(v => v.satisfaction != null && v.satisfaction.level == "normal"), dissatisfied = s.visits.Count(v => v.satisfaction != null && v.satisfaction.level == "dissatisfied") },
                store_event_label = StoreEventLabel(), store_event = StoreEventView(), service_assignment=ServiceAssignmentView(), waiting_area=WaitingAreaView(), rest_space=RestSpaceView(), items = SupplyView(), seat_equipment=EquipmentView(),
                specialist_results = s.visits.Where(v=>v.specialist_result!=null).Select(v=>new PythonSpecialistResult {customer_id=v.id,cat_id=v.cat_id,result=v.specialist_result,connect_count=v.connect_count,open_up_count=v.open_up_count,simultaneous_count=v.simultaneous_count,bonus=(float)v.specialist_bonus,service_count=v.service_count,service_equipment=v.service_equipment}).ToArray(), specialist_description = SpecialistDescription(), reservation = ReservationView(), customer_trust = CustomerTrustView(), customer_progress = CustomerProgress(), customer_schedule = Simulation.CustomerSchedule(s.day),
                dispatch_troubles=DispatchTroublesView(), dispatch_introductions=DispatchIntroductionsView(), patron_dispatch=PatronDispatchView(), patron_goal=PatronGoalView(), bond_goal=BondGoalView(), player_interaction=PlayerView(), general_dispatch=GeneralDispatchView(), dispatch_choices=DispatchChoicesView(), objective_progress=ObjectiveProgressView(), sidebar_progress=SidebarProgressView(), objective_start=ObjectiveStartView(), clear_results = ClearResultsView(), ending = EndingView(), expansion = ExpansionView(), next_goal = NextGoalView(), goal_result = GoalResultView(), housing = HousingView(), regular_introduction = RegularIntroductionView(), intake_request = IntakeView(), visiting_cat = VisitingCatView(), pet_shop = PetShopView(), recruitment = RecruitmentView(), adoption = AdoptionView(), required_action = Simulation.AdoptionWaiting?"adoption":Simulation.PlayerActive ? "player" : Simulation.BondWaiting ? "bond_result" : Simulation.DispatchTroubleWaiting ? "dispatch_trouble" : Simulation.DispatchChoiceWaiting ? "dispatch_choice" : Simulation.DispatchWaiting ? "dispatch_return" : MissingWaiting ? "missing_return" : Simulation.ReservationWaiting ? "reservation" : Simulation.PatronWaiting ? "patron_result" : Simulation.GoalWaiting ? "goal_result" : Simulation.DispatchIntroductionWaiting ? "dispatch_introduction" : Simulation.IntakeWaiting ? "intake_request" : Simulation.RegularIntroductionWaiting ? "regular_introduction" : Simulation.CustomerTrustWaiting ? "customer_trust" : Simulation.StoreEventWaiting ? "store_event" : Simulation.GrowthWaiting ? "growth" : Simulation.AssignmentWaiting?"service_assignment":"", rest_proposal = RestProposal(), missing_cats = MissingView(), growth_choices = GrowthChoices(), growth_practice_choices = GrowthPracticeView(),
                new_game_options = new[] { new PythonNewGameOption {choice="bond",label="猫6匹との好感度",detail="好感度80以上の猫を6匹",conditions="1日3セット・1セット10手。4日目の保護猫を迎えて6匹にできます。"}, new PythonNewGameOption {choice="patron",label="有力者の満足度",detail="3人の満足度をすべて達成",conditions="派遣は2回の閉店で帰還。条件・加点は出発時に固定します。"}, new PythonNewGameOption {choice="popularity",label="人気目標",detail="段階目標・席数拡張・追加来客",conditions="初期猫5匹 / 2席 / 資金1000 / 人気100。3段階の人気目標に挑戦します。猫の休養と設備を整え、専門のお客や特別予約を迎えましょう。"}, new PythonNewGameOption { choice = "free", label = "自由営業", detail = "新規開始・出勤・自動接客・閉店・翌日・保存再開", conditions = "初期猫5匹 / 2席 / 資金1000。自由営業は3席まで。4席以降は段階人気目標の達成が必要。" } },
                cats = s.cats.Select(c => new PythonCatState { cat_id = c.id, content_cat_id = Details.ContentId(c.id), name = Simulation.Definition(c.id).name, affinity=(float)Simulation.PlayerCat(c.id).affinity,remaining_sets=Simulation.PlayerRemaining,can_player_start=Simulation.PlayerBeginReason(c.id)=="",player_reason=Simulation.PlayerBeginReason(c.id), stamina = (float)c.stamina, max_stamina = (float)Simulation.MaxStamina(c.id), fatigue = (float)c.fatigue, stress = (float)c.stress, working = c.working, growth_details = GrowthDetails(c.id), profile_details=Details.Profile(c.id),shift_forecast=Details.Forecast(c.id), health_status = c.health, health_label = c.health == "healthy" ? "健康" : "療養あと"+c.recovery_remaining+"日", activity = c.activity, activity_label = c.activity=="adopted"?"譲渡済み":c.activity == "cafe" ? "在店" : c.activity=="dispatched"?"派遣中":"家出中", records = new PythonCatRecords {events=Details.Events(c.id),relationships=Details.Relationships(c.id),history=Details.History(c.id)} }).ToArray(),
                seats = Enumerable.Range(0, s.seat_count).Select(i => { var e = s.exchanges.Find(x => x.seat == i); var row=Narratives.LatestSeat(e); return new PythonSeatState { latest_action=CafeNarratives.ActionLabel(row),cat_reaction=CafeNarratives.ReactionLabel(row), narrative=Narratives.Seat(e),narrative_image=row?.image_resource,seat_id = "seat-" + (i + 1), customer_id = e?.customer_id, customer_name = Details.CustomerName(e?.customer_id), cat_id = e?.cat_id, equipment_name=Simulation.Installed(i)?.name,equipment_id=s.equipment_placements.Find(p=>p.seat==i)?.item,equipment_description=Simulation.Installed(i)?.name??"設備なし", disabled = Simulation.DisabledSeat(i) }; }).ToArray(),
                customers = s.visits.Where(v => v.departure == null).Select(v => new PythonCustomerState { customer_id = v.id, name = Details.CustomerName(v.id), seat_id=s.exchanges.Any(e=>e.customer_id==v.id)?"seat-"+(s.exchanges.Single(e=>e.customer_id==v.id).seat+1):"", status = s.queue.Contains(v.id) ? "waiting" : "seated" }).ToArray(),
                finance = new PythonFinanceState {dispatch_trouble_expenses=(float)s.dispatch_trouble_expenses,dispatch_income=(float)s.dispatch_income, waiting_area_expenses=(float)s.waiting_area_expenses, facility_cost=(float)s.waiting_area_cost, equipment_expenses=(float)s.rest_space_expenses, seat_equipment_expenses=(float)s.equipment_expenses, opening_funds = (float)s.opening_funds, total_income = (float)(s.revenue+s.item_sales_income+s.dispatch_income), total_expenses = (float)s.expenses, item_expenses = (float)s.item_expenses, item_sales_income = (float)s.item_sales_income, housing_expenses = (float)s.housing_expenses, housing_cost = (float)s.housing_cost, pet_shop_expenses = (float)s.pet_shop_expenses, recruitment_expenses = (float)s.recruitment_expenses, net_cash_flow = (float)(s.revenue+s.item_sales_income+s.dispatch_income - s.expenses), closing_funds = (float)s.funds },
                game_over = s.game_over == null ? null : new PythonGameOver { reason = s.game_over, day = s.day, popularity = (float)s.popularity }
            };
        }
    }
}
