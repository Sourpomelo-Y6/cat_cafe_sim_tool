using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CatCafePreview.Standalone;

namespace CatCafePreview
{
    // Explicit development flag only. Saves stay under the supplied verification directory.
    public sealed class StandaloneOptionalEventSmoke : MonoBehaviour
    {
        string output;bool resume;int checks;
        static readonly BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]static void Install()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"--catcafe-optional-event");if(index<0)return;
            var qa=new GameObject("Optional event verification").AddComponent<StandaloneOptionalEventSmoke>();DontDestroyOnLoad(qa);
            qa.output=Path.GetFullPath(args[index+1]);qa.resume=args.Contains("--resume-optional");Directory.CreateDirectory(qa.output);Application.runInBackground=true;qa.StartCoroutine(qa.Guard());
        }
        public static LocalCafeBackend Create(string path)
        {
            var r=JsonUtility.FromJson<CafeRules>(Resources.Load<TextAsset>("CatCafeStandalone/rules").text);
            r.store_events.probability=0;r.intake.day=r.regular_introduction.day=9999;r.growth.threshold=100000;
            r.exchange.affinity_enthusiastic=r.exchange.affinity_favorable=25;r.exchange.affinity_turn_away_loss=0;r.exchange.direct_gain=100;
            return new LocalCafeBackend(path,r);
        }
        static void Good(LocalCafeBackend b,string kind,string choice=null,int slot=1)=>StandaloneDispatchConversationSmoke.Good(b,kind,choice,cat:"cat-mike",slot:slot);
        void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
        void CheckArtwork(AcquisitionConversationPanel view,bool still)
        {
            var artwork=(RawImage)typeof(AcquisitionConversationPanel).GetField("sceneImage",Hidden).GetValue(view);
            var portrait=(RawImage)typeof(AcquisitionConversationPanel).GetField("portrait",Hidden).GetValue(view);
            Check(artwork.enabled && artwork.texture!=null && artwork.texture.name==(still?"mike-approaches-v1":"mike-window-v1"),"Correct scene illustration");
            Check(portrait.enabled!=still,"Portrait switches off for the full illustration");
            var heading=(Text)typeof(AcquisitionConversationPanel).GetField("heading",Hidden).GetValue(view);
            var headingBox=(GameObject)typeof(AcquisitionConversationPanel).GetField("sceneHeading",Hidden).GetValue(view);
            Check(headingBox.GetComponent<RectTransform>().rect.width>=heading.preferredWidth+32,"Title fits on its backing");
            if(still)return;
            var frame=portrait.transform.parent.parent.GetComponent<RectTransform>();
            Check(frame.rect.height>310 && portrait.texture!=ContentPreview.CatPlaceholderTexture(),"Larger portrait uses the frozen cat texture");
        }
        IEnumerator Guard()
        {
            var work=Run();while(true){bool more;try{more=work.MoveNext();}catch(Exception ex){File.WriteAllText(Path.Combine(output,resume?"resume-failure.txt":"failure.txt"),ex.ToString());Debug.LogException(ex);Application.Quit(1);yield break;}if(!more)break;yield return work.Current;}
            File.WriteAllText(Path.Combine(output,resume?"resume-result.txt":"result.txt"),"PASS: "+checks+" Player checks. PID="+System.Diagnostics.Process.GetCurrentProcess().Id+"; explicit isolated fixture; no physical input or natural occurrence claim.");Application.Quit(0);
        }
        IEnumerator Shot(string name)
        {
            // Explicit rendering works even when the verification window is hidden.
            yield return new WaitForEndOfFrame();
            var canvas=FindObjectOfType<ContentPreview>().GetComponentInChildren<Canvas>();
            var mode=canvas.renderMode;var oldCamera=canvas.worldCamera;float distance=canvas.planeDistance;
            var previous=RenderTexture.active;var go=new GameObject("Optional event capture camera",typeof(Camera));
            var camera=go.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.white;
            var target=new RenderTexture(Screen.width,Screen.height,24);var image=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);image.Apply();
                var pixels=image.GetPixels32();Check(pixels.Any(p=>p.r!=pixels[0].r || p.g!=pixels[0].g || p.b!=pixels[0].b),"Capture contains rendered content: "+name);
                File.WriteAllBytes(Path.Combine(output,name+".png"),image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active=previous;canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=distance;
                camera.targetTexture=null;target.Release();DestroyImmediate(image);DestroyImmediate(target);DestroyImmediate(go);
            }
            yield return null;
        }
        IEnumerator Run()
        {
            CafeOptions.Current.instant=true;
            var b=Create(Path.Combine(output,"isolated-save"));CafeBackends.UseLocal(b);
            var args=Environment.GetCommandLineArgs();int variant=Array.IndexOf(args,"--optional-authoring-variant");
            bool accepted=args.Contains("--optional-accept");
            string branch=accepted?"accepted":"declined";
            if(variant>=0)b.SetOptionalCatalog(CafeOptionalEventJson.Parse(File.ReadAllText(args[variant+1])));
            if(resume)Good(b,"load_game",slot:3);
            else
            {
                Check(!b.OptionalEventListed,"Day one unavailable");Good(b,"day_off");if(b.Simulation.State.phase=="closed")Good(b,"next_day");
                Good(b,"player_begin");Good(b,"player_step","direct");if(b.Simulation.PlayerActive)Good(b,"player_finish");
                Check(b.OptionalEventListed && b.OptionalEventReason=="","Legal threshold fixture");
                if(variant<0)
                {
                    Check(b.Simulation.PlayerCat("cat-mike").affinity==20 && b.Simulation.PlayerBeginReason("cat-mike").Contains("好感度20"),"Affinity overshoot is capped and the next interaction shows guidance");
                    Check(b.OptionalCandidates.Single().affinity_milestone,"Event list includes the milestone");
                    Check(b.Journal.Verify(b.Journal.Document).PlayerCat("cat-mike").affinity==20,"Business log replays the capped interaction");
                    Good(b,"day_off");if(b.Simulation.State.phase=="closed")Good(b,"next_day");
                    Check(b.OptionalEventListed && b.Simulation.PlayerCat("cat-mike").affinity==20,"Daily progression works with an unread milestone");
                }
            }
            string digest=CafeReplayJournal.Digest(b.Simulation.State);
            SceneManager.LoadScene("CatCafePythonState");yield return new WaitForSecondsRealtime(2);
            var client=FindObjectOfType<PythonStateClient>();client.enabled=false;CafeBackends.UseLocal(b);
            Action apply=()=>typeof(PythonStateClient).GetMethod("Accept",Hidden).Invoke(client,new object[]{b.View()});apply();
            bool compact=resume || !accepted;
            int width=compact?1024:1280;
            Screen.SetResolution(width,compact?768:800,false);yield return new WaitForSecondsRealtime(.5f);
            var view=client.GetComponent<OptionalEventConversationPanel>()??client.gameObject.AddComponent<OptionalEventConversationPanel>();
            Check(!view.Showing && !b.ConversationBlocksProgress,"Does not auto-open or block");
            if(resume && variant>=0)Check(JsonUtility.ToJson(b.OptionalEvent)==File.ReadAllText(Path.Combine(output,"frozen-story.json")),"Started story remains identical after catalogue rewrite across processes");
            if(resume)Check(b.OptionalEvent.progress.branch_id==branch && b.OptionalEvent.progress.node==1 && b.OptionalEvent.progress.page_offset==2,"Different-process result position restored");
            Check(view.OpenEvent(),"Explicit start or resume");Check(b.ConversationBlocksProgress,"Visible event blocks business input");
            CheckArtwork(view,resume && accepted);
            yield return Shot(resume?"resume-1024":"introduction-"+width);
            view.Close();Check(!b.ConversationBlocksProgress,"Close releases business and autoplay");
            Check(view.OpenEvent(),"Reopen retains progress");
            if(!resume)
            {
                b.OptionalEvent.progress.node=b.OptionalEvent.introduction.Length;b.OptionalEvent.progress.page_offset=0;view.Refresh();Check(view.PlaybackChoosing,"Two choices ready");yield return Shot("choices-"+width);
                yield return null;view.Answer(accepted);Check(b.OptionalEvent.progress.branch_id==branch,"UI choice records branch");CheckArtwork(view,accepted);yield return Shot("result-"+width);
                b.OptionalEvent.progress.node=1;b.OptionalEvent.progress.page_offset=2;File.WriteAllText(Path.Combine(output,"frozen-story.json"),JsonUtility.ToJson(b.OptionalEvent));Good(b,"save_game",slot:3);Check(!b.OptionalEvent.progress.completed,"Partial save is unfinished");
            }
            else
            {
                b.OptionalEvent.progress.node=CafeOptionalEvents.Lines(b.OptionalEvent,branch).Length;b.OptionalEvent.progress.page_offset=0;view.Refresh();yield return null;view.Next();
                Check(b.OptionalEvent.progress.completed && !b.OptionalEventListed,"UI completion recorded once");
                Check(b.Replays.Load().entries.Count(e=>e.conversation_id==b.OptionalEvent.progress.conversation_id)==1,"Only chosen branch archived");
                Good(b,"save_game",slot:4);Good(b,"load_game",slot:4);apply();Check(b.OptionalEvent.progress.completed && !b.ConversationBlocksProgress,"Completed save reload");
                var entry=b.Replays.Load().entries.Single(e=>e.conversation_id==b.OptionalEvent.progress.conversation_id);
                if(b.OptionalEvent.artwork!=null)
                    Check(entry.artwork.SequenceEqual(b.OptionalEvent.artwork) && entry.artwork_choice_node==b.OptionalEvent.introduction.Length,"Extra stores the authored artwork and choice boundary");
                else
                    Check(entry.scene_art==CafeOptionalEvents.MikeWindowArt && entry.still_start_node==(accepted?b.OptionalEvent.introduction.Length+1:0),"Extra stores the result artwork boundary");
                var host=new GameObject("Extra artwork verification",typeof(RectTransform),typeof(Image));
                host.transform.SetParent(view.PlaybackHost.transform.parent,false);
                var replay=client.gameObject.AddComponent<ReplayConversationPanel>();
                replay.Play(entry,host,client.GetComponent<PythonCatDetailsPanel>().title.font,()=>{});
                yield return null;replay.Refresh();CheckArtwork(replay,false);
                for(int i=0;i<b.OptionalEvent.introduction.Length+1;i++){yield return null;replay.Next();}
                CheckArtwork(replay,accepted);
                yield return Shot("extra-result-1024");replay.Close();
            }
            Check(digest==CafeReplayJournal.Digest(b.Simulation.State),"Story does not change gameplay digest");
            if(resume && variant<0)
            {
                Check(!b.Simulation.PlayerAtAffinityMilestone("cat-mike"),"Reading the result releases the cap after save and reload");
                Good(b,"player_begin");Good(b,"player_step","direct");if(b.Simulation.PlayerActive)Good(b,"player_finish");
                Check(b.Simulation.PlayerCat("cat-mike").affinity>20,"Subsequent interaction raises affinity above the milestone");
                Check(b.Journal.Verify(b.Journal.Document).PlayerCat("cat-mike").affinity>20,"Business log replays the unlocked interaction");
            }
            if(resume && variant>=0)
            {
                string oldRevision=b.OptionalEvent.revision;Good(b,"new_game","free");Good(b,"day_off");if(b.Simulation.State.phase=="closed")Good(b,"next_day");Good(b,"player_begin");Good(b,"player_step","direct");if(b.Simulation.PlayerActive)Good(b,"player_finish");apply();
                Check(view.OpenEvent() && b.OptionalEvent.revision==b.OptionalDefinition.revision && b.OptionalEvent.revision!=oldRevision,"New start uses revised definition");
                yield return Shot("new-revision-1024");view.Close();
            }
        }
    }
}
