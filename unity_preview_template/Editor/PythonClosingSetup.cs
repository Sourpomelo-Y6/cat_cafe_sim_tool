using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonClosingSetup
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
        [MenuItem("Cat Cafe/開発用/UI再生成/閉店の振り返り",false,13)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var root=preview.transform.Find("Content Canvas/Background");
            foreach(var name in new[]{"Closing Review","Closing Entry"}){var old=root.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
            var overlay=Box(root,"Closing Review",0,174,1280,626,new Color(0,0,0,.55f));var card=Box(overlay.transform,"Review Card",170,8,940,600,new Color(.98f,.97f,.93f,1));
            var panel=preview.GetComponent<PythonClosingPanel>() ?? preview.gameObject.AddComponent<PythonClosingPanel>();panel.panel=overlay;
            Label(card.transform,"Title",24,20,892,40,25).text="閉店・1日の振り返り";
            var area=Box(card.transform,"Scroll",24,76,892,406,Color.clear);var scroll=area.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            var viewport=Box(area.transform,"Viewport",0,0,866,406,Color.white);viewport.AddComponent<Mask>().showMaskGraphic=false;
            panel.details=Label(viewport.transform,"Details",0,0,866,406,18);panel.details.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;scroll.content=panel.details.rectTransform;scroll.viewport=viewport.GetComponent<RectTransform>();
            var bar=DefaultControls.CreateScrollbar(new DefaultControls.Resources());bar.transform.SetParent(area.transform,false);Place(bar.GetComponent<RectTransform>(),872,0,20,406);scroll.verticalScrollbar=bar.GetComponent<Scrollbar>();scroll.verticalScrollbar.direction=Scrollbar.Direction.BottomToTop;
            panel.notice=Label(card.transform,"Notice",24,490,892,56,16);
            panel.save=Button(card.transform,"Save","保存",24,550,270);UnityEventTools.AddPersistentListener(panel.save.onClick,panel.Save);
            panel.next=Button(card.transform,"Next","翌日の準備へ",314,550,300);UnityEventTools.AddPersistentListener(panel.next.onClick,panel.Next);
            panel.close=Button(card.transform,"Close","閉じる",634,550,282);UnityEventTools.AddPersistentListener(panel.close.onClick,panel.Close);
            panel.open=Button(root,"Closing Entry","1日の振り返り",332,24,240);UnityEventTools.AddPersistentListener(panel.open.onClick,panel.Open);
            panel.open.gameObject.SetActive(false);overlay.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("CAT_CAFE_CLOSING_UI_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="closing";state.closed=false;state.can_set_shifts=false;state.required_action="";
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});apply();var panel=preview.GetComponent<PythonClosingPanel>();if(panel.panel.activeSelf)throw new Exception("Review shown during business");
            state.closed=true;state.finance=new PythonFinanceState{opening_funds=1000,total_income=100,total_expenses=50,net_cash_flow=50,closing_funds=1050};state.revenue=80;state.objective_progress=new PythonObjectiveProgress{label="自由営業",details="期限なし・自由に営業"};apply();
            if(!panel.panel.activeSelf || !panel.save.interactable || !panel.next.interactable || !panel.details.text.Contains("+50") || !panel.details.text.Contains("疲労"))throw new Exception("Closing review missing");
            panel.Close();apply();if(panel.panel.activeSelf)throw new Exception("Review reopened during polling");panel.Open();
            state.required_action="先に帰還を確認してください。";apply();if(panel.next.interactable || !panel.notice.text.Contains("帰還"))throw new Exception("Required event guard failed");
            typeof(PythonStateClient).GetField("online",flags).SetValue(client,false);panel.Disconnect();if(panel.save.interactable || panel.next.interactable || !panel.notice.text.Contains("未接続"))throw new Exception("Offline guard failed");
            state.required_action="";apply();panel.Notify("保存しました。");if(!panel.notice.text.Contains("保存しました"))throw new Exception("Save notice missing");
            Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/ClosingReview.png"});
            state.closed=false;state.can_set_shifts=true;state.day++;apply();if(panel.panel.activeSelf || panel.open.gameObject.activeSelf)throw new Exception("Review remained next day");
            state.closed=true;apply();if(!panel.panel.activeSelf)throw new Exception("Next closing not shown");state.game_over=new PythonGameOver{reason="funds",day=state.day};apply();if(panel.panel.activeSelf || panel.open.gameObject.activeSelf)throw new Exception("Review obscured game over");
            Debug.Log("CAT_CAFE_CLOSING_DISPLAY_OK");
        }
    }
}
