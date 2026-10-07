using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonPlayerLayout
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
        [MenuItem("Cat Cafe/開発用/UI再生成/猫との交流の配置",false,16)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var panel=preview.GetComponent<PythonPlayerPanel>();
            var card=panel.panel.transform.Find("Interaction Card");
            Place(card.GetComponent<RectTransform>(),170,8,940,600);
            Place(panel.title.rectTransform,24,18,892,44);
            Place(panel.summary.rectTransform,24,72,432,160);panel.summary.fontSize=18;
            Place(card.Find("Switch Label").GetComponent<RectTransform>(),24,238,432,26);
            panel.choiceLabel=card.Find("Switch Label").GetComponent<Text>();panel.choiceLabel.text="交流を変える：選択後に「切り替える」";
            Place(panel.targets.GetComponent<RectTransform>(),24,270,432,40);
            panel.targets.onValueChanged=new Dropdown.DropdownEvent();
            UnityEventTools.AddPersistentListener(panel.targets.onValueChanged,panel.TargetChanged);
            panel.choiceLabel.text="現在の動作／切り替え先（選択後に切り替える）";
            panel.targets.template.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 310);
            for(int i=0;i<panel.actions.Length;i++)Place(panel.actions[i].GetComponent<RectTransform>(),24+(i%2)*222,324+(i/2)*44,210,38);
            Place(panel.notice.rectTransform,24,506,432,40);panel.notice.fontSize=14;
            Place(panel.finish.GetComponent<RectTransform>(),24,550,432,40);
            var old=card.Find("History Scroll");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var heading=card.Find("History Heading");if(heading!=null)UnityEngine.Object.DestroyImmediate(heading.gameObject);
            Label(card,"History Heading",480,292,436,28,18).text="行動と猫の反応";
            panel.history=ScrollText(card,"History Scroll",480,330,436,208,17,out panel.historyScroll);
            var oldPicture=card.Find("Action Picture");if(oldPicture!=null)UnityEngine.Object.DestroyImmediate(oldPicture.gameObject);
            var oldNotice=card.Find("Image Notice");if(oldNotice!=null)UnityEngine.Object.DestroyImmediate(oldNotice.gameObject);
            var frame=Box(card,"Action Picture",480,72,436,208,new Color(.93f,.94f,.89f));
            var image=new GameObject("Image",typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter));image.transform.SetParent(frame.transform,false);
            panel.picture=image.GetComponent<RawImage>();panel.picture.raycastTarget=false;
            image.GetComponent<AspectRatioFitter>().aspectMode=AspectRatioFitter.AspectMode.FitInParent;
            panel.imageNotice=Label(card,"Image Notice",480,150,436,40,18);panel.imageNotice.alignment=TextAnchor.MiddleCenter;
            panel.back=card.Find("Close").GetComponent<Button>();panel.back.onClick=new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(panel.back.onClick,panel.ReturnToDetails);
            panel.back.GetComponentInChildren<Text>().text="猫の詳細へ戻る";
            panel.panel.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            Debug.Log("CAT_CAFE_PLAYER_LAYOUT_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();
            var client=preview.GetComponent<PythonStateClient>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.cats[0].can_player_start=true;
            state.player_interaction=new PythonPlayerInteraction{session_id="layout",cat_id=state.cats[0].cat_id,content_cat_id=state.cats[0].content_cat_id,scene_key="teaser",name="ミケ",active=true,remaining_ticks=8,remaining_sets=2,mode="ねこじゃらし",stamina_before=100,stamina=92,affinity_after=1.5f,affinity_delta=1.5f,can_finish=true,
                actions=new[]{new PythonEventChoice{choice="direct",label="素直に動かす",can_select=true}},targets=new[]{new PythonEventChoice{choice="voice",label="声をかける"}},history="1：素直に動かす / 好意的に応じる\n体力 92 / 好感度 +1.5"};
            var keys=new[]{"ball","plush","tunnel","pet","brush","voice","presence"};
            var labels=new[]{"ボール遊び","ぬいぐるみ遊び","トンネル遊び","なでる","ブラッシング","声をかける","そばで見守る"};
            state.player_interaction.targets=Array.ConvertAll(keys,key=>new PythonEventChoice{choice=key,label=labels[Array.IndexOf(keys,key)]});
            state.player_interaction.actions=new[]{new PythonEventChoice{choice="direct",label="素直に動かす",can_select=true},new PythonEventChoice{choice="switch",label="動作を切り替える",can_select=true}};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});apply();
            var player=preview.GetComponent<PythonPlayerPanel>();
            if(!player.summary.text.Contains("100 → 92") || !player.back.interactable)throw new Exception("Interaction summary or return missing");
            if(player.picture.texture==null || player.picture.texture!=preview.PlayerTexture(state.cats[0].content_cat_id,"play"))throw new Exception("Play image fallback missing");
            if(!ContentValidation.ValidSceneKey("ball__feint") || ContentValidation.ValidSceneKey("brush__intense") || ContentValidation.ValidSceneKey("ball__missing"))throw new Exception("Combination keys invalid");
            var content=(ContentBundle)typeof(ContentPreview).GetField("bundle",flags).GetValue(preview);
            var contentCat=Array.Find(content.cats,cat=>cat.cat_id==state.cats[0].content_cat_id);
            var originalScenes=contentCat.scenes;
            var petScene=Array.Find(originalScenes,scene=>scene.key=="pet");
            var expanded=new System.Collections.Generic.List<SceneContent>(originalScenes);
            expanded.Add(new SceneContent{key="teaser__direct",label="test",text="test",image=petScene.image});contentCat.scenes=expanded.ToArray();ContentValidation.Validate(content);
            state.player_interaction.image_key="teaser__direct";apply();
            if(player.picture.texture!=preview.PlayerTexture(state.cats[0].content_cat_id,"pet"))throw new Exception("Combination image priority incorrect");
            state.player_interaction.image_key="teaser__feint";apply();
            if(player.picture.texture!=preview.PlayerTexture(state.cats[0].content_cat_id,"play"))throw new Exception("Combination image fallback incorrect");
            expanded.Add(new SceneContent{key="teaser__open_up",label="test",text="test",image=Array.Find(originalScenes,scene=>scene.key=="normal").image});
            expanded.Add(new SceneContent{key="teaser__simultaneous",label="test",text="test",image=petScene.image});
            contentCat.scenes=expanded.ToArray();ContentValidation.Validate(content);
            state.player_interaction.image_key="teaser__direct";state.player_interaction.event_image_key="teaser__open_up";apply();
            if(player.picture.texture!=preview.PlayerTexture(state.cats[0].content_cat_id,"normal"))throw new Exception("Open up did not override action image");
            state.player_interaction.event_image_key="teaser__simultaneous";apply();
            if(player.picture.texture!=preview.PlayerTexture(state.cats[0].content_cat_id,"pet"))throw new Exception("Simultaneous image incorrect");
            contentCat.scenes=originalScenes;apply();
            if(player.picture.texture!=preview.PlayerTexture(state.cats[0].content_cat_id,"play"))throw new Exception("Missing special image fallback incorrect");
            state.player_interaction.event_image_key=null;
            contentCat.scenes=originalScenes;state.player_interaction.image_key=null;
            state.player_interaction.scene_key="brush";apply();
            if(player.picture.texture!=preview.PlayerTexture(state.cats[0].content_cat_id,"pet"))throw new Exception("Brush fallback incorrect");
            state.player_interaction.content_cat_id="missing-cat";apply();
            if(player.picture.texture!=null || string.IsNullOrEmpty(player.imageNotice.text))throw new Exception("Missing image did not clear");
            state.player_interaction.content_cat_id=state.cats[0].content_cat_id;state.player_interaction.scene_key="teaser";apply();
            Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/PlayerLayout.png"});
            if(player.targets.options.Count!=8)throw new Exception("Switch targets were truncated");
            if(player.targets.options[player.targets.value].text!="ねこじゃらし")throw new Exception("Current mode caption missing");
            player.targets.value=6;apply();
            if(player.targets.options[player.targets.value].text!="声をかける" || !player.actions[1].interactable)throw new Exception("Pending target lost during polling");
            state.player_interaction.mode="声をかける";state.player_interaction.scene_key="voice";
            state.player_interaction.targets=new[]{new PythonEventChoice{choice="teaser",label="ねこじゃらし"},new PythonEventChoice{choice="ball",label="ボール遊び"}};apply();
            if(player.targets.value!=0 || player.targets.options[0].text!="声をかける" || player.actions[1].interactable)throw new Exception("Switch result caption or no-op guard failed");
            player.Close();player.Open();apply();
            if(player.targets.options[player.targets.value].text!="声をかける")throw new Exception("Reopen lost current mode");
            state.player_interaction.mode="ねこじゃらし";state.player_interaction.scene_key="teaser";
            state.player_interaction.targets=Array.ConvertAll(keys,key=>new PythonEventChoice{choice=key,label=labels[Array.IndexOf(keys,key)]});apply();
            typeof(Dropdown).GetMethod("Start",flags).Invoke(player.targets,null);
            player.targets.alphaFadeSpeed=0;player.targets.Show();Canvas.ForceUpdateCanvases();
            typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/PlayerTargets.png"});
            player.targets.Hide();
            player.ReturnToDetails();if(player.panel.activeSelf || !preview.GetComponent<PythonCatDetailsPanel>().panel.activeSelf)throw new Exception("Return to details failed");
            player.Open();state.player_interaction.active=false;state.player_interaction.can_finish=false;apply();
            if(player.finish.gameObject.activeSelf || player.actions[0].gameObject.activeSelf || !player.summary.text.Contains("確定"))throw new Exception("Result actions remained visible");
            player.Disconnect();if(player.back.interactable)throw new Exception("Offline return enabled");
            Debug.Log("CAT_CAFE_PLAYER_LAYOUT_DISPLAY_OK: summary / return / result / offline");
        }
    }
}

