using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonShopCat { public string cat_id,content_cat_id,name,reason,details; public float cost,funds_after; public bool accepted,can_purchase; public int accepted_day; }
    [Serializable] public class PythonPetShop { public PythonShopCat[] rows; public string housing,notes; }
    public class PythonPetShopPanel : MonoBehaviour
    {
        public GameObject panel,confirmation;
        public RawImage picture;
        public Text title,details,notice,confirmationText;
        public Button purchase,previous,next,housing,close,confirm,cancel;
        private PythonCafeState current;
        private string instance,identity,selected,selection;
        private bool online;
        private PythonShopCat[] Rows => current?.pet_shop?.rows ?? new PythonShopCat[0];
        private PythonShopCat Row => Array.Find(Rows,r=>r.cat_id==selected);
        public void Open(){if(current?.pet_shop==null || !GetComponent<PythonStateClient>().CanManageSaves)return;Clear();panel.SetActive(true);panel.transform.SetAsLastSibling();Render();ResetScroll();}
        public void Close(){Clear();panel.SetActive(false);}
        private void Clear(){selection=null;confirmation.SetActive(false);}
        private void ResetScroll(){Canvas.ForceUpdateCanvases();details.GetComponentInParent<ScrollRect>().verticalNormalizedPosition=1;}
        public void Previous(){Move(-1);}public void Next(){Move(1);}
        private void Move(int delta){if(!online || selection!=null || !GetComponent<PythonStateClient>().CanManageSaves)return;int i=Array.FindIndex(Rows,r=>r.cat_id==selected)+delta;if(i<0 || i>=Rows.Length)return;selected=Rows[i].cat_id;Clear();Render();ResetScroll();}
        public void Housing(){if(!online || !GetComponent<PythonStateClient>().CanManageSaves)return;Clear();GetComponent<PythonHousingPanel>().Open();}
        public void Purchase()
        {
            if(!online || Row==null || !GetComponent<PythonStateClient>().CanPurchaseCat(selected))return;
            selection=selected;confirmation.SetActive(true);confirmationText.text=Row.name+"を購入して迎えますか？\n価格："+Row.cost.ToString("0.#")+" / 購入後の資金："+Row.funds_after.ToString("0.#")+"\n飼育スペース："+current.pet_shop.housing+"\n健康・体力全回復・休養予定で加入します。";RefreshControls();
        }
        public void Confirm(){if(!online || selection==null || !GetComponent<PythonStateClient>().CanPurchaseCat(selection))return;var key=selection;Clear();GetComponent<PythonStateClient>().PurchaseCat(key);}
        public void Cancel(){Clear();RefreshControls();}
        public void Apply(PythonCafeState state)
        {
            if(instance!=state.instance_id){instance=state.instance_id;selected=null;Close();}
            current=state;online=true;string key=state.instance_id+"/"+state.revision+"/"+state.day;
            if(identity!=key){identity=key;Clear();}
            if(Row==null){selected=Rows.Length==0 ? null : Rows[0].cat_id;Clear();}
            if(state.pet_shop==null || state.game_over!=null)Close();Render();
        }
        private void Render()
        {
            var row=Row;int i=Array.FindIndex(Rows,r=>r.cat_id==selected);
            title.text="ペットショップ"+(row==null ? "" : " · "+(i+1)+"/"+Rows.Length+" · "+row.name+(row.accepted ? "（購入済み）" : ""));
            picture.texture=row==null ? null : GetComponent<ContentPreview>().CandidateCatTexture(row.content_cat_id);picture.enabled=picture.texture!=null;
            if(picture.texture!=null)picture.GetComponent<AspectRatioFitter>().aspectRatio=(float)picture.texture.width/picture.texture.height;
            details.text=row?.details ?? "購入候補の記録はありません。";RefreshControls();
        }
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();var row=Row;bool ready=online && client.CanManageSaves && selection==null;
            int i=Array.FindIndex(Rows,r=>r.cat_id==selected);previous.interactable=ready && i>0;next.interactable=ready && i>=0 && i<Rows.Length-1;
            purchase.interactable=ready && row!=null && client.CanPurchaseCat(selected);housing.interactable=ready;
            confirm.interactable=online && selection!=null && client.CanPurchaseCat(selection);cancel.interactable=close.interactable=true;
            notice.text=!online ? "未接続です。再接続後に購入できます。" : !client.CanManageSaves ? "操作を送信しています。完了までお待ちください。" : (row?.accepted==true ? row.accepted_day+"日目に購入済みです。" : row?.reason ?? "")+"\n"+(current?.pet_shop?.notes ?? "");
        }
        public void Disconnect(){online=false;Clear();RefreshControls();}
    }
}
