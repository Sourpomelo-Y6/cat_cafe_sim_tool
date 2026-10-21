using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonStoreEventPanel : MonoBehaviour
    {
        public GameObject panel;
        public Text title, details, notice;
        public Button[] choices;
        public Text[] choiceDetails;
        private string key, prompt;
        public EquipmentConversationPanel Dialogue=>GetComponent<EquipmentConversationPanel>()??gameObject.AddComponent<EquipmentConversationPanel>();
        public void First() { GetComponent<PythonStateClient>().RespondStore(0); }
        public void Second() { GetComponent<PythonStateClient>().RespondStore(1); }
        public void Third() { GetComponent<PythonStateClient>().RespondStore(2); }
        public void Apply(PythonCafeState state)
        {
            var ev=state.store_event;
            if(Application.isPlaying && CafeBackends.Standalone && (ev?.kind=="trouble" || (CafeBackends.Local as LocalCafeBackend).EquipmentResultPending)){panel.SetActive(false);Dialogue.Apply(state,this);return;}
            panel.SetActive(ev != null && !string.IsNullOrEmpty(ev.kind));
            if(!panel.activeSelf) return;
            title.text=ev.title;
            details.text=ev.day+"日目 / 所持金："+ev.funds+"\n\n";
            if(PythonEventPresentation.HasDescriptions(choiceDetails))details.text+=(ev.kind=="trouble" ? "設備にトラブルが起きています。修理・応急処置・休業の対応を選んでください。" : "保護団体から支援の依頼が届いています。支援する金額や見送りを選んでください。")+"\n\n対応を決めるまで営業を進められません。";
            for(int i=0;i<choices.Length;i++)
            {
                bool exists=ev.choices != null && i<ev.choices.Length;
                choices[i].gameObject.SetActive(exists);
                if(!exists) continue;
                var option=ev.choices[i];
                choices[i].GetComponentInChildren<Text>().text=option.label;
                if(!PythonEventPresentation.HasDescriptions(choiceDetails))details.text+=option.label+"\n"+option.detail+"\n"+(option.can_select ? "" : option.reason+"\n")+"\n";
            }
            PythonEventPresentation.Choices(choiceDetails,ev.choices);
            string nextKey=state.instance_id+"/"+ev.day+"/"+ev.kind;if(key!=nextKey){key=nextKey;PythonEventPresentation.Reset(details,choiceDetails);}
            prompt="対応を1つ選んでください。回答後、営業開始・自動進行は手動で選びます。";
            RefreshControls();
        }
        public void RefreshControls()
        { var client=GetComponent<PythonStateClient>();for(int i=0;i<choices.Length;i++) choices[i].interactable=client.CanRespondStore(i);if(panel.activeSelf)notice.text=PythonEventPresentation.Notice(client,prompt); }
        public void Disconnect()
        {
            foreach(var button in choices) button.interactable=false;
            if(panel.activeSelf) notice.text="Python未接続です。再接続後に回答できます。";
        }
    }
}
