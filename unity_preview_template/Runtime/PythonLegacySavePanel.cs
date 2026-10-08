using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonLegacySavePreview { public string save_id,source_path,details,instance_id,error; public int revision; }
    public class PythonLegacySavePanel : MonoBehaviour
    {
        public GameObject panel;
        public InputField path;
        public Text details,notice;
        public Button inspect,resume,close,entry;
        private PythonLegacySavePreview selected;
        private bool inspecting;
        private int generation;
        private PythonStateClient Client => GetComponent<PythonStateClient>();
        private PythonTitleScreen Title => GetComponent<PythonTitleScreen>();
        private PythonCafeState State => Client!=null ? Client.CurrentState : Title.CurrentState;
        private bool Ready => Client!=null ? Client.CanManageSaves : Title.CanManageSaves;
        private int Port => Client!=null ? Client.ServerPort : Title.port;
        public void Open(){if(!Ready)return;Client?.PauseAutomaticForSave();Invalidate();panel.SetActive(true);panel.transform.SetAsLastSibling();notice.text="Python版の営業セーブ（cafe.jsonなど）の絶対パスを入力し、内容を確認してください。";RefreshControls();}
        public void Close(){Invalidate();panel.SetActive(false);}
        private void Invalidate(){generation++;inspecting=false;selected=null;details.text="内容を確認してから再開できます。元のセーブと関係データは変更しません。";}
        public void PathChanged(string value){Invalidate();notice.text="パスを変更しました。内容を再確認してください。";RefreshControls();}
        public void Inspect(){if(!Ready || inspecting || string.IsNullOrWhiteSpace(path.text))return;StartCoroutine(ReadPreview());}
        private IEnumerator ReadPreview()
        {
            Invalidate();int ticket=generation;inspecting=true;notice.text="保存内容を確認しています。";RefreshControls();
            using(var request=UnityWebRequest.Get("http://127.0.0.1:"+Port+"/python-save?path="+UnityWebRequest.EscapeURL(path.text.Trim().Trim('"'))))
            {
                request.timeout=30;yield return request.SendWebRequest();
                if(ticket!=generation)yield break;
                inspecting=false;
                try
                {
                    var row=JsonUtility.FromJson<PythonLegacySavePreview>(request.downloadHandler.text);
                    if(request.result!=UnityWebRequest.Result.Success)throw new Exception(!string.IsNullOrEmpty(row?.error) ? row.error : "内容を確認できません。接続とパスを確認してください。");
                    AcceptPreview(row);
                }
                catch(Exception ex){selected=null;notice.text=ex.Message;}
                RefreshControls();
            }
        }
        public void AcceptPreview(PythonLegacySavePreview row)
        {
            if(row==null || string.IsNullOrEmpty(row.save_id) || string.IsNullOrEmpty(row.details) || !Ready || State.instance_id!=row.instance_id || State.revision!=row.revision)throw new ArgumentException("状態が変わりました。内容を再確認してください。");
            selected=row;details.text="元のファイル："+row.source_path+"\n\n"+row.details;notice.text="内容を確認し、このセーブで再開してください。現在の未保存の進捗は置き換わります。";
            Canvas.ForceUpdateCanvases();details.GetComponentInParent<ScrollRect>().verticalNormalizedPosition=1;RefreshControls();
        }
        public void Resume()
        {
            if(!Ready || selected==null || State.instance_id!=selected.instance_id || State.revision!=selected.revision)return;
            string token=selected.save_id;selected=null;notice.text="保存したゲームを読み込んでいます。";
            if(Client!=null)Client.LoadPythonSaved(token);else Title.ResumePython(token);RefreshControls();
        }
        public void Apply(PythonCafeState state)
        {
            if(selected!=null && (selected.instance_id!=state.instance_id || selected.revision!=state.revision)){Invalidate();notice.text="状態が変わりました。内容を再確認してください。";}
            RefreshControls();
        }
        public void Notify(string message){if(panel.activeSelf && !string.IsNullOrEmpty(message))notice.text=message;}
        public void RefreshControls(){bool ready=Ready && !inspecting;entry.interactable=Ready;inspect.interactable=ready && !string.IsNullOrWhiteSpace(path.text);path.interactable=ready;resume.interactable=ready && selected!=null;close.interactable=true;}
        public void Disconnect(){Invalidate();notice.text="未接続です。接続後に保存内容を再確認してください。";RefreshControls();}
    }
}
