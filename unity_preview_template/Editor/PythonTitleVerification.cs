using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CatCafePreview.Editor
{
    [InitializeOnLoad]
    public static class PythonTitleVerification
    {
        const string Key="CatCafeTitlePlay";
        static PythonTitleVerification(){EditorApplication.update+=Update;}
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var serialized=new SerializedObject(UnityEngine.Object.FindObjectOfType<PythonStateClient>());
            serialized.FindProperty("port").intValue=8191;serialized.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafeTitle.unity");
            UnityEngine.Object.FindObjectOfType<PythonTitleScreen>().port=8191;EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            SessionState.SetInt(Key,1);SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+100);EditorApplication.EnterPlaymode();
        }
        public static void RunAll()
        {
            Run();SessionState.SetInt(Key,21);SessionState.SetInt(Key+"mode",0);
            SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+240);
        }
        public static void VerifyPanels()
        {
            PythonGrowthSetup.BuildAndCapture();PythonCatDetailsSetup.BuildAndVerify();
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafeTitle.unity");
            var title=UnityEngine.Object.FindObjectOfType<PythonTitleScreen>();title.ShowHome();title.Disconnect("Python未接続です。状態サーバーを起動してください。");
            if(title.newGame.interactable || title.continueGame.interactable || title.startGame.interactable || title.resume.interactable)throw new Exception("Offline title guard failed");
            Canvas.ForceUpdateCanvases();typeof(PythonTitleSetup).GetMethod("Capture",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{"Logs/FlowOfflineTitle.png"});
            Debug.Log("CAT_CAFE_FLOW_PANELS_OK: growth / mastery / details / offline title");
        }
        public static void RunDropdown()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafeTitle.unity");
            SessionState.SetInt(Key,10);SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+30);EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            int stage=SessionState.GetInt(Key,0);if(stage==0)return;
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0))throw new Exception("Title Play timeout stage "+stage);
                if(!EditorApplication.isPlaying)return;
                var title=UnityEngine.Object.FindObjectOfType<PythonTitleScreen>();
                var client=UnityEngine.Object.FindObjectOfType<PythonStateClient>();
                if(stage>=21 && stage<=27)
                {
                    int mode=SessionState.GetInt(Key+"mode",0);
                    string objective=new[]{"popularity","patron","bond","free"}[mode];
                    if(stage==21 && title!=null && title.newGame.interactable)
                    {
                        title.newGame.onClick.Invoke();title.objectives.value=mode;title.SelectObjective(mode);
                        Canvas.ForceUpdateCanvases();
                        if(title.conditions.preferredHeight>title.conditions.rectTransform.rect.height)throw new Exception("Conditions text clipped: "+objective);
                        if(Camera.main==null || !Camera.main.isActiveAndEnabled)throw new Exception("Title camera missing");
                        title.startGame.onClick.Invoke();title.startGame.onClick.Invoke();SessionState.SetInt(Key,22);
                    }
                    else if(stage==22 && client!=null && client.CanManageSaves)
                    {
                        if(client.CurrentState.objective!=objective || client.CurrentState.tick!=0 || !client.CanProgressBusiness)throw new Exception("New game mismatch: "+objective);
                        client.GetComponent<ContentPreview>().GetSceneButton(2).onClick.Invoke();SessionState.SetInt(Key,23);
                    }
                    else if(stage==23 && client!=null && client.CanManageSaves && !client.CurrentState.can_set_shifts)
                    {client.GetComponent<ContentPreview>().GetSceneButton(2).onClick.Invoke();SessionState.SetInt(Key,24);}
                    else if(stage==24 && client!=null && client.CanManageSaves && client.CurrentState.tick>0)
                    {
                        var preview=client.GetComponent<ContentPreview>();var detail=client.GetComponent<PythonCatDetailsPanel>();
                        var roster=client.GetComponent<PythonCatRoster>();roster.GetCard(0).details.onClick.Invoke();
                        if(!detail.panel.activeSelf || !detail.details.text.Contains("成長"))throw new Exception("Live cat details missing");
                        Canvas.ForceUpdateCanvases();
                        typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/FlowDetails-"+objective+".png"});
                        detail.Close();
                        var progress=client.CurrentState.objective_progress;
                        var heading=preview.GetComponent<PythonServiceScreen>().heading;
                        Canvas.ForceUpdateCanvases();
                        if(progress==null || progress.mode!=objective || !heading.text.Contains(progress.summary) || heading.preferredHeight>heading.rectTransform.rect.height || !preview.DisplayText.Contains(progress.details))throw new Exception("Live objective progress missing or clipped: "+objective);
                        typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/Objective-"+objective+".png"});
                        SessionState.SetInt(Key+"tick",client.CurrentState.tick);SessionState.SetFloat(Key+"funds",client.CurrentState.funds);
                        SessionState.SetInt(Key+"count",client.GetComponent<PythonSavePanel>().Count);
                        preview.GetSceneButton(3).onClick.Invoke();SessionState.SetInt(Key,25);
                    }
                    else if(stage==25 && client!=null && client.CanManageSaves && client.GetComponent<PythonSavePanel>().Count>SessionState.GetInt(Key+"count",0))
                    {client.GetComponent<PythonReturnToTitle>().Return();SessionState.SetInt(Key,26);}
                    else if(stage==26 && title!=null && title.continueGame.interactable)
                    {title.continueGame.onClick.Invoke();title.saves.value=0;title.resume.onClick.Invoke();SessionState.SetInt(Key,27);}
                    else if(stage==27 && client!=null && client.CanManageSaves)
                    {
                        if(client.CurrentState.objective!=objective || client.CurrentState.tick!=SessionState.GetInt(Key+"tick",0) || client.CurrentState.funds!=SessionState.GetFloat(Key+"funds",0) || client.Automatic)throw new Exception("Saved progress mismatch: "+objective);
                        Debug.Log("CAT_CAFE_FULL_FLOW_MODE_OK: "+objective+" / new / business / tick / details / save / return / resume");
                        if(mode<3){SessionState.SetInt(Key+"mode",mode+1);SessionState.SetInt(Key,21);client.GetComponent<PythonReturnToTitle>().Return();}
                        else {SessionState.SetInt(Key,0);Debug.Log("CAT_CAFE_FULL_FLOW_OK: all four objectives");EditorApplication.Exit(0);}
                    }
                    return;
                }
                if(stage==10 && title!=null)
                {
                    var state=PythonServiceSetup.Sample();
                    state.new_game_options=new[]{
                        new PythonNewGameOption{choice="popularity",label="人気3段階"},
                        new PythonNewGameOption{choice="patron",label="有力者の満足度"},
                        new PythonNewGameOption{choice="bond",label="猫6匹との好感度"},
                        new PythonNewGameOption{choice="free",label="自由営業"}};
                    title.Apply(state);title.ShowConditions();SessionState.SetInt(Key,12);return;
                }
                if(stage==12 && title!=null)
                {
                    if(Time.frameCount<3)return;
                    title.ShowConditions();title.objectives.Show();SessionState.SetInt(Key,11);return;
                }
                if(stage==11 && title!=null)
                {
                    Canvas.ForceUpdateCanvases();
                    var list=title.objectives.transform.Find("Dropdown List");if(list==null)throw new Exception("Dropdown did not open");
                    if(list.GetComponent<CanvasGroup>().alpha<.99f)return;
                    int visible=0;
                    foreach(var text in list.GetComponentsInChildren<UnityEngine.UI.Text>())
                    {
                        if(text.gameObject.name!="Item Label")continue;
                        if(text.rectTransform.rect.height<30 || text.cachedTextGenerator.vertexCount==0)throw new Exception("Dropdown row text invisible: "+text.text);
                        visible++;
                    }
                    if(visible!=4)throw new Exception("Dropdown option count mismatch: "+visible);
                    typeof(PythonTitleSetup).GetMethod("Capture",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{"Logs/TitleDropdown.png"});
                    Debug.Log("CAT_CAFE_TITLE_DROPDOWN_OK: four visible option labels / sufficient row height / real dropdown opened");
                    SessionState.SetInt(Key,0);EditorApplication.ExitPlaymode();EditorApplication.delayCall+=()=>EditorApplication.Exit(0);return;
                }
                if(stage==1 && title!=null && title.newGame.interactable)
                {
                    if(!title.home.activeSelf)throw new Exception("Title entry failed");
                    title.newGame.onClick.Invoke();title.objectives.value=3;title.SelectObjective(3);title.startGame.onClick.Invoke();title.startGame.onClick.Invoke();
                    if(title.startGame.interactable)throw new Exception("Pending start guard failed");SessionState.SetInt(Key,2);
                }
                else if(stage==2 && client!=null && client.CanManageSaves)
                {
                    if(client.CurrentState.objective!="free" || client.Automatic || UnityEngine.Object.FindObjectOfType<PythonNewGamePanel>()!=null)throw new Exception("Business transition failed");
                    UnityEngine.Object.FindObjectOfType<PythonReturnToTitle>().Return();SessionState.SetInt(Key,3);
                }
                else if(stage==3 && title!=null && title.continueGame.interactable)
                {
                    title.continueGame.onClick.Invoke();title.resume.onClick.Invoke();SessionState.SetInt(Key,4);
                }
                else if(stage==4 && client!=null && client.CanManageSaves)
                {
                    if(client.CurrentState.objective!="free" || client.Automatic)throw new Exception("Resume transition failed");
                    Debug.Log("CAT_CAFE_TITLE_PLAY_OK: real buttons / create / busy / business scene / return / saved resume");
                    SessionState.SetInt(Key,0);EditorApplication.ExitPlaymode();EditorApplication.delayCall+=()=>EditorApplication.Exit(0);
                }
            }
            catch(Exception ex){SessionState.SetInt(Key,0);Debug.LogException(ex);EditorApplication.Exit(1);}
        }
    }
}
