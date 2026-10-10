using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonStartFlowLayout
    {
        static void Place(Component c,float x,float y,float w,float h){var rect=c.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h);}
        static GameObject Box(Transform parent,string name,float x,float y,float w,float h){var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);Place(go.GetComponent<RectTransform>(),x,y,w,h);go.GetComponent<Image>().color=new Color(.95f,.96f,.92f);return go;}
        static void Bind(Button button,UnityEngine.Events.UnityAction action){button.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(button.onClick,action);button.onClick.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);}
        public static void RefreshTitle(PythonTitleScreen title)
        {
            var scroll=title.conditions.GetComponentInParent<ScrollRect>(true);
            if(scroll==null){var parent=title.conditions.transform.parent;var area=Box(parent,"Start Conditions Scroll",40,96,760,240);scroll=area.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;var viewport=Box(area.transform,"Viewport",8,8,720,224);viewport.AddComponent<Mask>().showMaskGraphic=false;title.conditions.transform.SetParent(viewport.transform,false);Place(title.conditions,0,0,720,224);title.conditions.fontSize=18;scroll.viewport=viewport.GetComponent<RectTransform>();scroll.content=title.conditions.rectTransform;var bar=DefaultControls.CreateScrollbar(new DefaultControls.Resources());bar.name="Scrollbar";bar.transform.SetParent(area.transform,false);Place(bar.GetComponent<RectTransform>(),738,8,14,224);scroll.verticalScrollbar=bar.GetComponent<Scrollbar>();scroll.verticalScrollbar.direction=Scrollbar.Direction.BottomToTop;}
            title.conditionsBack=title.conditionsPanel.transform.Find("Back").GetComponent<Button>();Bind(title.conditionsBack,title.BackFromConditions);Bind(title.startGame,title.StartNewGame);title.startGame.GetComponentInChildren<Text>().text="この目標で開始（確認）";title.conditionsBack.GetComponentInChildren<Text>().text="タイトルへ戻る";
        }
        public static void RefreshResult(PythonGameOverPanel result)
        {
            Bind(result.save,result.Save);Bind(result.title,result.Return);if(result.summary==null){result.summary=Object.Instantiate(result.notice,result.notice.transform.parent);result.summary.name="End Summary";}Place(result.summary,24,76,892,92);result.summary.fontSize=18;Place(result.scroll,24,178,892,300);Place(result.scroll.viewport,0,0,866,300);result.scroll.verticalScrollbar.GetComponent<RectTransform>().sizeDelta=new Vector2(20,300);result.details.rectTransform.sizeDelta=new Vector2(866,300);result.save.GetComponentInChildren<Text>().text="結果をセーブ（スロット選択）";result.title.GetComponentInChildren<Text>().text="タイトルへ・新しく始める";Place(result.notice,24,488,892,50);result.notice.fontSize=15;result.notice.lineSpacing=1;result.notice.text="結果はセーブ画面で保存できます。新しいカフェはタイトルの「はじめから」で始めます。";
        }
        [MenuItem("Cat Cafe/開発用/UI再生成/開始・再開・終了画面",false,45)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafeTitle.unity");var title=Object.FindObjectOfType<PythonTitleScreen>();RefreshTitle(title);title.ShowHome();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");RefreshResult(Object.FindObjectOfType<PythonGameOverPanel>());EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("CAT_CAFE_START_FLOW_LAYOUT_OK");
        }
    }
}
