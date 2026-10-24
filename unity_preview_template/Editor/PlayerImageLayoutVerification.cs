using System;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview.Editor
{
 public static class PlayerImageLayoutVerification
 {
  public static void Build()
  {
   PythonPlayerSetup.Build();
   PythonPlayerLayout.BuildAndVerify();
   var p=UnityEngine.Object.FindObjectOfType<PythonPlayerPanel>();
   p.panel.SetActive(true);var fitter=p.picture.GetComponent<AspectRatioFitter>();
   foreach(float ratio in new[]{.6f,1f,1.8f,3f})
   {
    fitter.aspectRatio=ratio;fitter.SetLayoutHorizontal();fitter.SetLayoutVertical();Canvas.ForceUpdateCanvases();
    var r=p.picture.rectTransform.rect;
    if(r.width>793 || r.height>401 || Math.Abs(r.width/r.height-ratio)>.01f)throw new Exception("Image aspect bounds");
   }
   Debug.Log("CAT_CAFE_PLAYER_IMAGE_REGEN_VERIFIED: base generator + layout generator + portrait/square/landscape bounds");
   StandaloneScreenSetup.Build();
  }
 }
}
