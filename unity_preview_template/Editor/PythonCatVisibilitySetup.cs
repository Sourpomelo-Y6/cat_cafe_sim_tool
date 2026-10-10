using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonCatVisibilitySetup
    {
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static Toggle Toggle(Transform root,string name,float x,float y,float w,PythonCatVisibility target)
        {
            var old=root.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var go=DefaultControls.CreateToggle(new DefaultControls.Resources());go.name=name;go.transform.SetParent(root,false);Place(go.GetComponent<RectTransform>(),x,y,w,30);
            var label=go.GetComponentInChildren<Text>();label.font=ContentPreviewLayout.LoadFont();label.fontSize=16;label.text="譲渡済みも表示";label.color=new Color(.16f,.21f,.18f);
            var toggle=go.GetComponent<Toggle>();toggle.graphic.color=new Color(.16f,.3f,.2f);toggle.SetIsOnWithoutNotify(false);UnityEventTools.AddPersistentListener(toggle.onValueChanged,target.Change);toggle.onValueChanged.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);return toggle;
        }
        public static void Configure(ContentPreview preview)
        {
            var p=preview.GetComponent<PythonCatVisibility>() ?? preview.gameObject.AddComponent<PythonCatVisibility>();
            var root=preview.transform.Find("Content Canvas/Background");p.rosterToggle=Toggle(root,"Adopted Cat Visibility",24,184,260,p);
            var roster=preview.GetComponent<PythonCatRoster>();if(roster!=null && roster.content!=null){var scroll=roster.content.GetComponentInParent<ScrollRect>();Place(scroll.GetComponent<RectTransform>(),24,220,750,518);Place(scroll.viewport,0,0,750,518);}
            var prep=preview.GetComponent<PythonPreparationPanel>();
            if(prep!=null && prep.cats!=null)
            {
                Place(prep.cats.GetComponent<RectTransform>(),388,150,318,40);var arrow=prep.cats.transform.Find("Dropdown Arrow");if(arrow!=null)Place(arrow.GetComponent<RectTransform>(),284,7,26,26);
                p.preparationToggle=Toggle(prep.cats.transform.parent,"Adopted Cat Visibility",718,155,198,p);
            }
        }
        [MenuItem("Cat Cafe/開発用/UI再生成/譲渡済み猫の表示切替",false,44)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");Configure(UnityEngine.Object.FindObjectOfType<ContentPreview>());EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);PythonPreparationLayout.RefreshIfReady();Debug.Log("CAT_CAFE_CAT_VISIBILITY_UI_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="visibility";state.can_set_shifts=true;state.closed=false;state.tick=0;state.required_action="";
            state.cats[0].activity="adopted";state.cats[0].activity_label="譲渡済み";state.cats[0].working=false;
            state.cats[2].activity="dispatched";state.cats[2].working=false;state.cats[2].activity_label="派遣中";
            state.cats[3].activity="missing";state.cats[3].working=false;state.cats[3].activity_label="行方不明";
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});apply();
            var visibility=preview.GetComponent<PythonCatVisibility>();var roster=preview.GetComponent<PythonCatRoster>();var prep=preview.GetComponent<PythonPreparationPanel>();
            string before=JsonUtility.ToJson(state);if(roster.Count!=4 || prep.cats.options.Count!=4 || preview.VisiblePythonCats.Length!=4 || visibility.ShowAdopted)throw new Exception("Default adopted filtering failed");
            prep.SelectCat(0);if(!prep.selectedDetails.text.Contains(state.cats[1].name))throw new Exception("Filtered preparation index mismatch");
            visibility.rosterToggle.onValueChanged.Invoke(true);if(roster.Count!=5 || prep.cats.options.Count!=5 || !visibility.preparationToggle.isOn)throw new Exception("Visibility toggles not synchronized");
            preview.SelectCat(0);prep.SelectCat(0);if(prep.work.interactable || prep.rest.interactable || roster.GetCard(0).work.interactable || roster.GetCard(0).rest.interactable)throw new Exception("Adopted shift enabled");
            roster.GetCard(0).details.onClick.Invoke();var details=preview.GetComponent<PythonCatDetailsPanel>();if(!details.panel.activeSelf)throw new Exception("Adopted history inaccessible");
            visibility.preparationToggle.onValueChanged.Invoke(false);
            if(roster.Count!=4 || visibility.rosterToggle.isOn || details.panel.activeSelf || preview.SelectedPythonCatId!=state.cats[1].cat_id || !prep.selectedDetails.text.Contains(state.cats[1].name))throw new Exception("Hidden selected cat retained");
            preview.SelectCat(1);prep.SelectCat(1);visibility.Change(true);visibility.Change(false);if(preview.SelectedPythonCatId!=state.cats[2].cat_id || !prep.selectedDetails.text.Contains(state.cats[2].name))throw new Exception("Visible selection not retained");
            if(JsonUtility.ToJson(state)!=before)throw new Exception("Filtering mutated state");
            state.rest_proposal=new[]{state.cats[1].cat_id,state.cats[4].cat_id};apply();prep.ProposeRest();visibility.Change(true);visibility.Change(false);if(!prep.HasProposal)throw new Exception("Visibility discarded shift draft");prep.CancelProposal();
            foreach(var cat in state.cats){cat.activity="adopted";cat.working=false;}apply();if(roster.Count!=0 || prep.cats.interactable || prep.work.interactable || preview.SelectedPythonCatId!="" || !prep.selectedDetails.text.Contains("いません"))throw new Exception("Empty visibility failed");
            visibility.Change(true);if(roster.Count!=5 || !prep.cats.interactable)throw new Exception("All adopted cannot be viewed");
            typeof(PythonStateClient).GetField("online",flags).SetValue(client,false);prep.Disconnect();visibility.Change(false);if(prep.work.interactable || prep.rest.interactable)throw new Exception("Offline filter enabled actions");
            state.instance_id="new-game";apply();if(visibility.ShowAdopted)throw new Exception("New game visibility not reset");
            if(typeof(PythonStateClient).GetField("pending",flags).GetValue(client)!=null)throw new Exception("Visibility sent command");
            state.cats[1].activity="cafe";state.cats[1].activity_label="在店";apply();prep.Open();Canvas.ForceUpdateCanvases();
            typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/CatVisibilityPreparation.png"});
            prep.Close();preview.GetComponent<PythonServiceScreen>().ShowCats();visibility.Change(true);Canvas.ForceUpdateCanvases();
            typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/CatVisibilityRoster.png"});
            Debug.Log("CAT_CAFE_CAT_VISIBILITY_OK: defaults / adopted only / mapped selection / synchronized / history / empty / draft / offline / new game / read only");
        }
    }
}
