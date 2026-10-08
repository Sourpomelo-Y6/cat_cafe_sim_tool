using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview.Editor
{
    public static class PythonIntakeSetup
    {
        [MenuItem("Cat Cafe/開発用/UI再生成/猫の受け入れ依頼", false, 1)]
        public static void Build()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var preview = UnityEngine.Object.FindObjectOfType<ContentPreview>();
            var root = preview.transform.Find("Content Canvas/Background");
            var old = root.Find("Intake Request"); if(old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var overlay = Box(root, "Intake Request", 0, 174, 1280, 626, new Color(0,0,0,.55f));
            var card = Box(overlay.transform, "Request Card", 284, 0, 712, 610, new Color(.98f,.97f,.93f,1));
            var panel = preview.GetComponent<PythonIntakePanel>() ?? preview.gameObject.AddComponent<PythonIntakePanel>();
            panel.panel = overlay;
            panel.title = Label(card.transform,"Title",30,22,652,54,25);
            panel.picture = Portrait(card.transform,30,86,180,180);
            panel.details = Label(card.transform,"Cat Details",230,86,452,400,19);
            panel.notice = Label(card.transform,"Response Notice",30,502,652,50,17);
            panel.notice.lineSpacing=1;
            panel.accept = Button(card.transform,"Accept Cat","迎える",142,558);
            panel.decline = Button(card.transform,"Decline Cat","見送る",382,558);
            UnityEventTools.AddPersistentListener(panel.accept.onClick,panel.Accept);
            UnityEventTools.AddPersistentListener(panel.decline.onClick,panel.Decline);
            panel.title.text="保護猫の受け入れ依頼";
            panel.details.text="依頼が届くと、猫の情報と受け入れ費用を表示します。";
            panel.notice.text="迎えるか見送るかを選んでください。";
            panel.accept.interactable=panel.decline.interactable=false; overlay.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("CAT_CAFE_INTAKE_UI_OK: serialized request panel / cat details / accept and decline");
        }
        static void Place(RectTransform rect,float x,float y,float width,float height)
        {
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-y); rect.sizeDelta=new Vector2(width,height);
        }
        public static RawImage Portrait(Transform parent,float x,float y,float width,float height)
        {
            var frame = Box(parent,"Cat Portrait",x,y,width,height,new Color(.88f,.90f,.84f,1));
            var image = new GameObject("Picture",typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter));
            image.transform.SetParent(frame.transform,false);
            var rect = image.GetComponent<RectTransform>();
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,.5f);rect.offsetMin=rect.offsetMax=Vector2.zero;
            var picture=image.GetComponent<RawImage>();picture.texture=ContentPreview.CatPlaceholderTexture();picture.raycastTarget=false;
            var aspect=image.GetComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio=picture.texture == null ? 1 : (float)picture.texture.width/picture.texture.height;
            return picture;
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
            Place(go.GetComponent<RectTransform>(),x,y,188,44); var text=go.GetComponentInChildren<Text>();
            text.font=ContentPreviewLayout.LoadFont(); text.fontSize=22; text.text=caption; return go.GetComponent<Button>();
        }
        public static void VerifyAndCapture()
        {
            var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>(); preview.Initialize();
            var state=PythonServiceSetup.Sample(); state.day=4; state.tick=0; state.can_set_shifts=true;
            state.seats=new PythonSeatState[0]; state.customers=new PythonCustomerState[0];
            state.intake_request=new PythonIntakeState {cat_id="request-cat-1",name="ナナ",day=4,status="waiting",cost=200,
                personality="穏やかな甘えん坊",features="白黒 / 長毛",trait="特性：のんびり\n接客ストレス：通常の0.8倍",
                housing="在籍 5匹 / 上限 8匹 / 空き 3匹",can_accept=true,can_decline=true};
            var client=preview.GetComponent<PythonStateClient>();
            typeof(PythonStateClient).GetField("preview",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(client,preview);
            typeof(PythonStateClient).GetMethod("Accept",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(client,new object[]{state});
            var panel=preview.GetComponent<PythonIntakePanel>();
            if(!panel.panel.activeSelf || !panel.accept.interactable || !panel.decline.interactable || !panel.details.text.Contains("200"))throw new Exception("Intake display mismatch");
            Canvas.ForceUpdateCanvases();
            typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)
                .Invoke(null,new object[]{preview,"Logs/IntakeRequest.png"});
            state.intake_request.can_accept=false; state.intake_request.accept_reason="受け入れ後に資金が残る必要があります。"; preview.ApplyPythonState(state);
            if(panel.accept.interactable || !panel.decline.interactable || !panel.notice.text.Contains("資金"))throw new Exception("Insufficient funds guard failed");
            panel.Disconnect(); if(panel.accept.interactable || panel.decline.interactable)throw new Exception("Offline intake guard failed");
            state.intake_request.status="accepted"; preview.ApplyPythonState(state); if(panel.panel.activeSelf)throw new Exception("Resolved panel remains visible");
            Debug.Log("CAT_CAFE_INTAKE_DISPLAY_OK: waiting details / affordability / offline / resolved dismissal");
        }
        public static void BuildAndCapture() { Build(); VerifyAndCapture(); }
    }
}
