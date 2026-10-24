using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview.Editor {
 public static class HumanPortraitVerification {
  static int checks;
  static void Check(bool value,string label){checks++;if(!value)throw new Exception("Human portrait: "+label);}
  public static void VerifyAndBuild(){Run();StandaloneScreenSetup.Build();}
  public static void Run(){
   string workspace=Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),output=Path.Combine(workspace,"Logs/HumanPortraits/Editor");Directory.CreateDirectory(output);
   string folder=Environment.GetEnvironmentVariable("CATCAFE_HUMAN_FIXTURE");
   string root=Path.Combine(Application.dataPath,"Resources/CatCafeContent"),catalog=Path.Combine(root,"human_portraits.json");
   byte[] before=File.Exists(catalog)?File.ReadAllBytes(catalog):null;
   string cats=File.ReadAllText(Path.Combine(root,"catalog.json"));
   var fixture=JsonUtility.FromJson<HumanPortraitPack>(File.ReadAllText(Path.Combine(folder,"human_portraits.json")));
   string own=Path.Combine(root,"People",fixture.pack_id);bool existed=Directory.Exists(own);
   try {
    ContentPreviewSetup.Import(folder);ContentPreviewSetup.Import(folder);
    var first=HumanPortraitResources.Resolve("guest-1");Check(first!=null && first!=HumanPortraitResources.Placeholder(),"Registered guest did not resolve");
    Check(HumanPortraitResources.Resolve("guest-2")==HumanPortraitResources.Placeholder(),"Unregistered guest");Check(HumanPortraitResources.Resolve("temporary-sato")==HumanPortraitResources.Placeholder(),"Unknown/same-name guest");Check(HumanPortraitResources.Resolve(null)==HumanPortraitResources.Placeholder(),"Null guest");
    Check(File.ReadAllText(Path.Combine(root,"catalog.json"))==cats,"Person-only import changed cat catalog");
    var go=new GameObject("Human Icon Fixture",typeof(RectTransform),typeof(Image));
    HumanPortraitResources.ApplyIcon(go,"guest-1");Check(go.GetComponentInChildren<RawImage>().texture==first,"First seat guest");HumanPortraitResources.ApplyIcon(go,"guest-2");Check(go.GetComponentInChildren<RawImage>().texture==HumanPortraitResources.Placeholder(),"Seat replacement");HumanPortraitResources.ApplyIcon(go,null);Check(!go.GetComponentInChildren<RawImage>().enabled,"Empty seat retained portrait");UnityEngine.Object.DestroyImmediate(go);
    string missing=JsonUtility.ToJson(new HumanPortraitPack{version=1,pack_id=fixture.pack_id,people=new[]{new HumanPortraitRow{subject_id="guest-1",game_id="guest-1",image="people/"+new string('0',64)+".png"}}});File.WriteAllText(catalog,missing);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);Check(HumanPortraitResources.Resolve("guest-1")==HumanPortraitResources.Placeholder(),"Missing image fallback");File.WriteAllBytes(catalog,File.ReadAllBytes(Path.Combine(folder,"human_portraits.json")));AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    var duplicate=new HumanPortraitPack{version=1,pack_id=fixture.pack_id,people=new[]{fixture.people[0],fixture.people[0]}};bool rejected=false;try{duplicate.Validate();}catch(ArgumentException){rejected=true;}Check(rejected,"Duplicate game mapping accepted");
    // Existing backend tests use isolated saves and explicitly accelerated event fixtures.
    int trust=StandaloneTrustConversationSmoke.Run(Path.Combine(output,"trust"));int reservation=StandaloneReservationConversationSmoke.Run(Path.Combine(output,"reservation"));Check(trust>0 && reservation>0,"Conversation/save regressions");
    EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();
    foreach(int width in new[]{1280,1024})CaptureIcons(preview.GetComponentInChildren<Canvas>(),output,width,width==1280?800:768);
    File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: "+checks+" portrait checks; trust fixture="+trust+"; reservation fixture="+reservation+". Images are synthetic fixtures; no appearance adopted.");
    Debug.Log("CAT_CAFE_HUMAN_PORTRAITS_OK: "+checks);
   }finally{
    if(before==null){if(File.Exists(catalog))AssetDatabase.DeleteAsset("Assets/Resources/CatCafeContent/human_portraits.json");}else File.WriteAllBytes(catalog,before);
    if(!existed)AssetDatabase.DeleteAsset("Assets/Resources/CatCafeContent/People/"+fixture.pack_id);
    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
   }
  }
  public static void ImportSelectedAndVerify(){
   string folder=Environment.GetEnvironmentVariable("CATCAFE_HUMAN_SELECTED");ContentPreviewSetup.Import(folder);Run();
   string output=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/HumanPortraits/Selected");Directory.CreateDirectory(output);
   EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();var client=preview.GetComponent<PythonStateClient>();var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);
   var backend=new LocalCafeBackend(Path.Combine(output,"isolated-saves"));CafeBackends.UseLocal(backend);backend.Simulation.Execute("start_business");
   for(int i=0;i<25 && !backend.View().seats.Any(x=>!string.IsNullOrEmpty(x.customer_id));i++)backend.Simulation.Execute("advance_business");
   var view=backend.View();Check(view.seats.Any(x=>!string.IsNullOrEmpty(x.customer_id)),"No occupied seat fixture");
   // Only the detached view is changed, to display selected/unknown IDs on the actual seat card.
   var occupied=view.seats.First(x=>!string.IsNullOrEmpty(x.customer_id));occupied.customer_id="guest-1";occupied.customer_name="佐藤さん";
   typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{view});var screen=preview.GetComponent<PythonServiceScreen>();screen.ShowSeats();
   Check(preview.GetComponentsInChildren<RawImage>(true).Any(x=>x.texture==HumanPortraitResources.Resolve("guest-1")),"Actual seat did not show Sato");
   foreach(int width in new[]{1280,1024})CaptureReal(preview.GetComponentInChildren<Canvas>(),Path.Combine(output,"seat-"+width+".png"),width,width==1280?800:768);
   var actualCard=screen.seatContent.GetComponentsInChildren<PythonCatCard>(true).First(x=>x.gameObject.activeSelf);
   occupied.customer_id="temporary-sato";screen.Apply(view);Check(actualCard.customerIcon.GetComponentInChildren<RawImage>().texture==HumanPortraitResources.Placeholder(),"Actual seat unknown/same-name replacement");occupied.customer_id=null;screen.Apply(view);Check(!actualCard.customerIcon.activeSelf,"Actual empty seat retained portrait");
   Send(backend,"save_game");var restarted=new LocalCafeBackend(Path.Combine(output,"isolated-saves"));Send(restarted,"load_game");Check(restarted.View().seats.Select(x=>x.customer_id).SequenceEqual(backend.View().seats.Select(x=>x.customer_id)),"Saved/restarted customer IDs changed");Check(HumanPortraitResources.Resolve("guest-1")!=HumanPortraitResources.Placeholder(),"Restart portrait lookup");
   // An isolated trust event opens the existing conversation screen; it is not natural event incidence.
   var trust=new LocalCafeBackend(Path.Combine(output,"trust-screen"));trust.Simulation.State.trust_events.Add(new CafeTrustEvent{id="portrait-fixture",customer_id="guest-1",status="waiting",day=1,suspensions=1});CafeBackends.UseLocal(trust);typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{trust.View()});var owner=preview.GetComponent<PythonCustomerTrustPanel>();var ui=owner.Dialogue;ui.Apply(trust.View(),owner);Check(ui.OpenPending(),"Trust UI fixture did not open");
   Check((Texture2D)typeof(TrustConversationPanel).GetMethod("PortraitTexture",flags).Invoke(ui,new object[]{""})==HumanPortraitResources.Resolve("guest-1"),"Actual trust portrait ID");ui.Next();string before=JsonUtility.ToJson(trust.Simulation.State);
   foreach(int width in new[]{1280,1024})CaptureReal(preview.GetComponentInChildren<Canvas>(),Path.Combine(output,"trust-"+width+".png"),width,width==1280?800:768);ui.Close();
   var host=new GameObject("Portrait Extra Fixture",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));host.transform.SetParent(preview.GetComponentInChildren<Canvas>().transform,false);var replay=preview.gameObject.AddComponent<ReplayConversationPanel>();
   replay.Play(new CafeReplayEntry{conversation_id=CafeTrustDialogue.Id+".response",branch_id="recover",title="顧客の信頼回復",customer_id="guest-1",cat_name="佐藤さん",personality="顧客の信頼",lines=new[]{new CafeConversationLine("佐藤さん","少し、様子を見てみましょうか。")}},host,ContentPreviewLayout.LoadFont(),()=>{});
   Check((Texture2D)typeof(ReplayConversationPanel).GetMethod("PortraitTexture",flags).Invoke(replay,new object[]{""})==HumanPortraitResources.Resolve("guest-1"),"Actual Extra portrait ID");replay.Next();
   foreach(int width in new[]{1280,1024})CaptureReal(preview.GetComponentInChildren<Canvas>(),Path.Combine(output,"extra-"+width+".png"),width,width==1280?800:768);
   replay.Play(new CafeReplayEntry{conversation_id=CafeTrustDialogue.Id+".response",branch_id="recover",title="旧回想",cat_name="佐藤さん",lines=new[]{new CafeConversationLine("佐藤さん","旧回想の代替表示")}},host,ContentPreviewLayout.LoadFont(),()=>{});Check((Texture2D)typeof(ReplayConversationPanel).GetMethod("PortraitTexture",flags).Invoke(replay,new object[]{""})==HumanPortraitResources.Placeholder(),"Old Extra guessed ID from name");
   var reservation=preview.gameObject.AddComponent<ReservationConversationPanel>();Check((Texture2D)typeof(ReservationConversationPanel).GetMethod("PortraitTexture",flags).Invoke(reservation,new object[]{""})==HumanPortraitResources.Placeholder(),"Unregistered reservation portrait fallback");
   Check(before==JsonUtility.ToJson(trust.Simulation.State),"Rendering or Extra mutated gameplay");File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: selected Sato imported via standard importer; real seat/trust/Extra UI rendered with isolated event/view fixtures at 1280x800 and 1024x768. No physical input or natural trust occurrence claimed. Gameplay unchanged.");Debug.Log("CAT_CAFE_HUMAN_SELECTED_OK");
  }
  static void Send(LocalCafeBackend backend,string kind){
   var state=backend.Simulation.State;CafeResponse response=null;var command=new PythonShiftCommand{kind=kind,save_slot=1,save_id="1",request_id=Guid.NewGuid().ToString("N"),instance_id=state.instance_id,expected_revision=state.revision,working_cats=new string[0]};var routine=backend.Request("/commands",JsonUtility.ToJson(command),r=>response=r);while(routine.MoveNext()){}Check(response!=null && response.result==UnityEngine.Networking.UnityWebRequest.Result.Success,kind+" portrait resume fixture: "+response?.error);
  }
  static void CaptureReal(Canvas canvas,string file,int width,int height){
   var go=new GameObject("Capture",typeof(Camera));var cam=go.GetComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.white;var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;var distance=canvas.planeDistance;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;var target=new RenderTexture(width,height,24);cam.targetTexture=target;Canvas.ForceUpdateCanvases();cam.Render();var previous=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(file,image.EncodeToPNG());RenderTexture.active=previous;cam.targetTexture=null;canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=distance;UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(go);
  }
  static void CaptureIcons(Canvas canvas,string output,int width,int height){
   // Exact seat icon dimensions and the conversation portrait dimensions; isolated rendering fixture.
   foreach(Transform child in canvas.transform)child.gameObject.SetActive(false);
   var board=new GameObject("Human Portrait Fixture",typeof(RectTransform),typeof(Image));board.transform.SetParent(canvas.transform,false);var rect=(RectTransform)board.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;board.GetComponent<Image>().color=new Color(.96f,.95f,.9f);
   int index=0;foreach(string id in new[]{"guest-1","guest-2","temporary-sato"}){
    var icon=new GameObject("Seat "+id,typeof(RectTransform),typeof(Image));icon.transform.SetParent(board.transform,false);var r=(RectTransform)icon.transform;r.anchorMin=r.anchorMax=new Vector2(.2f+index*.3f,.8f);r.sizeDelta=new Vector2(34,34);HumanPortraitResources.ApplyIcon(icon,id);
    var image=new GameObject("Conversation "+id,typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter));image.transform.SetParent(board.transform,false);var ir=(RectTransform)image.transform;ir.anchorMin=ir.anchorMax=new Vector2(.2f+index*.3f,.43f);ir.sizeDelta=new Vector2(280,320);image.GetComponent<RawImage>().texture=HumanPortraitResources.Resolve(id);index++;
   }
   var cameraObject=new GameObject("Capture",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.white;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
   var target=new RenderTexture(width,height,24);camera.targetTexture=target;Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;var png=new Texture2D(width,height,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,width,height),0,0);png.Apply();File.WriteAllBytes(Path.Combine(output,"fixture-"+width+".png"),png.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(png);target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(board);
  }
 }
}
