using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonPresentationSetup
    {
        static readonly Color Ink=new Color(.16f,.25f,.21f), Cream=new Color(.97f,.96f,.92f), Green=new Color(.2f,.38f,.3f);
        static void Place(RectTransform rect,float x,float y,float w,float h)
        { rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h); }
        static void Style(Transform root)
        {
            foreach(var text in root.GetComponentsInChildren<Text>(true)){text.color=Ink;text.supportRichText=false;}
            foreach(var button in root.GetComponentsInChildren<Button>(true))
            {
                bool primary=button.name=="Scene sleep" || button.name=="Scene pet" || button.name=="Save" || button.name=="Next" || button.name=="Accept" || button.name=="Confirm" || button.name=="Work";
                var image=button.targetGraphic as Image;if(image!=null)image.color=button.name=="Open Cat Details" ? Color.clear : primary ? Green : new Color(.89f,.91f,.85f);
                var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.9f,.95f,.88f);colors.pressedColor=new Color(.72f,.82f,.7f);colors.selectedColor=colors.normalColor;colors.disabledColor=primary ? new Color(.65f,.7f,.66f) : new Color(.8f,.8f,.77f);colors.fadeDuration=.08f;button.colors=colors;
                foreach(var text in button.GetComponentsInChildren<Text>(true)){text.color=primary ? Color.white : Ink;text.alignment=TextAnchor.MiddleCenter;text.fontSize=button.name=="Open Cat Details" ? 14 : button.name=="Work" || button.name=="Rest" ? 16 : 18;}
            }
            foreach(var scroll in root.GetComponentsInChildren<ScrollRect>(true))
            { var image=scroll.GetComponent<Image>();if(image!=null && image.color.a>.8f)image.color=new Color(.91f,.93f,.87f); }
            foreach(var image in root.GetComponentsInChildren<Image>(true))
            { if(image.gameObject.name.EndsWith("Card") && image.color.a>.8f)image.color=Cream; }
        }
        static void Card(PythonCatCard card)
        {
            Style(card.transform);card.catName.fontSize=20;card.stateText.fontSize=15;
            card.stateText.alignment=TextAnchor.UpperLeft;card.stateText.lineSpacing=1;card.stateText.horizontalOverflow=HorizontalWrapMode.Wrap;
            Place(card.picture.rectTransform,50,36,142,142);
            Place(card.stateText.rectTransform,10,182,222,46);card.stateText.verticalOverflow=VerticalWrapMode.Overflow;
        }
        [MenuItem("Cat Cafe/開発用/UI再生成/営業画面の見た目",false,14)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var root=preview.transform.Find("Content Canvas/Background");Style(root);root.GetComponent<Image>().color=Cream;
            var presentation=preview.GetComponent<PythonPresentation>() ?? preview.gameObject.AddComponent<PythonPresentation>();
            foreach(var text in root.GetComponentsInChildren<Text>(true))if(text.transform.parent==root && text.text.StartsWith("猫カフェ")){presentation.title=text;break;}
            if(presentation.title==null)throw new Exception("Business title missing");Place(presentation.title.rectTransform,24,20,280,34);presentation.title.fontSize=24;
            var summary=root.Find("Business Summary");if(summary==null){var go=new GameObject("Business Summary",typeof(RectTransform),typeof(Text));go.transform.SetParent(root,false);summary=go.transform;}
            presentation.summary=summary.GetComponent<Text>();presentation.summary.font=ContentPreviewLayout.LoadFont();presentation.summary.fontSize=17;presentation.summary.color=Ink;presentation.summary.raycastTarget=false;Place(presentation.summary.rectTransform,24,54,750,24);presentation.summary.text="資金・営業状況は接続後に表示します";
            var heading=preview.GetComponent<PythonServiceScreen>().heading;Place(heading.rectTransform,24,78,750,48);heading.fontSize=16;heading.lineSpacing=1;
            foreach(var card in root.GetComponentsInChildren<PythonCatCard>(true))Card(card);
            var roster=preview.GetComponent<PythonCatRoster>();string path=AssetDatabase.GetAssetPath(roster.cardPrefab.gameObject);var prefab=PrefabUtility.LoadPrefabContents(path);Card(prefab.GetComponent<PythonCatCard>());PrefabUtility.SaveAsPrefabAsset(prefab,path);PrefabUtility.UnloadPrefabContents(prefab);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("CAT_CAFE_PRESENTATION_UI_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.day=2;state.funds=1234;state.seats=new[]{new PythonSeatState(),new PythonSeatState()};state.customers=new PythonCustomerState[0];state.can_set_shifts=true;state.closed=false;state.required_action="";state.objective_progress=new PythonObjectiveProgress{label="人気3段階",summary="第1段階：人気120 / 目標250（期限：20日目）",details="人気を育てて目標を達成しましょう。"};
            foreach(var cat in state.cats){cat.health_label="健康";cat.activity_label="在店";}
            state.cats[0].working=false;state.cats[1].activity="missing";state.cats[1].activity_label="行方不明";state.cats[1].working=false;state.cats[2].health_status="sick";state.cats[2].health_label="病気";
            typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});var roster=preview.GetComponent<PythonCatRoster>();
            if(!roster.GetCard(0).stateText.text.Contains("休養") || !roster.GetCard(1).stateText.text.Contains("行方不明") || !roster.GetCard(2).stateText.text.Contains("病気"))throw new Exception("Card state labels incorrect");
            var header=preview.GetComponent<PythonPresentation>();if(!header.title.text.Contains("2日目") || !header.summary.text.Contains("1234") || !header.summary.text.Contains("営業準備"))throw new Exception("Business summary missing");
            Canvas.ForceUpdateCanvases();foreach(var card in roster.content.GetComponentsInChildren<PythonCatCard>())if(card.stateText.cachedTextGenerator.lineCount<2 || card.stateText.preferredHeight>card.stateText.rectTransform.rect.height+1)throw new Exception("Card text clipped");
            var heading=preview.GetComponent<PythonServiceScreen>().heading;if(heading.preferredHeight>heading.rectTransform.rect.height+1)throw new Exception("Objective text clipped");
            var guide=preview.GetComponent<PythonPreparationPanel>();guide.Close();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/BusinessPresentation.png"});guide.Open();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/PresentationDialog.png"});
            state.can_set_shifts=false;state.tick=8;typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});if(!header.summary.text.Contains("営業中"))throw new Exception("Service summary missing");
            state.closed=true;state.finance=new PythonFinanceState();typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});if(!header.summary.text.Contains("閉店"))throw new Exception("Closing summary missing");
            header.Disconnect();if(!header.summary.text.Contains("未接続"))throw new Exception("Offline summary missing");
            Debug.Log("CAT_CAFE_PRESENTATION_DISPLAY_OK");
        }
    }
}
