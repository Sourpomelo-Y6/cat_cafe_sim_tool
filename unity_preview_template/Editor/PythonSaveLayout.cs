using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonSaveLayout
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

        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafeTitle.unity");
            var title=UnityEngine.Object.FindObjectOfType<PythonTitleScreen>();
            var old=title.savesPanel.transform.Find("Save Details");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            title.saveDetails=Label(title.savesPanel.transform,"Save Details",40,174,760,132,19);
            Place(title.resume.GetComponent<RectTransform>(),40,340,360,56);
            Place(title.savesPanel.transform.Find("Back").GetComponent<RectTransform>(),440,340,360,56);
            for(int i=title.saves.onValueChanged.GetPersistentEventCount()-1;i>=0;i--)if(title.saves.onValueChanged.GetPersistentMethodName(i)=="SelectSave")UnityEventTools.RemovePersistentListener(title.saves.onValueChanged,i);
            UnityEventTools.AddPersistentListener(title.saves.onValueChanged,title.SelectSave);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var panel=UnityEngine.Object.FindObjectOfType<PythonSavePanel>();var bg=panel.transform.Find("Content Canvas/Background");
            // Keep the original dropdown and resume button so their serialized references remain valid.
            panel.saves.transform.SetParent(bg,false);panel.resume.transform.SetParent(bg,false);
            foreach(string name in new[]{"Save Browser","Open Save Browser"}){old=bg.Find(name);if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);}
            panel.panel=Box(bg,"Save Browser",0,174,1280,626,new Color(.12f,.15f,.12f,.94f));
            var card=Box(panel.panel.transform,"Card",170,28,940,550,new Color(.99f,.98f,.95f));
            Label(card.transform,"Title",24,20,892,45,26).text="保存・再開";
            Label(card.transform,"Help",24,70,892,55,18).text="現在のゲームを保存するか、保存済みのゲームを選んで再開します。\n再開すると、現在の未保存の進捗は置き換わります。";
            panel.saves.transform.SetParent(card.transform,false);Place(panel.saves.GetComponent<RectTransform>(),24,140,892,44);
            panel.details=Label(card.transform,"Save Details",24,210,892,128,20);
            panel.notice=Label(card.transform,"Save Notice",24,350,892,80,18);
            panel.notice.text="進捗を残したい場合は、再開する前に保存してください。";
            panel.save=Button(card.transform,"Save Current","現在のゲームを保存",24,460,280);
            UnityEventTools.AddPersistentListener(panel.save.onClick,panel.GetComponent<PythonStateClient>().SaveGame);
            panel.resume.transform.SetParent(card.transform,false);Place(panel.resume.GetComponent<RectTransform>(),330,460,280,40);
            panel.resume.GetComponentInChildren<Text>().text="選んだゲームを再開";
            var close=Button(card.transform,"Close","戻る",636,460,280);UnityEventTools.AddPersistentListener(close.onClick,panel.Close);
            var entry=Button(bg,"Open Save Browser","保存データを確認・再開",804,76,452);UnityEventTools.AddPersistentListener(entry.onClick,panel.Open);
            for(int i=panel.saves.onValueChanged.GetPersistentEventCount()-1;i>=0;i--)if(panel.saves.onValueChanged.GetPersistentMethodName(i)=="SelectSave")UnityEventTools.RemovePersistentListener(panel.saves.onValueChanged,i);
            UnityEventTools.AddPersistentListener(panel.saves.onValueChanged,panel.SelectSave);
            panel.Close();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
            PythonSaveBrowserLayout.RefreshIfReady();Debug.Log("CAT_CAFE_SAVE_LAYOUT_OK");
        }
        public static void Verify()
        {
            foreach(string path in new[]{"Assets/Scenes/CatCafeTitle.unity","Assets/Scenes/CatCafePythonState.unity"})
            {
                EditorSceneManager.OpenScene(path);
                var rows=new PythonSaveList{saves=new[]{new PythonSavedGame{save_id="first",label="2日目 / 時刻 3 / 資金 900",created_at="2026-10-07T00:00:00+00:00",objective_label="人気3段階",phase="open"},new PythonSavedGame{save_id="legacy",label="以前のゲーム"}}};
                var title=UnityEngine.Object.FindObjectOfType<PythonTitleScreen>();
                var panel=UnityEngine.Object.FindObjectOfType<PythonSavePanel>();
                if(title!=null){title.ApplySaves(rows);title.SelectSave(0);if(!title.saveDetails.text.Contains("人気3段階") || !title.saveDetails.text.Contains("営業中"))throw new Exception("Title details missing");title.ShowSaves();Canvas.ForceUpdateCanvases();typeof(PythonTitleSetup).GetMethod("Capture",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{"Logs/SaveTitleDetails.png"});title.SelectSave(1);if(!title.saveDetails.text.Contains("以前の保存データ"))throw new Exception("Legacy title failed");}
                else {panel.Apply(rows);panel.Open();panel.SelectSave(0);if(!panel.details.text.Contains("2026/10/07") || !panel.details.text.Contains("人気3段階"))throw new Exception("Business details missing");var preview=panel.GetComponent<ContentPreview>();preview.Initialize();preview.ApplyPythonState(PythonServiceSetup.Sample());panel.Open();Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/SaveBusinessDetails.png"});panel.SelectSave(1);if(!panel.details.text.Contains("以前の保存データ"))throw new Exception("Legacy business failed");panel.ListUnavailable();if(panel.resume.interactable || !panel.details.text.Contains("接続"))throw new Exception("Unavailable list failed");panel.Close();}
            }
            Debug.Log("CAT_CAFE_SAVE_DETAILS_VERIFIED");
        }
        public static void BuildAndVerify(){Build();Verify();}
    }
}
