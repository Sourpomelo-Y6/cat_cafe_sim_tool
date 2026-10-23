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
        private PythonCafeState current; private bool notification;
        public void OpenNotice(){if(string.IsNullOrEmpty(current?.store_event_notice))return;notification=true;panel.SetActive(true);panel.transform.SetAsLastSibling();RenderNotice();}
        private void RenderNotice(){title.text="本日の店舗イベント";details.text=current.store_event_notice;for(int i=0;i<choices.Length;i++){choices[i].gameObject.SetActive(i==1 || (i==0 && current.store_event_notice_kind=="supplies"));if(i<2)choices[i].GetComponentInChildren<Text>().text=i==0?"用品・所持品を見る":"閉じる";if(choiceDetails!=null && i<choiceDetails.Length && choiceDetails[i]!=null)choiceDetails[i].text="";}notice.text="閲覧のみ。営業はそのまま進行します。";}
        public EquipmentConversationPanel Dialogue=>GetComponent<EquipmentConversationPanel>()??gameObject.AddComponent<EquipmentConversationPanel>();
        public void First() { if(notification){panel.SetActive(false);notification=false;GetComponent<PythonItemsPanel>()?.Open();return;}GetComponent<PythonStateClient>().RespondStore(0); }
        public void Second() { if(notification){panel.SetActive(false);notification=false;return;}GetComponent<PythonStateClient>().RespondStore(1); }
        public void Third() { GetComponent<PythonStateClient>().RespondStore(2); }
        public void Apply(PythonCafeState state)
        {
            bool same=current!=null && current.instance_id==state.instance_id && current.day==state.day && current.store_event_notice_kind==state.store_event_notice_kind;
            current=state;
            if(notification && !(CafeBackends.Standalone && (CafeBackends.Local as LocalCafeBackend).EquipmentResultPending) && same && !string.IsNullOrEmpty(state.store_event_notice)){RenderNotice();RefreshControls();return;}
            notification=false;
            var ev=state.store_event;
            if(Application.isPlaying && CafeBackends.Standalone && (CatCafePreview.Standalone.CafeEquipmentDialogue.Supports(ev?.kind) || (CafeBackends.Local as LocalCafeBackend).EquipmentResultPending)){panel.SetActive(false);Dialogue.Apply(state,this);return;}
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
        { if(notification){foreach(var b in choices)b.interactable=true;return;}var client=GetComponent<PythonStateClient>();for(int i=0;i<choices.Length;i++) choices[i].interactable=client.CanRespondStore(i);if(panel.activeSelf)notice.text=PythonEventPresentation.Notice(client,prompt); }
        public void Disconnect()
        {
            foreach(var button in choices) button.interactable=false;
            if(panel.activeSelf) notice.text="Python未接続です。再接続後に回答できます。";
        }
    }
}
