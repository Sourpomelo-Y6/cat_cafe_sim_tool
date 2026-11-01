using CatCafePreview.Standalone;
static partial class Program
{
    static void OptionalEventChecks()
    {
        var state=new CafeSnapshot();state.cats.Add(new CafeCat{id="cat-mike"});state.player_cats.Add(new CafePlayerCat{cat_id="cat-mike",affinity=20});
        Assert(!CafeOptionalEvents.Eligible(state),"Day one unavailable");state.day=2;
        Assert(CafeOptionalEvents.Eligible(state),"Boundary eligible");
        foreach(string health in new[]{"sick","recovering"}){state.cats[0].health=health;Assert(!CafeOptionalEvents.Eligible(state),"Health guard");}state.cats[0].health="healthy";
        foreach(string activity in new[]{"adopted","missing","dispatch"}){state.cats[0].activity=activity;Assert(!CafeOptionalEvents.Eligible(state),"Ownership/presence guard");}state.cats[0].activity="cafe";
        state.player_cats[0].affinity=19.999;Assert(!CafeOptionalEvents.Eligible(state),"Affinity boundary");state.player_cats[0].affinity=20;
        state.phase="open";Assert(!CafeOptionalEvents.Eligible(state),"Business guard");state.phase="preparation";
        var before=Encode(state);
        foreach(bool accept in new[]{true,false})
        {
            var row=CafeOptionalEvents.Start(state,"ミケ","cat-mike","image");
            Assert(!CafeOptionalEvents.Choose(row,accept),"Cannot choose before reading intro");
            row.progress.node=1;row.progress.page_offset=3;
            var loaded=Decode<CafeSaveEnvelope>(Encode(new CafeSaveEnvelope{state=state,optional_events=new[]{row}}));
            CafeOptionalEvents.Validate(loaded.optional_events);row=loaded.optional_events[0];
            Assert(row.progress.node==1 && row.progress.page_offset==3,"Page position roundtrip");
            row.progress.node=row.introduction.Length;row.progress.page_offset=0;
            Assert(CafeOptionalEvents.Choose(row,accept),"Choice accepted");
            Assert(!CafeOptionalEvents.Choose(row,!accept) && row.progress.branch_id==(accept?"accepted":"declined"),"Choice cannot be replaced");
            CafeOptionalEvents.Finish(row);Assert(!row.progress.completed,"Cannot complete unread result");
            row.progress.node=1;row.progress.page_offset=2;
            row=Decode<CafeOptionalEvent>(Encode(row));CafeOptionalEvents.Validate(new[]{row});
            Assert(row.progress.branch_id==(accept?"accepted":"declined") && row.progress.node==1 && row.progress.page_offset==2,"Result resume");
            string frozen=Encode(row);var newer=CafeOptionalEvents.Start(state,"別名","cat-mike","new-image");newer.introduction[0].text="改稿";
            Assert(Encode(row)==frozen,"New content does not mutate active receipt");
            row.progress.node=CafeOptionalEvents.Lines(row,row.progress.branch_id).Length;row.progress.page_offset=0;
            CafeOptionalEvents.Finish(row);CafeOptionalEvents.Validate(new[]{row});
            Assert(row.progress.completed && row.progress.viewed_branches.SequenceEqual(new[]{accept?"accepted":"declined"}),"Only chosen branch completed");
            string completed=Encode(row);CafeOptionalEvents.Finish(row);Assert(completed==Encode(row),"Completion idempotent");
            foreach(Action<CafeOptionalEvent> corrupt in new Action<CafeOptionalEvent>[] {r=>r.progress.node=999,r=>r.progress.branch_id="unknown",r=>r.progress.page_offset=-1,r=>r.progress.viewed_branches=new[]{"accepted","declined"},r=>r.introduction=null,r=>r.cat_id="cat-sora"})
            {
                var bad=Decode<CafeOptionalEvent>(Encode(row));corrupt(bad);bool rejected=false;try{CafeOptionalEvents.Validate(new[]{bad});}catch(ArgumentException){rejected=true;}Assert(rejected,"Invalid receipt rejected");
            }
        }
        CafeOptionalEvents.Validate(Decode<CafeSaveEnvelope>("{}").optional_events);
        Assert(Encode(state)==before,"Story never changes gameplay state");
        Console.WriteLine("PASS optional events: "+assertions+" assertions.");
    }
}
