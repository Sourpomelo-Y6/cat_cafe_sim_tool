using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview.Editor
{
    public static class PythonSaveSetup
    {
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>(); preview.ConfigurePythonButtons();
            var root=preview.transform.Find("Content Canvas/Background");
            foreach(var name in new[]{"Saved Games","Resume Saved"}) { var old=root.Find(name); if(old!=null) Object.DestroyImmediate(old.gameObject); }
            var dropdown=DefaultControls.CreateDropdown(new DefaultControls.Resources()); dropdown.name="Saved Games"; dropdown.transform.SetParent(root,false);
            Place(dropdown.GetComponent<RectTransform>(),804,76,304);
            var saves=dropdown.GetComponent<Dropdown>(); saves.options.Clear(); saves.options.Add(new Dropdown.OptionData("保存したゲームを選択")); saves.RefreshShownValue(); saves.interactable=false;
            foreach(var text in dropdown.GetComponentsInChildren<Text>(true)) { text.font=ContentPreviewLayout.LoadFont(); text.fontSize=16; }
            saves.itemText.rectTransform.sizeDelta=new Vector2(-30,40);
            ContentPreviewLayout.ConfigureCatDropdown(saves);
            var arrow=dropdown.transform.Find("Arrow");
            if(arrow!=null)
            {
                arrow.GetComponent<Image>().color=Color.clear;
                var marker=new GameObject("Arrow Label",typeof(RectTransform),typeof(Text)); marker.transform.SetParent(arrow,false);
                var markerRect=marker.GetComponent<RectTransform>(); markerRect.anchorMin=Vector2.zero; markerRect.anchorMax=Vector2.one; markerRect.offsetMin=markerRect.offsetMax=Vector2.zero;
                var markerText=marker.GetComponent<Text>(); markerText.font=ContentPreviewLayout.LoadFont(); markerText.fontSize=14; markerText.text="▼"; markerText.color=Color.black; markerText.alignment=TextAnchor.MiddleCenter; markerText.raycastTarget=false;
            }
            var button=DefaultControls.CreateButton(new DefaultControls.Resources()); button.name="Resume Saved"; button.transform.SetParent(root,false);
            Place(button.GetComponent<RectTransform>(),1118,76,138);
            var label=button.GetComponentInChildren<Text>(); label.font=ContentPreviewLayout.LoadFont(); label.fontSize=18; label.text="保存から再開";
            var panel=preview.GetComponent<PythonSavePanel>()??preview.gameObject.AddComponent<PythonSavePanel>(); panel.saves=saves; panel.resume=button.GetComponent<Button>(); panel.resume.interactable=false;
            UnityEventTools.AddPersistentListener(panel.resume.onClick,panel.ResumeSelected);
            var client=preview.GetComponent<PythonStateClient>();
            UnityEventTools.AddPersistentListener(preview.GetSceneButton(3).onClick,client.SaveGame);
            PrefabUtility.RecordPrefabInstancePropertyModifications(preview.GetSceneButton(3));
            PrefabUtility.RecordPrefabInstancePropertyModifications(preview.GetSceneButton(3).GetComponentInChildren<Text>());
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("CAT_CAFE_SAVE_UI_OK: save button / saved game dropdown / resume button");
        }
        static void Place(RectTransform rect,float x,float y,float width)
        {
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1); rect.anchoredPosition=new Vector2(x,-y); rect.sizeDelta=new Vector2(width,38);
        }
        public static void BuildAndCapture()
        {
            Build(); var preview=Object.FindObjectOfType<ContentPreview>(); preview.Initialize(); preview.ApplyPythonState(PythonServiceSetup.Sample());
            preview.GetComponent<PythonSavePanel>().Apply(new PythonSaveList{saves=new[]{new PythonSavedGame{save_id="sample",label="1日目 / 時刻 2 / 資金 1000"}}});
            Canvas.ForceUpdateCanvases();
            typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)
                .Invoke(null,new object[]{preview,"Logs/SaveResume.png"});
        }
    }
}
