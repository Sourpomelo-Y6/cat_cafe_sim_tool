using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace CatCafePreview
{
    [Serializable] public class PythonCatState
    {
        public string cat_id, content_cat_id, name, health_status, activity, health_label, activity_label, growth_details;
        public float stamina, max_stamina, fatigue, stress, affinity;
        public int remaining_sets;
        public bool can_player_start;
        public string player_reason;
        public bool working;
    }
    [Serializable] public class PythonCafeState
    {
        public int version, day, tick;
        public string instance_id;
        public int revision;
        public bool can_set_shifts;
        public bool closed;
        public string phase, required_action, objective;
        public PythonNewGameOption[] new_game_options;
        public int opening_ticks, waiting_count, completed_interactions;
        public float revenue;
        public PythonSeatState[] seats;
        public PythonCustomerState[] customers;
        public PythonFinanceState finance;
        public PythonIntakeState intake_request;
        public PythonVisitingCat visiting_cat;
        public PythonRegularIntroduction regular_introduction;
        public PythonReservation reservation;
        public PythonCustomerTrust customer_trust;
        public PythonHousingState housing;
        public PythonGoalResult goal_result;
        public PythonNextGoal next_goal;
        public PythonPlayerInteraction player_interaction;
        public PythonPatronDispatch patron_dispatch, general_dispatch;
        public PythonDispatchEncounter[] dispatch_choices;
        public PythonDispatchTrouble[] dispatch_troubles;
        public PythonDispatchIntroduction[] dispatch_introductions;
        public PythonItems items;
        public PythonDayOff day_off;
        public PythonExpansion expansion;
        public PythonSeatEquipment seat_equipment;
        public PythonRestSpace rest_space;
        public PythonWaitingArea waiting_area;
        public PythonCustomerDirectory customer_directory;
        public PythonMissingCat[] missing_cats;
        public PythonMissingRest missing_rest;
        public PythonAdoption adoption;
        public PythonGameOver game_over;
        public PythonObjectiveProgress objective_progress;
        public PythonStoreEvent store_event;
        public PythonGrowthChoice[] growth_choices;
        public float funds;
        public PythonCatState[] cats;
    }
    [Serializable] public class PythonNewGameOption { public string choice, label, detail, conditions; }
    [Serializable] public class PythonGrowthChoice
    { public string cat_id, name, kind, title, learned, group, effect; public float experience, threshold, service, rest, dispatch, play, contact, quiet; public PythonEventChoice[] choices; }
    [Serializable] public class PythonStoreEvent
    { public string kind, title; public int day; public float funds; public PythonEventChoice[] choices; }
    [Serializable] public class PythonEventChoice
    { public string choice, label, detail, reason; public bool can_select; }
    [Serializable] public class PythonObjectiveProgress { public string mode, label, status, summary, details; }
    [Serializable] public class PythonPlayerInteraction
    {
        public string session_id, cat_id, content_cat_id, scene_key, image_key, event_image_key, name, mode, history, reason;
        public bool active, can_finish;
        public int remaining_ticks, remaining_sets;
        public float affinity_before, affinity_after, affinity_delta, stamina_before, stamina, engagement, tension;
        public PythonEventChoice[] actions, targets;
    }
    [Serializable] public class PythonNextGoal
    { public int stage, started_day, deadline, days; public float target; public bool can_advance; public string reason; }
    [Serializable] public class PythonGoalResult
    {
        public string status, reason, kind, title, details;
        public int stage, started_day, deadline, resolved_day;
        public float target, popularity, funds;
        public bool can_continue;
    }
    [Serializable] public class PythonHousingState
    {
        public bool configured, can_expand;
        public int count, capacity, free, stage, next_capacity;
        public string kind, reason;
        public float cost, funds_after, daily_cost, next_daily_cost, operating_cost, next_operating_cost;
    }
    [Serializable] public class PythonSeatState { public string seat_id, customer_id, customer_name, cat_id, equipment_id, equipment_description, equipment_name; }
    [Serializable] public class PythonIntakeState
    {
        public string cat_id, name, status, personality, features, trait, housing, response_reason, accept_reason;
        public int day;
        public float cost;
        public bool can_accept, can_decline;
    }
    [Serializable] public class PythonCustomerState { public string customer_id, name, status, seat_id; }
    [Serializable] public class PythonFinanceState
    {
        public float opening_funds, total_income, total_expenses, net_cash_flow, closing_funds;
    }
    [Serializable] public class PythonShiftCommand
    {
        public string request_id, instance_id, kind = "set_shifts";
        public int expected_revision;
        public string[] working_cats;
        public string save_id = "";
        public string choice = "";
        public string cat_id = "";
        public string target_type = "";
        public string event_id = "";
    }
    [Serializable] public class PythonCommandReply
    {
        public string error;
        public PythonCafeState state;
    }
    [Serializable] public class PythonSavedGame { public string save_id, label, created_at, objective_label, phase;
        public string Details()
        {
            DateTimeOffset date;
            string timestamp=DateTimeOffset.TryParse(created_at,out date) ? date.ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss") : "日時不明";
            string stage=phase=="closed" ? "閉店後" : phase=="preparation" ? "開店準備" : phase=="open" ? "営業中" : "記録なし";
            return "保存日時："+timestamp+"\n経営目標："+(string.IsNullOrEmpty(objective_label) ? "記録なし（以前の保存データ）" : objective_label)+"　／　状態："+stage+"\n"+label;
        } }
    [Serializable] public class PythonSaveList { public PythonSavedGame[] saves; }
    [RequireComponent(typeof(ContentPreview))]
    public class PythonStateClient : MonoBehaviour
    {
        [SerializeField] private int port = 8190;
        private PythonCafeState current;
        private PythonShiftCommand pending;
        private bool online;
        private ContentPreview preview;
        private bool automatic, fast, pendingAutomatic, sendingCommand;
        private float nextAutomaticTick;
        public bool Automatic { get { return automatic; } }
        public bool Fast { get { return fast; } }
        public bool CanStartAutomatic { get { return CanManageSaves && IsOpen(current); } }
        private static bool IsOpen(PythonCafeState state)
        { return state != null && state.game_over == null && !state.closed && !state.can_set_shifts && string.IsNullOrEmpty(state.required_action); }
        public void ToggleAutomatic()
        {
            if (automatic) StopAutomatic();
            else if (CanStartAutomatic) { automatic=true; ResetAutomaticTimer(); }
            preview.RefreshPythonControls();
        }
        public void ToggleSpeed() { fast=!fast; ResetAutomaticTimer(); preview.RefreshPythonControls(); }
        private void ResetAutomaticTimer() { nextAutomaticTick=Time.realtimeSinceStartup+(fast ? .25f : 1f); }
        private void StopAutomatic()
        {
            automatic=false;
            if(pendingAutomatic && !sendingCommand) { pending=null; pendingAutomatic=false; }
        }
        public PythonCafeState CurrentState { get { return current; } }
        public bool CanLeaveResult { get { return pending == null; } }
        public bool CanManageSaves { get { return online && pending == null && current != null; } }
        public void NewGame(string objective)
        {
            if(!CanManageSaves || current.new_game_options == null || !Array.Exists(current.new_game_options,row=>row.choice==objective)) return;
            StopAutomatic();
            pending=new PythonShiftCommand {request_id=Guid.NewGuid().ToString("N"),instance_id=current.instance_id,
                expected_revision=current.revision,working_cats=new string[0],kind="new_game",choice=objective};
            preview.RefreshPythonControls();
        }
        public void SaveGame() { QueueSave("save_game", ""); }
        public void LoadSaved(string id) { QueueSave("load_game", id); }
        public bool CanContinueGoal { get { return CanManageSaves && current.goal_result != null && current.goal_result.can_continue; } }
        public bool CanBeginPlayer(string catId)
        { return CanManageSaves && Array.Exists(current.cats,row=>row.cat_id==catId && row.can_player_start); }
        public void BeginPlayer(string catId)
        { if(CanBeginPlayer(catId))QueuePlayer("player_begin",catId,"",""); }
        public bool CanPlayerAction(string choice)
        {
            var row=current==null ? null : current.player_interaction;
            return CanManageSaves && row!=null && row.active && row.actions!=null && Array.Exists(row.actions,a=>a.choice==choice && a.can_select);
        }
        public void PlayerAction(string choice,string target)
        {
            if(!CanPlayerAction(choice))return;
            var row=current.player_interaction;
            if(choice=="switch" && (row.targets==null || !Array.Exists(row.targets,t=>t.choice==target)))return;
            QueuePlayer("player_step",row.cat_id,choice,choice=="switch" ? target : "");
        }
        public bool CanFinishPlayer { get { return CanManageSaves && current.player_interaction!=null && current.player_interaction.active && current.player_interaction.can_finish; } }
        public void FinishPlayer() { if(CanFinishPlayer)QueuePlayer("player_finish",current.player_interaction.cat_id,"",""); }
        private void QueuePlayer(string kind,string catId,string choice,string target)
        {
            StopAutomatic();
            pending=new PythonShiftCommand{request_id=Guid.NewGuid().ToString("N"),instance_id=current.instance_id,
                expected_revision=current.revision,working_cats=new string[0],kind=kind,cat_id=catId,choice=choice,target_type=target};
            preview.RefreshPythonControls();
        }
        public void ToggleAdoption()
        {
            if(CanManageSaves && current.adoption!=null && current.adoption.can_configure)QueueAdoption("configure_adoption","",current.adoption.enabled ? "off" : "on","");
        }
        public void ResolveAdoption(string eventId,string choice)
        {
            if(!CanManageSaves || current.adoption==null || current.adoption.events==null)return;
            var row=Array.Find(current.adoption.events,r=>r.event_id==eventId);
            if(row!=null && (choice=="accept" ? row.can_accept : choice=="decline" && row.can_decline))QueueAdoption("resolve_adoption",row.cat_id,choice,eventId);
        }
        private void QueueAdoption(string kind,string catId,string choice,string eventId)
        {
            StopAutomatic();pending=new PythonShiftCommand{request_id=Guid.NewGuid().ToString("N"),instance_id=current.instance_id,
                expected_revision=current.revision,working_cats=new string[0],kind=kind,cat_id=catId,choice=choice,event_id=eventId};preview.RefreshPythonControls();
        }
        public void RestForMissing()
        { if(CanManageSaves && current.missing_rest!=null && current.missing_rest.can_rest)QueuePlayer("rest_for_missing","","",""); }
        public void ResolveMissing(string eventId)
        {
            if(!CanManageSaves || current.missing_cats==null)return;
            var row=Array.Find(current.missing_cats,r=>r.choice==eventId && r.can_resolve);
            if(row!=null)QueuePlayer("resolve_missing",row.cat_id,row.choice,"");
        }
        public void DispatchPatron(string catId,string destination)
        {
            if(!CanManageSaves || current.patron_dispatch==null || !Array.Exists(current.patron_dispatch.options,r=>r.cat_id==catId && r.choice==destination && r.can_select))return;
            QueuePlayer("dispatch_patron",catId,destination,"");
        }
        public void ReceivePatron(string eventId)
        {
            if(!CanManageSaves || current.patron_dispatch==null)return;
            var row=Array.Find(current.patron_dispatch.events,r=>r.choice==eventId && r.can_select);
            if(row!=null)QueuePlayer("receive_patron",row.cat_id,row.choice,"");
        }
        public bool CanItemAction(string kind,string choice,string catId)
        {
            if(!CanManageSaves || current.items==null)return false;
            if(kind=="purchase_item")return Array.Exists(current.items.shop ?? new PythonItemShop[0],r=>r.choice==choice && r.can_select);
            var row=Array.Find(current.items.owned ?? new PythonOwnedItem[0],r=>r.choice==choice);if(row==null)return false;
            return kind=="sell_item" ? row.can_sell : kind=="use_item" && Array.Exists(row.targets ?? new PythonItemTarget[0],r=>r.cat_id==catId && r.can_select);
        }
        public void ItemAction(string kind,string choice,string catId)
        {if(CanItemAction(kind,choice,catId))QueuePlayer(kind,catId,choice,"");}
        public bool CanRespondDispatchIntroduction(string eventId,string choice)
        {
            var row=current?.dispatch_introductions==null ? null : Array.Find(current.dispatch_introductions,r=>r.event_id==eventId);
            return CanManageSaves && row!=null && (choice=="accept" ? row.can_accept : choice=="decline" && row.can_decline);
        }
        public void RespondDispatchIntroduction(string eventId,string choice)
        {
            if(!CanRespondDispatchIntroduction(eventId,choice))return;var row=Array.Find(current.dispatch_introductions,r=>r.event_id==eventId);
            QueueAdoption("resolve_dispatch_introduction",row.cat_id,choice,eventId);
        }
        public bool CanRespondDispatchTrouble(string eventId,string choice)
        {
            var row=current?.dispatch_troubles==null ? null : Array.Find(current.dispatch_troubles,r=>r.event_id==eventId);
            return CanManageSaves && row?.choices!=null && Array.Exists(row.choices,r=>r.choice==choice && r.can_select);
        }
        public void RespondDispatchTrouble(string eventId,string choice)
        {if(CanRespondDispatchTrouble(eventId,choice))QueueAdoption("resolve_dispatch_trouble","",choice,eventId);}
        public bool CanRespondDispatchChoice(string eventId,string choice)
        {
            var row=current?.dispatch_choices==null ? null : Array.Find(current.dispatch_choices,r=>r.event_id==eventId);
            return CanManageSaves && row?.choices!=null && Array.Exists(row.choices,r=>r.choice==choice && r.can_select);
        }
        public void RespondDispatchChoice(string eventId,string choice)
        {
            if(CanRespondDispatchChoice(eventId,choice))QueueAdoption("resolve_dispatch_choice","",choice,eventId);
        }
        public void DispatchGeneral(string catId,string destination)
        {
            if(!CanManageSaves || current.general_dispatch==null || !Array.Exists(current.general_dispatch.options,r=>r.cat_id==catId && r.choice==destination && r.can_select))return;
            QueuePlayer("dispatch_general",catId,destination,"");
        }
        public void ReceiveGeneral(string eventId)
        {
            if(!CanManageSaves || current.general_dispatch==null)return;
            var row=Array.Find(current.general_dispatch.events,r=>r.choice==eventId && r.can_select);
            if(row!=null)QueuePlayer("receive_general",row.cat_id,row.choice,"");
        }
        public bool CanAdvanceGoal { get { return CanManageSaves && current.next_goal != null && current.next_goal.can_advance; } }
        public void AdvanceGoal()
        {
            if(!CanAdvanceGoal)return;
            StopAutomatic();
            pending=new PythonShiftCommand {request_id=Guid.NewGuid().ToString("N"),instance_id=current.instance_id,
                expected_revision=current.revision,working_cats=new string[0],kind="advance_goal"};
            preview.RefreshPythonControls();
        }
        public void ContinueGoal()
        {
            if (!CanContinueGoal) return;
            pending = new PythonShiftCommand { request_id=Guid.NewGuid().ToString("N"),instance_id=current.instance_id,
                expected_revision=current.revision,working_cats=new string[0],kind=string.IsNullOrEmpty(current.goal_result.kind) ? "continue_goal" : current.goal_result.kind };
            preview.RefreshPythonControls();
        }
        public bool CanRespondGrowth(int index)
        {
            var rows=current == null ? null : current.growth_choices;
            return CanManageSaves && rows != null && rows.Length>0 && rows[0].choices != null
                && index>=0 && index<rows[0].choices.Length && rows[0].choices[index].can_select;
        }
        public void RespondGrowth(int index)
        {
            if(!CanRespondGrowth(index))return;
            StopAutomatic(); var row=current.growth_choices[0];
            pending=new PythonShiftCommand { request_id=Guid.NewGuid().ToString("N"),instance_id=current.instance_id,
                expected_revision=current.revision,working_cats=new string[0],kind=string.IsNullOrEmpty(row.kind) ? "resolve_growth" : row.kind,cat_id=row.cat_id,choice=row.choices[index].choice };
            preview.RefreshPythonControls();
        }
        public bool CanRespondStore(int index)
        {
            var ev=current == null ? null : current.store_event;
            return CanManageSaves && ev != null && ev.choices != null && index >= 0 && index < ev.choices.Length && ev.choices[index].can_select;
        }
        public void RespondStore(int index)
        {
            if(!CanRespondStore(index)) return;
            StopAutomatic();
            pending=new PythonShiftCommand { request_id=Guid.NewGuid().ToString("N"),instance_id=current.instance_id,
                expected_revision=current.revision,working_cats=new string[0],kind="resolve_store_event",choice=current.store_event.choices[index].choice };
            preview.RefreshPythonControls();
        }
        public bool CanExpandHousing { get { return CanManageSaves && current.housing != null && current.housing.can_expand; } }
        public void ExpandHousing()
        {
            if (!CanExpandHousing) return;
            pending = new PythonShiftCommand { request_id=Guid.NewGuid().ToString("N"),instance_id=current.instance_id,
                expected_revision=current.revision,working_cats=new string[0],kind=current.housing.kind };
            preview.RefreshPythonControls();
        }
        public bool CanRespondIntake(bool accept)
        {
            var intake = current == null ? null : current.intake_request;
            return CanManageSaves && intake != null && intake.status == "waiting"
                && (accept ? intake.can_accept : intake.can_decline);
        }
        public void RespondIntake(bool accept)
        {
            if (!CanRespondIntake(accept)) return;
            pending = new PythonShiftCommand { request_id=Guid.NewGuid().ToString("N"),instance_id=current.instance_id,
                expected_revision=current.revision,working_cats=new string[0],kind="resolve_intake",choice=accept ? "accept" : "decline" };
            preview.RefreshPythonControls();
        }
        public bool CanRespondVisiting(string choice)
        {
            var cat=current?.visiting_cat;
            return CanManageSaves && cat!=null && (choice=="interact" ? cat.can_interact : choice=="skip" ? cat.can_skip : choice=="accept" && cat.can_accept);
        }
        public bool CanRespondRegular(string choice)
        {
            var row=current?.regular_introduction;
            return CanManageSaves && row!=null && row.status=="waiting" && (choice=="accept" ? row.can_accept : choice=="decline" && row.can_decline);
        }
        public bool CanRespondReservation(string choice)
        {
            var row=current?.reservation;
            return CanManageSaves && row!=null && row.status=="waiting" && (choice=="accept" ? row.can_accept : choice=="decline" && row.can_decline);
        }
        public void RespondReservation(string choice){if(CanRespondReservation(choice))QueueAdoption("resolve_reservation","",choice,current.reservation.event_id);}
        public bool CanRespondTrust(string id){return CanManageSaves && current.customer_trust?.events!=null && Array.Exists(current.customer_trust.events,e=>e.event_id==id && e.can_respond);}
        public void RespondTrust(string id,string choice){if(CanRespondTrust(id) && (choice=="recover" || choice=="ignore"))QueueAdoption("resolve_customer_trust","",choice,id);}
        public void RespondRegular(string choice)
        { if(CanRespondRegular(choice))QueuePlayer("resolve_regular_introduction",current.regular_introduction.cat_id,choice,""); }
        public void RespondVisiting(string choice)
        {
            if(!CanRespondVisiting(choice))return;
            QueuePlayer("resolve_visiting_cat",current.visiting_cat.cat_id,choice,"");
        }
        private void QueueSave(string kind, string id)
        {
            if (!CanManageSaves) return;
            if (kind == "load_game") StopAutomatic();
            pending = new PythonShiftCommand { request_id=Guid.NewGuid().ToString("N"),instance_id=current.instance_id,
                expected_revision=current.revision,working_cats=new string[0],kind=kind,save_id=id };
            GetComponent<PythonSavePanel>()?.Notify(kind=="save_game" ? "保存しています。完了までお待ちください。" : "保存したゲームを読み込んでいます。");
            preview.RefreshPythonControls();
        }
        public bool CanChangeShifts { get { return online && pending == null && current != null && current.game_over == null && current.can_set_shifts && (current.player_interaction==null || !current.player_interaction.active); } }
        public bool CanSeatEquipment(string kind,string seat,string choice)
        {
            if(!CanManageSaves || current.game_over!=null || current.seat_equipment?.can_manage!=true || Array.Find(current.seat_equipment.seats,r=>r.seat_id==seat)==null)return false;
            if(kind=="purchase_seat_equipment")return Array.Find(current.seat_equipment.catalog,r=>r.kind_id==choice)?.can_purchase==true;
            return kind=="equip_seat" && (choice=="" || Array.Find(current.seat_equipment.owned,r=>r.item_id==choice)!=null);
        }
        public void SeatEquipment(string kind,string seat,string choice){if(CanSeatEquipment(kind,seat,choice))QueuePlayer(kind,"",choice,seat);}
        public bool CanRestSpace(string kind){return CanManageSaves && current.game_over==null && Array.Find(current.rest_space?.actions ?? new PythonRestSpaceAction[0],r=>r.kind==kind)?.can_execute==true;}
        public void RestSpace(string kind){if(CanRestSpace(kind))QueuePlayer(kind,"","","");}
        public bool CanWaitingArea(string kind){return CanManageSaves && current.game_over==null && Array.Find(current.waiting_area?.actions ?? new PythonWaitingAreaAction[0],r=>r.kind==kind)?.can_execute==true;}
        public void WaitingArea(string kind){if(CanWaitingArea(kind))QueuePlayer(kind,"","","");}
        public bool CanExpandSeats { get { return CanManageSaves && current.game_over==null && current.expansion!=null && current.expansion.can_expand; } }
        public void ExpandSeats() { if(CanExpandSeats)QueuePlayer("expand_seats","","",""); }
        public bool CanDayOff { get { return CanManageSaves && current.game_over==null && current.day_off!=null && current.day_off.can_select; } }
        public void DayOff() { if(CanDayOff){StopAutomatic();QueuePlayer("day_off","","","");} }
        public bool CanProgressBusiness { get { return online && pending == null && current != null && current.game_over == null && !automatic
            && string.IsNullOrEmpty(current.required_action)
            && (current.closed || !current.can_set_shifts || Array.Exists(current.cats, cat => cat.working)); } }
        public void ProgressBusiness()
        {
            if (!CanProgressBusiness) return;
            QueueBusiness(current.closed ? "next_day" : current.can_set_shifts ? "start_business" : "advance_business");
        }
        private void QueueBusiness(string kind)
        {
            pending = new PythonShiftCommand { request_id = Guid.NewGuid().ToString("N"), instance_id = current.instance_id,
                expected_revision = current.revision, working_cats = new string[0],
                kind = kind };
            preview.RefreshPythonControls();
        }
        public void WorkSelected() { QueueShift(true); }
        public void RestSelected() { QueueShift(false); }
        private void QueueShift(bool working)
        {
            SetCatShift(preview.SelectedPythonCatId, working);
        }
        public void SetCatShift(string id, bool working)
        {
            if (!CanChangeShifts) return;
            var selected = Array.Find(current.cats, cat => cat.cat_id == id);
            if (selected == null || selected.working == working) return;
            var ids = new System.Collections.Generic.List<string>();
            foreach (var cat in current.cats)
                if (cat.cat_id == id ? working : cat.working) ids.Add(cat.cat_id);
            pending = new PythonShiftCommand { request_id = Guid.NewGuid().ToString("N"), instance_id = current.instance_id,
                expected_revision = current.revision, working_cats = ids.ToArray() };
            preview.RefreshPythonControls();
        }

        private void Accept(PythonCafeState state)
        {
            Validate(state);
            // JsonUtility can materialize a JSON null nested object with empty fields.
            if (state.goal_result != null && string.IsNullOrEmpty(state.goal_result.status)) state.goal_result = null;
            if(state.next_goal != null && state.next_goal.stage < 2)state.next_goal=null;
            if(state.player_interaction!=null && string.IsNullOrEmpty(state.player_interaction.session_id))state.player_interaction=null;
            if(state.store_event != null && string.IsNullOrEmpty(state.store_event.kind)) state.store_event=null;
            if(state.game_over!=null && string.IsNullOrEmpty(state.game_over.reason))state.game_over=null;
            if (!IsOpen(state) || (current != null && (state.instance_id != current.instance_id || state.day != current.day))) StopAutomatic();
            current = state; online = true; preview.ApplyPythonState(state);
        }
        private IEnumerator Start()
        {
            // ContentPreview.Start binds the saved UI before the first response.
            yield return null;
            preview = GetComponent<ContentPreview>();
            preview.PythonConnectionError("接続しています。");
            while (true)
            {
                string notice = "";
                if (pending != null)
                {
                    var command = pending;
                    sendingCommand=true;
                    // Retry once with the SAME request ID if the reply was lost.
                    for (int attempt = 0; attempt < 2; attempt++)
                    {
                        using (var request = new UnityWebRequest("http://127.0.0.1:" + port + "/commands", "POST"))
                        {
                            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(command)));
                            request.downloadHandler = new DownloadHandlerBuffer();
                            request.SetRequestHeader("Content-Type", "application/json"); request.timeout = 3;
                            yield return request.SendWebRequest();
                            if (request.result != UnityWebRequest.Result.Success) StopAutomatic();
                            if (request.result == UnityWebRequest.Result.ConnectionError && attempt == 0) continue;
                            try
                            {
                                if (request.result == UnityWebRequest.Result.Success)
                                {
                                    var reply = JsonUtility.FromJson<PythonCommandReply>(request.downloadHandler.text);
                                    if(command.kind=="load_game" || command.kind=="new_game")GetComponent<PythonEventPresentation>()?.Clear();
                                    Accept(reply.state);
                                    if(command.kind=="load_game")
                                    { var player=GetComponent<PythonPlayerPanel>();if(player!=null)player.Restore(); }
                                    if(command.kind=="new_game" || command.kind=="load_game") { var adoption=GetComponent<PythonAdoptionPanel>();if(adoption!=null)adoption.ResetConfirmation();var menu=GetComponent<PythonNewGamePanel>(); if(menu!=null) menu.Close(); }
                                    notice = command.kind == "set_shifts" ? "出勤・休養を変更しました。"
                                        : command.kind == "save_game" ? "保存しました。" : command.kind == "load_game" ? "保存したゲームを再開しました。"
                                        : command.kind == "new_game" ? "新規ゲームを作成し、初期状態を保存しました。出勤・休養を設定して営業を始めてください。"
                                        : command.kind == "next_day" ? "翌日の営業準備へ進みました。出勤・休養を設定してください。"
                                        : command.kind == "resolve_intake" ? (command.choice == "accept" ? "保護猫を迎えました。休養予定で猫一覧に加わりました。" : "受け入れ依頼を見送りました。")
                                        : command.kind == "resolve_regular_introduction" ? (command.choice == "accept" ? "常連から紹介された猫を迎えました。休養予定で猫一覧に加わりました。" : "常連からの猫紹介を見送りました。")
                                        : command.kind == "resolve_reservation" ? (command.choice == "accept" ? "予約を引き受けました。来店日と担当猫の条件を確認してください。" : "予約を辞退しました。")
                                        : command.kind == "resolve_customer_trust" ? (command.choice == "recover" ? "信頼回復に取り組みます。今後の接客結果を確認してください。" : "対応せず、お客さんの永久離脱を確定しました。")
                                        : command.kind == "purchase_housing" || command.kind == "upgrade_housing" ? "飼育スペースを拡張しました。"
                                        : command.kind == "resolve_store_event" ? (command.choice == "close" ? "休業し、翌日の営業準備へ進みました。" : "店舗イベントに回答しました。")
                                        : command.kind == "resolve_growth" ? "得意分野を選びました。"
                                        : command.kind == "configure_adoption" ? "譲渡申し出の発生設定を変更しました。"
                                        : command.kind == "resolve_adoption" ? (command.choice=="accept" ? "譲渡が成立しました。" : "譲渡申し出を見送りました。")
                                        : command.kind == "rest_for_missing" ? "休業し、帰還までの日数を進めました。結果を確認してください。"
                                        : command.kind == "resolve_missing" ? "猫の帰還と費用を確定しました。出勤設定を確認してください。"
                                        : command.kind == "dispatch_patron" ? "有力者への派遣を開始しました。"
                                        : command.kind == "receive_patron" ? "帰還を確認し、報酬と満足度を受け取りました。出勤設定を確認してください。"
                                        : command.kind == "player_begin" ? "猫との交流を開始しました。"
                                        : command.kind == "player_step" || command.kind == "player_finish" ? "猫との交流を更新しました。"
                                        : command.kind == "advance_goal" ? "次の人気目標への挑戦を開始しました。"
                                        : (command.kind == "continue_goal" || command.kind == "continue_patron" || command.kind == "continue_bond_goal") ? "目標結果を確認しました。営業を続けられます。"
                                        : reply.state.closed ? "閉店しました。結果を確認してください。" : "営業を進めました。";
                                }
                                else
                                {
                                    var reply = JsonUtility.FromJson<PythonCommandReply>(request.downloadHandler.text);
                                    notice = reply != null && !string.IsNullOrEmpty(reply.error) ? reply.error : "操作結果を確認できません。最新状態を取得します。";
                                }
                            }
                            catch (Exception) { StopAutomatic(); notice = "操作結果を確認できません。最新状態を取得します。"; }
                            break;
                        }
                    }
                    pending = null;
                    pendingAutomatic=false; sendingCommand=false;
                }
                using (var request = UnityWebRequest.Get("http://127.0.0.1:" + port + "/state"))
                {
                    request.timeout = 3;
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success) { StopAutomatic(); online = false; preview.PythonConnectionError(request.error); }
                    else
                    {
                        try
                        {
                            var state = JsonUtility.FromJson<PythonCafeState>(request.downloadHandler.text);
                            Accept(state);
                            if (notice != "") preview.PythonNotice(notice);
                        }
                        catch (Exception ex) { StopAutomatic(); online = false; preview.PythonConnectionError(ex.Message); }
                    }
                }
                var panel = GetComponent<PythonSavePanel>();
                if (panel != null)
                {
                    using (var listRequest = UnityWebRequest.Get("http://127.0.0.1:" + port + "/saves"))
                    {
                        listRequest.timeout = 3;
                        yield return listRequest.SendWebRequest();
                        try
                        {
                            if(listRequest.result == UnityWebRequest.Result.Success)
                                panel.Apply(JsonUtility.FromJson<PythonSaveList>(listRequest.downloadHandler.text));
                            else { StopAutomatic(); panel.ListUnavailable(); }
                        }
                        catch(Exception) { StopAutomatic(); panel.ListUnavailable(); }
                    }
                }
                float until = Time.realtimeSinceStartup + 2;
                if(automatic) ResetAutomaticTimer();
                preview.RefreshPythonControls();
                while (pending == null && Time.realtimeSinceStartup < until)
                {
                    if(automatic && CanStartAutomatic && Time.realtimeSinceStartup >= nextAutomaticTick)
                    {
                        pendingAutomatic=true; QueueBusiness("advance_business");
                        break;
                    }
                    yield return null;
                }
            }
        }
        public static void Validate(PythonCafeState state)
        {
            if (state == null || state.version != 1 || state.cats == null || state.cats.Length == 0)
                throw new ArgumentException("ゲーム状態の形式が不正です。");
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var cat in state.cats)
                if (cat == null || string.IsNullOrEmpty(cat.cat_id) || !ids.Add(cat.cat_id)
                    || ! (cat.max_stamina > 0) || float.IsInfinity(cat.max_stamina)
                    || !(cat.stamina >= 0 && cat.stamina <= cat.max_stamina))
                    throw new ArgumentException("猫の状態が不正です。");
        }
    }
}
