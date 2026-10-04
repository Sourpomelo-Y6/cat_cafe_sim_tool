using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonStoreEventPanel : MonoBehaviour
    {
        public GameObject panel;
        public Text title, details, notice;
        public Button[] choices;
        public void First() { GetComponent<PythonStateClient>().RespondStore(0); }
        public void Second() { GetComponent<PythonStateClient>().RespondStore(1); }
        public void Third() { GetComponent<PythonStateClient>().RespondStore(2); }
        public void Apply(PythonCafeState state)
        {
            var ev=state.store_event;
            panel.SetActive(ev != null && !string.IsNullOrEmpty(ev.kind));
            if(!panel.activeSelf) return;
            title.text=ev.title;
            details.text=ev.day+"日目 / 所持金："+ev.funds+"\n\n";
            for(int i=0;i<choices.Length;i++)
            {
                bool exists=ev.choices != null && i<ev.choices.Length;
                choices[i].gameObject.SetActive(exists);
                if(!exists) continue;
                var option=ev.choices[i];
                choices[i].GetComponentInChildren<Text>().text=option.label;
                details.text+=option.label+"\n"+option.detail+"\n"+(option.can_select ? "" : option.reason+"\n")+"\n";
            }
            notice.text="対応を選んでください。回答後、営業開始・自動進行は手動で選びます。";
            RefreshControls();
        }
        public void RefreshControls()
        { for(int i=0;i<choices.Length;i++) choices[i].interactable=GetComponent<PythonStateClient>().CanRespondStore(i); }
        public void Disconnect()
        {
            foreach(var button in choices) button.interactable=false;
            if(panel.activeSelf) notice.text="Python未接続です。再接続後に回答できます。";
        }
    }
}
