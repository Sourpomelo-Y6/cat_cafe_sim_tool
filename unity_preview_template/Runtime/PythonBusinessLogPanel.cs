using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonBusinessLogRow {public int index;public string label,details;}
    [Serializable] public class PythonBusinessLogView {public string instance_id,source,summary,notes,error,log_json;public int revision,offset,total;public PythonBusinessLogRow[] rows;}
    public class PythonBusinessLogPanel : MonoBehaviour
    {
        public GameObject panel;public Text title,details,notice;public InputField path;public Dropdown operations;
        public Button current,inspect,export,previous,next,close,summary;
        [NonSerialized] public string exportDirectory;
        private PythonBusinessLogView selected;private bool busy;private int generation,offset;private string source="",instance;private int revision=-1;
        private PythonStateClient Client=>GetComponent<PythonStateClient>();
        private bool Ready=>Client.CanManageSaves && !busy;
        public void Open(){if(!Client.CanManageSaves)return;Client.PauseAutomaticForSave();GetComponent<PythonMenuPanel>().Close();GetComponent<PythonEventListPanel>().Close();panel.SetActive(true);Focus();Current();}
        public void Close(){generation++;busy=false;panel.SetActive(false);}
        public void Current(){if(!Ready)return;source="";offset=int.MaxValue;StartCoroutine(Fetch(false));}
        public void Inspect(){if(!Ready || string.IsNullOrWhiteSpace(path.text))return;source=path.text.Trim().Trim('"');offset=0;StartCoroutine(Fetch(false));}
        public void Export(){if(Ready && source=="")StartCoroutine(Fetch(true));}
        public void Previous(){if(Ready && selected!=null && selected.offset>0){offset=selected.offset-20;StartCoroutine(Fetch(false));}}
        public void Next(){if(Ready && selected!=null && selected.offset+20<selected.total){offset=selected.offset+20;StartCoroutine(Fetch(false));}}
        public void Summary(){if(selected!=null){details.text=selected.summary+"\n\n"+selected.notes;ResetScroll();}}
        public void Select(int value){if(selected?.rows!=null && value>=0 && value<selected.rows.Length){details.text=selected.rows[value].details;ResetScroll();}}
        private void ResetScroll(){Canvas.ForceUpdateCanvases();details.GetComponentInParent<ScrollRect>(true).verticalNormalizedPosition=1;}
        private IEnumerator Fetch(bool output)
        {
            int ticket=++generation;busy=true;RefreshControls();notice.text=output ? "営業ログを出力しています。" : "営業ログを確認しています。";
            string route=output ? "/business-log/export" : "/business-log?offset="+offset+"&path="+UnityWebRequest.EscapeURL(source);
            using(var request=UnityWebRequest.Get("http://127.0.0.1:"+Client.ServerPort+route))
            {
                request.timeout=60;yield return request.SendWebRequest();if(ticket!=generation)yield break;busy=false;
                try
                {
                    var row=JsonUtility.FromJson<PythonBusinessLogView>(request.downloadHandler.text);
                    if(request.result!=UnityWebRequest.Result.Success)throw new Exception(row?.error ?? "接続とファイルのパスを確認してください。");
                    if(!Client.CanManageSaves || row.instance_id!=Client.CurrentState.instance_id || row.revision!=Client.CurrentState.revision)throw new Exception("営業状態が変わりました。現在のログを選び直してください。");
                    if(output){string file=WriteExport(row.log_json,exportDirectory);path.SetTextWithoutNotify(file);notice.text="出力しました：\n"+file;}
                    else AcceptView(row);
                }
                catch(Exception error){if(!output){selected=null;details.text="ログを確認できません。現在のログか保存済みログを選び直してください。";operations.ClearOptions();}notice.text=error.Message;}
                RefreshControls();
            }
        }
        public static string WriteExport(string json,string folder=null)
        {
            if(string.IsNullOrEmpty(json))throw new ArgumentException("出力する営業ログがありません。");
            folder=folder ?? Path.Combine(Application.persistentDataPath,"BusinessLogs");Directory.CreateDirectory(folder);string file=Path.Combine(folder,"cafe-log-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")+".json");
            using(var stream=new FileStream(file,FileMode.CreateNew,FileAccess.Write))using(var writer=new StreamWriter(stream,new UTF8Encoding(false)))writer.Write(json);return file;
        }
        public void AcceptView(PythonBusinessLogView row)
        {
            if(row==null || row.rows==null || string.IsNullOrEmpty(row.summary))throw new ArgumentException("営業ログの表示データが不正です。");
            selected=row;offset=row.offset;operations.ClearOptions();foreach(var item in row.rows)operations.options.Add(new Dropdown.OptionData(item.label));if(row.rows.Length==0)operations.options.Add(new Dropdown.OptionData("記録された操作はありません"));operations.SetValueWithoutNotify(0);operations.RefreshShownValue();if(row.rows.Length>0)Select(0);else Summary();notice.text=row.notes+" / "+(row.total==0 ? "0件" : (row.offset+1)+"〜"+(row.offset+row.rows.Length)+" / "+row.total+"件");RefreshControls();
        }
        public void Apply(PythonCafeState state)
        {
            if(instance!=state.instance_id){instance=state.instance_id;revision=state.revision;Close();selected=null;source="";offset=0;operations.ClearOptions();details.text="現在の営業ログか保存済みログを選んでください。";}
            else if(revision!=state.revision){revision=state.revision;if(panel.activeSelf && source==""){generation++;busy=false;selected=null;notice.text="営業状態が更新されました。「現在のログ」で最新の記録を確認してください。";}}
            RefreshControls();
        }
        public void RefreshControls(){bool ready=Ready;current.interactable=ready;inspect.interactable=ready && !string.IsNullOrWhiteSpace(path.text);export.interactable=ready && selected!=null && source=="";previous.interactable=ready && selected!=null && selected.offset>0;next.interactable=ready && selected!=null && selected.offset+20<selected.total;operations.interactable=ready && (selected?.rows?.Length ?? 0)>0;summary.interactable=ready && selected!=null;path.interactable=ready;close.interactable=true;}
        public void PathChanged(string value){RefreshControls();}
        public void Focus(){if(panel.activeSelf)panel.transform.SetAsLastSibling();}
        public void Disconnect(){generation++;busy=false;selected=null;notice.text="未接続です。再接続後にログを選び直してください。";RefreshControls();}
    }
}
