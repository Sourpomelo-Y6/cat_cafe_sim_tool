using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonNewGamePanel : MonoBehaviour
    {
        public GameObject panel;
        public Dropdown objectives;
        public Text details, notice;
        public Button open, start;
        private PythonNewGameOption[] options=new PythonNewGameOption[0];
        public void Open() { panel.SetActive(true); RefreshControls(); }
        public void Close() { panel.SetActive(false); }
        public void Select(int index)
        {
            if(index<0 || index>=options.Length) return;
            details.text=options[index].label+"\n\n"+options[index].detail+"\n\n開始条件\n"+options[index].conditions;
        }
        public void StartGame()
        {
            if(start.interactable && options.Length>0) GetComponent<PythonStateClient>().NewGame(options[objectives.value].choice);
        }
        public void Apply(PythonCafeState state)
        {
            string previous=options.Length>0 ? options[Math.Min(objectives.value,options.Length-1)].choice : state.objective;
            options=state.new_game_options ?? new PythonNewGameOption[0];objectives.options.Clear();
            foreach(var row in options)objectives.options.Add(new Dropdown.OptionData(row.label));
            objectives.SetValueWithoutNotify(Math.Max(0,Array.FindIndex(options,row=>row.choice==previous)));objectives.RefreshShownValue();
            Select(objectives.value);
            RefreshControls();
        }
        public void RefreshControls()
        {
            bool ready=GetComponent<PythonStateClient>().CanManageSaves;
            open.interactable=ready;objectives.interactable=start.interactable=ready && options.Length>0;
            notice.text=ready ? "新規ゲームは別の保存データとして作成します。現在の進捗は先に上部の「保存」で残してください。\n再開する場合は上部の保存一覧と「保存から再開」を使います。" : "Python未接続、または操作を送信中です。";
        }
        public void Disconnect() {open.interactable=objectives.interactable=start.interactable=false;notice.text="Python未接続です。状態サーバーを起動してください。";}
    }
}
