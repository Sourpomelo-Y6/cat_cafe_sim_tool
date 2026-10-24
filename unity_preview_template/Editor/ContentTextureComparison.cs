using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CatCafePreview.Editor
{
    // In-memory candidates: no adopted PNG, importer, catalog or save is changed.
    public static class ContentTextureComparison
    {
        public static void Run()
        {
            string output = Environment.GetEnvironmentVariable("CATCAFE_TEXTURE_OUTPUT");
            Directory.CreateDirectory(output);
            var pointer = JsonUtility.FromJson<ContentCatalog>(Resources.Load<TextAsset>("CatCafeContent/catalog").text);
            var bundle = JsonUtility.FromJson<ContentBundle>(Resources.Load<TextAsset>(pointer.bundleResource).text);
            string root = "Assets/Resources/" + pointer.bundleResource.Substring(0, pointer.bundleResource.Length - 6);
            var lines = new List<string>{"cat,scene,candidate,width,height,format,gpuBytes,displaySize,rgbPSNR,alphaMaxError"};
            var picks = bundle.cats.Where(c=>c.scenes.Any(s=>s.image!="")).Take(3).ToArray();
            foreach (var cat in picks)
            foreach (var scene in cat.scenes.Where(s=>s.image!="").Where((s,i)=>i==0 || s.key=="teaser__switch" || s.key=="teaser__direct" || s.key=="teaser__open_up").Take(4))
            {
                string name = cat.cat_id + "-" + scene.key;
                var source = new Texture2D(2,2,TextureFormat.RGBA32,false);
                source.LoadImage(File.ReadAllBytes(root+scene.image));
                foreach (int size in new[]{150,600})
                {
                    var reference = Read(source,size,size);
                    File.WriteAllBytes(Path.Combine(output,name+"-source-"+size+".png"),reference.EncodeToPNG());
                    foreach (var format in new[]{TextureFormat.RGBA32,TextureFormat.DXT5,TextureFormat.BC7})
                    {
                        var candidate = Read(source,1024,1024);
                        if(format!=TextureFormat.RGBA32)EditorUtility.CompressTexture(candidate,format,TextureCompressionQuality.Best);
                        var actual = Read(candidate,size,size);
                        var a=reference.GetPixels32();var b=actual.GetPixels32();double squared=0;int alpha=0;
                        for(int i=0;i<a.Length;i++){squared+=Math.Pow(a[i].r-b[i].r,2)+Math.Pow(a[i].g-b[i].g,2)+Math.Pow(a[i].b-b[i].b,2);alpha=Math.Max(alpha,Math.Abs(a[i].a-b[i].a));}
                        double mse=squared/(a.Length*3);double psnr=mse==0?99:10*Math.Log10(255*255/mse);
                        lines.Add(string.Join(",",cat.cat_id,scene.key,format,1024,1024,candidate.format,candidate.GetRawTextureData().Length,size,psnr.ToString("F3",System.Globalization.CultureInfo.InvariantCulture),alpha));
                        File.WriteAllBytes(Path.Combine(output,name+"-"+format+"-"+size+".png"),actual.EncodeToPNG());
                        UnityEngine.Object.DestroyImmediate(actual);UnityEngine.Object.DestroyImmediate(candidate);
                    }
                    UnityEngine.Object.DestroyImmediate(reference);
                }
                UnityEngine.Object.DestroyImmediate(source);
            }
            File.WriteAllText(Path.Combine(output,"comparison.csv"),string.Join("\n",lines)+"\n");
            Debug.Log("CAT_CAFE_TEXTURE_COMPARISON_OK: "+(lines.Count-1));
        }

        static Texture2D Read(Texture source,int width,int height)
        {
            var previous=RenderTexture.active;var rt=RenderTexture.GetTemporary(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            try
            {
                Graphics.Blit(source,rt);RenderTexture.active=rt;
                var result=new Texture2D(width,height,TextureFormat.RGBA32,false,false);
                result.ReadPixels(new Rect(0,0,width,height),0,0);result.Apply();return result;
            }
            finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
        }
    }
}
