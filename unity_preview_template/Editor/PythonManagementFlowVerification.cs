using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    [InitializeOnLoad] public static class PythonManagementFlowVerification
    {
        const string Key="CatCafeManagementLive";
        static PythonManagementFlowVerification(){EditorApplication.update+=Update;}
        static void Check(bool value,string text){if(!value)throw new Exception(text);}
        public static void Run()
        {
            int port=int.Parse(Environment.GetEnvironmentVariable("CAT_CAFE_FLOW_TEST_PORT"));Check(port>0 && port<=65535 && port!=8190,"Use an isolated live server");EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var client=UnityEngine.Object.FindObjectOfType<PythonStateClient>();var serialized=new SerializedObject(client);serialized.FindProperty("port").intValue=port;serialized.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.SaveScene(client.gameObject.scene);SessionState.SetInt(Key,1);SessionState.SetFloat(Key+"deadline",(float)EditorApplication.timeSinceStartup+240);EditorApplication.EnterPlaymode();
        }
        static string Snapshot(PythonCafeState state)=>JsonUtility.ToJson(new PythonCafeState{funds=state.funds,cats=state.cats,items=state.items,expansion=state.expansion,rest_space=state.rest_space,seat_equipment=state.seat_equipment,general_dispatch=state.general_dispatch});
        static void Capture(ContentPreview preview,string name){Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/"+name+".png"});}
        static void Move(int next){SessionState.SetInt(Key,next);Debug.Log("CAT_CAFE_MANAGEMENT_STAGE:"+next);}
        static void Update()
        {
            int stage=SessionState.GetInt(Key,0);if(stage==0)return;
            try
            {
                Check(EditorApplication.timeSinceStartup<SessionState.GetFloat(Key+"deadline",0),"Management live timeout stage "+stage);if(!EditorApplication.isPlaying)return;var client=UnityEngine.Object.FindObjectOfType<PythonStateClient>();if(client==null || !client.CanManageSaves)return;var preview=client.GetComponent<ContentPreview>();var state=client.CurrentState;Check(state.game_over==null,"Unexpected end");var menu=preview.GetComponent<PythonMenuPanel>();var browser=preview.GetComponent<PythonSaveBrowser>();var expansion=preview.GetComponent<PythonExpansionPanel>();var rest=preview.GetComponent<PythonRestSpacePanel>();var equipment=preview.GetComponent<PythonSeatEquipmentPanel>();var items=preview.GetComponent<PythonItemsPanel>();var general=preview.GetComponent<PythonGeneralDispatchPanel>();
                if(stage==1){preview.GetComponent<PythonPreparationPanel>().Close();menu.Open();menu.SelectGroup(2);menu.entries[4].onClick.Invoke();Check(expansion.panel.activeSelf && expansion.proceed.interactable,"Expansion menu entry");SessionState.SetFloat(Key+"funds",state.expansion.funds_after);expansion.proceed.onClick.Invoke();Check(expansion.confirmation.activeSelf,"Expansion confirm absent");expansion.cancel.onClick.Invoke();Check(state.expansion.seats==2,"Cancel expanded seats");expansion.proceed.onClick.Invoke();expansion.confirm.onClick.Invoke();Check(!expansion.proceed.interactable,"Expansion duplicate allowed");Move(2);}
                else if(stage==2){Check(state.expansion.seats==3 && state.funds==SessionState.GetFloat(Key+"funds",0),"Expansion result");expansion.close.onClick.Invoke();Check(menu.panel.activeSelf && menu.SelectedGroup==2,"Expansion menu return");menu.entries[6].onClick.Invoke();SessionState.SetFloat(Key+"funds",Array.Find(state.rest_space.actions,r=>r.kind=="purchase_rest_space").funds_after);rest.purchase.onClick.Invoke();rest.confirm.onClick.Invoke();Move(3);}
                else if(stage==3){Check(state.rest_space.recovery_bonus>0 && state.funds==SessionState.GetFloat(Key+"funds",0),"Rest purchase result");rest.close.onClick.Invoke();menu.entries[5].onClick.Invoke();SessionState.SetFloat(Key+"funds",state.seat_equipment.catalog[0].funds_after);equipment.purchase.onClick.Invoke();equipment.confirm.onClick.Invoke();Move(4);}
                else if(stage==4){Check(state.seat_equipment.owned.Length==1 && state.funds==SessionState.GetFloat(Key+"funds",0),"Equipment purchase result");equipment.close.onClick.Invoke();menu.SelectGroup(1);menu.entries[3].onClick.Invoke();items.modeTabs[1].onClick.Invoke();SessionState.SetFloat(Key+"funds",state.items.shop[0].funds_after);items.buy.onClick.Invoke();items.confirm.onClick.Invoke();Move(5);}
                else if(stage==5){Check(state.items.count==1 && state.funds==SessionState.GetFloat(Key+"funds",0),"Item purchase result");items.modeTabs[0].onClick.Invoke();Check(items.use.interactable,"Care target unavailable");SessionState.SetString(Key+"cat",state.items.owned[0].targets[items.cats.value].cat_id);SessionState.SetFloat(Key+"stress",Array.Find(state.cats,c=>c.cat_id==SessionState.GetString(Key+"cat","")).stress);items.use.onClick.Invoke();Check(items.confirmationText.text.Contains("1 → 0"),"Care confirmation count");items.confirm.onClick.Invoke();Move(6);}
                else if(stage==6){Check(state.items.count==0 && Array.Find(state.cats,c=>c.cat_id==SessionState.GetString(Key+"cat","")).stress<SessionState.GetFloat(Key+"stress",0),"Care result");items.ShowHistory();Check(items.details.text.Contains("使用"),"Care history missing");Capture(preview,"LiveManagementItems");items.close.onClick.Invoke();menu.Open();menu.SelectGroup(1);menu.entries[12].onClick.Invoke();Check(general.send.interactable,"General dispatch unavailable");general.send.onClick.Invoke();Check(general.send.interactable,"Dispatch confirmation sent early");general.send.onClick.Invoke();Check(!general.send.interactable,"Duplicate dispatch enabled");Move(7);}
                else if(stage==7){Check(state.general_dispatch.events.Length==1 && state.general_dispatch.events[0].status=="travelling","Departure result");Capture(preview,"LiveManagementDispatch");general.Close();browser.OpenSave();browser.cards[0].onClick.Invoke();if(!browser.action.interactable)return;SessionState.SetString(Key+"snapshot",Snapshot(state));SessionState.SetInt(Key+"revision",state.revision);browser.Choose();browser.Confirm();Move(8);}
                else if(stage==8 && state.revision>SessionState.GetInt(Key+"revision",0)){browser.OpenLoad();browser.cards[0].onClick.Invoke();if(!browser.action.interactable)return;browser.Choose();browser.Confirm();Move(9);}
                else if(stage==9){Check(Snapshot(state)==SessionState.GetString(Key+"snapshot",""),"Management save restore mismatch");Check(!general.panel.activeSelf && !items.confirmation.activeSelf && !rest.confirmation.activeSelf,"Load retained dialog");var dayoff=preview.GetComponent<PythonDayOffPanel>();dayoff.Open();dayoff.Choose();Check(dayoff.confirm.interactable,"Day off for return unavailable");dayoff.confirm.onClick.Invoke();Move(10);}
                else if(stage==10){var ev=state.general_dispatch.events[0];Check(ev.status=="waiting","Dispatch did not return");var list=preview.GetComponent<PythonEventListPanel>();list.open.onClick.Invoke();list.entries[10].onClick.Invoke();general.OpenReturn(ev.choice);Check(general.receive.interactable,"Return action unavailable");general.receive.onClick.Invoke();Move(11);}
                else if(stage==11){Check(state.general_dispatch.events[0].status=="resolved","Return reward unresolved");Capture(preview,"LiveManagementReturn");Debug.Log("CAT_CAFE_MANAGEMENT_LIVE_OK: menu / expansion / cancel / rest / equipment / item purchase and care / history / dispatch confirmation / slot save-load / reset / return and reward");SessionState.SetInt(Key,0);EditorApplication.Exit(0);}
            }
            catch(Exception ex){SessionState.SetInt(Key,0);Debug.LogException(ex);EditorApplication.Exit(1);}
        }
    }
}
