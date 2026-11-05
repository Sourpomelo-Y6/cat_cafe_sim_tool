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
        IEnumerator RosterFilterCare(string catId)
        {
            var detail = Client.GetComponent<PythonCatDetailsPanel>();
            detail.Care();
            var items = Client.GetComponent<PythonItemsPanel>();
            var item = backend.Simulation.Rules.item_shop.First(r => r.item.stress_relief > 5);
            items.ShowPurchase();
            items.shop.value = Array.FindIndex(Client.CurrentState.items.shop, r => r.choice == item.item.id);
            yield return Click(items.buy, "filter buy care item");
            int revision = Client.CurrentState.revision;
            yield return Click(items.confirm, "filter confirm purchase");
            yield return Changed(revision);
            items.ShowCare();
            items.owned.value = Array.FindIndex(Client.CurrentState.items.owned, r => r.item_id == item.item.id);
            items.cats.value = Array.FindIndex(Client.CurrentState.items.owned[items.owned.value].targets, r => r.cat_id == catId);
            yield return Click(items.use, "filter use care item");
            revision = Client.CurrentState.revision;
            yield return Click(items.confirm, "filter confirm care");
            yield return Changed(revision);
            Check(items.panel.activeSelf && detail.panel.activeSelf, "care can finish while target leaves filtered roster");
            items.Close();
            yield return Click(detail.panel.transform.Find("Details Card/Close").GetComponent<Button>(), "filter return from detail");
        }

        IEnumerator RosterCareFilterChecks()
        {
            File.Copy(Argument("--catcafe-full-roster-save"), Path.Combine(output, "saves/slot-1.json"), true);
            Client.LoadSaved("1");
            yield return Wait(() => Client.UIStateReady && Client.CurrentState.cats.Length == 32, "care filter roster loaded");
            var prep = Client.GetComponent<PythonPreparationPanel>();
            if (prep.panel.activeSelf) yield return Click(prep.close, "filter close preparation");
            var service = Client.GetComponent<PythonServiceScreen>();
            service.ShowCats();
            var roster = Client.GetComponent<PythonCatRoster>();
            var scroll = roster.content.GetComponentInParent<ScrollRect>();
            Check(!roster.CareOnly && roster.Count == 32, "all cats selected initially");
            var fixture = backend.View();
            foreach (var cat in fixture.cats) { cat.stress = cat.fatigue = 0; cat.affinity = 80; }
            fixture.cats[0].stress = 60;
            fixture.cats[1].fatigue = 60;
            fixture.cats[1].working = false;
            fixture.cats[1].content_cat_id = "qa-no-image";
            fixture.cats[2].stress = 60; fixture.cats[2].health_status = "sick";
            fixture.cats[3].stress = 60; fixture.cats[3].activity = "dispatched";
            fixture.cats[4].stress = 60; fixture.cats[4].activity = "adopted";
            roster.Apply(fixture);
            roster.ChangeCareFilter(true);
            Check(roster.Count == 2 && roster.GetCard(0).state.cat_id == fixture.cats[0].cat_id && roster.GetCard(1).state.cat_id == fixture.cats[1].cat_id, "threshold resting missing art and excluded states");
            var visibility = Client.GetComponent<PythonCatVisibility>();
            visibility.Change(true);
            roster.Apply(fixture);
            Check(roster.Count == 2 && roster.AllCatsButton.GetComponentInChildren<Text>().text.Contains("32"), "adopted display does not add care candidates");
            Check(visibility.Filter(fixture.cats).Length == 32, "shared visibility stays independent of care filter");
            roster.Disconnect();
            Check(!roster.AllCatsButton.interactable && !roster.CareCatsButton.interactable, "disconnected filters disabled");
            roster.Apply(fixture);
            Check(roster.CareOnly && roster.Count == 2, "same session reconnect retains filter");
            var different = JsonUtility.FromJson<PythonCafeState>(JsonUtility.ToJson(fixture));
            different.instance_id = "qa-new-care-session";
            roster.Apply(different);
            Check(!roster.CareOnly, "new session resets filter");
            visibility.Change(false);
            roster.Apply(backend.View());

            var pair = CombinedFamily();
            string target = pair[1].cat_id, sibling = pair[0].cat_id;
            foreach (var cat in backend.Simulation.State.cats) cat.stress = cat.fatigue = 0;
            var stressed = backend.Simulation.State.cats.Skip(14).Take(12).ToArray();
            foreach (var cat in stressed) cat.stress = 65;
            backend.Simulation.State.cats.Single(c => c.id == sibling).fatigue = 65;
            typeof(LocalCafeBackend).GetMethod("ResetJournal", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(backend, null);
            yield return Wait(() => Client.UIStateReady && Client.CurrentState.cats.Single(c => c.cat_id == target).stress == 65, "live care filter fixture refreshed");
            yield return Click(roster.CareCatsButton, "choose care targets");
            Check(roster.CareOnly && roster.Count == stressed.Length + 1, "live care candidate count");
            Canvas.ForceUpdateCanvases();
            var selected = Enumerable.Range(0, roster.Count).Select(roster.GetCard).Single(c => c.state.cat_id == target);
            float y = -((RectTransform)selected.transform).anchoredPosition.y;
            scroll.verticalNormalizedPosition = 1 - Mathf.Clamp01(y / (roster.content.rect.height - scroll.viewport.rect.height));
            yield return Capture("care-filter-before");
            yield return Click(selected.details, "open filtered late cat");
            yield return RosterFilterCare(target);
            Check(roster.CareOnly && roster.Count == stressed.Length && !Enumerable.Range(0, roster.Count).Any(i => roster.GetCard(i).state.cat_id == target), "only cared cat removed from filter");
            Check(Enumerable.Range(0, roster.Count).Any(i => roster.GetCard(i).state.cat_id == sibling), "same-family sibling stays a care target");
            Check(contentBoundsValid(scroll), "filtered return stays within scroll bounds");
            yield return Capture("care-filter-after");

            foreach (var cat in backend.Simulation.State.cats) cat.stress = cat.fatigue = 0;
            backend.Simulation.State.cats.Single(c => c.id == target).stress = 65;
            typeof(LocalCafeBackend).GetMethod("ResetJournal", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(backend, null);
            yield return Wait(() => Client.UIStateReady && roster.Count == 1 && roster.GetCard(0).state.cat_id == target, "last care target refreshed");
            yield return Click(roster.GetCard(0).details, "open last filtered cat");
            yield return RosterFilterCare(target);
            Check(roster.CareOnly && roster.Count == 0 && roster.ReturnToAllButton.gameObject.activeInHierarchy, "last care target shows empty state");
            yield return Capture("care-filter-empty");
            yield return Click(roster.ReturnToAllButton, "return to all cats");
            Check(!roster.CareOnly && roster.Count == 32 && scroll.verticalNormalizedPosition >= .99f, "all cats restored at top");
            yield return Click(roster.CareCatsButton, "filter empty again");
            int revision = Client.CurrentState.revision;
            Client.SaveToSlot(9);
            yield return Changed(revision);
            Check(roster.CareOnly, "normal save preserves active filter");
            Client.LoadSaved("9");
            yield return Wait(() => Client.UIStateReady && !roster.CareOnly && roster.Count == 32, "save load resets to all cats");
            yield return Capture("care-filter-reloaded");
        }

        bool contentBoundsValid(ScrollRect scroll)
        {
            float y = scroll.content.anchoredPosition.y;
            return y >= -1 && y <= Mathf.Max(0, scroll.content.rect.height - scroll.viewport.rect.height) + 1;
        }

        IEnumerator RosterStateMarkChecks()
        {
            File.Copy(Argument("--catcafe-full-roster-save"), Path.Combine(output, "saves/slot-1.json"), true);
            Client.LoadSaved("1");
            yield return Wait(() => Client.UIStateReady && Client.CurrentState.cats.Length == 32, "roster mark fixture loaded");
            var preparation = Client.GetComponent<PythonPreparationPanel>();
            if (preparation.panel.activeSelf) yield return Click(preparation.close, "close preparation to reveal roster");
            var roster = Client.GetComponent<PythonCatRoster>();
            var scroll = roster.content.GetComponentInParent<ScrollRect>();
            var service = Client.GetComponent<PythonServiceScreen>();
            service.ShowCats();
            Canvas.ForceUpdateCanvases();
            Check(roster.Count == 32 && roster.content.rect.height > scroll.viewport.rect.height, "32 cards remain scrollable");
            var legend = scroll.transform.Find("Roster State Legend");
            Check(legend.gameObject.activeInHierarchy && legend.GetComponentsInChildren<CatStateMarkGraphic>().Length == 3, "roster legend visible");

            var family = CombinedFamily();
            string first = family[0].cat_id, last = family[1].cat_id;
            PythonCatCard Card(string id) => Enumerable.Range(0, roster.Count).Select(roster.GetCard).Single(c => c.state.cat_id == id);
            CatStateMarkGraphic Mark(PythonCatCard card) => card.transform.Find("Roster State Mark")?.GetComponent<CatStateMarkGraphic>();
            int Kind(PythonCatCard card) => Mark(card)?.gameObject.activeSelf == true ? Mark(card).kind : 0;
            var fixture = backend.View();
            var target = fixture.cats.Single(c => c.cat_id == last);
            var sibling = fixture.cats.Single(c => c.cat_id == first);
            target.name = "派遣先から紹介されたとても長い名前の猫";
            target.stress = target.fatigue = 60;
            target.affinity = 80;
            target.working = false;
            target.content_cat_id = "qa-missing-cat-art";
            sibling.stress = 0;
            sibling.fatigue = 60;
            sibling.affinity = 80;
            fixture.seats = new[] { new PythonSeatState { cat_id = last, customer_id = "qa-customer" } };
            roster.Apply(fixture);
            Canvas.ForceUpdateCanvases();
            var card = Card(last);
            var mark = Mark(card);
            var customer = card.customerIcon.GetComponent<RectTransform>();
            Check(Kind(card) == 1 && Kind(Card(first)) == 2, "individual priority and resting cat without art");
            Check(card.customerIcon.activeSelf && card.catName.text.EndsWith("…") && card.catName.preferredWidth <= card.catName.rectTransform.rect.width + 1, "long name fits beside both icons");
            Check(card.catName.rectTransform.anchoredPosition.x + card.catName.rectTransform.rect.width < mark.rectTransform.anchoredPosition.x
                && mark.rectTransform.anchoredPosition.x + mark.rectTransform.rect.width < customer.anchoredPosition.x, "name and icons do not overlap");
            target.stress = target.fatigue = 0;
            roster.Apply(fixture);
            Check(Kind(card) == 3, "affinity threshold");
            target.health_status = "sick";
            roster.Apply(fixture);
            Check(Kind(card) == 0, "sick cat clears mark");
            target.health_status = "healthy";
            target.activity = "dispatched";
            roster.Apply(fixture);
            Check(Kind(card) == 0, "offsite cat clears mark");
            target.activity = "cafe";
            roster.Apply(fixture);
            roster.Disconnect();
            Check(Kind(card) == 0 && !legend.gameObject.activeSelf, "disconnect clears marks and legend");

            var native = backend.Simulation.State.cats.Single(c => c.id == last);
            native.stress = 65;
            backend.Simulation.State.cats.Single(c => c.id == first).fatigue = 65;
            typeof(LocalCafeBackend).GetMethod("ResetJournal", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(backend, null);
            yield return Wait(() => Client.UIStateReady && Card(last).state.stress == 65 && Kind(Card(last)) == 1 && Kind(Card(first)) == 2, "live roster refreshed from cat state");
            Canvas.ForceUpdateCanvases();
            var targetRect = (RectTransform)Card(last).transform;
            float offset = -targetRect.anchoredPosition.y - targetRect.rect.height * targetRect.pivot.y;
            float scrollPosition = 1 - Mathf.Clamp01(offset / (roster.content.rect.height - scroll.viewport.rect.height));
            scroll.verticalNormalizedPosition = scrollPosition;
            yield return Capture("roster-before-care");
            yield return Click(Card(last).details, "open late roster card");
            var detail = Client.GetComponent<PythonCatDetailsPanel>();
            Check(detail.panel.activeSelf && detail.title.text.Contains(Client.CurrentState.cats.Single(c => c.cat_id == last).name), "card opens correct cat details");
            detail.Care();
            var items = Client.GetComponent<PythonItemsPanel>();
            var item = backend.Simulation.Rules.item_shop.First(r => r.item.stress_relief > 5);
            items.ShowPurchase();
            items.shop.value = Array.FindIndex(Client.CurrentState.items.shop, r => r.choice == item.item.id);
            yield return Click(items.buy, "buy roster care item");
            int revision = Client.CurrentState.revision;
            yield return Click(items.confirm, "confirm roster purchase");
            yield return Changed(revision);
            items.ShowCare();
            items.owned.value = Array.FindIndex(Client.CurrentState.items.owned, r => r.item_id == item.item.id);
            items.cats.value = Array.FindIndex(Client.CurrentState.items.owned[items.owned.value].targets, r => r.cat_id == last);
            yield return Click(items.use, "use roster care item");
            revision = Client.CurrentState.revision;
            yield return Click(items.confirm, "confirm roster care");
            yield return Changed(revision);
            items.Close();
            yield return Click(detail.panel.transform.Find("Details Card/Close").GetComponent<Button>(), "return to roster");
            Check(Kind(Card(last)) == 0 && Kind(Card(first)) == 2, "care clears only target mark");
            Check(Mathf.Abs(scroll.verticalNormalizedPosition - scrollPosition) < .01f && !service.ShowingSeats, "return preserves roster scroll");
            yield return Capture("roster-after-care");
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
