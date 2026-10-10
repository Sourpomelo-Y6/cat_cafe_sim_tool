using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonClosingReview : MonoBehaviour
    {
        public Button[] tabs;public Dropdown cats;public Button catDetails;private int tab;private string selected,instance;
        private PythonClosingPanel Review=>GetComponent<PythonClosingPanel>();
        public int SelectedTab=>tab;
        public string SelectedCatId=>selected;
        public void Goal(){SelectTab(0);}public void Business(){SelectTab(1);}public void Finance(){SelectTab(2);}
        void SelectTab(int value){tab=value;Refresh();Canvas.ForceUpdateCanvases();Review.goalScroll.verticalNormalizedPosition=1;}
        public void SelectCat(int index){var rows=Review.CurrentState?.cats ?? new PythonCatState[0];if(index>=0 && index<rows.Length)selected=rows[index].cat_id;Refresh();}
        public void OpenCat(){if(catDetails.interactable)GetComponent<PythonCatDetailsPanel>().Open(selected);}
        public void Refresh()
        {
            var state=Review.CurrentState;if(state==null || cats==null)return;
            if(instance!=state.instance_id){instance=state.instance_id;selected=null;tab=0;}
            var rows=state.cats ?? new PythonCatState[0];int index=Array.FindIndex(rows,c=>c.cat_id==selected);if(index<0)index=0;selected=rows.Length>0 ? rows[index].cat_id : null;
            cats.options.Clear();foreach(var cat in rows)cats.options.Add(new Dropdown.OptionData(cat.name));cats.SetValueWithoutNotify(index);cats.RefreshShownValue();
            bool ready=Review.Online && GetComponent<PythonStateClient>().CanManageSaves;cats.interactable=rows.Length>0;catDetails.interactable=ready && selected!=null;
            var history=tab==1 ? state.business_history : state.finance_history;var today=Array.Find(history?.rows ?? new PythonFinanceDay[0],row=>row.day==state.day);
            if(tab==0)Review.goalDetails.text=(state.objective_progress?.label ?? "経営目標")+"\n\n"+(state.objective_progress?.details ?? "目標情報を取得していません。");
            else Review.goalDetails.text=today==null ? (tab==1 ? "今日の出来事" : "収支・前日比較")+"\n\nこの日の確定記録はありません。" : today.label+"\n"+today.overview+"\n\n"+today.details;
            for(int i=0;i<tabs.Length;i++)tabs[i].GetComponent<Image>().color=i==tab ? new Color(.76f,.87f,.73f) : Color.white;
        }
    }
}
