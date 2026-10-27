using System;
using System.IO;
using UnityEngine;
namespace CatCafePreview.Editor
{
 public static class SpecialNarrativeVerification
 {
  public static void ImportAndBuild()
  {
   ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_IMPORT_PACK"));
   var catalog=CafeNarrativeResources.Load();catalog.Validate();if(catalog.version!=4)throw new Exception("Expected v4 special catalog");
   Debug.Log("CAT_CAFE_SPECIAL_IMPORT_OK "+catalog.entries.Length);
   StandaloneScreenSetup.Build();
  }
 }
}
