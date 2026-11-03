using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace CatCafePreview
{
    public partial class StandaloneScreenSmoke
    {
        private IEnumerator AllCatOutfitChecks()
        {
            yield return Wait(()=>Client.UIStateReady,"all outfits ready");
            var panel=Client.GetComponent<PythonCatDetailsPanel>();
            var ui=Client.GetComponent<CatOutfitPanel>();
            string game=JsonUtility.ToJson(backend.Simulation.State);
            foreach(string id in new[]{"cat-mike","cat-sora"})
            {
                panel.Open(id);ui=Client.GetComponent<CatOutfitPanel>();
                yield return Click(ui.entry,"outfit entry "+id);
                Check(ui.host.transform.Find("Card/Title").GetComponent<Text>().text==Client.CurrentState.cats.Single(c=>c.cat_id==id).name+"の見た目","individual dialog title");
                yield return Click(ui.ribbon,"choose ribbon "+id);
                yield return Click(ui.cancel,"cancel ribbon "+id);
                Check(backend.CatOutfit(id)=="","cancel does not select");
                yield return Click(ui.entry,"reopen "+id);yield return Click(ui.ribbon,"candidate "+id);yield return Click(ui.apply,"apply "+id);
                yield return Wait(()=>Client.UIStateReady && backend.CatOutfit(id)=="ribbon","selection response "+id);
                var row=Client.CurrentState.cats.Single(c=>c.cat_id==id);
                Check(panel.panel.transform.Find("Details Card/State Portrait").GetComponent<RawImage>().texture==CatOutfits.ImageFor(id,row.content_cat_id),"individual ribbon displayed");
                yield return Capture("outfit-"+id);panel.Close();
            }
            Check(backend.CatOutfit("cat-mike")=="ribbon" && backend.CatOutfit("cat-sora")=="ribbon","both selections retained");
            Check(JsonUtility.ToJson(backend.Simulation.State)==game,"outfit choices preserve gameplay");
            // Dynamic IDs and long labels use the same UI/render path without altering gameplay.
            var view=backend.View();var source=view.cats.Single(c=>c.cat_id=="cat-sora");var flags=BindingFlags.NonPublic|BindingFlags.Instance;
            Action accept=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(Client,new object[]{view});
            var clone=JsonUtility.FromJson<PythonCatState>(JsonUtility.ToJson(source));clone.cat_id="dispatch-rescue-100";clone.name="派遣先から紹介された長い名前の猫";clone.outfit_id="";
            view.cats=view.cats.Concat(new[]{clone}).ToArray();accept();panel.Open(clone.cat_id);
            var card=panel.panel.transform.Find("Details Card");var entry=ui.entry.GetComponent<RectTransform>();
            Check(entry.anchoredPosition==new Vector2(24,-274) && entry.rect.width==304,"fixed outfit entry position");
            Check(panel.title.rectTransform.rect.width==310 && panel.title.rectTransform.rect.height==56 && panel.title.fontSize==22,"long title layout");
            Check(card.Find("State Portrait").GetComponent<RawImage>().texture!=CatOutfits.ImageFor(clone.cat_id,clone.content_cat_id),"shared family different choice");
            yield return Capture("outfit-long-detail");Canvas.ForceUpdateCanvases();Check(panel.title.cachedTextGenerator.characterCountVisible>=panel.title.text.Length,"long title fully generated: "+panel.title.cachedTextGenerator.characterCountVisible+"/"+panel.title.text.Length+" font="+panel.title.cachedTextGenerator.fontSizeUsedForBestFit);
            yield return Click(ui.entry,"dynamic dialog");yield return Capture("outfit-long-dialog");ui.Close();
            clone.outfit_id="ribbon";accept();Check(card.Find("State Portrait").GetComponent<RawImage>().texture==CatOutfits.ImageFor(clone.cat_id,clone.content_cat_id),"shared family selected");
            clone.stress=65;accept();Check(card.Find("State Portrait").GetComponent<RawImage>().texture==CatStateImages.Resolve(clone,1),"F4 overrides shared ribbon");
            yield return Capture("outfit-state-priority");clone.stress=0;clone.content_cat_id="not-registered";accept();
            Check(!card.Find("State Portrait").gameObject.activeSelf,"missing all artwork hides portrait");
            var health=panel.stateDetails.GetComponentInParent<ScrollRect>();
            Check(health.GetComponent<RectTransform>().rect.width==304 && health.GetComponent<RectTransform>().rect.height==188 && health.viewport.rect.height==176,"wide stats reserve outfit row");
            yield return Click(ui.entry,"missing artwork dialog");Check(!ui.ribbon.interactable && ui.normal.interactable,"missing artwork can clear");ui.Close();yield return Capture("outfit-missing-detail");
            panel.Close();
            // Return to backend-owned view before exercising actual care and saves.
            accept=()=>typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(Client,new object[]{backend.View()});accept();
            backend.Simulation.State.cats.Single(c=>c.id=="cat-sora").stress=65;typeof(LocalCafeBackend).GetMethod("ResetJournal",flags).Invoke(backend,null);
            yield return Wait(()=>Client.UIStateReady && Client.CurrentState.cats.Single(c=>c.cat_id=="cat-sora").stress==65,"care fixture refresh");
            panel.Open("cat-sora");panel.Care();var items=Client.GetComponent<PythonItemsPanel>();items.ShowPurchase();var item=backend.Simulation.Rules.item_shop.First(r=>r.item.stress_relief>5);
            items.shop.value=Array.FindIndex(Client.CurrentState.items.shop,r=>r.choice==item.item.id);yield return Click(items.buy,"outfit buy care");int revision=Client.CurrentState.revision;yield return Click(items.confirm,"outfit purchase confirm");yield return Changed(revision);
            items.ShowCare();items.owned.value=Array.FindIndex(Client.CurrentState.items.owned,r=>r.item_id==item.item.id);items.cats.value=Array.FindIndex(Client.CurrentState.items.owned[items.owned.value].targets,r=>r.cat_id=="cat-sora");yield return Click(items.use,"outfit care");revision=Client.CurrentState.revision;yield return Click(items.confirm,"outfit care confirm");yield return Changed(revision);items.Close();panel.Open("cat-sora");
            Check(card.Find("State Portrait").GetComponent<RawImage>().texture==CatOutfits.ImageFor("cat-sora","playtest-sora"),"care returns to chosen outfit");panel.Close();
            revision=Client.CurrentState.revision;Client.SaveToSlot(9);yield return Changed(revision);
            panel.Open("cat-mike");yield return Click(ui.entry,"clear Mike dialog");yield return Click(ui.normal,"clear Mike candidate");yield return Click(ui.apply,"clear Mike apply");yield return Wait(()=>Client.UIStateReady && backend.CatOutfit("cat-mike")=="","Mike cleared");Check(backend.CatOutfit("cat-sora")=="ribbon","clearing Mike preserves Sora");panel.Close();
            Client.LoadSaved("9");yield return Wait(()=>Client.UIStateReady && backend.CatOutfit("cat-mike")=="ribbon","multi selection load");
            string full=Argument("--catcafe-full-roster-save");File.Copy(full,Path.Combine(output,"saves/slot-1.json"),true);Client.LoadSaved("1");yield return Wait(()=>Client.UIStateReady && Client.CurrentState.cats.Length==32,"32 cats old save");
            Check(backend.CatOutfit("cat-sora")=="","legacy load resets selections");
            panel.Open("cat-sora");ui=Client.GetComponent<CatOutfitPanel>();yield return Click(ui.entry,"32 cat outfit dialog");yield return Click(ui.ribbon,"32 cat ribbon");yield return Click(ui.apply,"32 cat apply");yield return Wait(()=>Client.UIStateReady && backend.CatOutfit("cat-sora")=="ribbon","32 cat choice saved");panel.Close();
            revision=Client.CurrentState.revision;Client.SaveToSlot(9);yield return Changed(revision);
            Client.GetComponent<PythonReturnToTitle>().Return();yield return Wait(()=>FindObjectOfType<PythonTitleScreen>()?.CanManageSaves==true,"all outfit title ready");FindObjectOfType<PythonTitleScreen>().ResumeSaved("9");yield return Wait(()=>Client?.UIStateReady==true && Client.CurrentState.cats.Length==32,"all outfit title resume");
            Check(backend.CatOutfit("cat-sora")=="ribbon","all outfit title restores selection");panel=Client.GetComponent<PythonCatDetailsPanel>();panel.Open(Client.CurrentState.cats.Last().cat_id);yield return Capture("outfit-32-last");
        }
        private IEnumerator AllCatOutfitResume()
        {
            FindObjectOfType<PythonTitleScreen>().ResumeSaved("9");yield return Wait(()=>Client?.UIStateReady==true && Client.CurrentState.cats.Length==32,"all outfit cross-process 32 cats");
            Check(backend.CatOutfit("cat-sora")=="ribbon","all outfit cross-process selection");var panel=Client.GetComponent<PythonCatDetailsPanel>();panel.Open("cat-sora");
            var row=Client.CurrentState.cats.Single(c=>c.cat_id=="cat-sora");var texture=CatOutfits.Resolve(row)??Client.GetComponent<ContentPreview>().PythonCatTexture(row);
            var portrait=panel.panel.transform.Find("Details Card/State Portrait").GetComponent<RawImage>();Check(texture==ContentPreview.CatPlaceholderTexture()?!portrait.gameObject.activeSelf:portrait.texture==texture,"cross-process recomputes image");
            if(Array.Exists(Environment.GetCommandLineArgs(),a=>a=="--catcafe-outfit-no-images"))
            {
                var ui=Client.GetComponent<CatOutfitPanel>();yield return Click(ui.entry,"normal build missing-art dialog");Check(!ui.ribbon.interactable,"normal build excludes fake artwork");yield return Click(ui.normal,"normal build choose normal");yield return Click(ui.apply,"normal build clear");yield return Wait(()=>Client.UIStateReady && backend.CatOutfit("cat-sora")=="","normal build clears saved choice");
            }
            yield return Capture("outfit-cross-process");
        }
    }
}
