using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonGameOverSetup
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
        [MenuItem("Cat Cafe/開発用/UI再生成/経営終了",false,11)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var root=preview.transform.Find("Content Canvas/Background");
            var old=root.Find("Game Over");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var overlay=Box(root,"Game Over",0,174,1280,626,new Color(0,0,0,.55f));
            var card=Box(overlay.transform,"Result Card",170,8,940,600,new Color(.98f,.97f,.93f,1));
            var panel=preview.GetComponent<PythonGameOverPanel>() ?? preview.gameObject.AddComponent<PythonGameOverPanel>();panel.panel=overlay;
            Label(card.transform,"Title",24,20,892,40,25).text="経営終了";
            var area=Box(card.transform,"Scroll",24,76,892,414,Color.clear);panel.scroll=area.AddComponent<ScrollRect>();panel.scroll.horizontal=false;panel.scroll.movementType=ScrollRect.MovementType.Clamped;
            var viewport=Box(area.transform,"Viewport",0,0,866,414,Color.white);viewport.AddComponent<Mask>().showMaskGraphic=false;
            panel.details=Label(viewport.transform,"Details",0,0,866,414,18);panel.details.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            panel.scroll.content=panel.details.rectTransform;panel.scroll.viewport=viewport.GetComponent<RectTransform>();
            var bar=DefaultControls.CreateScrollbar(new DefaultControls.Resources());bar.transform.SetParent(area.transform,false);Place(bar.GetComponent<RectTransform>(),872,0,20,414);panel.scroll.verticalScrollbar=bar.GetComponent<Scrollbar>();panel.scroll.verticalScrollbar.direction=Scrollbar.Direction.BottomToTop;
            panel.notice=Label(card.transform,"Notice",24,500,892,44,16);panel.notice.text="結果を残す場合は、タイトルへ戻る前に保存してください。";
            panel.save=Button(card.transform,"Save","結果を保存",24,550,432);UnityEventTools.AddPersistentListener(panel.save.onClick,panel.Save);
            panel.title=Button(card.transform,"Return","タイトルへ",480,550,436);UnityEventTools.AddPersistentListener(panel.title.onClick,panel.Return);
            overlay.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("CAT_CAFE_GAME_OVER_UI_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.game_over=new PythonGameOver{reason="funds",day=5,popularity=10};state.funds=0;
            typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});var panel=preview.GetComponent<PythonGameOverPanel>();
            if(!panel.panel.activeSelf || !panel.save.interactable || !panel.title.interactable || client.CanChangeShifts || client.CanProgressBusiness || client.CanStartAutomatic || !panel.details.text.Contains("資金不足"))throw new Exception("Ended state guards failed");
            state.game_over.reason="popularity";typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});if(!panel.details.text.Contains("人気低下"))throw new Exception("Popularity result missing");
            state.game_over=null;typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});if(panel.panel.activeSelf)throw new Exception("Result remained after new game");
            Debug.Log("CAT_CAFE_GAME_OVER_DISPLAY_OK");
        }
    }
}
