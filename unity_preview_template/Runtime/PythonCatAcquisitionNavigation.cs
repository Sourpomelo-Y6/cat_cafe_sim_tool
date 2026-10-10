using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonCatAcquisitionNavigation : MonoBehaviour
    {
        public GameObject[] panels;
        public Text[] summaries;
        public Button[] detailEntries;
        public Button joinedEntry;
        private string instance,watched,joined,caption;
        private GameObject origin;
        private ScrollRect scroll;
        private float position;
        private PythonStateClient Client=>GetComponent<PythonStateClient>();
        public void Watch(string id){watched=id;}
        public void ResetNavigation(){origin=null;scroll=null;watched=joined=null;RestoreCaption();if(joinedEntry!=null)joinedEntry.gameObject.SetActive(false);}
        void RestoreCaption(){var h=GetComponent<PythonHousingPanel>();if(caption!=null && h?.close!=null)h.close.GetComponentInChildren<Text>().text=caption;caption=null;}
        public void OpenHousing(GameObject source)
        {
            if(!Client.CanManageSaves || Client.CurrentState?.housing==null || source==null || !source.activeSelf)return;
            origin=source;scroll=source.GetComponentInChildren<ScrollRect>();position=scroll==null?1:scroll.verticalNormalizedPosition;
            var h=GetComponent<PythonHousingPanel>();var menu=GetComponent<PythonMenuPanel>();
            if(menu!=null)menu.PreserveReturn(h.Open);else h.Open();
            if(!h.panel.activeSelf){origin=null;return;}
            caption=h.close.GetComponentInChildren<Text>().text;h.close.GetComponentInChildren<Text>().text="猫の候補へ戻る";
        }
        public void ReturnHousing()
        {
            var source=origin;origin=null;RestoreCaption();
            if(source==null || !source.activeSelf || Client.CurrentState?.game_over!=null)return;
            source.transform.SetAsLastSibling();Canvas.ForceUpdateCanvases();if(scroll!=null)scroll.verticalNormalizedPosition=position;scroll=null;
        }
        string Id(int i,PythonCafeState s)
        {
            switch(i){case 0:return GetComponent<PythonRecruitmentPanel>().Row?.cat_id;case 1:return GetComponent<PythonPetShopPanel>().Row?.cat_id;case 2:return s.regular_introduction?.cat_id;case 3:return GetComponent<PythonDispatchIntroductionPanel>().Row?.cat_id;case 4:return s.visiting_cat?.cat_id;default:return s.intake_request?.cat_id;}
        }
        bool Accepted(string id,PythonCafeState s)
        {
            if(string.IsNullOrEmpty(id))return false;
            return Array.Exists(s.recruitment?.rows ?? new PythonRecruitmentCat[0],r=>r.cat_id==id && r.accepted)
                || Array.Exists(s.pet_shop?.rows ?? new PythonShopCat[0],r=>r.cat_id==id && r.accepted)
                || (s.regular_introduction?.cat_id==id && s.regular_introduction.status=="accepted")
                || Array.Exists(s.dispatch_introductions ?? new PythonDispatchIntroduction[0],r=>r.cat_id==id && r.status=="accepted")
                || (s.visiting_cat?.cat_id==id && s.visiting_cat.status=="accepted")
                || (s.intake_request?.cat_id==id && s.intake_request.status=="accepted");
        }
        PythonCatState Cat(string id)=>string.IsNullOrEmpty(id)?null:Array.Find(Client.CurrentState?.cats ?? new PythonCatState[0],c=>c.cat_id==id && c.activity!="adopted");
        public void OpenDetails(int i){var s=Client.CurrentState;if(s==null)return;string id=Id(i,s);if(!Client.CanManageSaves || !Accepted(id,s) || Cat(id)==null)return;panels[i].SetActive(false);GetComponent<PythonMenuPanel>()?.ClearReturnContext();GetComponent<PythonCatDetailsPanel>().Open(id);}
        public void OpenJoined(){if(!Client.CanManageSaves || Cat(joined)==null || Client.CurrentState.game_over!=null)return;origin=null;scroll=null;RestoreCaption();foreach(var panel in panels)panel.SetActive(false);GetComponent<PythonHousingPanel>().Close();GetComponent<PythonMenuPanel>()?.ClearReturnContext();GetComponent<PythonEventListPanel>()?.Close();GetComponent<PythonCatDetailsPanel>().Open(joined);}
        public void Refresh()
        {
            var s=Client.CurrentState;if(s==null || panels==null)return;
            if(instance!=s.instance_id){ResetNavigation();instance=s.instance_id;}
            if(s.game_over!=null)ResetNavigation();
            if(origin!=null && (!origin.activeSelf || !GetComponent<PythonHousingPanel>().panel.activeSelf)){origin=null;scroll=null;RestoreCaption();}
            if(Accepted(watched,s) && Cat(watched)!=null){joined=watched;watched=null;}
            if(joinedEntry!=null){var c=Cat(joined);joinedEntry.gameObject.SetActive(c!=null);joinedEntry.interactable=Client.CanManageSaves;joinedEntry.GetComponentInChildren<Text>().text=c==null?"迎えた猫の詳細":"迎えた猫："+c.name+" の詳細";}
            for(int i=0;i<panels.Length;i++)
            {
                float cost=0,after=s.funds;string status="候補";bool done=Accepted(Id(i,s),s);
                switch(i)
                {
                    case 0:var r=GetComponent<PythonRecruitmentPanel>().Row;cost=r?.cost??0;after=r?.funds_after??s.funds;break;
                    case 1:var p=GetComponent<PythonPetShopPanel>().Row;cost=p?.cost??0;after=p?.funds_after??s.funds;break;
                    case 2:var a=s.regular_introduction;cost=a?.cost??0;after=a?.funds_after??s.funds;status=a?.status;break;
                    case 3:var d=GetComponent<PythonDispatchIntroductionPanel>().Row;cost=d?.cost??0;after=d?.funds_after??s.funds;status=d?.status;break;
                    case 4:var v=s.visiting_cat;cost=v?.cost??0;after=v?.funds_after??s.funds;break;
                    default:var q=s.intake_request;cost=q?.cost??0;after=s.funds-cost;break;
                }
                var h=s.housing;string capacity=h==null?"飼育枠：情報なし":h.configured?"飼育枠："+h.count+" / "+h.capacity+"匹（空き "+h.free+"匹）":"飼育枠：上限なし";
                if(summaries[i]!=null)summaries[i].text=(string.IsNullOrEmpty(Id(i,s))?"候補がありません":done?"受け入れ済み":status=="declined"?"見送り済み":"初期費用："+cost.ToString("0.#")+" / 資金："+s.funds.ToString("0.#")+" → "+after.ToString("0.#"))+"\n"+capacity;
                if(detailEntries[i]!=null){detailEntries[i].gameObject.SetActive(done);detailEntries[i].interactable=Client.CanManageSaves && Cat(Id(i,s))!=null;}
            }
        }
    }
}
