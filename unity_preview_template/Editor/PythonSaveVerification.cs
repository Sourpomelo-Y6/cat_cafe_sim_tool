using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatCafePreview.Editor
{
    [InitializeOnLoad]
    public static class PythonSaveVerification
    {
        const string Key="CatCafeSaveTest";
        static PythonSaveVerification() { EditorApplication.update+=Update; }
        public static void VerifyRequiredAction()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>(); preview.Initialize();
            var state=PythonServiceSetup.Sample(); state.required_action="保護猫の受け入れ依頼に回答してください。";
            var service=preview.GetComponent<PythonServiceScreen>();
            foreach(var phase in new[]{"open","closed","preparation"})
            {
                state.closed=phase=="closed"; state.can_set_shifts=phase=="preparation";
                state.finance=state.closed ? new PythonFinanceState() : null;
                preview.ApplyPythonState(state);
                if(!preview.DisplayText.Contains(state.required_action))throw new Exception("Required action missing in "+phase);
                if(phase=="open")
                {
                    service.ShowCats(); if(!preview.DisplayText.Contains(state.required_action))throw new Exception("Required action missing on cat tab");
                    service.ShowSeats();
                }
            }
            Debug.Log("CAT_CAFE_REQUIRED_ACTION_OK: preparation / seats / cats / closed result");
        }
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
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0))throw new Exception("Save UI test timed out");
                if(!EditorApplication.isPlaying)return;
                var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>(); var client=preview.GetComponent<PythonStateClient>(); var panel=preview.GetComponent<PythonSavePanel>();
                if(!client.CanManageSaves)return;
                if(stage==1) { preview.GetSceneButton(2).onClick.Invoke(); SessionState.SetInt(Key,2); }
                else if(stage==2 && client.CurrentState.tick==1) { preview.GetSceneButton(2).onClick.Invoke(); SessionState.SetInt(Key,3); }
                else if(stage==3 && client.CurrentState.tick==2)
                {
                    SessionState.SetString(Key+"snapshot",JsonUtility.ToJson(client.CurrentState));
                    preview.GetSceneButton(3).onClick.Invoke(); if(preview.GetSceneButton(3).interactable)throw new Exception("Save busy guard failed"); SessionState.SetInt(Key,4);
                }
                else if(stage==4 && panel.Count>0) { preview.GetSceneButton(2).onClick.Invoke(); SessionState.SetInt(Key,5); }
                else if(stage==5 && client.CurrentState.tick==3) { panel.saves.SetValueWithoutNotify(0); panel.resume.onClick.Invoke(); SessionState.SetInt(Key,6); }
                else if(stage==6 && client.CurrentState.tick==2)
                {
                    var expected=JsonUtility.FromJson<PythonCafeState>(SessionState.GetString(Key+"snapshot","")); expected.revision=client.CurrentState.revision;
                    if(JsonUtility.ToJson(expected)!=JsonUtility.ToJson(client.CurrentState))throw new Exception("Active save resume mismatch");
                    SessionState.SetInt(Key,7);
                }
                else if(stage==7)
                {
                    if(!client.CurrentState.closed)preview.GetSceneButton(2).onClick.Invoke();
                    else { preview.GetSceneButton(3).onClick.Invoke(); SessionState.SetInt(Key,8); }
                }
                else if(stage==8 && panel.Count>=2)
                {
                    if(!preview.GetSceneButton(2).interactable || preview.GetSceneButton(2).GetComponentInChildren<UnityEngine.UI.Text>().text!="翌日へ")
                        throw new Exception("Next day button missing");
                    preview.GetSceneButton(2).onClick.Invoke(); SessionState.SetInt(Key,9);
                }
                else if(stage==9 && client.CurrentState.day==2 && client.CurrentState.can_set_shifts)
                {
                    if(preview.GetComponent<PythonServiceScreen>().ShowingSeats || !client.CanChangeShifts)
                        throw new Exception("Next day roster preparation missing");
                    var cat=Array.Find(client.CurrentState.cats,item=>item.working);
                    client.SetCatShift(cat.cat_id,false); SessionState.SetString(Key+"resting",cat.cat_id); SessionState.SetInt(Key,10);
                }
                else if(stage==10 && !Array.Find(client.CurrentState.cats,item=>item.cat_id==SessionState.GetString(Key+"resting","")).working)
                {
                    SessionState.SetString(Key+"day2",JsonUtility.ToJson(client.CurrentState));
                    preview.GetSceneButton(3).onClick.Invoke(); SessionState.SetInt(Key,11);
                }
                else if(stage==11 && panel.Count>=3) { preview.GetSceneButton(2).onClick.Invoke(); SessionState.SetInt(Key,12); }
                else if(stage==12 && client.CurrentState.day==2 && client.CurrentState.tick>0)
                { panel.saves.SetValueWithoutNotify(0); panel.resume.onClick.Invoke(); SessionState.SetInt(Key,13); }
                else if(stage==13 && client.CurrentState.day==2 && client.CurrentState.tick==0)
                {
                    var expected=JsonUtility.FromJson<PythonCafeState>(SessionState.GetString(Key+"day2","")); expected.revision=client.CurrentState.revision;
                    if(JsonUtility.ToJson(expected)!=JsonUtility.ToJson(client.CurrentState))throw new Exception("Next day save resume mismatch");
                    Debug.Log("CAT_CAFE_SAVE_PLAY_OK: active and closed save / next day button / preparation roster / shift settings / day two save and resume");
                    SessionState.SetInt(Key,0); EditorApplication.ExitPlaymode(); EditorApplication.delayCall+=()=>EditorApplication.Exit(0);
                }
            }
            catch(Exception ex) { SessionState.SetInt(Key,0); Debug.LogException(ex); EditorApplication.Exit(1); }
        }
    }
}
