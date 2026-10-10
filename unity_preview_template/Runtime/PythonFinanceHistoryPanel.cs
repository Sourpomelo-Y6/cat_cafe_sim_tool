using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonFinanceDay { public int day,presentation_version; public string label,overview,details,headline,breakdown,comparison; }
    [Serializable] public class PythonFinanceHistory { public string totals,notes; public PythonFinanceDay[] rows; }
    public class PythonFinanceHistoryPanel : MonoBehaviour
    {
        public GameObject panel;public Text title,details,notice,summary;
        public Button previous,next,close,entryPrefab;public Button[] modes,sections;public RectTransform listContent;
        private readonly List<Button> entries=new List<Button>();
        private PythonCafeState current;private string instance;private int selected,section;private bool online,business;
        public int SelectedDay=>selected;public int Section=>section;public bool BusinessMode=>business;
        private bool Ready=>online && GetComponent<PythonStateClient>().CanManageSaves;
        private PythonFinanceHistory History=>business ? current?.business_history : current?.finance_history;
        private PythonFinanceDay[] Rows=>History?.rows ?? new PythonFinanceDay[0];
        private PythonFinanceDay[] Days=>(current?.finance_history?.rows ?? new PythonFinanceDay[0]).Concat(current?.business_history?.rows ?? new PythonFinanceDay[0]).GroupBy(r=>r.day).Select(g=>g.First()).OrderBy(r=>r.day).ToArray();
        public void Apply(PythonCafeState state)
        {
            if(instance!=state.instance_id){instance=state.instance_id;selected=section=0;business=false;Close();}
            current=state;online=true;int old=selected;EnsureDay();Render();if(old!=selected)ResetScroll();
        }
        private void EnsureDay(){var days=Days;if(!days.Any(r=>r.day==selected))selected=days.LastOrDefault()?.day ?? 0;}
        public void Open(){if(!GetComponent<PythonStateClient>().CanManageSaves)return;business=false;Show();}
        public void OpenBusiness(){if(!GetComponent<PythonStateClient>().CanManageSaves)return;business=true;GetComponent<PythonMenuPanel>()?.Close();GetComponent<PythonEventListPanel>()?.Close();Show();}
        private void Show(){if(!Ready || current==null)return;EnsureDay();panel.SetActive(true);panel.transform.SetAsLastSibling();Render();ResetScroll();}
        public void Close(){panel.SetActive(false);}
        public void Mode(int value){if(!Ready || value<0 || value>1)return;business=value==1;Render();ResetScroll();}
        public void SelectSection(int value){if(!Ready || value<0 || value>2)return;section=value;Render();ResetScroll();}
        public void SelectDay(int day){if(!Ready || !Days.Any(r=>r.day==day))return;selected=day;Render();ResetScroll();}
        public void Previous(){Move(-1);}public void Next(){Move(1);}
        private void Move(int delta){var days=Days;int i=Array.FindIndex(days,r=>r.day==selected)+delta;if(!Ready || i<0 || i>=days.Length)return;SelectDay(days[i].day);}
        private void ResetScroll(){Canvas.ForceUpdateCanvases();details.GetComponentInParent<ScrollRect>(true).verticalNormalizedPosition=1;var s=notice.GetComponentInParent<ScrollRect>(true);if(s!=null)s.verticalNormalizedPosition=1;}
        private void Render()
        {
            var row=Array.Find(Rows,r=>r.day==selected);title.text="収支・営業成果の履歴";
            string empty=Days.Length==0 ? "まだ閉店した日の記録はありません。" : selected+"日目の"+(business ? "営業成果" : "収支")+"は記録がありません。別の種類・日付を確認してください。";
            if(summary!=null)summary.text=row==null ? empty : row.label+"\n"+(row.presentation_version>0 ? row.headline : row.overview);
            string body=row==null ? empty : row.presentation_version<1 ? "分類情報のない記録は全文を表示します。\n\n"+row.overview+"\n\n"+row.details
                : section==0 ? row.breakdown : section==1 ? row.comparison : History?.totals;
            details.text=(row?.label ?? "")+"\n\n"+(section==2 ? History?.totals ?? "集計できる記録なし" : body);
            if(row!=null && row.presentation_version<1 && section!=2)details.text=(History?.totals ?? "")+"\n\n"+details.text;
            if(listContent!=null && entryPrefab!=null)
            {
                var days=Days;while(entries.Count<days.Length){var b=Instantiate(entryPrefab,listContent);b.name="History Day "+entries.Count;entries.Add(b);}
                listContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Math.Max(350,days.Length*54));
                for(int i=0;i<entries.Count;i++)
                {
                    var b=entries[i];b.gameObject.SetActive(i<days.Length);if(i>=days.Length)continue;var day=days[i];b.GetComponent<RectTransform>().anchoredPosition=new Vector2(4,-i*54);b.GetComponentInChildren<Text>().text=day.label;b.image.color=day.day==selected ? new Color(.88f,.93f,.8f) : Color.white;b.onClick.RemoveAllListeners();int value=day.day;b.onClick.AddListener(()=>SelectDay(value));
                }
            }
            RefreshControls();
        }
        public void RefreshControls()
        {
            var days=Days;int i=Array.FindIndex(days,r=>r.day==selected);previous.interactable=Ready && i>0;next.interactable=Ready && i>=0 && i<days.Length-1;close.interactable=true;
            previous.GetComponentInChildren<Text>().text="前の記録";next.GetComponentInChildren<Text>().text="次の記録";
            if(modes!=null)for(int n=0;n<modes.Length;n++){modes[n].interactable=Ready;modes[n].image.color=(business ? 1 : 0)==n ? new Color(.88f,.93f,.8f) : Color.white;}
            if(sections!=null)for(int n=0;n<sections.Length;n++){sections[n].interactable=Ready;sections[n].image.color=section==n ? new Color(.88f,.93f,.8f) : Color.white;}
            foreach(var b in entries)b.interactable=Ready;
            notice.text=!online ? "未接続です。表示は最後に取得した記録です。" : !GetComponent<PythonStateClient>().CanManageSaves ? "操作の完了後に記録を切り替えられます。" : History?.notes ?? "記録はありません。";
        }
        public void Focus(){if(business && panel.activeSelf)panel.transform.SetAsLastSibling();}
        public void Disconnect(){online=false;RefreshControls();}
    }
}
