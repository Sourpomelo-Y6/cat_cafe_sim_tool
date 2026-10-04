using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace CatCafePreview
{
    [Serializable] public class PythonCatState
    {
        public string cat_id, content_cat_id, name, health_status, activity;
        public float stamina, max_stamina, fatigue, stress;
        public bool working;
    }
    [Serializable] public class PythonCafeState
    {
        public int version, day, tick;
        public string instance_id;
        public int revision;
        public bool can_set_shifts;
        public bool closed;
        public string phase, required_action;
        public int opening_ticks, waiting_count, completed_interactions;
        public float revenue;
        public PythonSeatState[] seats;
        public PythonCustomerState[] customers;
        public PythonFinanceState finance;
        public PythonIntakeState intake_request;
        public PythonHousingState housing;
        public float funds;
        public PythonCatState[] cats;
    }
    [Serializable] public class PythonHousingState
    {
        public bool configured, can_expand;
        public int count, capacity, free, stage, next_capacity;
        public string kind, reason;
        public float cost, funds_after, daily_cost, next_daily_cost, operating_cost, next_operating_cost;
    }
    [Serializable] public class PythonSeatState { public string seat_id, customer_id, customer_name, cat_id; }
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
    }
    [Serializable] public class PythonCommandReply
    {
        public string error;
        public PythonCafeState state;
    }
    [Serializable] public class PythonSavedGame { public string save_id, label, created_at; }
    [Serializable] public class PythonSaveList { public PythonSavedGame[] saves; }
    [RequireComponent(typeof(ContentPreview))]
    public class PythonStateClient : MonoBehaviour
    {
        [SerializeField] private int port = 8190;
        private PythonCafeState current;
        private PythonShiftCommand pending;
        private bool online;
        private ContentPreview preview;
        public PythonCafeState CurrentState { get { return current; } }
        public bool CanManageSaves { get { return online && pending == null && current != null; } }
        public void SaveGame() { QueueSave("save_game", ""); }
        public void LoadSaved(string id) { QueueSave("load_game", id); }
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
        private void QueueSave(string kind, string id)
        {
            if (!CanManageSaves) return;
            pending = new PythonShiftCommand { request_id=Guid.NewGuid().ToString("N"),instance_id=current.instance_id,
                expected_revision=current.revision,working_cats=new string[0],kind=kind,save_id=id };
            preview.RefreshPythonControls();
        }
        public bool CanChangeShifts { get { return online && pending == null && current != null && current.can_set_shifts; } }
        public bool CanProgressBusiness { get { return online && pending == null && current != null
            && string.IsNullOrEmpty(current.required_action)
            && (current.closed || !current.can_set_shifts || Array.Exists(current.cats, cat => cat.working)); } }
        public void ProgressBusiness()
        {
            if (!CanProgressBusiness) return;
            pending = new PythonShiftCommand { request_id = Guid.NewGuid().ToString("N"), instance_id = current.instance_id,
                expected_revision = current.revision, working_cats = new string[0],
                kind = current.closed ? "next_day" : current.can_set_shifts ? "start_business" : "advance_business" };
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
            Validate(state); current = state; online = true; preview.ApplyPythonState(state);
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
                    // Retry once with the SAME request ID if the reply was lost.
                    for (int attempt = 0; attempt < 2; attempt++)
                    {
                        using (var request = new UnityWebRequest("http://127.0.0.1:" + port + "/commands", "POST"))
                        {
                            request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(command)));
                            request.downloadHandler = new DownloadHandlerBuffer();
                            request.SetRequestHeader("Content-Type", "application/json"); request.timeout = 3;
                            yield return request.SendWebRequest();
                            if (request.result == UnityWebRequest.Result.ConnectionError && attempt == 0) continue;
                            try
                            {
                                if (request.result == UnityWebRequest.Result.Success)
                                {
                                    var reply = JsonUtility.FromJson<PythonCommandReply>(request.downloadHandler.text);
                                    Accept(reply.state); notice = command.kind == "set_shifts" ? "出勤・休養を変更しました。"
                                        : command.kind == "save_game" ? "保存しました。" : command.kind == "load_game" ? "保存したゲームを再開しました。"
                                        : command.kind == "next_day" ? "翌日の営業準備へ進みました。出勤・休養を設定してください。"
                                        : command.kind == "resolve_intake" ? (command.choice == "accept" ? "保護猫を迎えました。休養予定で猫一覧に加わりました。" : "受け入れ依頼を見送りました。")
                                        : command.kind == "purchase_housing" || command.kind == "upgrade_housing" ? "飼育スペースを拡張しました。"
                                        : reply.state.closed ? "閉店しました。結果を確認してください。" : "営業を進めました。";
                                }
                                else
                                {
                                    var reply = JsonUtility.FromJson<PythonCommandReply>(request.downloadHandler.text);
                                    notice = reply != null && !string.IsNullOrEmpty(reply.error) ? reply.error : "操作結果を確認できません。最新状態を取得します。";
                                }
                            }
                            catch (Exception) { notice = "操作結果を確認できません。最新状態を取得します。"; }
                            break;
                        }
                    }
                    pending = null;
                }
                using (var request = UnityWebRequest.Get("http://127.0.0.1:" + port + "/state"))
                {
                    request.timeout = 3;
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success) { online = false; preview.PythonConnectionError(request.error); }
                    else
                    {
                        try
                        {
                            var state = JsonUtility.FromJson<PythonCafeState>(request.downloadHandler.text);
                            Accept(state);
                            if (notice != "") preview.PythonNotice(notice);
                        }
                        catch (Exception ex) { online = false; preview.PythonConnectionError(ex.Message); }
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
                            else panel.ListUnavailable();
                        }
                        catch(Exception) { panel.ListUnavailable(); }
                    }
                }
                float until = Time.realtimeSinceStartup + 2;
                while (pending == null && Time.realtimeSinceStartup < until) yield return null;
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
