using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonCustomerMatch { public string cat_id,details,name; public bool adopted,matches; public float affinity,tension_multiplier; }
    [Serializable] public class PythonDirectoryCustomer { public string customer_id,name,description,preference_text,today,tomorrow,details; public int visits,presentation_version; public string overview_details,status_details,condition_details; public bool planned_today,planned_tomorrow,needs_attention; public PythonCustomerMatch[] cats; }
    [Serializable] public class PythonCustomerDirectory { public string today_label,tomorrow_label,notes; public PythonDirectoryCustomer[] rows; }
    public class PythonCustomerDirectoryPanel : MonoBehaviour
    {
        public GameObject panel;
        public Text title,details,notice;
        public Button today,tomorrow,adopted,close,entryPrefab;
        public RectTransform listContent;
        public Button[] filters,tabs;public Button sort;
        private int filter,section,sortMode;
        private PythonDirectoryCustomer[] visible=new PythonDirectoryCustomer[0];
        public int VisibleCount=>visible.Length;public int Filter=>filter;public int Section=>section;
        private readonly List<Button> entries=new List<Button>();
        private PythonCafeState current;
        private string instance,selected;
        private bool online,showTomorrow,showAdopted;
        private PythonCustomerDirectory Data => current?.customer_directory;
        public string SelectedCustomer => selected;
        public void Open(){if(Data==null || !GetComponent<PythonStateClient>().CanManageSaves)return;panel.SetActive(true);panel.transform.SetAsLastSibling();Render();}
        public void Close(){panel.SetActive(false);}
        private bool Available=>online && Data!=null && GetComponent<PythonStateClient>().CanManageSaves;
        public void FilterBy(int value){if(!Available || value<0 || value>2)return;filter=value;Render();ResetScroll();}
        public void SectionBy(int value){if(!Available || value<0 || value>3)return;section=value;RenderDetails();RefreshControls();ResetDetailsScroll();}
        public void CycleSort(){if(!Available)return;sortMode=(sortMode+1)%3;RenderDetails();RefreshControls();ResetDetailsScroll();}
        private void ResetDetailsScroll(){Canvas.ForceUpdateCanvases();details.GetComponentInParent<ScrollRect>(true).verticalNormalizedPosition=1;}
        private void ResetScroll(){Canvas.ForceUpdateCanvases();listContent.GetComponentInParent<ScrollRect>(true).verticalNormalizedPosition=1;ResetDetailsScroll();}
        public void Today(){if(!Available)return;showTomorrow=false;Render();ResetScroll();}
        public void Tomorrow(){if(!Available)return;showTomorrow=true;Render();ResetScroll();}
        public void ToggleAdopted(){if(!Available)return;showAdopted=!showAdopted;RenderDetails();RefreshControls();}
        public void Select(string id){if(!Available || !visible.Any(r=>r.customer_id==id))return;selected=id;RenderDetails();Canvas.ForceUpdateCanvases();details.transform.parent.parent.GetComponent<ScrollRect>().verticalNormalizedPosition=1;}
        public void Apply(PythonCafeState state)
        {
            if(instance!=state.instance_id){instance=state.instance_id;selected=null;filter=section=sortMode=0;showTomorrow=showAdopted=false;Close();}
            current=state;online=true;if(Data==null)Close();Render();
        }
        private void Render()
        {
            title.text="お客さんの名簿・来店予定";
            today.GetComponentInChildren<Text>().text="今日："+(Data?.today_label ?? "—");tomorrow.GetComponentInChildren<Text>().text="翌日："+(Data?.tomorrow_label ?? "—");
            string previous=selected;var all=Data?.rows ?? new PythonDirectoryCustomer[0];
            visible=all.Where(r=>filter==0 || r.presentation_version>0 && (filter==1 ? (showTomorrow ? r.planned_tomorrow : r.planned_today) : r.needs_attention)).ToArray();
            var rows=visible;if(!rows.Any(r=>r.customer_id==selected))selected=rows.FirstOrDefault()?.customer_id;
            title.text+="（表示 "+rows.Length+" / 全 "+all.Length+"人）";
            while(entries.Count<rows.Length){var b=Instantiate(entryPrefab,listContent);b.name="Customer "+entries.Count;entries.Add(b);}
            listContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Math.Max(340,rows.Length*92));
            for(int i=0;i<entries.Count;i++)
            {
                var b=entries[i];b.gameObject.SetActive(i<rows.Length);if(i>=rows.Length)continue;
                var rect=b.GetComponent<RectTransform>();rect.anchoredPosition=new Vector2(4,-i*92);var row=rows[i];
                b.GetComponentInChildren<Text>().text=row.name+" / "+row.visits+"回来店\n"+row.preference_text+"\n"+(showTomorrow ? row.tomorrow : row.today);
                b.onClick.RemoveAllListeners();string id=row.customer_id;b.onClick.AddListener(()=>Select(id));
            }
            RenderDetails();RefreshControls();if(previous!=selected)ResetDetailsScroll();
        }
        private void RenderDetails()
        {
            var rows=visible;for(int i=0;i<entries.Count;i++)entries[i].image.color=i<rows.Length && rows[i].customer_id==selected ? new Color(.88f,.93f,.8f) : Color.white;
            var row=Data?.rows?.FirstOrDefault(r=>r.customer_id==selected);
            string text=(Data?.rows?.Length ?? 0)==0 ? "表示できる顧客の記録はありません。" : "該当するお客さんはいません。表示条件を変えてください。";
            if(row!=null)
            {
                string body=row.presentation_version<1 ? "分類情報がないため、既存の詳細を表示します。\n\n"+row.details
                    : section==0 ? row.overview_details+"\n\n"+Data.notes
                    : section==1 ? row.preference_text+"\n\n"+row.condition_details
                    : section==2 ? row.status_details : "好み一致は通常テンションの倍率です。健康・予定も確認してください。";
                text=row.name+"\n\n"+body;
                if(section==3 || row.presentation_version<1)
                {
                    IEnumerable<PythonCustomerMatch> cats=(row.cats ?? new PythonCustomerMatch[0]).Where(r=>showAdopted || !r.adopted);
                    if(row.presentation_version>0 && sortMode==1)cats=cats.OrderByDescending(r=>r.matches).ThenByDescending(r=>r.affinity);
                    if(row.presentation_version>0 && sortMode==2)cats=cats.OrderByDescending(r=>r.affinity);
                    text+="\n\n猫ごとの親しみ・相性\n\n"+(cats.Any() ? string.Join("\n\n",cats.Select(r=>r.details)) : "表示対象の猫はいません。");
                }
            }
            details.text=text;details.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Math.Max(340,details.preferredHeight+12));
        }
        public void RefreshControls()
        {
            bool available=Available;
            if(filters!=null)for(int i=0;i<filters.Length;i++){filters[i].interactable=available;filters[i].image.color=filter==i ? new Color(.88f,.93f,.8f) : Color.white;}
            if(tabs!=null)for(int i=0;i<tabs.Length;i++){tabs[i].interactable=available;tabs[i].image.color=section==i ? new Color(.88f,.93f,.8f) : Color.white;}
            if(sort!=null){sort.gameObject.SetActive(section==3);sort.interactable=available && (Data.rows ?? new PythonDirectoryCustomer[0]).Any(r=>r.customer_id==selected && r.presentation_version>0);sort.GetComponentInChildren<Text>().text="並び："+new[]{"登録順","好み一致→親しみ","親しみ順"}[sortMode];}
            today.interactable=available && showTomorrow;tomorrow.interactable=available && !showTomorrow;adopted.interactable=available;adopted.gameObject.SetActive(section==3 || Data?.rows?.Any(r=>r.customer_id==selected && r.presentation_version<1)==true);close.interactable=true;
            adopted.GetComponentInChildren<Text>().text="譲渡済みの猫："+(showAdopted ? "表示" : "非表示");
            foreach(var b in entries)b.interactable=available;
            notice.text=!online ? "Python未接続です。表示は最後に取得した状態です。" : !available ? "操作の完了後に名簿を更新します。" : filter>0 && (Data.rows ?? new PythonDirectoryCustomer[0]).Any(r=>r.presentation_version<1) ? "分類情報のない旧接続は「全員」で確認できます。" : "閲覧操作では営業を進めません。休業日は来店しません。";
        }
        public void Disconnect(){online=false;RefreshControls();}
    }
}
