using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CatCafePreview.Standalone;

namespace CatCafePreview
{
    // Built at runtime so existing scenes also gain the read-only record screen.
    public sealed class InteractionStatisticsPanel : MonoBehaviour
    {
        GameObject panel;Text body;ScrollRect scroll;Dropdown cats,people,channels;Button previous,next;
        string[] catIds,personIds;string epoch;int revision,page;bool binding;Font font;
        LocalCafeBackend Backend=>CafeBackends.Standalone?CafeBackends.Local as LocalCafeBackend:null;
        public static void EnsureEntry(MonoBehaviour owner,Transform card,Text heading,Button style,Func<string> cat,Func<string> person)
        {
            if(!CafeBackends.Standalone || card.Find("Interaction Statistics Entry")!=null)return;
            var button=Instantiate(style,card);button.name="Interaction Statistics Entry";button.gameObject.SetActive(true);button.onClick=new Button.ButtonClickedEvent();button.interactable=true;
            bool catEntry=cat!=null;Place(button.GetComponent<RectTransform>(),catEntry?350:680,20,catEntry?230:236,40);
            if(!catEntry)heading.fontSize=20;
            heading.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,catEntry?310:640);
            button.GetComponentInChildren<Text>().text="交流の累計記録";button.GetComponentInChildren<Text>().fontSize=17;
            button.onClick.AddListener(()=>{if(!owner.GetComponent<PythonStateClient>().CanManageSaves)return;var view=owner.GetComponent<InteractionStatisticsPanel>()??owner.gameObject.AddComponent<InteractionStatisticsPanel>();view.Open(owner,heading.font,cat?.Invoke(),person?.Invoke());});
        }
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        Text Label(Transform parent,string name,float x,float y,float w,float h,int size,string value)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);var t=go.GetComponent<Text>();Place(t.rectTransform,x,y,w,h);t.font=font;t.fontSize=size;t.color=new Color(.14f,.2f,.16f);t.supportRichText=false;t.raycastTarget=false;t.text=value;return t;
        }
        Button MakeButton(Transform parent,string caption,float x,float y,float width,UnityEngine.Events.UnityAction action)
        {
            var go=DefaultControls.CreateButton(new DefaultControls.Resources());go.transform.SetParent(parent,false);Place(go.GetComponent<RectTransform>(),x,y,width,36);var t=go.GetComponentInChildren<Text>();t.font=font;t.fontSize=17;t.text=caption;var b=go.GetComponent<Button>();b.onClick.AddListener(action);return b;
        }
        Dropdown MakeDropdown(Transform parent,float x,float width)
        {
            var go=DefaultControls.CreateDropdown(new DefaultControls.Resources());go.transform.SetParent(parent,false);Place(go.GetComponent<RectTransform>(),x,72,width,36);
            // These controls have no imported UI sprites. Use solid graphics explicitly,
            // including the arrow and selection marker, instead of empty sprite slots.
            var background=go.GetComponent<Image>();background.type=Image.Type.Simple;background.color=Color.white;
            var border=go.AddComponent<Outline>();border.effectColor=new Color(.35f,.43f,.36f);border.effectDistance=new Vector2(1,-1);
            foreach(var t in go.GetComponentsInChildren<Text>(true)){t.font=font;t.fontSize=16;t.color=new Color(.14f,.2f,.16f);t.supportRichText=false;}
            var arrow=go.transform.Find("Arrow");arrow.GetComponent<Image>().enabled=false;var arrowText=Label(arrow,"Arrow Caption",0,0,20,20,14,"▼");arrowText.alignment=TextAnchor.MiddleCenter;arrowText.verticalOverflow=VerticalWrapMode.Overflow;arrowText.horizontalOverflow=HorizontalWrapMode.Overflow;
            var d=go.GetComponent<Dropdown>();d.template.GetComponent<Image>().type=Image.Type.Simple;d.template.GetComponent<Image>().color=Color.white;
            d.template.sizeDelta=new Vector2(0,220);
            var item=d.template.GetComponentInChildren<Toggle>(true);item.GetComponent<RectTransform>().sizeDelta=new Vector2(0,34);item.transform.parent.GetComponent<RectTransform>().sizeDelta=new Vector2(0,42);
            var marker=item.graphic as Image;if(marker!=null){marker.type=Image.Type.Simple;marker.color=new Color(.2f,.48f,.26f);marker.rectTransform.sizeDelta=new Vector2(10,10);}
            var colors=d.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.88f,.94f,.87f);colors.selectedColor=colors.highlightedColor;d.colors=colors;
            d.onValueChanged.AddListener(_=>{if(!binding){page=0;Render();}});return d;
        }
        void Build(MonoBehaviour owner)
        {
            var source=owner is PythonCatDetailsPanel?(owner as PythonCatDetailsPanel).panel:(owner as PythonCustomerDirectoryPanel).panel;
            panel=new GameObject("Interaction Statistics",typeof(RectTransform),typeof(Image),typeof(Canvas),typeof(GraphicRaycaster));panel.transform.SetParent(source.transform.parent,false);
            // A fixed modal layer prevents other panels from competing every frame.
            var modalCanvas=panel.GetComponent<Canvas>();modalCanvas.overrideSorting=true;modalCanvas.sortingOrder=100;
            var rect=panel.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;panel.GetComponent<Image>().color=new Color(0,0,0,.7f);
            var card=new GameObject("Card",typeof(RectTransform),typeof(Image));card.transform.SetParent(panel.transform,false);var cr=card.GetComponent<RectTransform>();cr.anchorMin=cr.anchorMax=cr.pivot=new Vector2(.5f,.5f);cr.sizeDelta=new Vector2(1000,640);card.GetComponent<Image>().color=new Color(.95f,.96f,.91f);
            Label(card.transform,"Title",24,18,740,42,24,"猫・人物の交流記録（このセーブ内の累計）");MakeButton(card.transform,"閉じる",830,20,146,()=>panel.SetActive(false));
            cats=MakeDropdown(card.transform,24,300);people=MakeDropdown(card.transform,340,330);channels=MakeDropdown(card.transform,686,290);
            var area=new GameObject("Records Scroll",typeof(RectTransform),typeof(Image),typeof(ScrollRect));area.transform.SetParent(card.transform,false);Place(area.GetComponent<RectTransform>(),24,122,952,450);area.GetComponent<Image>().color=Color.white;
            var viewport=new GameObject("Viewport",typeof(RectTransform),typeof(RectMask2D));viewport.transform.SetParent(area.transform,false);Place(viewport.GetComponent<RectTransform>(),12,8,910,434);
            body=Label(viewport.transform,"Records",0,0,910,434,18,"");body.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            scroll=area.GetComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.viewport=viewport.GetComponent<RectTransform>();scroll.content=body.rectTransform;scroll.scrollSensitivity=30;
            var bar=DefaultControls.CreateScrollbar(new DefaultControls.Resources());bar.transform.SetParent(area.transform,false);Place(bar.GetComponent<RectTransform>(),930,8,14,434);scroll.verticalScrollbar=bar.GetComponent<Scrollbar>();scroll.verticalScrollbar.direction=Scrollbar.Direction.BottomToTop;
            previous=MakeButton(card.transform,"詳細の前ページ",24,588,220,()=>{page--;Render();});next=MakeButton(card.transform,"詳細の次ページ",260,588,220,()=>{page++;Render();});
            Label(card.transform,"Hint",500,588,476,36,15,"猫と人物を選ぶと組み合わせ別に表示します。");
        }
        string CatName(string id){try{return Backend.Simulation.Definition(id).name;}catch{return id;}}
        string PersonName(string id)=>id=="player"?"プレイヤー":Backend.Details.CustomerName(id);
        public void Open(MonoBehaviour owner,Font labelFont,string cat,string person)
        {
            if(Backend==null)return;font=labelFont;if(panel==null)Build(owner);var s=Backend.Simulation.State;epoch=s.instance_id;revision=s.revision;page=0;binding=true;
            catIds=new[]{(string)null}.Concat(s.cats.Select(c=>c.id)).ToArray();
            var directory=GetComponent<PythonCustomerDirectoryPanel>();
            personIds=new[]{(string)null,"player"}.Concat(directory?.KnownCustomerIds??new string[0]).Concat(Backend.InteractionStatistics.Data.rows.Select(r=>r.person_id)).Distinct().ToArray();
            cats.ClearOptions();cats.AddOptions(catIds.Select(id=>id==null?"全猫":CatName(id)).ToList());people.ClearOptions();people.AddOptions(personIds.Select(id=>id==null?"全人物":PersonName(id)).ToList());channels.ClearOptions();channels.AddOptions(new System.Collections.Generic.List<string>{"接客＋プレイヤー交流","接客のみ","プレイヤー交流のみ"});
            cats.value=Math.Max(0,Array.IndexOf(catIds,cat));people.value=Math.Max(0,Array.IndexOf(personIds,person));channels.value=0;cats.RefreshShownValue();people.RefreshShownValue();channels.RefreshShownValue();binding=false;panel.SetActive(true);panel.transform.SetAsLastSibling();Render();
        }
        void Render()
        {
            if(Backend==null || catIds==null)return;string cat=catIds[cats.value],person=personIds[people.value],channel=channels.value==0?null:channels.value==1?"service":"player";int total=Backend.InteractionStatistics.Select(cat,person,channel).Count();page=Math.Max(0,Math.Min(page,Math.Max(0,(total-1)/30)));
            body.text=CafeInteractionStatisticsPresentation.Describe(Backend.InteractionStatistics,CatName,PersonName,cat,person,channel,page);previous.interactable=page>0;next.interactable=(page+1)*30<total;Canvas.ForceUpdateCanvases();scroll.StopMovement();scroll.verticalNormalizedPosition=1;
        }
        void Update(){if(panel==null || !panel.activeSelf)return;var s=Backend?.Simulation.State;if(s==null || s.instance_id!=epoch){panel.SetActive(false);return;}if(s.revision!=revision){revision=s.revision;Render();}}
        void OnDestroy(){if(panel!=null)Destroy(panel);}
    }
}
