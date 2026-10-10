using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonRecruitmentOption { public string id,label; }
    [Serializable] public class PythonRecruitmentCat : PythonShopCat { public string trait_id; public string[] features; }
    [Serializable] public class PythonRecruitment { public PythonRecruitmentCat[] rows;public string housing,notes,reason;public bool can_refresh;public PythonRecruitmentOption[] traits,coats,hairs; }
    public class PythonRecruitmentPanel : MonoBehaviour
    {
        public GameObject panel,confirmation;
        public RawImage picture;
        public Text title,details,notice,confirmationText;
        public Button purchase,previous,next,housing,close,confirm,cancel;
        public Dropdown trait,coat,hair;public Toggle accepted;public Button reset;
        private bool updating;
        private string filterIdentity;
        private PythonCafeState current;
        private string instance,identity,selected,selection;
        private bool online;
        private PythonRecruitmentCat[] Rows => (current?.recruitment?.rows ?? new PythonRecruitmentCat[0]).Where(r=>(accepted.isOn || !r.accepted) && Match(trait,current?.recruitment?.traits,r.trait_id) && Feature(coat,current?.recruitment?.coats,r.features) && Feature(hair,current?.recruitment?.hairs,r.features)).OrderBy(r=>r.accepted).ToArray();
        private bool Match(Dropdown d,PythonRecruitmentOption[] opts,string key){return d.value==0 || opts!=null && d.value<=opts.Length && opts[d.value-1].id==key;}
        private bool Feature(Dropdown d,PythonRecruitmentOption[] opts,string[] keys){return d.value==0 || opts!=null && d.value<=opts.Length && (keys ?? new string[0]).Contains(opts[d.value-1].id);}
        private void Options(Dropdown d,PythonRecruitmentOption[] opts,string caption){int old=d.value;d.ClearOptions();d.AddOptions(new List<string>{caption+"：すべて"}.Concat((opts ?? new PythonRecruitmentOption[0]).Select(r=>caption+"："+r.label)).ToList());d.SetValueWithoutNotify(Math.Min(old,d.options.Count-1));}
        public void FilterValue(int value){FilterChanged();}
        public void FilterAccepted(bool value){FilterChanged();}
        public void FilterChanged(){if(updating || current==null)return;updating=true;try{Clear();if(Row==null)selected=Rows.FirstOrDefault()?.cat_id;Render();ResetScroll();}finally{updating=false;}}
        public void ResetFilters(){updating=true;trait.SetValueWithoutNotify(0);coat.SetValueWithoutNotify(0);hair.SetValueWithoutNotify(0);accepted.SetIsOnWithoutNotify(false);updating=false;FilterChanged();}
        public PythonRecruitmentCat Row => Array.Find(Rows,r=>r.cat_id==selected);
        public void Open(){if(current?.recruitment==null || !GetComponent<PythonStateClient>().CanManageSaves)return;Clear();panel.SetActive(true);panel.transform.SetAsLastSibling();Render();ResetScroll();if(current.recruitment.can_refresh)GetComponent<PythonStateClient>().OpenRecruitment();}
        public void Close(){Clear();panel.SetActive(false);}
        private void Clear(){selection=null;confirmation.SetActive(false);}
        private void ResetScroll(){details.GetComponentInParent<ScrollRect>(true).verticalNormalizedPosition=1;}
        public void Previous(){Move(-1);}public void Next(){Move(1);}
        private void Move(int delta){if(!online || selection!=null || !GetComponent<PythonStateClient>().CanManageSaves)return;int i=Array.FindIndex(Rows,r=>r.cat_id==selected)+delta;if(i<0 || i>=Rows.Length)return;selected=Rows[i].cat_id;Clear();Render();ResetScroll();}
        public void Housing(){if(!online || !GetComponent<PythonStateClient>().CanManageSaves)return;Clear();GetComponent<PythonCatAcquisitionNavigation>().OpenHousing(panel);}
        public void Purchase()
        {
            if(!online || Row==null || !GetComponent<PythonStateClient>().CanRecruitCat(selected))return;
            selection=selected;confirmation.SetActive(true);confirmationText.text=Row.name+"を受け入れして迎えますか？\n価格："+Row.cost.ToString("0.#")+" / 受け入れ後の資金："+Row.funds_after.ToString("0.#")+"\n飼育スペース："+current.recruitment.housing+"\n健康・体力全回復・休養予定で加入します。";RefreshControls();
        }
        public void Confirm(){if(!online || selection==null || !GetComponent<PythonStateClient>().CanRecruitCat(selection))return;var key=selection;Clear();GetComponent<PythonCatAcquisitionNavigation>()?.Watch(key);GetComponent<PythonStateClient>().RecruitCat(key);}
        public void Cancel(){Clear();RefreshControls();}
        public void Apply(PythonCafeState state)
        {
            if(instance!=state.instance_id){instance=state.instance_id;selected=null;Close();trait.SetValueWithoutNotify(0);coat.SetValueWithoutNotify(0);hair.SetValueWithoutNotify(0);accepted.SetIsOnWithoutNotify(false);}
            current=state;online=true;
            string filters=string.Join("/",(state.recruitment?.traits ?? new PythonRecruitmentOption[0]).Select(r=>r.id+":"+r.label));
            if(filterIdentity!=filters){filterIdentity=filters;updating=true;Options(trait,state.recruitment?.traits,"特性");Options(coat,state.recruitment?.coats,"毛色");Options(hair,state.recruitment?.hairs,"毛長");updating=false;}string key=state.instance_id+"/"+state.revision+"/"+state.day;
            if(identity!=key){identity=key;Clear();}
            if(Row==null){selected=Rows.Length==0 ? null : Rows[0].cat_id;Clear();}
            if(state.recruitment==null || state.game_over!=null)Close();Render();
        }
        private void Render()
        {
            var row=Row;int i=Array.FindIndex(Rows,r=>r.cat_id==selected);
            title.text="保護猫の紹介巡回"+(row==null ? "" : " · "+(i+1)+"/"+Rows.Length+" · "+row.name+(row.accepted ? "（受け入れ済み）" : ""));
            picture.texture=row==null ? null : GetComponent<ContentPreview>().CandidateCatTexture(row.content_cat_id);picture.enabled=picture.texture!=null;
            if(picture.texture!=null)picture.GetComponent<AspectRatioFitter>().aspectRatio=(float)picture.texture.width/picture.texture.height;
            details.text=row?.details ?? "条件に一致する紹介候補はありません。";RefreshControls();GetComponent<PythonCatAcquisitionNavigation>()?.Refresh();
        }
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();var row=Row;bool ready=online && client.CanManageSaves && selection==null;
            int i=Array.FindIndex(Rows,r=>r.cat_id==selected);previous.interactable=ready && i>0;next.interactable=ready && i>=0 && i<Rows.Length-1;
            trait.interactable=coat.interactable=hair.interactable=accepted.interactable=reset.interactable=ready;
            purchase.interactable=ready && row!=null && client.CanRecruitCat(selected);housing.interactable=ready;
            confirm.interactable=online && selection!=null && client.CanRecruitCat(selection);cancel.interactable=close.interactable=true;
            notice.text=!online ? "未接続です。再接続後に受け入れできます。" : !client.CanManageSaves ? "操作を送信しています。完了までお待ちください。" : (row?.accepted==true ? row.accepted_day+"日目に受け入れ済みです。" : row?.reason ?? current?.recruitment?.reason ?? "")+"\n"+(current?.recruitment?.notes ?? "");
        }
        public void Disconnect(){online=false;Clear();RefreshControls();}
    }
}
