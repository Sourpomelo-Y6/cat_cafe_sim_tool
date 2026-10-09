using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonItemsLayout
    {
        static void Place(Component c,float x,float y,float w,float h){var t=c.GetComponent<RectTransform>();t.anchorMin=t.anchorMax=t.pivot=new Vector2(0,1);t.anchoredPosition=new Vector2(x,-y);t.sizeDelta=new Vector2(w,h);}
        static void Bind(Button b,UnityEngine.Events.UnityAction action){b.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(b.onClick,action);b.onClick.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);}
        [MenuItem("Cat Cafe/開発用/UI再生成/用品・猫のケアの整理",false,30)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var p=UnityEngine.Object.FindObjectOfType<ContentPreview>().GetComponent<PythonItemsPanel>();var parent=p.title.transform.parent;
            foreach(string name in new[]{"Care Tab","Purchase Tab","Sale Tab","Section Label"}){var old=parent.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
            p.modeTabs=new Button[3];string[] names={"Care Tab","Purchase Tab","Sale Tab"};string[] captions={"所持品・猫のケア","用品の購入","所持品の売却"};UnityEngine.Events.UnityAction[] actions={p.ShowCare,p.ShowPurchase,p.ShowSale};
            for(int i=0;i<3;i++){var b=UnityEngine.Object.Instantiate(p.close,parent);b.name=names[i];Place(b,24+i*300,62,292,40);b.GetComponentInChildren<Text>().text=captions[i];Bind(b,actions[i]);p.modeTabs[i]=b;}
            p.sectionLabel=UnityEngine.Object.Instantiate(p.notice,parent);p.sectionLabel.name="Section Label";Place(p.sectionLabel,24,110,892,32);p.sectionLabel.fontSize=17;
            Place(p.shop,24,148,892,40);Place(p.owned,24,148,892,40);Place(p.cats,24,198,892,40);Place(p.shopNote,24,198,892,40);
            var scroll=p.details.transform.parent.parent.GetComponent<ScrollRect>();Place(scroll,24,250,892,224);Place(p.details.transform.parent.GetComponent<RectTransform>(),6,6,862,212);scroll.verticalScrollbar.GetComponent<RectTransform>().sizeDelta=new Vector2(12,212);p.details.fontSize=18;Place(p.notice,24,488,892,50);p.notice.fontSize=15;
            foreach(var dropdown in new[]{p.shop,p.owned,p.cats})dropdown.transform.Find("Arrow Text").GetComponent<RectTransform>().anchoredPosition=new Vector2(862,-7);
            Place(p.buy,24,550,432,40);Place(p.sell,24,550,432,40);Place(p.use,24,550,432,40);Place(p.close,480,550,436,40);
            foreach(var dropdown in new[]{p.shop,p.owned,p.cats})dropdown.onValueChanged.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
            Bind(p.buy,p.Buy);Bind(p.use,p.Use);Bind(p.sell,p.Sell);Bind(p.close,p.Close);Bind(p.confirm,p.Confirm);Bind(p.cancel,p.Cancel);
            p.confirmation.transform.SetAsLastSibling();p.confirmation.SetActive(false);p.panel.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("CAT_CAFE_ITEMS_LAYOUT_OK");
        }
    }
}
