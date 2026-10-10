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
        private IEnumerator Send(string kind, string[] working = null, int slot = 1, string catId = "")
        {
            if (busy) yield break;
            busy = true;
            var cmd = new PythonShiftCommand { request_id = Guid.NewGuid().ToString("N"), instance_id = state.instance_id, expected_revision = state.revision, kind = kind, choice = "free", working_cats = working ?? new string[0], save_id = slot.ToString(), save_slot = slot, cat_id = catId };
            yield return backend.Request("/commands", JsonUtility.ToJson(cmd), reply =>
            {
                var result = JsonUtility.FromJson<PythonCommandReply>(reply.text);
                state = backend.View(); notice = string.IsNullOrEmpty(result.error) ? "操作を反映しました。" : result.error;
                if (string.IsNullOrEmpty(result.error) && (kind == "new_game" || kind == "load_game")) title = false;
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
                GUILayout.Label("自由営業 / 初期猫5匹 / 2席。目標・設備・用品・加入・派遣・顧客イベントは後続移植。\n新規開始は単独版スロット1へ初期状態を保存します。");
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
                if (state.game_over != null) GUILayout.Label("経営終了：" + state.game_over.reason);
                foreach (var cat in state.cats)
                {
                    GUILayout.BeginHorizontal(); GUILayout.Label(cat.name + " / 体力 " + cat.stamina.ToString("0.0") + " / 疲労 " + cat.fatigue.ToString("0.0") + " / ストレス " + cat.stress.ToString("0.0") + " / " + cat.health_label + " / " + cat.activity_label);
                    GUI.enabled = !busy && state.can_set_shifts && state.game_over == null && cat.health_status == "healthy" && cat.activity == "cafe";
                    bool work = GUILayout.Toggle(cat.working, "出勤");
                    if (work != cat.working) StartCoroutine(Send("set_shifts", state.cats.Where(c => c.cat_id == cat.cat_id ? work : c.working).Select(c => c.cat_id).ToArray()));
                    GUILayout.EndHorizontal();
                }
                GUI.enabled = !busy && state.game_over == null;
                if (!state.can_set_shifts || state.cats.Any(c => c.working))
                    if (GUILayout.Button(state.closed ? "翌日へ" : state.can_set_shifts ? "営業開始" : "営業を1tick進める")) StartCoroutine(Send(state.closed ? "next_day" : state.can_set_shifts ? "start_business" : "advance_business"));
                if (state.can_set_shifts && GUILayout.Button("休業して翌日へ")) StartCoroutine(Send("day_off"));
                foreach (var cat in backend.Simulation.State.cats.Where(c => c.activity == "missing" && c.missing_remaining == 0))
                    if (GUILayout.Button("帰還確認：" + cat.id + (cat.kitten ? "（引き渡し費用あり）" : ""))) StartCoroutine(Send("resolve_missing", catId: cat.id));
                foreach (var seat in state.seats) GUILayout.Label(seat.seat_id + "：" + (seat.customer_id == null ? "空席" : seat.customer_id + " / " + seat.cat_id));
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
