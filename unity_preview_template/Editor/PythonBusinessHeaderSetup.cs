using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonBusinessHeaderSetup
    {
        static void Place(Component c,float x,float y,float w,float h){var r=c.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static void Button(Button b,float x,float y,float w,string caption=null){Place(b,x,y,w,38);var t=b.GetComponentInChildren<Text>();t.fontSize=17;if(caption!=null)t.text=caption;}
        public static void Configure(ContentPreview preview)
        {
            var root=preview.transform.Find("Content Canvas/Background");var header=preview.GetComponent<PythonBusinessHeader>() ?? preview.gameObject.AddComponent<PythonBusinessHeader>();
            var presentation=preview.GetComponent<PythonPresentation>();presentation.title.gameObject.SetActive(false);header.summary=presentation.summary;Place(header.summary,24,4,610,32);header.summary.fontSize=19;header.summary.alignment=TextAnchor.MiddleLeft;
            header.progress=preview.GetSceneButton(2);Button(header.progress,24,44,160);Button(preview.GetSceneButton(3),648,4,144,"保存");
            header.preparation=preview.GetComponent<PythonPreparationPanel>().open;Button(header.preparation,196,44,160,"営業準備");header.auto=preview.GetComponent<PythonAutoPanel>();Button(header.auto.toggle,368,44,174);Button(header.auto.speed,554,44,124);
            Button(preview.GetComponent<PythonMenuPanel>().open,690,44,140);Button(preview.GetComponent<PythonEventListPanel>().open,842,44,160);Button(preview.GetComponent<PythonHousingPanel>().open,1014,44,242,"飼育スペース");
            Button(root.Find("Open Save Browser").GetComponent<Button>(),804,4,220,"保存データを開く");Button(root.Find("Return To Title").GetComponent<Button>(),1036,4,220,"タイトルへ");
            header.hint=root.Find("Title Save Hint").GetComponent<Text>();Place(header.hint,24,130,760,34);header.hint.fontSize=14;
            var service=preview.GetComponent<PythonServiceScreen>();Button(service.seatsTab,824,118,210);Button(service.catsTab,1046,118,210);
            var p=preview.GetComponent<PythonAutoPlayPanel>();Place(p.activity.transform,24,94,760,70);Place(p.activityStatus,12,2,736,28);p.activityStatus.fontSize=17;Button(p.activityStop,12,32,350);Button(p.activityDetails,374,32,374);
            preview.GetComponentInChildren<CanvasScaler>().screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        }
        [MenuItem("Cat Cafe/開発用/UI再生成/営業画面の上部",false,22)]
        public static void Build(){var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");Configure(Object.FindObjectOfType<ContentPreview>());EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("CAT_CAFE_BUSINESS_HEADER_UI_OK");}
    }
}
