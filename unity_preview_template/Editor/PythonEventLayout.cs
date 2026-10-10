using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonEventLayout
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
        static Text[] Layout(GameObject panel,Text title,Button[] buttons,out Text details,out Text notice)
        {
            var card=panel.transform.Find("Request Card");Place(card.GetComponent<RectTransform>(),170,8,940,600);
            var keep=new HashSet<GameObject>{title.gameObject};foreach(var button in buttons)keep.Add(button.gameObject);
            var remove=new List<GameObject>();foreach(Transform child in card)if(!keep.Contains(child.gameObject))remove.Add(child.gameObject);foreach(var child in remove)UnityEngine.Object.DestroyImmediate(child);
            Place(title.rectTransform,24,18,892,54);title.fontSize=23;title.lineSpacing=1;
            ScrollRect scroll;details=ScrollText(card,"Event Details",24,86,304,396,17,out scroll);
            var descriptions=new Text[buttons.Length];
            int pitch=400/buttons.Length;
            for(int i=0;i<buttons.Length;i++)
            {
                Place(buttons[i].GetComponent<RectTransform>(),348,86+i*pitch,568,30);buttons[i].GetComponentInChildren<Text>().fontSize=16;buttons[i].GetComponent<Image>().color=new Color(.88f,.91f,.84f);
                descriptions[i]=ScrollText(card,"Choice Details "+i,348,122+i*pitch,568,pitch-40,14,out scroll);
            }
            notice=ScrollText(card,"Response Notice",24,490,892,54,14,out scroll);panel.SetActive(false);return descriptions;
        }
        [MenuItem("Cat Cafe/開発用/UI再生成/イベント確認画面の配置",false,18)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();
            if(preview.GetComponent<PythonEventPresentation>()==null)preview.gameObject.AddComponent<PythonEventPresentation>();
            var store=preview.GetComponent<PythonStoreEventPanel>();store.choiceDetails=Layout(store.panel,store.title,store.choices,out store.details,out store.notice);
            var growth=preview.GetComponent<PythonGrowthPanel>();growth.choiceDetails=Layout(growth.panel,growth.title,growth.choices,out growth.details,out growth.notice);
            var intake=preview.GetComponent<PythonIntakePanel>();intake.choiceDetails=Layout(intake.panel,intake.title,new[]{intake.accept,intake.decline},out intake.details,out intake.notice);
            intake.picture=PythonIntakeSetup.Portrait(intake.panel.transform.Find("Request Card"),24,86,304,168);
            var intakeScroll=intake.details.transform.parent.parent.GetComponent<ScrollRect>();
            Place(intakeScroll.GetComponent<RectTransform>(),24,266,304,216);
            Place(intakeScroll.viewport,8,6,272,204);
            Place(intakeScroll.verticalScrollbar.GetComponent<RectTransform>(),286,6,12,204);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);PythonCatAcquisitionLayout.RefreshIfReady();PythonEventReviewLayout.RefreshIfReady();Debug.Log("CAT_CAFE_EVENT_LAYOUT_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="event-layout";state.can_set_shifts=true;state.required_action="店舗イベントへの対応を選んでください。";
            state.store_event=new PythonStoreEvent{kind="support",title="保護団体からの支援依頼",day=1,funds=1000,choices=new[]{
                new PythonEventChoice{choice="full",label="支援する",detail="資金1200を使い、閉店時に人気を増やします。",reason="資金が足りません。",can_select=false},
                new PythonEventChoice{choice="small",label="少額支援する",detail="資金50を使い、閉店時に人気を増やします。",can_select=true},
                new PythonEventChoice{choice="decline",label="見送る",detail="資金と人気は変化しません。",can_select=true}}};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});apply();var store=preview.GetComponent<PythonStoreEventPanel>();
            if(store.choices[0].interactable || !store.choiceDetails[0].text.Contains("資金が足りません") || !store.details.text.Contains("保護団体"))throw new Exception("Choice reason or event context missing");
            Canvas.ForceUpdateCanvases();Capture(preview,"Logs/EventStoreLayout.png");store.Second();if(store.choices[1].interactable || !store.notice.text.Contains("送信"))throw new Exception("Event busy guard missing");typeof(PythonStateClient).GetField("pending",flags).SetValue(client,null);
            state.store_event=null;state.required_action="";apply();if(store.panel.activeSelf || !preview.GetComponent<PythonEventPresentation>().Message.Contains("対応を反映"))throw new Exception("Resolved event feedback missing");
            state.can_set_shifts=false;state.growth_choices=new[]{new PythonGrowthChoice{cat_id="cat-mike",name="ミケ",kind="resolve_growth_type_mastery",title="個別の交流",learned="声をかける",group="静かな交流",experience=10,threshold=10,choices=new[]{
                new PythonEventChoice{choice="voice",label="声をかける",detail="習得済みです。",reason="すでに習得しています。",can_select=false},new PythonEventChoice{choice="presence",label="静かにそばにいる",detail="静かな交流の働きかけを強化します。",can_select=true},new PythonEventChoice{choice="gaze",label="目を合わせる",detail="働きかけを強化します。",can_select=true},new PythonEventChoice{choice="rest",label="一緒に休む",detail="働きかけを強化します。",can_select=true}}}};state.required_action="猫の成長を選んでください。";apply();var growth=preview.GetComponent<PythonGrowthPanel>();
            if(growth.choices[0].interactable || !growth.choices[3].gameObject.activeSelf || !growth.choiceDetails[0].text.Contains("習得しています"))throw new Exception("Four growth choices missing");Canvas.ForceUpdateCanvases();Capture(preview,"Logs/EventGrowthLayout.png");
            growth.Disconnect();if(growth.choices[1].interactable || !growth.notice.text.Contains("未接続"))throw new Exception("Event offline guard missing");apply();
            growth.choiceDetails[1].text=new string('猫',4000);Canvas.ForceUpdateCanvases();var choiceScroll=growth.choiceDetails[1].GetComponentInParent<ScrollRect>();if(choiceScroll.content.rect.height<=choiceScroll.viewport.rect.height)throw new Exception("Long choice cannot scroll");
            state.growth_choices=null;state.required_action="";preview.GetComponent<PythonServiceScreen>().ShowCats();apply();if(!preview.GetComponent<PythonServiceScreen>().ShowingSeats)throw new Exception("Resolved event did not return to service");
            state.can_set_shifts=true;state.required_action="保護猫の受け入れに回答してください。";state.intake_request=new PythonIntakeState{status="waiting",cat_id="new-cat",name="ハル",personality="穏やか",features="人懐っこい",housing="空きなし",day=1,cost=100,can_accept=false,can_decline=true,accept_reason="飼育スペースが足りません。"};apply();var intake=preview.GetComponent<PythonIntakePanel>();
            if(intake.accept.interactable || !intake.decline.interactable || !intake.choiceDetails[0].text.Contains("飼育スペース"))throw new Exception("Intake blocked reason missing");
            if(intake.picture==null || !intake.picture.enabled || intake.picture.texture!=ContentPreview.CatPlaceholderTexture() || intake.picture.texture==null)throw new Exception("Intake silhouette missing");
            Canvas.ForceUpdateCanvases();Capture(preview,"Logs/EventIntakeLayout.png");
            state.intake_request.status="declined";state.required_action="";apply();if(intake.panel.activeSelf)throw new Exception("Intake remained after response");
            state.tick++;apply();if(!string.IsNullOrEmpty(preview.GetComponent<PythonEventPresentation>().Message))throw new Exception("Old response feedback persisted after progress");
            Debug.Log("CAT_CAFE_EVENT_LAYOUT_DISPLAY_OK: context / choices / reasons / busy / growth / offline / long text / intake / feedback / return to service");
        }
        static void Capture(ContentPreview preview,string path)
        { typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,path}); }
    }
}
