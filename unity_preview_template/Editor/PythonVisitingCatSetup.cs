using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonVisitingCatSetup
    {
        static void Place(RectTransform rect,float x,float y,float w,float h)
        { rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h); }
        static GameObject Box(Transform parent,string name,float x,float y,float w,float h,Color color)
        { var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);Place(go.GetComponent<RectTransform>(),x,y,w,h);go.GetComponent<Image>().color=color;return go; }
        static Text Label(Transform parent,string name,float x,float y,float w,float h,int size)
        { var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);Place(go.GetComponent<RectTransform>(),x,y,w,h);var text=go.GetComponent<Text>();text.font=ContentPreviewLayout.LoadFont();text.fontSize=size;text.color=new Color(.16f,.21f,.18f);text.raycastTarget=false;text.supportRichText=false;return text; }
        static Button Button(Transform parent,string name,string caption,float x,float y,float w)
        { var go=DefaultControls.CreateButton(new DefaultControls.Resources());go.name=name;go.transform.SetParent(parent,false);Place(go.GetComponent<RectTransform>(),x,y,w,40);var text=go.GetComponentInChildren<Text>();text.font=ContentPreviewLayout.LoadFont();text.fontSize=18;text.text=caption;return go.GetComponent<Button>(); }
        [MenuItem("Cat Cafe/開発用/UI再生成/店先に通う猫",false,20)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();
            var root=preview.transform.Find("Content Canvas/Background");var old=root.Find("Visiting Cat");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var panel=preview.GetComponent<PythonVisitingCatPanel>() ?? preview.gameObject.AddComponent<PythonVisitingCatPanel>();
            panel.panel=Box(root,"Visiting Cat",0,174,1280,626,new Color(0,0,0,.55f));var card=Box(panel.panel.transform,"Request Card",170,8,940,600,new Color(.98f,.97f,.93f));
            panel.title=Label(card.transform,"Title",24,18,892,54,23);panel.picture=PythonIntakeSetup.Portrait(card.transform,24,86,272,272);
            var area=Box(card.transform,"Details Scroll",316,86,600,336,new Color(.93f,.94f,.89f));var scroll=area.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            var viewport=Box(area.transform,"Viewport",8,6,570,324,Color.white);viewport.AddComponent<Mask>().showMaskGraphic=false;
            panel.details=Label(viewport.transform,"Text",0,0,570,324,17);panel.details.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport=viewport.GetComponent<RectTransform>();scroll.content=panel.details.rectTransform;
            var bar=DefaultControls.CreateScrollbar(new DefaultControls.Resources());bar.transform.SetParent(area.transform,false);Place(bar.GetComponent<RectTransform>(),582,6,12,324);scroll.verticalScrollbar=bar.GetComponent<Scrollbar>();scroll.verticalScrollbar.direction=Scrollbar.Direction.BottomToTop;
            panel.interact=Button(card.transform,"Interact","交流する（確認へ）",24,434,272);UnityEventTools.AddPersistentListener(panel.interact.onClick,panel.Interact);
            panel.skip=Button(card.transform,"Skip","今日は見送る（確認へ）",316,434,290);UnityEventTools.AddPersistentListener(panel.skip.onClick,panel.Skip);
            panel.accept=Button(card.transform,"Accept","迎える（確認へ）",626,434,290);UnityEventTools.AddPersistentListener(panel.accept.onClick,panel.Accept);
            panel.notice=Label(card.transform,"Notice",24,484,892,58,16);
            panel.housing=Button(card.transform,"Housing","飼育スペースを確認",24,550,432);UnityEventTools.AddPersistentListener(panel.housing.onClick,panel.Housing);
            panel.close=Button(card.transform,"Close","営業準備へ戻る",480,550,436);UnityEventTools.AddPersistentListener(panel.close.onClick,panel.Close);
            panel.confirmation=Box(card.transform,"Confirmation",0,0,940,600,new Color(.98f,.97f,.93f));panel.confirmationText=Label(panel.confirmation.transform,"Text",100,150,740,180,23);
            panel.confirm=Button(panel.confirmation.transform,"Confirm","この内容で確定する",100,380,340);UnityEventTools.AddPersistentListener(panel.confirm.onClick,panel.Confirm);
            panel.cancel=Button(panel.confirmation.transform,"Cancel","戻る",500,380,340);UnityEventTools.AddPersistentListener(panel.cancel.onClick,panel.Cancel);
            var preparation=preview.GetComponent<PythonPreparationPanel>();var parent=preparation.close.transform.parent;var entry=parent.Find("Visiting Cat Entry");if(entry!=null)UnityEngine.Object.DestroyImmediate(entry.gameObject);
            Place(preparation.close.GetComponent<RectTransform>(),706,550,210,40);preparation.close.GetComponentInChildren<Text>().fontSize=17;
            panel.open=Button(parent,"Visiting Cat Entry","店先に通う猫",480,550,210);UnityEventTools.AddPersistentListener(panel.open.onClick,panel.Open);
            panel.open.gameObject.SetActive(false);panel.confirmation.SetActive(false);panel.panel.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("CAT_CAFE_VISITING_UI_OK");
        }
        public static void BuildAndVerify()
        {
            PythonEventLayout.BuildAndVerify();Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="visiting-test";state.can_set_shifts=true;state.closed=false;state.required_action="";
            var cat=new PythonVisitingCat{cat_id="visiting-cat-1",content_cat_id="visiting-cat-1",name="ココ",status="visiting",required=3,cost=200,funds_after=800,personality="距離を大切にする猫",features="キジトラ / 短毛",housing="在籍5匹 / 上限6匹 / 空き1匹",can_interact=true,can_skip=true};state.visiting_cat=cat;
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});apply();var panel=preview.GetComponent<PythonVisitingCatPanel>();panel.Open();
            if(!panel.panel.activeSelf || !panel.interact.interactable || panel.accept.interactable || panel.picture.texture==null)throw new Exception("Visiting initial display failed");
            panel.Interact();if(!panel.confirmation.activeSelf || !panel.confirm.interactable)throw new Exception("Visiting confirmation missing");panel.Cancel();if(panel.confirmation.activeSelf)throw new Exception("Cancel failed");
            panel.Skip();state.revision++;apply();if(panel.confirmation.activeSelf)throw new Exception("Stale confirmation remained");
            cat.progress=3;cat.status="ready";cat.can_interact=false;cat.can_accept=true;cat.history="2日目：交流した\n3日目：交流した\n4日目：交流した";apply();panel.Accept();
            if(!panel.confirmationText.text.Contains("200") || !panel.confirmationText.text.Contains("800"))throw new Exception("Admission cost confirmation missing");panel.Cancel();
            Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/VisitingCat.png"});
            panel.Disconnect();if(panel.accept.interactable || panel.confirm.interactable)throw new Exception("Offline visiting enabled");apply();
            cat.can_accept=false;cat.accept_reason="飼育スペースが足りません。";apply();if(panel.accept.interactable || !panel.notice.text.Contains("飼育スペース"))throw new Exception("Housing guard missing");
            state.can_set_shifts=false;apply();if(panel.panel.activeSelf || panel.open.gameObject.activeSelf)throw new Exception("Visiting stayed in business");
            state.can_set_shifts=true;cat.status="accepted";cat.accepted_day=4;apply();panel.Open();if(!panel.notice.text.Contains("迎えました"))throw new Exception("Joined history missing");
            state.visiting_cat=null;apply();if(panel.panel.activeSelf || panel.open.gameObject.activeSelf)throw new Exception("Legacy visiting entry visible");
            Debug.Log("CAT_CAFE_VISITING_DISPLAY_OK");
        }
    }
}
