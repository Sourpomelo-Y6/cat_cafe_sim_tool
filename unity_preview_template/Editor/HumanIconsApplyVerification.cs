using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace CatCafePreview.Editor {
 public static class HumanIconsApplyVerification {
  public static void VerifyWatanabeAndBuild(){
   ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_HUMAN_SELECTED"));
   var textures=new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4","guest-5","guest-6"}.Select(HumanPortraitResources.Resolve).ToArray();
   if(textures.Any(t=>t==null || t==HumanPortraitResources.Placeholder()) || textures.Distinct().Count()!=7)throw new Exception("Seven customer icons did not resolve distinctly");
   if(HumanPortraitResources.Resolve("unknown")!=HumanPortraitResources.Placeholder())throw new Exception("Unknown customer fallback");
   Debug.Log("CAT_CAFE_WATANABE_IMPORTED");StandaloneScreenSetup.Build();
  }
  public static void VerifyItoAndBuild(){
   ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_HUMAN_SELECTED"));
   var textures=new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4","guest-5"}.Select(HumanPortraitResources.Resolve).ToArray();
   if(textures.Any(t=>t==null || t==HumanPortraitResources.Placeholder()) || textures.Distinct().Count()!=6)throw new Exception("Six customer icons did not resolve distinctly");
   if(HumanPortraitResources.Resolve("unknown")!=HumanPortraitResources.Placeholder())throw new Exception("Unknown customer fallback");
   Debug.Log("CAT_CAFE_ITO_IMPORTED");StandaloneScreenSetup.Build();
  }
  public static void VerifyTanakaAndBuild(){
   ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_HUMAN_SELECTED"));
   var textures=new[]{"guest-1","guest-2","reservation-longhair","guest-3","guest-4"}.Select(HumanPortraitResources.Resolve).ToArray();
   if(textures.Any(t=>t==null || t==HumanPortraitResources.Placeholder()) || textures.Distinct().Count()!=5)throw new Exception("Five customer icons did not resolve distinctly");
   if(HumanPortraitResources.Resolve("unknown")!=HumanPortraitResources.Placeholder())throw new Exception("Unknown customer fallback");
   Debug.Log("CAT_CAFE_TANAKA_IMPORTED");StandaloneScreenSetup.Build();
  }
  public static void VerifyTakahashiAndBuild(){
   ContentPreviewSetup.Import(Environment.GetEnvironmentVariable("CATCAFE_HUMAN_SELECTED"));
   var textures=new[]{"guest-1","guest-2","reservation-longhair","guest-3"}.Select(HumanPortraitResources.Resolve).ToArray();
   if(textures.Any(t=>t==null || t==HumanPortraitResources.Placeholder()) || textures.Distinct().Count()!=4)throw new Exception("Four customer icons did not resolve distinctly");
   if(HumanPortraitResources.Resolve("unknown")!=HumanPortraitResources.Placeholder())throw new Exception("Unknown customer fallback");
   Debug.Log("CAT_CAFE_TAKAHASHI_IMPORTED");StandaloneScreenSetup.Build();
  }
  public static void VerifyAndBuild(){
   string folder=Environment.GetEnvironmentVariable("CATCAFE_HUMAN_SELECTED");ContentPreviewSetup.Import(folder);var textures=new[]{"guest-1","guest-2","reservation-longhair"}.Select(HumanPortraitResources.Resolve).ToArray();
   if(textures.Any(t=>t==null || t==HumanPortraitResources.Placeholder()) || textures.Distinct().Count()!=3)throw new Exception("Three customer icons did not resolve distinctly");
   if(HumanPortraitResources.Resolve("temporary-sato")!=HumanPortraitResources.Placeholder())throw new Exception("Unknown customer did not fall back");
   // Apply the existing policy to referenced cat textures (including historical packs) before the ordinary build guard.
   CatTexturePolicy.ApplyAll();
   Debug.Log("CAT_CAFE_THREE_ICONS_IMPORTED");StandaloneScreenSetup.Build();
  }
 }
}
