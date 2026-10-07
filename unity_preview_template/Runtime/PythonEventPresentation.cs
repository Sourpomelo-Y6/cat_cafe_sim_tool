using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonEventPresentation : MonoBehaviour
    {
        private string instance, intakeKey, storeKey, growthKey;
        private string message;
        private int day, tick;
        public string Message { get { return message; } }
        public void Clear() { instance=intakeKey=storeKey=growthKey=message=null; }
        public void Apply(PythonCafeState state)
        {
            string nextIntake=state.intake_request!=null && state.intake_request.status=="waiting" ? state.intake_request.cat_id : null;
            string nextStore=state.store_event!=null ? state.store_event.kind : null;
            string nextGrowth=state.growth_choices!=null && state.growth_choices.Length>0 ? GrowthKey(state.growth_choices[0]) : null;
            bool same=instance==state.instance_id;
            bool resolved=same && ((intakeKey!=null && nextIntake==null) || (storeKey!=null && nextStore==null) || (growthKey!=null && nextGrowth!=growthKey));
            if(!same)message=null;
            else if(resolved)
            {
                day=state.day;tick=state.tick;
                message=state.closed ? "対応を反映しました。振り返りを確認して翌日へ。" : state.can_set_shifts ? "対応を反映しました。営業準備を続けられます。" : "対応を反映しました。自動進行は手動で再開できます。";
                if(!state.closed && !state.can_set_shifts && string.IsNullOrEmpty(state.required_action))GetComponent<PythonServiceScreen>()?.ShowSeats();
            }
            else if(day!=state.day || tick!=state.tick)message=null;
            instance=state.instance_id;intakeKey=nextIntake;storeKey=nextStore;growthKey=nextGrowth;
        }
        public static string GrowthKey(PythonGrowthChoice row) { return row.cat_id+"/"+row.kind+"/"+row.title+"/"+row.learned+"/"+row.group; }
        public static string Notice(PythonStateClient client,string prompt)
        { return !client.CanLeaveResult ? "回答を送信しています。完了までお待ちください。" : !client.CanManageSaves ? "Python未接続です。再接続後に回答できます。" : prompt; }
        public static bool HasDescriptions(Text[] texts) { return texts!=null && Array.Exists(texts,text=>text!=null); }
        public static void Choices(Text[] descriptions,PythonEventChoice[] rows)
        {
            if(descriptions==null)return;
            for(int i=0;i<descriptions.Length;i++)
            {
                if(descriptions[i]==null)continue;bool exists=rows!=null && i<rows.Length;
                var scroll=descriptions[i].GetComponentInParent<ScrollRect>();if(scroll!=null)scroll.gameObject.SetActive(exists);else descriptions[i].gameObject.SetActive(exists);
                if(!exists)continue;var row=rows[i];
                descriptions[i].text=(row.can_select ? "" : "選べません："+(string.IsNullOrEmpty(row.reason) ? "現在は選択できません。" : row.reason)+"\n")+row.detail;
                descriptions[i].color=row.can_select ? new Color(.16f,.25f,.21f) : new Color(.55f,.27f,.13f);
            }
        }
        public static void Reset(Text details,Text[] descriptions)
        {
            Canvas.ForceUpdateCanvases();var scroll=details.GetComponentInParent<ScrollRect>();if(scroll!=null)scroll.verticalNormalizedPosition=1;
            foreach(var text in descriptions ?? new Text[0]){if(text==null)continue;var area=text.GetComponentInParent<ScrollRect>();if(area!=null)area.verticalNormalizedPosition=1;}
        }
    }
}
