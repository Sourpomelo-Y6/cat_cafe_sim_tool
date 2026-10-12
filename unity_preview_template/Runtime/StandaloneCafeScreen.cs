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
        private bool showCustomers,showClearResults,showBusinessLog;
        private PythonBusinessLogView businessLog;private string logPath="";private int logOffset,logIndex;
        private string adoptionConfirmation,adoptionConfirmationIdentity;
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
        private string autoObjective="popularity",autoMode="basic";private int autoDays=3;private float nextAutoStep;
        private void Update(){if(!title && !showBusinessLog && !busy && state?.autoplay?.running==true && !state.autoplay.paused && Time.unscaledTime>=nextAutoStep){nextAutoStep=Time.unscaledTime+.05f;StartCoroutine(Send("step_autoplay"));}}
        private string assignmentCustomer,assignmentCat,assignmentSeat,assignmentInstance;
        private IEnumerator Send(string kind, string[] working = null, int slot = 1, string catId = "", string choice = "free", string eventId = "", string seatId = "", string targetType = "",string customerId="")
        {
            if (busy) yield break;
            busy = true;
            var cmd = new PythonShiftCommand { request_id = Guid.NewGuid().ToString("N"), instance_id = state.instance_id, expected_revision = state.revision, kind = kind, choice = choice, working_cats = working ?? new string[0], save_id = slot.ToString(), save_slot = slot, cat_id = catId, event_id = eventId, seat_id=seatId,target_type=targetType,customer_id=customerId,autoplay_objective=autoObjective,autoplay_mode=autoMode,autoplay_days=autoDays };
            yield return backend.Request("/commands", JsonUtility.ToJson(cmd), reply =>
            {
                var result = JsonUtility.FromJson<PythonCommandReply>(reply.text);
                state = backend.View(); notice = string.IsNullOrEmpty(result.error) ? "操作を反映しました。" : result.error;
                if (string.IsNullOrEmpty(result.error) && (kind == "new_game" || kind == "load_game")) {title = false;showClearResults=false;scroll=Vector2.zero;}
            });
            busy = false;
        }
        private IEnumerator FetchBusinessLog(string route,bool export=false)
        {
            if(busy)yield break;busy=true;
            yield return backend.Request(route,null,reply=>{try{var view=JsonUtility.FromJson<PythonBusinessLogView>(reply.text);if(reply.result!=UnityEngine.Networking.UnityWebRequest.Result.Success)throw new Exception(view.error??reply.error);if(export){logPath=PythonBusinessLogPanel.WriteExport(view.log_json,null);notice="営業ログを出力しました："+logPath;}else{businessLog=view;logOffset=view.offset;logIndex=0;notice=view.notes;}}catch(Exception ex){businessLog=null;notice=ex.Message;}});busy=false;
        }
        private void DrawBusinessLog()
        {
            GUILayout.Label("営業ログ・詳細リプレイ");GUILayout.Label("ファイルの再生は独立した状態で検証します。現在の営業は変更しません。");
            if(GUILayout.Button("現在のログ"))StartCoroutine(FetchBusinessLog("/business-log?offset="+int.MaxValue));
            if(GUILayout.Button("現在のログを別ファイルへ出力"))StartCoroutine(FetchBusinessLog("/business-log/export",true));
            GUILayout.Label("確認する営業ログJSONの絶対パス");logPath=GUILayout.TextField(logPath);
            if(GUILayout.Button("ファイルを再生して確認"))StartCoroutine(FetchBusinessLog("/business-log?path="+Uri.EscapeDataString(logPath)+"&offset=0"));
            if(businessLog!=null){GUILayout.Label(businessLog.summary+"\n"+businessLog.notes);string path=businessLog.source=="現在の営業"?"":businessLog.source;GUILayout.BeginHorizontal();GUI.enabled=!busy && logOffset>0;if(GUILayout.Button("前の20件"))StartCoroutine(FetchBusinessLog("/business-log?path="+Uri.EscapeDataString(path)+"&offset="+Math.Max(0,logOffset-20)));GUI.enabled=!busy && logOffset+20<businessLog.total;if(GUILayout.Button("次の20件"))StartCoroutine(FetchBusinessLog("/business-log?path="+Uri.EscapeDataString(path)+"&offset="+(logOffset+20)));GUILayout.EndHorizontal();GUI.enabled=!busy;for(int i=0;i<businessLog.rows.Length;i++)if(GUILayout.Button(businessLog.rows[i].label))logIndex=i;if(businessLog.rows.Length>0)GUILayout.Label(businessLog.rows[Math.Min(logIndex,businessLog.rows.Length-1)].details);}
            GUI.enabled=!busy;if(GUILayout.Button("営業画面へ戻る")){showBusinessLog=false;businessLog=null;}
        }
        private void DrawAutoplay()
        {
            var view=state.autoplay;if(view==null)return;GUILayout.Label("複数日おまかせ\n"+view.notes);
            if(view.running){GUILayout.Label(view.paused?"一時停止中":"実行中");if(GUILayout.Button(view.paused?"おまかせを再開":"一時停止"))StartCoroutine(Send(view.paused?"resume_autoplay":"pause_autoplay"));if(GUILayout.Button("中止して手動操作へ戻る"))StartCoroutine(Send("stop_autoplay"));}
            else{foreach(var target in view.objectives){GUI.enabled=!busy && target.can_start;if(GUILayout.Toggle(autoObjective==target.choice,target.label))autoObjective=target.choice;}GUI.enabled=!busy;foreach(var mode in view.modes)if(GUILayout.Toggle(autoMode==mode.choice,mode.label))autoMode=mode.choice;GUILayout.BeginHorizontal();if(GUILayout.Button("日数 −"))autoDays=Math.Max(1,autoDays-1);GUILayout.Label(autoDays+"日");if(GUILayout.Button("日数 ＋"))autoDays=Math.Min(10,autoDays+1);GUILayout.EndHorizontal();GUI.enabled=!busy && view.can_start && view.objectives.Any(t=>t.choice==autoObjective && t.can_start);if(GUILayout.Button("おまかせ開始"))StartCoroutine(Send("start_autoplay"));GUI.enabled=!busy;GUILayout.Label(view.reason);}
            if(view.report!=null){GUILayout.Label(view.report.days+"日 / "+view.report.operations+"操作 / "+view.report.reason+" / "+view.report.message+"\n"+view.report.summary);foreach(var row in view.report.decisions.Reverse().Take(12))GUILayout.Label(row.day+"日目 "+row.target+"："+row.choice+" / "+row.reason);}
        }
        private void OnGUI()
        {
            if(adoptionConfirmationIdentity!=state.instance_id+"/"+state.revision)adoptionConfirmation=null;
            if (font != null) GUI.skin.font = font;
            GUI.skin.label.fontSize = 18; GUI.skin.button.fontSize = 18; GUI.skin.toggle.fontSize = 18;
            GUILayout.BeginArea(new Rect(20, 20, Screen.width - 40, Screen.height - 40));
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.Label("猫カフェ — Unity単独版・第1段階");
            GUILayout.Label(notice); GUI.enabled = !busy;
            if(showBusinessLog){DrawBusinessLog();GUI.enabled=true;GUILayout.EndScrollView();GUILayout.EndArea();return;}
            if(GUILayout.Button("営業ログを確認")){showBusinessLog=true;StartCoroutine(FetchBusinessLog("/business-log?offset="+int.MaxValue));}
            if (title)
            {
                GUILayout.Label("自由営業・段階人気目標 / 初期猫5匹 / 2席。白猫のこだわり客・特別予約に対応。静かな交流・遊び・触れ合い客と接客設備に対応。長毛専門客・VIPにも対応。人気のクリア記録・結果表示・継続営業に対応。通常派遣と途中の二択イベントに対応。通常派遣9種類と有力者3人への派遣・満足度目標に対応。個別交流と猫6匹の好感度目標にも対応。\n新規開始は単独版スロット1へ初期状態を保存します。");
                if (GUILayout.Button("人気目標で新規開始（移植範囲）")) StartCoroutine(Send("new_game",choice:"popularity"));
                if (GUILayout.Button("有力者目標で新規開始")) StartCoroutine(Send("new_game",choice:"patron"));
                if (GUILayout.Button("好感度目標で新規開始")) StartCoroutine(Send("new_game",choice:"bond"));
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
                GUILayout.Label("資金 " + state.funds + " / 売上 " + state.revenue + " / 支出 " + state.finance.total_expenses + " / 猫購入費 " + state.finance.pet_shop_expenses + " / 完了接客 " + state.completed_interactions + " / 待機 " + state.waiting_count);
                GUILayout.Label("人気 " + state.popularity + " / 本日の人気上昇 " + state.popularity_gain + " / 満足 " + state.customer_satisfaction.satisfied + " / 普通 " + state.customer_satisfaction.normal + " / 不満 " + state.customer_satisfaction.dissatisfied);
                DrawAutoplay();
                if(state.autoplay?.running==true){GUI.enabled=!busy;for(int i=1;i<=3;i++)if(GUILayout.Button("保存・一時停止 "+i))StartCoroutine(Send("save_game",slot:i));GUI.enabled=true;GUILayout.EndScrollView();GUILayout.EndArea();return;}
                if (state.game_over != null) GUILayout.Label("経営終了：" + state.game_over.reason);
                var ending=state.ending;
                if(ending!=null)
                {
                    GUILayout.Label(ending.title+"\n"+ending.details+"\n"+ending.reason);
                    GUI.enabled=!busy && ending.can_continue;
                    if(GUILayout.Button("継続営業（結果を確認）"))StartCoroutine(Send(state.objective=="bond"?"continue_bond_goal":state.objective=="patron"?"continue_patron":"continue_goal"));
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
                if(state.objective_start?.rows!=null)foreach(var row in state.objective_start.rows){GUILayout.Label(row.label+"："+row.summary+"\n"+row.details+"\n"+row.reason);GUI.enabled=!busy && row.can_start;if(GUILayout.Button("現在の営業から"+row.label+"を開始"))StartCoroutine(Send("start_objective",choice:row.choice));}
                if(state.bond_goal!=null){GUILayout.Label(state.bond_goal.details);GUI.enabled=!busy && state.bond_goal.can_continue;if(GUILayout.Button("好感度目標の結果を確認して継続"))StartCoroutine(Send("continue_bond_goal"));}
                var player=state.player_interaction;
                if(player!=null)
                {
                    GUILayout.Label(player.name+"との交流 / 残り "+player.remaining_ticks+"手 / 体力 "+player.stamina+" / 関心 "+player.engagement+" / 心の通い "+player.tension+" / 好感度 "+player.affinity_before+" → "+player.affinity_after+"\n"+player.history+" / "+player.reason);
                    foreach(var action in player.actions){GUI.enabled=!busy && action.can_select;if(GUILayout.Button(action.label))StartCoroutine(Send("player_step",choice:action.choice));}
                    foreach(var target in player.targets){GUI.enabled=!busy && target.can_select;if(GUILayout.Button("交流を"+target.label+"へ変更"))StartCoroutine(Send("player_step",choice:"switch",targetType:target.choice));}
                    GUI.enabled=!busy && player.can_finish;if(GUILayout.Button("交流を終了する"))StartCoroutine(Send("player_finish"));
                }
                GUI.enabled=!busy;
                if(state.patron_goal!=null){GUILayout.Label(state.patron_goal.details);GUI.enabled=!busy && state.patron_goal.can_continue;if(GUILayout.Button("有力者目標の結果を確認して継続"))StartCoroutine(Send("continue_patron"));}
                foreach(var option in state.patron_dispatch.options){GUILayout.Label(option.label+" / "+option.days+"日 / 報酬 "+option.reward+" / "+option.detail+" / "+option.reason);GUI.enabled=!busy && option.can_select;if(GUILayout.Button(option.label+"へ派遣"))StartCoroutine(Send("dispatch_patron",catId:option.cat_id,choice:option.choice));}
                foreach(var returned in state.patron_dispatch.events){GUILayout.Label(returned.label+" / "+returned.status+" / "+returned.detail);GUI.enabled=!busy && returned.can_select;if(GUILayout.Button(returned.label+"の帰還を確認"))StartCoroutine(Send("receive_patron",catId:returned.cat_id,choice:returned.choice));}
                GUI.enabled=!busy;
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
                if(state.goal_result!=null)GUILayout.Label(state.specialist_description);
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
                    GUI.enabled=!busy && goal.can_continue;if(GUILayout.Button("目標結果を確認して営業継続"))StartCoroutine(Send(state.objective=="bond"?"continue_bond_goal":state.objective=="patron"?"continue_patron":"continue_goal"));
                    var next=state.next_goal;if(next!=null){GUI.enabled=!busy && next.can_advance;if(GUILayout.Button("次の人気目標へ（目標 "+next.target+"）"))StartCoroutine(Send("advance_goal"));}
                }
                var assignment=state.service_assignment;
                if(assignmentInstance!=state.instance_id){assignmentCustomer=assignmentCat=assignmentSeat=null;assignmentInstance=state.instance_id;}
                if(!assignment.customers.Any(r=>r.customer_id==assignmentCustomer))assignmentCustomer=null;
                if(!assignment.cats.Any(r=>r.cat_id==assignmentCat && r.available))assignmentCat=null;
                if(!assignment.seats.Any(r=>r.seat_id==assignmentSeat && r.available))assignmentSeat=null;
                GUILayout.Label("接客の割り当て / "+assignment.reason+"\n"+assignment.notes);
                GUI.enabled=!busy && state.game_over==null;
                if(GUILayout.Button("自動割り当て："+(assignment.auto_assign?"ON":"OFF")))StartCoroutine(Send("set_auto_assignment",choice:assignment.auto_assign?"manual":"automatic"));
                if(!assignment.auto_assign)
                {
                    foreach(var row in assignment.customers){GUI.enabled=!busy;if(GUILayout.Button((assignmentCustomer==row.customer_id?"選択中 ":"")+row.name+" / "+row.details))assignmentCustomer=row.customer_id;}
                    foreach(var row in assignment.cats){GUI.enabled=!busy && row.available;if(GUILayout.Button((assignmentCat==row.cat_id?"選択中 ":"")+row.name+" / "+row.details+" / "+row.reason))assignmentCat=row.cat_id;}
                    foreach(var row in assignment.seats){GUI.enabled=!busy && row.available;if(GUILayout.Button((assignmentSeat==row.seat_id?"選択中 ":"")+row.label+" / "+row.details+" / "+row.reason))assignmentSeat=row.seat_id;}
                    var customer=assignment.customers.FirstOrDefault(r=>r.customer_id==assignmentCustomer);
                    if(customer!=null)foreach(var match in customer.matches)GUILayout.Label(match.cat_id+" / "+match.details);
                    GUI.enabled=!busy && assignment.can_assign && assignmentCustomer!=null && assignmentCat!=null && assignmentSeat!=null;
                    if(GUILayout.Button("選んだ客・猫・席で接客を開始"))StartCoroutine(Send("assign_service",customerId:assignmentCustomer,catId:assignmentCat,seatId:assignmentSeat));
                }
                GUI.enabled=true;
                var waiting=state.waiting_area;
                GUILayout.Label("待合スペース："+waiting.status+" / 待機上限 "+waiting.capacity+"人 / 猶予 "+waiting.max_wait_ticks+"tick / 維持費 "+waiting.daily_cost+" / 運営費合計 "+waiting.operating_cost+"\n"+waiting.details+"\n"+waiting.notes);
                foreach(var action in waiting.actions)
                {
                    GUILayout.Label(action.label+" / 費用 "+action.cost+" / 支払い後 "+action.funds_after+" / 待機上限 "+action.capacity_before+" → "+action.capacity_after+" / 猶予 "+action.wait_before+" → "+action.wait_after+" / 維持費 "+action.daily_before+" → "+action.daily_after+" / 運営費 "+action.operating_before+" → "+action.operating_after+" / "+action.reason);
                    GUI.enabled=!busy && action.can_execute;
                    if(GUILayout.Button(action.label))StartCoroutine(Send(action.kind));
                }
                GUI.enabled=true;
                var rest=state.rest_space;
                GUILayout.Label("休養スペース："+rest.status+" / 疲労回復＋"+rest.recovery_bonus+" / ストレス回復＋"+rest.stress_recovery_bonus+"\n"+rest.details+"\n"+rest.notes);
                foreach(var action in rest.actions)
                {
                    GUILayout.Label(action.label+" / 費用 "+action.cost+" / 支払い後 "+action.funds_after+" / 疲労回復＋"+action.recovery_before+" → ＋"+action.recovery_after+" / ストレス回復＋"+action.stress_before+" → ＋"+action.stress_after+" / "+action.reason);
                    GUI.enabled=!busy && action.can_execute;
                    if(GUILayout.Button(action.label))StartCoroutine(Send(action.kind));
                }
                GUI.enabled=true;
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
                if(state.adoption!=null)
                {
                    GUILayout.Label("譲渡イベント："+(state.adoption.enabled?"ON":"OFF")+" / "+state.adoption.reason);GUI.enabled=!busy && state.adoption.can_configure;
                    if(GUILayout.Button(state.adoption.enabled?"譲渡イベントをOFFにする":"譲渡イベントをONにする"))StartCoroutine(Send("configure_adoption",choice:state.adoption.enabled?"off":"on"));
                    foreach(var e in state.adoption.events){GUILayout.Label(e.day+"日目 / "+e.name+"への譲渡申し出 / "+e.customer_name+" / 客の好感度 "+e.guest_affinity+" / プレイヤー "+e.player_affinity+" / "+e.status+" "+e.choice);GUI.enabled=!busy && e.can_accept;if(GUILayout.Button(e.name+"を譲渡する")){adoptionConfirmation=e.event_id;adoptionConfirmationIdentity=state.instance_id+"/"+state.revision;}
                        if(adoptionConfirmation==e.event_id){GUILayout.Label(e.name+"を"+e.customer_name+"へ譲渡します。このゲームでは復帰できません。報酬はなく、記録は残ります。");if(GUILayout.Button("確認して譲渡を確定"))StartCoroutine(Send("resolve_adoption",choice:"accept",catId:e.cat_id,eventId:e.event_id));GUI.enabled=!busy;if(GUILayout.Button("譲渡の確認を取り消す"))adoptionConfirmation=null;}GUI.enabled=!busy && e.can_decline;if(GUILayout.Button("譲渡を見送る"))StartCoroutine(Send("resolve_adoption",choice:"decline",catId:e.cat_id,eventId:e.event_id));}
                    GUI.enabled=!busy;
                }
                if(state.recruitment!=null)
                {
                    GUILayout.Label("保護猫の募集 / "+state.recruitment.housing+"\n"+state.recruitment.notes+"\n"+state.recruitment.reason);GUI.enabled=!busy && state.recruitment.can_refresh;
                    if(GUILayout.Button(state.recruitment.rows.Length==0?"保護猫の募集を開始":"募集候補を追加する"))StartCoroutine(Send("open_recruitment"));
                    foreach(var row in state.recruitment.rows){GUILayout.Label(row.details+"\n"+row.reason);GUI.enabled=!busy && row.can_purchase;if(GUILayout.Button(row.accepted?row.name+"（受け入れ済み）":row.name+"を迎える（"+row.cost+"）"))StartCoroutine(Send("recruit_cat",catId:row.cat_id));}
                    GUI.enabled=!busy;
                }
                if(state.pet_shop!=null)
                {
                    GUILayout.Label("ペットショップ / "+state.pet_shop.housing+"\n"+state.pet_shop.notes);
                    foreach(var row in state.pet_shop.rows)
                    {
                        GUILayout.Label(row.details+"\n"+row.reason);GUI.enabled=!busy && row.can_purchase;
                        if(GUILayout.Button(row.accepted?row.name+"（購入済み）":row.name+"を購入する（"+row.cost+"）"))StartCoroutine(Send("purchase_cat",catId:row.cat_id));
                    }
                    GUI.enabled=!busy;
                }
                var visiting=state.visiting_cat;
                if(visiting!=null)
                {
                    GUILayout.Label("店先の猫："+visiting.name+" / "+visiting.progress+"/"+visiting.required+"回 / "+visiting.status);
                    if(visiting.status=="untriggered")GUILayout.Label("人気第1段階を達成した翌日の準備から現れます。");
                    else if(visiting.status=="accepted")GUILayout.Label(visiting.accepted_day+"日目に迎えました。出勤・個別交流・成長を猫一覧から選べます。");
                    else
                    {
                        GUILayout.Label(visiting.personality+" / "+visiting.features+" / "+visiting.trait+"\n"+visiting.housing+" / 費用 "+visiting.cost+" / 加入後の資金 "+visiting.funds_after+"\n"+visiting.history);
                        GUILayout.Label("交流は1日1回、無料です。見送りもその日の交流枠を使います。営業へ進んでも進捗は残ります。\n迎えると健康・体力全回復・休養予定、疲労・ストレス・プレイヤー好感度0で加入します。");
                        GUILayout.Label(visiting.reason+" / "+visiting.daily_reason+" / "+visiting.accept_reason);
                        GUI.enabled=!busy && visiting.can_interact;if(GUILayout.Button(visiting.name+"と店先で交流する"))StartCoroutine(Send("resolve_visiting_cat",catId:visiting.cat_id,choice:"interact"));
                        GUI.enabled=!busy && visiting.can_skip;if(GUILayout.Button("今日は店先の交流を見送る"))StartCoroutine(Send("resolve_visiting_cat",catId:visiting.cat_id,choice:"skip"));
                        GUI.enabled=!busy && visiting.can_accept;if(GUILayout.Button(visiting.name+"を迎える（費用 "+visiting.cost+"）"))StartCoroutine(Send("resolve_visiting_cat",catId:visiting.cat_id,choice:"accept"));
                    }
                    GUI.enabled=!busy;
                }
                foreach (var cat in state.cats)
                {
                    GUILayout.BeginHorizontal(); GUILayout.Label(cat.name + " / 体力 " + cat.stamina.ToString("0.0") + " / 疲労 " + cat.fatigue.ToString("0.0") + " / ストレス " + cat.stress.ToString("0.0") + " / " + cat.health_label + " / " + cat.activity_label);
                    GUI.enabled = !busy && !state.adoption.events.Any(e=>e.status=="waiting") && !(state.player_interaction?.active??false) && !(state.bond_goal?.pending??false) && !state.patron_dispatch.events.Any(e=>e.needs_response) && !(state.patron_goal?.pending??false) && !state.general_dispatch.events.Any(e=>e.needs_response) && !state.dispatch_troubles.Any(e=>e.status=="waiting") && !state.dispatch_choices.Any(e=>e.status=="waiting") && (state.reservation==null || state.reservation.status!="waiting") && state.store_event == null && state.intake_request.status != "waiting" && state.regular_introduction.status != "waiting" && !state.dispatch_introductions.Any(e=>e.status=="waiting") && !state.customer_trust.events.Any(e => e.status == "waiting") && state.growth_choices.Length == 0 && !(state.goal_result != null && state.goal_result.pending) && state.can_set_shifts && state.game_over == null && cat.health_status == "healthy" && cat.activity == "cafe";
                    bool work = GUILayout.Toggle(cat.working, "出勤");
                    if (work != cat.working) StartCoroutine(Send("set_shifts", state.cats.Where(c => c.cat_id == cat.cat_id ? work : c.working).Select(c => c.cat_id).ToArray()));
                    GUILayout.EndHorizontal(); GUILayout.Label(cat.growth_details+" / プレイヤー好感度 "+cat.affinity+" / 本日の残り交流 "+cat.remaining_sets+"セット / "+cat.player_reason);
                    GUI.enabled=!busy && cat.can_player_start;if(GUILayout.Button(cat.name+"と交流する"))StartCoroutine(Send("player_begin",catId:cat.cat_id));
                }
                GUI.enabled = !busy && !state.adoption.events.Any(e=>e.status=="waiting") && !(state.player_interaction?.active??false) && !(state.bond_goal?.pending??false) && !state.patron_dispatch.events.Any(e=>e.needs_response) && !(state.patron_goal?.pending??false) && !state.general_dispatch.events.Any(e=>e.needs_response) && !state.dispatch_troubles.Any(e=>e.status=="waiting") && !state.dispatch_choices.Any(e=>e.status=="waiting") && (state.reservation==null || state.reservation.status!="waiting") && state.game_over == null && !state.customer_trust.events.Any(e => e.status == "waiting") && state.growth_choices.Length == 0 && !(state.goal_result != null && state.goal_result.pending) && state.store_event == null && state.intake_request.status != "waiting" && state.regular_introduction.status != "waiting" && !state.dispatch_introductions.Any(e=>e.status=="waiting");
                GUI.enabled=GUI.enabled && !state.service_assignment.waiting;
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
