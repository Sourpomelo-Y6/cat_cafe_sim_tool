using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CatCafePreview.Editor
{
    [InitializeOnLoad] public static class PythonAdoptionVerification
    {
        const string Key="CatCafeAdoptionPlay";
        static PythonAdoptionVerification() { EditorApplication.update+=Update; }
        public static void Run()
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
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0))throw new Exception("Adoption timeout stage "+stage);
                if(!EditorApplication.isPlaying)return;
                var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var client=preview.GetComponent<PythonStateClient>();if(!client.CanManageSaves)return;
                preview.RefreshPythonControls();var state=client.CurrentState;var panel=preview.GetComponent<PythonAdoptionPanel>();var saves=preview.GetComponent<PythonSavePanel>();
                var row=state.adoption.events.Length>0 ? state.adoption.events[0] : null;Action<int> move=value=>SessionState.SetInt(Key,value);
                if(stage==1)
                {
                    var detail=preview.GetComponent<PythonCatDetailsPanel>();detail.Open(state.cats[0].cat_id);detail.adoption.onClick.Invoke();
                    if(!panel.panel.activeSelf || state.adoption.enabled || !panel.configure.interactable)throw new Exception("Settings entry failed");panel.configure.onClick.Invoke();if(panel.configure.interactable)throw new Exception("Busy settings enabled");move(2);
                }
                else if(stage==2 && state.adoption.enabled) { preview.GetSceneButton(3).onClick.Invoke();move(3); }
                else if(stage==3 && saves.Count==1) { panel.Close();client.ProgressBusiness();move(4); }
                else if(stage==4)
                {
                    if(row==null) { if(client.CanProgressBusiness)client.ProgressBusiness(); }
                    else
                    {
                        if(row.status!="waiting" || !panel.panel.activeSelf || !panel.accept.interactable || !panel.decline.interactable || panel.close.interactable || client.CanProgressBusiness)throw new Exception("Waiting offer failed");
                        SessionState.SetString(Key+"cat",row.cat_id);SessionState.SetFloat(Key+"funds",state.funds);SessionState.SetInt(Key+"tick",state.tick);
                        Capture(preview,"Logs/AdoptionLiveOffer.png");preview.GetSceneButton(3).onClick.Invoke();move(5);
                    }
                }
                else if(stage==5 && saves.Count==2) { panel.decline.onClick.Invoke();if(panel.decline.interactable)throw new Exception("Busy decline enabled");move(6); }
                else if(stage==6 && row.status=="resolved")
                {
                    if(row.choice!="decline" || Array.Find(state.cats,r=>r.cat_id==row.cat_id).activity!="cafe" || panel.accept.interactable || state.tick!=SessionState.GetInt(Key+"tick",0) || state.funds!=SessionState.GetFloat(Key+"funds",0))throw new Exception("Decline changed cat or business");
                    preview.GetSceneButton(3).onClick.Invoke();move(7);
                }
                else if(stage==7 && saves.Count==3) { saves.saves.value=1;saves.resume.onClick.Invoke();move(8); }
                else if(stage==8 && row.status=="waiting")
                {
                    panel.accept.onClick.Invoke();if(!panel.confirmation.activeSelf || !client.CanManageSaves || row.status!="waiting")throw new Exception("First click submitted adoption");
                    Capture(preview,"Logs/AdoptionLiveConfirmation.png");panel.cancel.onClick.Invoke();if(panel.confirmation.activeSelf || !panel.decline.interactable)throw new Exception("Cancel failed");
                    panel.accept.onClick.Invoke();saves.saves.value=1;saves.resume.onClick.Invoke();move(9);
                }
                else if(stage==9 && row.status=="waiting")
                {
                    if(panel.confirmation.activeSelf)throw new Exception("Load preserved stale confirmation");panel.accept.onClick.Invoke();panel.confirmAccept.onClick.Invoke();if(panel.confirmAccept.interactable)throw new Exception("Busy final adoption enabled");move(10);
                }
                else if(stage==10 && row.status=="resolved")
                {
                    var cat=Array.Find(state.cats,r=>r.cat_id==row.cat_id);
                    if(row.choice!="accept" || cat.activity!="adopted" || cat.working || cat.can_player_start || panel.accept.interactable || panel.confirmation.activeSelf || state.funds!=SessionState.GetFloat(Key+"funds",0) || state.tick!=SessionState.GetInt(Key+"tick",0))throw new Exception("Adoption result failed");
                    Capture(preview,"Logs/AdoptionLiveResult.png");preview.GetSceneButton(3).onClick.Invoke();move(11);
                }
                else if(stage==11 && saves.Count==4) { saves.saves.value=1;saves.resume.onClick.Invoke();move(12); }
                else if(stage==12 && row.choice=="decline")
                { if(Array.Find(state.cats,r=>r.cat_id==row.cat_id).activity!="cafe")throw new Exception("Decline restore failed");saves.saves.value=0;saves.resume.onClick.Invoke();move(13); }
                else if(stage==13 && row.choice=="accept")
                { if(Array.Find(state.cats,r=>r.cat_id==row.cat_id).activity!="adopted" || panel.accept.interactable)throw new Exception("Accept restore failed");panel.Close();move(14); }
                else if(stage==14)
                {
                    if(state.can_set_shifts) { panel.Open(row.cat_id);panel.configure.onClick.Invoke();move(15); }
                    else if(client.CanProgressBusiness)client.ProgressBusiness();
                }
                else if(stage==15 && !state.adoption.enabled)
                {
                    if(row.choice!="accept" || row.status!="resolved" || panel.accept.interactable || !panel.configure.interactable)throw new Exception("OFF deleted adoption history");
                    Debug.Log("CAT_CAFE_ADOPTION_PLAY_OK: entry / ON / offer / decline / confirmation / cancel / reload invalidates confirmation / accept / roster / waiting and both result saves / business / OFF preserves history");SessionState.SetInt(Key,0);EditorApplication.Exit(0);
                }
            }
            catch(Exception ex) { SessionState.SetInt(Key,0);Debug.LogException(ex);EditorApplication.Exit(1); }
        }
        static void Capture(ContentPreview preview,string path)
        { Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,path}); }
    }
}
