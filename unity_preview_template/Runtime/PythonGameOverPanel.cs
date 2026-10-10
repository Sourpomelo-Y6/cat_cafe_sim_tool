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
        public Text details, notice,summary;
        public ScrollRect scroll;
        public Button save, title;
        private bool disconnected;
        private string instance;
        public void Apply(PythonCafeState state)
        {
            if(instance!=state.instance_id){instance=state.instance_id;disconnected=false;scroll.verticalNormalizedPosition=1;}panel.SetActive(state.game_over!=null);
            if(state.game_over==null)return;
            var browser=GetComponent<PythonSaveBrowser>();panel.transform.SetAsLastSibling();if(browser?.panel.activeSelf==true)browser.panel.transform.SetAsLastSibling();
            if(disconnected){notice.text="結果を残す場合は、タイトルへ戻る前に保存してください。";disconnected=false;}
            var end=state.game_over;if(summary!=null)summary.text="経営終了："+(end.reason=="funds"?"資金不足":end.reason=="popularity"?"人気低下":end.reason)+" / "+end.day+"日目\n資金 "+state.funds.ToString("0.#")+" / 人気 "+end.popularity.ToString("0.#")+" / 在籍 "+Array.FindAll(state.cats ?? new PythonCatState[0],c=>c.activity!="adopted").Length+"匹";
            var text=new StringBuilder("終了理由："+(end.reason=="funds" ? "資金不足" : "人気低下"));
            text.Append("\n終了日："+end.day+"日目 / 資金："+state.funds.ToString("0.#")+" / 人気："+end.popularity.ToString("0.#"));

            text.Append("\n目標の進捗\n"+(state.objective_progress?.summary ?? "記録はありません。")+"\n\n猫たちの状態\n");
            foreach(var cat in state.cats ?? new PythonCatState[0])
                text.Append(cat.name+" / "+cat.health_label+" / "+cat.activity_label+"\n体力："+cat.stamina.ToString("0.#")+" / 疲労："+cat.fatigue.ToString("0.#")+" / ストレス："+cat.stress.ToString("0.#")+"\n\n");
            if(details.text!=text.ToString()){details.text=text.ToString();Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=1;}
            RefreshControls();
        }
        public void RefreshControls()
        { var client=GetComponent<PythonStateClient>();save.interactable=client.CanManageSaves;title.interactable=client.CanLeaveResult;if(!disconnected)notice.text=!client.CanManageSaves?"操作の完了、または再接続を待っています。":"結果はセーブ画面で保存し、「つづきから」で見返せます。\n新しいカフェを始める場合は、タイトルの「はじめから」を選んでください。"; }
        public void Save() { if(!save.interactable)return;var browser=GetComponent<PythonSaveBrowser>();if(browser!=null)browser.OpenSave();else GetComponent<PythonStateClient>().SaveGame(); }
        public void Return() { if(GetComponent<PythonStateClient>().CanLeaveResult)SceneManager.LoadScene("CatCafeTitle"); }
        public void Disconnect() { disconnected=true;save.interactable=false;title.interactable=GetComponent<PythonStateClient>().CanLeaveResult;notice.text="Python未接続です。結果を保存するには再接続してください。"; }
    }
}
