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
        private readonly CafeSaveStore saves;
        // Cache the response AND payload: one ID must never name different commands.
        private readonly Dictionary<string, KeyValuePair<string, CafeResponse>> replies = new Dictionary<string, KeyValuePair<string, CafeResponse>>();
        public LocalCafeBackend(string saveDirectory = null, CafeRules rules = null)
        {
            if (rules == null)
            {
                var asset = Resources.Load<TextAsset>("CatCafeStandalone/rules");
                if (asset == null) throw new InvalidOperationException("単独版の初期設定がありません。");
                rules = JsonUtility.FromJson<CafeRules>(asset.text);
            }
            Simulation = new CafeSimulation(rules);
            saves = new CafeSaveStore(saveDirectory ?? Path.Combine(Application.persistentDataPath, "standalone-v1"), EncodeSave, DecodeSave, Simulation.Validate);
        }
        private static string EncodeSave(CafeSaveEnvelope save) { save.double_bits = CafeDoublePrecision.Capture(save.state); return JsonUtility.ToJson(save, true); }
        private static CafeSaveEnvelope DecodeSave(string text) { var save = JsonUtility.FromJson<CafeSaveEnvelope>(text); save?.state?.NormalizeJsonStrings(); if (save?.state != null) CafeDoublePrecision.Restore(save.state, save.double_bits); return save; }
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
                    default: throw new NotSupportedException("単独版の第1段階では未対応です。");
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
                switch (cmd.kind)
                {
                    case "new_game":
                        if (cmd.choice != "free" && cmd.choice != "popularity") throw new NotSupportedException("単独版は自由営業と移植範囲の人気目標に対応しています。");
                        var initial = new CafeSimulation(Simulation.Rules, s.seed, cmd.choice).State;
                        saves.Save(1, initial); Simulation.Restore(initial); break;
                    case "save_game": saves.Save(cmd.save_slot == 0 ? 1 : cmd.save_slot, s); s.revision++; break;
                    case "load_game":
                        if (!int.TryParse(cmd.save_id, out int slot)) throw new ArgumentException("保存スロットを選んでください。");
                        Simulation.Restore(saves.Load(slot).state); break;
                    case "resolve_customer_trust": Simulation.Execute(cmd.kind, choice: cmd.choice, source: cmd.event_id); break;
                    case "resolve_regular_introduction":
                        if (cmd.cat_id != Simulation.Rules.regular_introduction.cat.id) throw new ArgumentException("紹介された猫を選んでください。");
                        Simulation.Execute(cmd.kind, choice: cmd.choice); break;
                    default: Simulation.Execute(cmd.kind, cmd.working_cats, cmd.cat_id, cmd.choice, cmd.choice,cmd.seat_id); break;
                }
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
                    list.Add(new PythonSavedGame { slot = slot, save_id = slot.ToString(), label = slot + "：" + s.day + "日目 / " + s.phase, created_at = save.created_at, objective_label = "自由営業（単独版・第1段階）", phase = s.phase });
                }
                catch (FileNotFoundException) { }
                catch (DirectoryNotFoundException) { }
                catch (Exception ex) { list.Add(new PythonSavedGame { slot = slot, save_id = slot.ToString(), label = slot + "：読み込み不可 / " + ex.Message, phase = "invalid" }); }
            }
            return new PythonSaveList { saves = list.ToArray() };
        }
        public PythonCafeState View()
        {
            var s = Simulation.State; var rules = Simulation.Rules;
            return new PythonCafeState
            {
                version = 1, instance_id = s.instance_id, revision = s.revision, objective = s.objective, phase = s.phase, day = s.day, tick = s.tick,
                can_set_shifts = s.phase == "preparation", closed = s.phase == "closed", opening_ticks = rules.opening_ticks,
                funds = (float)s.funds, revenue = (float)s.revenue, waiting_count = s.queue.Count, completed_interactions = s.completed,
                popularity = (float)s.popularity, popularity_gain = (float)s.popularity_gain,
                customer_satisfaction = new PythonSatisfactionCounts { satisfied = s.visits.Count(v => v.satisfaction != null && v.satisfaction.level == "satisfied"), normal = s.visits.Count(v => v.satisfaction != null && v.satisfaction.level == "normal"), dissatisfied = s.visits.Count(v => v.satisfaction != null && v.satisfaction.level == "dissatisfied") },
                store_event_label = StoreEventLabel(), store_event = StoreEventView(), items = SupplyView(), seat_equipment=EquipmentView(),
                specialist_results = s.visits.Where(v=>v.specialist_result!=null).Select(v=>new PythonSpecialistResult {customer_id=v.id,cat_id=v.cat_id,result=v.specialist_result,connect_count=v.connect_count,open_up_count=v.open_up_count,simultaneous_count=v.simultaneous_count,bonus=(float)v.specialist_bonus,service_count=v.service_count,service_equipment=v.service_equipment}).ToArray(), specialist_description = SpecialistDescription(), reservation = ReservationView(), customer_trust = CustomerTrustView(), customer_progress = CustomerProgress(), customer_schedule = Simulation.CustomerSchedule(s.day),
                expansion = ExpansionView(), next_goal = NextGoalView(), goal_result = GoalResultView(), housing = HousingView(), regular_introduction = RegularIntroductionView(), intake_request = IntakeView(), required_action = Simulation.ReservationWaiting ? "reservation" : Simulation.GoalWaiting ? "goal_result" : Simulation.IntakeWaiting ? "intake_request" : Simulation.RegularIntroductionWaiting ? "regular_introduction" : Simulation.CustomerTrustWaiting ? "customer_trust" : Simulation.StoreEventWaiting ? "store_event" : Simulation.GrowthWaiting ? "growth" : "", rest_proposal = new string[0], missing_cats = new PythonMissingCat[0], growth_choices = GrowthChoices(), growth_practice_choices = GrowthPracticeView(),
                new_game_options = new[] { new PythonNewGameOption {choice="popularity",label="人気目標（単独版の移植範囲）",detail="段階目標・席数拡張・追加来客",conditions="白猫のこだわり客・特別予約に対応。静かな交流・遊び・触れ合い客と接客設備に対応。長毛専門客・VIPにも対応。固定クリア記録・終了演出等は後続移植です。"}, new PythonNewGameOption { choice = "free", label = "自由営業（単独版・第1段階）", detail = "新規開始・出勤・自動接客・閉店・翌日・保存再開", conditions = "初期猫5匹 / 2席 / 資金1000。自由営業は3席まで。4席以降は段階人気目標の達成が必要。" } },
                cats = s.cats.Select(c => new PythonCatState { cat_id = c.id, content_cat_id = c.id, name = Simulation.Definition(c.id).name, stamina = (float)c.stamina, max_stamina = (float)rules.max_stamina, fatigue = (float)c.fatigue, stress = (float)c.stress, working = c.working, growth_details = GrowthDetails(c.id), health_status = c.health, health_label = c.health == "healthy" ? "健康" : "療養中", activity = c.activity, activity_label = c.activity == "cafe" ? "在店" : "家出中", records = new PythonCatRecords() }).ToArray(),
                seats = Enumerable.Range(0, s.seat_count).Select(i => { var e = s.exchanges.Find(x => x.seat == i); return new PythonSeatState { seat_id = "seat-" + (i + 1), customer_id = e?.customer_id, customer_name = e?.customer_id, cat_id = e?.cat_id, equipment_name=Simulation.Installed(i)?.name,equipment_id=s.equipment_placements.Find(p=>p.seat==i)?.item,equipment_description=Simulation.Installed(i)?.name??"設備なし", disabled = Simulation.DisabledSeat(i) }; }).ToArray(),
                customers = s.visits.Where(v => v.departure == null).Select(v => new PythonCustomerState { customer_id = v.id, name = v.id, status = s.queue.Contains(v.id) ? "waiting" : "seated" }).ToArray(),
                finance = new PythonFinanceState { seat_equipment_expenses=(float)s.equipment_expenses, opening_funds = (float)s.opening_funds, total_income = (float)(s.revenue+s.item_sales_income), total_expenses = (float)s.expenses, item_expenses = (float)s.item_expenses, item_sales_income = (float)s.item_sales_income, housing_expenses = (float)s.housing_expenses, housing_cost = (float)s.housing_cost, recruitment_expenses = (float)s.recruitment_expenses, net_cash_flow = (float)(s.revenue+s.item_sales_income - s.expenses), closing_funds = (float)s.funds },
                game_over = s.game_over == null ? null : new PythonGameOver { reason = s.game_over, day = s.day, popularity = (float)s.popularity }
            };
        }
    }
}
