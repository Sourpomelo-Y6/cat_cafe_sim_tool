using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonCustomerMatch { public string cat_id,details; public bool adopted; }
    [Serializable] public class PythonDirectoryCustomer { public string customer_id,name,description,preference_text,today,tomorrow,details; public int visits; public PythonCustomerMatch[] cats; }
    [Serializable] public class PythonCustomerDirectory { public string today_label,tomorrow_label,notes; public PythonDirectoryCustomer[] rows; }
    public class PythonCustomerDirectoryPanel : MonoBehaviour
    {
        public GameObject panel;
        public Text title,details,notice;
        public Button today,tomorrow,adopted,close,entryPrefab;
        public RectTransform listContent;
        private readonly List<Button> entries=new List<Button>();
        private PythonCafeState current;
        private string instance,selected;
        private bool online,showTomorrow,showAdopted;
        private PythonCustomerDirectory Data => current?.customer_directory;
        public string SelectedCustomer => selected;
        public void Open(){if(Data==null || !GetComponent<PythonStateClient>().CanManageSaves)return;panel.SetActive(true);panel.transform.SetAsLastSibling();Render();}
        public void Close(){panel.SetActive(false);}
        public void Today(){showTomorrow=false;Render();}
        public void Tomorrow(){showTomorrow=true;Render();}
        public void ToggleAdopted(){showAdopted=!showAdopted;RenderDetails();RefreshControls();}
        public void Select(string id){if(Data?.rows?.Any(r=>r.customer_id==id)!=true)return;selected=id;RenderDetails();Canvas.ForceUpdateCanvases();details.transform.parent.parent.GetComponent<ScrollRect>().verticalNormalizedPosition=1;}
        public void Apply(PythonCafeState state)
        {
            if(instance!=state.instance_id){instance=state.instance_id;selected=null;showTomorrow=showAdopted=false;Close();}
            current=state;online=true;if(Data==null)Close();if(Data?.rows?.Any(r=>r.customer_id==selected)!=true)selected=Data?.rows?.FirstOrDefault()?.customer_id;Render();
        }
        private void Render()
        {
            title.text="お客さんの名簿・来店予定";
            today.GetComponentInChildren<Text>().text="今日："+(Data?.today_label ?? "—");tomorrow.GetComponentInChildren<Text>().text="翌日："+(Data?.tomorrow_label ?? "—");
            var rows=Data?.rows ?? new PythonDirectoryCustomer[0];
            while(entries.Count<rows.Length){var b=Instantiate(entryPrefab,listContent);b.name="Customer "+entries.Count;entries.Add(b);}
            listContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Math.Max(340,rows.Length*92));
            for(int i=0;i<entries.Count;i++)
            {
                var b=entries[i];b.gameObject.SetActive(i<rows.Length);if(i>=rows.Length)continue;
                var rect=b.GetComponent<RectTransform>();rect.anchoredPosition=new Vector2(4,-i*92);var row=rows[i];
                b.GetComponentInChildren<Text>().text=row.name+" / "+row.visits+"回来店\n"+row.preference_text+"\n"+(showTomorrow ? row.tomorrow : row.today);
                b.onClick.RemoveAllListeners();string id=row.customer_id;b.onClick.AddListener(()=>Select(id));
            }
            RenderDetails();RefreshControls();
        }
        private void RenderDetails()
        {
            var rows=Data?.rows ?? new PythonDirectoryCustomer[0];for(int i=0;i<entries.Count;i++)entries[i].image.color=i<rows.Length && rows[i].customer_id==selected ? new Color(.88f,.93f,.8f) : Color.white;
            var row=Data?.rows?.FirstOrDefault(r=>r.customer_id==selected);
            string text=row==null ? "表示できる顧客の記録はありません。" : (Data.notes ?? "")+"\n\n"+row.details+"\n\n猫ごとの親しみ・相性\n\n";
            if(row!=null){var cats=(row.cats ?? new PythonCustomerMatch[0]).Where(r=>showAdopted || !r.adopted).ToArray();text+=cats.Length==0 ? "表示対象の猫はいません。" : string.Join("\n\n",cats.Select(r=>r.details));}
            details.text=text;details.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Math.Max(340,details.preferredHeight+12));
        }
        public void RefreshControls()
        {
            bool available=online && Data!=null && GetComponent<PythonStateClient>().CanManageSaves;
            today.interactable=available && showTomorrow;tomorrow.interactable=available && !showTomorrow;adopted.interactable=available;close.interactable=true;
            adopted.GetComponentInChildren<Text>().text="譲渡済みの猫："+(showAdopted ? "表示" : "非表示");
            foreach(var b in entries)b.interactable=available;
            notice.text=!online ? "Python未接続です。表示は最後に取得した状態です。" : !available ? "操作の完了後に名簿を更新します。" : "顧客を選ぶと条件・状態・猫との相性を表示します。閲覧で営業は進みません。";
        }
        public void Disconnect(){online=false;RefreshControls();}
    }
}
