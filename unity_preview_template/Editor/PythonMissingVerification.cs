using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CatCafePreview.Editor
{
    [InitializeOnLoad] public static class PythonMissingVerification
    {
        const string Key="CatCafeMissingPlay";
        static PythonMissingVerification() { EditorApplication.update+=Update; }
        public static void Run()
        { SessionState.SetBool(Key+"rest",false);Start(); }
        public static void RunRest()
        { SessionState.SetBool(Key+"rest",true);Start(); }
        static void Start()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var serialized=new SerializedObject(UnityEngine.Object.FindObjectOfType<PythonStateClient>());serialized.FindProperty("port").intValue=8191;serialized.ApplyModifiedPropertiesWithoutUndo();
            SessionState.SetInt(Key,1);SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+240);EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            int stage=SessionState.GetInt(Key,0);if(stage==0)return;
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0))throw new Exception("Missing timeout stage "+stage);
                if(!EditorApplication.isPlaying)return;
                var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var client=preview.GetComponent<PythonStateClient>();if(!client.CanManageSaves)return;
                preview.RefreshPythonControls();var state=client.CurrentState;var panel=preview.GetComponent<PythonMissingPanel>();var detail=preview.GetComponent<PythonCatDetailsPanel>();var saves=preview.GetComponent<PythonSavePanel>();
                var row=Array.Find(state.missing_cats,r=>r.cat_id=="cat-sora");Action<int> move=value=>SessionState.SetInt(Key,value);
                if(stage==1)
                {
                    if(row.status!="missing" || row.remaining!=2 || !panel.panel.activeSelf || panel.confirm.interactable || row.kitten)throw new Exception("Missing initial state failed");
                    panel.Close();detail.Open("cat-sora");detail.missing.onClick.Invoke();if(!panel.panel.activeSelf || detail.panel.activeSelf)throw new Exception("Details entry failed");
                    Capture(preview,"Logs/MissingLiveAbsent.png");SessionState.SetString(Key+"missing",JsonUtility.ToJson(row));preview.GetSceneButton(3).onClick.Invoke();move(2);
                }
                else if(stage==2 && saves.Count==1) { saves.saves.value=0;saves.resume.onClick.Invoke();move(3); }
                else if(stage==3)
                {
                    if(JsonUtility.ToJson(row)!=SessionState.GetString(Key+"missing",""))throw new Exception("Missing save mismatch");panel.Close();move(4);
                }
                else if(stage==4)
                {
                    if(row.status=="waiting")
                    {
                        if(!panel.panel.activeSelf || !panel.confirm.interactable || panel.close.interactable || client.CanProgressBusiness || row.cost!=250 || !row.kitten)throw new Exception("Return waiting failed");
                        Capture(preview,"Logs/MissingLiveReturn.png");SessionState.SetString(Key+"waiting",JsonUtility.ToJson(row));preview.GetSceneButton(3).onClick.Invoke();move(5);
                    }
                    else
                    {
                        if(SessionState.GetBool(Key+"rest",false) && state.can_set_shifts)
                        {
                            panel.Open("cat-sora");if(!panel.rest.interactable)throw new Exception("Rest entry disabled");
                            Capture(preview,"Logs/MissingLiveRest.png");panel.rest.onClick.Invoke();if(panel.rest.interactable)throw new Exception("Busy rest enabled");return;
                        }
                        panel.Close();
                        if(client.CanProgressBusiness)client.ProgressBusiness();
                        else if(state.can_set_shifts)
                        { var cat=Array.Find(state.cats,r=>r.activity=="cafe" && r.health_status=="healthy" && r.stamina>0);if(cat==null)throw new Exception("No working cat for verification");client.SetCatShift(cat.cat_id,true); }
                    }
                }
                else if(stage==5 && saves.Count==2) { saves.saves.value=1;saves.resume.onClick.Invoke();move(6); }
                else if(stage==6 && row.status=="missing") { saves.saves.value=0;saves.resume.onClick.Invoke();move(7); }
                else if(stage==7 && row.status=="waiting")
                {
                    if(JsonUtility.ToJson(row)!=SessionState.GetString(Key+"waiting","") || !panel.panel.activeSelf)throw new Exception("Waiting restore mismatch");
                    SessionState.SetFloat(Key+"funds",state.funds);panel.confirm.onClick.Invoke();if(panel.confirm.interactable)throw new Exception("Busy return enabled");move(8);
                }
                else if(stage==8 && row.status=="resolved")
                {
                    var cat=Array.Find(state.cats,r=>r.cat_id=="cat-sora");
                    if(state.funds!=SessionState.GetFloat(Key+"funds",0)-250 || cat.activity!="cafe" || cat.working || cat.stamina!=cat.max_stamina || cat.stress!=10 || panel.confirm.interactable || !panel.close.interactable)throw new Exception("Return state mismatch");
                    Capture(preview,"Logs/MissingLiveResult.png");SessionState.SetString(Key+"resolved",JsonUtility.ToJson(row));preview.GetSceneButton(3).onClick.Invoke();move(9);
                }
                else if(stage==9 && saves.Count==3) { saves.saves.value=1;saves.resume.onClick.Invoke();move(10); }
                else if(stage==10 && row.status=="waiting") { saves.saves.value=0;saves.resume.onClick.Invoke();move(11); }
                else if(stage==11 && row.status=="resolved")
                {
                    if(JsonUtility.ToJson(row)!=SessionState.GetString(Key+"resolved","") || panel.confirm.interactable)throw new Exception("Resolved restore failed");
                    panel.Close();
                    if(SessionState.GetBool(Key+"rest",false))
                    {
                        var card=preview.GetComponent<PythonCatRoster>().GetCard(Array.FindIndex(state.cats,r=>r.cat_id=="cat-sora"));
                        if(!card.work.interactable)throw new Exception("Returned cat work entry disabled");card.work.onClick.Invoke();move(12);return;
                    }
                    if(!client.CanProgressBusiness)throw new Exception("Business resume failed");
                    panel.Close();Debug.Log("CAT_CAFE_MISSING_PLAY_OK: "+(SessionState.GetBool(Key+"rest",false) ? "rest without working cats / " : "business / ")+"missing / entry / two closing days / waiting / cost / stamina / stress / roster / three save stages / busy / resume business");SessionState.SetInt(Key,0);EditorApplication.Exit(0);
                }
                else if(stage==12)
                {
                    if(!Array.Find(state.cats,r=>r.cat_id=="cat-sora").working || !client.CanProgressBusiness)throw new Exception("Business remained blocked after rest and return");
                    Debug.Log("CAT_CAFE_MISSING_PLAY_OK: rest without working cats / entry / two closing days / waiting / cost / stamina / stress / roster work button / three save stages / busy / resume business");SessionState.SetInt(Key,0);EditorApplication.Exit(0);
                }
            }
            catch(Exception ex) { SessionState.SetInt(Key,0);Debug.LogException(ex);EditorApplication.Exit(1); }
        }
        static void Capture(ContentPreview preview,string path)
        { Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,path}); }
    }
}
