using System;
using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview
{
    public class PythonPlayerPanel : MonoBehaviour
    {
        public GameObject panel;
        public Text title, summary, history, notice, choiceLabel;
        public RawImage picture;
        public Text imageNotice;
        public Dropdown targets;
        public Button[] actions;
        public Button finish, back;
        public ScrollRect historyScroll;
        private PythonPlayerInteraction current;
        private string sessionId, instanceId;
        private bool showing;
        private string renderedMode, renderedSession, renderedInstance;
        public void TargetChanged(int value) { RefreshControls(); }
        public void Open()
        {
            if(current==null || !GetComponent<PythonStateClient>().CanManageSaves)return;
            Restore();
        }
        public void Restore()
        {
            if(current==null)return;
            showing=true;GetComponent<PythonCatDetailsPanel>().Close();Render();
        }
        public void Close() { showing=false;panel.SetActive(false); }
        public void ReturnToDetails()
        {
            if(current==null || !GetComponent<PythonStateClient>().CanManageSaves)return;
            string catId=current.cat_id;Close();GetComponent<PythonCatDetailsPanel>().Open(catId);
        }
        public void Finish() { GetComponent<PythonStateClient>().FinishPlayer(); }
        public void SelectAction(int index)
        {
            if(current==null || current.actions==null || index<0 || index>=current.actions.Length)return;
            int targetIndex=targets.value-1;
            if(current.actions[index].choice=="switch" && targetIndex<0)return;
            string target=current.targets!=null && targetIndex>=0 && targetIndex<current.targets.Length ? current.targets[targetIndex].choice : "";
            GetComponent<PythonStateClient>().PlayerAction(current.actions[index].choice,target);
        }
        public void Apply(PythonCafeState state)
        {
            current=state.player_interaction;
            if(current==null) { showing=false;sessionId=null;panel.SetActive(false);return; }
            if(sessionId!=current.session_id || instanceId!=state.instance_id)
            { showing=true;sessionId=current.session_id;instanceId=state.instance_id;historyScroll.verticalNormalizedPosition=1; }
            if(showing)GetComponent<PythonCatDetailsPanel>().Close();
            Render();
        }
        private void Render()
        {
            panel.SetActive(showing);if(showing)panel.transform.SetAsLastSibling();
            title.text=current.name+(current.active ? "と交流中" : "との交流結果");
            summary.text="現在の交流："+current.mode+"\n残り "+current.remaining_ticks+"ターン / 本日あと "+current.remaining_sets+"セット"
                +"\n好感度 "+current.affinity_before.ToString("0.#")+" → "+current.affinity_after.ToString("0.#")+"（"+current.affinity_delta.ToString("+0.#;-0.#;0")+"・"+(current.active ? "見込み" : "確定")+"）"
                +"\n体力 "+current.stamina_before.ToString("0.#")+" → "+current.stamina.ToString("0.#")
                +"\n関心 "+current.engagement.ToString("0.#")+" / テンション "+current.tension.ToString("0.#");
            if(picture != null)
            {
                var texture=GetComponent<ContentPreview>().PlayerTexture(current.content_cat_id, current.scene_key, current.image_key);
                picture.texture=texture;picture.enabled=texture!=null;
                var fitter=picture.GetComponent<AspectRatioFitter>();
                if(texture!=null && fitter!=null)fitter.aspectRatio=(float)texture.width/texture.height;
                if(imageNotice!=null)imageNotice.text=texture==null ? "この猫の画像は未登録です。" : "";
            }
            history.text=string.IsNullOrEmpty(current.history) ? "行動を選ぶと猫の反応を表示します。" : current.history;
            string selected=targets.options.Count>0 ? targets.options[targets.value].text : "";
            bool changed=renderedMode!=current.mode || renderedSession!=current.session_id || renderedInstance!=instanceId;
            targets.options.Clear();
            targets.options.Add(new Dropdown.OptionData(current.mode));
            foreach(var row in current.targets ?? new PythonEventChoice[0])targets.options.Add(new Dropdown.OptionData(row.label));
            targets.SetValueWithoutNotify(changed ? 0 : Math.Max(0,targets.options.FindIndex(row=>row.text==selected)));targets.RefreshShownValue();
            renderedMode=current.mode;renderedSession=current.session_id;renderedInstance=instanceId;
            notice.text=current.active ? (string.IsNullOrEmpty(current.reason) ? "好感度は終了時に確定します。営業時間は進みません。" : current.reason) : current.reason;
            for(int i=0;i<actions.Length;i++)
            {
                bool exists=current.actions!=null && i<current.actions.Length;
                actions[i].gameObject.SetActive(exists && current.active);
                if(exists)actions[i].GetComponentInChildren<Text>().text=current.actions[i].label;
            }
            if(choiceLabel!=null)choiceLabel.gameObject.SetActive(current.active);
            targets.gameObject.SetActive(current.active);
            finish.gameObject.SetActive(current.active);
            if(back!=null)back.GetComponentInChildren<Text>().text=current.active ? "猫の詳細へ戻る（交流を保持）" : "猫の詳細へ戻る";
            RefreshControls();
        }
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();
            for(int i=0;i<actions.Length;i++)actions[i].interactable=current!=null && current.actions!=null && i<current.actions.Length && client.CanPlayerAction(current.actions[i].choice) && (current.actions[i].choice!="switch" || targets.value>0);
            if(back!=null)back.interactable=current!=null && client.CanManageSaves;
            targets.interactable=client.CanPlayerAction("switch");finish.interactable=client.CanFinishPlayer;
        }
        public void Disconnect()
        {
            if(back!=null)back.interactable=false;
            foreach(var button in actions)button.interactable=false;
            targets.interactable=finish.interactable=false;
            if(panel.activeSelf)notice.text="Python未接続です。再接続後に交流を再開できます。";
        }
    }
}
