using CatCafePreview.Standalone;
static partial class Program
{
    static void OptionalAuthoringChecks(string folder)
    {
        CafeOptionalEventCatalog Read(string name)=>CafeOptionalEventJson.Parse(File.ReadAllText(Path.Combine(folder,name,"optional_events.json")));
        var first=Read("first");var second=Read("second");first.Validate();second.Validate();
        Assert(first.events[0].revision!=second.events[0].revision,"Content edits change the revision");
        var state=new CafeSnapshot{day=2};state.cats.Add(new CafeCat{id="cat-mike"});state.player_cats.Add(new CafePlayerCat{cat_id="cat-mike",affinity=20});
        var old=first.events[0].Start(state,"ミケ","frozen-image");old.progress.node=1;old.progress.page_offset=4;
        string frozen=Encode(old);var copied=first.Copy();copied.events[0].introduction[0].text="別の下書き";
        Assert(first.events[0].Digest()==first.events[0].revision,"Catalogue copy does not share line objects");
        var receipt=Decode<CafeOptionalEvent>(frozen);CafeOptionalEvents.Validate(new[]{receipt});
        Assert(Encode(receipt)==frozen,"Started receipt survives definition replacement");
        var newer=second.events[0].Start(state,"ミケ","new-image");Assert(newer.title!=old.title && newer.introduction[0].text!=old.introduction[0].text,"New start uses rewritten content");
        Assert(CafeReadStore.Key(old.progress.conversation_id,"introduction",old.introduction,0)!=CafeReadStore.Key(newer.progress.conversation_id,"introduction",newer.introduction,0),"Changed prose does not inherit read skip");
        var boundary=second.events[0].Copy();boundary.min_day=3;boundary.min_affinity=30;boundary.revision=boundary.Digest();boundary.Validate();
        Assert(!boundary.Eligible(state),"Edited day/affinity condition applies");state.day=3;Assert(!boundary.Eligible(state),"Affinity still required");state.player_cats[0].affinity=30;Assert(boundary.Eligible(state),"Edited threshold boundary");
        foreach(var definition in new[]{first.events[0],second.events[0]})
        {
            var row=definition.Start(state,"ミケ","");row.progress.node=row.introduction.Length;CafeOptionalEvents.Choose(row,true);row.progress.node=row.accepted.Length;CafeOptionalEvents.Finish(row);CafeOptionalEvents.Validate(new[]{row});
        }
        var profile=new CafeReplayProfile{entries=new[]{old,newer}.Select(r=>new CafeReplayEntry{conversation_id=r.progress.conversation_id,branch_id="accepted",title=r.title,cat_id=r.content_cat_id,cat_name=r.cat_name,lines=r.introduction}).ToArray()};
        CafeReplayCatalog.Validate(profile);var definitions=CafeReplayCatalog.Definitions(profile,new CafeOptionalEventCatalog{version=1,pack_id=new string('0',32),events=new CafeOptionalDefinition[0]});
        Assert(profile.entries.All(e=>definitions.Any(d=>d.Key==e.Key)),"Removed catalogue retains both historical Extra revisions");
        string json=File.ReadAllText(Path.Combine(folder,"first","optional_events.json"));
        foreach(string invalid in new[]{json.Replace("\"min_day\": 2","\"min_day\": \"2\""),json.Replace("\"version\": 1","\"version\": 1, \"version\": 1").Replace("\"version\": 2","\"version\": 2, \"version\": 2"),json.Replace("\"events\":", "\"reward\": 10, \"events\":"),json+"x","{}",json.Replace("\"min_day\": 2","\"min_day\": 2.0")})
        {bool denied=false;try{CafeOptionalEventJson.Parse(invalid);}catch(ArgumentException){denied=true;}Assert(denied,"Strict JSON contract rejects malformed fields/types/duplicates");}
        foreach(Action<CafeOptionalDefinition> edit in new Action<CafeOptionalDefinition>[] {d=>d.title=new string('長',31),d=>d.min_day=0,d=>d.min_affinity=101,d=>d.accept_caption=new string('長',25),d=>d.introduction[0].speaker=new string('話',8)})
        {var invalid=second.events[0].Copy();edit(invalid);invalid.revision=invalid.Digest();bool denied=false;try{invalid.Validate();}catch(ArgumentException){denied=true;}Assert(denied,"Field bounds rejected independently of revision verification");}
        var bad=second.Copy();bad.events[0].title+="tampered";bool rejected=false;try{bad.Validate();}catch(ArgumentException){rejected=true;}Assert(rejected,"Tampered revision rejected");
        Console.WriteLine("PASS optional authoring: "+assertions+" assertions; Python/C# digests, thresholds, old receipts, read keys and historical Extra.");
    }
}
