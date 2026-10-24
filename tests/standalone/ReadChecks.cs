using CatCafePreview.Standalone;
static partial class Program {
 static void ReadChecks(string folder,bool resume){
  CafeReadStore Store()=>new CafeReadStore(folder,p=>Encode(p),s=>Decode<CafeReadProfile>(s));
  var lines=new[]{new CafeConversationLine("speaker","abcdefghij"),new CafeConversationLine("","important")};
  string key=CafeReadStore.Key("event.v1","intro",lines,0);var store=Store();
  if(resume){Assert(store.Contains(key,0,10),"restart lost read");Console.WriteLine("PASS restart read");return;}
  Assert(!store.Contains(key,0,10),"old/missing data inferred read");
  Assert(store.Remember(key,5,10),"tail persist");Assert(!store.Contains(key,0,10),"resume inferred preceding page");
  Assert(store.Remember(key,0,3),"head persist");Assert(!store.Contains(key,0,10),"gap inferred read");
  Assert(store.Remember(key,3,5),"middle persist");Assert(store.Contains(key,0,10),"mixed resolution ranges lost");
  Assert(Store().Contains(key,0,10),"disk restore");
  Assert(!store.Contains(CafeReadStore.Key("event.v1","other",lines,0),0,10),"branch collision");
  Assert(!store.Contains(CafeReadStore.Key("event.v2","intro",lines,0),0,10),"version collision");
  Assert(!store.Contains(CafeReadStore.Key("event.v1","intro",lines,1),0,9),"section collision");
  lines[1].text="changed";Assert(!store.Contains(CafeReadStore.Key("event.v1","intro",lines,0),0,10),"updated branch inherited read");
  lines[0].speaker="new name";Assert(!store.Contains(CafeReadStore.Key("event.v1","intro",lines,0),0,10),"dynamic identity inherited read");
  var stale=Store();string extra=new string('a',64);Assert(store.Remember(extra,0,2),"additional section persist");Assert(stale.Remember(key,10,12),"stale merge persist");Assert(Store().Contains(extra,0,2) && Store().Contains(key,0,12),"stale backend lost ranges");
  string broken=Path.Combine(folder,"broken");Directory.CreateDirectory(broken);File.WriteAllText(Path.Combine(broken,"conversation-read-v1.json"),"{}");var bad=new CafeReadStore(broken,p=>Encode(p),s=>Decode<CafeReadProfile>(s));Assert(!bad.Contains(key,0,10) && !bad.Remember(key,0,10),"corruption overwrite");Assert(File.ReadAllText(Path.Combine(broken,"conversation-read-v1.json"))=="{}","bad file lost");
  Console.WriteLine("PASS read "+assertions);
 }
}
