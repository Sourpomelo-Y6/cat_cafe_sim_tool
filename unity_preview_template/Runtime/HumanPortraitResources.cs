using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
namespace CatCafePreview {
 [Serializable] public sealed class HumanPortraitRow { public string subject_id,game_id,image; }
 [Serializable] public sealed class HumanPortraitPack {
  public int version; public string pack_id; public HumanPortraitRow[] people;
  public void Validate(){
   if(version!=1 || !Regex.IsMatch(pack_id??"","^[0-9a-f]{32}$") || people==null || people.Length>500)throw new ArgumentException("Invalid human portrait pack");
   var subjects=new HashSet<string>();var games=new HashSet<string>();
   foreach(var r in people)if(r==null || !Regex.IsMatch(r.subject_id??"","^[a-z][a-z0-9_-]{0,99}$") || !Regex.IsMatch(r.game_id??"","^[a-z][a-z0-9_-]{0,99}$") || r.game_id=="player" || !subjects.Add(r.subject_id) || !games.Add(r.game_id) || !Regex.IsMatch(r.image??"","^people/[0-9a-f]{64}\\.png$"))throw new ArgumentException("Invalid human portrait binding");
  }
 }
 public static class HumanPortraitResources {
  static string loaded;static HumanPortraitPack pack;static Texture2D fallback;
  public static Texture2D Placeholder(){
   var resource=Resources.Load<Texture2D>("CatCafeStandalone/customer_icon");if(resource!=null)return resource;
   if(fallback==null){fallback=new Texture2D(64,64,TextureFormat.RGBA32,false);var pixels=new Color[4096];for(int y=0;y<64;y++)for(int x=0;x<64;x++){float dx=x-31.5f;bool head=dx*dx+(y-44)*(y-44)<100;bool body=dx*dx/440+(y-13)*(y-13)/280f<1;pixels[y*64+x]=head||body?new Color(.35f,.42f,.38f,1):Color.clear;}fallback.SetPixels(pixels);fallback.Apply();}return fallback;
  }
  public static Texture2D Resolve(string gameId){
   try {
    var asset=Resources.Load<TextAsset>("CatCafeContent/human_portraits");string json=asset==null?"":asset.text;
    if(json!=loaded){loaded=json;pack=null;if(json!=""){var next=JsonUtility.FromJson<HumanPortraitPack>(json);next.Validate();pack=next;}}
    if(!string.IsNullOrEmpty(gameId) && pack!=null)foreach(var r in pack.people)if(r.game_id==gameId){var texture=Resources.Load<Texture2D>("CatCafeContent/People/"+pack.pack_id+"/"+r.image.Substring(0,r.image.Length-4));return texture!=null?texture:Placeholder();}
   }catch(Exception e){Debug.LogWarning("Human portrait fallback: "+e.Message);pack=null;}
   return Placeholder();
  }
  public static void ApplyIcon(GameObject host,string gameId){
   if(host==null)return;
   // Keep the existing Image object and layout; use a child RawImage for the texture.
   var old=host.GetComponent<Image>();if(old!=null)old.enabled=false;
   var child=host.transform.Find("Human Portrait");RawImage image;
   if(child==null){var go=new GameObject("Human Portrait",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage),typeof(AspectRatioFitter));go.transform.SetParent(host.transform,false);image=go.GetComponent<RawImage>();var rect=(RectTransform)go.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;go.GetComponent<AspectRatioFitter>().aspectMode=AspectRatioFitter.AspectMode.FitInParent;image.raycastTarget=false;}
   else image=child.GetComponent<RawImage>();
   image.texture=string.IsNullOrEmpty(gameId)?null:Resolve(gameId);image.enabled=image.texture!=null;var fit=image.GetComponent<AspectRatioFitter>();if(image.texture!=null)fit.aspectRatio=(float)image.texture.width/image.texture.height;
  }
 }
}
