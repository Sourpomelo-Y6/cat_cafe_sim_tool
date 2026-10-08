using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonPetShopSetup
    {
        static T Copy<T>(T source,Transform root,Transform target) where T:Component{return target.Find(AnimationUtility.CalculateTransformPath(source.transform,root)).GetComponent<T>();}
        static void Bind(Button b,UnityEngine.Events.UnityAction action){b.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(b.onClick,action);b.onClick.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);}
        [MenuItem("Cat Cafe/開発用/UI再生成/ペットショップ",false,37)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var source=preview.GetComponent<PythonVisitingCatPanel>();var root=source.panel.transform.parent;var old=root.Find("Pet Shop");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var p=preview.GetComponent<PythonPetShopPanel>() ?? preview.gameObject.AddComponent<PythonPetShopPanel>();p.panel=UnityEngine.Object.Instantiate(source.panel,root);p.panel.name="Pet Shop";var origin=source.panel.transform;var target=p.panel.transform;
            p.picture=Copy(source.picture,origin,target);p.title=Copy(source.title,origin,target);p.details=Copy(source.details,origin,target);p.notice=Copy(source.notice,origin,target);p.confirmationText=Copy(source.confirmationText,origin,target);p.confirmation=target.Find(AnimationUtility.CalculateTransformPath(source.confirmation.transform,origin)).gameObject;
            p.previous=Copy(source.interact,origin,target);p.next=Copy(source.skip,origin,target);p.purchase=Copy(source.accept,origin,target);p.housing=Copy(source.housing,origin,target);p.close=Copy(source.close,origin,target);p.confirm=Copy(source.confirm,origin,target);p.cancel=Copy(source.cancel,origin,target);
            p.previous.GetComponentInChildren<Text>().text="前の候補";p.next.GetComponentInChildren<Text>().text="次の候補";p.purchase.GetComponentInChildren<Text>().text="購入して迎える（確認）";p.close.GetComponentInChildren<Text>().text="閉じる";p.notice.fontSize=14;
            Bind(p.previous,p.Previous);Bind(p.next,p.Next);Bind(p.purchase,p.Purchase);Bind(p.housing,p.Housing);Bind(p.close,p.Close);Bind(p.confirm,p.Confirm);Bind(p.cancel,p.Cancel);
            p.confirmation.SetActive(false);p.panel.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);PythonEventListSetup.Build();Debug.Log("CAT_CAFE_PET_SHOP_UI_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var f=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",f).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="pet-shop";state.can_set_shifts=true;state.required_action="";state.pet_shop=new PythonPetShop{housing="在籍5匹 / 上限6匹 / 空き1匹",notes="営業準備中に購入できます。購入した猫は再購入できません。",rows=new[]{new PythonShopCat{cat_id="shop-1",content_cat_id="shop-1",name="チロ",cost=400,funds_after=600,can_purchase=true,details="名前 / 個性：チロ / 穏やかな甘えん坊\n特徴：白猫 / 短毛\n特性：接客上手\n\n価格：400\n所持金：1000 → 購入後：600\n飼育スペース：在籍5匹 / 上限6匹 / 空き1匹\n\n健康・体力全回復・休養予定で加入します。"},new PythonShopCat{cat_id="shop-2",content_cat_id="shop-2",name="レオ",cost=450,funds_after=550,can_purchase=false,reason="飼育スペースが足りません。",details="レオ\n価格：450"}}};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",f).Invoke(client,new object[]{state});apply();var p=preview.GetComponent<PythonPetShopPanel>();var list=preview.GetComponent<PythonEventListPanel>();list.Open();if(list.entries.Length!=24 || !list.entries[20].interactable)throw new Exception("Shop entry missing");list.PetShop();if(list.panel.activeSelf || !p.panel.activeSelf || p.picture.texture==null || !p.purchase.interactable || p.previous.interactable || !p.next.interactable)throw new Exception("Shop navigation/silhouette failed");Capture(preview,"Logs/PetShop.png");
            p.purchase.onClick.Invoke();if(!p.confirmation.activeSelf || !p.confirmationText.text.Contains("400") || !p.confirmationText.text.Contains("600"))throw new Exception("Purchase confirmation failed");Capture(preview,"Logs/PetShopConfirmation.png");p.cancel.onClick.Invoke();if(p.confirmation.activeSelf || typeof(PythonStateClient).GetField("pending",f).GetValue(client)!=null)throw new Exception("Cancel sent purchase");
            p.Purchase();state.revision++;apply();if(p.confirmation.activeSelf)throw new Exception("Stale confirmation retained");p.next.onClick.Invoke();if(p.purchase.interactable || !p.notice.text.Contains("飼育スペース"))throw new Exception("Blocked candidate guard failed");apply();if(!p.title.text.Contains("レオ"))throw new Exception("Stable candidate lost");p.previous.onClick.Invoke();p.Purchase();p.Disconnect();if(p.confirmation.activeSelf || p.purchase.interactable || p.confirm.interactable || p.next.interactable)throw new Exception("Offline guards failed");apply();
            state.pet_shop.rows[0].accepted=true;state.pet_shop.rows[0].accepted_day=1;state.pet_shop.rows[0].can_purchase=false;state.revision++;apply();if(p.purchase.interactable || !p.title.text.Contains("購入済み") || !p.notice.text.Contains("1日目"))throw new Exception("Purchased history guard failed");
            state.instance_id="loaded";state.pet_shop.rows=new PythonShopCat[0];apply();if(p.panel.activeSelf)throw new Exception("Load retained shop");p.Open();if(!p.details.text.Contains("記録はありません") || p.purchase.interactable)throw new Exception("Empty shop failed");state.pet_shop=null;apply();if(p.panel.activeSelf || list.entries[20].interactable)throw new Exception("Legacy shop visible");
            state.pet_shop=new PythonPetShop{rows=new[]{new PythonShopCat{cat_id="shop-final",name="ナナ",can_purchase=true,cost=500,funds_after=500}}};state.revision++;apply();p.Open();p.Purchase();p.Confirm();var command=(PythonShiftCommand)typeof(PythonStateClient).GetField("pending",f).GetValue(client);if(command==null || command.kind!="purchase_cat" || command.cat_id!="shop-final" || command.working_cats.Length!=0)throw new Exception("Purchase command incorrect");var id=command.request_id;p.Confirm();if(((PythonShiftCommand)typeof(PythonStateClient).GetField("pending",f).GetValue(client)).request_id!=id || p.purchase.interactable)throw new Exception("Duplicate purchase queued");Debug.Log("CAT_CAFE_PET_SHOP_DISPLAY_OK");
        }
        static void Capture(ContentPreview preview,string path){Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,path});}
    }
}
