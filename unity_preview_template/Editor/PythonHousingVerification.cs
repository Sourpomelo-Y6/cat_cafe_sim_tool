using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatCafePreview.Editor
{
    [InitializeOnLoad]
    public static class PythonHousingVerification
    {
        const string Key="CatCafeHousingTest";
        static PythonHousingVerification() { EditorApplication.update+=Update; }
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var client=UnityEngine.Object.FindObjectOfType<PythonStateClient>(); var serialized=new SerializedObject(client);
            serialized.FindProperty("port").intValue=8191; serialized.ApplyModifiedPropertiesWithoutUndo();
            SessionState.SetInt(Key,1); SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+120); EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            int stage=SessionState.GetInt(Key,0); if(stage==0)return;
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0))throw new Exception("Housing test timed out, stage "+stage);
                if(!EditorApplication.isPlaying)return;
                var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>(); var client=preview.GetComponent<PythonStateClient>();
                if(!client.CanManageSaves)return;
                var state=client.CurrentState; var panel=preview.GetComponent<PythonHousingPanel>();
                var intake=preview.GetComponent<PythonIntakePanel>(); var saves=preview.GetComponent<PythonSavePanel>();
                if(stage==1)
                {
                    if(state.housing.free!=0 || !intake.panel.activeSelf || intake.accept.interactable || !panel.open.interactable)throw new Exception("Full intake mismatch");
                    panel.open.onClick.Invoke(); if(!panel.panel.activeSelf || !panel.expand.interactable)throw new Exception("Housing entry failed");
                    SessionState.SetString(Key+"before",JsonUtility.ToJson(state)); preview.GetSceneButton(3).onClick.Invoke(); SessionState.SetInt(Key,2);
                }
                else if(stage==2 && saves.Count==1)
                {
                    panel.expand.onClick.Invoke();panel.confirm.onClick.Invoke(); if(panel.expand.interactable)throw new Exception("Housing busy guard failed"); SessionState.SetInt(Key,3);
                }
                else if(stage==3 && state.housing.stage==1 && intake.accept.interactable)
                {
                    var before=JsonUtility.FromJson<PythonCafeState>(SessionState.GetString(Key+"before",""));
                    if(state.funds!=before.housing.funds_after || state.housing.capacity!=before.housing.next_capacity || !intake.accept.interactable)
                        throw new Exception("Expansion did not enable intake");
                    SessionState.SetString(Key+"expanded",JsonUtility.ToJson(state)); preview.GetSceneButton(3).onClick.Invoke(); SessionState.SetInt(Key,4);
                }
                else if(stage==4 && saves.Count==2) { saves.saves.SetValueWithoutNotify(1); saves.resume.onClick.Invoke(); SessionState.SetInt(Key,5); }
                else if(stage==5 && state.housing.stage==0)
                {
                    CheckRestored("before",state); if(intake.accept.interactable)throw new Exception("Full save enabled intake");
                    saves.saves.SetValueWithoutNotify(0); saves.resume.onClick.Invoke(); SessionState.SetInt(Key,6);
                }
                else if(stage==6 && state.housing.stage==1)
                { CheckRestored("expanded",state); panel.Open();panel.expand.onClick.Invoke();panel.confirm.onClick.Invoke(); SessionState.SetInt(Key,7); }
                else if(stage==7 && state.housing.stage==2)
                {
                    if(panel.expand.interactable || client.CanExpandHousing)throw new Exception("Completed housing expansion remains enabled");
                    SessionState.SetString(Key+"upgraded",JsonUtility.ToJson(state)); preview.GetSceneButton(3).onClick.Invoke(); SessionState.SetInt(Key,8);
                }
                else if(stage==8 && saves.Count==3) { saves.saves.SetValueWithoutNotify(1); saves.resume.onClick.Invoke(); SessionState.SetInt(Key,9); }
                else if(stage==9 && state.housing.stage==1)
                { CheckRestored("expanded",state); saves.saves.SetValueWithoutNotify(0); saves.resume.onClick.Invoke(); SessionState.SetInt(Key,10); }
                else if(stage==10 && state.housing.stage==2 && intake.accept.interactable)
                {
                    CheckRestored("upgraded",state); panel.close.onClick.Invoke();
                    if(panel.panel.activeSelf || !intake.panel.activeSelf || !intake.accept.interactable)throw new Exception("Return to intake failed");
                    intake.accept.onClick.Invoke(); SessionState.SetInt(Key,11);
                }
                else if(stage==11 && state.intake_request.status=="accepted")
                {
                    if(state.housing.count!=7 || !client.CanProgressBusiness || intake.panel.activeSelf)throw new Exception("Expanded intake acceptance failed");
                    Debug.Log("CAT_CAFE_HOUSING_PLAY_OK: full intake / housing entry / both expansion stages / funds / busy guard / save restore before and after / return to intake / accept");
                    SessionState.SetInt(Key,0); EditorApplication.ExitPlaymode(); EditorApplication.delayCall+=()=>EditorApplication.Exit(0);
                }
            }
            catch(Exception ex) { SessionState.SetInt(Key,0); Debug.LogException(ex); EditorApplication.Exit(1); }
        }
        static void CheckRestored(string name,PythonCafeState state)
        {
            var expected=JsonUtility.FromJson<PythonCafeState>(SessionState.GetString(Key+name,"")); expected.revision=state.revision;
            if(JsonUtility.ToJson(expected)!=JsonUtility.ToJson(state))throw new Exception("Housing restore mismatch: "+name);
        }
    }
}
