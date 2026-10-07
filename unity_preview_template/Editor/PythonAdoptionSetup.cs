using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonAdoptionSetup
    {
        static void Place(RectTransform rect,float x,float y,float w,float h)
        { rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h); }
        static GameObject Box(Transform parent,string name,float x,float y,float w,float h,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
            Place(go.GetComponent<RectTransform>(),x,y,w,h);go.GetComponent<Image>().color=color;return go;
        }
        static Text Label(Transform parent,string name,float x,float y,float w,float h,int size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
            Place(go.GetComponent<RectTransform>(),x,y,w,h);var text=go.GetComponent<Text>();text.font=ContentPreviewLayout.LoadFont();text.fontSize=size;
            text.color=new Color(.16f,.21f,.18f);text.raycastTarget=false;text.supportRichText=false;return text;
        }
        static Button Button(Transform parent,string name,string caption,float x,float y,float w)
        {
            var go=DefaultControls.CreateButton(new DefaultControls.Resources());go.name=name;go.transform.SetParent(parent,false);
            Place(go.GetComponent<RectTransform>(),x,y,w,40);var text=go.GetComponentInChildren<Text>();text.font=ContentPreviewLayout.LoadFont();text.fontSize=18;text.text=caption;return go.GetComponent<Button>();
        }
        static Dropdown Dropdown(Transform parent,string name,float y)
        {
            var go=DefaultControls.CreateDropdown(new DefaultControls.Resources());go.name=name;go.transform.SetParent(parent,false);
            Place(go.GetComponent<RectTransform>(),24,y,892,40);var drop=go.GetComponent<Dropdown>();
            foreach(var text in go.GetComponentsInChildren<Text>(true)){text.font=ContentPreviewLayout.LoadFont();text.fontSize=18;}
            drop.itemText.rectTransform.parent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,36);
            drop.template.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,180);drop.itemText.verticalOverflow=VerticalWrapMode.Overflow;drop.itemText.alignment=TextAnchor.MiddleLeft;
            go.transform.Find("Arrow").GetComponent<Image>().color=Color.clear;Label(go.transform,"Arrow Text",862,7,26,26,16).text="▼";return drop;
        }
        [MenuItem("Cat Cafe/開発用/UI再生成/猫の譲渡",false,10)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var root=preview.transform.Find("Content Canvas/Background");
            var old=root.Find("Cat Adoption");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var overlay=Box(root,"Cat Adoption",0,174,1280,626,new Color(0,0,0,.55f));
            var card=Box(overlay.transform,"Adoption Card",170,8,940,600,new Color(.98f,.97f,.93f,1));
            var panel=preview.GetComponent<PythonAdoptionPanel>() ?? preview.gameObject.AddComponent<PythonAdoptionPanel>();panel.panel=overlay;
            Label(card.transform,"Title",24,20,892,40,25).text="猫の譲渡・申し出";
            panel.configure=Button(card.transform,"Configure","申し出の発生：OFF → ONにする",24,72,892);UnityEventTools.AddPersistentListener(panel.configure.onClick,panel.Configure);
            panel.settings=Label(card.transform,"Settings",24,120,892,76,16);
            panel.events=Dropdown(card.transform,"Offers",206);UnityEventTools.AddPersistentListener(panel.events.onValueChanged,panel.Select);
            panel.details=Label(card.transform,"Details",24,258,892,208,18);
            panel.notice=Label(card.transform,"Notice",24,480,892,48,16);
            panel.accept=Button(card.transform,"Accept","譲渡する（確認へ）",24,550,270);UnityEventTools.AddPersistentListener(panel.accept.onClick,panel.BeginAccept);
            panel.decline=Button(card.transform,"Decline","見送る",314,550,270);UnityEventTools.AddPersistentListener(panel.decline.onClick,panel.Decline);
            panel.close=Button(card.transform,"Close","閉じる",604,550,312);UnityEventTools.AddPersistentListener(panel.close.onClick,panel.Close);
            panel.confirmation=Box(card.transform,"Adoption Confirmation",0,0,940,600,new Color(.98f,.97f,.93f,1));
            Label(panel.confirmation.transform,"Title",24,20,892,40,25).text="譲渡の最終確認";
            panel.confirmationDetails=Label(panel.confirmation.transform,"Details",24,100,892,360,20);
            panel.confirmAccept=Button(panel.confirmation.transform,"Confirm","この猫を譲渡する",24,550,432);UnityEventTools.AddPersistentListener(panel.confirmAccept.onClick,panel.ConfirmAccept);
            panel.cancel=Button(panel.confirmation.transform,"Cancel","戻る",480,550,436);UnityEventTools.AddPersistentListener(panel.cancel.onClick,panel.CancelAccept);
            panel.confirmation.SetActive(false);
            var detail=preview.GetComponent<PythonCatDetailsPanel>();var detailCard=detail.panel.transform.Find("Details Card");
            var entry=detailCard.Find("Adoption Entry");if(entry!=null)UnityEngine.Object.DestroyImmediate(entry.gameObject);
            panel.configure.interactable=panel.accept.interactable=panel.decline.interactable=false;
            detail.adoption=Button(detailCard,"Adoption Entry","譲渡の設定・履歴",516,20,188);detail.adoption.GetComponentInChildren<Text>().fontSize=16;
            UnityEventTools.AddPersistentListener(detail.adoption.onClick,detail.Adoption);detail.adoption.interactable=false;
            overlay.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("CAT_CAFE_ADOPTION_UI_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.adoption=new PythonAdoption{can_configure=true,events=new PythonAdoptionEvent[0]};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});apply();
            var detail=preview.GetComponent<PythonCatDetailsPanel>();detail.Open(state.cats[0].cat_id);detail.Adoption();var panel=preview.GetComponent<PythonAdoptionPanel>();
            if(!panel.panel.activeSelf || !panel.configure.interactable || panel.accept.interactable)throw new Exception("Adoption settings entry failed");
            state.adoption.enabled=true;state.adoption.can_configure=false;state.adoption.events=new[]{new PythonAdoptionEvent{event_id="adoption-1",cat_id=state.cats[0].cat_id,name="ミケ",customer_id="guest-1",customer_name="佐藤さん",day=1,status="waiting",guest_affinity=85,player_affinity=20,threshold=80,can_accept=true,can_decline=true}};apply();
            if(!panel.accept.interactable || !panel.decline.interactable || panel.close.interactable || panel.configure.interactable)throw new Exception("Waiting offer guards failed");
            Canvas.ForceUpdateCanvases();if(panel.details.preferredHeight>panel.details.rectTransform.rect.height || panel.settings.preferredHeight>panel.settings.rectTransform.rect.height)throw new Exception("Adoption text clipped");
            Capture(preview,"Logs/AdoptionOffer.png");panel.BeginAccept();
            if(!panel.confirmation.activeSelf || !panel.confirmAccept.interactable || !panel.confirmationDetails.text.Contains("ミケ") || !panel.confirmationDetails.text.Contains("佐藤") || !panel.confirmationDetails.text.Contains("戻せません"))throw new Exception("Final confirmation missing");
            Capture(preview,"Logs/AdoptionConfirmation.png");panel.CancelAccept();if(panel.confirmation.activeSelf || !panel.decline.interactable)throw new Exception("Cancel did not restore offer");
            panel.BeginAccept();panel.Disconnect();if(panel.confirmation.activeSelf || panel.accept.interactable)throw new Exception("Offline confirmation enabled");apply();
            state.adoption.events[0].status="resolved";state.adoption.events[0].choice="accept";state.adoption.events[0].can_accept=state.adoption.events[0].can_decline=false;apply();
            if(panel.accept.interactable || panel.decline.interactable || !panel.close.interactable || panel.confirmation.activeSelf)throw new Exception("Resolved offer remained actionable");
            panel.Close();apply();if(panel.panel.activeSelf)throw new Exception("Result reopened during polling");
            Debug.Log("CAT_CAFE_ADOPTION_DISPLAY_OK");
        }
        static void Capture(ContentPreview preview,string path)
        { typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,path}); }
    }
}
