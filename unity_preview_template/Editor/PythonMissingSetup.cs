using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonMissingSetup
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
        static Dropdown Dropdown(Transform parent,string name,float y)
        {
            var go=DefaultControls.CreateDropdown(new DefaultControls.Resources());go.name=name;go.transform.SetParent(parent,false);
            Place(go.GetComponent<RectTransform>(),24,y,892,40);var drop=go.GetComponent<Dropdown>();
            foreach(var text in go.GetComponentsInChildren<Text>(true)){text.font=ContentPreviewLayout.LoadFont();text.fontSize=18;}
            drop.itemText.rectTransform.parent.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,36);
            drop.template.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,180);drop.itemText.verticalOverflow=VerticalWrapMode.Overflow;drop.itemText.alignment=TextAnchor.MiddleLeft;
            go.transform.Find("Arrow").GetComponent<Image>().color=Color.clear;Label(go.transform,"Arrow Text",862,7,26,26,16).text="▼";return drop;
        }
        [MenuItem("Cat Cafe/開発用/UI再生成/猫の家出・帰還",false,9)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var root=preview.transform.Find("Content Canvas/Background");
            var old=root.Find("Missing Cats");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var overlay=Box(root,"Missing Cats",0,174,1280,626,new Color(0,0,0,.55f));
            var card=Box(overlay.transform,"Missing Card",170,8,940,600,new Color(.98f,.97f,.93f,1));
            var panel=preview.GetComponent<PythonMissingPanel>() ?? preview.gameObject.AddComponent<PythonMissingPanel>();panel.panel=overlay;
            panel.title=Label(card.transform,"Title",24,20,892,40,25);
            panel.cats=Dropdown(card.transform,"Missing Events",76);UnityEventTools.AddPersistentListener(panel.cats.onValueChanged,panel.Select);
            panel.details=Label(card.transform,"Details",24,136,892,348,20);
            panel.rest=Button(card.transform,"Rest","休業して帰還を待つ",24,434,892);UnityEventTools.AddPersistentListener(panel.rest.onClick,panel.Rest);
            panel.notice=Label(card.transform,"Notice",24,496,892,48,16);
            panel.confirm=Button(card.transform,"Confirm","帰還と費用を確認する",24,550,432);UnityEventTools.AddPersistentListener(panel.confirm.onClick,panel.Confirm);
            panel.close=Button(card.transform,"Close","閉じる",480,550,436);UnityEventTools.AddPersistentListener(panel.close.onClick,panel.Close);
            var detail=preview.GetComponent<PythonCatDetailsPanel>();var detailCard=detail.panel.transform.Find("Details Card");
            var entry=detailCard.Find("Missing Cats Entry");if(entry!=null)UnityEngine.Object.DestroyImmediate(entry.gameObject);
            Place(detail.interact.GetComponent<RectTransform>(),24,550,188,40);
            Place(detail.previousInteraction.GetComponent<RectTransform>(),232,550,188,40);
            Place(detail.dispatch.GetComponent<RectTransform>(),440,550,188,40);
            detail.missing=Button(detailCard,"Missing Cats Entry","家出・帰還",648,550,188);UnityEventTools.AddPersistentListener(detail.missing.onClick,detail.Missing);
            detail.missing.interactable=false;overlay.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("CAT_CAFE_MISSING_UI_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.missing_cats=new[]{new PythonMissingCat{choice="missing-1",cat_id=state.cats[0].cat_id,name="ミケ",status="missing",departed_day=1,remaining=2}};
            state.missing_rest=new PythonMissingRest{can_rest=true,cost=25,funds_after=975};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});apply();
            var panel=preview.GetComponent<PythonMissingPanel>();
            if(!panel.panel.activeSelf || panel.confirm.interactable || !panel.close.interactable || !panel.rest.interactable || !panel.details.text.Contains("あと 2回") || !panel.details.text.Contains("975"))throw new Exception("Missing display failed");
            panel.Close();apply();if(panel.panel.activeSelf)throw new Exception("Dismissed missing reopened");
            var detail=preview.GetComponent<PythonCatDetailsPanel>();detail.Open(state.cats[0].cat_id);detail.Missing();if(!panel.panel.activeSelf || detail.panel.activeSelf)throw new Exception("Missing entry failed");
            state.missing_cats[0].status="waiting";state.missing_cats[0].remaining=0;state.missing_cats[0].can_resolve=true;state.missing_cats[0].kitten=true;
            state.missing_cats[0].cost=250;state.missing_cats[0].funds_after=750;state.missing_cats[0].max_stamina=100;state.missing_cats[0].return_stress=20;apply();
            if(!panel.confirm.interactable || panel.close.interactable || !panel.details.text.Contains("250") || !panel.details.text.Contains("750"))throw new Exception("Return display failed");
            Canvas.ForceUpdateCanvases();if(panel.details.preferredHeight>panel.details.rectTransform.rect.height)throw new Exception("Return text clipped");
            typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/MissingReturn.png"});
            panel.Disconnect();if(panel.confirm.interactable || !panel.close.interactable)throw new Exception("Offline return enabled");
            state.missing_cats=new[]{state.missing_cats[0],new PythonMissingCat{choice="missing-2",cat_id=state.cats[1].cat_id,name="タマ",status="missing",departed_day=2,remaining=1}};apply();
            state.missing_cats[0].status="resolved";state.missing_cats[0].resolved_day=3;state.missing_cats[0].can_resolve=false;apply();
            if(panel.confirm.interactable || !panel.close.interactable)throw new Exception("Repeated return enabled");
            if(!panel.details.text.Contains("3日目に帰還を確認"))throw new Exception("Other missing cat hid return result");
            panel.Close();apply();if(panel.panel.activeSelf)throw new Exception("Dismissed result reopened");
            state.missing_cats=new PythonMissingCat[0];apply();if(panel.panel.activeSelf)throw new Exception("Old game remained visible");
            panel.Open(state.cats[0].cat_id);apply();if(!panel.panel.activeSelf || panel.confirm.interactable)throw new Exception("Empty history flickered");panel.Close();
            Debug.Log("CAT_CAFE_MISSING_DISPLAY_OK");
        }
    }
}
