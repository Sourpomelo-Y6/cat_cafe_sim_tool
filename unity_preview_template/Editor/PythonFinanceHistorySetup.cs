using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonFinanceHistorySetup
    {
        static T Copy<T>(T source,Transform root,Transform target) where T:Component{return target.Find(AnimationUtility.CalculateTransformPath(source.transform,root)).GetComponent<T>();}
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static void Bind(Button b,UnityEngine.Events.UnityAction action){b.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(b.onClick,action);b.onClick.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);}
        static Button Button(Transform parent,string name,float x,float y,float w){var g=DefaultControls.CreateButton(new DefaultControls.Resources());g.name=name;g.transform.SetParent(parent,false);Place(g.GetComponent<RectTransform>(),x,y,w,40);var text=g.GetComponentInChildren<Text>();text.font=ContentPreviewLayout.LoadFont();text.fontSize=18;return g.GetComponent<Button>();}
        [MenuItem("Cat Cafe/開発用/UI再生成/収支の内訳・日次比較",false,36)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var source=preview.GetComponent<PythonDayOffPanel>();var root=source.panel.transform.parent;var old=root.Find("Finance History");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var p=preview.GetComponent<PythonFinanceHistoryPanel>() ?? preview.gameObject.AddComponent<PythonFinanceHistoryPanel>();p.panel=UnityEngine.Object.Instantiate(source.panel,root);p.panel.name="Finance History";var s=source.panel.transform;var t=p.panel.transform;
            p.title=Copy(source.title,s,t);p.details=Copy(source.details,s,t);p.notice=Copy(source.notice,s,t);p.close=Copy(source.close,s,t);var card=p.close.transform.parent;
            UnityEngine.Object.DestroyImmediate(t.Find(AnimationUtility.CalculateTransformPath(source.confirmation.transform,s)).gameObject);UnityEngine.Object.DestroyImmediate(Copy(source.proceed,s,t).gameObject);
            p.previous=Button(card,"Previous",24,78,432);p.next=Button(card,"Next",484,78,432);Bind(p.previous,p.Previous);Bind(p.next,p.Next);Bind(p.close,p.Close);Place(p.close.GetComponent<RectTransform>(),24,550,892,40);
            var scroll=p.details.transform.parent.parent.GetComponent<ScrollRect>();Place(scroll.GetComponent<RectTransform>(),24,130,892,352);Place(scroll.viewport,6,6,862,340);scroll.verticalScrollbar.GetComponent<RectTransform>().sizeDelta=new Vector2(12,340);p.details.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,862);p.details.fontSize=18;
            PythonFinanceHistoryLayout.RefreshIfReady(preview);p.panel.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);PythonClosingSetup.Build();Debug.Log("CAT_CAFE_FINANCE_HISTORY_UI_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="finance-history";state.closed=true;state.required_action="";state.finance_history=new PythonFinanceHistory{totals="記録期間の収支\n総収入：+300 / 総支出：+500 / 純収支：-200",notes="閉店した日の確定結果です。営業途中の日は含みません。",rows=new[]{new PythonFinanceDay{day=1,label="1日目・営業日",overview="資金 1000 → 1100 / 純収支 +100",details="接客売上：200\n運営費：100\n前日との差：比較できる記録なし"},new PythonFinanceDay{day=2,label="2日目・休業日",overview="資金 1100 → 800 / 純収支 -300",details="総収入：100\n総支出：400\n派遣収入：100\n用品購入費：300\n運営費：100\n前日との差（当日−前日）\n純収支：-400"}}};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});apply();var p=preview.GetComponent<PythonFinanceHistoryPanel>();var closing=preview.GetComponent<PythonClosingPanel>();closing.history.onClick.Invoke();if(!p.panel.activeSelf || !p.details.text.Contains("2日目・休業日") || p.next.interactable || !p.previous.interactable)throw new Exception("Closing entry or newest day failed");
            Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/FinanceHistory.png"});
            p.previous.onClick.Invoke();if(!p.details.text.Contains("1日目・営業日") || p.previous.interactable || !p.next.interactable)throw new Exception("Previous day failed");state.revision++;apply();if(!p.details.text.Contains("1日目・営業日"))throw new Exception("Selection lost");p.next.onClick.Invoke();if(!p.details.text.Contains("-400"))throw new Exception("Next/comparison failed");
            state.finance_history.rows[1].details=new string('長',4000);apply();Canvas.ForceUpdateCanvases();var scroll=p.details.GetComponentInParent<ScrollRect>();if(scroll.content.rect.height<=scroll.viewport.rect.height)throw new Exception("Long history did not scroll");p.Disconnect();if(p.previous.interactable || !p.notice.text.Contains("未接続"))throw new Exception("Offline controls enabled");apply();state.instance_id="loaded";state.finance_history.rows=new PythonFinanceDay[0];apply();if(p.panel.activeSelf)throw new Exception("Load did not close");p.Open();if(!p.details.text.Contains("まだ閉店") || p.previous.interactable || p.next.interactable)throw new Exception("Empty history failed");if(typeof(PythonStateClient).GetField("pending",flags).GetValue(client)!=null)throw new Exception("Browsing sent command");Debug.Log("CAT_CAFE_FINANCE_HISTORY_DISPLAY_OK");
        }
    }
}
