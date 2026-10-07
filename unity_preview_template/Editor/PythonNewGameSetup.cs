using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonNewGameSetup
    {
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var root=preview.transform.Find("Content Canvas/Background");
            foreach(string name in new[]{"New Game","New Game Entry"}) {var old=root.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
            var panel=preview.GetComponent<PythonNewGamePanel>() ?? preview.gameObject.AddComponent<PythonNewGamePanel>();
            panel.open=Button(root,"New Game Entry","新規ゲーム",600,24);
            UnityEventTools.AddPersistentListener(panel.open.onClick,panel.Open);
            panel.panel=Box(root,"New Game",0,174,1280,626,new Color(0,0,0,.55f));
            var card=Box(panel.panel.transform,"Start Card",250,8,780,600,new Color(.98f,.97f,.93f,1));
            Label(card.transform,"Title",30,22,700,44,25).text="新規ゲーム・開始条件";
            var dropdown=DefaultControls.CreateDropdown(new DefaultControls.Resources());dropdown.name="Objective";dropdown.transform.SetParent(card.transform,false);
            Place(dropdown.GetComponent<RectTransform>(),30,82,720,44);panel.objectives=dropdown.GetComponent<Dropdown>();
            foreach(var text in dropdown.GetComponentsInChildren<Text>(true)){text.font=ContentPreviewLayout.LoadFont();text.fontSize=20;}
            UnityEventTools.AddPersistentListener(panel.objectives.onValueChanged,panel.Select);
            panel.details=Label(card.transform,"Conditions",30,150,720,245,22);
            panel.notice=Label(card.transform,"Notice",30,410,720,110,18);
            panel.start=Button(card.transform,"Start","この目標で始める",30,540);
            UnityEventTools.AddPersistentListener(panel.start.onClick,panel.StartGame);
            var close=Button(card.transform,"Close","現在のゲームへ戻る",500,540);UnityEventTools.AddPersistentListener(close.onClick,panel.Close);
            panel.panel.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("CAT_CAFE_NEW_GAME_UI_OK");
        }
        static void Place(RectTransform rect,float x,float y,float width,float height)
        {
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-y); rect.sizeDelta=new Vector2(width,height);
        }
        static GameObject Box(Transform parent,string name,float x,float y,float width,float height,Color color)
        {
            var box=new GameObject(name,typeof(RectTransform),typeof(Image)); box.transform.SetParent(parent,false);
            Place(box.GetComponent<RectTransform>(),x,y,width,height); box.GetComponent<Image>().color=color; return box;
        }
        static Text Label(Transform parent,string name,float x,float y,float width,float height,int fontSize)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text)); go.transform.SetParent(parent,false);
            Place(go.GetComponent<RectTransform>(),x,y,width,height); var text=go.GetComponent<Text>();
            text.font=ContentPreviewLayout.LoadFont(); text.fontSize=fontSize; text.color=new Color(.18f,.16f,.14f,1);
            text.alignment=TextAnchor.UpperLeft; text.lineSpacing=1.12f; text.raycastTarget=false; return text;
        }
        static Button Button(Transform parent,string name,string caption,float x,float y)
        {
            var go=DefaultControls.CreateButton(new DefaultControls.Resources()); go.name=name; go.transform.SetParent(parent,false);
            Place(go.GetComponent<RectTransform>(),x,y,220,44); var text=go.GetComponentInChildren<Text>();
            text.font=ContentPreviewLayout.LoadFont(); text.fontSize=19; text.text=caption; return go.GetComponent<Button>();
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.new_game_options=new[]{new PythonNewGameOption{choice="popularity",label="人気3段階",detail="期限内に人気を高めます。",conditions="初期猫：5匹 / 席：2席 / 資金：1000"},new PythonNewGameOption{choice="free",label="自由営業",detail="自由に営業します。",conditions="初期猫：5匹 / 席：2席 / 資金：1000"}};
            typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});var panel=preview.GetComponent<PythonNewGamePanel>();panel.Open();
            if(!panel.panel.activeSelf || !panel.start.interactable || !panel.details.text.Contains("1000"))throw new Exception("Start screen failed");
            Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/NewGame.png"});
            panel.objectives.value=1;panel.Select(1);panel.StartGame();
            var command=(PythonShiftCommand)typeof(PythonStateClient).GetField("pending",flags).GetValue(client);
            if(command.kind!="new_game" || command.choice!="free" || panel.start.interactable)throw new Exception("New game guard failed");
            panel.Disconnect();if(panel.open.interactable || panel.start.interactable)throw new Exception("Offline guard failed");
            panel.Close();if(panel.panel.activeSelf)throw new Exception("Close failed");
            Debug.Log("CAT_CAFE_NEW_GAME_DISPLAY_OK");
        }
    }
}
