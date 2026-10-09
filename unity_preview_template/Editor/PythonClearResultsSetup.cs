using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonClearResultsSetup
    {
        static T Copy<T>(T source,Transform root,Transform target) where T:Component=>target.Find(AnimationUtility.CalculateTransformPath(source.transform,root)).GetComponent<T>();
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static void Bind(Button b,UnityEngine.Events.UnityAction action){b.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(b.onClick,action);b.onClick.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);}
        [MenuItem("Cat Cafe/開発用/UI再生成/クリア記録",false,43)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var source=preview.GetComponent<PythonFinanceHistoryPanel>();var root=source.panel.transform.parent;var old=root.Find("Clear Results");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var p=preview.GetComponent<PythonClearResultsPanel>() ?? preview.gameObject.AddComponent<PythonClearResultsPanel>();p.panel=UnityEngine.Object.Instantiate(source.panel,root);p.panel.name="Clear Results";var s=source.panel.transform;var t=p.panel.transform;p.title=Copy(source.title,s,t);p.details=Copy(source.details,s,t);p.notice=Copy(source.notice,s,t);p.close=Copy(source.close,s,t);var card=p.close.transform.parent;
            UnityEngine.Object.DestroyImmediate(Copy(source.previous,s,t).gameObject);UnityEngine.Object.DestroyImmediate(Copy(source.next,s,t).gameObject);
            var drop=UnityEngine.Object.Instantiate(preview.GetComponent<PythonPreparationPanel>().cats,card);drop.name="Completed Goals";Place(drop.GetComponent<RectTransform>(),24,78,892,40);p.goals=drop;drop.onValueChanged=new Dropdown.DropdownEvent();UnityEventTools.AddPersistentListener(drop.onValueChanged,p.Select);drop.onValueChanged.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
            var arrow=drop.transform.Find("Dropdown Arrow");if(arrow!=null)Place(arrow.GetComponent<RectTransform>(),858,7,26,26);
            Bind(p.close,p.Close);p.close.GetComponentInChildren<Text>().text="営業画面へ";p.panel.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);PythonEventListSetup.Build();Debug.Log("CAT_CAFE_CLEAR_RESULTS_UI_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="clear-ui";state.clear_results=new PythonClearResults{rows=new PythonClearRecord[0],notes="人気の最終段階・有力者・猫との好感度の達成結果を、ここで見返せます。"};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});apply();var p=preview.GetComponent<PythonClearResultsPanel>();var menu=preview.GetComponent<PythonMenuPanel>();menu.Open();
            if(menu.entries.Length!=15 || !menu.entries[14].interactable || !menu.entries[14].GetComponentInChildren<Text>().text.Contains("クリア記録"))throw new Exception("Clear menu missing");
            Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/ClearResultsMenu.png"});
            menu.Select(14);if(menu.panel.activeSelf || !p.panel.activeSelf || p.goals.interactable || !p.details.text.Contains("まだありません"))throw new Exception("Empty clear view failed");
            state.clear_results.rows=new[]{
                new PythonClearRecord{mode="popularity",label="人気目標",details="達成日：12日目\n目標の達成値：650 / 650\n所持金：1234\n店の人気：650\n在籍猫数：5匹\n累計接客売上：5600",notes="達成した瞬間の記録です。継続営業後も数値は変わりません。"},
                new PythonClearRecord{mode="patron",label="有力者の満足度",details="達成日：8日目\n目標の達成値：25 / 25\n所持金：記録なし\n店の人気：記録なし\n在籍猫数：記録なし\n累計接客売上：記録なし",notes="旧セーブには達成時の詳しい成果がありません。現在値では補いません。"},
                new PythonClearRecord{mode="bond",label="猫との好感度",details="達成日：4日目\n目標の達成値：6 / 6匹",notes="この目標の結果は確認済みです。"}};
            apply();p.Open();if(!p.goals.interactable || p.goals.options.Count!=3 || !p.details.text.Contains("1234"))throw new Exception("Captured clear view failed");
            Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/ClearResults.png"});
            p.goals.onValueChanged.Invoke(1);if(!p.notice.text.Contains("旧セーブ") || !p.details.text.Contains("記録なし"))throw new Exception("Legacy clear view failed");
            p.goals.onValueChanged.Invoke(2);state.funds=99;apply();if(!p.details.text.Contains("6 / 6匹") || p.goals.value!=2)throw new Exception("Polling reset clear selection");
            p.Disconnect();if(p.goals.interactable || !p.notice.text.Contains("未接続") || !p.close.interactable)throw new Exception("Offline clear guard failed");apply();
            state.game_over=new PythonGameOver{reason="funds",day=10};apply();p.Open();preview.RefreshPythonControls();if(p.panel.transform.GetSiblingIndex()!=p.panel.transform.parent.childCount-1)throw new Exception("Game over obscured historical record");
            state.instance_id="new-game";state.clear_results.rows=new PythonClearRecord[0];apply();if(p.panel.activeSelf || !p.details.text.Contains("まだありません"))throw new Exception("Old records retained across load");
            if(typeof(PythonStateClient).GetField("pending",flags).GetValue(client)!=null)throw new Exception("Record browsing mutated game");
            Debug.Log("CAT_CAFE_CLEAR_RESULTS_DISPLAY_OK: menu / empty / three modes / frozen / legacy / offline / game over / new game / read only");
        }
    }
}
