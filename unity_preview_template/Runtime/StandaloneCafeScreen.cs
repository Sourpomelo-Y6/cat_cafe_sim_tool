using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace CatCafePreview
{
    // Explicitly limited UI: unavailable migration features have no live buttons.
    public sealed class StandaloneCafeScreen : MonoBehaviour
    {
        private LocalCafeBackend backend;
        private PythonCafeState state;
        private bool title = true, busy;
        private bool showCustomers,showClearResults;
        private string notice = "Pythonサーバー不要の第1段階です。自由営業の基本ルールを確認できます。";
        private Vector2 scroll;
        private Font font;
        private void Awake() { CafeBackends.Standalone = true; backend = (LocalCafeBackend)CafeBackends.Local; state = backend.View(); font = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic", "Meiryo", "Noto Sans CJK JP" }, 18); }
        private void Start()
        {
            var args = Environment.GetCommandLineArgs(); int index = Array.IndexOf(args, "-catCafeSmoke");
            if (index < 0 || index + 1 >= args.Length) return;
            try { StandaloneCafeSmoke.Run(args[index + 1]); Application.Quit(0); }
            catch (Exception ex) { Debug.LogException(ex); Application.Quit(1); }
        }
        private IEnumerator Send(string kind, string[] working = null, int slot = 1, string catId = "", string choice = "free", string eventId = "", string seatId = "")
        {
            if (busy) yield break;
            busy = true;
            var cmd = new PythonShiftCommand { request_id = Guid.NewGuid().ToString("N"), instance_id = state.instance_id, expected_revision = state.revision, kind = kind, choice = choice, working_cats = working ?? new string[0], save_id = slot.ToString(), save_slot = slot, cat_id = catId, event_id = eventId, seat_id=seatId };
            yield return backend.Request("/commands", JsonUtility.ToJson(cmd), reply =>
            {
                var result = JsonUtility.FromJson<PythonCommandReply>(reply.text);
                state = backend.View(); notice = string.IsNullOrEmpty(result.error) ? "操作を反映しました。" : result.error;
                if (string.IsNullOrEmpty(result.error) && (kind == "new_game" || kind == "load_game")) {title = false;showClearResults=false;scroll=Vector2.zero;}
            });
            busy = false;
        }
        private void OnGUI()
        {
            if (font != null) GUI.skin.font = font;
            GUI.skin.label.fontSize = 18; GUI.skin.button.fontSize = 18; GUI.skin.toggle.fontSize = 18;
            GUILayout.BeginArea(new Rect(20, 20, Screen.width - 40, Screen.height - 40));
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("猫カフェ — Unity単独版・第1段階");
            GUILayout.Label(notice); GUI.enabled = !busy;
            if (title)
            {
                GUILayout.Label("自由営業・段階人気目標 / 初期猫5匹 / 2席。白猫のこだわり客・特別予約に対応。静かな交流・遊び・触れ合い客と接客設備に対応。長毛専門客・VIPにも対応。人気のクリア記録・結果表示・継続営業に対応。通常派遣と途中の二択イベントに対応。他の派遣先と目標は後続移植。\n新規開始は単独版スロット1へ初期状態を保存します。");
                if (GUILayout.Button("人気目標で新規開始（移植範囲）")) StartCoroutine(Send("new_game",choice:"popularity"));
                if (GUILayout.Button("新規開始")) StartCoroutine(Send("new_game"));
                foreach (var save in backend.ListSaves().saves)
                {
                    GUI.enabled = !busy && save.phase != "invalid";
                    if (GUILayout.Button("再開：" + save.label)) StartCoroutine(Send("load_game", slot: save.slot));
                }
            }
            else
            {
                GUILayout.Label(state.day + "日目 / " + (state.closed ? "閉店" : state.can_set_shifts ? "営業準備" : "営業中") + " / 時刻 " + state.tick + "/" + state.opening_ticks);
                GUILayout.Label("資金 " + state.funds + " / 売上 " + state.revenue + " / 支出 " + state.finance.total_expenses + " / 完了接客 " + state.completed_interactions + " / 待機 " + state.waiting_count);
                GUILayout.Label("人気 " + state.popularity + " / 本日の人気上昇 " + state.popularity_gain + " / 満足 " + state.customer_satisfaction.satisfied + " / 普通 " + state.customer_satisfaction.normal + " / 不満 " + state.customer_satisfaction.dissatisfied);
                if (state.game_over != null) GUILayout.Label("経営終了：" + state.game_over.reason);
                var ending=state.ending;
                if(ending!=null)
                {
                    GUILayout.Label(ending.title+"\n"+ending.details+"\n"+ending.reason);
                    GUI.enabled=!busy && ending.can_continue;
                    if(GUILayout.Button("継続営業（結果を確認）"))StartCoroutine(Send("continue_goal"));
                    GUI.enabled=!busy;
                    if(GUILayout.Button("タイトルへ戻る"))title=true;
                }
                GUI.enabled=!busy;
                showClearResults=GUILayout.Toggle(showClearResults,"クリア記録を見返す");
                if(showClearResults)
                {
                    if(state.clear_results.rows.Length==0)GUILayout.Label("クリア記録はまだありません。");
                    foreach(var record in state.clear_results.rows)GUILayout.Label(record.label+" クリア！\n"+record.details+"\n"+record.notes);
                }
                GUILayout.Label("派遣 / 本日の派遣収入 "+state.finance.dispatch_income+" / 本日の捜索費 "+state.finance.dispatch_trouble_expenses);
                foreach(var option in state.general_dispatch.options)
                {
                    GUILayout.Label(option.label+" / "+option.days+"日 / 報酬 "+option.reward+" / "+option.detail+" / "+option.reason);
                    GUI.enabled=!busy && option.can_select;
                    if(GUILayout.Button(option.label+"へ派遣"))StartCoroutine(Send("dispatch_general",catId:option.cat_id,choice:option.choice));
                }
                foreach(var returned in state.general_dispatch.events)
                {
                    GUILayout.Label(returned.label+" / "+returned.status+" / 残り "+returned.remaining+"日 / "+returned.detail+" / "+returned.reason);
                    GUI.enabled=!busy && returned.can_select;
                    if(GUILayout.Button(returned.label+"の帰還を確認"))StartCoroutine(Send("receive_general",catId:returned.cat_id,choice:returned.choice));
                }
                GUI.enabled=!busy;
                GUILayout.Label("店舗イベント：" + state.store_event_label);
                GUILayout.Label("本日の来店予定：" + string.Join(" / ", state.customer_schedule.Select(a => a.customer_id + " @" + a.tick)));
                showCustomers = GUILayout.Toggle(showCustomers, "顧客の常連度・不満を表示");
                if (showCustomers) foreach (var customer in state.customer_progress)
                    GUILayout.Label(customer.customer_id + " / 常連度 " + customer.loyalty + (customer.regular_day > 0 ? "（" + customer.regular_day + "日目に常連化・追加来店 " + new[] { "月", "火", "水", "木", "金", "土", "日" }[customer.extra_weekday] + "）" : "") + " / 不満 " + customer.discontent + (customer.suspended_until > 0 ? " / 来店停止期限 " + customer.suspended_until + "日目" : ""));
                if (state.store_event != null)
                {
                    foreach (var option in state.store_event.choices)
                    {
                        GUI.enabled = !busy && option.can_select;
                        if (GUILayout.Button(option.label + "：" + option.detail)) StartCoroutine(Send("resolve_store_event", choice: option.choice));
                    }
                }
                GUILayout.Label("顧客の信頼：営業を続ける前に警告へ回答してください。回復を試みると、来店停止終了後の満足接客で不満25へ回復。不満接客では永久離脱。見送りは即時離脱。離脱時は人気が" + state.customer_trust.popularity_loss + "低下します。");
                foreach (var trust in state.customer_trust.events)
                {
                    GUILayout.Label(trust.customer_id + " / 警告日 " + trust.day + " / 来店停止 " + trust.suspensions + "回 / " + (trust.status == "waiting" ? "回答待ち" : trust.status == "recovery" ? "回復中" : trust.outcome == "recovered" ? "回復済み" : "永久離脱") + (trust.reason == "recovery_failed" ? "（回復失敗）" : trust.reason == "ignored" ? "（見送り）" : ""));
                    GUI.enabled = !busy && trust.can_respond;
                    if (trust.status == "waiting")
                    {
                        if (GUILayout.Button(trust.customer_id + "の信頼回復を試みる")) StartCoroutine(Send("resolve_customer_trust", choice: "recover", eventId: trust.event_id));
                        if (GUILayout.Button(trust.customer_id + "の回復を見送る（永久離脱）")) StartCoroutine(Send("resolve_customer_trust", choice: "ignore", eventId: trust.event_id));
                    }
                }
                foreach (var growth in state.growth_choices)
                {
                    GUILayout.Label(growth.name + " / " + growth.title + " / 実績 " + growth.experience + " / 必要 " + growth.threshold + " / 習得済み " + growth.learned);
                    foreach (var option in growth.choices)
                    {
                        GUI.enabled = !busy && option.can_select;
                        if (GUILayout.Button(option.label + "：" + option.detail)) StartCoroutine(Send(growth.kind, catId: growth.cat_id, choice: option.choice));
                    }
                }
                foreach (var practice in state.growth_practice_choices)
                {
                    GUI.enabled = !busy && practice.can_select;
                    if (GUILayout.Button(practice.label)) StartCoroutine(Send(practice.kind, catId: practice.cat_id));
                }
                if(state.objective=="popularity")GUILayout.Label(state.specialist_description);
                foreach(var result in state.specialist_results)GUILayout.Label(result.customer_id+" / 担当 "+result.cat_id+" / "+(result.result=="success"?"条件達成":"条件未達")+" / 分類交流 "+result.service_count+" / 専用設備 "+(result.customer_id=="advanced-longhair" || result.customer_id=="vip-calico"?"不要":result.service_equipment?"あり":"なし")+" / 心をつかむ "+result.connect_count+" / 心を開く "+result.open_up_count+" / 同時発動 "+result.simultaneous_count+" / 追加料金 "+result.bonus);
                var reservation=state.reservation;
                if(reservation!=null)
                {
                    GUILayout.Label("特別予約 / "+reservation.status+" / "+reservation.description+" / 来店日 "+reservation.visit_day+" / 結果 "+reservation.result+" / 追加料金 "+reservation.bonus+" / 人気 "+reservation.popularity_bonus);
                    GUI.enabled=!busy && reservation.can_accept;if(GUILayout.Button("予約を引き受ける"))StartCoroutine(Send("resolve_reservation",choice:"accept"));
                    GUI.enabled=!busy && reservation.can_decline;if(GUILayout.Button("予約を辞退する"))StartCoroutine(Send("resolve_reservation",choice:"decline"));
                }
                var equipment=state.seat_equipment;
                GUILayout.Label("接客設備 / "+equipment.reason+" / "+equipment.details);
                foreach(var seat in equipment.seats)
                {
                    GUILayout.Label(seat.seat_id+"："+seat.description);
                    foreach(var item in equipment.catalog)
                    {
                        GUI.enabled=!busy && item.can_purchase;
                        if(GUILayout.Button(seat.seat_id+"へ購入："+item.name+" / "+item.cost+" / "+item.effect))StartCoroutine(Send("purchase_seat_equipment",choice:item.kind_id,seatId:seat.seat_id));
                    }
                    GUI.enabled=!busy && equipment.can_manage;
                    if(GUILayout.Button(seat.seat_id+"の設備を取り外す"))StartCoroutine(Send("equip_seat",choice:"",seatId:seat.seat_id));
                    foreach(var item in equipment.owned)if(GUILayout.Button(seat.seat_id+"へ設置："+item.item_id+" "+item.name+"（現在 "+item.seat_id+"）"))StartCoroutine(Send("equip_seat",choice:item.item_id,seatId:seat.seat_id));
                }
                var expansion=state.expansion;
                GUILayout.Label("席数 "+expansion.seats+" / 次 "+expansion.next_seats+" / 費用 "+expansion.cost+" / 通常運営費 "+expansion.operating_cost+" → "+expansion.next_operating_cost+" / "+expansion.reason);
                GUI.enabled=!busy && expansion.can_expand;
                if(GUILayout.Button("席を増設"))StartCoroutine(Send("expand_seats"));
                var goal=state.goal_result;
                if(goal!=null)
                {
                    GUILayout.Label("人気目標 "+goal.stage+"段階 / "+goal.status+" / 目標 "+goal.target+" / 期限 "+goal.deadline+"日 / "+goal.details);
                    GUI.enabled=!busy && goal.can_continue;if(GUILayout.Button("目標結果を確認して営業継続"))StartCoroutine(Send("continue_goal"));
                    var next=state.next_goal;if(next!=null){GUI.enabled=!busy && next.can_advance;if(GUILayout.Button("次の人気目標へ（目標 "+next.target+"）"))StartCoroutine(Send("advance_goal"));}
                }
                var housing = state.housing;
                GUILayout.Label("飼育スペース：在籍 " + housing.count + " / 上限 " + housing.capacity + " / 空き " + housing.free + " / 拡張 " + housing.stage + "段階 / 維持費 " + housing.daily_cost + " / 通常運営費 " + housing.operating_cost);
                if (housing.stage < 2)
                {
                    GUILayout.Label("次の上限 " + housing.next_capacity + " / 費用 " + housing.cost + " / 購入後資金 " + housing.funds_after + " / 維持費 " + housing.next_daily_cost + " / " + housing.reason);
                    GUI.enabled = !busy && housing.can_expand;
                    if (GUILayout.Button(housing.stage == 0 ? "飼育スペースを拡張" : "飼育スペースを追加拡張")) StartCoroutine(Send(housing.kind));
                }
                var intake = state.intake_request;
                GUILayout.Label("受け入れ依頼：" + intake.name + " / " + (intake.status == "scheduled" ? "未発生" : intake.status == "waiting" ? "回答待ち" : intake.status == "accepted" ? "受け入れ済み" : "見送り済み") + " / 発生日 " + intake.day + " / 費用 " + intake.cost + " / 本日の受け入れ費用 " + state.finance.recruitment_expenses);
                if (intake.status == "waiting")
                {
                    GUILayout.Label(intake.personality + " / " + intake.features + " / " + intake.trait + " / " + intake.housing + " / 支払い後 " + (state.funds - intake.cost));
                    GUILayout.Label(intake.response_reason + intake.accept_reason);
                    GUI.enabled = !busy && intake.can_accept;
                    if (GUILayout.Button("迎える")) StartCoroutine(Send("resolve_intake", choice: "accept"));
                    GUI.enabled = !busy && intake.can_decline;
                    if (GUILayout.Button("見送る")) StartCoroutine(Send("resolve_intake", choice: "decline"));
                }
                var introduction = state.regular_introduction;
                GUILayout.Label("常連からの猫紹介：" + introduction.name + " / " + (introduction.status == "untriggered" ? "未発生" : introduction.status == "waiting" ? "回答待ち" : introduction.status == "accepted" ? "受け入れ済み" : "見送り済み"));
                if (introduction.status != "untriggered") GUILayout.Label("紹介者 " + introduction.customer_name + " / 提示日 " + introduction.presented_day + " / 回答日 " + introduction.resolved_day);
                if (introduction.status == "waiting")
                {
                    GUILayout.Label(introduction.personality + " / " + introduction.features + " / " + introduction.trait + " / " + introduction.housing + " / 費用 " + introduction.cost + " / 支払い後 " + introduction.funds_after);
                    GUILayout.Label(introduction.response_reason + introduction.accept_reason);
                    GUI.enabled = !busy && introduction.can_accept;
                    if (GUILayout.Button("紹介猫を迎える")) StartCoroutine(Send("resolve_regular_introduction", catId: introduction.cat_id, choice: "accept"));
                    GUI.enabled = !busy && introduction.can_decline;
                    if (GUILayout.Button("紹介を見送る")) StartCoroutine(Send("resolve_regular_introduction", catId: introduction.cat_id, choice: "decline"));
                }
                GUILayout.Label("用品：所持 " + state.items.count + " / 本日購入費 " + state.finance.item_expenses + " / 売却収入 " + state.finance.item_sales_income);
                foreach(var shop in state.items.shop)
                {
                    GUILayout.Label(shop.name+" / "+shop.effect+" / 費用 "+shop.cost+" / 購入後 "+shop.funds_after+" / 所持 "+shop.count+" / "+shop.reason);
                    GUI.enabled=!busy && shop.can_select;
                    if(GUILayout.Button(shop.name+"を購入"+(shop.ends_game?"（資金0で経営終了）":"")))StartCoroutine(Send("purchase_item",choice:shop.choice));
                }
                foreach (var item in state.items.owned)
                {
                    GUILayout.Label(item.name + " / " + item.effect + " / 売却価格 " + item.price + " / " + item.reason);
                    GUI.enabled = !busy && item.can_sell;
                    if (GUILayout.Button(item.name+"を売却")) StartCoroutine(Send("sell_item",choice:item.choice));
                    foreach (var target in item.targets)
                    {
                        GUI.enabled = !busy && target.can_select;
                        if (GUILayout.Button(target.name + "に使用：" + target.changes)) StartCoroutine(Send("use_item", catId: target.cat_id, choice: item.choice));
                    }
                }
                GUILayout.Label(state.items.history);
                foreach(var trouble in state.dispatch_troubles)
                {
                    GUILayout.Label(trouble.label+" / "+(trouble.status=="scheduled"?"派遣中":trouble.status=="not_triggered"?"トラブルなし":trouble.status=="waiting"?"対応待ち":"対応済み")+" / 家出日 "+trouble.missing_day+" / 回答日 "+trouble.choice_day);
                    GUILayout.Label(trouble.description);
                    if(trouble.status=="waiting")foreach(var option in trouble.choices)
                    {
                        GUILayout.Label(option.reason+option.description);GUI.enabled=!busy && option.can_select;
                        if(GUILayout.Button(option.label+" / 費用 "+option.cost+" / 支払い後 "+option.funds_after))StartCoroutine(Send("resolve_dispatch_trouble",catId:trouble.cat_id,eventId:trouble.event_id,choice:option.choice));
                    }
                    GUI.enabled=!busy;
                }
                foreach(var intro in state.dispatch_introductions)
                {
                    GUILayout.Label(intro.destination+"からの猫紹介："+intro.name+" / "+intro.status+" / 提示日 "+intro.presented_day+" / 回答日 "+intro.resolved_day);
                    if(intro.status=="waiting")
                    {
                        GUILayout.Label(intro.personality+" / "+intro.features+" / "+intro.trait+" / "+intro.housing+" / 費用 "+intro.cost+" / 支払い後 "+intro.funds_after);
                        GUILayout.Label(intro.response_reason+intro.accept_reason);
                        GUI.enabled=!busy && intro.can_accept;
                        if(GUILayout.Button("派遣紹介の猫を迎える"))StartCoroutine(Send("resolve_dispatch_introduction",catId:intro.cat_id,eventId:intro.event_id,choice:"accept"));
                        GUI.enabled=!busy && intro.can_decline;
                        if(GUILayout.Button("派遣紹介を見送る"))StartCoroutine(Send("resolve_dispatch_introduction",catId:intro.cat_id,eventId:intro.event_id,choice:"decline"));
                        GUI.enabled=!busy;
                    }
                }
                foreach(var encounter in state.dispatch_choices)
                {
                    GUILayout.Label(encounter.label+"："+encounter.title+" / "+(encounter.status=="resolved"?encounter.selected_label:"回答待ち"));
                    foreach(var option in encounter.choices)
                    {
                        GUI.enabled=!busy && option.can_select;
                        if(GUILayout.Button(option.label+" / 帰還報酬 "+option.return_reward+" / 疲労 "+option.fatigue+" / ストレス "+option.stress))StartCoroutine(Send("resolve_dispatch_choice",eventId:encounter.event_id,choice:option.choice));
                    }
                }
                foreach (var cat in state.cats)
                {
                    GUILayout.BeginHorizontal(); GUILayout.Label(cat.name + " / 体力 " + cat.stamina.ToString("0.0") + " / 疲労 " + cat.fatigue.ToString("0.0") + " / ストレス " + cat.stress.ToString("0.0") + " / " + cat.health_label + " / " + cat.activity_label);
                    GUI.enabled = !busy && !state.general_dispatch.events.Any(e=>e.needs_response) && !state.dispatch_troubles.Any(e=>e.status=="waiting") && !state.dispatch_choices.Any(e=>e.status=="waiting") && (state.reservation==null || state.reservation.status!="waiting") && state.store_event == null && state.intake_request.status != "waiting" && state.regular_introduction.status != "waiting" && !state.dispatch_introductions.Any(e=>e.status=="waiting") && !state.customer_trust.events.Any(e => e.status == "waiting") && state.growth_choices.Length == 0 && !(state.goal_result != null && state.goal_result.pending) && state.can_set_shifts && state.game_over == null && cat.health_status == "healthy" && cat.activity == "cafe";
                    bool work = GUILayout.Toggle(cat.working, "出勤");
                    if (work != cat.working) StartCoroutine(Send("set_shifts", state.cats.Where(c => c.cat_id == cat.cat_id ? work : c.working).Select(c => c.cat_id).ToArray()));
                    GUILayout.EndHorizontal(); GUILayout.Label(cat.growth_details);
                }
                GUI.enabled = !busy && !state.general_dispatch.events.Any(e=>e.needs_response) && !state.dispatch_troubles.Any(e=>e.status=="waiting") && !state.dispatch_choices.Any(e=>e.status=="waiting") && (state.reservation==null || state.reservation.status!="waiting") && state.game_over == null && !state.customer_trust.events.Any(e => e.status == "waiting") && state.growth_choices.Length == 0 && !(state.goal_result != null && state.goal_result.pending) && state.store_event == null && state.intake_request.status != "waiting" && state.regular_introduction.status != "waiting" && !state.dispatch_introductions.Any(e=>e.status=="waiting");
                if (!state.can_set_shifts || state.cats.Any(c => c.working) || state.reservation!=null && state.reservation.status=="accepted" && state.reservation.visit_day==state.day)
                    if (GUILayout.Button(state.closed ? "翌日へ" : state.can_set_shifts ? "営業開始" : "営業を1tick進める")) StartCoroutine(Send(state.closed ? "next_day" : state.can_set_shifts ? "start_business" : "advance_business"));
                GUI.enabled=GUI.enabled && !(state.reservation!=null && state.reservation.status=="accepted" && state.reservation.visit_day==state.day);
                if (state.can_set_shifts && GUILayout.Button("休業して翌日へ")) StartCoroutine(Send("day_off"));
                GUI.enabled = !busy && state.game_over == null;
                foreach (var cat in backend.Simulation.State.cats.Where(c => c.activity == "missing" && c.missing_remaining == 0 && !backend.Simulation.DispatchMissing(backend.Simulation.State,c.id)))
                    if (GUILayout.Button("帰還確認：" + cat.id + (cat.kitten ? "（引き渡し費用あり）" : ""))) StartCoroutine(Send("resolve_missing", catId: cat.id));
                foreach (var seat in state.seats) GUILayout.Label(seat.seat_id + "：" + (seat.disabled ? "応急処置により停止" : seat.customer_id == null ? "空席" : seat.customer_id + " / " + seat.cat_id));
                GUI.enabled = !busy;
                GUILayout.BeginHorizontal();
                for (int i = 1; i <= 3; i++) if (GUILayout.Button("保存 " + i)) StartCoroutine(Send("save_game", slot: i));
                GUILayout.EndHorizontal();
                if (GUILayout.Button("タイトルへ")) { title = true; notice = "進捗を残すには保存してください。"; }
            }
            GUI.enabled = true; GUILayout.EndScrollView(); GUILayout.EndArea();
        }
    }
}
