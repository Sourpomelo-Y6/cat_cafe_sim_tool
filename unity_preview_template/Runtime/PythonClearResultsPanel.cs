using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonClearRecord { public string mode,label,details,notes; }
    [Serializable] public class PythonClearResults { public PythonClearRecord[] rows;public string notes; }
    public class PythonClearResultsPanel : MonoBehaviour
    {
        public GameObject panel;public Text title,details,notice;public Dropdown goals;public Button close;
        private PythonCafeState current;private string instance,selected;private bool online;
        private PythonClearRecord[] Rows=>current?.clear_results?.rows ?? new PythonClearRecord[0];
        public void Apply(PythonCafeState state)
        {
            if(instance!=state.instance_id){instance=state.instance_id;selected=null;Close();}
            current=state;online=true;Render();
        }
        public void Open()
        {
            if(current==null || !GetComponent<PythonStateClient>().CanManageSaves)return;
            GetComponent<PythonStateClient>().PauseAutomaticForSave();
            GetComponent<PythonMenuPanel>()?.Close();GetComponent<PythonEventListPanel>()?.Close();
            panel.SetActive(true);Render();ResetScroll();Focus();
        }
        public void Close(){if(panel!=null)panel.SetActive(false);}
        public void Select(int index)
        {
            if(!online || index<0 || index>=Rows.Length)return;
            selected=Rows[index].mode;Render();ResetScroll();
        }
        private void Render()
        {
            goals.ClearOptions();foreach(var row in Rows)goals.options.Add(new Dropdown.OptionData(row.label));
            int index=Array.FindIndex(Rows,row=>row.mode==selected);if(index<0)index=0;
            selected=Rows.Length>0 ? Rows[index].mode : null;
            if(Rows.Length==0)goals.options.Add(new Dropdown.OptionData("達成済みの目標はありません"));
            goals.SetValueWithoutNotify(index);goals.RefreshShownValue();
            title.text="クリア記録";
            details.text=Rows.Length==0 ? "クリア記録はまだありません。\n\n"+(current?.clear_results?.notes ?? "達成済み目標の記録をここで見返せます。") : Rows[index].label+" クリア！\n\n"+Rows[index].details;
            RefreshControls();
        }
        private void ResetScroll(){Canvas.ForceUpdateCanvases();details.GetComponentInParent<ScrollRect>(true).verticalNormalizedPosition=1;}
        public void RefreshControls()
        {
            goals.interactable=online && GetComponent<PythonStateClient>().CanManageSaves && Rows.Length>0;close.interactable=true;
            var row=Array.Find(Rows,item=>item.mode==selected);
            notice.text=!online ? "未接続です。表示は最後に取得した記録です。" : row?.notes ?? current?.clear_results?.notes ?? "";
        }
        public void Focus(){if(panel!=null && panel.activeSelf)panel.transform.SetAsLastSibling();}
        public void Disconnect(){online=false;RefreshControls();}
    }
}
