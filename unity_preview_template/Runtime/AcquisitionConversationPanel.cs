using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    // Scene-independent view; scenario text and progress have no dependency on this UI.
    public class AcquisitionConversationPanel : MonoBehaviour
    {
        GameObject panel,confirmation; Font font; bool regular; protected PythonCafeState state; protected CafeConversationProgress progress;
        Transform[] originalChildren; bool[] originalActive; Vector2 originalMin,originalMax,originalPivot,originalOffsetMin,originalOffsetMax;
        public bool OriginalShowing {get;private set;}
        GameObject card,dialogue,choiceGroup,historyWindow; Text heading,body,speaker,hint,acceptEffect,declineEffect,catLabel,historyBody,sceneLabel; RawImage portrait;
        CanvasGroup catGroup; ScrollRect historyScroll; Vector2 layoutSize; bool layoutHistory;
        static readonly Color Paper=new Color(.99f,.97f,.92f),Ink=new Color(.22f,.26f,.20f),Olive=new Color(.36f,.43f,.31f);
        public ScrollRect HistoryScroll=>historyScroll;
        Button third;Text thirdEffect;
        protected virtual bool HasThirdChoice=>false;
        protected virtual string ThirdCaption=>"";
        protected virtual string ThirdEffect=>"";
        protected virtual bool CanThird=>false;
        protected virtual void SendThird(){}
        Button next,accept,decline,backlog,close; ScrollRect scroll;
        string rendered,epoch; string pageText=""; float revealed; bool history,hidden; int lastFrame=-1;
        protected LocalCafeBackend Backend=>CafeBackends.Local as LocalCafeBackend;
        protected PythonStateClient Client=>GetComponent<PythonStateClient>();
        public bool Showing=>!OriginalShowing && panel!=null && panel.activeSelf && progress!=null && !progress.completed;
        protected virtual bool Ready=>Client.CanManageSaves;
        protected virtual bool AutoRunning=>Client.AutoPlayExecuting;
        protected virtual bool ThreeLinePages=>false;
        protected virtual bool SaveControls=>true;
        protected virtual string CloseCaption=>"閉じる";
        protected virtual string EndText=>"通常画面へ戻りましょう。営業・自動進行は手動で再開してください。";
        protected virtual string BusyNotice=>!Client.CanSendCommands?"処理中です。完了をお待ちください。":Notice;
        protected virtual Texture2D PortraitTexture(string id)=>GetComponent<ContentPreview>().CandidateCatTexture(id);
        protected virtual void PauseProgress(){if(Client.Automatic)Client.PauseAutomaticForSave();}
        protected virtual void AfterClose(){GetComponent<ContentPreview>().RefreshPythonControls();}
        protected virtual void RecordCompletion(){Backend.RememberViewedConversations();}
        protected virtual bool CanAnswer(bool answer)=>regular?Client.CanRespondRegular(answer?"accept":"decline"):Client.CanRespondIntake(answer);
        protected virtual CafeConversationLine[] Lines(string branch)=>regular?CafeRegularConversation.Lines(branch,state.regular_introduction.name,state.regular_introduction.customer_name):CafeIntakeConversation.Lines(branch,state.intake_request.name);
        protected virtual PythonIntakeState Request
        {
            get
            {
                if(!regular)return state.intake_request;
                var q=state.regular_introduction;
                return new PythonIntakeState{cat_id=q.content_cat_id,name=q.name,personality=q.personality,cost=q.cost,housing=q.housing,can_accept=q.can_accept,can_decline=q.can_decline,response_reason=q.response_reason,accept_reason=q.accept_reason};
            }
        }
        protected virtual void SetOpen(bool open){if(regular)Backend.RegularConversationOpen=open;else Backend.ConversationOpen=open;}
        public void Apply(PythonCafeState value,PythonIntakePanel original){panel=original.panel;confirmation=original.confirmation;font=original.title.font;ApplyState(value);}
        public void ApplyRegular(PythonCafeState value,PythonRegularIntroductionPanel original){regular=true;panel=original.panel;confirmation=original.confirmation;font=original.title.font;ApplyState(value);}
        protected virtual CafeConversationProgress ResolveProgress(){if(regular)Backend.BeginRegularConversation();else Backend.BeginIntakeConversation();return regular?Backend.RegularConversation:Backend.Conversation;}
        protected void Bind(PythonCafeState value,GameObject host,Font textFont){panel=host;font=textFont;ApplyState(value);}
        protected virtual bool CanRaise=>true;
        protected virtual bool Available=>progress!=null && !progress.completed && (!Backend.EquipmentResultPending || this is EquipmentConversationPanel || this is ReplayConversationPanel);
        protected virtual bool Choosing=>progress.branch_id=="introduction" && progress.node>=Lines(progress.branch_id).Length;
        protected virtual string Title=>regular?"常連からの猫紹介":"保護猫の受け入れ依頼";
        protected virtual string SceneLabel=>state.day+"日目 · 営業準備";
        protected virtual string ChoicePrompt=>"この子を、ここへ迎えましょうか。\n迎えるか、見送るかを選んでください。";
        protected virtual string AcceptCaption=>"迎える";
        protected virtual string DeclineCaption=>"見送る";
        protected virtual string AcceptEffect {get{var q=Request;return (q.can_accept?"選択可能":"選べない理由："+(string.IsNullOrEmpty(q.response_reason)?q.accept_reason:q.response_reason))+"\n初期費用："+q.cost+" / 資金："+state.funds+" → "+(state.funds-q.cost)+"\n飼育枠："+q.housing+"\n健康・体力全回復、休養予定で加入。";}}
        protected virtual string DeclineEffect {get{var q=Request;return (q.can_decline?"選択可能":"選べない理由："+q.response_reason)+"\n費用・人気"+(regular?"・常連度":"")+"の変化なし。\nこの"+(regular?"紹介":"依頼")+"は再発生しません。";}}
        protected virtual string Notice=>progress.branch_id=="introduction"?"営業・自動進行は停止中。閉じても未回答の相談と会話の位置を保持します。":"回答は確定済みです。閉じた結果会話はイベント一覧から再開できます。";
        protected virtual string HistoryPrefix=>progress.branch_id=="introduction"?"":string.Join("\n\n",Lines("introduction").Select(l=>(string.IsNullOrEmpty(l.speaker)?"地の文":l.speaker)+"\n"+l.text))+"\n\n【"+(progress.branch_id=="accepted"?"迎える":"見送る")+"】\n\n";
        protected virtual void Finish(){if(regular)CafeRegularConversation.Finish(progress);else CafeIntakeConversation.Finish(progress);}
        protected virtual void SendAnswer(bool answer){if(regular)Client.RespondRegular(answer?"accept":"decline");else Client.RespondIntake(answer);}
        void ApplyState(PythonCafeState value)
        {
            state=value;var restored=ResolveProgress();if(!ReferenceEquals(progress,restored)){hidden=false;rendered=null;history=false;}progress=restored;
            if(card==null && progress==null){panel.SetActive(false);return;}
            if(card==null)Build();
            if(progress!=null && !progress.completed){OriginalShowing=false;foreach(var child in originalChildren)child.gameObject.SetActive(false);card.SetActive(true);Stretch(panel.GetComponent<RectTransform>());}
            string key=value.instance_id+"/"+progress?.branch_id;
            if(epoch!=key){epoch=key;hidden=false;history=false;rendered=null;}
            bool available=Available;
            if(available && GetComponents<AcquisitionConversationPanel>().Any(other=>other!=this && other.Showing))hidden=true;
            panel.SetActive(available && !hidden && !AutoRunning);
            if(confirmation!=null)confirmation.SetActive(false);
            Refresh();
            if(Showing && !GameOptionsPanel.IsOpen && CanRaise && (Request.can_decline || progress.branch_id!="introduction") && GetComponent<PythonSavePanel>()?.panel.activeSelf!=true)panel.transform.SetAsLastSibling();
        }
        public void Refresh()
        {
            if(card==null)return;
            if(panel.activeSelf)hidden=false;
            SetOpen(Showing);
            if(!Showing)return;
            PauseProgress();
            var q=Request;var lines=Lines(progress.branch_id);
            bool choosing=Choosing;
            Layout();
            string full=choosing?ChoicePrompt:progress.node<lines.Length?lines[progress.node].text:EndText;
            pageText=ThreeLinePages && !choosing?Page(full):full;
            string key=progress.branch_id+":"+progress.node+":"+progress.page_offset+":"+history+":"+pageText;
            if(rendered!=key)
            {
                rendered=key;revealed=0;scroll.verticalNormalizedPosition=historyScroll.verticalNormalizedPosition=1;
                speaker.text=choosing?"あなた":progress.node<lines.Length?(string.IsNullOrEmpty(lines[progress.node].speaker)?"地の文":lines[progress.node].speaker):"会話の終わり";
            }
            heading.text=Title;sceneLabel.text=SceneLabel;catLabel.text=q.name+"\n"+q.personality;
            Layout();historyWindow.SetActive(history);dialogue.SetActive(!history);choiceGroup.SetActive(choosing && !history);catGroup.alpha=choosing?.20f:1f;
            full=pageText;
            if(CafeOptions.Current.instant)revealed=full.Length;
            body.text=choosing?full:full.Substring(0,Math.Min(full.Length,(int)revealed));
            if(ThreeLinePages)body.text=body.text.TrimEnd('\r','\n');
            if(history)historyBody.text=History(lines);
            next.gameObject.SetActive(!choosing && !history);next.GetComponentInChildren<Text>().text=revealed<full.Length?"全文表示":progress.node>=lines.Length?"会話を終える":"次へ";
            bool ready=Ready;
            next.interactable=ready;close.interactable=ready;backlog.interactable=ready;
            accept.gameObject.SetActive(choosing && !history);decline.gameObject.SetActive(choosing && !history);
            accept.interactable=ready && CanAnswer(true);decline.interactable=ready && CanAnswer(false);
            accept.GetComponentInChildren<Text>().text=AcceptCaption;decline.GetComponentInChildren<Text>().text=DeclineCaption;
            if(third!=null){third.gameObject.SetActive(choosing && !history);third.interactable=ready && CanThird;third.GetComponentInChildren<Text>().text=ThirdCaption;thirdEffect.text=ThirdEffect;}
            acceptEffect.text=AcceptEffect;declineEffect.text=DeclineEffect;
            hint.text=BusyNotice;hint.gameObject.SetActive(!ThreeLinePages);
            portrait.texture=PortraitTexture(q.cat_id)??ContentPreview.CatPlaceholderTexture();portrait.enabled=portrait.texture!=null;
            if(portrait.texture!=null)portrait.GetComponent<AspectRatioFitter>().aspectRatio=(float)portrait.texture.width/portrait.texture.height;
        }
        string Page(string full)
        {
            // Use the same font, width, wrapping and spacing as the rendered text.
            int start=Mathf.Clamp(progress.page_offset,0,Math.Max(0,full.Length-1));
            progress.page_offset=start;string remaining=full.Substring(start);
            var settings=body.GetGenerationSettings(new Vector2(body.rectTransform.rect.width,10000));
            settings.verticalOverflow=VerticalWrapMode.Overflow;
            var generator=new TextGenerator();generator.Populate(remaining,settings);
            int count=generator.lines.Count>3?generator.lines[3].startCharIdx:remaining.Length;
            if(count<=0)count=remaining.Length;
            return remaining.Substring(0,count);
        }
        string History(CafeConversationLine[] lines)
        {
            string result=HistoryPrefix;
            return result+string.Join("\n\n",lines.Take(Math.Min(progress.node+1,lines.Length)).Select((l,i)=>(string.IsNullOrEmpty(l.speaker)?"地の文":l.speaker)+"\n"+(ThreeLinePages && i==progress.node?l.text.Substring(0,Math.Min(l.text.Length,progress.page_offset+pageText.Length)):l.text)));
        }
        protected virtual void Update(){if(Showing){if(!GameOptionsPanel.IsOpen)revealed=CafeOptions.Reveal(revealed,Time.unscaledDeltaTime,1000000);Refresh();}else if(Backend!=null)SetOpen(false);}
        bool Click(){if(GameOptionsPanel.IsOpen || !Ready || lastFrame==Time.frameCount)return false;lastFrame=Time.frameCount;return true;}
        public void Next()
        {
            if(!Showing || !Click() || progress==null || history)return;
            var lines=Lines(progress.branch_id);
            if(progress.node<lines.Length){if(revealed<pageText.Length)revealed=pageText.Length;else if(ThreeLinePages && progress.page_offset+pageText.Length<lines[progress.node].text.Length)progress.page_offset+=pageText.Length;else {progress.node++;progress.page_offset=0;}}
            else if(progress.branch_id!="introduction"){Finish();RecordCompletion();Close();return;}
            Refresh();
        }
        public void Answer(bool answer){if(!Showing || !Click() || progress==null || progress.branch_id!="introduction" || progress.node<Lines("introduction").Length)return;SendAnswer(answer);Refresh();}
        public virtual void Open(){foreach(var other in GetComponents<AcquisitionConversationPanel>())if(other!=this && other.Showing)other.Close();hidden=false;OriginalShowing=false;if(card!=null){card.SetActive(true);foreach(var child in originalChildren)child.gameObject.SetActive(false);Stretch(panel.GetComponent<RectTransform>());}panel.SetActive(Available);panel.transform.SetAsLastSibling();Refresh();}
        public virtual void Close(){if(!Ready)return;hidden=true;panel.SetActive(false);SetOpen(false);AfterClose();}
        void ToggleHistory(){if(!Click())return;history=!history;Refresh();}
        public void ShowOriginal()
        {
            OriginalShowing=true;
            if(card!=null)
            {
                card.SetActive(false);for(int i=0;i<originalChildren.Length;i++)originalChildren[i].gameObject.SetActive(originalActive[i]);
                var rect=panel.GetComponent<RectTransform>();rect.anchorMin=originalMin;rect.anchorMax=originalMax;rect.pivot=originalPivot;rect.offsetMin=originalOffsetMin;rect.offsetMax=originalOffsetMax;
            }
            panel.SetActive(true);panel.transform.SetAsLastSibling();SetOpen(false);
        }
        void Build()
        {
            originalChildren=panel.transform.Cast<Transform>().ToArray();originalActive=originalChildren.Select(t=>t.gameObject.activeSelf).ToArray();
            var original=panel.GetComponent<RectTransform>();originalMin=original.anchorMin;originalMax=original.anchorMax;originalPivot=original.pivot;originalOffsetMin=original.offsetMin;originalOffsetMax=original.offsetMax;
            foreach(Transform child in panel.transform)child.gameObject.SetActive(false);
            // The original event panel was below the business header. Stretch the same panel
            // to the whole canvas so existing event ordering and save overlays still own it.
            Stretch(panel.GetComponent<RectTransform>());panel.GetComponent<UnityEngine.UI.Image>().color=Paper;
            card=Box(panel.transform,regular?"Regular Conversation":"Intake Conversation",0,0,1280,800,Paper);Stretch(card.GetComponent<RectTransform>());
            var room=new GameObject("Cafe Interior",typeof(RectTransform),typeof(CanvasRenderer),typeof(ConversationRoomGraphic));room.transform.SetParent(card.transform,false);Stretch(room.GetComponent<RectTransform>());room.GetComponent<ConversationRoomGraphic>().raycastTarget=false;
            heading=Label(card.transform,"Title",48,30,900,42,25);sceneLabel=Label(card.transform,"Scene",48,78,800,30,17);
            var stage=new GameObject("Cat Stage",typeof(RectTransform),typeof(CanvasGroup));stage.transform.SetParent(card.transform,false);Stretch(stage.GetComponent<RectTransform>());catGroup=stage.GetComponent<CanvasGroup>();catGroup.blocksRaycasts=false;
            portrait=Image(stage.transform,0,0,322,310);var frame=portrait.transform.parent.parent.GetComponent<RectTransform>();frame.anchorMin=frame.anchorMax=new Vector2(.5f,.60f);frame.pivot=new Vector2(.5f,.5f);frame.anchoredPosition=Vector2.zero;
            var tag=Box(stage.transform,"Cat Label",0,0,344,96,new Color(Paper.r,Paper.g,Paper.b,.94f),12);var tagRect=tag.GetComponent<RectTransform>();tagRect.anchorMin=tagRect.anchorMax=new Vector2(.5f,.60f);tagRect.anchoredPosition=new Vector2(184,36);catLabel=Label(tag.transform,"Cat Name",16,12,312,80,21);catLabel.lineSpacing=1.4f;
            dialogue=Box(card.transform,"Dialogue Window",48,0,1184,254,Paper,16);var dock=dialogue.GetComponent<RectTransform>();dock.anchorMin=new Vector2(0,0);dock.anchorMax=new Vector2(1,0);dock.pivot=new Vector2(.5f,0);dock.offsetMin=new Vector2(48,32);dock.offsetMax=new Vector2(-48,286);
            var plate=Box(dialogue.transform,"Name Plate",24,-20,170,48,Olive,9);speaker=Label(plate.transform,"Speaker",12,8,146,32,20);speaker.color=Paper;speaker.alignment=TextAnchor.MiddleCenter;
            body=Scroll(dialogue.transform,"Dialogue",32,46,1120,112,23,out scroll);body.lineSpacing=1.2f;var surface=scroll.gameObject.AddComponent<ConversationAdvanceSurface>();surface.conversation=this;
            hint=Label(dialogue.transform,"Notice",32,164,1120,25,14);
            backlog=Button(dialogue.transform,"バックログ",24,202,120,ToggleHistory);
            Button(dialogue.transform,"保存",156,202,96,()=>GetComponent<PythonSaveBrowser>().OpenSave()).gameObject.SetActive(SaveControls);
            Button(dialogue.transform,"読込",264,202,96,()=>GetComponent<PythonSaveBrowser>().OpenLoad()).gameObject.SetActive(SaveControls);
            close=Button(dialogue.transform,CloseCaption,388,202,144,Close);
            Button(dialogue.transform,"設定",548,202,120,()=>GetComponent<GameOptionsPanel>()?.Open());
            next=Button(dialogue.transform,"次へ",948,202,200,Next);next.GetComponent<ConversationShape>().color=Olive;next.GetComponentInChildren<Text>().color=Paper;
            choiceGroup=new GameObject("Choices",typeof(RectTransform));choiceGroup.transform.SetParent(card.transform,false);var choices=choiceGroup.GetComponent<RectTransform>();choices.anchorMin=choices.anchorMax=new Vector2(.5f,.61f);choices.pivot=new Vector2(.5f,.5f);choices.sizeDelta=new Vector2(760,354);choices.anchoredPosition=Vector2.zero;
            var first=Box(choiceGroup.transform,"Accept choice",0,0,760,170,Paper,12);var second=Box(choiceGroup.transform,"Decline choice",0,184,760,170,Paper,12);
            accept=Button(first.transform,"迎える",16,8,728,()=>Answer(true));decline=Button(second.transform,"見送る",16,8,728,()=>Answer(false));
            acceptEffect=Scroll(first.transform,"Accept effect",20,52,720,110,17,out var a);declineEffect=Scroll(second.transform,"Decline effect",20,52,720,110,17,out var d);
            if(HasThirdChoice){choices.sizeDelta=new Vector2(760,354);Place(first.GetComponent<RectTransform>(),0,0,760,110);Place(second.GetComponent<RectTransform>(),0,122,760,110);SetScroll(a,720,50);SetScroll(d,720,50);var extra=Box(choiceGroup.transform,"Third choice",0,244,760,110,Paper,12);third=Button(extra.transform,ThirdCaption,16,8,728,()=>{if(Click() && CanThird)SendThird();});thirdEffect=Scroll(extra.transform,"Third effect",20,52,720,50,17,out var t);}
            historyWindow=Box(card.transform,"History Window",96,110,1088,620,Paper,16);Label(historyWindow.transform,"History Title",32,22,900,42,24).text="会話の履歴";
            historyBody=Scroll(historyWindow.transform,"History",32,82,1024,454,21,out historyScroll);historyBody.lineSpacing=1.3f;
            Button(historyWindow.transform,"会話へ戻る",32,558,210,ToggleHistory);historyWindow.SetActive(false);Layout();
        }
        void Layout()
        {
            var size=card.GetComponent<RectTransform>().rect.size;if(size==layoutSize && layoutHistory==history)return;layoutSize=size;layoutHistory=history;
            float width=size.x-96;var dock=dialogue.GetComponent<RectTransform>();dock.offsetMin=new Vector2(48,32);dock.offsetMax=new Vector2(-48,286);
            SetScroll(scroll,width-64,ThreeLinePages?144:112);
            if(ThreeLinePages){scroll.vertical=false;scroll.verticalScrollbar.gameObject.SetActive(false);}hint.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,width-64);next.GetComponent<RectTransform>().anchoredPosition=new Vector2(width-224,-202);
            var box=historyWindow.GetComponent<RectTransform>();Place(box,96,110,size.x-192,size.y-158);SetScroll(historyScroll,size.x-256,size.y-334);
            historyWindow.transform.Find("会話へ戻る").GetComponent<RectTransform>().anchoredPosition=new Vector2(32,-(size.y-216));
        }
        static void SetScroll(ScrollRect s,float w,float h)
        {
            s.GetComponent<RectTransform>().sizeDelta=new Vector2(w,h);s.viewport.sizeDelta=new Vector2(w-32,h-12);s.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,w-32);var bar=s.verticalScrollbar.GetComponent<RectTransform>();bar.anchoredPosition=new Vector2(w-18,-6);bar.sizeDelta=new Vector2(12,h-12);
        }
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,.5f);r.offsetMin=r.offsetMax=Vector2.zero;}
        Font Font=>font;
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static GameObject Box(Transform p,string name,float x,float y,float w,float h,Color c,float radius=0){var g=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(ConversationShape));g.transform.SetParent(p,false);Place(g.GetComponent<RectTransform>(),x,y,w,h);var graphic=g.GetComponent<ConversationShape>();graphic.color=c;graphic.radius=radius;return g;}
        Text Label(Transform p,string n,float x,float y,float w,float h,int size){var g=new GameObject(n,typeof(RectTransform),typeof(Text));g.transform.SetParent(p,false);Place(g.GetComponent<RectTransform>(),x,y,w,h);var t=g.GetComponent<Text>();t.font=Font;t.fontSize=size;t.color=Ink;t.supportRichText=false;t.raycastTarget=false;return t;}
        Text Scroll(Transform p,string n,float x,float y,float w,float h,int size,out ScrollRect s){var g=Box(p,n,x,y,w,h,Paper);s=g.AddComponent<ScrollRect>();s.horizontal=false;s.scrollSensitivity=28;s.movementType=ScrollRect.MovementType.Clamped;var v=Box(g.transform,"Viewport",8,6,w-32,h-12,Paper);v.AddComponent<Mask>().showMaskGraphic=false;var t=Label(v.transform,"Text",0,0,w-32,h-12,size);t.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;s.viewport=v.GetComponent<RectTransform>();s.content=t.rectTransform;var bar=DefaultControls.CreateScrollbar(new DefaultControls.Resources());bar.transform.SetParent(g.transform,false);Place(bar.GetComponent<RectTransform>(),w-18,6,12,h-12);s.verticalScrollbar=bar.GetComponent<Scrollbar>();s.verticalScrollbar.direction=Scrollbar.Direction.BottomToTop;s.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;bar.GetComponent<UnityEngine.UI.Image>().color=new Color(.91f,.90f,.84f);s.verticalScrollbar.targetGraphic.color=new Color(.58f,.64f,.51f);return t;}
        RawImage Image(Transform p,float x,float y,float w,float h){var g=Box(p,"Cat",x,y,w,h,Paper,24);var inside=Box(g.transform,"Portrait Mask",8,8,w-16,h-16,Paper,18);inside.AddComponent<Mask>().showMaskGraphic=false;var i=new GameObject("Portrait",typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter));i.transform.SetParent(inside.transform,false);Stretch(i.GetComponent<RectTransform>());i.GetComponent<AspectRatioFitter>().aspectMode=AspectRatioFitter.AspectMode.FitInParent;i.GetComponent<RawImage>().raycastTarget=false;return i.GetComponent<RawImage>();}
        Button Button(Transform p,string text,float x,float y,float w,UnityEngine.Events.UnityAction action){var g=Box(p,text,x,y,w,40,new Color(.94f,.93f,.87f),8);var button=g.AddComponent<Button>();button.targetGraphic=g.GetComponent<ConversationShape>();var t=Label(g.transform,"Caption",8,2,w-16,36,18);t.alignment=TextAnchor.MiddleCenter;t.text=text;button.onClick.AddListener(action);return button;}
    }
}
