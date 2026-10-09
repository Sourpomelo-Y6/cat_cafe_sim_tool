using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    // Stable route IDs are shared with the existing scene and editor verification helpers.
    public class PythonMenuPanel : MonoBehaviour
    {
        public static readonly int[] Routes={24,23,21,14,15,16,17,18,19,20,22,6,10,25,26};
        public GameObject panel;public Button open,close;public Button[] entries;public Text notice;
        private PythonEventListPanel List=>GetComponent<PythonEventListPanel>();
        public void Open(){GetComponent<PythonBusinessLogPanel>()?.Close();GetComponent<PythonClearResultsPanel>()?.Close();List.Close();panel.SetActive(true);panel.transform.SetAsLastSibling();RefreshControls();}
        public void Close(){panel.SetActive(false);}
        public void Select(int index){if(index>=0 && index<Routes.Length){if(Routes[index]==26)GetComponent<PythonClearResultsPanel>()?.Open();else if(Routes[index]==25)GetComponent<PythonBusinessLogPanel>()?.Open();else List.ShowMenu(Routes[index]);}}
        public void Apply(PythonCafeState state){if(instance!=state.instance_id){instance=state.instance_id;Close();}RefreshControls();}
        private string instance;
        public void RefreshControls(){if(entries==null || List.entries==null)return;open.interactable=GetComponent<PythonStateClient>().CurrentState!=null;for(int i=0;i<entries.Length;i++){int route=Routes[i];entries[i].GetComponentInChildren<Text>().text=route==26 ? "クリア記録：達成時の成果を見返す" : route==25 ? "営業ログ・詳細リプレイ・ファイル出力" : route==6 ? "有力者への派遣：猫・行き先を選ぶ" : route==10 ? "一般派遣：猫・行き先を選ぶ" : List.entries[route].GetComponentInChildren<Text>().text;entries[i].interactable=route==26 ? GetComponent<PythonStateClient>().CanManageSaves && GetComponent<PythonClearResultsPanel>()!=null : route==25 ? GetComponent<PythonStateClient>().CanManageSaves && GetComponent<PythonBusinessLogPanel>()!=null : List.MenuAvailable(route);}notice.text=GetComponent<PythonStateClient>().AutoPlayRunning ? "おまかせ実行中です。詳細を確認できます。設定の変更は停止後に行えます。" : "経営の設定・設備・名簿を選びます。猫との出会いや回答待ちは「イベント一覧」で確認できます。";}
    }
}
