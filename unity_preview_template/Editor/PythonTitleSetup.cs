using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonTitleSetup
    {
        const string TitlePath="Assets/Scenes/CatCafeTitle.unity";
        const string BusinessPath="Assets/Scenes/CatCafePythonState.unity";
        const string BackgroundPath="Assets/CatCafePreview/Art/cat-cafe-title-background-v1.png";
        static void DecorateBackground(GameObject background)
        {
            AssetDatabase.ImportAsset(BackgroundPath,ImportAssetOptions.ForceSynchronousImport);
            var importer=AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
            if(importer==null)throw new Exception("Title background missing: "+BackgroundPath);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var image=background.GetComponent<Image>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            if(image.sprite==null)throw new Exception("Title background sprite could not be loaded");
            image.color=Color.white;image.raycastTarget=false;
        }
        [MenuItem("Cat Cafe/開発用/UI再生成/タイトル背景を適用",false,9)]
        public static void ApplyBackground()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            var scene=EditorSceneManager.OpenScene(TitlePath);
            var title=UnityEngine.Object.FindObjectOfType<PythonTitleScreen>();
            var background=title.transform.Find("Background").gameObject;
            DecorateBackground(background);
            background.transform.Find("Game Title").GetComponent<Text>().alignment=TextAnchor.UpperCenter;
            background.transform.Find("Subtitle").GetComponent<Text>().alignment=TextAnchor.UpperCenter;
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Canvas.ForceUpdateCanvases();Capture("Logs/TitleBackground.png");
            Debug.Log("CAT_CAFE_TITLE_BACKGROUND_OK");
        }
        [MenuItem("Cat Cafe/プレビュー/タイトル画面を開く",false,0)]
        public static void Open() {if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(TitlePath);}
        static Button MakeButton(Transform parent,string name,string caption,float x,float y)
        {
            var result=Button(parent,name,caption,x,y);Place(result.GetComponent<RectTransform>(),x,y,360,56);return result;
        }
        static Dropdown Dropdown(Transform parent,string name,float y)
        {
            var go=DefaultControls.CreateDropdown(new DefaultControls.Resources());go.name=name;go.transform.SetParent(parent,false);
            Place(go.GetComponent<RectTransform>(),40,y,760,48);
            foreach(var text in go.GetComponentsInChildren<Text>(true)){text.font=ContentPreviewLayout.LoadFont();text.fontSize=20;}
            var dropdown=go.GetComponent<Dropdown>();
            dropdown.itemText.rectTransform.parent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,40);
            dropdown.template.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,180);
            dropdown.itemText.verticalOverflow=VerticalWrapMode.Overflow;
            dropdown.itemText.alignment=TextAnchor.MiddleLeft;
            ConfigureArrow(dropdown);
            return dropdown;
        }
        static void ConfigureArrow(UnityEngine.UI.Dropdown dropdown)
        {
            var arrow=dropdown.transform.Find("Arrow");if(arrow==null)return;
            arrow.GetComponent<Image>().color=Color.clear;
            var old=arrow.Find("Arrow Label");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var go=new GameObject("Arrow Label",typeof(RectTransform),typeof(Text));go.transform.SetParent(arrow,false);
            var rect=go.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var text=go.GetComponent<Text>();text.font=ContentPreviewLayout.LoadFont();text.fontSize=16;text.text="▼";text.color=new Color(.18f,.16f,.14f);text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
        }
        public static void ApplyDropdownArrows()
        {
            var scene=EditorSceneManager.OpenScene(TitlePath);var title=UnityEngine.Object.FindObjectOfType<PythonTitleScreen>();
            ConfigureArrow(title.objectives);ConfigureArrow(title.saves);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("CAT_CAFE_TITLE_ARROWS_OK");
        }
        [MenuItem("Cat Cafe/開発用/UI再生成/タイトル画面",false,8)]
        public static void Build()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var titleCamera=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));
            titleCamera.tag="MainCamera";titleCamera.transform.position=new Vector3(0,0,-10);
            var camera=titleCamera.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.95f,.94f,.89f,1);camera.cullingMask=0;camera.orthographic=true;
            var root=new GameObject("Cat Cafe Title",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,800);scaler.matchWidthOrHeight=.5f;
            new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            var background=Box(root.transform,"Background",0,0,1280,800,new Color(.95f,.94f,.89f,1));
            DecorateBackground(background);
            Label(background.transform,"Game Title",200,90,880,90,56).text="Cat Cafe Sim";
            Label(background.transform,"Subtitle",205,190,870,50,25).text="猫たちと育てる、あなたの猫カフェ";
            background.transform.Find("Game Title").GetComponent<Text>().alignment=TextAnchor.UpperCenter;
            background.transform.Find("Subtitle").GetComponent<Text>().alignment=TextAnchor.UpperCenter;
            var panel=root.AddComponent<PythonTitleScreen>();
            panel.home=Box(background.transform,"Home",200,270,880,340,Color.clear);
            panel.newGame=MakeButton(panel.home.transform,"New Game","はじめから",260,35);UnityEventTools.AddPersistentListener(panel.newGame.onClick,panel.ShowConditions);
            panel.continueGame=MakeButton(panel.home.transform,"Continue","つづきから",260,125);UnityEventTools.AddPersistentListener(panel.continueGame.onClick,panel.ShowSaves);
            panel.conditionsPanel=Box(background.transform,"Starting Conditions",220,260,840,420,new Color(.99f,.98f,.95f,1));
            panel.objectives=Dropdown(panel.conditionsPanel.transform,"Objective",28);UnityEventTools.AddPersistentListener(panel.objectives.onValueChanged,panel.SelectObjective);
            panel.conditions=Label(panel.conditionsPanel.transform,"Conditions",40,96,760,240,22);
            panel.startGame=MakeButton(panel.conditionsPanel.transform,"Start","この目標ではじめる",40,350);UnityEventTools.AddPersistentListener(panel.startGame.onClick,panel.StartNewGame);
            var back=MakeButton(panel.conditionsPanel.transform,"Back","戻る",440,350);UnityEventTools.AddPersistentListener(back.onClick,panel.ShowHome);
            panel.savesPanel=Box(background.transform,"Saved Games",220,260,840,420,new Color(.99f,.98f,.95f,1));
            Label(panel.savesPanel.transform,"Resume Title",40,28,760,48,26).text="保存したゲームを選んでください";
            panel.saves=Dropdown(panel.savesPanel.transform,"Saves",110);
            panel.saveDetails=Label(panel.savesPanel.transform,"Save Details",40,174,760,132,19);
            UnityEventTools.AddPersistentListener(panel.saves.onValueChanged,panel.SelectSave);
            panel.resume=MakeButton(panel.savesPanel.transform,"Resume","このゲームを再開",40,340);UnityEventTools.AddPersistentListener(panel.resume.onClick,panel.Resume);
            back=MakeButton(panel.savesPanel.transform,"Back","戻る",440,340);UnityEventTools.AddPersistentListener(back.onClick,panel.ShowHome);
            panel.notice=Label(background.transform,"Connection Notice",220,710,840,70,18);
            panel.ShowHome();EditorSceneManager.SaveScene(scene,TitlePath);
            var business=EditorSceneManager.OpenScene(BusinessPath);var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var bg=preview.transform.Find("Content Canvas/Background");
            foreach(string name in new[]{"New Game","New Game Entry","Return To Title","Title Save Hint"}){var old=bg.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
            var legacy=preview.GetComponent<PythonNewGamePanel>();if(legacy!=null)UnityEngine.Object.DestroyImmediate(legacy);
            var entry=Button(bg,"Return To Title","タイトルへ",600,24);
            var navigation=preview.GetComponent<PythonReturnToTitle>() ?? preview.gameObject.AddComponent<PythonReturnToTitle>();
            UnityEventTools.AddPersistentListener(entry.onClick,navigation.Return);
            Label(bg,"Title Save Hint",840,24,400,44,15).text="進捗を残す場合は、先に保存してください。";
            EditorSceneManager.MarkSceneDirty(business);EditorSceneManager.SaveScene(business);
            var builds=new List<EditorBuildSettingsScene>{new EditorBuildSettingsScene(TitlePath,true),new EditorBuildSettingsScene(BusinessPath,true)};
            foreach(var item in EditorBuildSettings.scenes)if(item.path!=TitlePath && item.path!=BusinessPath)builds.Add(item);
            EditorBuildSettings.scenes=builds.ToArray();EditorSceneManager.OpenScene(TitlePath);
            Debug.Log("CAT_CAFE_TITLE_UI_OK: title first / business second / serialized UI / legacy menu removed");
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
            Build();
            if(Camera.main==null || !Camera.main.isActiveAndEnabled || Camera.main.targetTexture!=null || Camera.main.targetDisplay!=0)throw new Exception("Title display camera missing");
            var title=UnityEngine.Object.FindObjectOfType<PythonTitleScreen>();
            var state=PythonServiceSetup.Sample();state.new_game_options=new[]{new PythonNewGameOption{choice="popularity",label="人気3段階",detail="20日以内に人気250 → 20日以内に人気450 → 25日以内に人気650",conditions="初期猫：5匹 / 席：2席 / 資金：1000"}};
            title.Apply(state);title.ApplySaves(new PythonSaveList{saves=new PythonSavedGame[0]});
            if(!title.newGame.interactable || !title.continueGame.interactable || title.resume.interactable)throw new Exception("Title initial guards failed");
            Canvas.ForceUpdateCanvases();Capture("Logs/TitleScreen.png");
            title.ShowConditions();if(!title.conditionsPanel.activeSelf || !title.conditions.text.Contains("1000"))throw new Exception("Conditions failed");
            Canvas.ForceUpdateCanvases();Capture("Logs/TitleConditions.png");
            title.ApplySaves(new PythonSaveList{saves=new[]{new PythonSavedGame{save_id=new string('a',32),label="1日目 / 資金1000"}}});
            title.ShowSaves();if(!title.resume.interactable || !title.savesPanel.activeSelf)throw new Exception("Resume failed");
            title.Disconnect("未接続");if(title.newGame.interactable || title.resume.interactable)throw new Exception("Offline guards failed");
            title.Apply(state);title.ShowHome();if(!title.home.activeSelf || title.conditionsPanel.activeSelf)throw new Exception("Back failed");
            if(EditorBuildSettings.scenes[0].path!=TitlePath || EditorBuildSettings.scenes[1].path!=BusinessPath)throw new Exception("Build order failed");
            Debug.Log("CAT_CAFE_TITLE_DISPLAY_OK");
        }
        static void Capture(string path)
        {
            var canvas=UnityEngine.Object.FindObjectOfType<Canvas>();var previous=canvas.renderMode;
            var camera=new GameObject("Capture Camera").AddComponent<Camera>();var target=new RenderTexture(1280,800,24);
            camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            Canvas.ForceUpdateCanvases();camera.Render();var prior=RenderTexture.active;RenderTexture.active=target;
            var texture=new Texture2D(1280,800,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,800),0,0);texture.Apply();
            System.IO.Directory.CreateDirectory("Logs");System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());RenderTexture.active=prior;
            canvas.renderMode=previous;canvas.worldCamera=null;UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(camera.gameObject);target.Release();UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
