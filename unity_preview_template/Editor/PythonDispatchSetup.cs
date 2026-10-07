using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonDispatchSetup
    {
        static void Place(RectTransform rect,float x,float y,float w,float h)
        { rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h); }
        static GameObject Box(Transform parent,string name,float x,float y,float w,float h,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
            Place(go.GetComponent<RectTransform>(),x,y,w,h);go.GetComponent<Image>().color=color;return go;
        }
        static Text Label(Transform parent,string name,float x,float y,float w,float h,int size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
            Place(go.GetComponent<RectTransform>(),x,y,w,h);var text=go.GetComponent<Text>();text.font=ContentPreviewLayout.LoadFont();text.fontSize=size;
            text.color=new Color(.16f,.21f,.18f);text.raycastTarget=false;text.supportRichText=false;return text;
        }
        static Button Button(Transform parent,string name,string caption,float x,float y,float w)
        {
            var go=DefaultControls.CreateButton(new DefaultControls.Resources());go.name=name;go.transform.SetParent(parent,false);
            Place(go.GetComponent<RectTransform>(),x,y,w,40);var text=go.GetComponentInChildren<Text>();text.font=ContentPreviewLayout.LoadFont();text.fontSize=18;text.text=caption;return go.GetComponent<Button>();
        }
        static Dropdown Dropdown(Transform parent,string name,float y)
        {
            var go=DefaultControls.CreateDropdown(new DefaultControls.Resources());go.name=name;go.transform.SetParent(parent,false);
            Place(go.GetComponent<RectTransform>(),24,y,892,40);var drop=go.GetComponent<Dropdown>();
            foreach(var text in go.GetComponentsInChildren<Text>(true)){text.font=ContentPreviewLayout.LoadFont();text.fontSize=18;}
            drop.itemText.rectTransform.parent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,36);
            drop.template.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,180);drop.itemText.verticalOverflow=VerticalWrapMode.Overflow;drop.itemText.alignment=TextAnchor.MiddleLeft;
            go.transform.Find("Arrow").GetComponent<Image>().color=Color.clear;Label(go.transform,"Arrow Text",862,7,26,26,16).text="▼";return drop;
        }
        [MenuItem("Cat Cafe/開発用/UI再生成/有力者への派遣",false,8)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var root=preview.transform.Find("Content Canvas/Background");
            var old=root.Find("Patron Dispatch");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var overlay=Box(root,"Patron Dispatch",0,174,1280,626,new Color(0,0,0,.55f));
            var card=Box(overlay.transform,"Dispatch Card",170,8,940,600,new Color(.98f,.97f,.93f,1));
            var panel=preview.GetComponent<PythonDispatchPanel>() ?? preview.gameObject.AddComponent<PythonDispatchPanel>();panel.panel=overlay;
            panel.title=Label(card.transform,"Title",24,16,892,40,25);
            panel.destinations=Dropdown(card.transform,"Destinations",62);UnityEventTools.AddPersistentListener(panel.destinations.onValueChanged,panel.Select);
            panel.details=Label(card.transform,"Terms",24,110,892,128,17);
            panel.send=Button(card.transform,"Send","この条件で派遣する",24,244,432);UnityEventTools.AddPersistentListener(panel.send.onClick,panel.Send);
            panel.returns=Dropdown(card.transform,"Returns",300);UnityEventTools.AddPersistentListener(panel.returns.onValueChanged,panel.Select);
            panel.progress=Label(card.transform,"Progress",24,346,892,146,16);
            panel.notice=Label(card.transform,"Notice",24,500,892,44,16);
            panel.receive=Button(card.transform,"Receive","帰還・報酬を受け取る",24,550,432);UnityEventTools.AddPersistentListener(panel.receive.onClick,panel.Receive);
            panel.close=Button(card.transform,"Close","閉じる",480,550,436);UnityEventTools.AddPersistentListener(panel.close.onClick,panel.Close);
            var detail=preview.GetComponent<PythonCatDetailsPanel>();var detailCard=detail.panel.transform.Find("Details Card");
            var entry=detailCard.Find("Patron Dispatch Entry");if(entry!=null)UnityEngine.Object.DestroyImmediate(entry.gameObject);
            Place(detail.interact.GetComponent<RectTransform>(),24,550,250,40);Place(detail.previousInteraction.GetComponent<RectTransform>(),294,550,250,40);
            detail.dispatch=Button(detailCard,"Patron Dispatch Entry","有力者へ派遣",564,550,272);UnityEventTools.AddPersistentListener(detail.dispatch.onClick,detail.Dispatch);
            detail.dispatch.interactable=false;overlay.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("CAT_CAFE_DISPATCH_UI_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.patron_dispatch=new PythonPatronDispatch{options=new[]{new PythonDispatchOption{cat_id=state.cats[0].cat_id,choice="patron_visit",label="地域の有力者への訪問",days=2,reward=220,satisfaction=30,detail="歓迎条件：白い毛 一致 / 得意分野 接客 不一致",can_select=true}},events=new PythonDispatchEvent[0]};
            state.objective_progress=new PythonObjectiveProgress{details="全員の満足度を目標まで高めます。\n地域の有力者：0 / 100\n気分が変わる有力者：0 / 100\n条件に厳格な有力者：0 / 100"};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});apply();
            var panel=preview.GetComponent<PythonDispatchPanel>();panel.Open(state.cats[0].cat_id);
            if(!panel.send.interactable || !panel.details.text.Contains("220") || panel.destinations.options[0].text.Length==0)throw new Exception("Dispatch preview missing");
            state.patron_dispatch.events=new[]{new PythonDispatchEvent{cat_id=state.cats[0].cat_id,choice="dispatch-1",label="ミケ / 地域の有力者",status="waiting",reward=220,satisfaction=30,can_select=true}};
            state.patron_dispatch.options[0].can_select=false;apply();
            if(!panel.receive.interactable || panel.close.interactable || panel.send.interactable)throw new Exception("Return guard failed");
            Canvas.ForceUpdateCanvases();if(panel.progress.preferredHeight>panel.progress.rectTransform.rect.height)throw new Exception("Patron satisfaction text clipped");
            typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/PatronDispatch.png"});
            panel.Disconnect();if(panel.receive.interactable)throw new Exception("Offline return enabled");
            state.patron_dispatch.events[0].status="resolved";state.patron_dispatch.events[0].can_select=false;apply();
            if(panel.receive.interactable || !panel.close.interactable)throw new Exception("Repeated reward enabled");
            panel.Close();apply();if(panel.panel.activeSelf)throw new Exception("Dismissed return reopened");
            Debug.Log("CAT_CAFE_DISPATCH_DISPLAY_OK");
        }
    }
}
