using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonSaveBrowser : MonoBehaviour
    {
        public GameObject panel,confirmation;
        public Text title,details,notice,pageLabel,confirmationText,current;
        public Button saveTab,loadTab,action,close,previous,next,confirm,cancel;
        public Button[] cards;
        private PythonSavedGame[] entries=new PythonSavedGame[0];
        private bool saving,available;
        private int page;
        private string selected="slot:1",identity,chosenId,chosenStamp,confirmationIdentity;
        private int chosenSlot;
        private PythonStateClient Client=>GetComponent<PythonStateClient>();
        private PythonTitleScreen Title=>GetComponent<PythonTitleScreen>();
        private PythonCafeState State=>Client!=null?Client.CurrentState:Title.CurrentState;
        private bool Ready=>Client!=null?Client.CanManageSaves:Title.CanManageSaves;
        private int Total=>10+entries.Count(e=>e.slot<1 || e.slot>10);
        private string Key(int index)=>index<10?"slot:"+(index+1):entries.Where(e=>e.slot<1 || e.slot>10).ElementAt(index-10).save_id;
        private PythonSavedGame Row(string key)=>key.StartsWith("slot:")?Array.Find(entries,e=>e.slot.ToString()==key.Substring(5)):Array.Find(entries,e=>e.save_id==key);
        public void OpenSave(){Open(true);}
        public void OpenLoad(){Open(false);}
        void Open(bool save)
        {
            if(Client!=null){Client.PauseAutomaticForSave();GetComponent<PythonSavePanel>().Open();}
            else Title.ShowSaves();
            saving=save && Client!=null;Cancel();if(saving && !selected.StartsWith("slot:"))selected="slot:1";
            panel.SetActive(true);panel.transform.SetAsLastSibling();notice.text="スロットを選び、内容を確認してください。";Refresh();
        }
        public void SaveMode(){saving=Client!=null;Cancel();Refresh();}
        public void LoadMode(){saving=false;Cancel();Refresh();}
        public void Close(){Cancel();if(Client!=null)GetComponent<PythonSavePanel>().Close();else Title.ShowHome();}
        public void Select(int index){int n=page*cards.Length+index;if(n<0 || n>=Total)return;selected=Key(n);Cancel();Refresh();}
        public void Previous(){if(page>0){page--;Cancel();Refresh();}}
        public void Next(){if((page+1)*cards.Length<Total){page++;Cancel();Refresh();}}
        public void Apply(PythonSaveList list)
        {
            if(!available && panel.activeSelf)notice.text="保存一覧を更新しました。スロットを選んでください。";entries=list.saves;available=true;page=Math.Min(page,(Total-1)/cards.Length);
            if(!selected.StartsWith("slot:") && Row(selected)==null)selected="slot:1";
            if(confirmation.activeSelf && (Row(selected)?.save_id!=chosenId || Row(selected)?.created_at!=chosenStamp))Cancel();Refresh();
        }
        public void Notify(string message){if(!string.IsNullOrEmpty(message))notice.text=message;}
        public void Unavailable(){available=false;Cancel();notice.text="保存一覧を取得できません。再接続後に確認してください。";Refresh();}
        public void Choose()
        {
            if(!action.interactable)return;var row=Row(selected);chosenId=row?.save_id;chosenStamp=row?.created_at;chosenSlot=selected.StartsWith("slot:")?int.Parse(selected.Substring(5)):0;
            confirmationIdentity=State.instance_id+"/"+State.revision;confirmationText.text=saving?(row==null?"スロット "+chosenSlot+" にセーブしますか？":"スロット "+chosenSlot+" のデータを上書きしますか？\n\n"+row.Details()):"このデータをロードしますか？\n\n"+row.Details()+"\n\n現在の未保存の進捗は置き換わります。";
            if(saving)confirmationText.text+="\n\n現在："+State.day+"日目 / 資金 "+State.funds.ToString("0.#");
            confirmation.SetActive(true);confirmation.transform.SetAsLastSibling();Refresh();
        }
        public void Cancel(){chosenId=chosenStamp=confirmationIdentity=null;chosenSlot=0;if(confirmation!=null)confirmation.SetActive(false);}
        public void CancelAndRefresh(){Cancel();Refresh();}
        public void Confirm()
        {
            if(!Ready || !available || !confirmation.activeSelf || confirmationIdentity!=State.instance_id+"/"+State.revision)return;
            if(Row(selected)?.save_id!=chosenId || Row(selected)?.created_at!=chosenStamp){Cancel();Refresh();return;}
            string id=chosenId;int slot=chosenSlot;Cancel();
            if(saving){Client.SaveToSlot(slot);notice.text="セーブしています…";}
            else{if(Client!=null)Client.LoadSaved(id);else Title.ResumeSaved(id);notice.text="ロードしています…";}
            Refresh();
        }
        public void Refresh()
        {
            if(cards==null || cards.Length==0 || State==null)return;
            string stamp=State.instance_id+"/"+State.revision;if(identity!=stamp || !Ready){identity=stamp;Cancel();}
            title.text=saving?"セーブ":"ロード";bool choosing=confirmation.activeSelf;bool ready=Ready && available && !choosing;
            current.text=Client==null?"保存したカフェを選んで再開します":"現在："+State.day+"日目 / 資金 "+State.funds.ToString("0.#");
            for(int i=0;i<cards.Length;i++)
            {
                int index=page*cards.Length+i;var b=cards[i];b.gameObject.SetActive(index<Total);if(index>=Total)continue;string key=Key(index);var row=Row(key);
                string heading=index<10?"SLOT "+(index+1).ToString("00"):"以前の保存データ";DateTimeOffset date;string saved=row==null?"空のスロット":DateTimeOffset.TryParse(row.created_at,out date)?date.ToLocalTime().ToString("yyyy/MM/dd HH:mm"):"保存日時不明";
                b.GetComponentInChildren<Text>().text=heading+"　"+saved+"\n"+(row?.label??(Client!=null?"ここに新しくセーブできます":"保存データがありません"));b.GetComponent<Image>().color=key==selected?new Color(.76f,.87f,.73f):new Color(.94f,.94f,.90f);b.interactable=!choosing;
            }
            var selectedRow=Row(selected);details.text=selectedRow==null?"SLOT "+selected.Substring(5)+"\n\n空のスロットです。\n"+(Client!=null?"セーブを選ぶと現在の進捗を保存できます。":"このスロットには保存データがありません。")+"":(selected.StartsWith("slot:")?"SLOT "+selected.Substring(5):"以前の保存データ")+"\n\n"+selectedRow.Details();
            action.GetComponentInChildren<Text>().text=saving?selectedRow==null?"セーブする":"上書きする":"ロードする";action.interactable=ready && (saving?selected.StartsWith("slot:"):selectedRow!=null);
            saveTab.gameObject.SetActive(Client!=null);loadTab.gameObject.SetActive(Client!=null);saveTab.interactable=loadTab.interactable=!choosing;
            saveTab.GetComponent<Image>().color=saving?new Color(.76f,.87f,.73f):Color.white;loadTab.GetComponent<Image>().color=!saving?new Color(.76f,.87f,.73f):Color.white;
            previous.interactable=!choosing && page>0;next.interactable=!choosing && (page+1)*cards.Length<Total;pageLabel.text=(page+1)+" / "+((Total+cards.Length-1)/cards.Length);confirm.interactable=Ready && available && choosing;cancel.interactable=close.interactable=true;
        }
    }
}
