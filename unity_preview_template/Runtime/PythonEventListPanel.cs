using System.Linq;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonEventListPanel : MonoBehaviour
    {
        public GameObject panel;
        public Button open, close;
        public Button[] entries;
        public Text notice;
        private PythonCafeState current;
        private bool online;
        private string instance;
        public void Open(){if(current==null)return;panel.SetActive(true);panel.transform.SetAsLastSibling();RefreshControls();}
        public void Close(){panel.SetActive(false);}
        public void Visiting(){Show(0);} public void Intake(){Show(1);} public void Store(){Show(2);} public void Growth(){Show(3);}
        public void Missing(){Show(4);} public void Adoption(){Show(5);} public void Dispatch(){Show(6);}
        private bool Available(int i)
        {
            if(current==null)return false;
            switch(i){
                case 0:return current.visiting_cat!=null && current.can_set_shifts && !current.closed && current.game_over==null;
                case 1:return current.intake_request?.status=="waiting";
                case 2:return current.store_event!=null;
                case 3:return current.growth_choices!=null && current.growth_choices.Length>0;
                case 4:return current.missing_cats!=null && current.missing_cats.Length>0;
                case 5:return current.adoption!=null;
                case 6:return current.patron_dispatch?.events?.Length>0 || current.patron_dispatch?.options?.Length>0;
                default:return false;
            }
        }
        private void Show(int i)
        {
            if(!online || !GetComponent<PythonStateClient>().CanManageSaves || !Available(i))return;
            GameObject target=null;
            switch(i){
                case 0:var v=GetComponent<PythonVisitingCatPanel>();v.Open();target=v.panel;break;
                case 1:target=GetComponent<PythonIntakePanel>().panel;break;
                case 2:target=GetComponent<PythonStoreEventPanel>().panel;break;
                case 3:target=GetComponent<PythonGrowthPanel>().panel;break;
                case 4:var m=GetComponent<PythonMissingPanel>();m.Open(current.missing_cats.FirstOrDefault(r=>r.status=="waiting")?.cat_id ?? current.missing_cats[0].cat_id);target=m.panel;break;
                case 5:var a=GetComponent<PythonAdoptionPanel>();a.Open(current.adoption.events?.FirstOrDefault(r=>r.status=="waiting")?.cat_id ?? "");target=a.panel;break;
                case 6:var d=GetComponent<PythonDispatchPanel>();d.Open(current.patron_dispatch.events?.FirstOrDefault(r=>r.status=="waiting")?.cat_id ?? current.patron_dispatch.options?.FirstOrDefault()?.cat_id ?? current.patron_dispatch.events?.FirstOrDefault()?.cat_id);target=d.panel;break;
            }
            if(target!=null){Close();target.SetActive(true);target.transform.SetAsLastSibling();}
        }
        public void Apply(PythonCafeState s){if(instance!=s.instance_id){instance=s.instance_id;Close();}current=s;online=true;RefreshControls();}
        public void RefreshControls()
        {
            var client=GetComponent<PythonStateClient>();open.interactable=current!=null;close.interactable=true;if(current==null)return;
            var v=current.visiting_cat;var intake=current.intake_request;var missing=current.missing_cats ?? new PythonMissingCat[0];var adoption=current.adoption?.events ?? new PythonAdoptionEvent[0];var dispatch=current.patron_dispatch?.events ?? new PythonDispatchEvent[0];
            var captions=new[]{
                "店先に通う猫："+(v==null ? "対象なし" : v.status=="untriggered" ? "出現前・人気第1段階達成の翌日から" : v.name+" / "+(v.status=="accepted" ? "加入済み" : v.progress+"/"+v.required+"回")+(!current.can_set_shifts ? "（営業準備中に確認）" : "")),
                "保護猫の受け入れ："+(intake==null ? "依頼なし" : intake.name+" / "+(intake.status=="waiting" ? "確認待ち" : intake.status=="accepted" ? "受け入れ済み" : "見送り済み")),
                "店舗イベント："+(current.store_event==null ? "確認待ちなし" : current.store_event.title+" / 確認待ち"),
                "猫の成長："+(current.growth_choices?.Length ?? 0)+"件の選択待ち",
                "猫の家出・帰還："+missing.Count(r=>r.status=="waiting")+"件の帰還待ち / 家出中 "+missing.Count(r=>r.status=="missing")+"匹 / 記録 "+missing.Length+"件",
                "譲渡の申し出："+adoption.Count(r=>r.status=="waiting")+"件の確認待ち / 記録 "+adoption.Length+"件",
                "有力者への派遣："+dispatch.Count(r=>r.status=="waiting")+"件の帰還待ち / 派遣中 "+dispatch.Count(r=>r.status=="travelling")+"匹"};
            for(int i=0;i<entries.Length;i++){entries[i].GetComponentInChildren<Text>().text=captions[i];entries[i].interactable=online && client.CanManageSaves && Available(i);}
            notice.text=!online ? "Python未接続です。再接続後にイベントを開けます。" : !client.CanManageSaves ? "操作を送信しています。完了するまでお待ちください。" : "項目を選んで確認します。店先の猫は任意です。確認待ちに回答すると営業を再開できます。";
        }
        public void Disconnect(){online=false;RefreshControls();}
    }
}
