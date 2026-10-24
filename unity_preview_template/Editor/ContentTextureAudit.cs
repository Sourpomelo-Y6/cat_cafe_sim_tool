using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatCafePreview.Editor
{
    public static class ContentTextureAudit
    {
        public static void Run()
        {
            string output=Environment.GetEnvironmentVariable("CATCAFE_TEXTURE_OUTPUT");Directory.CreateDirectory(output);
            var report=new List<string>{"path,width,height,format,runtimeBytes,mipmaps,readable,maxSize,override,importFormat"};
            string root="Assets/Resources/CatCafeContent/Imports";
            foreach(string folder in Directory.GetDirectories(root).OrderBy(p=>p))
            {
                string path=Directory.GetFiles(folder+"/images","*.png").OrderBy(p=>p).First().Replace('\\','/');
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);var settings=importer.GetPlatformTextureSettings("Standalone");
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                report.Add(string.Join(",",path,texture.width,texture.height,texture.format,UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture),texture.mipmapCount,texture.isReadable,settings.maxTextureSize,settings.overridden,settings.format));
            }
            File.WriteAllText(Path.Combine(output,"importers.csv"),string.Join("\n",report)+"\n");
            // The same standard commands/layouts can be rendered before/after.
            var b=new LocalCafeBackend(Path.Combine(output,"saves"));
            b.Simulation.Execute("player_begin",catId:"cat-mike");b.Simulation.Execute("player_step",choice:"direct");
            // Current valid envelope with a historical pack receipt isolates image
            // compatibility from unrelated rule-version migrations.
            const string oldRoot="CatCafeContent/Imports/75f31c320b3f4de7978ac2c77f7eada1/";
            var oldBundle=JsonUtility.FromJson<ContentBundle>(Resources.Load<TextAsset>(oldRoot+"bundle").text);
            string oldImage=oldBundle.cats.First(c=>c.cat_id=="playtest-mike").scenes.First(s=>s.key=="normal").image;
            b.Narratives.Rows.Last().image_resource=oldRoot+oldImage.Substring(0,oldImage.Length-4);
            StandaloneDispatchConversationSmoke.Good(b,"save_game",slot:2);
            EditorSceneManager.OpenScene("Assets/Scenes/CatCafePythonState.unity");
            var preview=UnityEngine.Object.FindObjectOfType<ContentPreview>();preview.Initialize();
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;var client=preview.GetComponent<PythonStateClient>();
            typeof(PythonStateClient).GetField("preview",flags).SetValue(client,preview);CafeBackends.UseLocal(b);
            typeof(PythonStateClient).GetMethod("Accept",flags).Invoke(client,new object[]{b.View()});
            var panel=preview.GetComponent<PythonPlayerPanel>();panel.Open();
            var capture=typeof(NarrativeVerification).GetMethod("Capture",BindingFlags.Static|BindingFlags.NonPublic);
            foreach(var size in new[]{new Vector2Int(1280,800),new Vector2Int(1024,768)})
                capture.Invoke(null,new object[]{preview,Path.Combine(output,"player-"+size.x+".png"),size.x,size.y});
            File.WriteAllText(Path.Combine(output,"result.txt"),"PASS: representative importers and same-command player canvases; no source/user-save changes\n"+panel.history.text);
            Debug.Log("CAT_CAFE_TEXTURE_AUDIT_OK");
        }
    }
}
