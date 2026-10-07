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
                    title.newGame.onClick.Invoke();title.objectives.value=3;title.SelectObjective(3);title.startGame.onClick.Invoke();
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
