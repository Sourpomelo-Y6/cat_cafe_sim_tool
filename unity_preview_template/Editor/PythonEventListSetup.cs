using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonEventListSetup
    {
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static GameObject Box(Transform p,string n,float x,float y,float w,float h,Color c){var g=new GameObject(n,typeof(RectTransform),typeof(Image));g.transform.SetParent(p,false);Place(g.GetComponent<RectTransform>(),x,y,w,h);g.GetComponent<Image>().color=c;return g;}
        static Text Label(Transform p,string n,float x,float y,float w,float h,int size){var g=new GameObject(n,typeof(RectTransform),typeof(Text));g.transform.SetParent(p,false);Place(g.GetComponent<RectTransform>(),x,y,w,h);var t=g.GetComponent<Text>();t.font=ContentPreviewLayout.LoadFont();t.fontSize=size;t.color=new Color(.16f,.21f,.18f);t.raycastTarget=false;t.supportRichText=false;return t;}
        static Button Button(Transform p,string n,string caption,float x,float y,float w,float h){var g=DefaultControls.CreateButton(new DefaultControls.Resources());g.name=n;g.transform.SetParent(p,false);Place(g.GetComponent<RectTransform>(),x,y,w,h);var t=g.GetComponentInChildren<Text>();t.font=ContentPreviewLayout.LoadFont();t.fontSize=18;t.text=caption;t.supportRichText=false;return g.GetComponent<Button>();}
        [MenuItem("Cat Cafe/開発用/UI再生成/イベント一覧",false,21)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var root=preview.transform.Find("Content Canvas/Background");
            foreach(var name in new[]{"Event List","Event List Entry","Management Menu","Management Menu Entry","Auto Play Activity"}){var old=root.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
            var list=preview.GetComponent<PythonEventListPanel>() ?? preview.gameObject.AddComponent<PythonEventListPanel>();
            var preparation=preview.GetComponent<PythonPreparationPanel>();
            var oldVisiting=preparation.close.transform.parent.Find("Visiting Cat Entry");
            if(oldVisiting!=null)UnityEngine.Object.DestroyImmediate(oldVisiting.gameObject);
            if(preparation.dayOff==null)Place(preparation.close.GetComponent<RectTransform>(),480,550,436,40);
            else Place(preparation.close.GetComponent<RectTransform>(),620,550,296,40);
            list.open=Button(root,"Event List Entry","イベント一覧",560,128,156,40);UnityEventTools.AddPersistentListener(list.open.onClick,list.Open);
            list.panel=Box(root,"Event List",0,174,1280,626,new Color(0,0,0,.55f));var card=Box(list.panel.transform,"Card",170,8,940,600,new Color(.98f,.97f,.93f));Label(card.transform,"Title",24,18,892,50,25).text="イベント一覧";
            list.entries=new Button[25];var callbacks=new UnityEngine.Events.UnityAction[]{list.Visiting,list.Intake,list.Store,list.Growth,list.Missing,list.Adoption,list.Dispatch,list.Regular,list.Reservation,list.Trust,list.GeneralDispatch,list.DispatchChoice,list.DispatchTrouble,list.DispatchIntroduction,list.Items,list.Expansion,list.SeatEquipment,list.RestSpace,list.WaitingArea,list.CustomerDirectory,list.PetShop,list.ObjectiveStart,list.DispatchComparison,list.ServiceAssignment,list.AutoPlay};
            var area=Box(card.transform,"Events Scroll",24,76,892,404,new Color(.93f,.94f,.89f));var scroll=area.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            var viewport=Box(area.transform,"Viewport",0,0,872,404,Color.white);viewport.AddComponent<Mask>().showMaskGraphic=false;scroll.viewport=viewport.GetComponent<RectTransform>();
            var content=new GameObject("Content",typeof(RectTransform));content.transform.SetParent(viewport.transform,false);Place(content.GetComponent<RectTransform>(),0,0,872,14*40);scroll.content=content.GetComponent<RectTransform>();
            var bar=DefaultControls.CreateScrollbar(new DefaultControls.Resources());bar.name="Scrollbar";bar.transform.SetParent(area.transform,false);Place(bar.GetComponent<RectTransform>(),878,0,14,404);scroll.verticalScrollbar=bar.GetComponent<Scrollbar>();scroll.verticalScrollbar.direction=Scrollbar.Direction.BottomToTop;
            // Keep legacy route slots for serialized scenes; only event routes appear in this list.
            for(int i=0;i<list.entries.Length;i++){list.entries[i]=Button(content.transform,"Event "+i,"状態を取得しています",4,i*40,860,36);list.entries[i].GetComponentInChildren<Text>().alignment=TextAnchor.MiddleLeft;UnityEventTools.AddPersistentListener(list.entries[i].onClick,callbacks[i]);list.entries[i].gameObject.SetActive(i<14);}
            BuildMenu(preview,root,list);
            BuildActivity(preview,root);
            list.notice=Label(card.transform,"Notice",24,492,892,50,16);list.close=Button(card.transform,"Close","閉じる",480,550,436,40);UnityEventTools.AddPersistentListener(list.close.onClick,list.Close);
            if(preview.GetComponent<PythonBusinessHeader>()!=null)PythonBusinessHeaderSetup.Configure(preview);
            list.open.interactable=false;list.panel.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("CAT_CAFE_EVENT_LIST_UI_OK");
        }
        static void BuildMenu(ContentPreview preview,Transform root,PythonEventListPanel list)
        {
            var menu=preview.GetComponent<PythonMenuPanel>() ?? preview.gameObject.AddComponent<PythonMenuPanel>();
            menu.open=Button(root,"Management Menu Entry","メニュー",392,128,156,40);UnityEventTools.AddPersistentListener(menu.open.onClick,menu.Open);
            menu.panel=Box(root,"Management Menu",0,174,1280,626,new Color(0,0,0,.55f));var card=Box(menu.panel.transform,"Card",170,8,940,600,new Color(.98f,.97f,.93f));
            menu.title=Label(card.transform,"Title",24,18,892,50,25);menu.title.text="メニュー / 営業";menu.tabs=new Button[4];
            for(int i=0;i<menu.tabs.Length;i++){menu.tabs[i]=Button(card.transform,"Category "+i,PythonMenuPanel.GroupNames[i],24+i*226,76,214,44);UnityEventTools.AddIntPersistentListener(menu.tabs[i].onClick,menu.SelectGroup,i);menu.tabs[i].onClick.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);}
            menu.entries=new Button[PythonMenuPanel.Routes.Length];
            for(int i=0;i<menu.entries.Length;i++){menu.entries[i]=Button(card.transform,"Menu "+i,"状態を取得しています",24,148,892,46);menu.entries[i].GetComponentInChildren<Text>().fontSize=18;menu.entries[i].GetComponentInChildren<Text>().alignment=TextAnchor.MiddleLeft;UnityEventTools.AddIntPersistentListener(menu.entries[i].onClick,menu.Select,i);menu.entries[i].onClick.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);menu.entries[i].gameObject.SetActive(PythonMenuPanel.Groups[i]==0);}
            menu.detailPanels=new GameObject[menu.entries.Length];menu.detailClose=new Button[menu.entries.Length];
            string[] names={"PythonAutoPlayPanel","PythonServiceAssignmentPanel","PythonObjectiveStartPanel","PythonItemsPanel","PythonExpansionPanel","PythonSeatEquipmentPanel","PythonRestSpacePanel","PythonWaitingAreaPanel","PythonCustomerDirectoryPanel","PythonPetShopPanel","PythonDispatchComparisonPanel","PythonDispatchPanel","PythonGeneralDispatchPanel","PythonBusinessLogPanel","PythonClearResultsPanel","PythonFinanceHistoryPanel","PythonRecruitmentPanel"};
            for(int i=0;i<names.Length;i++){var component=preview.GetComponent(names[i]);if(component==null)continue;menu.detailPanels[i]=(GameObject)component.GetType().GetField("panel").GetValue(component);menu.detailClose[i]=(Button)component.GetType().GetField("close").GetValue(component);}
            menu.notice=Label(card.transform,"Notice",24,492,892,50,16);menu.close=Button(card.transform,"Close","営業画面へ",480,550,436,40);UnityEventTools.AddPersistentListener(menu.close.onClick,menu.Close);menu.panel.SetActive(false);
        }
        static void BuildActivity(ContentPreview preview,Transform root)
        {
            var p=preview.GetComponent<PythonAutoPlayPanel>();if(p==null)return;p.activity=Box(root,"Auto Play Activity",24,76,760,40,new Color(.82f,.9f,.78f));p.activityStatus=Label(p.activity.transform,"Status",12,0,416,40,17);p.activityStatus.alignment=TextAnchor.MiddleLeft;p.activityStop=Button(p.activity.transform,"Stop","おまかせ停止",432,0,158,40);p.activityDetails=Button(p.activity.transform,"Details","実行の詳細",602,0,158,40);UnityEventTools.AddPersistentListener(p.activityStop.onClick,p.Stop);UnityEventTools.AddPersistentListener(p.activityDetails.onClick,p.Open);p.activityStop.onClick.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);p.activityDetails.onClick.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);p.activity.SetActive(false);
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="event-list-test";state.can_set_shifts=true;state.required_action="";state.visiting_cat=new PythonVisitingCat{cat_id="visiting-cat-1",content_cat_id="visiting-cat-1",name="ココ",status="visiting",progress=1,required=3,can_interact=true,can_skip=true};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});apply();var list=preview.GetComponent<PythonEventListPanel>();list.Open();
            if(preview.GetComponent<PythonPreparationPanel>().close.transform.parent.Find("Visiting Cat Entry")!=null)throw new Exception("Old visiting entry remains");
            if(!list.panel.activeSelf || !list.entries[0].interactable || list.entries[1].interactable || !list.entries[0].GetComponentInChildren<Text>().text.Contains("1/3"))throw new Exception("Event list status mismatch");
            if(Array.FindAll(list.entries,b=>b.gameObject.activeSelf).Length!=14)throw new Exception("Management items remain in event list");var menu=preview.GetComponent<PythonMenuPanel>();menu.Open();if(list.panel.activeSelf || !menu.panel.activeSelf || menu.entries.Length!=PythonMenuPanel.Routes.Length)throw new Exception("Menu and event list are not separated");list.Open();if(menu.panel.activeSelf)throw new Exception("Opening events did not close menu");
            Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/EventList.png"});
            list.Visiting();if(list.panel.activeSelf || !preview.GetComponent<PythonVisitingCatPanel>().panel.activeSelf)throw new Exception("Visiting navigation failed");
            list.Open();list.Disconnect();if(list.entries[0].interactable)throw new Exception("Offline event navigation enabled");apply();
            state.intake_request=new PythonIntakeState{cat_id="test",name="ハル",status="waiting",can_decline=true};state.required_action="受け入れに回答してください。";apply();list.Open();preview.RefreshPythonControls();if(list.panel.transform.GetSiblingIndex()!=list.panel.transform.parent.childCount-1)throw new Exception("Pending event covered list");
            list.Intake();if(list.panel.activeSelf || !preview.GetComponent<PythonIntakePanel>().panel.activeSelf)throw new Exception("Intake navigation failed");
            list.Open();state.instance_id="new-game";apply();if(list.panel.activeSelf)throw new Exception("Old list persisted across load");Debug.Log("CAT_CAFE_EVENT_LIST_DISPLAY_OK");
        }
    }
}
