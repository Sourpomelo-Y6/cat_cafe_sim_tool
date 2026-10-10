using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CatCafePreview.Editor
{
    [InitializeOnLoad] public static class PythonMultiDayVerification
    {
        const string Key="CatCafeMultiDayPlay";
        static int TestPort
        {
            get { string value=Environment.GetEnvironmentVariable("CAT_CAFE_FLOW_TEST_PORT");int port=string.IsNullOrEmpty(value) ? 8191 : int.Parse(value);if(port<=0 || port>65535 || port==8190)throw new Exception("Use an isolated flow-test server");return port; }
        }
        static PythonMultiDayVerification(){EditorApplication.update+=Update;}
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var serialized=new SerializedObject(UnityEngine.Object.FindObjectOfType<PythonStateClient>());serialized.FindProperty("port").intValue=TestPort;serialized.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafeTitle.unity");UnityEngine.Object.FindObjectOfType<PythonTitleScreen>().port=TestPort;EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            SessionState.SetBool(Key+"events",false);SessionState.SetInt(Key,1);SessionState.SetInt(Key+"mode",0);SessionState.SetInt(Key+"days",0);SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+900);EditorApplication.EnterPlaymode();
        }
        public static void RunEvents()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafeTitle.unity");UnityEngine.Object.FindObjectOfType<PythonTitleScreen>().port=TestPort;EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var serialized=new SerializedObject(UnityEngine.Object.FindObjectOfType<PythonStateClient>());serialized.FindProperty("port").intValue=TestPort;serialized.ApplyModifiedPropertiesWithoutUndo();
            SessionState.SetBool(Key+"events",true);SessionState.SetInt(Key,12);SessionState.SetInt(Key+"mode",3);SessionState.SetInt(Key+"days",2);SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+180);EditorApplication.EnterPlaymode();
        }
        public static void VerifyOptionalEvents()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var sample=PythonServiceSetup.Sample();sample.instance_id="empty-events";sample.reservation=null;sample.regular_introduction=null;sample.visiting_cat=null;sample.intake_request=null;
            var state=JsonUtility.FromJson<PythonCafeState>(JsonUtility.ToJson(sample));typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});var list=preview.GetComponent<PythonEventListPanel>();list.Open();
            if(state.reservation!=null || state.regular_introduction!=null || state.visiting_cat!=null || state.intake_request!=null || list.entries[8].interactable || !list.entries[8].GetComponentInChildren<UnityEngine.UI.Text>().text.Contains("対象なし"))throw new Exception("JSON null became a fictional event");
            Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/AuditOptionalEvents.png"});
            foreach(var status in new[]{"untriggered","waiting","accepted","declined"})
            {
                state.reservation=new PythonReservation{status=status,name="予約客",visit_day=status=="untriggered" ? 0 : 3};typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});
                string expected=status=="untriggered" ? "解放前" : status=="waiting" ? "確認待ち" : status=="accepted" ? "引き受け済み" : "辞退済み";
                if(state.reservation==null || !list.entries[8].interactable || !list.entries[8].GetComponentInChildren<UnityEngine.UI.Text>().text.Contains(expected))throw new Exception("Valid reservation state was lost: "+status);
            }
            Debug.Log("CAT_CAFE_OPTIONAL_EVENTS_OK: JSON null / no fictional reservation / configured locked waiting accepted declined");
        }
        public static void VerifyPanels()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.can_set_shifts=true;state.closed=false;state.instance_id="overlap";state.required_action="保護猫の受け入れ依頼に回答してください。";
            state.intake_request=new PythonIntakeState{status="waiting",name="新しい猫",can_decline=true};state.goal_result=new PythonGoalResult{status="cleared",can_continue=false};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});apply();
            var intake=preview.GetComponent<PythonIntakePanel>();if(intake.panel.transform.GetSiblingIndex()!=intake.panel.transform.parent.childCount-1 || !intake.decline.interactable)throw new Exception("Actionable event hidden behind result");
            var root=preview.transform.Find("Content Canvas/Background");var hint=root.Find("Title Save Hint").GetComponent<UnityEngine.UI.Text>();var entry=root.Find("Return To Title").GetComponent<UnityEngine.UI.Button>();
            if(!hint.text.Contains("受け入れ"))throw new Exception("Required reason missing in header");
            typeof(PythonStateClient).GetField("pending",flags).SetValue(client,new PythonShiftCommand());preview.RefreshPythonControls();if(entry.interactable || !hint.text.Contains("送信"))throw new Exception("Pending header guards missing");preview.GetComponent<PythonReturnToTitle>().Return();if(SceneManager.GetActiveScene().name!="CatCafePythonState")throw new Exception("Pending command navigated away");
            typeof(PythonStateClient).GetField("pending",flags).SetValue(client,null);state.required_action="";state.intake_request=null;state.goal_result=null;foreach(var cat in state.cats)cat.working=false;apply();if(!hint.text.Contains("出勤する猫がいません"))throw new Exception("No shift reason missing");
            typeof(PythonStateClient).GetField("online",flags).SetValue(client,false);preview.PythonConnectionError("検証");if(!entry.interactable || !hint.text.Contains("未接続"))throw new Exception("Offline navigation failed");
            state.closed=true;state.can_set_shifts=false;state.next_goal=new PythonNextGoal{stage=2,can_advance=true};apply();preview.RefreshPythonControls();var closing=preview.GetComponent<PythonClosingPanel>();var nextGoal=preview.GetComponent<PythonGoalPanel>();
            if(!closing.open.gameObject.activeSelf || !nextGoal.openNext.gameObject.activeSelf || closing.open.GetComponent<RectTransform>().anchoredPosition==nextGoal.openNext.GetComponent<RectTransform>().anchoredPosition)throw new Exception("Closing review unavailable beside next goal");
            closing.Open();closing.Close();closing.Open();if(!closing.panel.activeSelf)throw new Exception("Closing review cannot reopen beside next goal");
            Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/FlowReviewAndNextGoal.png"});
            state.game_over=new PythonGameOver{reason="funds",day=1};state.intake_request=new PythonIntakeState{status="waiting",can_decline=true};state.required_action="経営終了";apply();preview.RefreshPythonControls();var end=preview.GetComponent<PythonGameOverPanel>();if(end.panel.transform.GetSiblingIndex()!=end.panel.transform.parent.childCount-1)throw new Exception("Game over priority lost");
            Debug.Log("CAT_CAFE_FLOW_PANEL_FIXES_OK: required event priority / header reason / pending title guard / offline / game over priority");
        }
        static bool Resolve(PythonStateClient client)
        {
            var state=client.CurrentState;if(string.IsNullOrEmpty(state.required_action))return false;
            if(client.Automatic || client.CanProgressBusiness)throw new Exception("Required event failed to pause business");
            if(SessionState.GetBool(Key+"events",false))return ResolveThroughList(client);
            ButtonClick(client.GetComponent<PythonIntakePanel>().panel,client.GetComponent<PythonIntakePanel>().decline);
            if(!client.CanManageSaves)return true;
            var growth=client.GetComponent<PythonGrowthPanel>();foreach(var button in growth.choices)if(ButtonClick(growth.panel,button))return true;
            var store=client.GetComponent<PythonStoreEventPanel>();if(state.store_event!=null && state.store_event.choices!=null)for(int i=0;i<state.store_event.choices.Length;i++)if(state.store_event.choices[i].choice!="close" && ButtonClick(store.panel,store.choices[i]))return true;
            var goal=client.GetComponent<PythonGoalPanel>();if(ButtonClick(goal.panel,goal.resume))return true;
            var missing=client.GetComponent<PythonMissingPanel>();if(ButtonClick(missing.panel,missing.confirm))return true;
            return true; // Wait for the UI refresh after the response finishes.
        }
        static string EventSnapshot(PythonCafeState state)=>JsonUtility.ToJson(new PythonCafeState{day=state.day,tick=state.tick,funds=state.funds,cats=state.cats,intake_request=state.intake_request,growth_choices=state.growth_choices,store_event=state.store_event});
        static bool ResolveThroughList(PythonStateClient client)
        {
            var list=client.GetComponent<PythonEventListPanel>();var state=client.CurrentState;
            int route=-1;GameObject panel=null;UnityEngine.UI.Button button=null;
            if(client.CanRespondIntake(false)){route=1;var p=client.GetComponent<PythonIntakePanel>();panel=p.panel;button=p.decline;}
            else if(state.growth_choices?.Length>0){for(int i=0;i<state.growth_choices[0].choices.Length;i++)if(client.CanRespondGrowth(i)){route=3;var p=client.GetComponent<PythonGrowthPanel>();panel=p.panel;button=p.choices[i];break;}}
            if(route<0 && state.store_event!=null){for(int i=0;i<state.store_event.choices.Length;i++)if(state.store_event.choices[i].choice!="close" && client.CanRespondStore(i)){route=2;var p=client.GetComponent<PythonStoreEventPanel>();panel=p.panel;button=p.choices[i];break;}}
            if(route<0)return true;
            list.open.onClick.Invoke();if(!list.panel.activeSelf || !list.entries[route].interactable)throw new Exception("Required event entry unavailable: "+route);
            list.entries[route].onClick.Invoke();client.GetComponent<ContentPreview>().RefreshPythonControls();if(list.panel.activeSelf || !panel.activeSelf || !button.interactable)throw new Exception("Event list did not open selected response: "+route);
            Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{client.GetComponent<ContentPreview>(),"Logs/AuditEvent-"+route+".png"});
            if(!ButtonClick(panel,button))throw new Exception("Response could not be clicked");if(route==1){var intake=client.GetComponent<PythonIntakePanel>();if(!ButtonClick(intake.confirmation,intake.confirm))throw new Exception("Intake confirmation could not be clicked");}if(button.interactable)throw new Exception("Duplicate response remained enabled");return true;
        }
        static bool ButtonClick(GameObject panel,UnityEngine.UI.Button button)
        {
            if(!panel.activeSelf || !button.gameObject.activeInHierarchy || !button.interactable)return false;
            if(panel.transform.GetSiblingIndex()!=panel.transform.parent.childCount-1)throw new Exception("Actionable event obscured: "+panel.name);
            Debug.Log("CAT_CAFE_MULTI_DAY_EVENT: "+panel.name);button.onClick.Invoke();return true;
        }
        static void Update()
        {
            int stage=SessionState.GetInt(Key,0);if(stage==0)return;
            if(SessionState.GetInt(Key+"loggedStage",-1)!=stage){SessionState.SetInt(Key+"loggedStage",stage);Debug.Log("CAT_CAFE_FLOW_STAGE: "+stage+" / "+SceneManager.GetActiveScene().name);}
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0))throw new Exception("Multi-day timeout stage "+stage);
                if(!EditorApplication.isPlaying)return;var title=UnityEngine.Object.FindObjectOfType<PythonTitleScreen>();var client=UnityEngine.Object.FindObjectOfType<PythonStateClient>();
                if(stage==1 && title!=null && title.newGame.interactable)
                {title.newGame.onClick.Invoke();int mode=SessionState.GetInt(Key+"mode",0);title.objectives.value=mode;title.SelectObjective(mode);title.startGame.onClick.Invoke();SessionState.SetInt(Key,2);return;}
                if(stage==9 && title!=null && title.continueGame.interactable)
                {
                    title.continueGame.onClick.Invoke();
                    // State and save-list requests complete separately on a new title
                    // scene. Do not advance the test until the real Resume button works.
                    if(!title.resume.interactable)return;
                    title.saves.value=0;title.resume.onClick.Invoke();SessionState.SetInt(Key,10);return;
                }
                if(client==null || !client.CanManageSaves)return;var state=client.CurrentState;var preview=client.GetComponent<ContentPreview>();var closing=client.GetComponent<PythonClosingPanel>();
                if(state.game_over!=null)throw new Exception("Unexpected game over");
                if(stage==12)
                {
                    if(state.intake_request?.status!="waiting" || !(state.growth_choices?.Length>0) || state.store_event==null || client.CanProgressBusiness)throw new Exception("Expected overlapping events missing");
                    SessionState.SetString(Key+"pendingEvents",EventSnapshot(state));SessionState.SetInt(Key+"count",client.GetComponent<PythonSavePanel>().Count);
                    var list=client.GetComponent<PythonEventListPanel>();list.open.onClick.Invoke();Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/AuditEventList.png"});list.Close();preview.GetSceneButton(3).onClick.Invoke();SessionState.SetInt(Key,13);
                }
                else if(stage==13 && client.GetComponent<PythonSavePanel>().Count>SessionState.GetInt(Key+"count",0))
                {var saves=client.GetComponent<PythonSavePanel>();saves.saves.value=0;saves.ResumeSelected();SessionState.SetInt(Key,14);}
                else if(stage==14)
                {if(EventSnapshot(state)!=SessionState.GetString(Key+"pendingEvents",""))throw new Exception("Pending-event save restore mismatch");Debug.Log("CAT_CAFE_OVERLAPPING_EVENT_SAVE_OK");SessionState.SetInt(Key,5);}
                else if(stage==2)
                {
                    if(!client.GetComponent<PythonPreparationPanel>().panel.activeSelf)throw new Exception("First guide missing");client.GetComponent<PythonPreparationPanel>().close.onClick.Invoke();client.GetComponent<PythonCatRoster>().GetCard(0).rest.onClick.Invoke();
                    var entry=client.transform.Find("Content Canvas/Background/Return To Title").GetComponent<UnityEngine.UI.Button>();if(entry.interactable || client.CanLeaveResult)throw new Exception("Pending navigation guard failed");SessionState.SetInt(Key,3);
                }
                else if(stage==3 && !state.cats[0].working){client.GetComponent<PythonCatRoster>().GetCard(0).work.onClick.Invoke();SessionState.SetInt(Key,4);}
                else if(stage==4 && state.cats[0].working){SessionState.SetInt(Key,5);}
                else if(stage==5)
                {
                    if(Resolve(client))return;if(!client.CanProgressBusiness)throw new Exception("Cannot start business");preview.GetSceneButton(2).onClick.Invoke();SessionState.SetInt(Key,6);
                }
                else if(stage==6)
                {
                    if(Resolve(client))return;
                    if(state.closed)
                    {
                        if(state.finance==null || Mathf.Abs(state.finance.opening_funds+state.finance.net_cash_flow-state.finance.closing_funds)>.01f)throw new Exception("Daily finance mismatch");
                        closing.Open();if(!closing.panel.activeSelf || !closing.next.interactable)throw new Exception("Closing review unavailable");
                        SessionState.SetInt(Key+"day",state.day);SessionState.SetFloat(Key+"funds",state.funds);SessionState.SetString(Key+"cats",JsonUtility.ToJson(new PythonCafeState{cats=state.cats}));SessionState.SetString(Key+"progress",JsonUtility.ToJson(state.objective_progress));SessionState.SetInt(Key+"count",client.GetComponent<PythonSavePanel>().Count);
                        closing.save.onClick.Invoke();SessionState.SetInt(Key,8);
                    }
                    else if(!client.Automatic){if(!client.Fast)client.GetComponent<PythonAutoPanel>().speed.onClick.Invoke();client.GetComponent<PythonAutoPanel>().toggle.onClick.Invoke();if(!client.Automatic)throw new Exception("Cannot start automatic service");}
                }
                else if(stage==8 && client.GetComponent<PythonSavePanel>().Count>SessionState.GetInt(Key+"count",0))
                {client.GetComponent<PythonReturnToTitle>().Return();SessionState.SetInt(Key,9);}
                else if(stage==10)
                {
                    if(!state.closed || !closing.panel.activeSelf || state.day!=SessionState.GetInt(Key+"day",0) || state.funds!=SessionState.GetFloat(Key+"funds",0) || JsonUtility.ToJson(new PythonCafeState{cats=state.cats})!=SessionState.GetString(Key+"cats","") || JsonUtility.ToJson(state.objective_progress)!=SessionState.GetString(Key+"progress","") || client.Automatic)throw new Exception("Restored state mismatch");
                    int days=SessionState.GetInt(Key+"days",0)+1;SessionState.SetInt(Key+"days",days);Debug.Log("CAT_CAFE_MULTI_DAY_RESTORE_OK: "+state.objective+" / day "+state.day);
                    if(days==3)
                    {
                        int mode=SessionState.GetInt(Key+"mode",0);if(mode==3){SessionState.SetInt(Key,0);Debug.Log(SessionState.GetBool(Key+"events",false) ? "CAT_CAFE_OVERLAPPING_EVENTS_PLAY_OK: response / service / closing / save / title resume" : "CAT_CAFE_MULTI_DAY_PLAY_OK: four objectives / three days each / title save resume / shift / finance / cats / progress");EditorApplication.Exit(0);return;}
                        SessionState.SetInt(Key+"mode",mode+1);SessionState.SetInt(Key+"days",0);client.GetComponent<PythonReturnToTitle>().Return();SessionState.SetInt(Key,1);
                    }
                    else {closing.next.onClick.Invoke();SessionState.SetInt(Key,11);}
                }
                else if(stage==11 && !state.closed)
                {
                    if(!state.can_set_shifts || closing.panel.activeSelf || state.day!=SessionState.GetInt(Key+"day",0)+1)throw new Exception("Next preparation mismatch");SessionState.SetInt(Key,5);
                }
            }
            catch(Exception ex){SessionState.SetInt(Key,0);Debug.LogException(ex);EditorApplication.Exit(1);}
        }
    }
}
