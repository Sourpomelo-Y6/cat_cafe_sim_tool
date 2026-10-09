using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CatCafePreview.Editor
{
    [InitializeOnLoad] public static class PythonCatVisibilityVerification
    {
        const string Key="CatCafeVisibilityPlay";
        static PythonCatVisibilityVerification(){EditorApplication.update+=Update;}
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var serialized=new SerializedObject(UnityEngine.Object.FindObjectOfType<PythonStateClient>());serialized.FindProperty("port").intValue=int.Parse(Environment.GetEnvironmentVariable("CAT_CAFE_VISIBILITY_TEST_PORT"));serialized.ApplyModifiedPropertiesWithoutUndo();
            SessionState.SetInt(Key,1);SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+90);EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            int stage=SessionState.GetInt(Key,0);if(stage==0)return;
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0))throw new Exception("Visibility play timeout "+stage);
                if(!EditorApplication.isPlaying)return;var client=UnityEngine.Object.FindObjectOfType<PythonStateClient>();if(client==null || !client.CanManageSaves)return;
                var state=client.CurrentState;var preview=client.GetComponent<ContentPreview>();var visibility=client.GetComponent<PythonCatVisibility>();var roster=client.GetComponent<PythonCatRoster>();var prep=client.GetComponent<PythonPreparationPanel>();preview.RefreshPythonControls();
                if(stage==1)
                {
                    var adopted=Array.Find(state.cats,cat=>cat.activity=="adopted");if(adopted==null || roster.Count!=state.cats.Length-1)throw new Exception("Real adoption not hidden");
                    SessionState.SetInt(Key+"revision",state.revision);
                    visibility.preparationToggle.isOn=true;if(roster.Count!=state.cats.Length || !visibility.rosterToggle.isOn)throw new Exception("Real toggle synchronization failed");
                    int index=Array.FindIndex(preview.VisiblePythonCats,cat=>cat.cat_id==adopted.cat_id);preview.SelectCat(index);prep.cats.value=index;
                    if(prep.work.interactable || prep.rest.interactable)throw new Exception("Real adopted shift enabled");
                    roster.GetCard(index).details.onClick.Invoke();var details=client.GetComponent<PythonCatDetailsPanel>();details.EventRecords();if(!details.panel.activeSelf || !details.details.text.Contains("譲渡"))throw new Exception("Adopted history lost");
                    details.Close();visibility.rosterToggle.isOn=false;
                    if(state.revision!=SessionState.GetInt(Key+"revision",0) || !prep.work.interactable && !prep.rest.interactable || preview.SelectedPythonCatId==adopted.cat_id)throw new Exception("Visibility changed game or retained hidden selection");
                    prep.cats.value=1;var expected=preview.VisiblePythonCats[1];SessionState.SetString(Key+"cat",expected.cat_id);SessionState.SetBool(Key+"working",expected.working);
                    if(expected.working)prep.rest.onClick.Invoke();else prep.work.onClick.Invoke();
                    if(prep.work.interactable || prep.rest.interactable)throw new Exception("Mapped command busy guard failed");SessionState.SetInt(Key,2);
                }
                else if(stage==2)
                {
                    var cat=Array.Find(state.cats,row=>row.cat_id==SessionState.GetString(Key+"cat",""));if(cat.working==SessionState.GetBool(Key+"working",false))throw new Exception("Filtered shift targeted wrong cat");
                    if(Array.Find(state.cats,row=>row.activity=="adopted")==null || roster.Count!=state.cats.Length-1)throw new Exception("Adoption lost after shift");
                    prep.Open();Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/CatVisibilityLive.png"});
                    Debug.Log("CAT_CAFE_CAT_VISIBILITY_PLAY_OK: real adoption / toggle buttons / history retained / filtered selection / HTTP shift / busy guard / no toggle commands");
                    SessionState.SetInt(Key,0);EditorApplication.Exit(0);
                }
            }
            catch(Exception ex){SessionState.SetInt(Key,0);Debug.LogException(ex);EditorApplication.Exit(1);}
        }
    }
}
