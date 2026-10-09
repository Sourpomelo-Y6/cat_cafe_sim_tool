using System;
using UnityEditor;
using UnityEngine;
namespace CatCafePreview.Editor
{
    public static class PythonBusinessHistorySetup
    {
        [MenuItem("Cat Cafe/開発用/UI再生成/営業成果・日次比較",false,45)]
        public static void Build()
        {
            PythonEventListSetup.Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var panel=preview.GetComponent<PythonFinanceHistoryPanel>();panel.notice.fontSize=14;panel.notice.alignment=UnityEngine.TextAnchor.UpperLeft;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(preview.gameObject.scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(preview.gameObject.scene);
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="business-history";state.business_history=new PythonFinanceHistory{totals="閉店済みの記録：0日分",notes="閉店済みの日を比較します（休業日を含む）。\n未導入・記録不足は現在値やゼロで補いません。",rows=new PythonFinanceDay[0]};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});apply();var menu=preview.GetComponent<PythonMenuPanel>();var p=preview.GetComponent<PythonFinanceHistoryPanel>();menu.Open();
            if(menu.entries.Length!=16 || !menu.entries[15].interactable)throw new Exception("Business history menu missing");menu.Select(15);
            if(menu.panel.activeSelf || !p.panel.activeSelf || p.previous.interactable || p.next.interactable || !p.title.text.Contains("営業成果") || !p.details.text.Contains("まだ閉店"))throw new Exception("Empty history failed");
            state.business_history.rows=new[]{new PythonFinanceDay{day=1,label="1日目 / 営業日",overview="閉店した日の成果",details="店の人気：105\n獲得人気：5\n接客評価・満足：3\n接客評価・普通：1\n接客評価・不満：0\n満員で帰った客：2\n待ち切れず帰った客：1\n\n前日との差\n店の人気：比較できる記録なし"},new PythonFinanceDay{day=2,label="2日目 / 休業日",overview="休業日の成果",details="店の人気：100\n獲得人気：0\n接客評価・満足：0\n接客評価・普通：0\n接客評価・不満：0\n満員で帰った客：0\n待ち切れず帰った客：0\n\n前日との差\n店の人気：-5"}};
            state.business_history.totals="閉店済みの記録：2日分";apply();p.OpenBusiness();if(!p.previous.interactable || p.next.interactable || !p.details.text.Contains("-5"))throw new Exception("Latest comparison failed");
            p.previous.onClick.Invoke();if(p.previous.interactable || !p.next.interactable || !p.details.text.Contains("営業日"))throw new Exception("History navigation failed");
            apply();if(!p.details.text.Contains("1日目"))throw new Exception("Polling reset selection");
            Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/BusinessHistory.png"});
            p.Disconnect();if(p.previous.interactable || p.next.interactable || !p.close.interactable || !p.notice.text.Contains("未接続"))throw new Exception("Offline history failed");apply();
            state.finance_history=new PythonFinanceHistory{totals="総収入：500",notes="収支",rows=new[]{new PythonFinanceDay{day=1,label="収支1日目",details="純収支：100"}}};apply();p.Open();if(!p.title.text.Contains("収支") || !p.details.text.Contains("純収支"))throw new Exception("Financial mode regression");
            p.OpenBusiness();state.instance_id="new-game";state.business_history.rows=new PythonFinanceDay[0];apply();if(p.panel.activeSelf || !p.details.text.Contains("まだ閉店"))throw new Exception("Old history retained");
            if(typeof(PythonStateClient).GetField("pending",flags).GetValue(client)!=null)throw new Exception("Browsing sent command");
            Debug.Log("CAT_CAFE_BUSINESS_HISTORY_OK: menu / empty / closed days / previous difference / navigation / selection / offline / finance mode / new game / read only");
        }
    }
}
