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
        public Button[] modeTabs;
        public Text sectionLabel;
        private int mode;
        private bool returnToCat;
        private string preferredCat;
        private PythonCafeState cafe;
        private PythonItems current;
        private string instance,identity,selection,selectedSource,selectedCat;
        private bool online;
        private PythonItemShop Shop {get{return current?.shop!=null && shop.value<current.shop.Length ? current.shop[shop.value] : null;}}
        private PythonOwnedItem Owned {get{return current?.owned!=null && owned.value<current.owned.Length ? current.owned[owned.value] : null;}}
        private PythonItemTarget Cat {get{return Owned?.targets!=null && cats.value<Owned.targets.Length ? Owned.targets[cats.value] : null;}}
        public void Open(){OpenForCat(null);}
        public void OpenForCat(string catId){if(current==null || !GetComponent<PythonStateClient>().CanManageSaves)return;Clear();returnToCat=!string.IsNullOrEmpty(catId);preferredCat=catId;mode=0;FillCats(catId);panel.SetActive(true);panel.transform.SetAsLastSibling();Render();ResetScroll();}
        public void ShowHistory(){SetMode(3);}public void ShowCare(){SetMode(0);}public void ShowPurchase(){SetMode(1);}public void ShowSale(){SetMode(2);}
        private void SetMode(int value){if(!online || !GetComponent<PythonStateClient>().CanManageSaves)return;Clear();mode=value;Render();ResetScroll();}
        private void ResetScroll(){Canvas.ForceUpdateCanvases();var scroll=details.GetComponentInParent<ScrollRect>();if(scroll!=null)scroll.verticalNormalizedPosition=1;}
        public void Close(){Clear();panel.SetActive(false);if(returnToCat){var detail=GetComponent<PythonCatDetailsPanel>();if(detail!=null && detail.panel.activeSelf)detail.panel.transform.SetAsLastSibling();}returnToCat=false;preferredCat=null;}
        private void Clear(){selection=selectedSource=selectedCat=null;confirmation.SetActive(false);}
        static string N(float value){return value.ToString("0.#");}
        static void Fill(Dropdown dropdown,string[] captions,int index)
        {dropdown.options.Clear();foreach(var caption in captions)dropdown.options.Add(new Dropdown.OptionData(caption));if(captions.Length==0)dropdown.options.Add(new Dropdown.OptionData("対象なし"));dropdown.SetValueWithoutNotify(Math.Max(0,Math.Min(index,captions.Length-1)));dropdown.RefreshShownValue();}
        public void SelectShop(int _){Clear();Render();ResetScroll();}
        public void SelectOwned(int _){Clear();FillCats(preferredCat);Render();ResetScroll();}
        public void SelectCat(int _){Clear();preferredCat=Cat?.cat_id;Render();ResetScroll();}
        private void FillCats(string id){var targets=Owned?.targets ?? new PythonItemTarget[0];Fill(cats,targets.Select(r=>r.name+" / "+r.activity).ToArray(),Math.Max(0,Array.FindIndex(targets,r=>r.cat_id==id)));preferredCat=Cat?.cat_id;}
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
            online=true;cafe=state;current=state.items;
            var options=current?.shop ?? new PythonItemShop[0];var holdings=current?.owned ?? new PythonOwnedItem[0];
            Fill(shop,options.Select(r=>r.name).ToArray(),Math.Max(0,Array.FindIndex(options,r=>r.choice==oldShop)));
            Fill(owned,holdings.Select(r=>r.name+" / 所持 "+r.count+"個").ToArray(),Math.Max(0,Array.FindIndex(holdings,r=>r.choice==oldOwned)));FillCats(oldCat);
            if(current==null || state.game_over!=null)panel.SetActive(false);Render();
        }
        private void Render()
        {
            title.text="用品・猫のケア";var option=Shop;var item=Owned;var cat=Cat;
            bool organized=modeTabs!=null && modeTabs.Length>=3;
            if(organized){shop.gameObject.SetActive(mode==1);shopNote.gameObject.SetActive(mode==1);owned.gameObject.SetActive(mode==0 || mode==2);cats.gameObject.SetActive(mode==0);buy.gameObject.SetActive(mode==1);use.gameObject.SetActive(mode==0);sell.gameObject.SetActive(mode==2);sectionLabel.text=mode==0?"所持品と対象の猫を選び、効果を確認してください。":mode==1?"購入する用品を選び、価格と効果を確認してください。":mode==2?"売却する所持品を選び、所持数と売却額を確認してください。":"購入・使用・売却の記録を確認できます。";sectionLabel.text="資金 "+N(cafe?.funds ?? 0)+" / 所持 "+(current?.count ?? 0)+"個　"+sectionLabel.text;for(int i=0;i<modeTabs.Length;i++)modeTabs[i].GetComponent<Image>().color=i==mode?new Color(.8f,.9f,.76f):Color.white;}
            close.GetComponentInChildren<Text>().text=returnToCat?"猫の詳細へ戻る":"閉じる";
            shopNote.text=option==null?"購入できる用品はありません。":"価格：1個 "+N(option.cost)+" / 所持："+option.count+"個";
            if(mode==3 && organized)details.text="購入・使用・売却の履歴\n"+(string.IsNullOrEmpty(current?.history)?"記録はありません。":current.history);
            else if(mode==1 && organized)details.text=option==null?"購入できる用品はありません。":option.name+"\n効果："+option.effect+"\n購入後の資金："+N(option.funds_after)+(option.ends_game?"（資金0で営業終了）":"")+"\n"+(option.can_select?"購入できます。":option.reason);
            else if(mode==2 && organized)details.text=item==null?"売却できる所持品はありません。":item.name+" / 所持 "+item.count+"個\n1個の売却額："+N(item.price)+"\n売却後の資金："+N(item.funds_after)+"\n売却後の所持数："+(item.count-1)+"個\n"+(item.can_sell?"売却できます。":item.reason);
            else {
                var state=cafe?.cats==null || cat==null?null:Array.Find(cafe.cats,c=>c.cat_id==cat.cat_id);
                details.text=item==null?"所持品はありません。購入タブ、または派遣・店舗イベントの報酬で入手できます。":item.name+" / 所持 "+item.count+"個\n効果："+item.effect+(cat==null?"\n対象の猫はありません。":"\n\n対象："+cat.name+" / "+cat.activity+(state==null?"":"\n現在：疲労 "+N(state.fatigue)+" / ストレス "+N(state.stress))+"\n使用による変化："+cat.changes+"\n"+(cat.can_select?"使用できます。所持数："+item.count+" → "+(item.count-1):"使用できない理由："+cat.reason));
            }
            if(!organized || modeTabs.Length==3)details.text+="\n\n購入・使用・売却の履歴\n"+(string.IsNullOrEmpty(current?.history)?"記録はありません。":current.history);
            var scroll=details.GetComponentInParent<ScrollRect>();if(scroll!=null){if(organized && modeTabs.Length>=4){float height=mode==3?326:224;var rect=scroll.GetComponent<RectTransform>();rect.anchoredPosition=new Vector2(24,mode==3?-148:-250);rect.sizeDelta=new Vector2(892,height);scroll.viewport.sizeDelta=new Vector2(862,height-12);scroll.verticalScrollbar.GetComponent<RectTransform>().sizeDelta=new Vector2(12,height-12);}details.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Math.Max(scroll.viewport.rect.height,details.preferredHeight+12));}RefreshControls();
        }
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();bool ready=online && client.CanManageSaves && selection==null;
            buy.interactable=ready && Shop!=null && Shop.can_select;sell.interactable=ready && Owned!=null && Owned.can_sell;use.interactable=ready && Cat!=null && Cat.can_select;
            shop.interactable=ready && (current?.shop?.Length ?? 0)>0;owned.interactable=ready && (current?.owned?.Length ?? 0)>0;cats.interactable=ready && (Owned?.targets?.Length ?? 0)>0;
            confirm.interactable=online && client.CanManageSaves && selection!=null && client.CanItemAction(selection,selectedSource,selectedCat);close.interactable=cancel.interactable=true;
            if(modeTabs!=null)foreach(var tab in modeTabs)tab.interactable=ready;
            notice.text=!online ? "Python未接続です。再接続後に操作できます。" : !client.CanManageSaves ? "操作を送信しています。完了するまでお待ちください。" : mode==3 ? "用品の記録を閲覧しています。購入・ケア・売却は上のタブから選べます。" : "購入・使用・売却は営業準備中に1個ずつ行います。選択後、確認画面で確定してください。";
        }
        public void Disconnect(){online=false;Clear();RefreshControls();}
    }
}
