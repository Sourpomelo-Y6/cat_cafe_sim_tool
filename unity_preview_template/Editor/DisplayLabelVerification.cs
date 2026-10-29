using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using CatCafePreview.Standalone;
namespace CatCafePreview.Editor {
 public static class DisplayLabelVerification {
  public static void Verify() {
   string output=Path.Combine(Environment.GetEnvironmentVariable("CATCAFE_WORKSPACE_ROOT"),"Logs/DisplayLabels");
   ContentDisplayNames.Install();
   UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
   var header=UnityEngine.Object.FindObjectOfType<PythonBusinessHeader>();
   var eventText=header.GetComponent<PythonEventListPanel>().open.GetComponentInChildren<UnityEngine.UI.Text>();
   var align=typeof(PythonBusinessHeader).GetMethod("AlignCommands",BindingFlags.Instance|BindingFlags.NonPublic);
   foreach(int count in new[]{0,1,12,0}) {
    eventText.text=count==0?"イベント一覧":"イベント一覧\n要確認 "+count+"件";
    eventText.fontSize=count==0?17:14;eventText.resizeTextForBestFit=count>0;eventText.resizeTextMinSize=10;eventText.resizeTextMaxSize=14;
    for(int frame=0;frame<120;frame++)align.Invoke(header,null);
    if(eventText.fontSize!=(count==0?17:14) || eventText.resizeTextForBestFit!=(count>0))throw new Exception("Header overwrote event typography");
   }
   Debug.Log("CAT_CAFE_EVENT_HEADER_STYLE_OK");
   var pointer=JsonUtility.FromJson<ContentCatalog>(Resources.Load<TextAsset>("CatCafeContent/catalog").text);
   var legacy=JsonUtility.FromJson<ContentBundle>(Resources.Load<TextAsset>(pointer.bundleResource).text);
   ContentValidation.Validate(legacy);
   string root=pointer.bundleResource.Substring(0,pointer.bundleResource.Length-"bundle".Length);
   int images=0;
   foreach(var cat in legacy.cats)foreach(var scene in cat.scenes)if(!string.IsNullOrEmpty(scene.image)) {
    if(Resources.Load<Texture2D>(root+scene.image.Substring(0,scene.image.Length-4))==null)throw new Exception("Missing image: "+cat.cat_id+"/"+scene.key);
    images++;
   }
   Debug.Log("CAT_CAFE_LEGACY_IMAGES_OK: "+images);
   var absent=JsonUtility.FromJson<ContentBundle>("{}");ContentDisplayNames.Validate(absent.display_labels);
   var bundle=JsonUtility.FromJson<ContentBundle>(File.ReadAllText(Path.Combine(output,"labels-fixture.json")));
   ContentDisplayNames.Validate(bundle.display_labels);
   if(ContentDisplayNames.Resolve(bundle.display_labels,"modes","voice","old")!="おしゃべり" || ContentDisplayNames.Resolve(bundle.display_labels,"reactions","confused","old")!="きょとん" || ContentDisplayNames.Resolve(bundle.display_labels,"modes","missing","fallback")!="fallback")throw new Exception("Exported labels mismatch");
   string mode=CafeSimulation.PlayerTypeName("voice"),action=CafeSimulation.PlayerActionName("direct"),reaction=CafeSimulation.PlayerReactionName("confused");
   var field=typeof(ContentDisplayNames).GetField("current",BindingFlags.Static|BindingFlags.NonPublic);var original=field.GetValue(null);
   try {
    field.SetValue(null,bundle.display_labels);
    if(CafeSimulation.PlayerTypeName("voice")!="おしゃべり" || CafeSimulation.PlayerActionName("direct")!="まっすぐ動かす" || CafeSimulation.PlayerReactionName("confused")!="きょとん")throw new Exception("Game entry point mismatch");
    if(CafeNarratives.ReactionLabel(new CafeNarrativeRow{special_reaction="open_up"})!="安心する")throw new Exception("Special label mismatch");
    if(CafeSimulation.PlayerTypeName("ball")!="ボール遊び")throw new Exception("Unedited label changed");
   } finally {field.SetValue(null,original);}
   if(CafeSimulation.PlayerTypeName("voice")!=mode || CafeSimulation.PlayerActionName("direct")!=action || CafeSimulation.PlayerReactionName("confused")!=reaction)throw new Exception("Fixture leaked");
   int rejected=0;
   foreach(var data in new[]{new ContentDisplayLabels{version=2},new ContentDisplayLabels{version=1,modes=new[]{new ContentDisplayName{id="new",label="test"}}},new ContentDisplayLabels{version=1,reactions=new[]{new ContentDisplayName{id="confused",label="<b>x</b>"}}}})try{ContentDisplayNames.Validate(data);}catch(ArgumentException){rejected++;}
   if(rejected!=3)throw new Exception("Invalid labels accepted");ContentDisplayNames.Validate(null);
   File.WriteAllText(Path.Combine(output,"unity-result.txt"),"PASS: exported alias schema, mode/action/reaction entry points, unedited and absent-label fallback, invalid version/ID/markup rejection, fixture restoration. No game data or saves changed; no build.");Debug.Log("CAT_CAFE_DISPLAY_LABELS_OK");
  }
 }
}
