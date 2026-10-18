using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
namespace CatCafePreview
{
    public sealed class StandaloneCGGallerySmoke : MonoBehaviour
    {
        string output;int checks;bool completed,failed;LocalCafeBackend backend;readonly List<string> errors=new List<string>();
        static LocalCafeBackend Create(string folder)=>StandaloneExtraConversationSmoke.Create(folder);
        static void Good(LocalCafeBackend b,string kind,string choice=null,int slot=1)=>StandaloneDispatchConversationSmoke.Good(b,kind,choice,slot:slot);
        public static int BackendChecks(string folder,bool restart)
        {
            int n=0;Action<bool,string> check=(ok,label)=>{n++;if(!ok)throw new Exception(label);};var b=Create(folder);int count=b.Gallery.Load().viewed.Length;if(restart)check(count==2,"CG restart lost observations");
            string state=JsonUtility.ToJson(b.Simulation.State);var definitions=CatImageGalleryCatalog.Load();check(definitions.Length>=3 && definitions.Select(d=>d.key).Distinct().Count()==definitions.Length,"CG asset catalog invalid");check(definitions.All(d=>d.Texture()!=null),"Registered CG resource unavailable in build");check(state==JsonUtility.ToJson(b.Simulation.State),"CG catalog changed game");
            Good(b,"new_game","free");Good(b,"save_game",slot:3);Good(b,"load_game",slot:3);check(b.Gallery.Load().viewed.Length==count,"CG new game/load changed unlocks");return n;
        }
        void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
        void Log(string m,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert){errors.Add(m+"\n"+stack);if(completed)Fail(new Exception(m));}}
        void Fail(Exception e){if(failed)return;failed=true;File.WriteAllText(Path.Combine(output,"failure.txt"),e.ToString());Debug.LogException(e);Application.Quit(1);}
        void OnDestroy(){Application.logMessageReceived-=Log;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"--catcafe-cg-gallery-check");if(i<0)return;var qa=new GameObject("CG verification").AddComponent<StandaloneCGGallerySmoke>();DontDestroyOnLoad(qa);qa.output=Path.GetFullPath(args[i+1]);Directory.CreateDirectory(qa.output);Application.runInBackground=true;Application.logMessageReceived+=qa.Log;
            try{qa.checks+=BackendChecks(Path.Combine(qa.output,"cases"),args.Contains("--cg-restart"));qa.StartCoroutine(qa.Guard());}catch(Exception e){qa.Fail(e);}
        }
        IEnumerator Guard(){var run=Run();while(true){bool more;try{if(errors.Count>0)throw new Exception(string.Join("\n",errors));more=run.MoveNext();}catch(Exception e){Fail(e);yield break;}if(!more)yield break;yield return run.Current;}}
        static GameObject Surface(Transform p,string name){var g=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));g.transform.SetParent(p,false);var r=g.GetComponent<RectTransform>();r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;g.GetComponent<Image>().color=Color.white;return g;}
        IEnumerator Run()
        {
            backend=Create(Path.Combine(output,"run-"+Guid.NewGuid().ToString("N")));CafeBackends.UseLocal(backend);SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);var preview=FindObjectOfType<ContentPreview>();var observer=preview.GetComponent<ViewedCatImages>();Check(observer!=null,"Business observer missing");observer.enabled=false;
            var overlay=Surface(preview.GetComponentInChildren<Canvas>().transform,"CG visibility fixture");overlay.transform.SetAsLastSibling();Canvas.ForceUpdateCanvases();backend=Create(Path.Combine(output,"observe-"+Guid.NewGuid().ToString("N")));CafeBackends.UseLocal(backend);var items=CatImageGalleryCatalog.Load();string before=JsonUtility.ToJson(backend.Simulation.State);
            var imageObject=new GameObject("Observed picture",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));imageObject.transform.SetParent(overlay.transform,false);var image=imageObject.GetComponent<RawImage>();var rect=image.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(240,240);image.texture=items[0].Texture();imageObject.SetActive(false);Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();observer.Scan();Check(backend.Gallery.Load().viewed.Length==0,"Inactive or covered business images unlocked; overlay depth="+overlay.GetComponent<CanvasRenderer>().absoluteDepth+", canvas="+overlay.GetComponent<Image>().canvas?.name+", size="+overlay.GetComponent<RectTransform>().rect.size);
            preview.CandidateCatTexture(items[2].cat_id);observer.Scan();Check(backend.Gallery.Load().viewed.Length==0,"Texture getter unlocked unseen image");imageObject.SetActive(true);Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();observer.Scan();Check(ViewedCatImages.IsVisible(image) && backend.Gallery.Load().viewed.SequenceEqual(new[]{items[0].key}),"Visible normal image not recorded");
            image.texture=items[1].Texture();var cover=Surface(overlay.transform,"Opaque modal");Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();observer.Scan();Check(!ViewedCatImages.IsVisible(image) && backend.Gallery.Load().viewed.Length==1,"Occluded image unlocked");cover.SetActive(false);Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();observer.Scan();Check(backend.Gallery.Load().viewed.Length==2,"New visible image not recorded");
            image.texture=items[2].Texture();var mask=Surface(overlay.transform,"Clipped viewport");mask.GetComponent<Image>().color=Color.clear;mask.AddComponent<RectMask2D>();var mr=mask.GetComponent<RectTransform>();mr.anchorMin=mr.anchorMax=new Vector2(.5f,.5f);mr.sizeDelta=new Vector2(100,100);imageObject.transform.SetParent(mask.transform,false);rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(400,0);rect.sizeDelta=new Vector2(240,240);Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();observer.Scan();Check(!ViewedCatImages.IsVisible(image) && backend.Gallery.Load().viewed.Length==2,"Masked image unlocked");
            imageObject.transform.SetParent(overlay.transform,false);rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;var group=imageObject.AddComponent<CanvasGroup>();group.alpha=0;Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();observer.Scan();Check(!ViewedCatImages.IsVisible(image) && backend.Gallery.Load().viewed.Length==2,"Transparent image unlocked");group.alpha=1;image.texture=ContentPreview.CatPlaceholderTexture();Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();observer.Scan();Check(backend.Gallery.Load().viewed.Length==2,"Placeholder unlocked a CG");Check(before==JsonUtility.ToJson(backend.Simulation.State),"Observation mutated game");
            string[] observed=backend.Gallery.Load().viewed;backend=Create(Path.Combine(output,"cases"));backend.Gallery.Merge(observed);Good(backend,"save_game",slot:3);CafeBackends.UseLocal(backend);SceneManager.LoadScene("CatCafeTitle");yield return new WaitForSecondsRealtime(2);var title=FindObjectOfType<PythonTitleScreen>();var extra=title.GetComponent<TitleExtraPanel>();extra.Open();extra.Images.Open();yield return null;var gallery=extra.Images;Check(gallery.Panel.activeSelf && !extra.Panel.activeSelf,"CG tab did not open");Check(gallery.Panel.GetComponentsInChildren<RawImage>(true).Count(r=>r.enabled && r.texture!=null)==2,"CG leaked locked thumbnails");Check(gallery.Panel.GetComponentsInChildren<Graphic>(true).All(g=>g.GetComponent<CanvasRenderer>()!=null),"CG renderer missing");
            before=JsonUtility.ToJson(backend.Simulation.State);string profile=JsonUtility.ToJson(backend.Gallery.Load());string saved=File.ReadAllText(Path.Combine(output,"cases/slot-3.json"));gallery.OpenImage(items[2].key);Check(!gallery.Viewer.activeSelf,"Locked image direct entry opened");gallery.OpenImage(items[0].key);yield return null;Canvas.ForceUpdateCanvases();Check(gallery.Viewer.activeSelf && !gallery.Panel.activeSelf && gallery.Enlarged.texture==items[0].Texture(),"Unlocked enlargement failed");Check(gallery.Enlarged.rectTransform.rect.width>0 && gallery.Enlarged.rectTransform.rect.height>0,"Image has no layout");gallery.Viewer.transform.Find("CG一覧へ戻る").GetComponent<Button>().onClick.Invoke();yield return null;Check(gallery.Panel.activeSelf && !gallery.Viewer.activeSelf,"Viewer return failed");
            Check(before==JsonUtility.ToJson(backend.Simulation.State) && profile==JsonUtility.ToJson(backend.Gallery.Load()) && saved==File.ReadAllText(Path.Combine(output,"cases/slot-3.json")),"Gallery mutated game, save or unlocks");gallery.Panel.transform.Find("会話の回想").GetComponent<Button>().onClick.Invoke();yield return null;Check(extra.Panel.activeSelf && !gallery.Panel.activeSelf,"Conversation tab broken");extra.Images.Open();yield return null;
            completed=true;File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: "+checks+" CG checks; zero Unity errors. Visible-only normal assets, hidden/occluded/masked/transparent exclusion, independent profile, locked thumbnails, enlargement and read-only navigation.");
        }
    }
}
