using System;
using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview
{
    public class PythonPlayerPanel : MonoBehaviour
    {
        public GameObject panel;
        public Text title, summary, history, notice, choiceLabel;
        public Text[] actionHints;
        public Text resultGuide;
        public RawImage picture;
        public Text imageNotice;
        public Dropdown targets;
        public Button[] actions;
        public Button finish, back;
        public ScrollRect historyScroll;
        private PythonPlayerInteraction current;
        private string sessionId, instanceId;
        private bool showing,wasAutomatic,disconnected;
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
            if(current==null || current.actions==null || index<0 || index>=current.actions.Length || !GetComponent<PythonStateClient>().CanPlayerAction(current.actions[index].choice))return;
            int targetIndex=targets.value-1;
            if(targetIndex>=0 && current.actions[index].choice!="switch")return;
            if(current.actions[index].choice=="switch" && targetIndex<0)return;
            string target=current.targets!=null && targetIndex>=0 && targetIndex<current.targets.Length ? current.targets[targetIndex].choice : "";
            GetComponent<PythonStateClient>().PlayerAction(current.actions[index].choice,target);
        }
        public void Apply(PythonCafeState state)
        {
            disconnected=false;current=state.player_interaction;
            if(current==null) { showing=false;sessionId=null;panel.SetActive(false);return; }
            bool automatic=state.autoplay?.running==true;
            if(sessionId!=current.session_id || instanceId!=state.instance_id || (wasAutomatic && !automatic))
            { showing=true;sessionId=current.session_id;instanceId=state.instance_id;historyScroll.verticalNormalizedPosition=1; }
            wasAutomatic=automatic;
            if(automatic || (!current.active && !string.IsNullOrEmpty(state.required_action)))showing=false;
            if(showing)GetComponent<PythonCatDetailsPanel>().Close();
            Render();
        }
        private void Render()
        {
            panel.SetActive(showing);if(showing)panel.transform.SetAsLastSibling();
            title.text=current.name+(current.active ? "と交流中" : "との交流結果");
            summary.text="現在の動作："+current.mode+"\n残り "+current.remaining_ticks+"ターン / 本日あと "+current.remaining_sets+"セット"
                +"\n好感度 "+current.affinity_before.ToString("0.#")+" → "+current.affinity_after.ToString("0.#")+"（"+current.affinity_delta.ToString("+0.#;-0.#;0")+"・"+(current.active ? "見込み" : "確定")+"）"
                +"\n体力 "+current.stamina_before.ToString("0.#")+" → "+current.stamina.ToString("0.#")+" / "+(current.max_stamina>0?current.max_stamina:100).ToString("0.#")
                +"\n関心 "+current.engagement.ToString("0.#")+" / テンション "+current.tension.ToString("0.#");
            if(picture != null)
            {
                var texture=GetComponent<ContentPreview>().PlayerTexture(current.content_cat_id, current.scene_key, current.image_key, current.event_image_key);
                picture.texture=texture;picture.enabled=texture!=null;
                var fitter=picture.GetComponent<AspectRatioFitter>();
                if(texture!=null && fitter!=null)fitter.aspectRatio=(float)texture.width/texture.height;
                if(imageNotice!=null)imageNotice.text=texture==null ? "この猫の画像は未登録です。" : "";
            }
            bool narrativeChanged=history.text!=current.history;
            bool narrative=current.history!=null && current.history.StartsWith("最新のやり取り");
            if(picture!=null){var frame=picture.transform.parent.GetComponent<RectTransform>();frame.anchoredPosition=new Vector2(480,-72);frame.sizeDelta=new Vector2(436,narrative?112:208);}
            var heading=historyScroll.transform.parent.Find("History Heading") as RectTransform;
            if(heading!=null)heading.anchoredPosition=new Vector2(480,narrative?-194:-292);
            var area=historyScroll.GetComponent<RectTransform>();area.anchoredPosition=new Vector2(480,narrative?-228:-330);area.sizeDelta=new Vector2(436,narrative?310:208);
            historyScroll.viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,narrative?298:196);
            history.fontSize=narrative?18:17;history.lineSpacing=narrative?1.15f:1;
            history.text=string.IsNullOrEmpty(current.history) ? "行動を選ぶと猫の反応を表示します。" : current.history;
            if(narrativeChanged && current.cat_id=="cat-mike"){Canvas.ForceUpdateCanvases();historyScroll.verticalNormalizedPosition=1;}
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
            if(resultGuide!=null){resultGuide.gameObject.SetActive(!current.active);resultGuide.text="交流が終了しました\n\n好感度の変化は確定しています。\n終了理由："+(string.IsNullOrEmpty(current.reason) ? "記録なし" : current.reason)+"\n\n右側で行動と猫の反応を振り返れます。\n猫の詳細から次の交流や営業準備へ戻れます。";}
            if(choiceLabel!=null)choiceLabel.gameObject.SetActive(current.active);
            targets.gameObject.SetActive(current.active);
            finish.gameObject.SetActive(current.active);
            if(back!=null)back.GetComponentInChildren<Text>().text=current.active ? "猫の詳細へ戻る（交流を保持）" : "猫の詳細へ戻る";
            RefreshControls();
        }
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();
            for(int i=0;i<actions.Length;i++)
            {
                bool exists=current!=null && current.actions!=null && i<current.actions.Length;
                var row=exists ? current.actions[i] : null;
                bool switching=exists && row.choice=="switch";
                actions[i].interactable=!disconnected && exists && client.CanPlayerAction(row.choice) && (switching ? targets.value>0 : targets.value==0);
                if(actionHints!=null && i<actionHints.Length && actionHints[i]!=null)
                {
                    var hint=actionHints[i];hint.gameObject.SetActive(exists && current.active);
                    hint.text=!exists ? "" : !row.can_select ? (string.IsNullOrEmpty(row.reason) ? "現在の動作・猫の状態では選べません" : row.reason)
                        : switching ? (targets.value>0 ? "選んだ動作へ変更（1ターン使用）" : "先に切り替え先を選んでください") : targets.value>0 ? "先に動作を切り替えてください" : "";
                    hint.color=exists && !row.can_select ? new Color(.55f,.29f,.12f) : new Color(.28f,.36f,.30f);
                }
            }
            if(current!=null && notice!=null)
                notice.text=disconnected ? "Python未接続です。再接続後に交流を再開できます。" : !client.CanManageSaves ? (client.HasPendingResults ? "未保存の接客結果を先に確定してください。" : client.AutoPlayRunning ? "おまかせ実行中です。" : "通信を確認中です。操作の完了をお待ちください。")
                    : current.active ? (targets.value>0 ? "「動作を切り替える」を押すと、選んだ動作へ変更します（1ターン使用）。" : string.IsNullOrEmpty(current.reason) ? "好感度は終了時に確定。戻っても交流は続きます。" : current.reason) : "好感度は確定済みです。猫の詳細へ戻れます。";
            if(back!=null)back.interactable=!disconnected && current!=null && client.CanManageSaves;
            targets.interactable=!disconnected && client.CanPlayerAction("switch");finish.interactable=!disconnected && client.CanFinishPlayer;
        }
        public void Disconnect()
        {
            disconnected=true;
            if(back!=null)back.interactable=false;
            foreach(var button in actions)button.interactable=false;
            targets.interactable=finish.interactable=false;
            if(panel.activeSelf)notice.text="Python未接続です。再接続後に交流を再開できます。";
        }
    }
}
