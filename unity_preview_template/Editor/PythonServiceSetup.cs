using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview.Editor
{
    public static class PythonServiceSetup
    {
        static Button Tab(Transform root,string name,string label,float x)
        {
            var existing = root.Find(name); if(existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var obj = DefaultControls.CreateButton(new DefaultControls.Resources()); obj.name = name; obj.transform.SetParent(root,false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            rect.anchoredPosition=new Vector2(x,-128); rect.sizeDelta=new Vector2(160,40);
            var text=obj.GetComponentInChildren<Text>(); text.font=ContentPreviewLayout.LoadFont(); text.fontSize=20; text.text=label;
            return obj.GetComponent<Button>();
        }
        public static PythonCafeState Sample()
        {
            string[] ids={"mike","tama","sora","kohaku","mugi"}; string[] names={"ミケ","タマ","ソラ","コハク","ムギ"};
            var state=new PythonCafeState {version=1,phase="open",day=1,tick=8,opening_ticks=40,funds=1000,cats=new PythonCatState[5],
                seats=new[]{new PythonSeatState{seat_id="seat-1",cat_id="cat-mike",customer_id="guest-1",customer_name="佐藤さん"},
                            new PythonSeatState{seat_id="seat-2",cat_id="cat-tama",customer_id="guest-2",customer_name="鈴木さん"},
                            new PythonSeatState{seat_id="seat-3"}},
                customers=new[]{new PythonCustomerState{customer_id="guest-1",name="佐藤さん",status="seated",seat_id="seat-1"},
                                new PythonCustomerState{customer_id="guest-2",name="鈴木さん",status="seated",seat_id="seat-2"},
                                new PythonCustomerState{customer_id="guest-3",name="高橋さん",status="waiting"},
                                new PythonCustomerState{customer_id="guest-4",name="田中さん",status="departed"}}};
            for(int i=0;i<5;i++) state.cats[i]=new PythonCatState{cat_id="cat-"+ids[i],content_cat_id="playtest-"+ids[i],name=names[i],working=true,health_status="healthy",activity="cafe",stamina=85,max_stamina=100};
            Array.Resize(ref state.seats,8);
            for(int i=3;i<8;i++) state.seats[i]=new PythonSeatState{seat_id="seat-"+(i+1)};
            return state;
        }
        public static void BuildAndVerify()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>(); var roster=preview.GetComponent<PythonCatRoster>();
            var root=preview.transform.Find("Content Canvas/Background");
            var existing=root.Find("Seat List Scroll"); if(existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var panel=UnityEngine.Object.Instantiate(root.Find("Cat Roster Scroll").gameObject,root); panel.name="Seat List Scroll";
            foreach(var card in panel.GetComponentsInChildren<PythonCatCard>(true)) UnityEngine.Object.DestroyImmediate(card.gameObject);
            var service=preview.GetComponent<PythonServiceScreen>() ?? preview.gameObject.AddComponent<PythonServiceScreen>();
            service.rosterPanel=root.Find("Cat Roster Scroll").gameObject; service.seatPanel=panel;
            service.seatContent=panel.GetComponent<ScrollRect>().content; service.cardPrefab=roster.cardPrefab;
            var panelRect=panel.GetComponent<RectTransform>(); panelRect.sizeDelta=new Vector2(944,550);
            var viewport=panel.GetComponent<ScrollRect>().viewport; viewport.sizeDelta=new Vector2(944,550);
            service.seatContent.sizeDelta=new Vector2(944,550);
            var seatTemplate=UnityEngine.Object.Instantiate(roster.cardPrefab);
            seatTemplate.name="Seat Card";
            seatTemplate.GetComponent<RectTransform>().sizeDelta=SeatListLayout.CardSize;
            var nameRect=seatTemplate.catName.rectTransform;nameRect.anchoredPosition=new Vector2(10,-8);nameRect.sizeDelta=new Vector2(190,30);seatTemplate.catName.fontSize=18;
            seatTemplate.picture.rectTransform.anchoredPosition=new Vector2(59,-44);seatTemplate.picture.rectTransform.sizeDelta=new Vector2(112,96);
            var iconRect=seatTemplate.customerIcon.GetComponent<RectTransform>();iconRect.anchoredPosition=new Vector2(200,-10);iconRect.sizeDelta=new Vector2(24,24);
            SeatListLayout.StyleCard(seatTemplate);
            const string seatPrefab="Assets/CatCafePreview/Prefabs/PythonSeatCard.prefab";
            PrefabUtility.SaveAsPrefabAsset(seatTemplate.gameObject,seatPrefab); UnityEngine.Object.DestroyImmediate(seatTemplate.gameObject);
            service.cardPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(seatPrefab).GetComponent<PythonCatCard>();
            service.detailPanel=root.Find("Description Scroll").GetComponent<RectTransform>();
            service.detailText=service.detailPanel.GetComponentInChildren<Text>();
            service.heading=root.Find("Roster Heading").GetComponent<Text>();
            SeatListLayout.Configure(service);
            service.seatsTab=Tab(root,"Seats Tab","席一覧",384); service.catsTab=Tab(root,"Cats Tab","猫一覧",562);
            UnityEventTools.AddPersistentListener(service.seatsTab.onClick,service.ShowSeats); UnityEventTools.AddPersistentListener(service.catsTab.onClick,service.ShowCats);
            preview.Initialize(); var state=Sample(); preview.ApplyPythonState(state);
            if(!service.ShowingSeats || service.OccupiedCount!=2 || !preview.DisplayText.Contains("待機中") || !preview.DisplayText.Contains("退店済み")) throw new Exception("Service display mismatch");
            service.ShowCats(); if(service.ShowingSeats || !service.rosterPanel.activeSelf) throw new Exception("Cat tab mismatch");
            service.ShowSeats(); if(!service.ShowingSeats) throw new Exception("Seat tab mismatch");
            state.closed=true; state.finance=new PythonFinanceState(); preview.ApplyPythonState(state);
            if(service.ShowingSeats || !service.rosterPanel.activeSelf || !service.heading.text.Contains("閉店結果")) throw new Exception("Closing screen mismatch");
            state.closed=false; state.can_set_shifts=true; state.tick=0; state.seats=new PythonSeatState[0]; state.customers=new PythonCustomerState[0]; preview.ApplyPythonState(state);
            if(service.ShowingSeats) throw new Exception("Preparation screen mismatch");
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Debug.Log("CAT_CAFE_SERVICE_OK: preparation cats / open seats / customer names and statuses / tabs / closing result");
        }
        public static void BuildAndCapture()
        {
            BuildAndVerify(); var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>(); preview.ApplyPythonState(Sample()); Canvas.ForceUpdateCanvases();
            typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic)
                .Invoke(null,new object[]{preview,"Logs/ServiceScreen.png"});
        }
    }
}
