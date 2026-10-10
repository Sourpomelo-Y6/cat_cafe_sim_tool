using System;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace CatCafePreview.Editor
{
    public static class PythonSaveBrowserVerification
    {
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        public static void BuildAndVerify()
        {
            PythonSaveBrowserLayout.Build();var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            foreach(var path in new[]{"Assets/Scenes/CatCafeTitle.unity","Assets/Scenes/CatCafePythonState.unity"})
            {
                EditorSceneManager.OpenScene(path);var b=UnityEngine.Object.FindObjectOfType<PythonSaveBrowser>();var c=b.GetComponent<PythonStateClient>();var title=b.GetComponent<PythonTitleScreen>();var state=PythonServiceSetup.Sample();state.instance_id="save-slot-ui";state.can_set_shifts=true;state.funds=1234;state.new_game_options=new PythonNewGameOption[0];ContentPreview preview=null;
                Action apply;if(c!=null){preview=c.GetComponent<ContentPreview>();preview.Initialize();typeof(PythonStateClient).GetField("preview",flags).SetValue(c,preview);apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(c,new object[]{state});}else apply=()=>title.Apply(state);apply();
                var one=new PythonSavedGame{slot=1,save_id="one",label="4日目 / 時刻 12 / 資金 900",objective_label="人気3段階",created_at="2026-10-10T00:00:00+00:00",phase="open"};var three=new PythonSavedGame{slot=3,save_id="three",label="2日目 / 資金 1100",created_at="2026-10-09T00:00:00+00:00",phase="preparation"};var legacy=new PythonSavedGame{save_id="legacy",label="以前の保存データ"};var list=new PythonSaveList{saves=new[]{three,one,legacy}};
                Action<PythonSaveList> lists=rows=>{if(c!=null)c.GetComponent<PythonSavePanel>().Apply(rows);else title.ApplySaves(rows);};lists(list);b.OpenLoad();Check(b.panel.activeSelf && b.action.interactable && b.details.text.Contains("人気3段階"),"Load slot display missing");Capture(preview,"SaveSlots"+(c==null?"Title":"Business"));
                b.cards[1].onClick.Invoke();Check(!b.action.interactable && b.details.text.Contains("空のスロット"),"Empty slot loaded");b.cards[0].onClick.Invoke();b.action.onClick.Invoke();Check(b.confirmation.activeSelf && b.confirmationText.text.Contains("未保存"),"Load confirmation missing");b.cancel.onClick.Invoke();Check(!b.confirmation.activeSelf,"Load cancel failed");
                b.Choose();lists(new PythonSaveList{saves=new[]{legacy,one,three}});Check(b.confirmation.activeSelf,"Unchanged list invalidated selection");lists(new PythonSaveList{saves=new[]{legacy,new PythonSavedGame{slot=1,save_id="replaced",label="更新済み"},three}});Check(!b.confirmation.activeSelf,"Replaced slot kept stale confirmation");lists(list);
                b.Next();b.Next();b.cards[2].onClick.Invoke();Check(b.details.text.Contains("以前の保存データ") && b.action.interactable,"Legacy save unreachable");b.Previous();b.Previous();b.cards[0].onClick.Invoke();b.Choose();state.revision++;apply();Check(!b.confirmation.activeSelf,"State update retained confirmation");
                if(c!=null)
                {
                    Func<PythonShiftCommand> pending=()=>typeof(PythonStateClient).GetField("pending",flags).GetValue(c) as PythonShiftCommand;Action clear=()=>typeof(PythonStateClient).GetField("pending",flags).SetValue(c,null);
                    b.OpenSave();b.Select(1);b.Choose();Check(pending()==null && b.confirmationText.text.Contains("スロット 2"),"Empty slot sent early");b.CancelAndRefresh();Check(pending()==null,"Cancel sent save");b.Choose();b.Confirm();Check(pending()?.kind=="save_game" && pending().save_slot==2,"Slot save payload wrong");string request=pending().request_id;b.Confirm();Check(pending().request_id==request,"Duplicate slot save");clear();apply();
                    b.Select(0);b.Choose();Check(b.confirmationText.text.Contains("上書き") && b.confirmationText.text.Contains("人気3段階"),"Overwrite target not shown");Capture(preview,"SaveSlotsOverwrite");b.Confirm();Check(pending()?.save_slot==1,"Overwrite target changed");clear();apply();b.OpenLoad();b.Select(2);b.Choose();b.Confirm();Check(pending()?.kind=="load_game" && pending().save_id=="three","Load ID changed");clear();apply();
                    b.Notify("保存に失敗しました。再試行してください。");apply();Check(b.notice.text.Contains("失敗"),"Save failure disappeared");typeof(PythonStateClient).GetField("online",flags).SetValue(c,false);b.Refresh();Check(!b.action.interactable && !b.confirm.interactable,"Offline slot enabled");typeof(PythonStateClient).GetField("online",flags).SetValue(c,true);apply();
                }
                else{title.Disconnect("未接続");b.Refresh();Check(!b.action.interactable,"Offline title load enabled");apply();}
                b.Unavailable();Check(!b.action.interactable,"Unavailable list enabled overwrite");lists(list);b.Close();Check(!b.panel.activeSelf,"Close failed");
            }
            Debug.Log("CAT_CAFE_SAVE_SLOTS_VERIFIED: title / save-load tabs / empty and occupied slots / paging / legacy saves / cancel / overwrite / stable IDs / stale confirmations / offline / unavailable list / command payloads");
        }
        static void Capture(ContentPreview preview,string name){Canvas.ForceUpdateCanvases();if(preview!=null)typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/"+name+".png"});else typeof(PythonTitleSetup).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{"Logs/"+name+".png"});}
    }
}
