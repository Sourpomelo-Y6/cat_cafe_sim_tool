using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview.Editor
{
    public static class PythonGrowthSetup
    {
        [MenuItem("Cat Cafe/開発用/UI再生成/猫の成長・習得選択", false, 0)]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var preview = UnityEngine.Object.FindObjectOfType<ContentPreview>();
            var root = preview.transform.Find("Content Canvas/Background");
            var old = root.Find("Growth Choice"); if(old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var overlay = Box(root, "Growth Choice", 0, 174, 1280, 626, new Color(0,0,0,.55f));
            var card = Box(overlay.transform, "Request Card", 284, 0, 712, 610, new Color(.98f,.97f,.93f,1));
            var panel = preview.GetComponent<PythonGrowthPanel>() ?? preview.gameObject.AddComponent<PythonGrowthPanel>();
            panel.panel = overlay;
            panel.title = Label(card.transform,"Title",30,22,652,54,22);
            panel.details = Label(card.transform,"Cat Details",30,86,652,400,19);
            panel.notice = Label(card.transform,"Response Notice",30,502,652,50,17);
            panel.notice.lineSpacing=1;
            panel.choices=new [] {Button(card.transform,"First Choice","選択肢1",20,558),Button(card.transform,"Second Choice","選択肢2",246,558),Button(card.transform,"Third Choice","選択肢3",472,558),Button(card.transform,"Fourth Choice","選択肢4",356,558)};
            UnityEventTools.AddPersistentListener(panel.choices[0].onClick,panel.First);
            UnityEventTools.AddPersistentListener(panel.choices[1].onClick,panel.Second);
            UnityEventTools.AddPersistentListener(panel.choices[2].onClick,panel.Third);
            UnityEventTools.AddPersistentListener(panel.choices[3].onClick,panel.Fourth);
            for(int i=0;i<panel.choices.Length;i++) Place(panel.choices[i].GetComponent<RectTransform>(),20+i*170,558,166,44);
            panel.title.text="猫の成長";
            foreach(var button in panel.choices) button.interactable=false;
            overlay.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("CAT_CAFE_GROWTH_UI_OK: serialized event panel / details / four choices");
        }
        static void Place(RectTransform rect,float x,float y,float width,float height)
        {
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-y); rect.sizeDelta=new Vector2(width,height);
        }
        static GameObject Box(Transform parent,string name,float x,float y,float width,float height,Color color)
        {
            var box=new GameObject(name,typeof(RectTransform),typeof(Image)); box.transform.SetParent(parent,false);
            Place(box.GetComponent<RectTransform>(),x,y,width,height); box.GetComponent<Image>().color=color; return box;
        }
        static Text Label(Transform parent,string name,float x,float y,float width,float height,int fontSize)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text)); go.transform.SetParent(parent,false);
            Place(go.GetComponent<RectTransform>(),x,y,width,height); var text=go.GetComponent<Text>();
            text.font=ContentPreviewLayout.LoadFont(); text.fontSize=fontSize; text.color=new Color(.18f,.16f,.14f,1);
            text.alignment=TextAnchor.UpperLeft; text.lineSpacing=1.12f; text.raycastTarget=false; return text;
        }
        static Button Button(Transform parent,string name,string caption,float x,float y)
        {
            var go=DefaultControls.CreateButton(new DefaultControls.Resources()); go.name=name; go.transform.SetParent(parent,false);
            Place(go.GetComponent<RectTransform>(),x,y,220,44); var text=go.GetComponentInChildren<Text>();
            text.font=ContentPreviewLayout.LoadFont(); text.fontSize=19; text.text=caption; return go.GetComponent<Button>();
        }
        public static void VerifyAndCapture()
        {
            var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>(); preview.Initialize();
            var client=preview.GetComponent<PythonStateClient>();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample(); state.can_set_shifts=true; state.required_action="猫の成長への対応を選んでください。";
            state.growth_choices=new [] {new PythonGrowthChoice {cat_id="cat-mike",name="ミケ",experience=20,threshold=20,service=16,rest=4,dispatch=0,choices=new [] {
                new PythonEventChoice {choice="service",label="接客を得意にする",detail="接客終了時、消費した体力の10%を回復します。",can_select=true},
                new PythonEventChoice {choice="rest",label="休養を得意にする",detail="在店休養時の疲労回復＋5。",can_select=true},
                new PythonEventChoice {choice="dispatch",label="派遣を得意にする",detail="派遣報酬を1.1倍にします。",can_select=true}}}};
            typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});
            var panel=preview.GetComponent<PythonGrowthPanel>();
            if(!panel.panel.activeSelf || !panel.choices[0].interactable || client.CanProgressBusiness) throw new Exception("Event display guard failed");
            Canvas.ForceUpdateCanvases();
            typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/GrowthChoice.png"});
            var growth=state.growth_choices[0]; growth.kind="resolve_growth_mastery"; growth.title="得意な交流";
            growth.play=10; growth.contact=5; growth.quiet=0; growth.experience=15; growth.threshold=15;
            growth.choices=new [] {
                new PythonEventChoice {choice="play",label="遊び",detail="通常行動の関心増加×1.1。",can_select=true},
                new PythonEventChoice {choice="contact",label="触れ合い",detail="通常行動の関心増加×1.1。",can_select=true},
                new PythonEventChoice {choice="quiet",label="静かな交流",detail="通常行動の関心増加×1.1。",reason="接客実績が不足しています。"}};
            preview.ApplyPythonState(state);
            if(!panel.title.text.Contains("得意な交流") || panel.choices[2].interactable || !panel.details.text.Contains("遊び：10")) throw new Exception("Mastery display failed");
            Canvas.ForceUpdateCanvases();
            typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/InteractionMastery.png"});
            growth.kind="resolve_growth_type_mastery"; growth.title="得意な行動"; growth.group="遊び";
            growth.effect="選んだ行動の関心増加を追加で1.1倍にします。";
            growth.choices=new [] {
                new PythonEventChoice {choice="teaser",label="ねこじゃらし",detail="成功接客の実績：5回",can_select=true},
                new PythonEventChoice {choice="ball",label="ボール",detail="成功接客の実績：4回",can_select=true},
                new PythonEventChoice {choice="plush",label="ぬいぐるみ",detail="成功接客の実績：3回",can_select=true},
                new PythonEventChoice {choice="tunnel",label="トンネル",detail="成功接客の実績：3回",can_select=true}};
            preview.ApplyPythonState(state); Canvas.ForceUpdateCanvases();
            if(!panel.choices[3].interactable || !panel.details.text.Contains("トンネル")) throw new Exception("Fourth action display failed");
            typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/ActionMastery.png"});
            if(panel.choices[3].onClick.GetPersistentMethodName(0)!="Fourth") throw new Exception("Fourth button binding failed");
            panel.Fourth();
            var action=(PythonShiftCommand)typeof(PythonStateClient).GetField("pending",flags).GetValue(client);
            if(action.kind!="resolve_growth_type_mastery" || action.choice!="tunnel" || panel.choices[3].interactable) throw new Exception("Action command guard failed");
            typeof(PythonStateClient).GetField("pending",flags).SetValue(client,null);
            growth.kind="resolve_growth_mastery"; growth.choices[0].choice="play";
            preview.ApplyPythonState(state);
            panel.First(); if(panel.choices[0].interactable || panel.choices[1].interactable) throw new Exception("Pending guard failed");
            var pending=(PythonShiftCommand)typeof(PythonStateClient).GetField("pending",flags).GetValue(client);
            if(pending.kind!="resolve_growth_mastery" || pending.choice!="play" || pending.cat_id!=growth.cat_id) throw new Exception("Mastery command failed");
            typeof(PythonStateClient).GetField("pending",flags).SetValue(client,null);
            state.growth_choices[0].choices[2].can_select=false; state.growth_choices[0].choices[2].reason="先に受け入れ依頼に回答してください。"; preview.ApplyPythonState(state);
            if(panel.choices[2].interactable || !panel.details.text.Contains("受け入れ")) throw new Exception("Rule guard failed");
            panel.Disconnect(); foreach(var button in panel.choices) if(button.interactable) throw new Exception("Offline guard failed");
            state.growth_choices=new PythonGrowthChoice[0]; preview.ApplyPythonState(state); if(panel.panel.activeSelf) throw new Exception("Event dismissal failed");
            Debug.Log("CAT_CAFE_GROWTH_DISPLAY_OK: details / manual progress guard / pending / rule / offline / dismissal");
        }
        public static void BuildAndCapture() { Build(); VerifyAndCapture(); }
    }
}
