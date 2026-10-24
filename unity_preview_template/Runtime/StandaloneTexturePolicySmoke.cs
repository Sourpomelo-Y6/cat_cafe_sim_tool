using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CatCafePreview
{
    // Explicit rendering fixture; ordinary game startup is unchanged.
    public class StandaloneTexturePolicySmoke : MonoBehaviour
    {
        string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Install()
        {
            var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "--catcafe-texture-policy"); if (i < 0) return;
            var q = new GameObject("Texture policy verification").AddComponent<StandaloneTexturePolicySmoke>();
            DontDestroyOnLoad(q); q.output = Path.GetFullPath(args[i + 1]); Directory.CreateDirectory(q.output); Application.runInBackground = true; q.StartCoroutine(q.Guard());
        }
        IEnumerator Guard()
        {
            var run = Run(); while (true) { bool more; try { more = run.MoveNext(); } catch (Exception e) { File.WriteAllText(Path.Combine(output, "failure.txt"), e.ToString()); Debug.LogException(e); Application.Quit(1); yield break; } if (!more) yield break; yield return run.Current; }
        }
        IEnumerator Run()
        {
            File.WriteAllText(Path.Combine(output, "environment.txt"), SystemInfo.graphicsDeviceName + "\n" + SystemInfo.graphicsDeviceType + "\nBC7 supported=" + SystemInfo.SupportsTextureFormat(TextureFormat.BC7) + "\n" + SystemInfo.operatingSystem);
            var b = new LocalCafeBackend(Path.Combine(output, "save"));
            if (Environment.GetCommandLineArgs().Contains("--texture-resume"))
            {
                string saved = File.ReadAllText(Path.Combine(output, "saved-image.txt"));
                StandaloneDispatchConversationSmoke.Good(b, "load_game", slot:2);
                if (b.Narratives.Rows.Last().image_resource != saved || Resources.Load<Texture2D>(saved) == null) throw new Exception("Historical image receipt changed across process");
                File.WriteAllText(Path.Combine(output, "resume.txt"), "PASS: exact historical image reference restored in separate process"); Application.Quit(0); yield break;
            }
            StandaloneDispatchConversationSmoke.Good(b, "new_game", "free");
            StandaloneDispatchConversationSmoke.Good(b, "player_begin", cat:"cat-mike");
            CafeBackends.UseLocal(b); SceneManager.LoadScene("CatCafePythonState"); yield return new WaitForSecondsRealtime(2);
            var client = FindObjectOfType<PythonStateClient>(); var p = client.GetComponent<PythonPlayerPanel>();
            typeof(PythonStateClient).GetMethod("Accept", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(client, new object[] {b.View()}); p.Open();
            var pointer = JsonUtility.FromJson<ContentCatalog>(Resources.Load<TextAsset>("CatCafeContent/catalog").text);
            var bundle = JsonUtility.FromJson<ContentBundle>(Resources.Load<TextAsset>(pointer.bundleResource).text);
            string root = pointer.bundleResource.Substring(0, pointer.bundleResource.Length - 6);
            var lines = new List<string> {"cat,scene,width,height,format,loadMs,runtimeBytes"};
            foreach (int width in new[] {1280, 1024})
            {
                Screen.SetResolution(width, width == 1280 ? 800 : 768, false); yield return new WaitForSecondsRealtime(1);
                foreach (var c in bundle.cats.Where(c => new[] {"playtest-mike", "playtest-tama", "playtest-sora"}.Contains(c.cat_id)))
                foreach (var s in c.scenes.Where(s => new[] {"normal", "teaser__switch", "teaser__direct", "teaser__open_up"}.Contains(s.key)))
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var t = Resources.Load<Texture2D>(root + s.image.Substring(0, s.image.Length - 4)); watch.Stop();
                    if (t == null || t.width != 1024 || t.format != TextureFormat.BC7 || t.isReadable || t.mipmapCount != 1) throw new Exception("Player texture mismatch: " + c.cat_id + s.key);
                    lines.Add(string.Join(",", c.cat_id, s.key, t.width, t.height, t.format, watch.Elapsed.TotalMilliseconds, UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t)));
                    p.picture.texture = t; Canvas.ForceUpdateCanvases(); yield return new WaitForEndOfFrame();
                    ScreenCapture.CaptureScreenshot(Path.Combine(output, c.cat_id + "-" + s.key + "-" + width + ".png")); yield return new WaitForSecondsRealtime(.2f);
                }
            }
            File.WriteAllLines(Path.Combine(output, "textures.csv"), lines);
            var historical = new List<string>();
            foreach (var text in Resources.LoadAll<TextAsset>("CatCafeContent/Imports").Where(t => t.name == "bundle"))
            {
                var old = JsonUtility.FromJson<ContentBundle>(text.text);
                var scene = old.cats.SelectMany(c => c.scenes).First(s => !string.IsNullOrEmpty(s.image));
                string resource = "CatCafeContent/Imports/" + old.pack_id + "/" + scene.image.Substring(0, scene.image.Length - 4);
                if (Resources.Load<Texture2D>(resource) == null) throw new Exception("Historical resource missing: " + resource);
                historical.Add(resource);
            }
            File.WriteAllLines(Path.Combine(output, "historical-paths.txt"), historical);
            b.Simulation.Execute("player_step", choice:"direct");
            b.Narratives.Rows.Last().image_resource = historical.First(s => s.Contains("75f31c320b3f4de7978ac2c77f7eada1"));
            StandaloneDispatchConversationSmoke.Good(b, "save_game", slot:2);
            File.WriteAllText(Path.Combine(output, "saved-image.txt"), b.Narratives.Rows.Last().image_resource);
            File.WriteAllText(Path.Combine(output, "result.txt"), "PASS: 24 actual Player render fixtures; imported BC7 dimensions/mip/readability. Not physical action selection.");
            Application.Quit(0);
        }
    }
}
