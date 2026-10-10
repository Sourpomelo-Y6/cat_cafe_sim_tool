using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonSaveBrowserLayout
    {
        static void Place(Component c,float x,float y,float w,float h){var r=c.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static GameObject Box(Transform parent,string name,float x,float y,float w,float h,Color color){var g=new GameObject(name,typeof(RectTransform),typeof(Image));g.transform.SetParent(parent,false);Place(g.GetComponent<RectTransform>(),x,y,w,h);g.GetComponent<Image>().color=color;return g;}
        static Text Label(Transform parent,string name,float x,float y,float w,float h,int size){var g=new GameObject(name,typeof(RectTransform),typeof(Text));g.transform.SetParent(parent,false);Place(g.GetComponent<RectTransform>(),x,y,w,h);var t=g.GetComponent<Text>();t.font=ContentPreviewLayout.LoadFont();t.fontSize=size;t.color=new Color(.16f,.23f,.19f);t.raycastTarget=false;t.supportRichText=false;return t;}
        static Button Button(Transform parent,string name,string caption,float x,float y,float w,float h,UnityEngine.Events.UnityAction call){var g=DefaultControls.CreateButton(new DefaultControls.Resources());g.name=name;g.transform.SetParent(parent,false);Place(g.GetComponent<RectTransform>(),x,y,w,h);var b=g.GetComponent<Button>();var t=b.GetComponentInChildren<Text>();t.font=ContentPreviewLayout.LoadFont();t.fontSize=18;t.text=caption;if(call!=null)Bind(b,call);return b;}
        static void Bind(Button b,UnityEngine.Events.UnityAction call){b.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(b.onClick,call);b.onClick.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);}
        static Text Scroll(Transform card,string name,float x,float y,float w,float h){var root=Box(card,name,x,y,w,h,new Color(.94f,.95f,.91f));var scroll=root.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;var viewport=Box(root.transform,"Viewport",10,8,w-20,h-16,Color.white);viewport.AddComponent<Mask>().showMaskGraphic=false;var text=Label(viewport.transform,"Text",0,0,w-20,h-16,19);text.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;scroll.viewport=viewport.GetComponent<RectTransform>();scroll.content=text.rectTransform;return text;}
        public static void RefreshIfReady(){if(UnityEngine.Object.FindObjectOfType<PythonSavePanel>()?.panel!=null)Build();}
        [MenuItem("Cat Cafe/開発用/UI再生成/セーブ・ロードのスロット画面",false,43)]
        public static void Build()
        {
            foreach(var path in new[]{"Assets/Scenes/CatCafeTitle.unity","Assets/Scenes/CatCafePythonState.unity"})
            {
                var scene=EditorSceneManager.OpenScene(path);var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var title=UnityEngine.Object.FindObjectOfType<PythonTitleScreen>();var owner=preview!=null?preview.gameObject:title.gameObject;var saves=owner.GetComponent<PythonSavePanel>();var browser=owner.GetComponent<PythonSaveBrowser>() ?? owner.AddComponent<PythonSaveBrowser>();
                browser.panel=preview!=null?saves.panel:title.savesPanel;var parent=browser.panel.transform;var existingLegacy=owner.GetComponent<PythonLegacySavePanel>();if(existingLegacy?.entry!=null)existingLegacy.entry.transform.SetParent(parent,false);var old=parent.Find("Slot Browser");if(old!=null)Object.DestroyImmediate(old.gameObject);
                if(preview==null){Place(title.notice,220,766,840,30);title.notice.fontSize=14;}
                if(preview==null)Place(browser.panel.GetComponent<RectTransform>(),170,160,940,600);
                var card=Box(parent,"Slot Browser",preview!=null?170:0,preview!=null?8:0,940,600,new Color(.99f,.98f,.94f));
                if(preview!=null){var original=parent.Find("Card");if(original!=null)original.gameObject.SetActive(false);}else foreach(Transform child in parent)if(child!=card.transform)child.gameObject.SetActive(false);
                browser.title=Label(card.transform,"Title",24,16,892,40,28);browser.saveTab=Button(card.transform,"Save Mode","セーブ",24,64,180,40,browser.SaveMode);browser.loadTab=Button(card.transform,"Load Mode","ロード",216,64,180,40,browser.LoadMode);browser.current=Label(card.transform,"Current",424,70,492,34,17);
                Label(card.transform,"Hint",24,116,892,24,15).text="スロットを選択してください。以前の保存データは最後のページから選べます。";browser.cards=new Button[4];
                for(int i=0;i<4;i++){var b=Button(card.transform,"Slot "+i,"空のスロット",24,148+i*90,448,80,null);b.GetComponentInChildren<Text>().fontSize=16;b.GetComponentInChildren<Text>().alignment=TextAnchor.MiddleLeft;b.GetComponentInChildren<Text>().rectTransform.offsetMin=new Vector2(14,4);b.GetComponentInChildren<Text>().rectTransform.offsetMax=new Vector2(-14,-4);UnityEventTools.AddIntPersistentListener(b.onClick,browser.Select,i);b.onClick.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);browser.cards[i]=b;}
                browser.details=Scroll(card.transform,"Selected Save",496,148,420,350);browser.previous=Button(card.transform,"Previous","◀ 前へ",24,512,132,30,browser.Previous);browser.next=Button(card.transform,"Next","次へ ▶",340,512,132,30,browser.Next);browser.pageLabel=Label(card.transform,"Page",168,516,160,26,17);browser.pageLabel.alignment=TextAnchor.MiddleCenter;browser.notice=Label(card.transform,"Notice",496,506,420,38,14);
                browser.action=Button(card.transform,"Execute","ロードする",496,550,200,40,browser.Choose);browser.close=Button(card.transform,"Back","戻る",716,550,200,40,browser.Close);
                var legacy=owner.GetComponent<PythonLegacySavePanel>();if(legacy!=null){legacy.entry.transform.SetParent(card.transform,false);Place(legacy.entry,24,550,448,40);legacy.entry.gameObject.SetActive(true);legacy.entry.GetComponentInChildren<Text>().text="Python版のセーブを開く";}
                browser.confirmation=Box(card.transform,"Confirmation",0,0,940,600,new Color(.99f,.98f,.94f));browser.confirmationText=Scroll(browser.confirmation.transform,"Confirmation Details",40,54,860,400);browser.confirm=Button(browser.confirmation.transform,"Confirm","確定する",100,480,340,44,browser.Confirm);browser.cancel=Button(browser.confirmation.transform,"Cancel","取り消す",500,480,340,44,browser.CancelAndRefresh);browser.confirmation.SetActive(false);
                if(preview!=null){Bind(preview.GetSceneButton(3),browser.OpenSave);var entry=preview.transform.Find("Content Canvas/Background/Open Save Browser").GetComponent<Button>();Bind(entry,browser.OpenLoad);entry.GetComponentInChildren<Text>().text="ロード";saves.saves.gameObject.SetActive(false);saves.resume.gameObject.SetActive(false);}else Bind(title.continueGame,browser.OpenLoad);
                browser.panel.SetActive(false);browser.title.text="セーブ / ロード";EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("CAT_CAFE_SAVE_SLOT_LAYOUT_OK");
        }
    }
}
