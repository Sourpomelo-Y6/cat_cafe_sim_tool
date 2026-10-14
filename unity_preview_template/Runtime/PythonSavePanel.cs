using System;
using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview
{
    public class PythonSavePanel : MonoBehaviour
    {
        public Dropdown saves;
        public Button resume;
        public GameObject panel;
        public Text details, notice;
        public Button save;
        public void Open() { panel.SetActive(true);panel.transform.SetAsLastSibling();SelectSave(saves.value);RefreshControls(); }
        public void Close() { panel.SetActive(false); }
        public void SelectSave(int index) { if(details!=null)details.text=index>=0 && index<entries.Length ? entries[index].Details() : "保存したゲームはありません。"; }
        public void Notify(string message) { GetComponent<PythonSaveBrowser>()?.Notify(message); if(notice!=null && !string.IsNullOrEmpty(message))notice.text=message; }
        private PythonSavedGame[] entries = new PythonSavedGame[0];
        public int Count { get { return entries.Length; } }
        public void Apply(PythonSaveList list)
        {
            if(list == null || list.saves == null) throw new ArgumentException("保存一覧の形式が不正です。");
            string previous = entries.Length == 0 ? "" : entries[Math.Max(0,Math.Min(saves.value,entries.Length-1))].save_id;
            entries = list.saves;
            saves.options.Clear();
            foreach(var entry in entries) saves.options.Add(new Dropdown.OptionData(entry.label));
            if(entries.Length == 0) saves.options.Add(new Dropdown.OptionData("保存したゲームはありません"));
            saves.SetValueWithoutNotify(Math.Max(0,Array.FindIndex(entries,entry=>entry.save_id==previous)));
            saves.RefreshShownValue(); SelectSave(saves.value);RefreshControls();GetComponent<PythonSaveBrowser>()?.Apply(list);
        }
        public void ResumeSelected()
        {
            if(entries.Length > 0 && GetComponent<PythonStateClient>().CanManageSaves)
                GetComponent<PythonStateClient>().LoadSaved(entries[saves.value].save_id);
        }
        public void RefreshControls()
        {
            bool ready = entries.Length > 0 && GetComponent<PythonStateClient>().CanManageSaves;
            saves.interactable = resume.interactable = ready;
            if(save!=null)save.interactable=GetComponent<PythonStateClient>().CanSaveNow;
        }
        public void Disconnect() { saves.interactable = resume.interactable = false;if(save!=null)save.interactable=false; }
        public void ListUnavailable()
        {
            GetComponent<PythonSaveBrowser>()?.Unavailable();
            entries = new PythonSavedGame[0]; saves.options.Clear(); saves.options.Add(new Dropdown.OptionData("保存一覧を取得できません"));
            saves.RefreshShownValue();if(details!=null)details.text="接続を確認して、保存一覧の取得をお待ちください。"; Disconnect();
        }
    }
}
