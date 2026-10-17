using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    // Scene-independent view; scenario text and progress have no dependency on this UI.
    public sealed class IntakeConversationPanel : MonoBehaviour
    {
        PythonIntakePanel owner; PythonCafeState state; CafeConversationProgress progress;
        GameObject card; Text heading,body,speaker,hint,acceptEffect,declineEffect; RawImage portrait;
        Button next,accept,decline,backlog,close; ScrollRect scroll;
        string rendered,epoch; float revealed; bool history,hidden; int lastFrame=-1;
        LocalCafeBackend Backend=>CafeBackends.Local as LocalCafeBackend;
        PythonStateClient Client=>GetComponent<PythonStateClient>();
        public bool Showing=>owner!=null && owner.panel.activeSelf && progress!=null && !progress.completed;
        public void Apply(PythonCafeState value,PythonIntakePanel original)
        {
            owner=original;state=value;Backend.BeginIntakeConversation();var restored=Backend.Conversation;if(!ReferenceEquals(progress,restored)){hidden=false;rendered=null;history=false;}progress=restored;
            if(card==null)Build();
            string key=value.instance_id+"/"+progress?.branch_id;
            if(epoch!=key){epoch=key;hidden=false;history=false;rendered=null;}
            bool available=progress!=null && !progress.completed;
            owner.panel.SetActive(available && !hidden && !Client.AutoPlayRunning);
            if(owner.confirmation!=null)owner.confirmation.SetActive(false);
            Refresh();
            if(Showing && (value.intake_request.can_decline || progress.branch_id!="introduction") && !GetComponent<PythonSavePanel>().panel.activeSelf)owner.panel.transform.SetAsLastSibling();
        }
        public void Refresh()
        {
            if(card==null)return;
            if(owner.panel.activeSelf)hidden=false;
            Backend.ConversationOpen=Showing;
            if(!Showing)return;
            if(Client.Automatic)Client.PauseAutomaticForSave();
            var q=state.intake_request;var lines=CafeIntakeConversation.Lines(progress.branch_id,q.name);
            bool choosing=progress.branch_id=="introduction" && progress.node>=lines.Length;
            string key=progress.branch_id+":"+progress.node+":"+history;
            if(rendered!=key)
            {
                rendered=key;revealed=0;scroll.verticalNormalizedPosition=1;
                speaker.text=history?"会話バックログ":choosing?"受け入れの判断":progress.node<lines.Length?(string.IsNullOrEmpty(lines[progress.node].speaker)?"地の文":lines[progress.node].speaker):"会話の終わり";
            }
            heading.text="保護猫の受け入れ依頼 — "+q.name;
            float height=history?384:164;scroll.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height);scroll.viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height-12);scroll.verticalScrollbar.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,height-12);
            string full=history?History(lines):choosing?"迎えるか、見送るかを選んでください。回答は一度だけ確定します。\n\n他の確認待ちがある場合は「閉じる」から先に対応できます。":progress.node<lines.Length?lines[progress.node].text:"営業準備へ戻りましょう。営業・自動進行は手動で再開してください。";
            body.text=history || choosing?full:full.Substring(0,Math.Min(full.Length,(int)revealed));
            next.gameObject.SetActive(!choosing && !history);next.GetComponentInChildren<Text>().text=revealed<full.Length?"全文表示":progress.node>=lines.Length?"会話を終える":"次へ";
            bool ready=Client.CanManageSaves;
            next.interactable=ready;close.interactable=ready;backlog.interactable=ready;
            accept.gameObject.SetActive(choosing && !history);decline.gameObject.SetActive(choosing && !history);
            acceptEffect.transform.parent.parent.gameObject.SetActive(choosing && !history);declineEffect.transform.parent.parent.gameObject.SetActive(choosing && !history);
            accept.interactable=ready && Client.CanRespondIntake(true);decline.interactable=ready && Client.CanRespondIntake(false);
            acceptEffect.text="初期費用："+q.cost+" / 資金："+state.funds+" → "+(state.funds-q.cost)+"\n飼育枠："+q.housing+"\n健康・体力全回復、休養予定で加入。\n"+(q.can_accept?"選択可能":("選べない理由："+(string.IsNullOrEmpty(q.response_reason)?q.accept_reason:q.response_reason)));
            declineEffect.text="費用・人気の変化なし。\nこの依頼は再発生しません。\n"+(q.can_decline?"選択可能":"選べない理由："+q.response_reason);
            hint.text=!Client.CanSendCommands?"処理中です。完了をお待ちください。":progress.branch_id=="introduction"?"営業・自動進行は停止中。閉じても未回答の依頼と会話の位置を保持します。":"回答は確定済みです。閉じた結果会話はイベント一覧から再開できます。";
            portrait.texture=GetComponent<ContentPreview>().CandidateCatTexture(q.cat_id)??ContentPreview.CatPlaceholderTexture();portrait.enabled=portrait.texture!=null;
            if(portrait.texture!=null)portrait.GetComponent<AspectRatioFitter>().aspectRatio=(float)portrait.texture.width/portrait.texture.height;
        }
        string History(CafeConversationLine[] lines)
        {
            string result="";
            if(progress.branch_id!="introduction")result=string.Join("\n\n",CafeIntakeConversation.Lines("introduction",state.intake_request.name).Select(l=>(string.IsNullOrEmpty(l.speaker)?"地の文":l.speaker)+"\n"+l.text))+"\n\n【"+(progress.branch_id=="accepted"?"迎える":"見送る")+"】\n\n";
            return result+string.Join("\n\n",lines.Take(Math.Min(progress.node+1,lines.Length)).Select(l=>(string.IsNullOrEmpty(l.speaker)?"地の文":l.speaker)+"\n"+l.text));
        }
        void Update(){if(Showing){revealed+=Time.unscaledDeltaTime*40;Refresh();}else if(Backend!=null)Backend.ConversationOpen=false;}
        bool Click(){if(!Client.CanManageSaves || lastFrame==Time.frameCount)return false;lastFrame=Time.frameCount;return true;}
        public void Next()
        {
            if(!Click() || progress==null || history)return;
            var lines=CafeIntakeConversation.Lines(progress.branch_id,state.intake_request.name);
            if(progress.node<lines.Length){if(revealed<lines[progress.node].text.Length)revealed=lines[progress.node].text.Length;else progress.node++;}
            else if(progress.branch_id!="introduction"){CafeIntakeConversation.Finish(progress);Close();return;}
            Refresh();
        }
        public void Answer(bool answer){if(!Click() || progress.branch_id!="introduction" || progress.node<CafeIntakeConversation.Lines("introduction","").Length)return;Client.RespondIntake(answer);Refresh();}
        public void Open(){hidden=false;owner.panel.SetActive(progress!=null && !progress.completed);owner.panel.transform.SetAsLastSibling();Refresh();}
        public void Close(){if(!Client.CanManageSaves)return;hidden=true;owner.panel.SetActive(false);Backend.ConversationOpen=false;GetComponent<ContentPreview>().RefreshPythonControls();}
        void ToggleHistory(){if(!Click())return;history=!history;Refresh();}
        void Build()
        {
            foreach(Transform child in owner.panel.transform)child.gameObject.SetActive(false);
            card=Box(owner.panel.transform,"Intake Conversation",170,8,940,600,new Color(.98f,.97f,.93f));
            heading=Label(card.transform,"Title",24,16,892,40,23);
            portrait=Image(card.transform,24,66,200,192);
            speaker=Label(card.transform,"Speaker",244,66,648,30,20);
            body=Scroll(card.transform,"Dialogue",244,102,672,164,19,out scroll);
            acceptEffect=Scroll(card.transform,"Accept effect",24,280,436,162,17,out var a);
            declineEffect=Scroll(card.transform,"Decline effect",480,280,436,162,17,out var d);
            accept=Button(card.transform,"迎える",24,450,436,()=>Answer(true));decline=Button(card.transform,"見送る",480,450,436,()=>Answer(false));
            next=Button(card.transform,"次へ",680,450,236,Next);
            backlog=Button(card.transform,"バックログ",24,506,164,ToggleHistory);
            Button(card.transform,"保存",204,506,128,()=>GetComponent<PythonSaveBrowser>().OpenSave());
            Button(card.transform,"読込",348,506,128,()=>GetComponent<PythonSaveBrowser>().OpenLoad());
            close=Button(card.transform,"閉じる",736,506,180,Close);
            hint=Label(card.transform,"Notice",24,556,892,40,15);
        }
        Font Font=>owner.title.font;
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static GameObject Box(Transform p,string name,float x,float y,float w,float h,Color c){var g=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));g.transform.SetParent(p,false);Place(g.GetComponent<RectTransform>(),x,y,w,h);g.GetComponent<UnityEngine.UI.Image>().color=c;return g;}
        Text Label(Transform p,string n,float x,float y,float w,float h,int size){var g=new GameObject(n,typeof(RectTransform),typeof(Text));g.transform.SetParent(p,false);Place(g.GetComponent<RectTransform>(),x,y,w,h);var t=g.GetComponent<Text>();t.font=Font;t.fontSize=size;t.color=new Color(.15f,.21f,.18f);t.supportRichText=false;t.raycastTarget=false;return t;}
        Text Scroll(Transform p,string n,float x,float y,float w,float h,int size,out ScrollRect s){var g=Box(p,n,x,y,w,h,new Color(.93f,.94f,.89f));s=g.AddComponent<ScrollRect>();s.horizontal=false;s.movementType=ScrollRect.MovementType.Clamped;var v=Box(g.transform,"Viewport",8,6,w-32,h-12,Color.white);v.AddComponent<Mask>().showMaskGraphic=false;var t=Label(v.transform,"Text",0,0,w-32,h-12,size);t.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;s.viewport=v.GetComponent<RectTransform>();s.content=t.rectTransform;var bar=DefaultControls.CreateScrollbar(new DefaultControls.Resources());bar.transform.SetParent(g.transform,false);Place(bar.GetComponent<RectTransform>(),w-18,6,12,h-12);s.verticalScrollbar=bar.GetComponent<Scrollbar>();s.verticalScrollbar.direction=Scrollbar.Direction.BottomToTop;return t;}
        RawImage Image(Transform p,float x,float y,float w,float h){var g=Box(p,"Cat",x,y,w,h,new Color(.9f,.92f,.86f));var i=new GameObject("Portrait",typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter));i.transform.SetParent(g.transform,false);var r=i.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;i.GetComponent<AspectRatioFitter>().aspectMode=AspectRatioFitter.AspectMode.FitInParent;i.GetComponent<RawImage>().raycastTarget=false;return i.GetComponent<RawImage>();}
        Button Button(Transform p,string text,float x,float y,float w,UnityEngine.Events.UnityAction action){var g=DefaultControls.CreateButton(new DefaultControls.Resources());g.transform.SetParent(p,false);g.name=text;Place(g.GetComponent<RectTransform>(),x,y,w,40);var t=g.GetComponentInChildren<Text>();t.font=Font;t.fontSize=18;t.text=text;g.GetComponent<Button>().onClick.AddListener(action);return g.GetComponent<Button>();}
    }
}
