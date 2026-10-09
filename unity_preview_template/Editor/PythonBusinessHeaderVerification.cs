using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
    public static class PythonBusinessHeaderVerification
    {
        public static void BuildAndVerify()
        {
            PythonBusinessHeaderSetup.Build();var p=UnityEngine.Object.FindObjectOfType<ContentPreview>();p.Initialize();var client=p.GetComponent<PythonStateClient>();var flags=BindingFlags.Instance|BindingFlags.NonPublic;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,p);
            var state=PythonServiceSetup.Sample();state.instance_id="header-test";state.required_action="";var h=p.GetComponent<PythonBusinessHeader>();Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{state});
            state.can_set_shifts=true;state.tick=0;apply();Check(h.preparation.gameObject.activeSelf && !h.auto.toggle.gameObject.activeSelf && h.progress.GetComponentInChildren<Text>().text=="営業開始","Preparation controls");Capture(p,"HeaderPreparation",1280,800);
            state.can_set_shifts=false;state.tick=8;apply();Check(!h.preparation.gameObject.activeSelf && h.auto.toggle.gameObject.activeSelf,"Open controls");h.auto.Toggle();Check(h.auto.toggle.GetComponentInChildren<Text>().text.Contains("停止"),"Auto stop");Capture(p,"HeaderOpen",1280,800);h.auto.Toggle();
            var service=p.GetComponent<PythonServiceScreen>();service.ShowCats();Check(service.heading.rectTransform.anchoredPosition.y==-92,"Cat tab overwrote header");service.ShowSeats();
            state.closed=true;apply();Check(!h.auto.toggle.gameObject.activeSelf && !h.preparation.gameObject.activeSelf && h.progress.GetComponentInChildren<Text>().text=="翌日へ","Closing controls");Capture(p,"HeaderClosed",1280,800);
            state.closed=false;state.autoplay=new PythonAutoPlay{running=true,report=new PythonAutoPlayReport{run_id="header-run",running=true,limit=3,days=1},objectives=new PythonAutoPlayOption[0],modes=new PythonAutoPlayOption[0]};apply();var batch=p.GetComponent<PythonAutoPlayPanel>();Check(batch.activity.activeSelf && batch.activityStop.interactable && !h.progress.gameObject.activeSelf && !h.hint.gameObject.activeSelf,"Batch activity");Capture(p,"HeaderBatch",1280,800);Capture(p,"HeaderSmall",960,600);Capture(p,"HeaderWide",1440,810);
            batch.activityStop.onClick.Invoke();Check(client.BatchStopping,"Batch stop callback");typeof(PythonStateClient).GetField("pending",flags).SetValue(client,null);state.autoplay.running=false;state.autoplay.report.running=false;apply();Check(batch.activity.activeSelf && !batch.activityStop.interactable && batch.activityDetails.interactable,"Finished activity");
            state.autoplay=null;state.required_action="イベントに回答してください。";apply();Check(h.hint.text.Contains("イベント"),"Required hint missing");state.required_action="";state.can_set_shifts=true;foreach(var cat in state.cats)cat.working=false;apply();Check(h.hint.text.Contains("出勤する猫がいません"),"Missing workers hint overwritten");typeof(PythonStateClient).GetField("online",flags).SetValue(client,false);p.PythonConnectionError("test");Check(h.summary.text.Contains("未接続") && !h.progress.interactable && !h.auto.toggle.interactable,"Offline controls");
            Check(typeof(PythonStateClient).GetField("pending",flags).GetValue(client)==null,"Unexpected gameplay command");Debug.Log("CAT_CAFE_BUSINESS_HEADER_OK: preparation / open / closed / auto stop / batch stop / required / offline / 3 sizes");
        }
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        static void Capture(ContentPreview p,string name,int width,int height)
        {
            var cameraObject=new GameObject("Header Capture",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();var canvas=p.GetComponentInChildren<Canvas>();var mode=canvas.renderMode;var previousCamera=canvas.worldCamera;float distance=canvas.planeDistance;
            var target=new RenderTexture(width,height,24);camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();
            var root=p.transform.Find("Content Canvas/Background").GetComponent<RectTransform>();var corners=new Vector3[4];root.GetWorldCorners(corners);var bounds=new Rect(0,0,width,height);
            var h=p.GetComponent<PythonBusinessHeader>();foreach(var b in new[]{h.progress,h.preparation,h.auto.toggle,h.auto.speed,p.GetSceneButton(3),p.GetComponent<PythonMenuPanel>().open,p.GetComponent<PythonEventListPanel>().open,p.GetComponent<PythonHousingPanel>().open,p.GetComponent<PythonAutoPlayPanel>().activityStop,p.GetComponent<PythonAutoPlayPanel>().activityDetails})if(b.gameObject.activeInHierarchy){b.GetComponent<RectTransform>().GetWorldCorners(corners);foreach(var corner in corners){var point=RectTransformUtility.WorldToScreenPoint(camera,corner);Check(point.x>=-1 && point.x<=width+1 && point.y>=-1 && point.y<=height+1,"Button clipped at "+width+": "+b.name);}}
            camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(bounds,0,0);image.Apply();Directory.CreateDirectory("Logs");File.WriteAllBytes("Logs/"+name+".png",image.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);canvas.renderMode=mode;canvas.worldCamera=previousCamera;canvas.planeDistance=distance;UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }
}
