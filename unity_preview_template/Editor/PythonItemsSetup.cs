using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonItemsSetup
    {
        static T Copy<T>(T source,Transform root,Transform target) where T:Component{return target.Find(AnimationUtility.CalculateTransformPath(source.transform,root)).GetComponent<T>();}
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static void Bind(Button button,UnityEngine.Events.UnityAction action){button.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(button.onClick,action);}
        static void Dropdown(Dropdown dropdown,float y,float width,UnityEngine.Events.UnityAction<int> callback)
        {Place(dropdown.GetComponent<RectTransform>(),24,y,width,40);dropdown.transform.Find("Arrow Text").GetComponent<RectTransform>().anchoredPosition=new Vector2(width-30,-7);dropdown.onValueChanged=new Dropdown.DropdownEvent();UnityEventTools.AddPersistentListener(dropdown.onValueChanged,callback);}
        [MenuItem("Cat Cafe/開発用/UI再生成/用品・猫のケア",false,29)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var source=preview.GetComponent<PythonDispatchChoicePanel>();var root=source.panel.transform.parent;var old=root.Find("Items");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var p=preview.GetComponent<PythonItemsPanel>() ?? preview.gameObject.AddComponent<PythonItemsPanel>();p.panel=UnityEngine.Object.Instantiate(source.panel,root);p.panel.name="Items";var s=source.panel.transform;var t=p.panel.transform;
            p.title=Copy(source.title,s,t);p.details=Copy(source.details,s,t);p.notice=Copy(source.notice,s,t);p.confirmationText=Copy(source.confirmationText,s,t);p.confirmation=t.Find(AnimationUtility.CalculateTransformPath(source.confirmation.transform,s)).gameObject;
            p.buy=Copy(source.first,s,t);p.sell=Copy(source.second,s,t);p.close=Copy(source.close,s,t);p.confirm=Copy(source.confirm,s,t);p.cancel=Copy(source.cancel,s,t);p.owned=Copy(source.records,s,t);
            p.shop=UnityEngine.Object.Instantiate(p.owned,p.owned.transform.parent);p.shop.name="Shop";p.cats=UnityEngine.Object.Instantiate(p.owned,p.owned.transform.parent);p.cats.name="Cats";
            Dropdown(p.shop,62,580,p.SelectShop);Dropdown(p.owned,164,580,p.SelectOwned);Dropdown(p.cats,210,892,p.SelectCat);
            p.shopNote=UnityEngine.Object.Instantiate(p.notice,p.notice.transform.parent);p.shopNote.name="Shop Note";Place(p.shopNote.rectTransform,24,108,892,50);
            Place(p.buy.GetComponent<RectTransform>(),624,62,292,40);Place(p.sell.GetComponent<RectTransform>(),624,164,292,40);p.buy.GetComponentInChildren<Text>().text="1個購入（確認へ）";p.sell.GetComponentInChildren<Text>().text="1個売却（確認へ）";
            p.use=UnityEngine.Object.Instantiate(p.buy,p.buy.transform.parent);p.use.name="Use";Place(p.use.GetComponent<RectTransform>(),24,550,432,40);p.use.GetComponentInChildren<Text>().text="選んだ猫に1個使う（確認へ）";Place(p.close.GetComponent<RectTransform>(),480,550,436,40);
            var scroll=p.details.transform.parent.parent.GetComponent<RectTransform>();Place(scroll,24,258,892,224);Place(p.details.transform.parent.GetComponent<RectTransform>(),6,6,862,212);scroll.GetComponent<ScrollRect>().verticalScrollbar.GetComponent<RectTransform>().sizeDelta=new Vector2(12,212);
            Bind(p.buy,p.Buy);Bind(p.sell,p.Sell);Bind(p.use,p.Use);Bind(p.close,p.Close);Bind(p.confirm,p.Confirm);Bind(p.cancel,p.Cancel);p.confirmation.transform.SetAsLastSibling();p.confirmation.SetActive(false);p.panel.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);PythonItemsLayout.Build();Debug.Log("CAT_CAFE_ITEMS_UI_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var f=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",f).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="items-test";state.can_set_shifts=true;state.items=new PythonItems{count=2,shop=new[]{new PythonItemShop{choice="care_supplies",name="ケア用品",effect="ストレス −10",cost=100,count=2,funds_after=900,can_select=true}},owned=new[]{new PythonOwnedItem{choice="item-purchase-1",item_id="care_supplies",name="ケア用品",effect="ストレス −10",count=2,price=40,funds_after=1040,can_sell=true,targets=new[]{new PythonItemTarget{cat_id="cat-mike",name="ミケ",activity="在店",changes="ストレス 5 → 0",can_select=true}}}},history="1日目 購入：ケア用品 / 費用 100"};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",f).Invoke(client,new object[]{state});apply();var p=preview.GetComponent<PythonItemsPanel>();var list=preview.GetComponent<PythonEventListPanel>();list.Open();list.Items();if(list.panel.activeSelf || !p.panel.activeSelf || !p.buy.interactable || !p.use.interactable || !p.sell.interactable)throw new Exception("Items navigation failed");p.Use();if(!p.confirmationText.text.Contains("5 → 0") || !p.confirmationText.text.Contains("2 → 1"))throw new Exception("Item use effect missing");Canvas.ForceUpdateCanvases();if(p.confirmationText.preferredHeight>p.confirmationText.rectTransform.rect.height)throw new Exception("Item confirmation clipped");typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/ItemsConfirmation.png"});p.Cancel();
            Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/Items.png"});p.Sell();if(!p.confirmationText.text.Contains("1040"))throw new Exception("Sale funds missing");state.revision++;apply();if(p.confirmation.activeSelf)throw new Exception("Stale item confirmation remained");p.Disconnect();if(p.buy.interactable || p.sell.interactable || p.use.interactable)throw new Exception("Offline items enabled");apply();
            state.items.owned[0].targets[0].can_select=false;state.items.owned[0].targets[0].reason="この猫のストレスは0です。";state.revision++;apply();if(p.use.interactable)throw new Exception("Zero stress item enabled");state.items.owned=new PythonOwnedItem[0];state.items.count=0;state.revision++;apply();if(p.use.interactable || p.sell.interactable || !p.buy.interactable)throw new Exception("Empty inventory guard failed");
            state.items.shop[0].ends_game=true;state.items.shop[0].funds_after=0;state.revision++;apply();p.Buy();if(!p.confirmationText.text.Contains("営業が終了"))throw new Exception("Zero funds ending consequence missing");p.Cancel();if(typeof(PythonStateClient).GetField("pending",f).GetValue(client)!=null)throw new Exception("Item cancel queued action");
            if(p.buy.onClick.GetPersistentTarget(0)!=p || p.cats.onValueChanged.GetPersistentTarget(0)!=p)throw new Exception("Items callbacks incorrectly bound");p.Close();state.instance_id="loaded-game";apply();if(p.panel.activeSelf)throw new Exception("Items retained across load");p.Open();p.Buy();p.Confirm();var queued=(PythonShiftCommand)typeof(PythonStateClient).GetField("pending",f).GetValue(client);if(queued==null || queued.kind!="purchase_item" || queued.choice!="care_supplies" || p.buy.interactable)throw new Exception("Item queue failed");Debug.Log("CAT_CAFE_ITEMS_DISPLAY_OK");
        }
    }
}
