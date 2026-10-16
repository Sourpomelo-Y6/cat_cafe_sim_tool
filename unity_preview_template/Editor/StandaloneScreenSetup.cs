using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    // Upgrade the existing scenes in place, retaining artwork and all other panels.
    public static class StandaloneScreenSetup
    {
        public static readonly string[] Scenes={"Assets/Scenes/CatCafeTitle.unity","Assets/Scenes/CatCafePythonState.unity"};
        [MenuItem("Cat Cafe/Standalone/Connect existing screens")]
        public static void Configure()
        {
            foreach(string path in Scenes)
            {
                var scene=EditorSceneManager.OpenScene(path);foreach(var scaler in UnityEngine.Object.FindObjectsOfType<CanvasScaler>())scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;var panel=UnityEngine.Object.FindObjectOfType<PythonLegacySavePanel>();
                if(panel==null)throw new Exception("Missing legacy import panel: "+path);
                var card=panel.panel.transform.Find("Card");
                if(panel.slots==null)
                {
                    var go=DefaultControls.CreateDropdown(new DefaultControls.Resources());go.name="Import Slot";go.transform.SetParent(card,false);
                    var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(330,-178);r.sizeDelta=new Vector2(586,40);
                    panel.slots=go.GetComponent<Dropdown>();foreach(var text in go.GetComponentsInChildren<Text>(true)){text.font=ContentPreviewLayout.LoadFont();text.fontSize=18;}
                }
                card.Find("Help").GetComponent<Text>().text="変換済み取込JSONの絶対パスを貼り付けてください。\n元のPythonセーブは convert_python_save.py で事前に変換します。";
                panel.path.transform.Find("Placeholder").GetComponent<Text>().text="例：C:\\Games\\CatCafeSim\\converted-save.json";
                panel.resume.GetComponentInChildren<Text>().text="空きスロットへ取込・再開";
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            }
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(Scenes[0],true),new EditorBuildSettingsScene(Scenes[1],true)};
        }
        public static void VerifyAndBuild()
        {
            Configure();StandaloneCafeSmoke.Run("StandaloneEditorSmoke.json");Build();
        }
        public static void Build()
        {
            StandaloneBuildStorage.Build(Scenes);
            Debug.Log("CAT_CAFE_STANDALONE_SCREENS_BUILD_OK");
        }
    }
}
