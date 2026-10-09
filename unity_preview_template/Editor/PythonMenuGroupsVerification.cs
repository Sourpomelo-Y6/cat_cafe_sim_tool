using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonMenuGroupsVerification
    {
        public static void BuildAndVerify()
        {
            PythonEventListSetup.Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="menu-groups";state.can_set_shifts=true;state.required_action="";
            state.pet_shop=new PythonPetShop{rows=new[]{new PythonShopCat{cat_id="shop-1",name="ハル",can_purchase=true,cost=200,funds_after=800,details="ハルの受け入れ候補"}}};
            state.recruitment=new PythonRecruitment{rows=new PythonRecruitmentCat[0],traits=new PythonRecruitmentOption[0],coats=new PythonRecruitmentOption[0],hairs=new PythonRecruitmentOption[0]};
            state.clear_results=new PythonClearResults{rows=new PythonClearRecord[0]};state.business_history=new PythonFinanceHistory{rows=new PythonFinanceDay[0]};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});apply();var menu=preview.GetComponent<PythonMenuPanel>();var list=preview.GetComponent<PythonEventListPanel>();menu.Open();
            if(menu.SelectedGroup!=0 || menu.tabs.Length!=4 || menu.entries.Length!=17)throw new Exception("Menu groups missing");
            int[] counts={3,6,4,4};for(int g=0;g<4;g++)
            {
                menu.tabs[g].onClick.Invoke();int count=0;for(int i=0;i<17;i++){if(menu.detailPanels[i]==null || menu.detailClose[i]==null)throw new Exception("Missing return route "+i);bool visible=menu.entries[i].gameObject.activeSelf;if(visible!=(PythonMenuPanel.Groups[i]==g))throw new Exception("Wrong category "+i);if(visible){count++;var r=menu.entries[i].GetComponent<RectTransform>();if(-r.anchoredPosition.y+r.sizeDelta.y>492)throw new Exception("Menu row overlaps notice");}}
                if(count!=counts[g] || !menu.title.text.Contains(PythonMenuPanel.GroupNames[g]))throw new Exception("Category contents mismatch");Capture(preview,"Logs/MenuGroup-"+g+".png");
            }
            if(typeof(PythonStateClient).GetField("pending",flags).GetValue(client)!=null)throw new Exception("Browsing sent command");
            menu.SelectGroup(1);menu.entries[9].onClick.Invoke();var shop=preview.GetComponent<PythonPetShopPanel>();if(menu.panel.activeSelf || !shop.panel.activeSelf || shop.close.GetComponentInChildren<Text>().text!="メニューへ戻る")throw new Exception("Shop return entry missing");
            shop.Purchase();shop.Cancel();if(!shop.panel.activeSelf || menu.panel.activeSelf)throw new Exception("Cancel left detail");shop.close.onClick.Invoke();if(!menu.panel.activeSelf || menu.SelectedGroup!=1 || shop.panel.activeSelf)throw new Exception("Return lost category");
            menu.Close();menu.Open();if(menu.SelectedGroup!=1)throw new Exception("Reopen lost category");menu.entries[9].onClick.Invoke();list.Open();if(menu.panel.activeSelf)throw new Exception("Events failed to close menu");shop.close.onClick.Invoke();if(menu.panel.activeSelf)throw new Exception("Old return leaked into event navigation");
            menu.Open();menu.SelectGroup(3);menu.entries[14].onClick.Invoke();var results=preview.GetComponent<PythonClearResultsPanel>();results.close.onClick.Invoke();if(!menu.panel.activeSelf || menu.SelectedGroup!=3)throw new Exception("Records return failed");
            menu.entries[15].onClick.Invoke();var finance=preview.GetComponent<PythonFinanceHistoryPanel>();finance.close.onClick.Invoke();if(!menu.panel.activeSelf || menu.SelectedGroup!=3)throw new Exception("Finance return failed");
            typeof(PythonStateClient).GetField("online",flags).SetValue(client,false);preview.PythonConnectionError("test");menu.SelectGroup(1);foreach(var entry in menu.entries)if(entry.gameObject.activeSelf && entry.interactable)throw new Exception("Offline command enabled");apply();
            menu.entries[9].onClick.Invoke();state.instance_id="loaded";apply();if(menu.panel.activeSelf || menu.SelectedGroup!=0)throw new Exception("New instance retained navigation");shop.close.onClick.Invoke();if(menu.panel.activeSelf)throw new Exception("Load retained old return");
            Debug.Log("CAT_CAFE_MENU_GROUPS_OK: all 17 routes / 4 categories / captions / return / cancel / events / offline / load / read only");
        }
        static void Capture(ContentPreview preview,string path){Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,path});}
    }
}
