using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview.Editor
{
    public static class PythonPlayerSetup
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
        [MenuItem("Cat Cafe/開発用/UI再生成/猫との交流",false,7)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var root=preview.transform.Find("Content Canvas/Background");
            var old=root.Find("Player Interaction");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var overlay=Box(root,"Player Interaction",0,174,1280,626,new Color(0,0,0,.55f));
            var card=Box(overlay.transform,"Interaction Card",170,8,940,600,new Color(.98f,.97f,.93f,1));
            var panel=preview.GetComponent<PythonPlayerPanel>() ?? preview.gameObject.AddComponent<PythonPlayerPanel>();panel.panel=overlay;
            panel.title=Label(card.transform,"Title",24,20,892,44,25);
            panel.summary=Label(card.transform,"Summary",24,76,432,136,18);
            Label(card.transform,"Switch Label",24,210,432,28,16).text="動作の切り替え先";
            var drop=DefaultControls.CreateDropdown(new DefaultControls.Resources());drop.name="Interaction Type";drop.transform.SetParent(card.transform,false);
            Place(drop.GetComponent<RectTransform>(),24,242,432,40);panel.targets=drop.GetComponent<Dropdown>();
            foreach(var text in drop.GetComponentsInChildren<Text>(true)){text.font=ContentPreviewLayout.LoadFont();text.fontSize=18;}
            panel.targets.itemText.rectTransform.parent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,36);
            panel.targets.template.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,280);panel.targets.itemText.verticalOverflow=VerticalWrapMode.Overflow;
            panel.targets.itemText.alignment=TextAnchor.MiddleLeft;
            var arrow=drop.transform.Find("Arrow");arrow.GetComponent<Image>().color=Color.clear;
            var arrowLabel=Label(drop.transform,"Dropdown Arrow",402,7,26,26,16);arrowLabel.text="▼";
            panel.actions=new Button[7];
            for(int i=0;i<7;i++)
            {
                panel.actions[i]=Button(card.transform,"Action "+i,"行動",24+(i%2)*222,300+(i/2)*46,210);
                UnityEventTools.AddIntPersistentListener(panel.actions[i].onClick,panel.SelectAction,i);
            }
            var area=Box(card.transform,"History Scroll",480,76,436,448,Color.clear);
            panel.historyScroll=area.AddComponent<ScrollRect>();panel.historyScroll.horizontal=false;panel.historyScroll.movementType=ScrollRect.MovementType.Clamped;
            var viewport=Box(area.transform,"Viewport",0,0,410,448,Color.white);viewport.AddComponent<Mask>().showMaskGraphic=false;
            panel.history=Label(viewport.transform,"History",0,0,410,448,18);
            panel.history.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            panel.historyScroll.viewport=viewport.GetComponent<RectTransform>();panel.historyScroll.content=panel.history.rectTransform;
            var bar=DefaultControls.CreateScrollbar(new DefaultControls.Resources());bar.name="History Scrollbar";bar.transform.SetParent(area.transform,false);
            Place(bar.GetComponent<RectTransform>(),416,0,20,448);panel.historyScroll.verticalScrollbar=bar.GetComponent<Scrollbar>();panel.historyScroll.verticalScrollbar.direction=Scrollbar.Direction.BottomToTop;
            panel.notice=Label(card.transform,"Notice",24,486,432,56,16);
            panel.finish=Button(card.transform,"Finish","このセットを切り上げる",24,550,432);
            UnityEventTools.AddPersistentListener(panel.finish.onClick,panel.Finish);
            var close=Button(card.transform,"Close","閉じる（途中状態を保持）",480,550,436);UnityEventTools.AddPersistentListener(close.onClick,panel.Close);
            var detail=preview.GetComponent<PythonCatDetailsPanel>();var detailCard=detail.panel.transform.Find("Details Card");
            foreach(string name in new[]{"Start Player Interaction","Previous Player Interaction","Interaction Notice"})
            { var previous=detailCard.Find(name);if(previous!=null)UnityEngine.Object.DestroyImmediate(previous.gameObject); }
            detail.scroll.GetComponent<RectTransform>().sizeDelta=new Vector2(812,426);
            detail.scroll.viewport.sizeDelta=new Vector2(786,426);detail.scroll.verticalScrollbar.GetComponent<RectTransform>().sizeDelta=new Vector2(20,426);
            detail.interact=Button(detailCard,"Start Player Interaction","猫と交流する",24,550,320);
            detail.previousInteraction=Button(detailCard,"Previous Player Interaction","前回の交流を見る",364,550,300);
            detail.interactionNotice=Label(detailCard,"Interaction Notice",24,506,812,40,16);
            UnityEventTools.AddPersistentListener(detail.interact.onClick,detail.Interact);
            UnityEventTools.AddPersistentListener(detail.previousInteraction.onClick,detail.PreviousInteraction);
            detail.interact.interactable=detail.previousInteraction.interactable=false;overlay.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("CAT_CAFE_PLAYER_UI_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();
            var client=preview.GetComponent<PythonStateClient>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.can_set_shifts=true;
            state.cats[0].can_player_start=true;state.cats[0].remaining_sets=3;
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});apply();
            var details=preview.GetComponent<PythonCatDetailsPanel>();details.Open(state.cats[0].cat_id);
            if(!details.interact.interactable || !details.details.text.Contains("好感度"))throw new Exception("Player entry missing");
            state.player_interaction=new PythonPlayerInteraction{session_id="player-1-1",cat_id=state.cats[0].cat_id,name="ミケ",active=true,remaining_ticks=9,remaining_sets=2,mode="ねこじゃらし",affinity_after=.5f,stamina=95,can_finish=true,
                actions=new[]{new PythonEventChoice{choice="direct",label="素直に動かす",can_select=true},new PythonEventChoice{choice="switch",label="動作を切り替える",can_select=true}},
                targets=new[]{new PythonEventChoice{choice="voice",label="声をかける"}},history="1：素直に動かす / 好意的に応じる\n体力95 / 好感度 +0.5"};
            apply();var player=preview.GetComponent<PythonPlayerPanel>();
            if(!player.panel.activeSelf || details.panel.activeSelf || !player.finish.interactable || !player.summary.text.Contains("見込み"))throw new Exception("Active interaction display missing");
            Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/PlayerInteraction.png"});
            player.Close();apply();if(player.panel.activeSelf)throw new Exception("Dismissed interaction reopened during polling");player.Open();
            player.Disconnect();if(player.finish.interactable || player.actions[0].interactable)throw new Exception("Player offline guard failed");
            state.player_interaction.active=false;state.player_interaction.can_finish=false;state.player_interaction.reason="自分で切り上げました";apply();
            if(player.finish.interactable || player.actions[0].interactable || !player.summary.text.Contains("確定"))throw new Exception("Completed interaction actions enabled");
            state.player_interaction=null;apply();if(player.panel.activeSelf)throw new Exception("Old interaction remained after loading other game");
            Debug.Log("CAT_CAFE_PLAYER_DISPLAY_OK: entry / live / dismiss / resume / offline / result / other game");
        }
    }
}
