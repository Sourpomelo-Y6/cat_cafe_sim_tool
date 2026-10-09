using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview
{
    public class PythonMenuPanel : MonoBehaviour
    {
        // Keep route IDs and entry indices stable for existing scenes and verification helpers.
        public static readonly int[] Routes={24,23,21,14,15,16,17,18,19,20,22,6,10,25,26,27,28};
        public static readonly int[] Groups={0,0,0,1,2,2,2,2,3,1,1,1,1,3,3,3,1};
        public static readonly string[] GroupNames={"営業","猫・派遣","設備","記録"};
        public GameObject panel;public Button open,close;public Button[] entries,tabs,detailClose;
        public GameObject[] detailPanels;public Text title,notice;
        public int SelectedGroup=>group;
        private int group;private string instance,returnCaption;private GameObject returnPanel;private Button returnButton;
        private readonly HashSet<Button> bound=new HashSet<Button>();
        private PythonEventListPanel List=>GetComponent<PythonEventListPanel>();
        public void Open(){ClearReturnContext();CloseDetails();List.Close();panel.SetActive(true);panel.transform.SetAsLastSibling();RefreshControls();}
        public void Close(){ClearReturnContext();panel.SetActive(false);}
        public void CloseDetails()
        {
            GetComponent<PythonCatDetailsPanel>()?.Close();GetComponent<PythonAutoPlayPanel>()?.Close();GetComponent<PythonServiceAssignmentPanel>()?.Close();GetComponent<PythonObjectiveStartPanel>()?.Close();
            GetComponent<PythonItemsPanel>()?.Close();GetComponent<PythonExpansionPanel>()?.Close();GetComponent<PythonSeatEquipmentPanel>()?.Close();
            GetComponent<PythonRestSpacePanel>()?.Close();GetComponent<PythonWaitingAreaPanel>()?.Close();GetComponent<PythonCustomerDirectoryPanel>()?.Close();
            GetComponent<PythonPetShopPanel>()?.Close();GetComponent<PythonDispatchComparisonPanel>()?.Close();GetComponent<PythonDispatchPanel>()?.Close();
            GetComponent<PythonGeneralDispatchPanel>()?.Close();GetComponent<PythonBusinessLogPanel>()?.Close();GetComponent<PythonClearResultsPanel>()?.Close();
            GetComponent<PythonFinanceHistoryPanel>()?.Close();GetComponent<PythonRecruitmentPanel>()?.Close();
        }
        public void SelectGroup(int value){if(value<0 || value>=GroupNames.Length)return;group=value;RefreshControls();}
        public void Select(int index)
        {
            if(index<0 || index>=Routes.Length || !entries[index].interactable)return;
            group=Groups[index];ClearReturnContext();
            if(Routes[index]==28){Close();GetComponent<PythonRecruitmentPanel>()?.Open();}
            else if(Routes[index]==27)GetComponent<PythonFinanceHistoryPanel>()?.OpenBusiness();
            else if(Routes[index]==26)GetComponent<PythonClearResultsPanel>()?.Open();
            else if(Routes[index]==25)GetComponent<PythonBusinessLogPanel>()?.Open();else List.ShowMenu(Routes[index]);
            if(detailPanels!=null && index<detailPanels.Length && detailPanels[index]!=null && detailPanels[index].activeSelf)Track(detailPanels[index],detailClose[index]);
            RefreshControls();
        }
        private void Track(GameObject target,Button button){ClearReturnContext();returnPanel=target;returnButton=button;returnCaption=button.GetComponentInChildren<Text>().text;button.GetComponentInChildren<Text>().text="メニューへ戻る";}
        public void ClearReturnContext(){if(returnButton!=null)returnButton.GetComponentInChildren<Text>().text=returnCaption;returnPanel=null;returnButton=null;returnCaption=null;}
        public void TransferReturn(GameObject source,GameObject target)
        {
            if(returnPanel!=source || detailPanels==null)return;int i=Array.IndexOf(detailPanels,target);
            if(i>=0)Track(target,detailClose[i]);else ClearReturnContext();
        }
        private void ReturnFromDetail(GameObject source){if(returnPanel!=source)return;Open();}
        public void Apply(PythonCafeState state)
        {
            if(instance!=state.instance_id){instance=state.instance_id;group=0;Close();}
            if(detailClose!=null && detailPanels!=null)for(int i=0;i<detailClose.Length;i++)
            {var button=detailClose[i];var target=detailPanels[i];if(button!=null && bound.Add(button))button.onClick.AddListener(()=>ReturnFromDetail(target));}
            RefreshControls();
        }
        public void RefreshControls()
        {
            if(entries==null || List.entries==null)return;var client=GetComponent<PythonStateClient>();open.interactable=client.CurrentState!=null;
            if(title!=null)title.text="メニュー / "+GroupNames[group];
            if(tabs!=null)for(int i=0;i<tabs.Length;i++){tabs[i].interactable=client.CurrentState!=null;tabs[i].GetComponent<Image>().color=i==group ? new Color(.78f,.88f,.74f) : Color.white;}
            int row=0;
            for(int i=0;i<entries.Length;i++)
            {
                int route=Routes[i];bool visible=Groups[i]==group;entries[i].gameObject.SetActive(visible);
                if(visible){var rect=entries[i].GetComponent<RectTransform>();rect.anchoredPosition=new Vector2(24,-(148+row*52));row++;}
                entries[i].GetComponentInChildren<Text>().text=route==28 ? "保護猫紹介：候補巡回・絞り込み・受け入れ" : route==27 ? "営業成果・日次比較：人気・評価・離店" : route==26 ? "クリア記録：達成時の成果を見返す" : route==25 ? "営業ログ・詳細リプレイ・ファイル出力" : route==6 ? "有力者への派遣：猫・行き先を選ぶ" : route==10 ? "一般派遣：猫・行き先を選ぶ" : List.entries[route].GetComponentInChildren<Text>().text;
                entries[i].interactable=route==28 ? client.CanManageSaves && client.CurrentState.recruitment!=null && GetComponent<PythonRecruitmentPanel>()!=null : route==27 ? client.CanManageSaves && GetComponent<PythonFinanceHistoryPanel>()!=null : route==26 ? client.CanManageSaves && GetComponent<PythonClearResultsPanel>()!=null : route==25 ? client.CanManageSaves && GetComponent<PythonBusinessLogPanel>()!=null : List.MenuAvailable(route);
            }
            string[] hints={"営業の進め方・接客設定・目標を選びます。","猫を迎える・ケアする・派遣する操作を選びます。","お店の席・接客設備・休養・待合を整えます。","お客さんの名簿や営業の記録・成果を確認します。"};
            notice.text=hints[group]+"\n"+(client.AutoPlayRunning ? "おまかせ実行中です。設定の変更は停止後に行えます。" : "出会い・回答待ち・帰還は「イベント一覧」で確認できます。");
            if(returnButton!=null && returnPanel!=null && returnPanel.activeSelf)returnButton.GetComponentInChildren<Text>().text="メニューへ戻る";
        }
    }
}
