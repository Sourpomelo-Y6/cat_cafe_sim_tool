using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonPreparationSetup
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
        [MenuItem("Cat Cafe/開発用/UI再生成/営業準備の案内",false,12)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var root=preview.transform.Find("Content Canvas/Background");
            var old=root.Find("Preparation Guide");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var overlay=Box(root,"Preparation Guide",0,174,1280,626,new Color(0,0,0,.55f));var card=Box(overlay.transform,"Guide Card",170,8,940,600,new Color(.98f,.97f,.93f,1));
            var panel=preview.GetComponent<PythonPreparationPanel>() ?? preview.gameObject.AddComponent<PythonPreparationPanel>();panel.panel=overlay;
            panel.title=Label(card.transform,"Title",24,18,892,40,25);panel.title.text="営業の準備";
            var status=Box(card.transform,"Status",24,70,892,70,new Color(.91f,.93f,.87f));panel.summary=Label(status.transform,"Summary",12,8,868,58,18);
            Label(card.transform,"Goal Heading",24,150,340,28,18).text="目標と営業の進め方";
            panel.details=ScrollText(card.transform,"Goal Scroll",24,184,340,238,17,out panel.goalScroll);
            var drop=DefaultControls.CreateDropdown(new DefaultControls.Resources());drop.name="Cats";drop.transform.SetParent(card.transform,false);Place(drop.GetComponent<RectTransform>(),388,150,528,40);panel.cats=drop.GetComponent<Dropdown>();
            foreach(var text in drop.GetComponentsInChildren<Text>(true)){text.font=ContentPreviewLayout.LoadFont();text.fontSize=17;text.color=new Color(.16f,.25f,.21f);}
            panel.cats.itemText.rectTransform.parent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,36);panel.cats.template.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,180);panel.cats.itemText.verticalOverflow=VerticalWrapMode.Overflow;
            drop.transform.Find("Arrow").GetComponent<Image>().color=Color.clear;Label(drop.transform,"Dropdown Arrow",494,7,26,26,16).text="▼";
            UnityEventTools.AddPersistentListener(panel.cats.onValueChanged,panel.SelectCat);
            ScrollRect forecastScroll;panel.selectedDetails=ScrollText(card.transform,"Selected Details",388,202,528,174,15,out forecastScroll);
            panel.work=Button(card.transform,"Work","出勤にする",388,382,250);UnityEventTools.AddPersistentListener(panel.work.onClick,panel.Work);
            panel.rest=Button(card.transform,"Rest","休養にする",656,382,260);UnityEventTools.AddPersistentListener(panel.rest.onClick,panel.Rest);
            panel.work.GetComponent<Image>().color=new Color(.88f,.91f,.84f);
            panel.catsDetails=ScrollText(card.transform,"Cats Scroll",388,430,528,52,15,out panel.catsScroll);
            panel.propose=Button(card.transform,"Rest Proposal","休養案を作成",24,430,140);UnityEventTools.AddPersistentListener(panel.propose.onClick,panel.ProposeRest);
            panel.confirmProposal=Button(card.transform,"Confirm Proposal","案を確定",174,430,90);UnityEventTools.AddPersistentListener(panel.confirmProposal.onClick,panel.ConfirmProposal);
            panel.cancelProposal=Button(card.transform,"Cancel Proposal","取消",274,430,90);UnityEventTools.AddPersistentListener(panel.cancelProposal.onClick,panel.CancelProposal);
            panel.confirmProposal.GetComponentInChildren<Text>().fontSize=16;
            panel.details.text="";
            ScrollRect noticeScroll;panel.notice=ScrollText(card.transform,"Notice Scroll",24,490,892,54,14,out noticeScroll);
            panel.start=Button(card.transform,"Start Business","営業開始",24,550,282);UnityEventTools.AddPersistentListener(panel.start.onClick,panel.StartBusiness);
            panel.start.GetComponent<Image>().color=new Color(.2f,.38f,.3f);panel.start.GetComponentInChildren<Text>().color=Color.white;
            panel.dayOff=Button(card.transform,"Day Off","今日は休業する",322,550,282);UnityEventTools.AddPersistentListener(panel.dayOff.onClick,panel.OpenDayOff);
            panel.close=Button(card.transform,"Close","閉じて猫一覧へ",620,550,296);UnityEventTools.AddPersistentListener(panel.close.onClick,panel.Close);
            var entry=root.Find("Preparation Entry");if(entry!=null)panel.open=entry.GetComponent<Button>();else {panel.open=Button(root,"Preparation Entry","営業準備の案内",332,24,240);UnityEventTools.AddPersistentListener(panel.open.onClick,panel.Open);}
            panel.open.gameObject.SetActive(false);overlay.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("CAT_CAFE_PREPARATION_UI_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.seats=new[]{new PythonSeatState(),new PythonSeatState()};state.instance_id="guide-first";state.day=1;state.tick=0;state.closed=false;state.can_set_shifts=true;state.required_action="";state.objective="free";state.new_game_options=new[]{new PythonNewGameOption{choice="free",label="自由営業",detail="期限なし・自由に営業",conditions="初期猫：5匹 / 席：2席 / 資金：1000"}};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});apply();var panel=preview.GetComponent<PythonPreparationPanel>();
            if(!panel.panel.activeSelf || !panel.details.text.Contains("自由営業") || !panel.details.text.Contains("自動進行") || !panel.summary.text.Contains("1000"))throw new Exception("First preparation guide missing");
            if(!panel.summary.text.Contains("出勤 5匹") || !panel.summary.text.Contains("席 2席") || !panel.start.interactable)throw new Exception("Working count or start control missing");
            state.cats[0].working=false;state.cats[0].health_status="sick";state.cats[0].health_label="病気";panel.SelectCat(0);apply();if(panel.work.interactable || !panel.selectedDetails.text.Contains("出勤できません"))throw new Exception("Unavailable cat enabled");
            state.cats[0].health_status="healthy";state.cats[0].health_label="健康";state.cats[0].working=true;apply();
            panel.Close();apply();if(panel.panel.activeSelf)throw new Exception("Polling reopened dismissed guide");
            foreach(var cat in state.cats)cat.working=false;panel.Open();apply();if(panel.start.interactable || !panel.notice.text.Contains("出勤する猫がいません"))throw new Exception("No working cat reason missing");
            state.required_action="先に帰還を確認してください。";apply();if(panel.start.interactable || !panel.notice.text.Contains("帰還"))throw new Exception("Required action reason missing");
            panel.Disconnect();if(panel.work.interactable || panel.start.interactable || !panel.notice.text.Contains("未接続"))throw new Exception("Offline guide missing");apply();if(panel.notice.text.Contains("未接続"))throw new Exception("Offline message persisted");
            state.required_action="";foreach(var cat in state.cats){cat.health_label="健康";cat.activity_label="在店";}apply();
            state.day=2;apply();if(!panel.panel.activeSelf || !panel.title.text.Contains("2日目"))throw new Exception("Next day preparation missing");
            Canvas.ForceUpdateCanvases();if(panel.summary.preferredHeight>panel.summary.rectTransform.rect.height+1)throw new Exception("Preparation summary clipped");typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/PreparationGuide.png"});
            state.can_set_shifts=false;apply();if(panel.panel.activeSelf || panel.open.gameObject.activeSelf)throw new Exception("Guide remained in business");
            state.can_set_shifts=true;state.game_over=new PythonGameOver{reason="funds",day=1};apply();if(panel.panel.activeSelf || panel.open.gameObject.activeSelf)throw new Exception("Guide obscured game over");
            state.game_over=null;state.instance_id="guide-new";apply();if(!panel.panel.activeSelf)throw new Exception("New game did not show guide");
            foreach(var cat in state.cats)cat.working=true;
            state.rest_proposal=new[]{state.cats[2].cat_id,state.cats[3].cat_id,state.cats[4].cat_id};
            state.cats[0].shift_forecast="前日接客：6行動\n出勤：疲労 / 発症 66 / 12.0% / ストレス 8\n休養：疲労 / 発症 40 / 0.0% / ストレス 0\n前日の接客6行動を基準にしています。当日の担当回数や交流時間によって、実際の疲労・発症確率・ストレスは変わります。";
            apply();panel.SelectCat(0);panel.ProposeRest();
            if(!panel.HasProposal || !panel.summary.text.Contains("出勤 3匹") || panel.start.interactable || !panel.confirmProposal.interactable || !state.cats[0].working || !panel.selectedDetails.text.Contains("12.0%"))throw new Exception("Rest draft: "+panel.HasProposal+" / "+panel.summary.text+" / "+panel.propose.interactable+" / "+panel.confirmProposal.interactable+" / "+panel.selectedDetails.text);
            panel.Work();if(!panel.summary.text.Contains("出勤 4匹") || !panel.HasProposal)throw new Exception("Manual draft edit failed");
            panel.CancelProposal();if(panel.HasProposal || !panel.summary.text.Contains("出勤 5匹"))throw new Exception("Cancel changed shifts");
            panel.ProposeRest();panel.Disconnect();if(panel.confirmProposal.interactable || panel.propose.interactable)throw new Exception("Offline draft enabled");apply();
            panel.Close();panel.Open();if(panel.HasProposal)throw new Exception("Close retained draft");
            panel.ProposeRest();state.revision++;apply();if(panel.HasProposal)throw new Exception("Stale draft retained");
            panel.SelectCat(0);Canvas.ForceUpdateCanvases();
            typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",flags | System.Reflection.BindingFlags.Static).Invoke(null,new object[]{preview,"Logs/ShiftForecast.png"});
            panel.ProposeRest();panel.ConfirmProposal();
            var command=(PythonShiftCommand)typeof(PythonStateClient).GetField("pending",flags).GetValue(client);
            if(command==null || command.working_cats.Length!=3 || panel.confirmProposal.interactable || panel.work.interactable || panel.start.interactable)throw new Exception("Confirmation command or busy guards failed");
            Debug.Log("CAT_CAFE_SHIFT_FORECAST_OK: forecasts / draft / manual edit / cancel / close / stale / offline / confirmation");
            Debug.Log("CAT_CAFE_PREPARATION_DISPLAY_OK");
        }
    }
}
