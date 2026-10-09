using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CatCafePreview.Editor
{
    [InitializeOnLoad] public static class PythonRecruitmentVerification
    {
        const string Key="CatCafeRecruitmentPlay";
        static PythonRecruitmentVerification(){EditorApplication.update+=Update;}
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var serialized=new SerializedObject(UnityEngine.Object.FindObjectOfType<PythonStateClient>());serialized.FindProperty("port").intValue=int.Parse(Environment.GetEnvironmentVariable("CAT_CAFE_RECRUITMENT_TEST_PORT"));serialized.ApplyModifiedPropertiesWithoutUndo();
            SessionState.SetInt(Key,1);SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+120);EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            int stage=SessionState.GetInt(Key,0);if(stage==0)return;
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0))throw new Exception("Business history timeout "+stage);
                if(!EditorApplication.isPlaying)return;var client=UnityEngine.Object.FindObjectOfType<PythonStateClient>();if(client==null || !client.CanManageSaves)return;
                var state=client.CurrentState;var preview=client.GetComponent<ContentPreview>();var menu=client.GetComponent<PythonMenuPanel>();var p=client.GetComponent<PythonFinanceHistoryPanel>();var saves=client.GetComponent<PythonSavePanel>();preview.RefreshPythonControls();
                var recruitment=client.GetComponent<PythonRecruitmentPanel>();
                if(stage==1){menu.open.onClick.Invoke();menu.entries[16].onClick.Invoke();SessionState.SetInt(Key,2);}
                else if(stage==2)
                {
                    if(state.recruitment.rows.Length!=3)return;
                    if(!recruitment.panel.activeSelf || !recruitment.purchase.interactable)throw new Exception("HTTP candidate generation failed");
                    SessionState.SetString(Key+"cat",state.recruitment.rows[0].cat_id);SessionState.SetInt(Key+"funds",(int)state.recruitment.rows[0].funds_after);
                    recruitment.coat.value=1;recruitment.ResetFilters();recruitment.purchase.onClick.Invoke();recruitment.cancel.onClick.Invoke();recruitment.purchase.onClick.Invoke();recruitment.confirm.onClick.Invoke();SessionState.SetInt(Key,3);
                }
                else if(stage==3)
                {
                    var row=Array.Find(state.recruitment.rows,r=>r.cat_id==SessionState.GetString(Key+"cat",""));if(!row.accepted)return;
                    if(state.funds!=SessionState.GetInt(Key+"funds",0) || !Array.Exists(state.cats,r=>r.cat_id==row.cat_id))throw new Exception("HTTP join/funds mismatch");
                    recruitment.accepted.isOn=true;recruitment.Next();recruitment.Next();if(!recruitment.title.text.Contains("受け入れ済み") || recruitment.purchase.interactable)throw new Exception("Joined visibility/repeat guard failed");
                    recruitment.Close();saves.save.onClick.Invoke();SessionState.SetInt(Key,4);
                }
                else if(stage==4){if(saves.Count==0)return;saves.saves.SetValueWithoutNotify(0);saves.ResumeSelected();SessionState.SetInt(Key,5);}
                else if(stage==5)
                {
                    var row=Array.Find(state.recruitment.rows,r=>r.cat_id==SessionState.GetString(Key+"cat",""));if(row==null || !row.accepted)throw new Exception("Recruitment resume failed");
                    menu.Open();menu.entries[16].onClick.Invoke();recruitment.accepted.isOn=true;recruitment.Next();recruitment.Next();
                    Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/RecruitmentLive.png"});
                    Debug.Log("CAT_CAFE_RECRUITMENT_PLAY_OK: HTTP open / filter / cancel / confirm / join / repeat guard / save resume");SessionState.SetInt(Key,0);EditorApplication.Exit(0);
                }
            }
            catch(Exception ex){SessionState.SetInt(Key,0);Debug.LogException(ex);EditorApplication.Exit(1);}
        }
    }
}
