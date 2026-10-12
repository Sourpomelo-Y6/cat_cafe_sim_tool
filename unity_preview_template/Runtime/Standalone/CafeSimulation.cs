using System;
using System.Linq;

namespace CatCafePreview.Standalone
{
    // First migration slice: base multi-seat service, shifts, health, management.
    // No UI, filesystem, clock, Unity or HTTP dependency.
    public sealed partial class CafeSimulation
    {
        public readonly CafeRules Rules;
        public CafeSnapshot State { get; private set; }
        public CafeSimulation(CafeRules rules, int seed = 0, string objective = "free")
        {
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            if (rules.version != 1 || rules.cats == null || rules.cats.Length == 0 || seed < 0) throw new ArgumentException("Invalid initial configuration");
            State = new CafeSnapshot { seed = seed, rules_id = rules.rules_id, instance_id = Guid.NewGuid().ToString("N"), funds = rules.starting_funds, opening_funds = rules.starting_funds, popularity = rules.starting_popularity };
            foreach (var cat in rules.cats.OrderBy(c => c.id, StringComparer.Ordinal)) State.cats.Add(new CafeCat { id = cat.id, stamina = rules.max_stamina });
            foreach (var cat in State.cats) EnsureGrowthCat(cat.id);
            if(rules.visiting_cat==null)State.visiting_cat=null;
            State.objective = objective; if (objective == "popularity") State.goal.Add(new CafeGoalStage { started_day = 1 }); else if(objective=="patron")State.patron=new CafePatron {id="patron",members=rules.patron.members.Select(r=>new CafePatronScore {id=r.id}).ToList()}; else if(objective=="bond")State.bond_goal=new CafeBondGoal {id="bond"}; else if (objective != "free") throw new ArgumentException("Invalid objective");
            PresentStoreEvent();
            PresentIntake();
            PresentRegularIntroduction();
            PresentVisitingCat();
        }
        public void Restore(CafeSnapshot value)
        {
            Validate(value);
            foreach (var e in value.store_events) e.draw = StoreDraw(value.seed, e.day, "occur");
            State = value;
            if(State.autoplay?.running==true && !State.autoplay.paused)PauseAutoplay();
            // A load starts a new command epoch. Old queued buttons cannot mutate it.
            State.instance_id = Guid.NewGuid().ToString("N"); State.revision = 0;
        }
        public void Validate(CafeSnapshot s)
        {
            if(s?.import_history!=null){ValidateImportedHistory(s);return;}
            if (s == null || s.version != 2 || Rules.service_assignment_version!=1 || Rules.autoplay_version!=1 || s.rules_id != Rules.rules_id || s.seed < 0 || s.day < 1 || s.tick < 0 || s.tick > Rules.opening_ticks
                || !new[] { "preparation", "open", "closed" }.Contains(s.phase)
                || s.cats == null || s.cats.Count != Rules.cats.Length + (s.intake_request != null && s.intake_request.status == "accepted" ? 1 : 0) + (s.regular_introduction != null && s.regular_introduction.status == "accepted" ? 1 : 0) + DispatchJoinedCount(s,s.day) + VisitingJoinedCount(s,s.day) + ShopJoinedCount(s,s.day) + RecruitmentJoinedCount(s,s.day) || s.visits == null || s.exchanges == null || s.queue == null || s.relationships == null || s.history == null
                || s.cats.Select(c => c.id).Distinct().Count() != s.cats.Count || s.revision < 0
                || !Finite(s.funds) || !Finite(s.opening_funds) || !Finite(s.revenue) || !Finite(s.expenses)
                || s.revenue < 0 || s.expenses < 0 || Math.Abs(s.funds - (s.opening_funds + s.revenue + s.item_sales_income + s.dispatch_income - s.expenses)) > 1e-7
                || s.history.Count != s.day - 1 || s.completed < 0
                || !Range(s.popularity, 0, Rules.popularity.cap)) throw new ArgumentException("Invalid standalone save");
            ValidateAutoplay(s);
            ValidateAdoption(s);
            ValidateRecruitment(s);
            ValidateShop(s);
            ValidateDispatchIntroductions(s);
            ValidateDispatch(s);
            ValidateEquipment(s);
            ValidateRestSpace(s);
            ValidateWaitingArea(s);
            ValidateSpecialists(s);
            ValidateExpansion(s);
            ValidateGoal(s);
            ValidatePatron(s);
            ValidatePlayer(s);
            ValidateClearResults(s);
            ValidateItems(s);
            ValidateHousing(s);
            ValidateIntake(s);
            ValidateVisitingCat(s);
            ValidateStoreEvents(s);
            ValidateGrowth(s);
            ValidateTrust(s);
            ValidateCustomers(s);
            ValidateRegularIntroduction(s);
            foreach (var c in s.cats)
                if (!Definitions(s).Any(d => d.id == c.id) || !Range(c.stamina, 0, Rules.max_stamina) || !Range(c.fatigue, 0, Rules.max_fatigue)
                    || !Range(c.stress, 0, 100) || !new[] { "healthy", "sick" }.Contains(c.health) || !new[] { "cafe", "missing", "dispatched", "adopted" }.Contains(c.activity)
                    || c.service_ticks < 0 || c.recovery_remaining < 0 || c.missing_remaining < 0) throw new ArgumentException("Invalid saved cat");
            if (s.phase == "preparation" && (s.tick != 0 || s.visits.Count != 0 || s.exchanges.Count != 0)
                || (s.phase == "closed") != (s.tick == Rules.opening_ticks)
                || s.visits.Select(v => v.id).Distinct().Count() != s.visits.Count
                || s.queue.Distinct().Count() != s.queue.Count
                || s.exchanges.Select(e => e.seat).Distinct().Count() != s.exchanges.Count
                || s.exchanges.Select(e => e.cat_id).Distinct().Count() != s.exchanges.Count
                || s.exchanges.Select(e => e.customer_id).Distinct().Count() != s.exchanges.Count
                || s.relationships.Select(r => r.cat_id + "/" + r.customer_id).Distinct().Count() != s.relationships.Count) throw new ArgumentException("Invalid saved phase or IDs");
            foreach (var id in s.queue) if (!s.visits.Any(v => v.id == id && v.departure == null && v.seated_at == -1)) throw new ArgumentException("Invalid saved queue");
            foreach (var v in s.visits) if (!Finite(v.bill) || v.seated_ticks < 0 || v.arrival < 0 || v.arrival >= s.tick) throw new ArgumentException("Invalid saved visit");
            if (!Range(s.popularity_gain, 0, Rules.popularity.cap) || s.phase != "closed" && s.popularity_gain != 0
                || s.visits.Count(v => v.satisfaction != null) != s.completed) throw new ArgumentException("Invalid satisfaction settlement");
            foreach (var v in s.visits.Where(v => v.satisfaction != null))
            {
                var a = v.satisfaction;
                string expected = a.score >= Rules.satisfaction.satisfied_score ? "satisfied" : a.score <= Rules.satisfaction.dissatisfied_score ? "dissatisfied" : "normal";
                if (v.departure == null || v.cat_id == null || a.level != expected || a.score < (v.specialist_result==null?-2:-3) || a.score > (v.specialist_result==null?4:5) || a.reasons == null
                    || a.bonus != (expected == "satisfied" ? Rules.satisfaction.satisfied_bonus : 0)) throw new ArgumentException("Invalid saved satisfaction");
            }
            foreach (var e in s.exchanges)
                if (s.phase != "open" || e.seat < 0 || e.seat >= s.seat_count || !s.cats.Any(c => c.id == e.cat_id)
                    || !s.visits.Any(v => v.id == e.customer_id && v.cat_id == e.cat_id && v.departure == null && v.seated_at >= 0)
                    || !Rules.types.Any(t => t.id == e.mode) || e.remaining < 1 || e.end_reason != null
                    || !Range(e.stamina, 0, Rules.max_stamina) || !Range(e.tension, 0, Rules.exchange.target) || !Range(e.engagement, 0, Rules.exchange.target)
                    || !Finite(e.bonus) || !Finite(e.affinity_pending) || !Range(e.affinity_start, 0, 100)) throw new ArgumentException("Invalid saved exchange");
            foreach (var r in s.relationships) if (!s.cats.Any(c => c.id == r.cat_id) || string.IsNullOrEmpty(r.customer_id) || !Range(r.affinity, 0, 100)) throw new ArgumentException("Invalid relationship");
            for (int i = 0; i < s.history.Count; i++)
            {
                var h = s.history[i];
                if (h.day != i + 1 || !Finite(h.closing_funds) || !Range(h.revenue, 0, double.MaxValue) || !Range(h.expenses, 0, double.MaxValue)
                    || Math.Abs(h.closing_funds - (h.opening_funds + h.revenue + h.item_sales_income + h.dispatch_income - h.expenses)) > 1e-7
                    || Math.Abs(h.opening_funds - (i == 0 ? Rules.starting_funds : s.history[i - 1].closing_funds)) > 1e-7) throw new ArgumentException("Invalid finance history");
            }
        }
        private static bool Finite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
        private static bool Range(double v, double min, double max) => Finite(v) && v >= min && v <= max;
        private static double Clamp(double v, double max) => Math.Max(0, Math.Min(max, v));
        public void Execute(string kind,string[] working=null,string catId=null,string choice=null,string source=null,string seatId=null,string customerId=null)
        {TraceImported(new CafeReplayOperation {kind=kind,working=working,cat_id=catId,choice=choice,source=source,seat_id=seatId,customer_id=customerId},()=>{ExecuteCore(kind,working,catId,choice,source,seatId,customerId);UpdateDispatchUnlocks();});}
        private void ExecuteCore(string kind, string[] working = null, string catId = null, string choice = null, string source = null, string seatId = null,string customerId=null)
        {
            var s = State;
            if(kind=="open_recruitment"){OpenRecruitment();return;}
            if (s.game_over != null) throw new InvalidOperationException("経営終了後は保存・閲覧のみ利用できます。");
            if(kind=="set_auto_assignment"){SetAutoAssignment(choice);return;}
            if(kind=="assign_service"){AssignService(customerId,catId,seatId);return;}
            if(kind=="resolve_adoption"){ResolveAdoption(source,choice);return;}
            if(AdoptionWaiting && !kind.StartsWith("resolve_growth",StringComparison.Ordinal) && kind!="resolve_activity" && kind!="resolve_missing" && kind!="resolve_customer_trust" && kind!="resolve_dispatch_trouble" && kind!="resolve_dispatch_choice" && !(kind=="resolve_store_event" && choice!="close"))throw new InvalidOperationException("譲渡の申し出に回答してください。");
            if(kind=="configure_adoption"){ConfigureAdoption(choice);return;}
            if(kind=="start_popularity_challenge" || kind=="start_objective" && choice=="popularity"){StartPopularityChallenge();return;}
            if(kind=="enable_bond_goal" || kind=="enable_patron" || kind=="start_objective" && (choice=="bond" || choice=="patron")){StartIndependentObjective(kind=="enable_bond_goal"?"bond":kind=="enable_patron"?"patron":choice);return;}
            if(kind=="player_step" || kind=="player_finish"){AdvancePlayer(choice,source,kind=="player_finish");return;}
            if(PlayerActive)throw new InvalidOperationException("進行中のプレイヤー交流を再開または終了してください。");
            if(kind=="player_begin"){BeginPlayer(catId);return;}
            if(kind=="resolve_dispatch_trouble"){ResolveDispatchTrouble(source,choice);return;}
            if(kind=="resolve_dispatch_choice"){ResolveDispatchChoice(source,choice);return;}
            if (kind == "resolve_dispatch_introduction") { ResolveDispatchIntroduction(source,choice); return; }
            if(kind=="resolve_activity"){ReceiveDispatch(source,choice);return;}
            if (kind == "resolve_reservation") { ResolveReservation(choice); return; }
            if (ReservationWaiting && !kind.StartsWith("resolve_growth", StringComparison.Ordinal) && kind != "resolve_missing" && kind != "resolve_customer_trust" && !(kind == "resolve_store_event" && choice != "close")) throw new InvalidOperationException("特別予約に回答してください。");
            if((DispatchWaiting || DispatchChoiceWaiting || DispatchTroubleWaiting) && !kind.StartsWith("resolve_growth",StringComparison.Ordinal) && kind!="resolve_missing" && kind!="resolve_customer_trust")throw new InvalidOperationException("派遣の帰還結果を確認してください。");
            if(kind=="continue_bond_goal"){ContinueBond();return;}
            if(kind=="continue_patron"){ContinuePatron();return;}
            if (kind == "continue_goal" || kind == "advance_goal") { RespondGoal(kind); return; }
            if (kind == "purchase_housing" || kind == "upgrade_housing") { ExpandHousing(kind); return; }
            if (kind.StartsWith("resolve_growth", StringComparison.Ordinal)) { ResolveGrowth(kind, catId, choice); return; }
            if (kind == "resolve_customer_trust") { ResolveCustomerTrust(source, choice); return; }
            if (kind == "recruit_cat") { RecruitCat(catId); return; }
            if (kind == "purchase_cat") { PurchaseCat(catId); return; }
            if (kind == "resolve_visiting_cat") { ResolveVisitingCat(choice); return; }
            if (kind == "resolve_regular_introduction") { ResolveRegularIntroduction(choice); return; }
            if (kind == "resolve_intake" || kind == "resolve_intake_request") { ResolveIntake(choice); return; }
            if (BondWaiting && kind != "resolve_missing" && !(kind == "resolve_store_event" && choice != "close"))throw new InvalidOperationException("好感度目標の結果を確認してください。");
            if (PatronWaiting && kind != "resolve_missing" && !(kind == "resolve_store_event" && choice != "close"))throw new InvalidOperationException("有力者目標の結果を確認してください。");
            if (GoalWaiting && kind != "resolve_missing" && !(kind == "resolve_store_event" && choice != "close")) throw new InvalidOperationException("人気目標の結果を確認してください。");
            if (CustomerTrustWaiting && kind != "resolve_missing" && !(kind == "resolve_store_event" && choice != "close")) throw new InvalidOperationException("顧客の信頼回復イベントに回答してください。");
            if (kind != "resolve_missing" && s.cats.Any(c => c.activity == "missing" && c.missing_remaining == 0 && !DispatchMissing(State,c.id))) throw new InvalidOperationException("帰還結果を確認してください。");
            if (IntakeWaiting && kind != "resolve_missing" && kind != "resolve_store_event") throw new InvalidOperationException("保護猫の受け入れ依頼に回答してください。");
            if (RegularIntroductionWaiting && kind != "resolve_missing" && kind != "resolve_store_event") throw new InvalidOperationException("常連からの猫紹介で迎えるか見送るかを選んでください。");
            if (DispatchIntroductionWaiting && kind != "resolve_missing" && kind != "resolve_store_event") throw new InvalidOperationException("派遣先の猫紹介に回答してください。");
            if (GrowthWaiting && kind != "resolve_missing" && kind != "resolve_store_event") throw new InvalidOperationException("猫の成長・習熟を選択してください。");
            if (StoreEventWaiting && kind != "resolve_store_event" && kind != "resolve_missing" && !(kind == "day_off" && CurrentStoreEvent.type == "trouble")) throw new InvalidOperationException("店舗イベントに回答してください。");
            switch (kind)
            {
                case "begin_second_group_type_practice": case "begin_second_group_second_type_practice": case "begin_third_group_practice": case "begin_third_group_type_practice": case "begin_third_group_second_type_practice": BeginGrowthPractice(kind, catId); break;
                case "resolve_store_event": ResolveStoreEvent(choice); return;
                case "dispatch_patron":if(!IsPatronDestination(choice))throw new ArgumentException("有力者の派遣先を選んでください。");Dispatch(catId,choice);break;
                case "dispatch":case "dispatch_general":Dispatch(catId,choice);break;
                case "expand_seats": ExpandSeats(); break;
                case "purchase_waiting_area": case "upgrade_waiting_area": ChangeWaitingArea(kind); break;
                case "purchase_rest_space": case "upgrade_rest_space": case "soundproof_rest_space": ChangeRestSpace(kind); break;
                case "purchase_seat_equipment": PurchaseEquipment(seatId,choice); break;
                case "equip_seat": EquipSeat(seatId,choice); break;
                case "purchase_item": PurchaseItem(choice); break;
                case "sell_item": SellItem(source); break;
                case "use_item": UseItem(source, catId); break;
                case "set_shifts":
                    if (s.phase != "preparation" || working == null || working.Distinct().Count() != working.Length
                        || working.Any(id => !s.cats.Any(c => c.id == id && c.health == "healthy" && c.activity == "cafe"))) throw new ArgumentException("営業準備中に健康な猫の出勤を設定してください。");
                    foreach (var c in s.cats) c.working = working.Contains(c.id);
                    break;
                case "start_business":
                    Require("preparation");
                    if (!s.cats.Any(c => c.working) && !ReservationVisitToday) throw new InvalidOperationException("出勤猫を選んでください。");
                    s.phase = "open"; Advance(); break;
                case "advance_business": Require("open"); Advance(); break;
                case "next_day":
                    Require("closed");
                    AdvanceToNextDay();
                    break;
                case "day_off":
                    if (ReservationVisitToday) throw new InvalidOperationException("受け入れた特別予約があるため休業できません。");
                    Require("preparation");
                    if (StoreEventWaiting) { CurrentStoreEvent.status = "resolved"; CurrentStoreEvent.choice = "close"; }
                    var planned = s.cats.Where(c => c.working).Select(c => c.id).ToArray();
                    foreach (var c in s.cats) c.working = false;
                    Close(); s.tick = Rules.opening_ticks;
                    foreach (var c in s.cats) c.working = planned.Contains(c.id) && c.health == "healthy" && c.activity == "cafe";
                    if (s.game_over == null) AdvanceToNextDay();
                    s.revision++;
                    return;
                case "resolve_missing":
                    var cat = s.cats.Find(c => c.id == catId && c.activity == "missing" && c.missing_remaining == 0 && !DispatchMissing(State,c.id));
                    if (cat == null) throw new ArgumentException("帰還待ちの猫を選んでください。");
                    cat.activity = "cafe"; cat.stamina = Rules.max_stamina; cat.cannot_continue = cat.health != "healthy"; cat.stress = Rules.return_stress;
                    double cost = cat.kitten ? Rules.kitten_cost : 0; s.funds -= cost; s.expenses += cost; CheckEnd(); break;
                default: throw new NotSupportedException("単独版の第1段階では未対応の操作です：" + kind);
            }
            s.revision++;
        }
        private void Require(string phase) { if (State.phase != phase) throw new InvalidOperationException("現在の営業段階では実行できません。"); }
        private void AdvanceToNextDay()
        {
            var s = State;
            s.history.Add(new CafeDayResult { day = s.day, dispatch_trouble_expenses=s.dispatch_trouble_expenses, dispatch_income=s.dispatch_income, waiting_area_expenses=s.waiting_area_expenses, waiting_area_cost=s.waiting_area_cost, rest_space_expenses=s.rest_space_expenses, equipment_expenses=s.equipment_expenses, seat_count = s.seat_count, expansion_expenses = s.expansion_expenses, goal_popularity = s.goal_popularity, arrivals = s.visits.Count, completed = s.completed, opening_funds = s.opening_funds, revenue = s.revenue, expenses = s.expenses, closing_funds = s.funds, popularity_gain = s.popularity_gain, recruitment_expenses = s.recruitment_expenses, pet_shop_expenses = s.pet_shop_expenses, housing_expenses = s.housing_expenses, housing_cost = s.housing_cost, item_expenses = s.item_expenses, item_sales_income = s.item_sales_income, satisfied = s.visits.Count(v => v.satisfaction != null && v.satisfaction.level == "satisfied"), normal = s.visits.Count(v => v.satisfaction != null && v.satisfaction.level == "normal"), dissatisfied = s.visits.Count(v => v.satisfaction != null && v.satisfaction.level == "dissatisfied") });
            s.history[s.history.Count - 1].customer_records = s.visits.Select(v => new CafeCustomerRecord { customer_id = v.id, cat_id = v.cat_id, service_ticks=v.seated_ticks, completed_tick=v.completed_tick, departure = v.departure, satisfaction = v.satisfaction?.level, connect_count=v.connect_count, open_up_count=v.open_up_count, simultaneous_count=v.simultaneous_count, specialist_result=v.specialist_result, specialist_bonus=v.specialist_bonus, service_count=v.service_count, service_seat=v.service_seat, service_equipment=v.service_equipment, type_actions = (int[])v.type_actions.Clone(), affinity_delta = v.affinity_delta, stamina_spent = v.stamina_spent }).ToList();
            s.history[s.history.Count - 1].resting_cats = s.resting_cats.ToList(); s.resting_cats.Clear();
            foreach(var player in s.player_cats)player.today=0;
            s.day++; s.tick = 0; s.phase = "preparation"; s.opening_funds = s.funds; s.revenue = s.expenses = 0; s.completed = 0; s.recruitment_expenses = s.pet_shop_expenses = 0; s.waiting_area_expenses=s.waiting_area_cost=0; s.equipment_expenses=s.rest_space_expenses=0; s.housing_expenses = s.housing_cost = s.item_expenses = s.item_sales_income = s.expansion_expenses = s.goal_popularity = 0;
            s.visits.Clear(); s.queue.Clear(); s.exchanges.Clear(); s.popularity_gain = 0;s.dispatch_income=s.dispatch_trouble_expenses=0;
            foreach (var c in s.cats) { c.service_ticks = 0; if (c.activity == "cafe") { c.stamina = Rules.max_stamina; c.cannot_continue = c.health != "healthy"; } c.working &= c.health == "healthy" && c.activity == "cafe"; }
            foreach (var cat in State.cats) EnsureGrowthCat(cat.id);
            PresentStoreEvent();
            PresentIntake();
            PresentRegularIntroduction();
            PresentVisitingCat();
            PresentDispatchIntroductions();
            PresentCustomerTrust();
            PresentReservation();
        }
        private string Preference(string customer) => State.goal.Count>0 && customer=="advanced-longhair"?"long_hair":State.goal.Count>0 && customer=="vip-calico"?Rules.vip_customer.feature:State.goal.Count>0 && customer=="advanced-white" ? Rules.advanced_customer.feature : State.goal.Count>0 && customer=="reservation-longhair" ? Rules.reservation.feature : Rules.preference_pool[(int)(PythonRandom.Hash64(State.seed + ":" + customer) % (ulong)Rules.preference_pool.Length)];
        private bool Matched(CafeCat c, string customer) => Array.IndexOf(Definition(c.id).features, Preference(customer)) >= 0;
        private void Advance()
        {
            var s = State;
            // Assignment precedes arrival, matching CafeInteractionSession.automatic_step.
            if(AssignmentWaiting)throw new InvalidOperationException("待機中の客に担当猫と席を割り当てるか、自動割り当てをONにしてください。");
            while(s.auto_assignment && s.queue.Count>0)
            {
                int[] free=Enumerable.Range(0,s.seat_count).Where(i=>!DisabledSeat(i) && !s.exchanges.Any(e=>e.seat==i)).ToArray();if(free.Length==0)break;
                int seat=PreferredSeat(s.queue[0],free);
                string customer = s.queue[0];
                var cat = s.cats.Where(c => c.working && c.health == "healthy" && c.activity == "cafe" && !c.cannot_continue && c.stamina > 0 && !s.exchanges.Any(e => e.cat_id == c.id))
                    .OrderByDescending(c => Matched(c, customer)).ThenByDescending(c => c.stamina).ThenBy(c => c.id, StringComparer.Ordinal).FirstOrDefault();
                if (cat == null) break;
                StartService(customer,cat,seat);
            }
            s.exchanges.Sort((a, b) => a.seat.CompareTo(b.seat));
            foreach (var arrival in CustomerSchedule(s.day).Where(a => a.tick == s.tick)) Arrive(arrival.customer_id);
            foreach (var e in s.exchanges.OrderBy(e => e.seat).ToArray())
            {
                var cat = s.cats.Find(c => c.id == e.cat_id); var definition = Definition(cat.id);
                Choose(e, out string action, out string target); string replayBefore=ReplayBefore(e);int replayOpened=e.open_up_count; StepExchange(e, definition, action, target); RecordReplayAction(e,action,target,replayBefore,replayOpened);
                cat.stamina = e.stamina; cat.service_ticks++; var visit = s.visits.Find(v => v.id == e.customer_id); visit.seated_ticks++;
                if (e.end_reason != null)
                {
                    Depart(visit, e.end_reason == "exhausted" ? "interaction_exhausted" : s.tick + 1 >= Rules.opening_ticks ? "closing" : "interaction_time_limit");
                    visit.completed_tick=s.tick; visit.type_actions=(int[])e.type_actions.Clone(); visit.service_seat=e.service_group!=""?e.seat:-1; visit.service_equipment=e.service_group!="" && e.service_group==e.equipment_group; visit.service_count=e.service_group==""?0:Rules.types.Select((t,i)=>t.group==e.service_group?e.type_actions[i]:0).Sum();
                    visit.connect_count=e.connect_count; visit.open_up_count=e.open_up_count; visit.simultaneous_count=e.simultaneous_count; SetSpecialistResult(visit);
                    visit.satisfaction = CafeSatisfactionEvaluator.Evaluate(Rules.satisfaction, e, Matched(cat, e.customer_id),visit.specialist_result,SpecialistLabel(e.customer_id));
                    ApplyCustomerService(State.loyalty, State.discontent, s.day, visit.id, visit.satisfaction.level, visit.specialist_result);
                    ApplyTrustService(visit.id, visit.satisfaction.level);
                    double bonus = e.bonus + visit.satisfaction.bonus + visit.specialist_bonus;
                    visit.bill += bonus; s.funds += bonus; s.revenue += bonus;
                    var relationship = s.relationships.Find(r => r.cat_id == e.cat_id && r.customer_id == e.customer_id);
                    if (relationship == null) { relationship = new CafeRelationship { cat_id = e.cat_id, customer_id = e.customer_id }; s.relationships.Add(relationship); }
                    relationship.affinity = Clamp(e.affinity_start + e.affinity_pending, 100);
                    visit.type_actions = (int[])e.type_actions.Clone(); visit.affinity_delta = relationship.affinity - e.affinity_start; visit.stamina_spent = e.stamina_spent;
                    ConsiderAdoption(cat.id,visit,relationship.affinity);
                    GrowthService(cat.id, visit);
                    s.completed++; s.exchanges.Remove(e); if (cat.stamina == 0) cat.cannot_continue = true;
                }
            }
            foreach (string id in s.queue.ToArray()) { var v = s.visits.Find(r => r.id == id); if (s.tick - v.arrival + 1 >= MaxWaitTicks) Depart(v, "wait_timeout"); }
            if (s.tick + 1 >= Rules.opening_ticks) { foreach (var v in s.visits.Where(v => v.departure == null).ToArray()) Depart(v, "closing"); Close(); }
            s.tick++;
        }
        private void Depart(CafeVisit visit, string reason)
        {
            if (visit.departure != null) return;
            visit.departure = reason; visit.bill = visit.seated_ticks * Rules.time_price;
            State.funds += visit.bill; State.revenue += visit.bill; State.queue.Remove(visit.id);
            if (reason == "queue_full" || reason == "wait_timeout") ChangeDiscontent(State.discontent, State.day, visit.id, Rules.discontent.waiting_gain);
        }
        private void Arrive(string id)
        {
            var visit = new CafeVisit { id = id, arrival = State.tick }; State.visits.Add(visit);
            if (State.queue.Count >= QueueCapacity) Depart(visit, "queue_full"); else State.queue.Add(id);
        }
        private string BestGrowthType(CafeExchange e, string group, string current, string exclude = null)
        {
            var p = Definition(e.cat_id);
            bool multiple = e.growth_types.GroupBy(GrowthGroup).Any(g => g.Count() > 1);
            string second = e.growth_groups.Length > 1 ? e.growth_groups[1] : null, third = e.growth_groups.Length > 2 ? e.growth_groups[2] : null;
            return Rules.types.Select((t,i) => new { t, i }).Where(x => x.t.id != current && x.t.group != exclude && (group == null || x.t.group == group))
                .OrderByDescending(x => multiple ? group != null && (group == second || group == third) && e.growth_types.Contains(x.t.id) : e.growth_types.Contains(x.t.id))
                .ThenByDescending(x => x.t.gain * p.type_preferences[x.i] * (multiple && e.growth_types.Contains(x.t.id) ? Rules.growth.type_mastery_engagement_multiplier : 1))
                .ThenBy(x => x.i).Select(x => x.t.id).FirstOrDefault();
        }
        public void Choose(CafeExchange e, out string action, out string target)
        {
            target = null; action = "direct";
            if (e.connect_pending || e.tension >= Rules.exchange.optional_threshold) { action = "connect"; return; }
            if (e.stamina <= 20) { action = "pause"; return; }
            string mastery = e.service_group!=""?e.service_group:e.growth_groups.FirstOrDefault(), currentGroup = GrowthGroup(e.mode);
            string[] primaryTypes = e.growth_types.Where(t => GrowthGroup(t) == mastery).ToArray();
            string individual = primaryTypes.Length > 1 ? BestGrowthType(e, mastery, "") : primaryTypes.FirstOrDefault();
            if (e.last_group == null && individual != null && GrowthGroup(individual)==mastery && e.mode != individual) target = individual;
            else if (e.last_group == null && mastery != null && currentGroup != mastery) target = BestGrowthType(e, mastery, e.mode);
            else if (e.previous_reaction == "turn_away" || e.previous_reaction == "confused")
                target = mastery == null ? Rules.types[(Array.FindIndex(Rules.types, t => t.id == e.mode) + 1) % Rules.types.Length].id : BestGrowthType(e, currentGroup == mastery ? null : mastery, e.mode, currentGroup == mastery ? currentGroup : null);
            else if (mastery != null && e.streak >= 2 && e.previous_action != "switch")
                target = BestGrowthType(e, currentGroup == mastery ? null : mastery, e.mode, currentGroup == mastery ? currentGroup : null);
            if (target != null) action = "switch";
        }
        public void StepExchange(CafeExchange e, CatDefinition p, string action, string target = null)
        {
            var c = Rules.exchange; var t = Rules.types.First(r => r.id == e.mode);
            bool exchange = new[] { "direct", "adapt", "intense", "feint" }.Contains(action);
            if (e.end_reason != null || e.connect_pending && action != "connect" || !(t.actions.Contains(action) || action == "pause" || action == "switch" || action == "connect" && e.tension >= c.optional_threshold)
                || action == "switch" && (target == e.mode || !Rules.types.Any(r => r.id == target)) || action != "switch" && target != null) throw new ArgumentException("Invalid exchange action");
            bool connect = action == "connect", opened = e.open_up_pending || e.engagement >= c.optional_threshold && (e.tension >= c.optional_threshold || e.remaining == 1 || e.stamina <= c.low_stamina);
            e.tension -= connect ? c.gauge_cost : 0; e.engagement -= opened ? c.gauge_cost : 0;
            string reaction = null;
            if (!connect)
            {
                double score = 0, cost = 0;
                reaction = e.stamina <= c.low_stamina ? "listless" : "neutral";
                if (exchange)
                {
                    double gain; int strength = action == "intense" ? 2 : 1;
                    switch (action) { case "adapt": gain = c.adapt_gain; cost = c.adapt_cost; break; case "intense": gain = c.intense_gain; cost = c.intense_cost; break; case "feint": gain = c.feint_gain; cost = c.feint_cost; break; default: gain = c.direct_gain; cost = c.direct_cost; break; }
                    if (action == "adapt" && new[] { "turn_away", "listless", "confused" }.Contains(e.previous_reaction)) { gain = c.gentle_gain; cost = c.gentle_cost; strength = 0; }
                    if (action == "feint" && e.previous_action == "pause" && e.pause_after_interaction) gain = c.combo_gain;
                    bool switched = e.last_group != null && e.last_group != t.group;
                    e.streak = e.last_group == t.group ? e.streak + 1 : 1; e.last_group = t.group;
                    double preference = p.type_preferences[Array.IndexOf(Rules.types, t)], intensity = p.intensity_preferences[strength];
                    score = gain * t.gain * preference * intensity * Math.Max(c.boredom_floor, 1 - p.boredom_decay * (e.streak - 1)) * (switched ? 1 + p.switch_affinity : 1);
                    cost *= t.cost;
                    reaction = preference == 0 || intensity == 0 ? "turn_away" : e.stamina <= c.low_stamina ? "listless" : score < c.confused_threshold ? "confused" : score >= c.enthusiastic_threshold ? "enthusiastic" : score >= c.favorable_threshold ? "favorable" : "neutral";
                }
                if(exchange && score>0 && (e.equipment_group=="" || e.equipment_group==t.group))score*=e.equipment_engagement;
                if (exchange) { e.type_actions[Array.IndexOf(GrowthTypes, e.mode)]++; if (score > 0 && e.growth_groups.Contains(t.group)) score *= Rules.growth.mastery_engagement_multiplier; if (score > 0 && e.growth_types.Contains(e.mode)) score *= Rules.growth.type_mastery_engagement_multiplier; }
                e.stamina_spent += Math.Min(e.stamina, cost);
                e.engagement = Math.Min(c.target, e.engagement + score * (reaction == "turn_away" || reaction == "listless" ? 0 : reaction == "confused" ? c.confused_multiplier : 1));
                e.stamina = Math.Max(0, e.stamina - cost);
                if (action == "pause") e.stamina = Math.Min(c.max_stamina, e.stamina + c.pause_recovery);
                if (action == "switch") e.mode = target;
                e.pause_after_interaction = action == "pause" && new[] { "direct", "adapt", "intense", "feint" }.Contains(e.previous_action);
            }
            else e.pause_after_interaction = false;
            double delta = opened ? c.open_up_tension : !exchange ? 0 : reaction == "enthusiastic" ? c.tension_enthusiastic : reaction == "favorable" ? c.tension_favorable : reaction == "neutral" ? c.tension_neutral : reaction == "confused" ? -c.tension_confused_loss : reaction == "listless" ? -c.tension_listless_loss : -c.tension_turn_away_loss;
            if (delta > 0 && !opened) delta *= e.tension_multiplier * e.equipment_tension;
            e.tension = Clamp(e.tension + delta, c.target);
            e.bonus += (connect ? c.connect_bonus : 0) + (opened ? c.open_up_bonus : 0) + (connect && opened ? c.simultaneous_bonus : 0);
            e.affinity_pending += (exchange ? reaction == "enthusiastic" ? c.affinity_enthusiastic : reaction == "favorable" ? c.affinity_favorable : reaction == "turn_away" ? -c.affinity_turn_away_loss : 0 : 0)
                + (connect ? c.affinity_connect : 0) + (opened ? c.affinity_open_up : 0) + (connect && opened ? c.affinity_simultaneous : 0);
            if (connect) e.connect_count++; if (opened) e.open_up_count++; if (connect && opened) e.simultaneous_count++;
            e.previous_action = action; e.previous_reaction = reaction; e.remaining--;
            e.end_reason = e.stamina == 0 ? "exhausted" : e.remaining == 0 ? "time_limit" : null;
            if (e.end_reason == "exhausted") e.affinity_pending -= c.affinity_exhausted_loss;
            e.connect_pending = e.end_reason == null && e.tension >= c.forced_threshold;
            e.open_up_pending = e.end_reason == null && e.engagement >= c.forced_threshold;
        }
        private void Close()
        {
            var s = State;
            GrowthRestDay();
            double after = Math.Min(Rules.popularity.cap, s.popularity + s.visits.Count(v => v.satisfaction != null && v.satisfaction.level == "satisfied") * Rules.popularity.gain_per_success + CurrentStoreEvent.popularity + s.visits.Sum(v=>SpecialistPopularity(v.id,v.specialist_result)));
            s.popularity_gain = after - s.popularity; s.popularity = after;
            foreach (var cat in s.cats.Where(cat => cat.activity == "missing" && !DispatchMissing(s,cat.id))) if (cat.missing_remaining > 0) cat.missing_remaining--;
            foreach (var cat in s.cats.Where(cat => cat.activity == "cafe"))
            {
                var p = Definition(cat.id);
                cat.fatigue = Clamp(cat.fatigue + (cat.working ? cat.service_ticks * Rules.fatigue_per_service_tick * p.service_fatigue : -Rules.rest_day_recovery * p.rest_fatigue - RestSpaceRecovery - (GrowthCat(cat.id).specialization == "rest" ? Rules.growth.rest_recovery_bonus : 0)), Rules.max_fatigue);
                if (cat.health == "sick") { cat.recovery_remaining = Math.Max(0, cat.recovery_remaining - 1); if (cat.recovery_remaining == 0) cat.health = "healthy"; }
                else
                {
                    double chance = Math.Min(Rules.max_probability, Math.Max(0, cat.fatigue - Rules.safe_fatigue) * Rules.probability_per_fatigue);
                    double draw = PythonRandom.Draw("[\"cafe-health-v1\", " + s.seed + ", " + s.day + ", \"" + cat.id + "\"]");
                    if (draw < chance) { cat.health = "sick"; cat.recovery_remaining = Rules.recovery_days; }
                }
                if (cat.health == "sick") cat.cannot_continue = true;
                cat.stress = Clamp(cat.stress + (cat.working ? cat.service_ticks * Rules.stress_per_service_tick * p.service_stress : -Rules.rest_recovery - RestSpaceStressRecovery), 100);
                if (cat.service_ticks > 0 && cat.stress >= Rules.runaway_threshold)
                {
                    cat.activity = "missing"; cat.working = false; cat.missing_remaining = Rules.missing_days;
                    cat.kitten = PythonRandom.Hash64("missing-kitten:" + s.seed + ":missing-" + s.day + "-" + cat.id) / 18446744073709551616.0 < Rules.kitten_probability;
                    s.popularity = Math.Max(0, s.popularity - Rules.popularity_loss);
                }
            }
            DispatchCloseDay();
            s.housing_cost = HousingDailyCost(s, s.day);
            s.waiting_area_cost=WaitingAreaDailyCost(s,s.day);
            double expenses = s.waiting_area_cost + s.housing_cost + Rules.base_cost + Rules.per_seat_cost * s.seat_count + (CurrentStoreEvent.choice == "repair" ? Rules.store_events.trouble_cost : 0);
            s.expenses += expenses; s.funds -= expenses; s.phase = "closed"; CheckEnd(); s.goal_popularity = s.popularity; SettleGoal();
        }
        private void CheckEnd() { if (State.game_over == null) State.game_over = State.funds <= 0 ? "funds" : State.popularity <= 0 ? "popularity" : null; }
    }
}
