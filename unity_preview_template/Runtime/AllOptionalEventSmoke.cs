using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using CatCafePreview.Standalone;
namespace CatCafePreview
{
    public partial class StandaloneScreenSmoke
    {
        CafeOptionalEventCatalog OptionalScenario(PythonCatState[] family,PythonCatState other)
        {
            var seed=CafeOptionalEventResources.Load().events.First(e=>e.target_kind=="content");
            return new CafeOptionalEventCatalog{version=2,pack_id=new string('0',32),events=new[]{family[0],other}.Select(c=>
            {var d=seed.Copy();d.content_cat_id=c.content_cat_id;d.event_id="optional."+c.content_cat_id+".story";d.min_day=1;d.min_affinity=0;d.revision=d.Digest();return d;}).ToArray()};
        }
        Button OptionalButton(OptionalEventConversationPanel panel,string name)=>(Button)typeof(AcquisitionConversationPanel).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(panel);
        IEnumerator OptionalComplete(OptionalEventConversationPanel panel,bool accepted)
        {
            var row=backend.OptionalEvent;row.progress.node=row.introduction.Length;row.progress.page_offset=0;panel.Refresh();yield return null;
            yield return Click(OptionalButton(panel,accepted?"accept":"decline"),"optional choice");Check(row.progress.branch_id==(accepted?"accepted":"declined"),"branch chosen for selected individual");
            row.progress.node=CafeOptionalEvents.Lines(row,row.progress.branch_id).Length;row.progress.page_offset=0;panel.Refresh();yield return null;
            yield return Click(OptionalButton(panel,"next"),"optional completion");Check(row.progress.completed,"selected individual completed");
        }
        IEnumerator AllOptionalChecks()
        {
            CafeOptions.Current.instant=true;
            string full=Argument("--catcafe-full-roster-save");File.Copy(full,Path.Combine(output,"saves/slot-1.json"),true);Client.LoadSaved("1");
            yield return Wait(()=>Client.UIStateReady && Client.CurrentState.cats.Length==32,"optional old 32-cat save load");
            var family=Client.CurrentState.cats.GroupBy(c=>c.content_cat_id).First(g=>g.Count()>1).Take(2).ToArray();var other=Client.CurrentState.cats.First(c=>c.content_cat_id!=family[0].content_cat_id);
            var catalog=OptionalScenario(family,other);backend.SetOptionalCatalog(catalog);
            string game=JsonUtility.ToJson(backend.Simulation.State),digest=CafeReplayJournal.Digest(backend.Simulation.State);
            var list=Client.GetComponent<PythonEventListPanel>();list.Open();list.SelectGroup(0);
            Check(list.RequiredPendingCount==0 && list.PendingCount>=3,"multiple optional candidates excluded from required count");yield return Capture("optional-32-list");
            var entry=list.content.GetComponentsInChildren<Button>().First(b=>b.gameObject.activeSelf && b.GetComponentInChildren<Text>().text.Contains("（"+family[0].cat_id+"）"));
            yield return Click(entry,"specific individual list entry");var panel=Client.GetComponent<OptionalEventConversationPanel>();
            Check(panel.Showing && backend.OptionalEvent.cat_id==family[0].cat_id,"list starts exact individual");Check(backend.ConversationBlocksProgress,"visible optional conversation protects gameplay input");yield return Capture("optional-individual-A");
            yield return Click(OptionalButton(panel,"close"),"close optional");Check(!backend.ConversationBlocksProgress,"closed optional allows normal gameplay");
            var detail=Client.GetComponent<PythonCatDetailsPanel>();detail.Open(family[1].cat_id);
            var detailEntry=detail.panel.transform.Find("Details Card/Optional Cat Event Entry").GetComponent<Button>();Check(detailEntry.gameObject.activeSelf && !detailEntry.interactable,"B cannot start while A remains unfinished");detail.Close();
            Check(panel.OpenEvent(family[0].cat_id),"resume A explicitly");yield return OptionalComplete(panel,true);
            Check(backend.OptionalForCat(family[0].cat_id)==null && backend.OptionalForCat(family[1].cat_id)!=null,"A completed B still unstarted");
            string firstName=backend.Replays.Load().entries.Single(e=>e.conversation_id==catalog.events[0].event_id+".v"+catalog.events[0].revision).cat_name;
            detail.Open(family[1].cat_id);yield return Click(detailEntry,"B detail start");yield return OptionalComplete(panel,true);
            Check(backend.OptionalEvents.Length==2 && backend.OptionalEvents.All(r=>r.progress.completed),"two independent completion records");
            Check(backend.Replays.Load().entries.Count(e=>e.conversation_id==catalog.events[0].event_id+".v"+catalog.events[0].revision)==1 && backend.Replays.Load().entries.Single(e=>e.conversation_id==catalog.events[0].event_id+".v"+catalog.events[0].revision).cat_name==firstName,"shared Extra keeps original identity");
            Check(game==JsonUtility.ToJson(backend.Simulation.State) && digest==CafeReplayJournal.Digest(backend.Simulation.State),"start choices completion never alter gameplay state or RNG");
            detail.Open(other.cat_id);yield return Click(detailEntry,"C detail start");var rowC=backend.OptionalEvent;
            rowC.progress.node=rowC.introduction.Length;rowC.progress.page_offset=0;panel.Refresh();yield return null;yield return Click(OptionalButton(panel,"decline"),"C different branch");
            rowC.progress.node=1;rowC.progress.page_offset=2;panel.Refresh();yield return Capture("optional-partial-result");
            File.WriteAllText(Path.Combine(output,"frozen-story.json"),JsonUtility.ToJson(rowC));
            yield return Click(OptionalButton(panel,"close"),"close before partial save");int revision=Client.CurrentState.revision;Client.SaveToSlot(9);yield return Changed(revision);
            var changed=catalog.Copy();foreach(var d in changed.events){d.introduction[0].text="改稿後の新しい本文。";d.revision=d.Digest();}backend.SetOptionalCatalog(changed);
            Check(panel.OpenEvent(other.cat_id) && JsonUtility.ToJson(backend.OptionalEvent)==File.ReadAllText(Path.Combine(output,"frozen-story.json")),"revision update keeps active receipt");
            yield return Click(OptionalButton(panel,"close"),"close old revision");backend.SetOptionalCatalog(new CafeOptionalEventCatalog{version=2,pack_id=new string('0',32),events=new CafeOptionalDefinition[0]});
            Check(panel.OpenEvent(other.cat_id),"removed definition still resumes");yield return Click(OptionalButton(panel,"close"),"close removed definition record");
            Client.GetComponent<PythonReturnToTitle>().Return();yield return Wait(()=>FindObjectOfType<PythonTitleScreen>()?.CanManageSaves==true,"optional title ready");FindObjectOfType<PythonTitleScreen>().ResumeSaved("9");yield return Wait(()=>Client?.UIStateReady==true && Client.CurrentState.cats.Length==32,"optional title 32-cat resume");
            Check(backend.OptionalEvents.Length==3 && backend.OptionalEvents.Count(r=>r.progress.completed)==2,"title restores every individual record");
            panel=Client.GetComponent<OptionalEventConversationPanel>()??Client.gameObject.AddComponent<OptionalEventConversationPanel>();var pending=backend.OptionalCandidates.Single(c=>c.resume);
            Check(panel.OpenEvent(pending.cat_id,pending.event_id,pending.revision),"title resumes frozen pending record");yield return Capture("optional-title-resume");panel.Close();
        }
        // The normal build has no authored state/ribbon art for this family.
        // Seed the existing resource caches only inside the smoke-test process.
        void CombinedPortraitFixtures(PythonCatState cat)
        {
            const string image = "images/combined.png";
            const string root = "CombinedSmoke/";
            CatOutfits.ImageFor(cat.cat_id, cat.content_cat_id);
            CatStateImages.Resolve(cat, 1);
            var flags = BindingFlags.Static | BindingFlags.NonPublic;
            var outfit = typeof(CatOutfits);
            outfit.GetField("pack", flags).SetValue(null, new CatOutfitPack
            {
                version = 2,
                outfits = new[] { new CatOutfitBinding { content_cat_id = cat.content_cat_id, outfit_id = "ribbon", label = "QA Ribbon", image = image } }
            });
            outfit.GetField("root", flags).SetValue(null, root);
            var images = typeof(CatStateImages);
            images.GetField("pack", flags).SetValue(null, new CatStateImagePack
            {
                version = 2,
                bindings = new[] { new CatStateImageBinding { content_cat_id = cat.content_cat_id, state = "stress_high", image = image } }
            });
            images.GetField("root", flags).SetValue(null, root);
            foreach (var type in new[] { outfit, images })
            {
                var textures = (System.Collections.Generic.Dictionary<string, Texture2D>)type.GetField("textures", flags).GetValue(null);
                var texture = new Texture2D(32, 32) { name = type.Name + " Smoke Portrait" };
                texture.SetPixels(Enumerable.Repeat(type == outfit ? Color.magenta : Color.cyan, 1024).ToArray());
                texture.Apply();
                textures[root + "images/combined"] = texture;
            }
        }

        PythonCatState[] CombinedFamily()
        {
            return Client.CurrentState.cats.Where(c => c.health_status == "healthy" && c.activity == "cafe")
                .GroupBy(c => c.content_cat_id).First(g => g.Count() >= 2).Take(2).ToArray();
        }

        IEnumerator CombinedCatStart()
        {
            CafeOptions.Current.instant = true;
            File.Copy(Argument("--catcafe-full-roster-save"), Path.Combine(output, "saves/slot-1.json"), true);
            Client.LoadSaved("1");
            yield return Wait(() => Client.UIStateReady && Client.CurrentState.cats.Length == 32, "combined roster loaded");
            var pair = CombinedFamily();
            var a = pair[0];
            var b = pair[1];
            Check(CatStateMark.Resolve(a) == 0, "neutral cat fixture");
            CombinedPortraitFixtures(a);
            var pack = CafeOptionalEventJson.Parse(File.ReadAllText(Argument("--catcafe-combined-catalog")));
            var definition = pack.events.First(d => d.target_kind == "content");
            definition.content_cat_id = a.content_cat_id;
            definition.event_id = "optional." + a.content_cat_id + ".story";
            definition.min_day = 1;
            definition.min_affinity = 0;
            definition.revision = definition.Digest();
            pack.events = new[] { definition };
            backend.SetOptionalCatalog(pack);

            var detail = Client.GetComponent<PythonCatDetailsPanel>();
            detail.Open(a.cat_id);
            var ui = Client.GetComponent<CatOutfitPanel>();
            yield return Click(ui.entry, "combined outfit entry");
            yield return Click(ui.ribbon, "combined ribbon candidate");
            yield return Click(ui.apply, "combined apply outfit");
            yield return Wait(() => Client.UIStateReady && backend.CatOutfit(a.cat_id) == "ribbon", "A outfit selected");
            var card = detail.panel.transform.Find("Details Card");
            var portrait = card.Find("State Portrait").GetComponent<RawImage>();
            var ribbon = CatOutfits.ImageFor(a.cat_id, a.content_cat_id);
            Check(ribbon != null && portrait.texture == ribbon, "A shows its ribbon");
            detail.Open(b.cat_id);
            Check(backend.CatOutfit(b.cat_id) == "" && portrait.texture != ribbon, "same-family B stays in normal outfit");
            detail.Open(a.cat_id);
            yield return Click(card.Find("Optional Cat Event Entry").GetComponent<Button>(), "combined A optional entry");
            var panel = Client.GetComponent<OptionalEventConversationPanel>();
            var row = backend.OptionalEvent;
            Check(panel.Showing && row.cat_id == a.cat_id, "A opens its own conversation");
            for (int step = 0; step < 20 && !OptionalButton(panel, "accept").gameObject.activeInHierarchy; step++)
            {
                yield return Click(OptionalButton(panel, "next"), "read combined introduction");
                yield return null;
            }
            yield return Click(OptionalButton(panel, "decline"), "choose combined declined branch");
            Check(row.progress.branch_id == "declined", "combined branch selected");
            yield return null;
            yield return Click(OptionalButton(panel, "close"), "pause combined conversation");
            detail.Open(b.cat_id);
            Check(backend.OptionalForCat(b.cat_id)?.resume == false && !card.Find("Optional Cat Event Entry").GetComponent<Button>().interactable, "B is unstarted while A is pending");
            detail.Close();
            var native = backend.Simulation.State.cats.Single(c => c.id == a.cat_id);
            native.stress = 65;
            typeof(LocalCafeBackend).GetMethod("ResetJournal", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(backend, null);
            yield return Wait(() => Client.UIStateReady && Client.CurrentState.cats.Single(c => c.cat_id == a.cat_id).stress == 65, "stress fixture refreshed");
            detail.Open(a.cat_id);
            Check(card.Find("State Caption").GetComponent<Text>().text == "ストレス高め", "stress mark in combined save");
            Check(portrait.texture == CatStateImages.Resolve(Client.CurrentState.cats.Single(c => c.cat_id == a.cat_id), 1) && portrait.texture != ribbon, "state portrait overrides selected ribbon");
            yield return Capture("combined-stress-before-save");
            detail.Close();
            File.WriteAllText(Path.Combine(output, "frozen-story.json"), JsonUtility.ToJson(row));
            Client.SaveToSlot(9);
            yield return Wait(() => Client.UIStateReady && File.Exists(Path.Combine(output, "saves/slot-9.json")), "combined slot written");
        }

        IEnumerator CombinedCatResume()
        {
            CafeOptions.Current.instant = true;
            FindObjectOfType<PythonTitleScreen>().ResumeSaved("9");
            yield return Wait(() => Client?.UIStateReady == true && Client.CurrentState.cats.Length == 32, "combined fresh process load");
            var a = CombinedFamily()[0];
            var b = CombinedFamily()[1];
            CombinedPortraitFixtures(a);
            var row = backend.OptionalEvents.Single();
            Check(row.cat_id == a.cat_id && JsonUtility.ToJson(row) == File.ReadAllText(Argument("--catcafe-optional-frozen")), "A resumes exact frozen branch and position");
            Check(backend.CatOutfit(a.cat_id) == "ribbon" && backend.CatOutfit(b.cat_id) == "", "separate outfits restored");
            var detail = Client.GetComponent<PythonCatDetailsPanel>();
            detail.Open(a.cat_id);
            var card = detail.panel.transform.Find("Details Card");
            var portrait = card.Find("State Portrait").GetComponent<RawImage>();
            var stateTexture = CatStateImages.Resolve(a, 1);
            Check(stateTexture != null && portrait.texture == stateTexture && card.Find("State Badge").gameObject.activeSelf, "stress portrait and badge restored");

            var item = backend.Simulation.Rules.item_shop.First(r => r.item.stress_relief > 5);
            detail.Care();
            var items = Client.GetComponent<PythonItemsPanel>();
            items.ShowPurchase();
            items.shop.value = Array.FindIndex(Client.CurrentState.items.shop, r => r.choice == item.item.id);
            yield return Click(items.buy, "buy combined care item");
            int revision = Client.CurrentState.revision;
            yield return Click(items.confirm, "confirm combined purchase");
            yield return Changed(revision);
            items.ShowCare();
            items.owned.value = Array.FindIndex(Client.CurrentState.items.owned, r => r.item_id == item.item.id);
            items.cats.value = Array.FindIndex(Client.CurrentState.items.owned[items.owned.value].targets, r => r.cat_id == a.cat_id);
            yield return Click(items.use, "use combined care item");
            revision = Client.CurrentState.revision;
            yield return Click(items.confirm, "confirm combined care");
            yield return Changed(revision);
            items.Close();
            detail.Open(a.cat_id);
            Check(!card.Find("State Badge").gameObject.activeSelf && portrait.texture == CatOutfits.ImageFor(a.cat_id, a.content_cat_id), "care clears mark and restores ribbon");
            yield return Capture("combined-ribbon-after-care");
            yield return Click(card.Find("Optional Cat Event Entry").GetComponent<Button>(), "resume combined event from detail");
            var panel = Client.GetComponent<OptionalEventConversationPanel>();
            Check(panel.Showing && backend.OptionalEvent.cat_id == a.cat_id, "care leaves A's event resumable");
            for (int step = 0; step < 20 && !backend.OptionalEvents.Single().progress.completed; step++)
            {
                yield return Click(OptionalButton(panel, "next"), "read combined result");
                yield return null;
            }
            row = backend.OptionalEvents.Single();
            Check(row.progress.completed && row.cat_id == a.cat_id, "only A completed event");
            var receipt = backend.Replays.Load().entries.Single(e => e.conversation_id == row.progress.conversation_id);
            Check(receipt.cat_name == a.name && receipt.branch_id == "declined", "Extra archives A and declined branch");
            Client.GetComponent<PythonReturnToTitle>().Return();
            yield return Wait(() => FindObjectOfType<PythonTitleScreen>()?.CanManageSaves == true, "combined title ready");
            var extra = FindObjectOfType<TitleExtraPanel>();
            yield return Click(extra.Entry, "open combined Extra");
            var entry = extra.Panel.GetComponentsInChildren<Button>().Single(button => button.name == receipt.Key.Replace('/', '_'));
            Check(entry.interactable && entry.GetComponentInChildren<Text>().text.Contains(a.name), "Extra displays A name on unlocked branch");
            yield return Click(entry, "play combined Extra branch");
            yield return Capture("combined-extra-replay");
        }
        IEnumerator AllOptionalResume()
        {
            CafeOptions.Current.instant=true;FindObjectOfType<PythonTitleScreen>().ResumeSaved("9");yield return Wait(()=>Client?.UIStateReady==true && Client.CurrentState.cats.Length==32,"optional cross-process 32-cat resume");
            var pending=backend.OptionalCandidates.Single(c=>c.resume);var panel=Client.GetComponent<OptionalEventConversationPanel>()??Client.gameObject.AddComponent<OptionalEventConversationPanel>();
            string frozen=Argument("--catcafe-optional-frozen");Check(JsonUtility.ToJson(backend.OptionalEvents.Single(r=>!r.progress.completed))==File.ReadAllText(frozen),"cross-process frozen text and page retained");
            Check(panel.OpenEvent(pending.cat_id,pending.event_id,pending.revision),"cross-process explicit resume");yield return Capture("optional-cross-process");
            var row=backend.OptionalEvent;row.progress.node=row.declined.Length;row.progress.page_offset=0;panel.Refresh();yield return null;yield return Click(OptionalButton(panel,"next"),"cross-process finish");
            Check(backend.OptionalEvents.Count(r=>r.progress.completed)==3 && !backend.ConversationBlocksProgress,"cross-process completes only pending individual");
            Check(backend.Replays.Load().entries.Any(e=>e.conversation_id==row.progress.conversation_id && e.branch_id=="declined"),"cross-process Extra chosen branch archived");
            int revision=Client.CurrentState.revision;Client.SaveToSlot(8);yield return Changed(revision);Client.LoadSaved("8");yield return Wait(()=>Client.UIStateReady && backend.OptionalEvents.Length==3 && backend.OptionalEvents.All(r=>r.progress.completed),"completed records save and reload");
        }
    }
}
