using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonGrowthPanel : MonoBehaviour
    {
        public GameObject panel;
        public Text title, details, notice;
        public Button[] choices;
        public Text[] choiceDetails;
        Button manual,previous,automatic,confirm;PythonCafeState state;
        public void First(){GetComponent<PythonStateClient>().RespondGrowth(0);}
        public void Second(){GetComponent<PythonStateClient>().RespondGrowth(1);}
        public void Third(){GetComponent<PythonStateClient>().RespondGrowth(2);}
        public void Fourth(){GetComponent<PythonStateClient>().RespondGrowth(3);}
        Button Add(string label,float x,float width,UnityEngine.Events.UnityAction action)
        {
            var go=DefaultControls.CreateButton(new DefaultControls.Resources());go.name=label;go.transform.SetParent(title.transform.parent,false);
            var rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-548);rect.sizeDelta=new Vector2(width,36);
            var t=go.GetComponentInChildren<Text>();t.font=title.font;t.fontSize=14;t.text=label;var b=go.GetComponent<Button>();b.onClick.AddListener(action);return b;
        }
        void Controls()
        {
            if(manual!=null)return;
            previous=Add("前の成長",24,104,()=>Move(-1));
            manual=Add("自分で選ぶ：次の成長",136,214,()=>Move(1));
            automatic=Add("猫に合う内容で自動決定",358,290,()=>GetComponent<PythonStateClient>().GrowthBatch("resolve_growth_auto"));
            confirm=Add("一覧をまとめて確定",656,260,()=>GetComponent<PythonStateClient>().GrowthBatch("resolve_growth_batch"));
        }
        void Move(int delta)
        {
            if(state?.growth_choices==null || state.growth_choices.Length==0)return;
            var c=GetComponent<PythonStateClient>();c.GrowthRowIndex=(c.GrowthRowIndex+delta+state.growth_choices.Length)%state.growth_choices.Length;Apply(state);
        }
        static string Label(PythonGrowthChoice row,string value)=>row.choices?.FirstOrDefault(c=>c.choice==value)?.label ?? "未選択";
        public void Apply(PythonCafeState value)
        {
            state=value;if(CafeBackends.Standalone && (((CafeBackends.Local as LocalCafeBackend).DispatchResultPending || (CafeBackends.Local as LocalCafeBackend).EquipmentBlocksProgress || (CafeBackends.Local as LocalCafeBackend).ReservationBlocksProgress) || (CafeBackends.Local as LocalCafeBackend).TrustBlocksProgress || (CafeBackends.Local as LocalCafeBackend).MissingBlocksProgress)){panel.SetActive(false);return;}var rows=state.growth_choices;panel.SetActive(rows!=null && rows.Length>0);if(!panel.activeSelf)return;
            Controls();var client=GetComponent<PythonStateClient>();client.GrowthRowIndex=Mathf.Clamp(client.GrowthRowIndex,0,rows.Length-1);var ev=rows[client.GrowthRowIndex];
            title.text="営業後の成長確認（"+(client.GrowthRowIndex+1)+" / "+rows.Length+"）  "+ev.name+"："+ev.title;
            details.fontSize=15;details.lineSpacing=1.05f;
            details.text="対象の猫と成長事項の一覧\n"+string.Join("\n",rows.Select((r,i)=>(i==client.GrowthRowIndex?"▶ ":"・")+r.name+" / "+r.title+" → "+Label(r,r.selected)))+"\n\n自分で選ぶ：右の候補を押します。\n前／次の成長で猫を切り替えます。\n\n"+ev.name+"の選択理由\n"+ev.decision_reason+"\n\n経験："+ev.experience+" / 必要："+ev.threshold+"\n習得済み："+ev.learned;
            for(int i=0;i<choices.Length;i++)
            {
                bool exists=ev.choices!=null && i<ev.choices.Length;choices[i].gameObject.SetActive(exists);if(!exists)continue;
                var option=ev.choices[i];choices[i].GetComponentInChildren<Text>().text=(ev.selected==option.choice?"選択中：":"自分で選ぶ：")+option.label;
            }
            PythonEventPresentation.Choices(choiceDetails,ev.choices);
            RefreshControls();
        }
        public void RefreshControls()
        {
            var c=GetComponent<PythonStateClient>();for(int i=0;i<choices.Length;i++)choices[i].interactable=c.CanRespondGrowth(i);
            if(!panel.activeSelf || automatic==null || state?.growth_choices==null)return;
            bool ready=c.CanManageSaves;automatic.interactable=ready;previous.interactable=manual.interactable=ready;
            confirm.interactable=ready && state.growth_choices.All(r=>!string.IsNullOrEmpty(r.selected));
            notice.text=PythonEventPresentation.Notice(c,"内容は確定前に変更できます。全員を選んで「一覧をまとめて確定」。\n効果は確定後から適用。追加の成長があれば次の一覧で確認します。自動進行は手動で再開してください。");
        }
        public void Disconnect(){foreach(var b in choices)b.interactable=false;if(automatic!=null)automatic.interactable=confirm.interactable=manual.interactable=previous.interactable=false;}
    }
}
