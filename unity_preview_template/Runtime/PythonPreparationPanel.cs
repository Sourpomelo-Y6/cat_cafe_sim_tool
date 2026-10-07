using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonPreparationPanel : MonoBehaviour
    {
        public GameObject panel;
        public Text details, notice;
        public Button open, close;
        private PythonCafeState current;
        private string instanceId;
        private bool online;
        public void Apply(PythonCafeState state)
        {
            current=state;online=true;
            bool preparation=state.can_set_shifts && !state.closed && state.game_over==null;
            if(instanceId!=state.instance_id)
            {
                instanceId=state.instance_id;
                panel.SetActive(preparation && state.day==1 && state.tick==0 && string.IsNullOrEmpty(state.required_action));
            }
            if(!preparation)panel.SetActive(false);
            open.gameObject.SetActive(preparation && state.next_goal==null);
            Render();RefreshControls();
        }
        public void Open()
        { if(current==null || !current.can_set_shifts || current.closed || current.game_over!=null)return;panel.SetActive(true);panel.transform.SetAsLastSibling();Render(); }
        public void Close() { panel.SetActive(false); }
        private void Render()
        {
            if(current==null)return;
            var option=Array.Find(current.new_game_options ?? new PythonNewGameOption[0],row=>row.choice==current.objective);
            var text=new StringBuilder("経営目標："+(current.objective_progress?.label ?? option?.label ?? "自由営業"));
            if(current.objective_progress!=null)text.Append("\n"+current.objective_progress.details);
            else if(option!=null)text.Append("\n"+option.detail);
            if(option!=null)text.Append("\n\n標準の開始条件\n"+option.conditions);
            text.Append("\n\n現在の準備状態："+current.day+"日目 / 猫 "+current.cats.Length+"匹 / 席 "+(current.seats?.Length ?? 0)+"席 / 資金 "+current.funds.ToString("0.#"));
            text.Append("\n\n1. 猫カードの「出勤」で、接客する猫を選びます。\n2. 画面上部の「営業開始」でお店を開きます。\n3. 「自動進行」で接客を進めます。もう一度押すと停止します。\n\n疲れた猫は「休養」にして休ませましょう。\n病気・派遣中・行方不明・譲渡済みの猫は出勤できません。\nイベントの確認画面が出たら、先に回答してください。");
            foreach(var cat in current.cats)
                if(cat.health_status!="healthy" || cat.activity!="cafe")text.Append("\n"+cat.name+"："+cat.health_label+" / "+cat.activity_label+"（出勤できません）");
            details.text=text.ToString();RenderNotice();
        }
        private void RenderNotice()
        {
            if(!online){notice.text="Python未接続です。再接続後に営業の準備を確認してください。";return;}
            if(!string.IsNullOrEmpty(current.required_action)){notice.text="先に確認が必要です："+current.required_action;return;}
            bool working=Array.Exists(current.cats,cat=>cat.working);
            notice.text=working ? "出勤する猫を確認して、案内を閉じたら「営業開始」を押してください。" : "出勤する猫がいません。案内を閉じて、猫カードの「出勤」を押してください。";
            if(!GetComponent<PythonStateClient>().CanLeaveResult)notice.text="操作を送信しています。完了するまでお待ちください。";
        }
        public void RefreshControls() { open.interactable=current!=null;close.interactable=true;if(current!=null)RenderNotice(); }
        public void Disconnect() { online=false;if(current!=null)RenderNotice(); }
    }
}
