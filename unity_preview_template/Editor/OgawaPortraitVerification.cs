using System;
using System.Linq;
using UnityEngine;
namespace CatCafePreview.Editor {
 public static class OgawaPortraitVerification {
  public static void ImportAndBuild(){
   ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_HUMAN_SELECTED"));
   var pack=JsonUtility.FromJson<HumanPortraitPack>(Resources.Load<TextAsset>("CatCafeContent/human_portraits").text);
   var textures=pack.people.Select(r=>HumanPortraitResources.Resolve(r.game_id)).ToArray();
   if(pack.people.Length!=25 || textures.Distinct().Count()!=25 || textures.Any(t=>t==null || t==HumanPortraitResources.Placeholder()))throw new Exception("25 distinct portraits required");
   foreach(var id in new[]{"guest-29","guest-49","event-guest-42","unknown"})if(HumanPortraitResources.Resolve(id)!=HumanPortraitResources.Placeholder())throw new Exception("Unexpected portrait: "+id);
   Debug.Log("CAT_CAFE_OGAWA_IMPORTED: 25 distinct portraits; unrelated IDs retain fallback");
   StandaloneScreenSetup.Build();
  }
 }
}
