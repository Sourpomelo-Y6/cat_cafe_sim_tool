using CatCafePreview;
using CatCafePreview.Standalone;
static partial class Program {
 static void DisplayNameChecks(string path) {
  using var json=System.Text.Json.JsonDocument.Parse(File.ReadAllText(path));
  var data=Decode<ContentDisplayLabels>(json.RootElement.GetProperty("display_labels").GetRawText());
  CafeDisplayNames.Validate(data);var old=CafeDisplayNames.Lookup;
  string baseline=CafeSimulation.PlayerReactionName("confused");
  try {
   CafeDisplayNames.Lookup=(group,id,fallback)=>CafeDisplayNames.Resolve(data,group,id,fallback);
   Assert(CafeSimulation.PlayerTypeName("voice")=="おしゃべり","mode alias");
   Assert(CafeSimulation.PlayerActionName("direct")=="まっすぐ動かす","action alias");
   Assert(CafeSimulation.PlayerReactionName("confused")=="きょとん","reaction alias");
   Assert(CafeNarratives.ReactionLabel(new CafeNarrativeRow{special_reaction="open_up"})=="安心する","special alias");
   Assert(CafeSimulation.PlayerTypeName("ball")=="ボール遊び","default preserved");
   Assert(!CafeSimulation.GrowthTypes.Contains("おしゃべり") && CafeSimulation.GrowthTypes.Contains("voice"),"IDs preserved");
  } finally {CafeDisplayNames.Lookup=old;}
  Assert(CafeSimulation.PlayerReactionName("confused")==baseline,"lookup restored");
  Console.WriteLine("Display labels: "+assertions+" checks passed");
 }
}
