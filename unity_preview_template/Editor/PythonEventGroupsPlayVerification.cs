using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CatCafePreview.Editor
{
    [InitializeOnLoad] public static class PythonEventGroupsPlayVerification
    {
        const string Key="CatCafeEventGroupsPlay";
        static PythonEventGroupsPlayVerification(){EditorApplication.update+=Update;}
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var serialized=new SerializedObject(UnityEngine.Object.FindObjectOfType<PythonStateClient>());int port=int.Parse(Environment.GetEnvironmentVariable("CAT_CAFE_EVENT_GROUPS_TEST_PORT"));Check(port>0 && port!=8190,"Use an isolated test server");serialized.FindProperty("port").intValue=port;serialized.ApplyModifiedPropertiesWithoutUndo();SessionState.SetInt(Key,1);SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+120);EditorApplication.EnterPlaymode();
        }
        static void Update()
        {
            int stage=SessionState.GetInt(Key,0);if(stage==0)return;
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0))throw new Exception("Event groups Play timeout "+stage);
                if(!EditorApplication.isPlaying)return;var client=UnityEngine.Object.FindObjectOfType<PythonStateClient>();if(client==null || !client.CanManageSaves)return;
                var state=client.CurrentState;var list=client.GetComponent<PythonEventListPanel>();var saves=client.GetComponent<PythonSavePanel>();
                if(stage==1)
                {
                    Check(list.PendingCount==7,"Expected intake, five growth choices and store");int revision=state.revision;list.open.onClick.Invoke();Check(list.SelectedGroup==0,"Pending priority");list.entries[1].onClick.Invoke();list.detailClose[1].onClick.Invoke();Check(list.panel.activeSelf && list.PendingCount==7 && state.revision==revision && !client.CanProgressBusiness,"Returning answered or unblocked event");Capture(client,"EventGroupsLivePending");
                    SessionState.SetString(Key+"instance",state.instance_id);SessionState.SetFloat(Key+"funds",state.funds);SessionState.SetString(Key+"cats",JsonUtility.ToJson(new PythonCafeState{cats=state.cats}));saves.save.onClick.Invoke();SessionState.SetInt(Key,2);
                }
                else if(stage==2 && saves.Count>0){SessionState.SetInt(Key+"loadRevision",state.revision);saves.saves.SetValueWithoutNotify(0);saves.ResumeSelected();SessionState.SetInt(Key,3);}
                else if(stage==3 && state.revision>SessionState.GetInt(Key+"loadRevision",0))
                {
                    Check(list.PendingCount==7 && !list.panel.activeSelf && list.SelectedGroup==0 && state.funds==SessionState.GetFloat(Key+"funds",0) && JsonUtility.ToJson(new PythonCafeState{cats=state.cats})==SessionState.GetString(Key+"cats",""),"Pending save restore mismatch");SessionState.SetInt(Key+"previous",7);SessionState.SetInt(Key,4);Answer(client,list);
                }
                else if(stage==4 && list.PendingCount<SessionState.GetInt(Key+"previous",0))
                {
                    int previous=SessionState.GetInt(Key+"previous",0);Check(list.PendingCount==previous-1,"Answer count did not decrease once");SessionState.SetInt(Key+"previous",list.PendingCount);
                    if(list.PendingCount>0)Answer(client,list);
                    else
                    {
                        Check(list.panel.activeSelf && list.GroupFor(1)==3 && !list.open.GetComponentInChildren<UnityEngine.UI.Text>().text.Contains("回答待ち"),"Final answer did not return or clear badge");list.tabs[3].onClick.Invoke();Check(list.entries[1].gameObject.activeSelf && list.entries[1].GetComponentInChildren<UnityEngine.UI.Text>().text.Contains("見送り済み"),"Resolved record missing");Capture(client,"EventGroupsLiveRecords");SessionState.SetInt(Key+"saves",saves.Count);SessionState.SetString(Key+"instance",state.instance_id);saves.save.onClick.Invoke();SessionState.SetInt(Key,5);
                    }
                }
                else if(stage==5 && saves.Count>SessionState.GetInt(Key+"saves",0)){SessionState.SetInt(Key+"loadRevision",state.revision);saves.saves.SetValueWithoutNotify(0);saves.ResumeSelected();SessionState.SetInt(Key,6);}
                else if(stage==6 && state.revision>SessionState.GetInt(Key+"loadRevision",0))
                {
                    Check(list.PendingCount==0 && list.GroupFor(1)==3 && list.SelectedGroup==1 && !list.panel.activeSelf,"Resolved save restore mismatch");list.open.onClick.Invoke();int revision=state.revision;foreach(var tab in list.tabs)tab.onClick.Invoke();Check(state.revision==revision,"Tabs changed gameplay");SessionState.SetInt(Key,0);Debug.Log("CAT_CAFE_EVENT_GROUPS_PLAY_OK: HTTP / seven overlapping answers / back without answering / pending save restore / count decreases / records / resolved save restore / read only tabs");EditorApplication.Exit(0);
                }
            }
            catch(Exception ex){SessionState.SetInt(Key,0);Debug.LogException(ex);EditorApplication.Exit(1);}
        }
        static void Answer(PythonStateClient client,PythonEventListPanel list)
        {
            list.open.onClick.Invoke();Check(list.SelectedGroup==0,"Lost pending priority");int route;UnityEngine.UI.Button button;
            if(client.CanRespondIntake(false)){route=1;button=client.GetComponent<PythonIntakePanel>().decline;}
            else if(client.CurrentState.growth_choices?.Length>0){route=3;int i=Array.FindIndex(client.CurrentState.growth_choices[0].choices,c=>c.can_select);Check(i>=0,"No growth answer");button=client.GetComponent<PythonGrowthPanel>().choices[i];}
            else{route=2;int i=Array.FindIndex(client.CurrentState.store_event.choices,c=>c.can_select && c.choice!="close");Check(i>=0,"No store answer");button=client.GetComponent<PythonStoreEventPanel>().choices[i];}
            list.entries[route].onClick.Invoke();Check(!list.panel.activeSelf && button.interactable && button.gameObject.activeInHierarchy,"Response not visible route="+route+" list="+list.panel.activeSelf+" enabled="+button.interactable+" visible="+button.gameObject.activeInHierarchy);button.onClick.Invoke();Check(!button.interactable,"Duplicate response remained enabled");
        }
        static void Capture(PythonStateClient client,string name){if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{client.GetComponent<ContentPreview>(),"Logs/"+name+".png"});}
    }
}
