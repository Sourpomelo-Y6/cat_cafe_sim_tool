using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonServiceLayout
    {
        static void Place(RectTransform rect,float x,float y,float w,float h)
        { rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,-y);rect.sizeDelta=new Vector2(w,h); }
        static GameObject Box(Transform parent,string name,float x,float y,float w,float h,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
            Place(go.GetComponent<RectTransform>(),x,y,w,h);go.GetComponent<Image>().color=color;return go;
        }
        static Text Label(Transform parent,string name,float x,float y,float w,float h,int size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
            Place(go.GetComponent<RectTransform>(),x,y,w,h);var text=go.GetComponent<Text>();text.font=ContentPreviewLayout.LoadFont();text.fontSize=size;
            text.color=new Color(.16f,.21f,.18f);text.raycastTarget=false;text.supportRichText=false;return text;
        }
        [MenuItem("Cat Cafe/開発用/UI再生成/営業中の接客画面の配置",false,17)]
        public static void Build()
        {
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();var root=preview.transform.Find("Content Canvas/Background");var service=preview.GetComponent<PythonServiceScreen>();
            var old=root.Find("Service Status");if(old!=null)UnityEngine.Object.DestroyImmediate(old.gameObject);
            service.serviceStatus=Box(root,"Service Status",24,184,1232,66,new Color(.91f,.93f,.87f));
            service.serviceSummary=Label(service.serviceStatus.transform,"Summary",12,6,970,56,18);
            Label(service.serviceStatus.transform,"Time Heading",1032,4,188,22,14).text="営業時間の進み具合";
            var slider=DefaultControls.CreateSlider(new DefaultControls.Resources());slider.name="Day Progress";slider.transform.SetParent(service.serviceStatus.transform,false);Place(slider.GetComponent<RectTransform>(),1032,32,188,20);
            service.dayProgress=slider.GetComponent<Slider>();service.dayProgress.interactable=false;service.dayProgress.wholeNumbers=true;service.dayProgress.handleRect=null;UnityEngine.Object.DestroyImmediate(slider.transform.Find("Handle Slide Area").gameObject);
            service.dayProgress.fillRect.GetComponent<Image>().color=new Color(.2f,.38f,.3f);service.dayProgress.fillRect.GetComponent<Image>().raycastTarget=false;
            var rect=service.seatPanel.GetComponent<RectTransform>();Place(rect,24,262,944,514);var scroll=service.seatPanel.GetComponent<ScrollRect>();scroll.vertical=true;scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;
            scroll.viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,514);service.seatContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,944);
            foreach(var existing in service.seatContent.GetComponentsInChildren<PythonCatCard>(true))UnityEngine.Object.DestroyImmediate(existing.gameObject);
            var grid=service.seatContent.GetComponent<GridLayoutGroup>();grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=4;grid.cellSize=new Vector2(230,252);grid.spacing=new Vector2(8,8);
            string path=AssetDatabase.GetAssetPath(service.cardPrefab);var prefab=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var card=prefab.GetComponent<PythonCatCard>();card.GetComponent<RectTransform>().sizeDelta=new Vector2(230,252);
                Place(card.catName.rectTransform,10,8,190,30);card.catName.fontSize=18;
                Place(card.picture.rectTransform,59,44,112,96);Place(card.customerIcon.GetComponent<RectTransform>(),200,10,24,24);
                Place(card.stateText.rectTransform,10,146,210,72);card.stateText.fontSize=15;card.stateText.lineSpacing=1;card.stateText.alignment=TextAnchor.UpperLeft;card.stateText.verticalOverflow=VerticalWrapMode.Truncate;card.stateText.resizeTextForBestFit=true;card.stateText.resizeTextMinSize=12;card.stateText.resizeTextMaxSize=15;
                if(card.details==null){var button=DefaultControls.CreateButton(new DefaultControls.Resources());button.name="Assigned Cat Details";button.transform.SetParent(card.transform,false);card.details=button.GetComponent<Button>();card.details.GetComponentInChildren<Text>().font=ContentPreviewLayout.LoadFont();}
                if(card.details!=null){Place(card.details.GetComponent<RectTransform>(),10,220,210,30);card.details.GetComponentInChildren<Text>().fontSize=15;card.details.GetComponent<Image>().color=new Color(.88f,.91f,.84f);}
                card.work.gameObject.SetActive(false);card.rest.gameObject.SetActive(false);PrefabUtility.SaveAsPrefabAsset(prefab,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(prefab);}
            var auto=preview.GetComponent<PythonAutoPanel>();auto.toggle.GetComponentInChildren<Text>().fontSize=17;auto.speed.GetComponentInChildren<Text>().fontSize=16;
            auto.toggle.GetComponentInChildren<Text>().text="自動進行を開始";auto.speed.GetComponentInChildren<Text>().text="速度：通常";
            service.serviceStatus.SetActive(false);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);Debug.Log("CAT_CAFE_SERVICE_LAYOUT_OK");
        }
        public static void BuildAndVerify()
        {
            Build();var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
            var state=PythonServiceSetup.Sample();state.instance_id="service-layout";state.waiting_count=1;state.required_action="";state.objective_progress=new PythonObjectiveProgress{label="人気3段階",summary="第1段階：人気120 / 目標250",details="人気を育てて目標を達成しましょう。"};
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});apply();var service=preview.GetComponent<PythonServiceScreen>();
            if(!service.ShowingSeats || service.OccupiedCount!=2 || !service.serviceSummary.text.Contains("待ち 1人") || !service.GetSeatCard(0).stateText.text.Contains("佐藤さん") || !service.GetSeatCard(0).stateText.text.Contains("ミケ"))throw new Exception("Service seat or summary missing");
            if(service.GetSeatCard(0).details==null || service.GetSeatCard(0).details.GetComponent<RectTransform>().anchoredPosition!=new Vector2(10,-220))throw new Exception("Seat detail button layout overridden");
            if(service.GetSeatCard(2).details!=null && service.GetSeatCard(2).details.gameObject.activeSelf)throw new Exception("Empty seat details remained visible");
            if(service.GetSeatCard(0).details!=null){service.GetSeatCard(0).details.onClick.Invoke();if(!preview.GetComponent<PythonCatDetailsPanel>().title.text.Contains("ミケ"))throw new Exception("Wrong assigned cat details");preview.GetComponent<PythonCatDetailsPanel>().Close();}
            var auto=preview.GetComponent<PythonAutoPanel>();auto.Toggle();if(!service.serviceSummary.text.Contains("進行中"))throw new Exception("Automatic status stale");auto.ChangeSpeed();if(!service.serviceSummary.text.Contains("0.25秒"))throw new Exception("Speed status stale");auto.Toggle();
            Canvas.ForceUpdateCanvases();if(service.serviceSummary.preferredHeight>service.serviceSummary.rectTransform.rect.height+1)throw new Exception("Service summary clipped");
            typeof(ContentPreviewSetup).GetMethod("CaptureCanvas",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).Invoke(null,new object[]{preview,"Logs/ServiceLayout.png"});
            Array.Resize(ref state.seats,12);for(int i=8;i<12;i++)state.seats[i]=new PythonSeatState{seat_id="seat-"+(i+1)};apply();Canvas.ForceUpdateCanvases();if(service.seatContent.rect.height<=service.seatPanel.GetComponent<ScrollRect>().viewport.rect.height)throw new Exception("Expanded seats cannot scroll");
            service.ShowCats();if(service.serviceStatus.activeSelf || !service.rosterPanel.activeSelf)throw new Exception("Cat view overlapped service banner");service.ShowSeats();
            service.Disconnect();if(!service.serviceSummary.text.Contains("未接続") || service.GetSeatCard(0).details.interactable)throw new Exception("Offline seat controls enabled");apply();
            state.can_set_shifts=true;state.tick=0;apply();if(service.serviceStatus.activeSelf)throw new Exception("Service banner remained in preparation");
            Debug.Log("CAT_CAFE_SERVICE_LAYOUT_DISPLAY_OK: seats / assigned cat / empty seat / waiting / auto status / speed / scrolling / tabs / offline / preparation");
        }
    }
}
