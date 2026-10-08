using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonTitleScreen : MonoBehaviour
    {
        public GameObject home, conditionsPanel, savesPanel;
        public Dropdown objectives, saves;
        public Text conditions, notice, saveDetails;
        private bool commandFailed;
        public void SelectSave(int index) { if(saveDetails!=null)saveDetails.text=index>=0 && index<entries.Length ? entries[index].Details() : "保存したゲームはありません。"; }
        public Button newGame, continueGame, startGame, resume;
        public int port=8190;
        public PythonCafeState CurrentState => state;
        public bool CanManageSaves => online && !busy && state!=null;
        public void ResumePython(string token){if(CanManageSaves)StartCoroutine(Send("load_python_game","",token));}
        private PythonCafeState state;
        private PythonSavedGame[] entries=new PythonSavedGame[0];
        private bool busy, online, savesAvailable;
        public void ShowHome() { home.SetActive(true);conditionsPanel.SetActive(false);savesPanel.SetActive(false); }
        public void ShowConditions() { home.SetActive(false);conditionsPanel.SetActive(true);savesPanel.SetActive(false);SelectObjective(objectives.value); }
        public void ShowSaves() { home.SetActive(false);conditionsPanel.SetActive(false);savesPanel.SetActive(true);SelectSave(saves.value); }
        public void SelectObjective(int index)
        {
            if(state==null || state.new_game_options==null || index<0 || index>=state.new_game_options.Length)return;
            var option=state.new_game_options[index];conditions.text=option.label+"\n\n"+option.detail+"\n\n開始条件\n"+option.conditions;
        }
        public void RefreshControls()
        {
            bool ready=online && !busy;
            newGame.interactable=startGame.interactable=objectives.interactable=ready && state.new_game_options!=null && state.new_game_options.Length>0;
            continueGame.interactable=ready;
            saves.interactable=resume.interactable=ready && savesAvailable && entries.Length>0;
            GetComponent<PythonLegacySavePanel>()?.RefreshControls();
        }
        public void Apply(PythonCafeState latest)
        {
            PythonStateClient.Validate(latest);string previous=state!=null && state.new_game_options!=null && objectives.value>=0 && objectives.value<state.new_game_options.Length ? state.new_game_options[objectives.value].choice : "";
            state=latest;online=true;GetComponent<PythonLegacySavePanel>()?.Apply(latest);objectives.options.Clear();
            foreach(var row in latest.new_game_options ?? new PythonNewGameOption[0])objectives.options.Add(new Dropdown.OptionData(row.label));
            objectives.SetValueWithoutNotify(Math.Max(0,Array.FindIndex(latest.new_game_options ?? new PythonNewGameOption[0],row=>row.choice==previous)));
            objectives.RefreshShownValue();SelectObjective(objectives.value);RefreshControls();
        }
        public void ApplySaves(PythonSaveList list)
        {
            if(list==null || list.saves==null)throw new ArgumentException("保存一覧を取得できません。");
            string previous=entries.Length>0 ? entries[Math.Max(0,Math.Min(saves.value,entries.Length-1))].save_id : "";
            entries=list.saves;savesAvailable=true;saves.options.Clear();
            foreach(var row in entries)saves.options.Add(new Dropdown.OptionData(row.label));
            if(entries.Length==0)saves.options.Add(new Dropdown.OptionData("保存したゲームはありません"));
            saves.SetValueWithoutNotify(Math.Max(0,Array.FindIndex(entries,row=>row.save_id==previous)));saves.RefreshShownValue();SelectSave(saves.value);RefreshControls();
        }
        public void Disconnect(string message) {online=false;GetComponent<PythonLegacySavePanel>()?.Disconnect();notice.text=message;RefreshControls();}
        private IEnumerator Start()
        {
            ShowHome();notice.text="接続しています。";RefreshControls();
            while(true)
            {
                if(!busy)
                {
                    using(var request=UnityWebRequest.Get("http://127.0.0.1:"+port+"/state"))
                    {
                        request.timeout=3;yield return request.SendWebRequest();
                        if(busy)continue;
                        if(request.result!=UnityWebRequest.Result.Success)Disconnect("Python未接続です。状態サーバーを起動してください。");
                        else {try{Apply(JsonUtility.FromJson<PythonCafeState>(request.downloadHandler.text));if(!commandFailed)notice.text="目標を選んではじめるか、保存したゲームを再開してください。";}catch(Exception ex){Disconnect(ex.Message);}}
                    }
                    if(online && !busy)using(var request=UnityWebRequest.Get("http://127.0.0.1:"+port+"/saves"))
                    {
                        request.timeout=3;yield return request.SendWebRequest();if(busy)continue;
                        try {if(request.result!=UnityWebRequest.Result.Success)throw new Exception("保存一覧を取得できません。");ApplySaves(JsonUtility.FromJson<PythonSaveList>(request.downloadHandler.text));}
                        catch(Exception ex){savesAvailable=false;if(saveDetails!=null)saveDetails.text="保存一覧を取得できません。接続を確認してください。";notice.text=ex.Message;RefreshControls();}
                    }
                }
                yield return new WaitForSecondsRealtime(2);
            }
        }
        public void StartNewGame() {if(startGame.interactable)StartCoroutine(Send("new_game",state.new_game_options[objectives.value].choice,""));}
        public void Resume() {if(resume.interactable)StartCoroutine(Send("load_game","",entries[saves.value].save_id));}
        private IEnumerator Send(string kind,string choice,string saveId)
        {
            busy=true;commandFailed=false;RefreshControls();notice.text=kind=="load_game" || kind=="load_python_game" ? "保存したゲームを読み込んでいます。" : "新しいゲームを準備しています。";
            var command=new PythonShiftCommand{request_id=Guid.NewGuid().ToString("N"),instance_id=state.instance_id,expected_revision=state.revision,
                kind=kind,choice=choice,save_id=saveId,working_cats=new string[0]};
            for(int attempt=0;attempt<2;attempt++)
            {
                using(var request=new UnityWebRequest("http://127.0.0.1:"+port+"/commands","POST"))
                {
                    request.uploadHandler=new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(command)));request.downloadHandler=new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type","application/json");request.timeout=5;yield return request.SendWebRequest();
                    if(request.result==UnityWebRequest.Result.ConnectionError && attempt==0)continue;
                    bool accepted=false;
                    try
                    {
                        var reply=JsonUtility.FromJson<PythonCommandReply>(request.downloadHandler.text);
                        if(request.result!=UnityWebRequest.Result.Success)throw new Exception(reply!=null && !string.IsNullOrEmpty(reply.error) ? reply.error : "通信に失敗しました。接続後に再試行してください。");
                        PythonStateClient.Validate(reply.state);accepted=true;
                    }
                    catch(Exception ex){notice.text=ex.Message;GetComponent<PythonLegacySavePanel>()?.Notify(ex.Message);commandFailed=true;online=false;}
                    if(accepted){SceneManager.LoadScene("CatCafePythonState");yield break;}
                    break;
                }
            }
            busy=false;RefreshControls();
        }
    }
}
