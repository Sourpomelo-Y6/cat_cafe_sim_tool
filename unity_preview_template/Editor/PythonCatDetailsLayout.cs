using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonCatDetailsLayout
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
        static void Move(Button button,float x,float y,float w,string caption)
        { Place(button.GetComponent<RectTransform>(),x,y,w,40);var label=button.GetComponentInChildren<Text>();label.text=caption;label.fontSize=17; }
        [MenuItem("Cat Cafe/開発用/UI再生成/猫の詳細の配置",false,15)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var panel=preview.GetComponent<PythonCatDetailsPanel>();var card=panel.panel.transform.Find("Details Card");
            foreach(var name in new[]{"Details Scroll","Interaction Notice","Health Section","Growth Section","Interaction Section","Dispatch Section","Adoption Section","Interaction Heading","Dispatch Heading","Adoption Heading"}){var old=card.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
            ScrollRect health;panel.stateDetails=ScrollText(card,"Health Section",24,80,304,282,17,out health);panel.details=ScrollText(card,"Growth Section",348,80,488,282,18,out panel.scroll);
            Label(card,"Interaction Heading",24,374,250,26,18).text="猫との交流";Label(card,"Dispatch Heading",294,374,252,26,18).text="派遣・家出と帰還";Label(card,"Adoption Heading",566,374,270,26,18).text="譲渡の設定と履歴";
            Move(panel.interact,24,408,250,"猫と交流する");Move(panel.previousInteraction,24,456,250,"前回の交流を見る");Move(panel.dispatch,294,408,252,"派遣先・報酬を確認");Move(panel.missing,294,548,252,"家出・帰還の記録");Move(panel.adoption,566,408,270,"譲渡の設定・履歴");
            ScrollRect ignored;panel.interactionNotice=ScrollText(card,"Interaction Section",24,502,250,84,14,out ignored);panel.dispatchNotice=ScrollText(card,"Dispatch Section",294,456,252,84,14,out ignored);panel.adoptionNotice=ScrollText(card,"Adoption Section",566,456,270,130,14,out ignored);
            Place(panel.title.rectTransform,24,20,670,48);panel.panel.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("CAT_CAFE_CAT_DETAILS_LAYOUT_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="detail-layout";state.cats[0].health_label="健康";state.cats[0].activity_label="在店";state.cats[0].growth_details="得意な交流：遊び・触れ合い\n習得した行動：ねこじゃらし\n次の習得まであと12回";state.cats[0].can_player_start=false;state.cats[0].player_reason="営業中は交流できません。";state.adoption=new PythonAdoption{enabled=false,can_configure=false,reason="営業準備中に変更できます。",events=new PythonAdoptionEvent[0]};state.patron_dispatch=new PythonPatronDispatch{options=new[]{new PythonDispatchOption{cat_id=state.cats[0].cat_id,can_select=false,reason="営業準備中に派遣してください。"}},events=new PythonDispatchEvent[0]};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});apply();var panel=preview.GetComponent<PythonCatDetailsPanel>();panel.Open(state.cats[0].cat_id);
            if(!panel.stateDetails.text.Contains("体力") || !panel.details.text.Contains("ねこじゃらし") || panel.details.text.Contains("体力：") || panel.interact.interactable || !panel.interactionNotice.text.Contains("営業中") || !panel.dispatchNotice.text.Contains("営業準備") || !panel.adoptionNotice.text.Contains("OFF"))throw new Exception("Detail groups or reasons missing");
            Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/CatDetailsLayout.png"});
            state.cats[0].growth_details=new string('長',4000);apply();Canvas.ForceUpdateCanvases();if(panel.scroll.content.rect.height<=panel.scroll.viewport.rect.height)throw new Exception("Growth did not scroll");
            typeof(PythonStateClient).GetField("online",flags).SetValue(client,false);panel.Disconnect();if(panel.interact.interactable || panel.dispatch.interactable || panel.adoption.interactable || !panel.dispatchNotice.text.Contains("未接続"))throw new Exception("Offline actions enabled");
            state.cats[0].growth_details="得意な交流：静かな交流";state.cats[0].can_player_start=true;apply();if(!panel.interact.interactable || !panel.details.text.Contains("静かな交流"))throw new Exception("Reconnect lost selected cat");
            Debug.Log("CAT_CAFE_CAT_DETAILS_LAYOUT_DISPLAY_OK");
        }
    }
}
