using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    [Serializable] public class PythonMissingRest { public bool can_rest; public string reason; public float cost, funds_after; }
    [Serializable] public class PythonMissingCat
    {
        public string choice, cat_id, name, status, reason;
        public int departed_day, remaining, resolved_day;
        public bool kitten, can_resolve;
        public float cost, funds_after, return_stress, max_stamina;
    }
    public class PythonMissingPanel : MonoBehaviour
    {
        public GameObject panel;
        public Text title, details, notice;
        public Dropdown cats;
        public Button confirm, close, rest;
        private PythonMissingRest restState;
        private PythonMissingCat[] rows=new PythonMissingCat[0];
        private string signature, selected, instanceId;
        private bool showing;
        private string offered;
        public void ShowRecords(){showing=true;Render();}
        MissingConversationPanel Dialogue=>GetComponent<MissingConversationPanel>()??gameObject.AddComponent<MissingConversationPanel>();
        public void Open(string catId)
        {
            if(!GetComponent<PythonStateClient>().CanManageSaves)return;
            selected=rows.LastOrDefault(r=>r.cat_id==catId && r.status!="resolved")?.choice ?? rows.LastOrDefault(r=>r.cat_id==catId)?.choice;showing=true;
            GetComponent<PythonCatDetailsPanel>().Close();Render();if(CafeBackends.Standalone && selected!=null)Dialogue.Begin(selected);
        }
        public void Close() { showing=false;panel.SetActive(false); }
        public void Select(int index)
        { if(index>=0 && index<rows.Length)selected=rows[index].choice;RenderDetails();if(CafeBackends.Standalone && selected!=null)Dialogue.Begin(selected); }
        public void Confirm() { if(selected!=null)GetComponent<PythonStateClient>().ResolveMissing(selected); }
        public void Rest() { GetComponent<PythonStateClient>().RestForMissing(); }
        public void Apply(PythonCafeState state)
        {
            bool hadRecords=rows.Length>0;rows=state.missing_cats ?? new PythonMissingCat[0];
            restState=state.missing_rest;
            if(instanceId!=state.instance_id) { instanceId=state.instance_id;signature=null;showing=false;selected=null; }
            string next=string.Join("|",rows.Where(r=>r.status!="resolved").Select(r=>r.choice+":"+r.status));
            if(next!=signature)
            {
                var previous=(signature ?? "").Split('|');
                var active=rows.FirstOrDefault(r=>r.status=="waiting") ?? rows.FirstOrDefault(r=>r.status=="missing" && !previous.Contains(r.choice+":"+r.status));
                signature=next;
                if(active!=null) { selected=active.choice;showing=true;GetComponent<PythonCatDetailsPanel>().Close(); }
            }
            if(rows.Length==0) { if(hadRecords)showing=false;selected=null; }
            Render();if(CafeBackends.Standalone){var backend=CafeBackends.Local as LocalCafeBackend;var pending=backend.Simulation.State.missing_cases.FirstOrDefault(e=>e.result!=null && !e.result.completed) ?? backend.Simulation.State.missing_cases.FirstOrDefault(e=>e.confirmation!=null && !e.confirmation.completed) ?? backend.Simulation.State.missing_cases.FirstOrDefault(e=>e.notice!=null && !e.notice.completed);string offer=pending==null?null:state.instance_id+"/"+pending.id+"/"+(pending.result!=null?"result":pending.confirmation!=null?"confirmation":"notice");if(offer!=null && offered!=offer){offered=offer;selected=pending.id;Dialogue.Begin(selected);}Dialogue.Apply(state,this);}
        }
        private void Render()
        {
            panel.SetActive(showing);if(!showing)return;
            cats.options.Clear();foreach(var row in rows)cats.options.Add(new Dropdown.OptionData(row.name+" / "+(row.departed_day>0 ? row.departed_day+"日目" : "家出日不明")+"（"+(row.status=="missing" ? "家出中" : row.status=="waiting" ? "帰還待ち" : "確認済み")+"）"));
            if(rows.Length==0)cats.options.Add(new Dropdown.OptionData("家出の記録はありません"));
            cats.SetValueWithoutNotify(Math.Max(0,Array.FindIndex(rows,r=>r.choice==selected)));cats.RefreshShownValue();
            selected=cats.value<rows.Length ? rows[cats.value].choice : null;RenderDetails();
        }
        private void RenderDetails()
        {
            var row=Array.Find(rows,r=>r.choice==selected);title.text="猫の家出・帰還";
            rest.gameObject.SetActive(row!=null && row.status=="missing");
            if(row==null) { details.text="家出の記録はありません。";notice.text="猫のストレスは猫一覧や詳細画面で確認できます。";RefreshControls();return; }
            details.text=row.name+" / "+(row.departed_day>0 ? row.departed_day+"日目に家出" : "家出日の記録なし")+"\n";
            if(row.status=="missing")
            {
                details.text+="あと "+row.remaining+"回の閉店で帰還予定\n家出した日の閉店は残り日数に含みません。\n\n家出中は接客・プレイヤーとの交流・派遣を担当できません。\n帰還時に結果と費用を表示します。";
                if(restState!=null && restState.can_rest)details.text+="\n\n休業費用："+restState.cost.ToString("0.#")+" / 休業後の資金："+restState.funds_after.ToString("0.#")+(restState.funds_after<=0 ? "（資金不足で経営終了）" : "");
                notice.text=restState!=null && !string.IsNullOrEmpty(restState.reason) ? restState.reason : "営業を続けるか、準備中に休業すると帰還までの日数が進みます。";
            }
            else
            {
                details.text+=(row.status=="waiting" ? "帰還しました。確認すると在店状態へ戻ります。" : row.resolved_day+"日目に帰還を確認しました。")
                    +"\n\n"+(row.kitten ? "子猫を連れて帰還しました。引き渡し費用："+row.cost.ToString("0.#")+"\n子猫はカフェの在籍猫には加わりません。" : "子猫の引き渡しはありません。費用：0")
                    +"\n\n帰還時：体力 "+row.max_stamina.ToString("0.#")+" / ストレス "+row.return_stress.ToString("0.#")
                    +"\n体調と疲労は引き継ぎます。出勤設定を確認してください。";
                if(row.status=="resolved")details.text+="\n精算直後の資金："+row.funds_after.ToString("0.#");
                if(row.status=="waiting")details.text+="\n確認後の資金："+row.funds_after.ToString("0.#")+(row.funds_after<=0 ? "（資金不足で経営終了）" : "");
                notice.text=!string.IsNullOrEmpty(row.reason) ? row.reason : row.status=="waiting" ? "帰還待ちの猫を確認すると、営業・翌日への進行を再開できます。" : "帰還と費用は確定済みです。";
            }
            RefreshControls();
        }
        public void RefreshControls()
        {
            bool ready=GetComponent<PythonStateClient>().CanManageSaves;var row=Array.Find(rows,r=>r.choice==selected);
            confirm.interactable=ready && row!=null && row.can_resolve;cats.interactable=ready && rows.Length>0;
            rest.interactable=ready && row!=null && row.status=="missing" && restState!=null && restState.can_rest;
            close.interactable=CafeBackends.Standalone || !rows.Any(r=>r.status=="waiting" && r.can_resolve);
        }
        public void Disconnect()
        { signature=null;rest.interactable=confirm.interactable=cats.interactable=false;close.interactable=true;if(panel.activeSelf)notice.text="Python未接続です。再接続後に帰還を確認できます。"; }
    }
}
