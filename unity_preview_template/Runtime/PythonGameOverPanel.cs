using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
namespace CatCafePreview
{
    [Serializable] public class PythonGameOver { public string reason; public int day; public float popularity; }
    public class PythonGameOverPanel : MonoBehaviour
    {
        public GameObject panel;
        public Text details, notice;
        public ScrollRect scroll;
        public Button save, title;
        private bool disconnected;
        public void Apply(PythonCafeState state)
        {
            panel.SetActive(state.game_over!=null);
            if(state.game_over==null)return;
            panel.transform.SetAsLastSibling();
            if(disconnected){notice.text="結果を残す場合は、タイトルへ戻る前に保存してください。";disconnected=false;}
            var end=state.game_over;
            var text=new StringBuilder("終了理由："+(end.reason=="funds" ? "資金不足" : "人気低下"));
            text.Append("\n終了日："+end.day+"日目 / 資金："+state.funds.ToString("0.#")+" / 人気："+end.popularity.ToString("0.#"));
            text.Append("\n\n猫たちの状態\n");
            foreach(var cat in state.cats)
                text.Append(cat.name+" / "+cat.health_label+" / "+cat.activity_label+"\n体力："+cat.stamina.ToString("0.#")+" / 疲労："+cat.fatigue.ToString("0.#")+" / ストレス："+cat.stress.ToString("0.#")+"\n\n");
            if(details.text!=text.ToString()){details.text=text.ToString();Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=1;}
            RefreshControls();
        }
        public void RefreshControls()
        { var client=GetComponent<PythonStateClient>();save.interactable=client.CanManageSaves;title.interactable=client.CanLeaveResult; }
        public void Save() { GetComponent<PythonStateClient>().SaveGame(); }
        public void Return() { if(GetComponent<PythonStateClient>().CanLeaveResult)SceneManager.LoadScene("CatCafeTitle"); }
        public void Disconnect() { disconnected=true;save.interactable=false;title.interactable=GetComponent<PythonStateClient>().CanLeaveResult;notice.text="Python未接続です。結果を保存するには再接続してください。"; }
    }
}
