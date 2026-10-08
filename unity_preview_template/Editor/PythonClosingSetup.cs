using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonClosingSetup
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
        static Text ScrollText(Transform parent,string name,float x,float y,float w,float h,int size,out ScrollRect scroll)
        {
            var area=Box(parent,name,x,y,w,h,new Color(.93f,.94f,.89f));scroll=area.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            var viewport=Box(area.transform,"Viewport",8,6,w-32,h-12,Color.white);viewport.AddComponent<Mask>().showMaskGraphic=false;
            var text=Label(viewport.transform,"Text",0,0,w-32,h-12,size);text.lineSpacing=1;text.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport=viewport.GetComponent<RectTransform>();scroll.content=text.rectTransform;
            var bar=DefaultControls.CreateScrollbar(new DefaultControls.Resources());bar.transform.SetParent(area.transform,false);Place(bar.GetComponent<RectTransform>(),w-18,6,12,h-12);scroll.verticalScrollbar=bar.GetComponent<Scrollbar>();scroll.verticalScrollbar.direction=Scrollbar.Direction.BottomToTop;
            return text;
        }
        [MenuItem("Cat Cafe/開発用/UI再生成/閉店の振り返り",false,13)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var root=preview.transform.Find("Content Canvas/Background");
            var old=root.Find("Closing Review");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var overlay=Box(root,"Closing Review",0,174,1280,626,new Color(0,0,0,.55f));var card=Box(overlay.transform,"Review Card",170,8,940,600,new Color(.98f,.97f,.93f,1));
            var panel=preview.GetComponent<PythonClosingPanel>() ?? preview.gameObject.AddComponent<PythonClosingPanel>();panel.panel=overlay;
            panel.title=Label(card.transform,"Title",24,18,892,40,25);panel.title.text="営業の振り返り";
            panel.title.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,600);
            panel.history=Button(card.transform,"History","収支の内訳・日次比較",660,18,256);UnityEventTools.AddPersistentListener(panel.history.onClick,panel.History);panel.history.onClick.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
            var finance=Box(card.transform,"Finance",24,70,892,126,new Color(.91f,.93f,.87f));
            panel.financeDetails=Label(finance.transform,"Finance Details",12,8,868,112,18);
            Label(card.transform,"Goal Heading",24,208,340,28,18).text="目標の進捗";
            Label(card.transform,"Cats Heading",388,208,528,28,18).text="猫たちの状態・翌日の準備";
            panel.goalDetails=ScrollText(card.transform,"Goal Scroll",24,244,340,238,17,out panel.goalScroll);
            panel.details=ScrollText(card.transform,"Cats Scroll",388,244,528,238,17,out panel.catsScroll);
            ScrollRect noticeScroll;panel.notice=ScrollText(card.transform,"Notice Scroll",24,490,892,54,14,out noticeScroll);
            panel.save=Button(card.transform,"Save","この日の状態を保存",24,550,270);UnityEventTools.AddPersistentListener(panel.save.onClick,panel.Save);
            panel.next=Button(card.transform,"Next","翌日の準備へ",314,550,300);UnityEventTools.AddPersistentListener(panel.next.onClick,panel.Next);
            panel.next.GetComponent<Image>().color=new Color(.2f,.38f,.3f);panel.next.GetComponentInChildren<Text>().color=Color.white;
            panel.save.GetComponent<Image>().color=new Color(.88f,.91f,.84f);
            panel.close=Button(card.transform,"Close","閉じて猫一覧へ",634,550,282);UnityEventTools.AddPersistentListener(panel.close.onClick,panel.Close);
            var entry=root.Find("Closing Entry");
            if(entry!=null)panel.open=entry.GetComponent<Button>();
            else {panel.open=Button(root,"Closing Entry","1日の振り返り",384,128,240);UnityEventTools.AddPersistentListener(panel.open.onClick,panel.Open);}
            panel.open.gameObject.SetActive(false);overlay.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("CAT_CAFE_CLOSING_UI_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="closing";state.closed=false;state.can_set_shifts=false;state.required_action="";
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});apply();var panel=preview.GetComponent<PythonClosingPanel>();if(panel.panel.activeSelf)throw new Exception("Review shown during business");
            state.closed=true;state.finance=new PythonFinanceState{opening_funds=1000,total_income=100,total_expenses=50,net_cash_flow=50,closing_funds=1050};state.revenue=80;state.funds=1050;foreach(var cat in state.cats){cat.health_label="健康";cat.activity_label="在店";}state.objective_progress=new PythonObjectiveProgress{label="自由営業",details="期限なし・自由に営業"};apply();
            if(!panel.panel.activeSelf || !panel.save.interactable || !panel.next.interactable || !panel.financeDetails.text.Contains("+50") || !panel.details.text.Contains("疲労"))throw new Exception("Closing review missing");
            Canvas.ForceUpdateCanvases();
            if(panel.financeDetails.preferredHeight>panel.financeDetails.rectTransform.rect.height+1)throw new Exception("Finance summary clipped");
            string shortGoal=state.objective_progress.details;state.objective_progress.details=new string('猫',3000);apply();Canvas.ForceUpdateCanvases();
            if(panel.goalScroll.content.rect.height<=panel.goalScroll.viewport.rect.height || panel.catsScroll.content.rect.height<=panel.catsScroll.viewport.rect.height)throw new Exception("Independent review scrolling missing");
            state.objective_progress.details=shortGoal;state.finance.net_cash_flow=-25;apply();if(!panel.financeDetails.text.Contains("-25"))throw new Exception("Negative balance missing");state.finance.net_cash_flow=50;apply();
            panel.Close();apply();if(panel.panel.activeSelf)throw new Exception("Review reopened during polling");panel.Open();
            state.required_action="先に帰還を確認してください。";apply();if(panel.next.interactable || !panel.notice.text.Contains("帰還"))throw new Exception("Required event guard failed");
            typeof(PythonStateClient).GetField("online",flags).SetValue(client,false);panel.Disconnect();if(panel.save.interactable || panel.next.interactable || !panel.notice.text.Contains("未接続"))throw new Exception("Offline guard failed");
            state.required_action="";apply();panel.Notify("保存しました。");if(!panel.notice.text.Contains("保存しました"))throw new Exception("Save notice missing");
            Canvas.ForceUpdateCanvases();if(panel.notice.preferredHeight>panel.notice.GetComponentInParent<ScrollRect>().viewport.rect.height+1)throw new Exception("Save guidance clipped");typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/ClosingReview.png"});
            state.closed=false;state.can_set_shifts=true;state.day++;apply();if(panel.panel.activeSelf || panel.open.gameObject.activeSelf)throw new Exception("Review remained next day");
            state.closed=true;apply();if(!panel.panel.activeSelf)throw new Exception("Next closing not shown");state.game_over=new PythonGameOver{reason="funds",day=state.day};apply();if(panel.panel.activeSelf || panel.open.gameObject.activeSelf)throw new Exception("Review obscured game over");
            Debug.Log("CAT_CAFE_CLOSING_DISPLAY_OK");
        }
    }
}
