using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    // Opt-in Player QA uses real scene clients, UGUI callbacks, coroutines and disk saves.
    // Normal launches do not create this component or override the user's save directory.
    public partial class StandaloneScreenSmoke : MonoBehaviour
    {
        private string output,importPath;
        private LocalCafeBackend backend;
        private bool nativeDefault;
        private RestReferences pythonReference;
        private readonly List<string> checks=new List<string>(),errors=new List<string>();
        private PythonStateClient Client=>FindObjectOfType<PythonStateClient>();
        private void Check(bool condition,string label){if(!condition)throw new Exception(label);checks.Add(label);}
        private static string Argument(string name){var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,name);return i>=0 && i+1<args.Length?args[i+1]:null;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var path=Argument("--catcafe-ui-check");if(string.IsNullOrEmpty(path))return;
            Application.runInBackground=true;var go=new GameObject("Standalone screen verification");DontDestroyOnLoad(go);var runner=go.AddComponent<StandaloneScreenSmoke>();
            runner.output=Path.GetFullPath(path);Directory.CreateDirectory(runner.output);runner.importPath=Argument("--catcafe-import-fixture");
            runner.nativeDefault=CafeBackends.Standalone;runner.backend=new LocalCafeBackend(Argument("--catcafe-ui-save-directory") ?? Path.Combine(runner.output,"saves"));CafeBackends.UseLocal(runner.backend);
            Application.logMessageReceived+=runner.Log;runner.StartCoroutine(runner.Guard(runner.Run()));
        }
        private void Log(string message,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)errors.Add(message+"\n"+stack);}
        private IEnumerator Guard(IEnumerator root)
        {
            var stack=new Stack<IEnumerator>();stack.Push(root);string failure=null;
            while(stack.Count>0)
            {
                object value=null;bool more=false;
                try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception ex){failure=ex.ToString();break;}
                if(!more){stack.Pop();continue;}if(value is IEnumerator nested){stack.Push(nested);continue;}yield return value;
            }
            if(errors.Count>0 && failure==null)failure=string.Join("\n",errors);
            File.WriteAllText(Path.Combine(output,"result.json"),JsonUtility.ToJson(new Result{success=failure==null,checks=checks.ToArray(),errors=errors.ToArray(),failure=failure},true));
            Application.Quit(failure==null?0:1);
        }
        [Serializable] private class Result{public bool success;public string[] checks,errors;public string failure;}
        private IEnumerator Wait(Func<bool> ready,string label)
        {float deadline=Time.realtimeSinceStartup+15;while(!ready() && Time.realtimeSinceStartup<deadline)yield return null;Check(ready(),label);}
        private IEnumerator Click(Button button,string label)
        {
            yield return Wait(()=>button!=null && button.IsActive() && button.interactable,label+" available");
            ExecuteEvents.Execute(button.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
        }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.2f);yield return null;var canvases=FindObjectsOfType<Canvas>().Where(c=>c.isRootCanvas).ToArray();var modes=canvases.Select(c=>c.renderMode).ToArray();var cameras=canvases.Select(c=>c.worldCamera).ToArray();var distances=canvases.Select(c=>c.planeDistance).ToArray();
            var camera=new GameObject("QA capture camera").AddComponent<Camera>();var target=new RenderTexture(Screen.width,Screen.height,24);camera.targetTexture=target;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=RenderMode.ScreenSpaceCamera;canvases[i].worldCamera=camera;canvases[i].planeDistance=1;}
            Canvas.ForceUpdateCanvases();camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;var texture=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(output,name+".png"),texture.EncodeToPNG());RenderTexture.active=previous;
            for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=modes[i];canvases[i].worldCamera=cameras[i];canvases[i].planeDistance=distances[i];}Destroy(texture);Destroy(camera.gameObject);target.Release();Destroy(target);yield return null;
        }
        private void RestoreFixture(CafeSnapshot state){backend.Simulation.Restore(state);typeof(LocalCafeBackend).GetMethod("ResetJournal",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(backend,null);}
        private IEnumerator Changed(int revision){yield return Wait(()=>Client!=null && Client.CurrentState!=null && Client.CurrentState.revision!=revision && Client.CanManageSaves,"command completed");}
        private Button EquipmentButton(EquipmentConversationPanel view,string field)=>
            (Button)typeof(AcquisitionConversationPanel).GetField(field,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(view);
        private IEnumerator EquipmentChoices(EquipmentConversationPanel view)
        {
            for(int i=0;i<60 && view.Showing && !view.PlaybackChoosing;i++){yield return Click(EquipmentButton(view,"next"),"equipment introduction next");yield return null;}
            Check(view.Showing && view.PlaybackChoosing,"equipment choices reached");
        }
        private IEnumerator EquipmentFinish(EquipmentConversationPanel view)
        {
            for(int i=0;i<60 && view.Showing;i++){yield return Click(EquipmentButton(view,"next"),"equipment result next");yield return null;}
            Check(!view.Showing && !backend.EquipmentResultPending,"equipment result completed");
        }
        [Serializable] private class RestReferences{public RestCase[] cases;public PythonDay day;}
        [Serializable] private class PythonDay{public double funds,revenue,expenses,popularity;public int completed;public PythonDayCat[] cats;}
        [Serializable] private class PythonDayCat{public string id;public double stamina,fatigue,stress;}
        [Serializable] private class RestCase{public RestCat[] cats;public string[] expected;}
        [Serializable] private class RestCat{public string id,health,activity;public double fatigue;}
        private void CompareRestProposals()
        {
            string path=Argument("--catcafe-rest-reference");if(string.IsNullOrEmpty(path))return;
            var reference=JsonUtility.FromJson<RestReferences>(File.ReadAllText(path));pythonReference=reference;var candidate=new LocalCafeBackend(Path.Combine(output,"projection-only"),backend.Simulation.Rules);
            foreach(var row in reference.cases)
            {
                foreach(var input in row.cats){var cat=candidate.Simulation.State.cats.Single(c=>c.id==input.id);cat.fatigue=input.fatigue;cat.health=input.health;cat.activity=input.activity;}
                Check(candidate.View().rest_proposal.SequenceEqual(row.expected),"rest proposal Python reference");
            }
        }
        private void ComparePythonDay()
        {
            if(pythonReference?.day==null)return;var expected=pythonReference.day;var state=backend.Simulation.State;
            Check(state.funds==expected.funds && state.revenue==expected.revenue && state.expenses==expected.expenses && state.completed==expected.completed && state.popularity==expected.popularity,"UI full day matches original complete Python settlement");
            foreach(var row in expected.cats){var cat=state.cats.Single(c=>c.id==row.id);Check(Math.Abs(cat.stamina-row.stamina)<1e-9 && Math.Abs(cat.fatigue-row.fatigue)<1e-9 && Math.Abs(cat.stress-row.stress)<1e-9,"UI full day matches Python cat condition");}
        }
        private IEnumerator StateImageChecks()
        {
            var hidden=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            var preview=Client.GetComponent<ContentPreview>();var panel=Client.GetComponent<PythonCatDetailsPanel>();
            Action<PythonCafeState> apply=s=>typeof(PythonStateClient).GetMethod("Accept",hidden).Invoke(Client,new object[]{s});
            var state=backend.View();var mike=state.cats.Single(c=>c.cat_id=="cat-mike");apply(state);panel.Open("cat-mike");
            var card=panel.panel.transform.Find("Details Card");var portrait=card.Find("State Portrait").GetComponent<RawImage>();
            foreach(int kind in new[]{1,2,3})
            {
                mike.stress=kind==1?65:0;mike.fatigue=kind==2?65:0;mike.affinity=80;apply(state);
                Check(CatStateImages.Resolve(mike,kind)!=null && portrait.texture==CatStateImages.Resolve(mike,kind),"F4 compiled image "+kind);
                Check(card.Find("State Badge").gameObject.activeSelf,"F4 mark agrees "+kind);yield return Capture("f4-state-"+kind);
            }
            string before=JsonUtility.ToJson(backend.Simulation.State);mike.stress=mike.fatigue=0;mike.affinity=0;apply(state);Check(portrait.texture==preview.PythonCatTexture(mike),"F4 normal fallback");
            panel.Open(state.cats.First(c=>c.cat_id!="cat-mike").cat_id);Check(!card.Find("State Badge").gameObject.activeSelf,"F4 other cat reset");panel.Close();
            Check(JsonUtility.ToJson(backend.Simulation.State)==before,"F4 render does not alter simulation");apply(backend.View());
        }
        private IEnumerator OutfitChecks()
        {
            yield return Wait(()=>Client.UIStateReady,"F5 UI ready");
            var initial=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(backend.Simulation.State));initial.NormalizeJsonStrings();
            var panel=Client.GetComponent<PythonCatDetailsPanel>();panel.Open("cat-mike");var ui=Client.GetComponent<CatOutfitPanel>();
            yield return Click(ui.entry,"F5 open selection");yield return Click(ui.ribbon,"F5 preview ribbon");yield return Capture("f5-selection");yield return Click(ui.cancel,"F5 cancel");Check(backend.CatOutfit("cat-mike")=="","F5 cancel preserves normal");
            yield return Click(ui.entry,"F5 reopen");yield return Click(ui.ribbon,"F5 choose ribbon");string before=JsonUtility.ToJson(backend.Simulation.State);yield return Click(ui.apply,"F5 apply");
            yield return Wait(()=>Client.UIStateReady && backend.CatOutfit("cat-mike")=="ribbon","F5 selection response without revision change");Check(JsonUtility.ToJson(backend.Simulation.State)==before,"F5 game state unchanged");
            var portrait=panel.panel.transform.Find("Details Card/State Portrait").GetComponent<RawImage>();Check(portrait.texture==CatOutfits.ImageFor("playtest-mike"),"F5 ribbon in detail");yield return Capture("f5-ribbon");
            var browser=Client.GetComponent<PythonSaveBrowser>();panel.Close();browser.OpenSave();browser.Select(0);yield return Click(browser.action,"F5 save review");int rev=Client.CurrentState.revision;yield return Click(browser.confirm,"F5 save confirm");yield return Changed(rev);browser.Close();
            // Keep a dedicated slot for cross-process QA, away from the rest of the smoke run.
            Client.SaveToSlot(9);yield return Wait(()=>Client.UIStateReady && File.Exists(Path.Combine(output,"saves/slot-9.json")),"F5 dedicated save");
            panel.Open("cat-mike");yield return Click(ui.entry,"F5 clear selection dialog");yield return Click(ui.normal,"F5 preview normal");yield return Click(ui.apply,"F5 clear");yield return Wait(()=>Client.UIStateReady && backend.CatOutfit("cat-mike")=="","F5 clear response");
            panel.Close();browser.OpenLoad();browser.Select(0);yield return Click(browser.action,"F5 load review");yield return Click(browser.confirm,"F5 load confirm");yield return Wait(()=>Client.UIStateReady && backend.CatOutfit("cat-mike")=="ribbon","F5 load restores");panel.Open("cat-mike");Check(portrait.texture==CatOutfits.ImageFor("playtest-mike"),"F5 load recomputes image");
            foreach(var tab in panel.recordTabs)yield return Click(tab,"F5 existing record tab");panel.Open("cat-sora");Check(ui.entry.gameObject.activeSelf,"F5 other cat entry available");ui.Open();Check(!ui.ribbon.interactable,"v1 ribbon unavailable for other individual");ui.Close();panel.Open("cat-mike");
            // Conditions are an isolated fixture; purchase/use goes through real UGUI confirmation callbacks.
            var fixture=JsonUtility.FromJson<CafeSnapshot>(JsonUtility.ToJson(backend.Simulation.State));fixture.NormalizeJsonStrings();fixture.cats.Single(c=>c.id=="cat-mike").stress=65;fixture.cats.Single(c=>c.id=="cat-mike").fatigue=65;RestoreFixture(fixture);
            yield return Wait(()=>Client.UIStateReady && Client.CurrentState.cats.Single(c=>c.cat_id=="cat-mike").stress==65,"F5 stress fixture refreshed");
            foreach(int kind in new[]{1,2})
            {
                panel.Open("cat-mike");yield return Capture("f5-care-detail-"+kind);var cat=Client.CurrentState.cats.Single(c=>c.cat_id=="cat-mike");Check(CatStateMark.Resolve(cat)==kind && portrait.texture==CatStateImages.Resolve(cat,kind),"F5 F4 priority before care "+kind+" resolved="+CatStateMark.Resolve(cat)+" actual="+portrait.texture?.name+" expected="+CatStateImages.Resolve(cat,kind)?.name);yield return Capture("f5-before-care-"+kind);
                var item=backend.Simulation.Rules.item_shop.First(r=>kind==1?r.item.stress_relief>5:r.item.fatigue_relief>5);panel.Care();var items=Client.GetComponent<PythonItemsPanel>();items.ShowPurchase();items.shop.value=Array.FindIndex(Client.CurrentState.items.shop,r=>r.choice==item.item.id);yield return Click(items.buy,"F5 buy care item");rev=Client.CurrentState.revision;yield return Click(items.confirm,"F5 confirm purchase");yield return Changed(rev);
                items.ShowCare();items.owned.value=Array.FindIndex(Client.CurrentState.items.owned,r=>r.item_id==item.item.id);var owned=Client.CurrentState.items.owned[items.owned.value];items.cats.value=Array.FindIndex(owned.targets,r=>r.cat_id=="cat-mike");yield return Click(items.use,"F5 use care item");rev=Client.CurrentState.revision;yield return Click(items.confirm,"F5 confirm care");yield return Changed(rev);items.Close();
            }
            Check(backend.CatOutfit("cat-mike")=="ribbon" && portrait.texture==CatOutfits.ImageFor("playtest-mike"),"F5 care returns to chosen outfit");yield return Capture("f5-after-care");panel.Close();
            Client.GetComponent<PythonReturnToTitle>().Return();yield return Wait(()=>FindObjectOfType<PythonTitleScreen>()?.CanManageSaves==true,"F5 title ready");FindObjectOfType<PythonTitleScreen>().ResumeSaved("9");yield return Wait(()=>Client?.UIStateReady==true,"F5 title resume ready");Check(backend.CatOutfit("cat-mike")=="ribbon","F5 title resumes selection");
            panel=Client.GetComponent<PythonCatDetailsPanel>();panel.Open("cat-mike");yield return Capture("f5-title-resume");var newUi=Client.GetComponent<CatOutfitPanel>();yield return Click(newUi.entry,"F5 final clear dialog");yield return Click(newUi.normal,"F5 final normal");yield return Click(newUi.apply,"F5 final clear");yield return Wait(()=>Client.UIStateReady && backend.CatOutfit("cat-mike")=="","F5 final clear completed");panel.Close();RestoreFixture(initial);yield return Wait(()=>Client.UIStateReady && Client.CurrentState.instance_id==initial.instance_id && Client.CurrentState.funds==initial.funds,"F5 original fixture restored");
        }
        private IEnumerator AllCatMarkChecks()
        {
            yield return Wait(()=>Client.UIStateReady,"all-cat UI ready");var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;var panel=Client.GetComponent<PythonCatDetailsPanel>();var preview=Client.GetComponent<ContentPreview>();var view=backend.View();string game=JsonUtility.ToJson(backend.Simulation.State);Action accept=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(Client,new object[]{view});accept();
            foreach(var row in view.cats)
            {
                row.stress=65;accept();panel.Open(row.cat_id);var card=panel.panel.transform.Find("Details Card");Check(card.Find("State Badge").gameObject.activeSelf,"initial cat mark "+row.cat_id);Check(card.Find("State Portrait").GetComponent<RawImage>().texture==preview.PythonCatTexture(row),"individual image "+row.cat_id);
            }
            var source=view.cats.Single(c=>c.cat_id=="cat-sora");source.stress=0;source.fatigue=65;source.working=false;accept();panel.Open(source.cat_id);yield return Capture("all-sora-resting");var detail=panel.panel.transform.Find("Details Card");Check(detail.Find("State Caption").GetComponent<Text>().text=="疲労高め","resting cat keeps fatigue mark");foreach(var tab in panel.recordTabs)yield return Click(tab,"all-cat record tab");
            foreach(string id in new[]{"request-cat-1","regular-rescue-1","visiting-cat-1","shop-3","rescue-100","dispatch-rescue-100","future-cat"})
            {
                var copy=JsonUtility.FromJson<PythonCatState>(JsonUtility.ToJson(source));copy.cat_id=id;copy.name=id;copy.stress=65;copy.fatigue=0;var originals=view.cats;view.cats=originals.Concat(new[]{copy}).ToArray();accept();panel.Open(id);Check(detail.Find("State Caption").GetComponent<Text>().text=="ストレス高め","dynamic individual "+id);Check(Client.GetComponent<CatOutfitPanel>().entry.gameObject.activeSelf,"outfit available "+id);view.cats=originals;
            }
            accept();panel.Open(source.cat_id);Check(detail.Find("State Caption").GetComponent<Text>().text=="疲労高め","shared texture restores own state");string content=source.content_cat_id;source.content_cat_id="not-registered";accept();Check(!detail.Find("State Portrait").gameObject.activeSelf && !detail.Find("State Badge").gameObject.activeSelf,"missing picture hides block");Check(panel.stateDetails.GetComponentInParent<ScrollRect>().GetComponent<RectTransform>().rect.width==304,"missing picture restores width");yield return Capture("all-missing");source.content_cat_id=content;source.health_status="sick";accept();Check(!detail.Find("State Badge").gameObject.activeSelf,"sick hides mark");source.health_status="healthy";accept();Check(detail.Find("State Badge").gameObject.activeSelf,"healthy restores");
            panel.Open("cat-mike");var entry=Client.GetComponent<CatOutfitPanel>().entry.GetComponent<RectTransform>();Check(entry.anchoredPosition==new Vector2(24,-274) && panel.title.rectTransform.rect.width==310,"outfit left column entry");panel.Disconnect();Check(!detail.Find("State Badge").gameObject.activeSelf,"disconnect clears");accept();panel.Close();Check(JsonUtility.ToJson(backend.Simulation.State)==game,"views never change gameplay");
            // One non-Mike care transition uses native state and actual confirmation callbacks.
            backend.Simulation.State.cats.Single(c=>c.id=="cat-sora").stress=65;typeof(LocalCafeBackend).GetMethod("ResetJournal",flags).Invoke(backend,null);yield return Wait(()=>Client.UIStateReady && Client.CurrentState.cats.Single(c=>c.cat_id=="cat-sora").stress==65,"non-Mike care fixture refreshed");panel.Open("cat-sora");panel.Care();var items=Client.GetComponent<PythonItemsPanel>();items.ShowPurchase();var item=backend.Simulation.Rules.item_shop.First(r=>r.item.stress_relief>5);items.shop.value=Array.FindIndex(Client.CurrentState.items.shop,r=>r.choice==item.item.id);yield return Click(items.buy,"all-cat buy care");int revision=Client.CurrentState.revision;yield return Click(items.confirm,"all-cat confirm purchase");yield return Changed(revision);items.ShowCare();items.owned.value=Array.FindIndex(Client.CurrentState.items.owned,r=>r.item_id==item.item.id);items.cats.value=Array.FindIndex(Client.CurrentState.items.owned[items.owned.value].targets,r=>r.cat_id=="cat-sora");yield return Click(items.use,"all-cat use care");revision=Client.CurrentState.revision;yield return Click(items.confirm,"all-cat confirm care");yield return Changed(revision);items.Close();panel.Open("cat-sora");Check(!detail.Find("State Badge").gameObject.activeSelf,"non-Mike care clears high stress");yield return Capture("all-after-care");panel.Close();
            string file=Argument("--catcafe-full-roster-save");Check(!string.IsNullOrEmpty(file),"full roster fixture supplied");File.Copy(file,Path.Combine(output,"saves/slot-1.json"),true);Client.LoadSaved("1");yield return Wait(()=>Client.UIStateReady && Client.CurrentState.cats.Length==32,"32-cat native legacy load");
            foreach(var row in Client.CurrentState.cats.Skip(29)){panel.Open(row.cat_id);Check(panel.panel.activeSelf && panel.title.text==row.name+"の詳細","32-cat late individual "+row.cat_id);var image=preview.PythonCatTexture(row);Check(detail.Find("State Badge").gameObject.activeSelf==(image!=ContentPreview.CatPlaceholderTexture() && CatStateMark.Resolve(row)!=0),"32-cat expected mark "+row.cat_id);}yield return Capture("all-full-roster-last");panel.Close();revision=Client.CurrentState.revision;Client.SaveToSlot(9);yield return Changed(revision);
            Client.GetComponent<PythonReturnToTitle>().Return();yield return Wait(()=>FindObjectOfType<PythonTitleScreen>()?.CanManageSaves==true,"all-cat title ready");FindObjectOfType<PythonTitleScreen>().ResumeSaved("9");yield return Wait(()=>Client?.UIStateReady==true && Client.CurrentState.cats.Length==32,"all-cat title resume");panel=Client.GetComponent<PythonCatDetailsPanel>();var last=Client.CurrentState.cats.Last();panel.Open(last.cat_id);Check(panel.title.text==last.name+"の詳細","all-cat resumed late individual");yield return Capture("all-title-resume");
        }
        private IEnumerator AllStateImageChecks(bool legacy)
        {
            yield return Wait(()=>Client.UIStateReady,"all-state UI ready");var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;var panel=Client.GetComponent<PythonCatDetailsPanel>();var preview=Client.GetComponent<ContentPreview>();var view=backend.View();string before=JsonUtility.ToJson(backend.Simulation.State);Action accept=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(Client,new object[]{view});accept();
            var source=view.cats.Single(c=>c.cat_id==(legacy?"cat-mike":"cat-sora"));panel.Open(source.cat_id);var card=panel.panel.transform.Find("Details Card");var portrait=card.Find("State Portrait").GetComponent<RawImage>();
            foreach(int kind in new[]{1,2,3})
            {source.stress=kind==1?65:0;source.fatigue=kind==2?65:0;source.affinity=80;accept();var texture=CatStateImages.Resolve(source,kind);Check(texture!=null && portrait.texture==texture,"state image agrees "+kind);if(!legacy)Check(texture.width==140 && texture.height==120+(kind-1)*20,"Sora artwork identity "+kind);Check(card.Find("State Badge").gameObject.activeSelf,"state mark agrees "+kind);yield return Capture("state-image-"+kind);}
            var twin=JsonUtility.FromJson<PythonCatState>(JsonUtility.ToJson(source));twin.cat_id="rescue-state-100";twin.fatigue=65;twin.stress=0;view.cats=view.cats.Concat(new[]{twin}).ToArray();source.stress=65;accept();panel.Open(twin.cat_id);
            if(legacy){Check(CatStateImages.Resolve(twin,2)==null && portrait.texture==preview.PythonCatTexture(twin),"v1 remains Mike individual only");Check(JsonUtility.ToJson(backend.Simulation.State)==before,"legacy display never mutates game");yield return Capture("legacy-shared-identity-base");yield break;}
            Check(portrait.texture==CatStateImages.Resolve(twin,2) && portrait.texture.height==140,"same content different individual state");panel.Open(source.cat_id);Check(portrait.texture==CatStateImages.Resolve(source,1) && portrait.texture.height==120,"source retains own state");var kohaku=view.cats.Single(c=>c.cat_id=="cat-kohaku");kohaku.stress=65;accept();panel.Open(kohaku.cat_id);Check(portrait.texture.width==160,"different content uses different artwork");
            panel.Open(source.cat_id);source.health_status="sick";accept();Check(portrait.texture==preview.PythonCatTexture(source) && !card.Find("State Badge").gameObject.activeSelf,"sick uses base");source.health_status="healthy";source.activity="dispatched";accept();Check(portrait.texture==preview.PythonCatTexture(source),"away uses base");source.activity="cafe";source.stress=source.fatigue=source.affinity=0;accept();Check(portrait.texture==preview.PythonCatTexture(source),"normal never leaves previous image");Check(Client.GetComponent<CatOutfitPanel>().entry.gameObject.activeSelf,"outfit available to all cats");panel.Close();Check(JsonUtility.ToJson(backend.Simulation.State)==before,"state image rendering never mutates game");
            backend.Simulation.State.cats.Single(c=>c.id=="cat-sora").stress=65;typeof(LocalCafeBackend).GetMethod("ResetJournal",flags).Invoke(backend,null);yield return Wait(()=>Client.UIStateReady && Client.CurrentState.cats.Single(c=>c.cat_id=="cat-sora").stress==65,"native state refreshed");panel.Open("cat-sora");yield return Capture("state-before-care");panel.Care();var items=Client.GetComponent<PythonItemsPanel>();items.ShowPurchase();var item=backend.Simulation.Rules.item_shop.First(r=>r.item.stress_relief>5);items.shop.value=Array.FindIndex(Client.CurrentState.items.shop,r=>r.choice==item.item.id);yield return Click(items.buy,"state image buy care");int revision=Client.CurrentState.revision;yield return Click(items.confirm,"state image confirm purchase");yield return Changed(revision);items.ShowCare();items.owned.value=Array.FindIndex(Client.CurrentState.items.owned,r=>r.item_id==item.item.id);items.cats.value=Array.FindIndex(Client.CurrentState.items.owned[items.owned.value].targets,r=>r.cat_id=="cat-sora");yield return Click(items.use,"state image use care");revision=Client.CurrentState.revision;yield return Click(items.confirm,"state image confirm use");yield return Changed(revision);items.Close();panel.Open("cat-sora");var sora=Client.CurrentState.cats.Single(c=>c.cat_id=="cat-sora");Check(CatStateMark.Resolve(sora)==0 && portrait.texture==preview.PythonCatTexture(sora),"care recomputes to base");yield return Capture("state-after-care");panel.Close();
            string file=Argument("--catcafe-full-roster-save");Check(!string.IsNullOrEmpty(file),"32-cat fixture supplied");File.Copy(file,Path.Combine(output,"saves/slot-1.json"),true);Client.LoadSaved("1");yield return Wait(()=>Client.UIStateReady && Client.CurrentState.cats.Length==32,"state images legacy 32-cat load");
            foreach(var cat in Client.CurrentState.cats.Skip(29)){panel.Open(cat.cat_id);Check(panel.panel.activeSelf && panel.title.text==cat.name+"の詳細","state image late individual");var expected=CatStateImages.Resolve(cat,CatStateMark.Resolve(cat))??preview.PythonCatTexture(cat);Check(expected==ContentPreview.CatPlaceholderTexture()?!portrait.gameObject.activeSelf:portrait.gameObject.activeSelf && portrait.texture==expected,"state image late identity fallback");}panel.Close();revision=Client.CurrentState.revision;Client.SaveToSlot(9);yield return Changed(revision);Check(!File.ReadAllText(Path.Combine(output,"saves/slot-9.json")).Contains("CatCafeStateImages/"),"save has no state artwork path");
            Client.GetComponent<PythonReturnToTitle>().Return();yield return Wait(()=>FindObjectOfType<PythonTitleScreen>()?.CanManageSaves==true,"state images title ready");FindObjectOfType<PythonTitleScreen>().ResumeSaved("9");yield return Wait(()=>Client?.UIStateReady==true && Client.CurrentState.cats.Length==32,"state images title resume");panel=Client.GetComponent<PythonCatDetailsPanel>();var last=Client.CurrentState.cats.Last();panel.Open(last.cat_id);Check(panel.title.text==last.name+"の詳細","state images resumed individual");yield return Capture("state-title-resume");
        }
        private IEnumerator Run()
        {
            var dailyReference=Argument("--catcafe-daily-reference");if(!string.IsNullOrEmpty(dailyReference))StandaloneDailySmoke.Run(Path.Combine(output,"daily-checks"),dailyReference);
            Check(nativeDefault,"standalone default before QA override");Check(CafeBackends.Standalone,"standalone default");CompareRestProposals();var referencePath=Argument("--catcafe-details-reference");if(!string.IsNullOrEmpty(referencePath))checks.Add("Unity details references: "+StandaloneDetailsSmoke.Run(Path.Combine(output,"details-checks"),referencePath));
            yield return Wait(()=>FindObjectOfType<PythonTitleScreen>()?.CanManageSaves==true,"title ready without server");
            var title=FindObjectOfType<PythonTitleScreen>();yield return Capture("01-title");
            if(Array.Exists(Environment.GetCommandLineArgs(),a=>a=="--catcafe-all-outfits-resume-only")){yield return AllCatOutfitResume();yield break;}
            if(Array.Exists(Environment.GetCommandLineArgs(),a=>a=="--catcafe-all-state-resume-only"))
            {title.ResumeSaved("9");yield return Wait(()=>Client?.UIStateReady==true && Client.CurrentState.cats.Length==32,"all-state cross-process resume");var panel=Client.GetComponent<PythonCatDetailsPanel>();var cat=Client.CurrentState.cats.Last();panel.Open(cat.cat_id);Check(panel.panel.activeSelf && panel.title.text==cat.name+"の詳細","all-state cross-process identity");yield return Capture("state-cross-process");yield break;}
            if(Array.Exists(Environment.GetCommandLineArgs(),a=>a=="--catcafe-all-cat-resume-only"))
            {title.ResumeSaved("9");yield return Wait(()=>Client?.UIStateReady==true && Client.CurrentState.cats.Length==32,"all-cat cross-process resume");var panel=Client.GetComponent<PythonCatDetailsPanel>();var cat=Client.CurrentState.cats.Last();panel.Open(cat.cat_id);Check(panel.panel.activeSelf && panel.title.text==cat.name+"の詳細","all-cat cross-process individual");yield return Capture("all-cross-process");yield break;}
            if(Array.Exists(Environment.GetCommandLineArgs(),a=>a=="--catcafe-outfit-resume-only"))
            {
                title.ResumeSaved("9");yield return Wait(()=>Client?.UIStateReady==true,"F5 cross-process business ready");var panel=Client.GetComponent<PythonCatDetailsPanel>();panel.Open("cat-mike");Check(backend.CatOutfit("cat-mike")=="ribbon","F5 cross-process selection");bool absent=Array.Exists(Environment.GetCommandLineArgs(),a=>a=="--catcafe-outfit-no-images");var cat=Client.CurrentState.cats.Single(c=>c.cat_id=="cat-mike");Check(panel.panel.transform.Find("Details Card/State Portrait").GetComponent<RawImage>().texture==(absent?Client.GetComponent<ContentPreview>().PythonCatTexture(cat):CatOutfits.ImageFor("playtest-mike")),"F5 cross-process image");if(absent){Check(CatOutfits.ImageFor("playtest-mike")==null,"F5 normal build excludes fake image");var ui=Client.GetComponent<CatOutfitPanel>();yield return Click(ui.entry,"F5 missing-image selection");Check(!ui.ribbon.interactable,"F5 missing ribbon disabled");yield return Click(ui.normal,"F5 missing-image normal");yield return Click(ui.apply,"F5 missing-image clear");yield return Wait(()=>Client.UIStateReady && backend.CatOutfit("cat-mike")=="","F5 missing-image clear completes");}yield return Capture("f5-cross-process");yield break;
            }
            if(Array.Exists(Environment.GetCommandLineArgs(),a=>a=="--catcafe-ui-resume-only"))
            {
                title.ShowSaves();var saved=title.GetComponent<PythonSaveBrowser>();saved.OpenLoad();saved.Select(1);yield return Click(saved.action,"restart title load review");yield return Click(saved.confirm,"restart title load");
                yield return Wait(()=>Client?.CanManageSaves==true,"restart business ready");Check(Client.CurrentState.day==1 && Client.CurrentState.closed && Client.CurrentState.funds==1730,"cross process restores known settlement");Check(Client.CurrentState.cats[4].records.history.Contains("疲労変化：0 →") && !Client.CurrentState.cats[4].records.history.Contains("体調変化：記録なし"),"cross process restores complete daily record");
                var closePanel=Client.GetComponent<PythonClosingPanel>();int old=Client.CurrentState.revision;yield return Click(closePanel.next,"restart next day");yield return Changed(old);Check(Client.CurrentState.day==2 && Client.CurrentState.can_set_shifts,"restart continues next day");yield return Capture("09-restart");yield break;
            }
            yield return Click(title.newGame,"new game");title.objectives.value=Array.FindIndex(title.CurrentState.new_game_options,r=>r.choice=="popularity");Canvas.ForceUpdateCanvases();var startScroll=title.conditions.GetComponentInParent<ScrollRect>();
            Check(title.conditions.verticalOverflow==VerticalWrapMode.Overflow && title.conditions.raycastTarget,"start conditions render full scroll content");Check(startScroll.content.rect.height>=title.conditions.preferredHeight,"start conditions height includes all text");startScroll.verticalNormalizedPosition=0;yield return Capture("01-popularity-conditions-end");title.objectives.value=Array.FindIndex(title.CurrentState.new_game_options,r=>r.choice=="free");
            yield return Click(title.startGame,"new game confirmation");Check(title.conditions.text.Contains("開始の確認"),"new game review");yield return Click(title.startGame,"confirmed new game");
            yield return Wait(()=>Client?.CanManageSaves==true,"business scene ready");Check(Client.CurrentState.day==1 && Client.CurrentState.can_set_shifts,"initial preparation");Check(Client.CurrentState.rest_proposal.SequenceEqual(new[]{"cat-mugi","cat-sora","cat-tama"}),"rest proposal matches Python initial tie ordering");
            if(Array.Exists(Environment.GetCommandLineArgs(),a=>a=="--catcafe-state-images-check"))yield return StateImageChecks();
            // Outfit fixtures modify care records; run baseline regression in a fresh Player.
            if(Array.Exists(Environment.GetCommandLineArgs(),a=>a=="--catcafe-all-outfits-check")){yield return AllCatOutfitChecks();yield break;}
            if(Array.Exists(Environment.GetCommandLineArgs(),a=>a=="--catcafe-outfit-check")){yield return OutfitChecks();yield break;}
            if(Array.Exists(Environment.GetCommandLineArgs(),a=>a=="--catcafe-all-cat-marks-check")){yield return AllCatMarkChecks();yield break;}
            if(Array.Exists(Environment.GetCommandLineArgs(),a=>a=="--catcafe-all-state-images-check")){yield return AllStateImageChecks(false);yield break;}
            if(Array.Exists(Environment.GetCommandLineArgs(),a=>a=="--catcafe-v1-state-images-check")){yield return AllStateImageChecks(true);yield break;}
            var reference=new CafeSimulation(backend.Simulation.Rules,backend.Simulation.State.seed,"free");
            var owner=Client.gameObject;var prep=owner.GetComponent<PythonPreparationPanel>();prep.Open();yield return Capture("02-preparation");
            prep.SelectCat(0);int revision=Client.CurrentState.revision;yield return Click(owner.GetComponent<PythonPreparationRoster>().RestButton(0),"rest first cat");yield return Changed(revision);Check(!Client.CurrentState.cats[0].working,"rest command mapped");
            revision=Client.CurrentState.revision;yield return Click(owner.GetComponent<PythonPreparationRoster>().WorkButton(0),"work first cat");yield return Changed(revision);Check(Client.CurrentState.cats[0].working,"work command mapped");
            reference.Execute("set_shifts",working:reference.State.cats.Where(c=>c.id!=reference.State.cats[0].id && c.working).Select(c=>c.id).ToArray());reference.Execute("set_shifts",working:backend.Simulation.State.cats.Where(c=>c.working).Select(c=>c.id).ToArray());reference.Execute("start_business");
            var details=owner.GetComponent<PythonCatDetailsPanel>();details.Open(Client.CurrentState.cats[0].cat_id);Check(details.panel.activeSelf && !string.IsNullOrEmpty(details.details.text),"cat details rendered");yield return Capture("03-cat-details");foreach(int tab in new[]{1,2,3,4}){yield return Click(details.recordTabs[tab],"detail tab "+tab);Check(!string.IsNullOrWhiteSpace(details.details.text),"detail tab content "+tab);yield return Capture("03-details-tab-"+tab);if(tab==4){Canvas.ForceUpdateCanvases();Check(details.scroll.content.rect.height>details.scroll.viewport.rect.height,"long profile scrolls");details.scroll.verticalNormalizedPosition=0;yield return Capture("03-profile-end");}}details.Close();
            var events=owner.GetComponent<PythonEventListPanel>();events.Open();Check(events.panel.activeSelf,"event list rendered");events.Close();
            revision=Client.CurrentState.revision;yield return Click(prep.start,"start business");yield return Changed(revision);Check(!Client.CurrentState.can_set_shifts && !Client.CurrentState.closed,"business started");
            prep.Close();yield return Capture("04-service");
            for(int i=0;i<200 && !Client.CurrentState.closed;i++)
            {
                Check(string.IsNullOrEmpty(Client.CurrentState.required_action),"free day no unanswered event");revision=Client.CurrentState.revision;yield return Click(owner.GetComponent<ContentPreview>().GetSceneButton(2),"advance service");yield return Changed(revision);
                if(Client.CurrentState.tick==11){var service=owner.GetComponent<PythonServiceScreen>();Check(service.OccupiedCount>0,"occupied seats rendered");yield return Capture("04-active-service");var card=service.GetSeatCard(0);yield return Click(card.details,"active seat details");var seatDetails=owner.GetComponent<PythonSeatNarrativePanel>();Check(seatDetails!=null && seatDetails.panel.activeSelf,"active seat opens interaction details");seatDetails.panel.SetActive(false);
                    // Exercise the real one-second and quarter-second scheduler with seat receipts.
                    var seatAuto=owner.GetComponent<PythonAutoPanel>();
                    foreach(bool fastMode in new[]{false,true})
                    {
                        if(Client.Fast!=fastMode)yield return Click(seatAuto.speed,"seat speed change");
                        int startTick=Client.CurrentState.tick;
                        yield return Click(seatAuto.toggle,"seat automatic start");
                        yield return Wait(()=>Client.CurrentState.tick>=startTick+3,"seat timed progress");
                        yield return Click(seatAuto.toggle,"seat automatic stop");
                        yield return Wait(()=>Client.CanManageSaves,"seat timed progress settled");
                        service.ShowSeats();Canvas.ForceUpdateCanvases();
                        foreach(var seat in Client.CurrentState.seats.Where(s=>!string.IsNullOrEmpty(s.cat_id)))
                        {
                            int index=Array.IndexOf(Client.CurrentState.seats,seat);var visible=service.GetSeatCard(index);
                            Check(visible.stateText.text.Contains(seat.latest_action) && visible.stateText.text.Contains(seat.cat_reaction),"timed seat uses receipt");
                            Check(visible.picture.texture==(CafeNarrativeResources.Texture(seat.narrative_image)??owner.GetComponent<ContentPreview>().PythonCatTexture(visible.state)),"timed seat image matches receipt");
                            Check(visible.stateText.preferredHeight<=visible.stateText.rectTransform.rect.height+1,"timed seat text fits");
                        }
                        yield return Capture(fastMode?"04-seat-fast":"04-seat-normal");
                    }
                    Check(service.OccupiedCount>=2,"timed multiple seats visible");
                    if(Client.Fast)yield return Click(seatAuto.speed,"restore normal speed");
                }
            }
            Check(Client.CurrentState.closed,"full day closes");while(reference.State.phase!="closed")reference.Execute("advance_business");Check(CafeLegacyImport.GameDigest(reference.State)==CafeLegacyImport.GameDigest(backend.Simulation.State),"UI full day matches independent simulation");ComparePythonDay();var closing=owner.GetComponent<PythonClosingPanel>();Check(closing.panel.activeSelf,"closing panel opened");yield return Capture("05-closing");details.Open(Client.CurrentState.cats[4].cat_id);details.DailyRecords();Check(details.details.text.Contains("日目：1") && details.details.text.Contains("体力消耗：83"),"closing cat net stamina history");Check(!details.details.text.Contains("疲労変化：記録なし") && !details.details.text.Contains("体調変化：記録なし"),"complete daily health and fatigue rendered");yield return Capture("05-cat-history");details.scroll.verticalNormalizedPosition=0;yield return Capture("05-cat-history-end");details.Close();
            var browser=owner.GetComponent<PythonSaveBrowser>();yield return Click(closing.save,"open closing save");browser.Select(1);yield return Click(browser.action,"save slot 2 review");Check(browser.confirmation.activeSelf,"save confirmation");revision=Client.CurrentState.revision;yield return Click(browser.confirm,"save slot 2");yield return Changed(revision);Check(File.Exists(Path.Combine(output,"saves/slot-2.json")),"selected slot saved");browser.Close();
            revision=Client.CurrentState.revision;yield return Click(closing.next,"next day");yield return Changed(revision);Check(Client.CurrentState.day==2 && Client.CurrentState.can_set_shifts,"next day preparation");Check(Client.CurrentState.cats[4].records.history.Contains("体力消耗：83"),"known closed net stamina survives next day");
            browser.OpenLoad();browser.Select(1);yield return Click(browser.action,"load slot 2 review");yield return Click(browser.confirm,"load slot 2");yield return Wait(()=>Client.CanManageSaves && Client.CurrentState.day==1 && Client.CurrentState.closed,"business load restores closing");
            owner.GetComponent<PythonReturnToTitle>().Return();yield return Wait(()=>FindObjectOfType<PythonTitleScreen>()?.CanManageSaves==true,"return to title");
            Check(ReferenceEquals(CafeBackends.Local,backend),"session shared across scenes");title=FindObjectOfType<PythonTitleScreen>();title.ShowSaves();var titleBrowser=title.GetComponent<PythonSaveBrowser>();titleBrowser.OpenLoad();titleBrowser.Select(1);yield return Wait(()=>titleBrowser.action.interactable,"title save list ready");yield return Click(titleBrowser.action,"title load review");yield return Click(titleBrowser.confirm,"title load");yield return Wait(()=>Client?.CanManageSaves==true,"title resume to business");Check(Client.CurrentState.day==1 && Client.CurrentState.closed,"title resume preserves saved day");
            if(!string.IsNullOrEmpty(importPath))
            {
                owner=Client.gameObject;var legacy=owner.GetComponent<PythonLegacySavePanel>();legacy.Open();legacy.path.text=importPath;yield return Click(legacy.inspect,"legacy preview");yield return Wait(()=>legacy.resume.interactable,"converted import preview ready");Check(legacy.slots.options.Count>0,"empty import slots exposed");yield return Capture("06-import");
                legacy.path.text=importPath+"x";Check(!legacy.resume.interactable,"changed path invalidates preview");legacy.path.text=importPath;yield return Click(legacy.inspect,"preview again");yield return Wait(()=>legacy.resume.interactable,"preview refreshed");
                string oldId=Client.CurrentState.instance_id;yield return Click(legacy.resume,"import empty slot");yield return Wait(()=>Client.CanManageSaves && Client.CurrentState.instance_id!=oldId,"import command completed");Check(backend.Simulation.State.import_history!=null,"legacy migration anchor retained");Check(!legacy.panel.activeSelf,"import panel closes after success");
            }
            // Establish a deterministic unanswered event, then exercise its actual UI/save routes.
            var eventState=new CafeSimulation(backend.Simulation.Rules,Enumerable.Range(0,1000).First(i=>new CafeSimulation(backend.Simulation.Rules,i).CurrentStoreEvent.type=="trouble")).State;
            RestoreFixture(eventState);yield return Wait(()=>Client.CurrentState.instance_id==backend.Simulation.State.instance_id && Client.CanManageSaves,"event fixture displayed");owner=Client.gameObject;
            var store=owner.GetComponent<PythonStoreEventPanel>();var equipment=store.Dialogue;
            Check(!store.panel.activeSelf && !Client.CanProgressBusiness && backend.Simulation.StoreEventWaiting,"unanswered equipment conversation blocks service");
            Check(equipment.OpenPending(),"open equipment conversation");yield return null;yield return EquipmentChoices(equipment);
            string equipmentKey=equipment.PlaybackKey;yield return Capture("08-event");
            browser=owner.GetComponent<PythonSaveBrowser>();browser.OpenSave();browser.Select(3);yield return Click(browser.action,"event save review");revision=Client.CurrentState.revision;yield return Click(browser.confirm,"save unanswered event");yield return Changed(revision);browser.Close();
            revision=Client.CurrentState.revision;yield return Click(EquipmentButton(equipment,"accept"),"answer equipment conversation");yield return Changed(revision);Check(!backend.Simulation.StoreEventWaiting && backend.EquipmentResultPending,"event answered once with result pending");
            Check(!Client.CanProgressBusiness,"equipment result blocks service until completed");yield return Capture("08-event-result");yield return EquipmentFinish(equipment);
            browser.OpenLoad();browser.Select(3);yield return Click(browser.action,"event reload review");yield return Click(browser.confirm,"reload unanswered event");yield return Wait(()=>Client.CanManageSaves && backend.Simulation.StoreEventWaiting,"reload restores unanswered event");
            Check(!Client.CanProgressBusiness && !store.panel.activeSelf,"restored unanswered equipment blocks service");
            Check(equipment.OpenPending(),"open restored equipment conversation");yield return null;
            Check(equipment.PlaybackKey==equipmentKey && equipment.PlaybackChoosing,"equipment save restores conversation position");yield return Capture("08-event-restored");
            revision=Client.CurrentState.revision;yield return Click(EquipmentButton(equipment,"accept"),"answer restored equipment conversation");yield return Changed(revision);Check(!backend.Simulation.StoreEventWaiting,"restored event answer accepted");yield return EquipmentFinish(equipment);
            // A paused native autoplay save needs an explicit resume command, not only a UI flag.
            RestoreFixture(new CafeSimulation(backend.Simulation.Rules,0,"bond").State);backend.Simulation.StartAutoplay("bond","basic",1);backend.Simulation.PauseAutoplay();yield return Wait(()=>Client.CurrentState.autoplay?.paused==true,"paused autoplay displayed");
            var auto=owner.GetComponent<PythonAutoPlayPanel>();auto.Open();yield return Click(auto.proceed,"resume paused autoplay");yield return Wait(()=>backend.Simulation.State.autoplay.paused==false,"native autoplay resumed");Client.StopAutoPlay();yield return Wait(()=>Client.CanManageSaves && !Client.AutoPlayRunning,"autoplay stopped through UI client");
            yield return GoalPlaytestRegressions();
            Check(CafeBackends.Standalone,"all UI operations remain standalone");yield return Capture("07-resumed");
        }
        private IEnumerator GoalPlaytestRegressions()
        {
            RestoreFixture(new CafeSimulation(backend.Simulation.Rules,0,"popularity").State);
            yield return Wait(()=>Client.CurrentState.instance_id==backend.Simulation.State.instance_id && Client.CanManageSaves,"goal regression preparation ready");
            var owner=Client.gameObject;var auto=owner.GetComponent<PythonAutoPlayPanel>();var housing=owner.GetComponent<PythonHousingPanel>();
            auto.Open();housing.Open();yield return null;Check(housing.panel.activeSelf && !auto.panel.activeSelf,"housing closes autoplay overlay");
            yield return Click(housing.expand,"housing expansion review");yield return null;Check(housing.confirmation.activeSelf && housing.confirm.interactable,"housing confirmation remains accessible");housing.Cancel();housing.Close();
            Client.StartAutoPlay("popularity","basic",10);
            yield return Wait(()=>Client.BatchAutomatic && Client.AutoPlayRunning && Client.CanSaveNow && backend.Simulation.State.phase=="open","running autoplay yields to UI before completing day");
            int savedDay=backend.Simulation.State.day;Client.SaveToSlot(5);
            yield return Wait(()=>Client.CanSaveNow && Client.CurrentState.autoplay?.paused==true && !Client.BatchAutomatic,"running save pauses native autoplay");
            var saved=JsonUtility.FromJson<CafeSaveEnvelope>(File.ReadAllText(Path.Combine(output,"saves/slot-5.json")));Check(saved.state.autoplay.paused && saved.state.day==savedDay && saved.state.phase=="open","saved autoplay is paused midbusiness");
            int savedTick=backend.Simulation.State.tick;for(int frame=0;frame<4;frame++)yield return null;Check(backend.Simulation.State.tick==savedTick,"saved autoplay does not advance implicitly");
            Client.StopAutoPlay();yield return Wait(()=>Client.CanManageSaves && !Client.AutoPlayRunning,"stop before replacing paused session");
            var browser=owner.GetComponent<PythonSaveBrowser>();browser.OpenLoad();browser.Select(4);yield return Click(browser.action,"paused autoplay load review");yield return Click(browser.confirm,"paused autoplay load");
            yield return Wait(()=>Client.CanSaveNow && Client.CurrentState.autoplay?.paused==true,"load preserves explicit autoplay pause");browser.Close();auto.Open();yield return Click(auto.proceed,"saved autoplay explicit resume");yield return Wait(()=>Client.BatchAutomatic && !backend.Simulation.State.autoplay.paused,"saved autoplay resumes through screen");
            var pendingField=typeof(PythonStateClient).GetField("pending",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            yield return Wait(()=>Client.CanSaveNow && (pendingField.GetValue(Client) as PythonShiftCommand)?.kind=="step_autoplay","save remains enabled during queued autoplay operation");
            Client.SaveToSlot(6);Client.SaveToSlot(7);yield return Wait(()=>Client.CanSaveNow && Client.CurrentState.autoplay?.paused==true && !Client.BatchAutomatic,"queued autoplay operation finishes before deferred save");
            Check(File.Exists(Path.Combine(output,"saves/slot-6.json")) && !File.Exists(Path.Combine(output,"saves/slot-7.json")),"deferred save retains chosen slot and rejects duplicate request");
            Client.StopAutoPlay();yield return Wait(()=>Client.CanManageSaves && !Client.AutoPlayRunning,"resumed autoplay stops");
            var bond=new CafeSimulation(backend.Simulation.Rules,0,"bond");bond.StartAutoplay("bond","fast",10);for(int step=0;step<1000 && bond.State.autoplay.running;step++)bond.StepAutoplay();Check(bond.State.day==4 && bond.State.player_sessions.Count>0 && bond.IntakeWaiting,"legitimate autoplay reaches intake after exchanges");
            RestoreFixture(bond.State);yield return Wait(()=>Client.CurrentState.instance_id==bond.State.instance_id && Client.CanManageSaves,"blocked bond gameplay displayed");Check(!owner.GetComponent<PythonPlayerPanel>().panel.activeSelf && owner.GetComponent<PythonIntakePanel>().panel.activeSelf,"answered exchange does not cover unanswered intake");yield return Capture("10-bond-intake-priority");
        }
    }
}
