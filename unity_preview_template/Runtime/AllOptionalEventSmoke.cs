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
