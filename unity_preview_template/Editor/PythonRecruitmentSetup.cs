using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonRecruitmentSetup
    {
        static T Copy<T>(T source,Transform root,Transform target) where T:Component{return target.Find(AnimationUtility.CalculateTransformPath(source.transform,root)).GetComponent<T>();}
        static void Bind(Button b,UnityEngine.Events.UnityAction action){b.onClick=new Button.ButtonClickedEvent();UnityEventTools.AddPersistentListener(b.onClick,action);b.onClick.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);}
        [MenuItem("Cat Cafe/開発用/UI再生成/保護猫紹介",false,37)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var source=preview.GetComponent<PythonPetShopPanel>();var root=source.panel.transform.parent;var old=root.Find("Recruitment");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            var p=preview.GetComponent<PythonRecruitmentPanel>() ?? preview.gameObject.AddComponent<PythonRecruitmentPanel>();p.panel=UnityEngine.Object.Instantiate(source.panel,root);p.panel.name="Recruitment";var origin=source.panel.transform;var target=p.panel.transform;
            p.picture=Copy(source.picture,origin,target);p.title=Copy(source.title,origin,target);p.details=Copy(source.details,origin,target);p.notice=Copy(source.notice,origin,target);p.confirmationText=Copy(source.confirmationText,origin,target);p.confirmation=target.Find(AnimationUtility.CalculateTransformPath(source.confirmation.transform,origin)).gameObject;
            p.previous=Copy(source.previous,origin,target);p.next=Copy(source.next,origin,target);p.purchase=Copy(source.purchase,origin,target);p.housing=Copy(source.housing,origin,target);p.close=Copy(source.close,origin,target);p.confirm=Copy(source.confirm,origin,target);p.cancel=Copy(source.cancel,origin,target);
            p.previous.GetComponentInChildren<Text>().text="前の候補";p.next.GetComponentInChildren<Text>().text="次の候補";p.purchase.GetComponentInChildren<Text>().text="受け入れる（確認）";p.close.GetComponentInChildren<Text>().text="閉じる";p.notice.fontSize=14;
            Bind(p.previous,p.Previous);Bind(p.next,p.Next);Bind(p.purchase,p.Purchase);Bind(p.housing,p.Housing);Bind(p.close,p.Close);Bind(p.confirm,p.Confirm);Bind(p.cancel,p.Cancel);
            var card=p.title.transform.parent;
            Place(p.picture.GetComponent<RectTransform>(),32,128,232,232);
            var area=p.details.GetComponentInParent<ScrollRect>(true);Place(area.GetComponent<RectTransform>(),292,128,624,294);Place(area.viewport,8,6,594,282);Place(p.details.rectTransform,0,0,594,282);Place(area.verticalScrollbar.GetComponent<RectTransform>(),606,6,12,282);
            p.trait=Filter(card,"Trait",24,76,280);p.coat=Filter(card,"Coat",316,76,290);p.hair=Filter(card,"Hair",626,76,290);
            p.trait.onValueChanged=new Dropdown.DropdownEvent();p.coat.onValueChanged=new Dropdown.DropdownEvent();p.hair.onValueChanged=new Dropdown.DropdownEvent();
            UnityEventTools.AddPersistentListener(p.trait.onValueChanged,p.FilterValue);UnityEventTools.AddPersistentListener(p.coat.onValueChanged,p.FilterValue);UnityEventTools.AddPersistentListener(p.hair.onValueChanged,p.FilterValue);
            foreach(var d in new[]{p.trait,p.coat,p.hair})d.onValueChanged.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
            var toggle=DefaultControls.CreateToggle(new DefaultControls.Resources());toggle.name="Show Accepted";toggle.transform.SetParent(card,false);Place(toggle.GetComponent<RectTransform>(),24,366,248,26);p.accepted=toggle.GetComponent<Toggle>();p.accepted.isOn=false;p.accepted.graphic.color=new Color(.2f,.4f,.25f);var label=toggle.GetComponentInChildren<Text>();label.font=ContentPreviewLayout.LoadFont();label.fontSize=16;label.text="受け入れ済みも表示";UnityEventTools.AddPersistentListener(p.accepted.onValueChanged,p.FilterAccepted);p.accepted.onValueChanged.SetPersistentListenerState(0,UnityEngine.Events.UnityEventCallState.EditorAndRuntime);
            var button=DefaultControls.CreateButton(new DefaultControls.Resources());button.name="Reset Filters";button.transform.SetParent(card,false);Place(button.GetComponent<RectTransform>(),24,398,248,28);p.reset=button.GetComponent<Button>();label=button.GetComponentInChildren<Text>();label.font=ContentPreviewLayout.LoadFont();label.fontSize=16;label.text="絞り込みを解除";Bind(p.reset,p.ResetFilters);
            p.confirmation.SetActive(false);p.panel.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);PythonEventListSetup.Build();Debug.Log("CAT_CAFE_RECRUITMENT_UI_OK");
        }
        static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
        static Dropdown Filter(Transform root,string name,float x,float y,float w){var g=DefaultControls.CreateDropdown(new DefaultControls.Resources());g.name=name;g.transform.SetParent(root,false);Place(g.GetComponent<RectTransform>(),x,y,w,36);foreach(var t in g.GetComponentsInChildren<Text>(true)){t.font=ContentPreviewLayout.LoadFont();t.fontSize=16;}var d=g.GetComponent<Dropdown>();d.ClearOptions();d.AddOptions(new System.Collections.Generic.List<string>{"すべて"});return d;}
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var f=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",f).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="recruitment";state.required_action="";state.recruitment=new PythonRecruitment{housing="在籍5匹 / 上限6匹 / 空き1匹",notes="3日ごとに候補を3匹追加",traits=new[]{new PythonRecruitmentOption{id="calm",label="穏やか"}},coats=new[]{new PythonRecruitmentOption{id="white",label="白猫"}},hairs=new[]{new PythonRecruitmentOption{id="short_hair",label="短毛"}},rows=new[]{new PythonRecruitmentCat{cat_id="rescue-1",name="ハル",cost=200,funds_after=800,can_purchase=true,trait_id="calm",features=new[]{"white","short_hair"},details="ハル / 白猫 / 短毛 / 穏やか"},new PythonRecruitmentCat{cat_id="rescue-2",name="リン",can_purchase=false,features=new[]{"black"},reason="資金が足りません。",details="リン"},new PythonRecruitmentCat{cat_id="rescue-3",name="受け入れ済み",accepted=true,accepted_day=1,features=new string[0]}}};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",f).Invoke(client,new object[]{state});apply();var p=preview.GetComponent<PythonRecruitmentPanel>();var menu=preview.GetComponent<PythonMenuPanel>();menu.Open();menu.Select(16);if(menu.panel.activeSelf || !p.panel.activeSelf || !p.title.text.Contains("1/2") || !p.purchase.interactable || p.picture.texture==null)throw new Exception("Recruitment menu/default filter failed");
            p.coat.value=1;if(p.next.interactable || !p.title.text.Contains("1/1"))throw new Exception("Coat filter failed");p.hair.value=1;p.trait.value=1;if(!p.purchase.interactable)throw new Exception("Combined filters failed");p.ResetFilters();p.accepted.isOn=true;p.Next();p.Next();if(!p.title.text.Contains("受け入れ済み") || p.purchase.interactable)throw new Exception("Accepted visibility failed");p.ResetFilters();p.Previous();p.Purchase();if(!p.confirmation.activeSelf || !p.confirmationText.text.Contains("800"))throw new Exception("Confirmation missing");p.Cancel();if(typeof(PythonStateClient).GetField("pending",f).GetValue(client)!=null)throw new Exception("Cancel queued command");p.Purchase();state.revision++;apply();if(p.confirmation.activeSelf)throw new Exception("Stale confirmation retained");p.Next();if(p.purchase.interactable || !p.notice.text.Contains("資金"))throw new Exception("Funds guard missing");p.Disconnect();if(p.next.interactable || p.trait.interactable || p.purchase.interactable)throw new Exception("Offline guard missing");apply();p.ResetFilters();p.Previous();
            Canvas.ForceUpdateCanvases();typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/Recruitment.png"});
            p.Purchase();p.Confirm();var cmd=(PythonShiftCommand)typeof(PythonStateClient).GetField("pending",f).GetValue(client);if(cmd==null || cmd.kind!="recruit_cat" || cmd.cat_id!="rescue-1")throw new Exception("Recruit command wrong");Debug.Log("CAT_CAFE_RECRUITMENT_DISPLAY_OK");
        }
    }
}
