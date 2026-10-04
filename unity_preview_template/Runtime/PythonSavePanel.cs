using System;
using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview
{
    public class PythonSavePanel : MonoBehaviour
    {
        public Dropdown saves;
        public Button resume;
        private PythonSavedGame[] entries = new PythonSavedGame[0];
        public int Count { get { return entries.Length; } }
        public void Apply(PythonSaveList list)
        {
            if(list == null || list.saves == null) throw new ArgumentException("保存一覧の形式が不正です。");
            string previous = entries.Length == 0 ? "" : entries[Math.Min(saves.value,entries.Length-1)].save_id;
            entries = list.saves;
            saves.options.Clear();
            foreach(var entry in entries) saves.options.Add(new Dropdown.OptionData(entry.label));
            if(entries.Length == 0) saves.options.Add(new Dropdown.OptionData("保存したゲームはありません"));
            saves.SetValueWithoutNotify(Math.Max(0,Array.FindIndex(entries,entry=>entry.save_id==previous)));
            saves.RefreshShownValue(); RefreshControls();
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
        }
        public void Disconnect() { saves.interactable = resume.interactable = false; }
        public void ListUnavailable()
        {
            entries = new PythonSavedGame[0]; saves.options.Clear(); saves.options.Add(new Dropdown.OptionData("保存一覧を取得できません"));
            saves.RefreshShownValue(); Disconnect();
        }
    }
}
