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
        private PythonCafeState state;
        public Button preparation;
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
        public void ReturnToPreparation()
        {
            if(current==null || current.active || !GetComponent<PythonStateClient>().CanManageSaves)return;
            var target=GetComponent<PythonPreparationPanel>();
            if(state?.can_set_shifts!=true || GetComponent<PythonStateClient>().AutoPlayRunning)return;
            string id=current.cat_id;Close();target.Open();target.SelectCatId(id);
        }
        private void EnsurePreparationButton()
        {
            if(preparation!=null)return;
            preparation=Instantiate(finish,finish.transform.parent);preparation.name="After Interaction Preparation";
            preparation.onClick=new Button.ButtonClickedEvent();preparation.onClick.AddListener(ReturnToPreparation);
            var rect=preparation.GetComponent<RectTransform>();rect.anchoredPosition=new Vector2(24,-478);rect.sizeDelta=new Vector2(432,40);
            preparation.GetComponentInChildren<Text>().text="営業準備・出勤／休養の設定へ";
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
            disconnected=false;this.state=state;current=state.player_interaction;
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
            EnsurePreparationButton();
            panel.SetActive(showing);if(showing)panel.transform.SetAsLastSibling();
            title.text=current.name+(current.active ? "と交流中" : "との交流結果");
            var cat=Array.Find(state?.cats ?? new PythonCatState[0],c=>c.cat_id==current.cat_id);
            summary.fontSize=16;
            summary.text="現在の動作："+current.mode+"\n残り "+current.remaining_ticks+"ターン / 本日あと "+current.remaining_sets+"セット"
                +"\n好感度 "+current.affinity_before.ToString("0.#")+" → "+current.affinity_after.ToString("0.#")+"（"+current.affinity_delta.ToString("+0.#;-0.#;0")+"・"+(current.active ? "見込み" : "確定")+"）"
                +"\n現在体力 "+(cat?.stamina ?? current.stamina).ToString("0.#")+" / "+(cat?.max_stamina ?? current.max_stamina).ToString("0.#")
                +"\n"+current.stamina_guidance
                +"\n交流後の体力で営業へ。疲労・ストレスは別の値です。";
            if(picture != null)
            {
                var texture=CafeNarrativeResources.Texture(current.narrative_image) ?? GetComponent<ContentPreview>().PlayerTexture(current.content_cat_id, current.scene_key, current.image_key, current.event_image_key);
                picture.texture=texture;picture.enabled=texture!=null;
                var fitter=picture.GetComponent<AspectRatioFitter>();
                if(texture!=null && fitter!=null)fitter.aspectRatio=(float)texture.width/texture.height;
                if(imageNotice!=null)imageNotice.text=texture==null ? "この猫の画像は未登録です。" : "";
            }
            bool narrativeChanged=history.text!=current.history;
            bool narrative=current.history!=null && current.history.StartsWith("最新のやり取り");
            if(picture!=null){var frame=picture.transform.parent.GetComponent<RectTransform>();frame.anchoredPosition=new Vector2(480,-72);frame.sizeDelta=new Vector2(436,narrative?112:208);}
            var heading=historyScroll.transform.parent.Find("History Heading") as RectTransform;
            if(heading!=null){heading.anchoredPosition=new Vector2(480,narrative?-194:-292);var label=heading.GetComponent<Text>();label.fontSize=15;label.text="行動と反応 / 関心 "+current.engagement.ToString("0.#")+" / テンション "+current.tension.ToString("0.#");}
            var area=historyScroll.GetComponent<RectTransform>();area.anchoredPosition=new Vector2(480,narrative?-228:-330);area.sizeDelta=new Vector2(436,narrative?310:208);
            historyScroll.viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,narrative?298:196);
            history.fontSize=17;history.lineSpacing=narrative?1.05f:1;
            history.text=string.IsNullOrEmpty(current.history) ? "行動を選ぶと猫の反応を表示します。" : current.history;
            if(narrativeChanged){Canvas.ForceUpdateCanvases();historyScroll.verticalNormalizedPosition=1;}
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
            if(resultGuide!=null)
            {
                resultGuide.gameObject.SetActive(!current.active);resultGuide.fontSize=16;
                resultGuide.rectTransform.sizeDelta=new Vector2(432,232);
                resultGuide.text="交流終了・好感度は確定済み\n"+current.reason
                    +"\n\n休養予定への変更では、今の体力は回復しません。体力は翌日の準備で最大まで回復します。\n疲労・ストレスの休養回復は閉店時です。\n\n"+PreparationReason;
            }
            preparation.gameObject.SetActive(!current.active);

            if(choiceLabel!=null)choiceLabel.gameObject.SetActive(current.active);
            targets.gameObject.SetActive(current.active);
            finish.gameObject.SetActive(current.active);
            if(back!=null)back.GetComponentInChildren<Text>().text=current.active ? "猫の詳細へ戻る（交流を保持）" : "猫の詳細へ戻る";
            RefreshControls();
        }
        private string PreparationReason=>current?.active==true?"交流中は予定を変更できません。終了後に営業準備で変更できます。"
            : state?.autoplay?.running==true?"おまかせを停止してから予定を変更できます。"
            : !string.IsNullOrEmpty(state?.required_action)?"先にイベント・目標の確認が必要です。閉じて案内に従ってください。"
            : state?.can_set_shifts!=true?"営業中・閉店後は予定を変更できません。翌日の営業準備で変更できます。"
            : "下の営業準備で、この猫の出勤・休養と最新の予測を確認できます。";
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
            if(preparation!=null)preparation.interactable=!disconnected && current!=null && !current.active && state?.can_set_shifts==true && !client.AutoPlayRunning && client.CanManageSaves;
            if(current?.active==true && targets.value==0 && notice!=null && !disconnected && client.CanManageSaves)notice.text="交流終了後に営業準備で予定変更できます。";
            if(back!=null)back.interactable=!disconnected && current!=null && client.CanManageSaves;
            targets.interactable=!disconnected && client.CanPlayerAction("switch");finish.interactable=!disconnected && client.CanFinishPlayer;
        }
        public void Disconnect()
        {
            disconnected=true;if(preparation!=null)preparation.interactable=false;
            if(back!=null)back.interactable=false;
            foreach(var button in actions)button.interactable=false;
            targets.interactable=finish.interactable=false;
            if(panel.activeSelf)notice.text="Python未接続です。再接続後に交流を再開できます。";
        }
    }
}
