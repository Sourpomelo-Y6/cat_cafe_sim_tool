using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview.Editor
{
    public static class PythonGoalSetup
    {
        [MenuItem("Cat Cafe/開発用/UI再生成/目標と結果", false, 4)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>(); var root=preview.transform.Find("Content Canvas/Background");
            var old=root.Find("Goal Result"); if(old!=null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var overlay=Box(root,"Goal Result",0,174,1280,626,new Color(0,0,0,.55f));
            // Keep later event dialogs above the result, as in the existing scene.
            overlay.transform.SetSiblingIndex(19);
            var card=Box(overlay.transform,"Result Card",284,0,712,610,new Color(.98f,.97f,.93f,1));
            var panel=preview.GetComponent<PythonGoalPanel>()??preview.gameObject.AddComponent<PythonGoalPanel>(); panel.panel=overlay;
            panel.title=Label(card.transform,"Title",30,22,652,54,25);
            var area=Box(card.transform,"Result Scroll",30,86,652,410,Color.clear);
            var scroll=area.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            var viewport=Box(area.transform,"Viewport",0,0,626,410,Color.white);viewport.AddComponent<Mask>().showMaskGraphic=false;
            panel.details=Label(viewport.transform,"Details",0,0,626,410,20);
            panel.details.supportRichText=false;
            panel.details.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport=viewport.GetComponent<RectTransform>();scroll.content=panel.details.rectTransform;
            var bar=DefaultControls.CreateScrollbar(new DefaultControls.Resources());bar.name="Vertical Scrollbar";bar.transform.SetParent(area.transform,false);
            Place(bar.GetComponent<RectTransform>(),632,0,20,410);scroll.verticalScrollbar=bar.GetComponent<Scrollbar>();scroll.verticalScrollbar.direction=Scrollbar.Direction.BottomToTop;
            var heading=root.Find("Roster Heading").GetComponent<Text>();heading.fontSize=18;Place(heading.rectTransform,24,68,750,58);
            panel.notice=Label(card.transform,"Notice",30,504,652,50,17);
            var button=DefaultControls.CreateButton(new DefaultControls.Resources()); button.name="Continue Business"; button.transform.SetParent(card.transform,false);
            Place(button.GetComponent<RectTransform>(),262,558,188,44); var text=button.GetComponentInChildren<Text>();
            text.font=ContentPreviewLayout.LoadFont(); text.fontSize=22; text.text="継続営業";
            panel.resume=button.GetComponent<Button>(); UnityEventTools.AddPersistentListener(panel.resume.onClick,panel.Continue);
            panel.title.text="目標の結果"; panel.details.text="確定した目標結果を表示します。"; panel.resume.interactable=false; overlay.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("CAT_CAFE_GOAL_UI_OK: serialized result panel / explicit continue button");
        }
        static void Place(RectTransform rect,float x,float y,float width,float height)
        { rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1); rect.anchoredPosition=new Vector2(x,-y); rect.sizeDelta=new Vector2(width,height); }
        static GameObject Box(Transform parent,string name,float x,float y,float width,float height,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent,false);
            Place(go.GetComponent<RectTransform>(),x,y,width,height); go.GetComponent<Image>().color=color; return go;
        }
        static Text Label(Transform parent,string name,float x,float y,float width,float height,int size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text)); go.transform.SetParent(parent,false);
            Place(go.GetComponent<RectTransform>(),x,y,width,height); var text=go.GetComponent<Text>();
            text.font=ContentPreviewLayout.LoadFont(); text.fontSize=size; text.color=new Color(.18f,.16f,.14f,1); text.raycastTarget=false; return text;
        }
        public static void BuildAndCapture()
        {
            Build(); var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>(); preview.Initialize();
            var state=PythonServiceSetup.Sample(); state.closed=true; state.finance=new PythonFinanceState();
            state.seats=new PythonSeatState[0]; state.customers=new PythonCustomerState[0];
            state.required_action="目標画面で結果を確認し、継続営業を選んでください。";
            state.goal_result=new PythonGoalResult {status="cleared",stage=1,started_day=1,deadline=20,resolved_day=8,target=250,popularity=255,funds=1600,can_continue=true};
            var client=preview.GetComponent<PythonStateClient>(); var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview); typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});
            var panel=preview.GetComponent<PythonGoalPanel>();
            if(!panel.panel.activeSelf || !panel.resume.interactable || !panel.details.text.Contains("達成"))throw new Exception("Goal result display mismatch");
            Canvas.ForceUpdateCanvases(); typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/GoalResult.png"});
            state.goal_result.status="expired"; preview.ApplyPythonState(state); if(!panel.details.text.Contains("期限内未達"))throw new Exception("Expired result display mismatch");
            state.goal_result.can_continue=false; state.goal_result.reason="先に帰還イベントを確認してください。"; preview.ApplyPythonState(state);
            if(panel.resume.interactable || !panel.notice.text.Contains("帰還"))throw new Exception("Goal blocked reason missing");
            panel.Disconnect(); if(panel.resume.interactable)throw new Exception("Goal offline guard failed");
            state.goal_result=null; preview.ApplyPythonState(state); if(panel.panel.activeSelf)throw new Exception("Confirmed goal panel remained visible");
            foreach(var mode in new[]{"popularity","patron","bond","free"})
            {
                state.objective_progress=new PythonObjectiveProgress{mode=mode,label=mode,summary="進捗テスト 1 / 6",details="目標条件の説明"};
                preview.ApplyPythonState(state);
                if(!preview.GetComponent<PythonServiceScreen>().heading.text.Contains("1 / 6") || !preview.DisplayText.Contains("目標条件の説明"))throw new Exception("Objective progress missing: "+mode);
            }
            state.goal_result=new PythonGoalResult{status="cleared",kind="continue_bond_goal",title="好感度目標を達成しました",details="好感度80以上の在籍猫　6 / 6匹\n結果：達成\n結果確定：12日目\n\nクリア時の成果\n所持金：2400　人気：320\n在籍猫：6匹　累計売上：6000",can_continue=true};
            preview.ApplyPythonState(state);
            if(panel.title.text!=state.goal_result.title || panel.details.text!=state.goal_result.details)throw new Exception("Mode result text mismatch");
            Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/ObjectiveResult.png"});
            state.goal_result=null;preview.ApplyPythonState(state);
            Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/ObjectiveProgress.png"});
            Debug.Log("CAT_CAFE_OBJECTIVE_DISPLAY_OK: all four modes / custom clear result / scroll");
            Debug.Log("CAT_CAFE_GOAL_DISPLAY_OK: cleared / expired / blocked reason / offline / confirmed dismissal");
        }
    }
}
