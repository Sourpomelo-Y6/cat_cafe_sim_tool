using CatCafePreview.Standalone;
using System.Text.Json;
using System.Text.Encodings.Web;
static partial class Program
{
    static void AllOptionalEventChecks(string folder)
    {
        using var cases=JsonDocument.Parse(File.ReadAllText(Path.Combine(folder,"cases.json")));
        foreach(var test in cases.RootElement.GetProperty("cases").EnumerateArray())
        {
            bool valid=true;try{CafeOptionalEventJson.Parse(test.GetProperty("json").GetString());}catch(ArgumentException){valid=false;}
            Assert(valid==test.GetProperty("valid").GetBoolean(),"Python/C# contract "+test.GetProperty("name").GetString());
        }
        var pack=CafeOptionalEventJson.Parse(File.ReadAllText(Path.Combine(folder,"all","optional_events.json")));
        var def=pack.events.First(d=>d.content_cat_id=="playtest-sora");
        var state=new CafeSnapshot{day=2};
        foreach(string id in new[]{"cat-sora","rescue-100"}){state.cats.Add(new CafeCat{id=id});state.player_cats.Add(new CafePlayerCat{cat_id=id,affinity=20});}
        Assert(def.Matches("cat-sora","playtest-sora") && def.Matches("rescue-100","playtest-sora"),"shared target matches both individuals");
        var a=def.Start(state,"cat-sora","ソラ","image");var b=def.Start(state,"rescue-100","紹介された猫","image");
        a.progress.node=3;Assert(CafeOptionalEvents.Choose(a,true),"choice A");a.progress.node=2;CafeOptionalEvents.Finish(a);
        CafeOptionalEvents.Validate(new[]{a,b});Assert(a.progress.completed && !b.progress.completed && b.progress.node==0,"individual progress independent");
        bool Reject(Action action){try{action();return false;}catch(ArgumentException){return true;}}
        Assert(Reject(()=>CafeOptionalEvents.Validate(new[]{b,b})),"duplicate and unfinished records reject");
        var c=def.Start(state,"cat-sora","別記録","");c.event_id="optional.other.story";c.progress.conversation_id=c.event_id+".v"+c.revision;
        Assert(Reject(()=>CafeOptionalEvents.Validate(new[]{b,c})),"two unfinished records reject");
        var codec=new JsonSerializerOptions(Options){Encoder=JavaScriptEncoder.UnsafeRelaxedJsonEscaping};
        string Receipt(CafeOptionalEvent[] rows)=>JsonSerializer.Serialize(new {optional_events=rows},codec);
        var round=CafeOptionalEventSaveJson.ReadSave(Receipt(new[]{a,b}));Assert(round.Length==2 && round[1].cat_id=="rescue-100","strict save readback");
        Assert(CafeOptionalEventSaveJson.ReadSave("{}").Length==0 && CafeOptionalEventSaveJson.ReadSave("{\"optional_events\":null}").Length==0,"legacy absent/null saves");
        string json=Receipt(new[]{a,b});
        var legacyReceipt=CafeOptionalEventSaveJson.ReadSave(json.Replace("\"scene_art\":0,", ""));
        Assert(legacyReceipt.Length==2 && legacyReceipt.All(row=>row.scene_art==0),"Old receipts without artwork still load");
        Assert(Reject(()=>CafeOptionalEventSaveJson.ReadSave(json.Replace("\"scene_art\":0","\"scene_art\":1"))),"Mike artwork is limited to his receipt");
        foreach(string bad in new[]{"false","{}","[{\"unknown\":1}]"})Assert(Reject(()=>CafeOptionalEventSaveJson.ReadSave("{\"optional_events\":"+bad+"}")),"bad save field rejects");
        Assert(Reject(()=>CafeOptionalEventSaveJson.ReadSave("{\"optional_events\":[],\"optional_events\":[]}")),"duplicate root rejects");
        Assert(Reject(()=>CafeOptionalEventSaveJson.ReadSave(json.Replace("\"completed\":true","\"completed\":1"))),"boolean type strict");
        Assert(Reject(()=>CafeOptionalEventSaveJson.ReadSave(json.Replace("\"event_id\":","\"extra\":1,\"event_id\":"))),"unknown receipt field strict");
        try{CafeOptionalEventSaveJson.ReadSave("{\"optional_events\":[],\"optional_events\":[]}");throw new Exception("Expected duplicate rejection");}catch(ArgumentException error){Assert(error.Message.Contains("任意イベント"),"save error identifies optional events");}
        var max=Enumerable.Range(0,512).Select(i=>{var row=Decode<CafeOptionalEvent>(Encode(a));row.cat_id="c"+i.ToString().PadLeft(63,'0');row.event_id="optional."+new string('e',87);row.progress.conversation_id=row.event_id+".v"+row.revision;row.content_cat_id=new string('c',64);row.title=new string('"',30);row.accept_caption=row.decline_caption=new string('"',24);row.cat_name=new string('\u0001',256);row.image_resource=new string('\u0001',1024);foreach(var branch in new[]{row.introduction,row.accepted,row.declined})foreach(var line in branch){line.speaker=new string('話',7);line.text=new string('\n',999)+"終";}return row;}).ToArray();
        // A legitimate old Mike receipt has wider per-line limits than new authored definitions.
        var old=Decode<CafeOptionalEvent>(Encode(a));old.event_id=CafeOptionalEvents.MikeId;old.revision="1";old.cat_id="cat-mike";old.content_cat_id=new string('m',256);old.progress.conversation_id=old.event_id+".v1";
        old.introduction=old.accepted=old.declined=Enumerable.Range(0,20).Select(_=>new CafeConversationLine("",new string('\u0001',10000))).ToArray();old.progress.node=20;
        max[0]=old;string maximal=JsonSerializer.Serialize(new {optional_events=max},new JsonSerializerOptions(codec){WriteIndented=true});Assert(maximal.Length<=CafeOptionalEvents.MaxSaveJson,"normal maximum output within scoped JSON bound");
        File.WriteAllText(Path.Combine(folder,"maximum-size.txt"),maximal.Length+" UTF16 units; 512 records, escaped metadata, legacy wide text, indented JSON.");
        Assert(CafeOptionalEventSaveJson.ReadSave(maximal).Length==512,"512 maximum rows and legacy escaped payload roundtrip");
        Assert(Reject(()=>CafeOptionalEvents.Validate(max.Concat(new[]{a}).ToArray())),"513 rows reject");
        Assert(Reject(()=>CafeOptionalEventSaveJson.ReadSave("{\"optional_events\":[\""+new string('x',16000001)+"\"]}")),"oversize field rejects");
        Assert(Reject(()=>CafeOptionalEventJson.ReadValue("\""+new string('x',100001)+"\"")),"other feature parser limit unchanged");
        state.player_cats[0].affinity=double.PositiveInfinity;Assert(!def.Eligible(state,"cat-sora"),"nonfinite affinity cannot start");
        string profilePath=Path.Combine(folder,"capacity-profile");Directory.CreateDirectory(profilePath);var seed=new CafeReplayEntry{conversation_id=a.progress.conversation_id,branch_id="accepted",title="記録",cat_name="猫",cat_id="playtest-sora",lines=a.accepted};var filled=new CafeReplayProfile{entries=Enumerable.Range(0,CafeReplayCatalog.All.Length+1000).Select(i=>new CafeReplayEntry{conversation_id="optional.cap"+i+".v"+new string('a',64),branch_id="accepted",title=seed.title,cat_name=seed.cat_name,cat_id=seed.cat_id,lines=seed.lines}).ToArray()};CafeReplayCatalog.Validate(filled);string profileFile=Path.Combine(profilePath,"extra-v1.json");File.WriteAllText(profileFile,Encode(filled));string originalProfile=File.ReadAllText(profileFile);var store=new CafeReplayStore(profilePath,p=>Encode(p),text=>Decode<CafeReplayProfile>(text));Assert(!store.Merge(new[]{seed}) && store.Error!=null,"full Extra reports failure");Assert(File.ReadAllText(profileFile)==originalProfile,"full Extra preserves prior profile");File.WriteAllText(profileFile,"{broken");Assert(!store.Merge(new[]{seed}) && File.ReadAllText(profileFile)=="{broken","corrupt Extra refuses overwrite");
        Console.WriteLine("PASS all optional events: "+assertions+" assertions; maximum JSON "+maximal.Length+" UTF16 units.");
    }
}
