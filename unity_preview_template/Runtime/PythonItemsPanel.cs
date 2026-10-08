using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonItemShop
    {public string choice,name,effect,reason;public int count;public float cost,funds_after;public bool can_select,ends_game;}
    [Serializable] public class PythonItemTarget
    {public string cat_id,name,activity,changes,reason;public bool can_select;}
    [Serializable] public class PythonOwnedItem
    {public string choice,item_id,name,effect,reason;public int count;public float price,funds_after;public bool can_sell;public PythonItemTarget[] targets;}
    [Serializable] public class PythonItems
    {public PythonItemShop[] shop;public PythonOwnedItem[] owned;public string history;public int count;}
    public class PythonItemsPanel : MonoBehaviour
    {
        public GameObject panel,confirmation;
        public Dropdown shop,owned,cats;
        public Text title,shopNote,details,notice,confirmationText;
        public Button buy,use,sell,close,confirm,cancel;
        private PythonItems current;
        private string instance,identity,selection,selectedSource,selectedCat;
        private bool online;
        private PythonItemShop Shop {get{return current?.shop!=null && shop.value<current.shop.Length ? current.shop[shop.value] : null;}}
        private PythonOwnedItem Owned {get{return current?.owned!=null && owned.value<current.owned.Length ? current.owned[owned.value] : null;}}
        private PythonItemTarget Cat {get{return Owned?.targets!=null && cats.value<Owned.targets.Length ? Owned.targets[cats.value] : null;}}
        public void Open(){if(current==null || !GetComponent<PythonStateClient>().CanManageSaves)return;Clear();panel.SetActive(true);panel.transform.SetAsLastSibling();Render();}
        public void Close(){Clear();panel.SetActive(false);}
        private void Clear(){selection=selectedSource=selectedCat=null;confirmation.SetActive(false);}
        static string N(float value){return value.ToString("0.#");}
        static void Fill(Dropdown dropdown,string[] captions,int index)
        {dropdown.options.Clear();foreach(var caption in captions)dropdown.options.Add(new Dropdown.OptionData(caption));if(captions.Length==0)dropdown.options.Add(new Dropdown.OptionData("対象なし"));dropdown.SetValueWithoutNotify(Math.Max(0,Math.Min(index,captions.Length-1)));dropdown.RefreshShownValue();}
        public void SelectShop(int _){Clear();Render();}
        public void SelectOwned(int _){Clear();FillCats(null);Render();}
        public void SelectCat(int _){Clear();Render();}
        private void FillCats(string id){var targets=Owned?.targets ?? new PythonItemTarget[0];Fill(cats,targets.Select(r=>r.name+" / "+r.activity).ToArray(),Math.Max(0,Array.FindIndex(targets,r=>r.cat_id==id)));}
        public void Buy(){Choose("purchase_item");}public void Use(){Choose("use_item");}public void Sell(){Choose("sell_item");}
        private void Choose(string kind)
        {
            string source=kind=="purchase_item" ? Shop?.choice : Owned?.choice;string catId=kind=="use_item" ? Cat?.cat_id : "";
            if(!online || !GetComponent<PythonStateClient>().CanItemAction(kind,source,catId))return;
            selection=kind;selectedSource=source;selectedCat=catId;confirmation.SetActive(true);
            confirmationText.text=kind=="purchase_item" ? Shop.name+"を1個購入しますか？\n費用："+N(Shop.cost)+" / 購入後の資金："+N(Shop.funds_after)+"\n"+Shop.effect+(Shop.ends_game ? "\n\n資金が0になり、営業が終了します。" : "")
                : kind=="sell_item" ? Owned.name+"を1個売却しますか？\n売却額："+N(Owned.price)+" / 売却後の資金："+N(Owned.funds_after)+"\n所持数："+Owned.count+" → "+(Owned.count-1)
                : Cat.name+"に"+Owned.name+"を1個使いますか？\n"+Cat.changes+"\n所持数："+Owned.count+" → "+(Owned.count-1)+"\n資金は変わりません。";
            RefreshControls();
        }
        public void Confirm(){if(!online || selection==null || !GetComponent<PythonStateClient>().CanItemAction(selection,selectedSource,selectedCat))return;string kind=selection,source=selectedSource,cat=selectedCat;Clear();GetComponent<PythonStateClient>().ItemAction(kind,source,cat);}
        public void Cancel(){Clear();RefreshControls();}
        public void Apply(PythonCafeState state)
        {
            string oldShop=Shop?.choice,oldOwned=Owned?.choice,oldCat=Cat?.cat_id;
            if(instance!=state.instance_id){instance=state.instance_id;oldShop=oldOwned=oldCat=null;Close();}
            string key=state.instance_id+"/"+state.revision;if(identity!=key){identity=key;Clear();}
            online=true;current=state.items;
            var options=current?.shop ?? new PythonItemShop[0];var holdings=current?.owned ?? new PythonOwnedItem[0];
            Fill(shop,options.Select(r=>r.name).ToArray(),Math.Max(0,Array.FindIndex(options,r=>r.choice==oldShop)));
            Fill(owned,holdings.Select(r=>r.name+" / 所持 "+r.count+"個").ToArray(),Math.Max(0,Array.FindIndex(holdings,r=>r.choice==oldOwned)));FillCats(oldCat);
            if(current==null || state.game_over!=null)panel.SetActive(false);Render();
        }
        private void Render()
        {
            title.text="用品・猫のケア";var option=Shop;var item=Owned;var cat=Cat;
            shopNote.text=option==null ? "購入できる用品はありません。" : "1個 "+N(option.cost)+" / 所持 "+option.count+"個 / "+option.effect+"\n購入後の資金："+N(option.funds_after)+(option.ends_game ? "（資金0で営業終了）" : "");
            details.text=item==null ? "所持品はありません。購入または派遣・店舗イベントの報酬で入手できます。" : item.name+" / 所持 "+item.count+"個\n"+item.effect+"\n1個の売却額："+N(item.price)+" / 売却後の資金："+N(item.funds_after)
                +(cat==null ? "" : "\n\n対象："+cat.name+" / "+cat.activity+"\n使用後："+cat.changes+"\n"+cat.reason)+"\n"+item.reason;
            details.text+="\n\n履歴\n"+(string.IsNullOrEmpty(current?.history) ? "購入・使用・売却の記録はありません。" : current.history);
            var scroll=details.GetComponentInParent<ScrollRect>();if(scroll!=null)details.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Math.Max(212,details.preferredHeight+12));RefreshControls();
        }
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();bool ready=online && client.CanManageSaves && selection==null;
            buy.interactable=ready && Shop!=null && Shop.can_select;sell.interactable=ready && Owned!=null && Owned.can_sell;use.interactable=ready && Cat!=null && Cat.can_select;
            shop.interactable=ready && (current?.shop?.Length ?? 0)>0;owned.interactable=ready && (current?.owned?.Length ?? 0)>0;cats.interactable=ready && (Owned?.targets?.Length ?? 0)>0;
            confirm.interactable=online && client.CanManageSaves && selection!=null && client.CanItemAction(selection,selectedSource,selectedCat);close.interactable=cancel.interactable=true;
            notice.text=!online ? "Python未接続です。再接続後に操作できます。" : !client.CanManageSaves ? "操作を送信しています。完了するまでお待ちください。" : !string.IsNullOrEmpty(Shop?.reason) ? Shop.reason : "購入・使用・売却は営業準備中に1個ずつ行います。選択後、確認画面で確定してください。";
        }
        public void Disconnect(){online=false;Clear();RefreshControls();}
    }
}
