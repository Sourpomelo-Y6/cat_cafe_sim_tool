using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonDispatchComparisonRow
    { public string choice,kind,label,reason,unlock,details;public int days;public float cost,base_reward,trait_multiplier,growth_multiplier,welcome_bonus,reward,satisfaction;public bool can_select; }
    [Serializable] public class PythonDispatchComparisonCat {public string cat_id,name,status;public PythonDispatchComparisonRow[] rows;}
    [Serializable] public class PythonDispatchComparison {public PythonDispatchComparisonCat[] cats;public string notes;}
    public class PythonDispatchComparisonPanel : MonoBehaviour
    {
        public GameObject panel;
        public Dropdown cats,destinations;
        public Text title,details,notice;
        public Button proceed,close;
        private PythonCafeState current;
        private string instance,catId,destinationKey;
        private bool online;
        private PythonDispatchComparisonCat Cat => Array.Find(current?.dispatch_comparison?.cats ?? new PythonDispatchComparisonCat[0],r=>r.cat_id==catId);
        private PythonDispatchComparisonRow Row => Array.Find(Cat?.rows ?? new PythonDispatchComparisonRow[0],r=>Key(r)==destinationKey);
        private static string Key(PythonDispatchComparisonRow row)=>row.kind+"/"+row.choice;
        public void Open(){if(!online || current?.dispatch_comparison==null || !GetComponent<PythonStateClient>().CanManageSaves)return;panel.SetActive(true);panel.transform.SetAsLastSibling();Render();ScrollToSelection();}
        public void Close(){panel.SetActive(false);}
        public void ResetSelection(){catId=destinationKey=null;Close();}
        public void SelectCat(int index){var rows=current?.dispatch_comparison?.cats;if(!online || !GetComponent<PythonStateClient>().CanManageSaves || rows==null || index<0 || index>=rows.Length)return;catId=rows[index].cat_id;Render();ScrollToSelection();}
        public void SelectDestination(int index){var rows=Cat?.rows;if(!online || !GetComponent<PythonStateClient>().CanManageSaves || rows==null || index<0 || index>=rows.Length)return;destinationKey=Key(rows[index]);Render();ScrollToSelection();}
        public void Proceed()
        {
            if(!online || !GetComponent<PythonStateClient>().CanManageSaves || Row==null)return;
            var row=Row;Close();
            if(row.kind=="patron")GetComponent<PythonDispatchPanel>().OpenOption(catId,row.choice);else GetComponent<PythonGeneralDispatchPanel>().OpenOption(catId,row.choice);
        }
        public void Apply(PythonCafeState state){if(instance!=state.instance_id){instance=state.instance_id;ResetSelection();}current=state;online=true;if(state.dispatch_comparison==null)Close();Render();}
        static void Fill(Dropdown dropdown,string[] labels,int index){dropdown.options.Clear();foreach(var label in labels)dropdown.options.Add(new Dropdown.OptionData(label));if(labels.Length==0)dropdown.options.Add(new Dropdown.OptionData("比較対象なし"));dropdown.SetValueWithoutNotify(Math.Max(0,index));dropdown.RefreshShownValue();}
        private void Render()
        {
            var list=current?.dispatch_comparison?.cats ?? new PythonDispatchComparisonCat[0];if(!Array.Exists(list,r=>r.cat_id==catId))catId=list.FirstOrDefault()?.cat_id;
            Fill(cats,list.Select(r=>r.name+"（"+r.status+"）").ToArray(),Array.FindIndex(list,r=>r.cat_id==catId));var rows=Cat?.rows ?? new PythonDispatchComparisonRow[0];if(!Array.Exists(rows,r=>Key(r)==destinationKey))destinationKey=rows.FirstOrDefault()==null ? null : Key(rows[0]);
            Fill(destinations,rows.Select(r=>r.label+"（"+(r.can_select ? "派遣可能" : "条件を確認")+"）").ToArray(),Array.FindIndex(rows,r=>Key(r)==destinationKey));title.text=(Cat?.name ?? "猫")+"の派遣先比較";
            details.text=rows.Length==0 ? "この営業には比較できる派遣先がありません。" : string.Join("\n\n",rows.Select(r=>(Key(r)==destinationKey ? "▶ 選択中：" : "")+r.details));RefreshControls();
        }
        private void ScrollToSelection(){Canvas.ForceUpdateCanvases();var scroll=details.GetComponentInParent<ScrollRect>(true);int start=details.text.IndexOf("▶ 選択中：",StringComparison.Ordinal);float hidden=scroll.content.rect.height-scroll.viewport.rect.height;float offset=start<=0 ? 0 : details.cachedTextGeneratorForLayout.GetPreferredHeight(details.text.Substring(0,start),details.GetGenerationSettings(new Vector2(details.rectTransform.rect.width,0)))/details.pixelsPerUnit;scroll.verticalNormalizedPosition=hidden>0 ? 1-Mathf.Clamp01(offset/hidden) : 1;}
        public void RefreshControls(){bool ready=online && GetComponent<PythonStateClient>().CanManageSaves;cats.interactable=ready && (current?.dispatch_comparison?.cats?.Length ?? 0)>0;destinations.interactable=proceed.interactable=ready && Row!=null;close.interactable=true;notice.text=!online ? "未接続です。再接続後に派遣先の画面へ進めます。" : !ready ? "操作を送信しています。" : (Row==null ? "" : "選択："+Row.label+" / "+(Row.can_select ? "派遣可能" : Row.reason))+"\n"+(current?.dispatch_comparison?.notes ?? "");}
        public void Disconnect(){online=false;RefreshControls();}
    }
}
