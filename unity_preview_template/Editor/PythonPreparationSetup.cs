using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonPreparationSetup
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
        [MenuItem("Cat Cafe/開発用/UI再生成/営業準備の案内",false,12)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var root=preview.transform.Find("Content Canvas/Background");
            foreach(var name in new[]{"Preparation Guide","Preparation Entry"}){var old=root.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
            var overlay=Box(root,"Preparation Guide",0,174,1280,626,new Color(0,0,0,.55f));var card=Box(overlay.transform,"Guide Card",170,8,940,600,new Color(.98f,.97f,.93f,1));
            var panel=preview.GetComponent<PythonPreparationPanel>() ?? preview.gameObject.AddComponent<PythonPreparationPanel>();panel.panel=overlay;
            Label(card.transform,"Title",24,20,892,40,25).text="営業を始める前に";
            var area=Box(card.transform,"Scroll",24,76,892,406,Color.clear);var scroll=area.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            var viewport=Box(area.transform,"Viewport",0,0,866,406,Color.white);viewport.AddComponent<Mask>().showMaskGraphic=false;
            panel.details=Label(viewport.transform,"Details",0,0,866,406,18);panel.details.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;scroll.content=panel.details.rectTransform;scroll.viewport=viewport.GetComponent<RectTransform>();
            var bar=DefaultControls.CreateScrollbar(new DefaultControls.Resources());bar.transform.SetParent(area.transform,false);Place(bar.GetComponent<RectTransform>(),872,0,20,406);scroll.verticalScrollbar=bar.GetComponent<Scrollbar>();scroll.verticalScrollbar.direction=Scrollbar.Direction.BottomToTop;
            panel.notice=Label(card.transform,"Notice",24,490,892,56,16);
            panel.close=Button(card.transform,"Close","案内を閉じて猫を選ぶ",24,550,892);UnityEventTools.AddPersistentListener(panel.close.onClick,panel.Close);
            panel.open=Button(root,"Preparation Entry","営業準備の案内",332,24,240);UnityEventTools.AddPersistentListener(panel.open.onClick,panel.Open);
            panel.open.gameObject.SetActive(false);overlay.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("CAT_CAFE_PREPARATION_UI_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="guide-first";state.day=1;state.tick=0;state.closed=false;state.can_set_shifts=true;state.required_action="";state.objective="free";state.new_game_options=new[]{new PythonNewGameOption{choice="free",label="自由営業",detail="期限なし・自由に営業",conditions="初期猫：5匹 / 席：2席 / 資金：1000"}};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});apply();var panel=preview.GetComponent<PythonPreparationPanel>();
            if(!panel.panel.activeSelf || !panel.details.text.Contains("自由営業") || !panel.details.text.Contains("自動進行") || !panel.details.text.Contains("1000"))throw new Exception("First preparation guide missing");
            panel.Close();apply();if(panel.panel.activeSelf)throw new Exception("Polling reopened dismissed guide");
            foreach(var cat in state.cats)cat.working=false;panel.Open();apply();if(!panel.notice.text.Contains("出勤する猫がいません"))throw new Exception("No working cat reason missing");
            state.required_action="先に帰還を確認してください。";apply();if(!panel.notice.text.Contains("帰還"))throw new Exception("Required action reason missing");
            panel.Disconnect();if(!panel.notice.text.Contains("未接続"))throw new Exception("Offline guide missing");apply();if(panel.notice.text.Contains("未接続"))throw new Exception("Offline message persisted");
            state.required_action="";Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/PreparationGuide.png"});
            state.can_set_shifts=false;apply();if(panel.panel.activeSelf || panel.open.gameObject.activeSelf)throw new Exception("Guide remained in business");
            state.can_set_shifts=true;state.game_over=new PythonGameOver{reason="funds",day=1};apply();if(panel.panel.activeSelf || panel.open.gameObject.activeSelf)throw new Exception("Guide obscured game over");
            state.game_over=null;state.instance_id="guide-new";apply();if(!panel.panel.activeSelf)throw new Exception("New game did not show guide");
            Debug.Log("CAT_CAFE_PREPARATION_DISPLAY_OK");
        }
    }
}
