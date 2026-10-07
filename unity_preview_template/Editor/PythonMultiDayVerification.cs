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
        static PythonMultiDayVerification(){EditorApplication.update+=Update;}
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var serialized=new SerializedObject(UnityEngine.Object.FindObjectOfType<PythonStateClient>());serialized.FindProperty("port").intValue=8191;serialized.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafeTitle.unity");UnityEngine.Object.FindObjectOfType<PythonTitleScreen>().port=8191;EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            SessionState.SetBool(Key+"events",false);SessionState.SetInt(Key,1);SessionState.SetInt(Key+"mode",0);SessionState.SetInt(Key+"days",0);SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+900);EditorApplication.EnterPlaymode();
        }
        public static void RunEvents()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafeTitle.unity");UnityEngine.Object.FindObjectOfType<PythonTitleScreen>().port=8191;EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var serialized=new SerializedObject(UnityEngine.Object.FindObjectOfType<PythonStateClient>());serialized.FindProperty("port").intValue=8191;serialized.ApplyModifiedPropertiesWithoutUndo();
            SessionState.SetBool(Key+"events",true);SessionState.SetInt(Key,5);SessionState.SetInt(Key+"mode",3);SessionState.SetInt(Key+"days",2);SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+180);EditorApplication.EnterPlaymode();
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
            ButtonClick(client.GetComponent<PythonIntakePanel>().panel,client.GetComponent<PythonIntakePanel>().decline);
            if(!client.CanManageSaves)return true;
            var growth=client.GetComponent<PythonGrowthPanel>();foreach(var button in growth.choices)if(ButtonClick(growth.panel,button))return true;
            var store=client.GetComponent<PythonStoreEventPanel>();if(state.store_event!=null && state.store_event.choices!=null)for(int i=0;i<state.store_event.choices.Length;i++)if(state.store_event.choices[i].choice!="close" && ButtonClick(store.panel,store.choices[i]))return true;
            var goal=client.GetComponent<PythonGoalPanel>();if(ButtonClick(goal.panel,goal.resume))return true;
            var missing=client.GetComponent<PythonMissingPanel>();if(ButtonClick(missing.panel,missing.confirm))return true;
            return true; // Wait for the UI refresh after the response finishes.
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
            try
            {
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"deadline",0))throw new Exception("Multi-day timeout stage "+stage);
                if(!EditorApplication.isPlaying)return;var title=UnityEngine.Object.FindObjectOfType<PythonTitleScreen>();var client=UnityEngine.Object.FindObjectOfType<PythonStateClient>();
                if(stage==1 && title!=null && title.newGame.interactable)
                {title.newGame.onClick.Invoke();int mode=SessionState.GetInt(Key+"mode",0);title.objectives.value=mode;title.SelectObjective(mode);title.startGame.onClick.Invoke();SessionState.SetInt(Key,2);return;}
                if(stage==9 && title!=null && title.continueGame.interactable)
                {title.continueGame.onClick.Invoke();title.saves.value=0;title.resume.onClick.Invoke();SessionState.SetInt(Key,10);return;}
                if(client==null || !client.CanManageSaves)return;var state=client.CurrentState;var preview=client.GetComponent<ContentPreview>();var closing=client.GetComponent<PythonClosingPanel>();
                if(state.game_over!=null)throw new Exception("Unexpected game over");
                if(stage==2)
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
