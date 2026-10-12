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
        private bool commandFailed,confirming;
        private string stateIdentity,instance;
        public Button conditionsBack;
        public bool ConfirmingStart=>confirming;
        private string Guide=>conditionsPanel.activeSelf?(confirming?"開始内容を確認しています。「この内容で開始する」で確定できます。":"目標と開始条件を確認して、開始の確認へ進んでください。"):savesPanel.activeSelf?"スロットを選び、内容を確認してロードしてください。":"はじめから：目標を選んで新しいカフェを作ります。つづきから：保存したカフェや終了結果を読み込みます。";
        public void SelectSave(int index) { if(saveDetails!=null)saveDetails.text=index>=0 && index<entries.Length ? entries[index].Details() : "保存したゲームはありません。"; }
        public Button newGame, continueGame, startGame, resume;
        public int port=8190;
        public PythonCafeState CurrentState => state;
        public bool CanManageSaves => online && !busy && state!=null;
        public void ResumePython(string token){if(CanManageSaves)StartCoroutine(Send("load_python_game","",token));}
        public void ImportLegacy(string path,int slot){if(CanManageSaves)StartCoroutine(Send("import_legacy_save","",path,slot));}
        private PythonCafeState state;
        private PythonSavedGame[] entries=new PythonSavedGame[0];
        private bool busy, online, savesAvailable;
        public void ShowHome() { confirming=false;GetComponent<PythonSaveBrowser>()?.Cancel();home.SetActive(true);conditionsPanel.SetActive(false);savesPanel.SetActive(false);RefreshControls(); }
        public void ShowConditions() { confirming=false;home.SetActive(false);conditionsPanel.SetActive(true);savesPanel.SetActive(false);SelectObjective(objectives.value); }
        public void ShowSaves() { confirming=false;home.SetActive(false);conditionsPanel.SetActive(false);savesPanel.SetActive(true);SelectSave(saves.value);RefreshControls(); }
        public void BackFromConditions(){if(confirming){confirming=false;RenderObjective(objectives.value);RefreshControls();}else ShowHome();}
        public void SelectObjective(int index){confirming=false;RenderObjective(index);RefreshControls();ResetConditionsScroll();}
        private void ResetConditionsScroll(){var scroll=conditions.GetComponentInParent<ScrollRect>(true);if(scroll!=null){Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=1;}}
        private void RenderObjective(int index)
        {
            if(state==null || state.new_game_options==null || index<0 || index>=state.new_game_options.Length){confirming=false;conditions.text="開始できる目標の情報を取得していません。再接続後に確認してください。";return;}
            var option=state.new_game_options[index];conditions.text=(confirming?"開始の確認\n":"目標\n")+option.label+"\n\n"+option.detail+"\n\n開始条件\n"+option.conditions;var scroll=conditions.GetComponentInParent<ScrollRect>(true);if(scroll!=null)conditions.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Math.Max(scroll.viewport.rect.height,conditions.preferredHeight+12));
        }
        public void RefreshControls()
        {
            bool ready=online && !busy;
            newGame.interactable=startGame.interactable=objectives.interactable=ready && state.new_game_options!=null && state.new_game_options.Length>0;
            continueGame.interactable=ready;
            saves.interactable=resume.interactable=ready && savesAvailable && entries.Length>0;
            startGame.GetComponentInChildren<Text>().text=confirming?"この内容で開始する":"この目標で開始（確認）";if(conditionsBack!=null)conditionsBack.GetComponentInChildren<Text>().text=confirming?"開始確認を取り消す":"タイトルへ戻る";if(ready && !commandFailed)notice.text=Guide;
            GetComponent<PythonLegacySavePanel>()?.RefreshControls();GetComponent<PythonSaveBrowser>()?.Refresh();
        }
        public void Apply(PythonCafeState latest)
        {
            PythonStateClient.Validate(latest);string previous=state!=null && state.new_game_options!=null && objectives.value>=0 && objectives.value<state.new_game_options.Length ? state.new_game_options[objectives.value].choice : "";
            string identity=latest.instance_id+"/"+latest.revision;if(stateIdentity!=identity){stateIdentity=identity;confirming=false;}if(instance!=latest.instance_id){instance=latest.instance_id;previous="";ShowHome();}state=latest;online=true;GetComponent<PythonLegacySavePanel>()?.Apply(latest);objectives.options.Clear();
            foreach(var row in latest.new_game_options ?? new PythonNewGameOption[0])objectives.options.Add(new Dropdown.OptionData(row.label));
            objectives.SetValueWithoutNotify(Math.Max(0,Array.FindIndex(latest.new_game_options ?? new PythonNewGameOption[0],row=>row.choice==previous)));
            objectives.RefreshShownValue();RenderObjective(objectives.value);RefreshControls();
        }
        public void ApplySaves(PythonSaveList list)
        {
            if(list==null || list.saves==null)throw new ArgumentException("保存一覧を取得できません。");
            string previous=entries.Length>0 ? entries[Math.Max(0,Math.Min(saves.value,entries.Length-1))].save_id : "";
            entries=list.saves;savesAvailable=true;saves.options.Clear();
            foreach(var row in entries)saves.options.Add(new Dropdown.OptionData(row.label));
            if(entries.Length==0)saves.options.Add(new Dropdown.OptionData("保存したゲームはありません"));
            saves.SetValueWithoutNotify(Math.Max(0,Array.FindIndex(entries,row=>row.save_id==previous)));saves.RefreshShownValue();SelectSave(saves.value);RefreshControls();GetComponent<PythonSaveBrowser>()?.Apply(list);
        }
        public void Disconnect(string message) {confirming=false;RenderObjective(objectives.value);online=false;GetComponent<PythonLegacySavePanel>()?.Disconnect();notice.text=message;GetComponent<PythonSaveBrowser>()?.Notify(message);RefreshControls();}
        private IEnumerator Start()
        {
            ShowHome();notice.text="接続しています。";RefreshControls();
            while(true)
            {
                if(!busy)
                {
                    using(var request=new CafeRequest(CafeBackends.Resolve(port),"/state"))
                    {
                        yield return request.SendWebRequest();
                        if(busy)continue;
                        if(request.result!=UnityWebRequest.Result.Success)Disconnect("ゲーム状態を取得できません。");
                        else {try{Apply(JsonUtility.FromJson<PythonCafeState>(request.Text));if(!commandFailed)notice.text=Guide;}catch(Exception ex){Disconnect(ex.Message);}}
                    }
                    if(online && !busy)using(var request=new CafeRequest(CafeBackends.Resolve(port),"/saves"))
                    {
                        yield return request.SendWebRequest();if(busy)continue;
                        try {if(request.result!=UnityWebRequest.Result.Success)throw new Exception("保存一覧を取得できません。");ApplySaves(JsonUtility.FromJson<PythonSaveList>(request.Text));}
                        catch(Exception ex){savesAvailable=false;GetComponent<PythonSaveBrowser>()?.Unavailable();if(saveDetails!=null)saveDetails.text="保存一覧を取得できません。接続を確認してください。";notice.text=ex.Message;RefreshControls();}
                    }
                }
                yield return new WaitForSecondsRealtime(2);
            }
        }
        public void StartNewGame() {if(!CanManageSaves || !startGame.interactable || state.new_game_options==null || objectives.value<0 || objectives.value>=state.new_game_options.Length)return;if(!confirming){confirming=true;RenderObjective(objectives.value);RefreshControls();ResetConditionsScroll();return;}confirming=false;StartCoroutine(Send("new_game",state.new_game_options[objectives.value].choice,""));}
        public void Resume() {if(resume.interactable)StartCoroutine(Send("load_game","",entries[saves.value].save_id));}
        public void ResumeSaved(string id){if(CanManageSaves)StartCoroutine(Send("load_game","",id));}
        private IEnumerator Send(string kind,string choice,string saveId,int slot=0)
        {
            busy=true;commandFailed=false;RefreshControls();notice.text=kind=="load_game" || kind=="load_python_game" || kind=="import_legacy_save" ? "保存したゲームを読み込んでいます。" : "新しいゲームを準備しています。";
            var command=new PythonShiftCommand{request_id=Guid.NewGuid().ToString("N"),instance_id=state.instance_id,expected_revision=state.revision,
                kind=kind,choice=choice,save_id=saveId,save_slot=slot,working_cats=new string[0]};
            for(int attempt=0;attempt<2;attempt++)
            {
                using(var request=new CafeRequest(CafeBackends.Resolve(port),"/commands",JsonUtility.ToJson(command)))
                {
                    request.timeout=5;yield return request.SendWebRequest();
                    if(request.result==UnityWebRequest.Result.ConnectionError && attempt==0)continue;
                    bool accepted=false;
                    try
                    {
                        var reply=JsonUtility.FromJson<PythonCommandReply>(request.Text);
                        if(request.result!=UnityWebRequest.Result.Success)throw new Exception(reply!=null && !string.IsNullOrEmpty(reply.error) ? reply.error : "通信に失敗しました。接続後に再試行してください。");
                        PythonStateClient.Validate(reply.state);accepted=true;
                    }
                    catch(Exception ex){notice.text=ex.Message;GetComponent<PythonSaveBrowser>()?.Notify(ex.Message);GetComponent<PythonLegacySavePanel>()?.Notify(ex.Message);commandFailed=true;online=false;}
                    if(accepted){SceneManager.LoadScene("CatCafePythonState");yield break;}
                    break;
                }
            }
            busy=false;RefreshControls();
        }
    }
}
