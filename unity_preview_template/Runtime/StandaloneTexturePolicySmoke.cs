using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CatCafePreview.Standalone;
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
        IEnumerator VerifySharedPair(LocalCafeBackend backend)
        {
            CafeBackends.UseLocal(backend);
            SceneManager.LoadScene("CatCafePythonState");
            yield return new WaitForSecondsRealtime(2);
            var client = FindObjectOfType<PythonStateClient>();
            var accept = typeof(PythonStateClient).GetMethod("Accept", BindingFlags.Instance | BindingFlags.NonPublic);
            var panel = client.GetComponent<PythonSeatNarrativePanel>() ?? client.gameObject.AddComponent<PythonSeatNarrativePanel>();
            var lines = new List<string>();
            string prefix = SharedPackImages.Root + SharedPackImages.PreviousPack + SharedPackImages.ImageFolder;
            string shared = SharedPackImages.Root + SharedPackImages.SharedPack + SharedPackImages.ImageFolder;
            foreach (int slot in new[] {2, 6})
            {
                StandaloneDispatchConversationSmoke.Good(backend, "load_game", slot:slot);
                var oldRows = backend.Narratives.Rows.Where(row => row.image_resource?.StartsWith(prefix, StringComparison.Ordinal) == true).ToArray();
                if (oldRows.Length == 0) throw new Exception("Old save has no historical image receipt: " + slot);
                foreach (var path in oldRows.Select(row => row.image_resource).Distinct())
                {
                    var expected = Resources.Load<Texture2D>(shared + path.Substring(prefix.Length));
                    if (Resources.Load<Texture2D>(path) != null || expected == null || CafeNarrativeResources.Texture(path) != expected)
                        throw new Exception("Historical path does not share the expected texture: " + path);
                }
                var view = backend.View();
                accept.Invoke(client, new object[] {view});
                var example = oldRows.Last();
                var cat = view.cats.First(row => row.cat_id == example.cat_id);
                panel.Open(new PythonSeatState { narrative_image = example.image_resource, narrative = example.text,
                    customer_name = backend.Details.CustomerName(example.customer_id) }, 0, cat);
                if (!panel.panel.activeInHierarchy || !panel.picture.enabled ||
                    panel.picture.texture != Resources.Load<Texture2D>(shared + example.image_resource.Substring(prefix.Length)))
                    throw new Exception("Saved texture is not displayed in the seat history panel: " + slot);
                yield return new WaitForEndOfFrame();
                CaptureCanvas(panel.panel.GetComponentInParent<Canvas>().rootCanvas, Path.Combine(output, "old-slot-" + slot + ".png"));
                panel.panel.SetActive(false);
                lines.Add("slot " + slot + ": loaded " + oldRows.Length + " old image receipts; verified shared asset in seat history.");
            }
            if (Environment.GetCommandLineArgs().Contains("--shared-all-history"))
            {
                var extraCheck = VerifySharedHistory(backend, lines);
                while (extraCheck.MoveNext()) yield return extraCheck.Current;
            }
            File.WriteAllLines(Path.Combine(output, "shared-pair-result.txt"), lines);
        }

        IEnumerator VerifySharedHistory(LocalCafeBackend backend, List<string> lines)
        {
            string oldPrefix = SharedPackImages.Root + SharedPackImages.PreviousPack + SharedPackImages.ImageFolder;
            var aliases = SharedPackImages.Aliases;
            if (aliases.Count <= 860) throw new Exception("Cross-pack alias data was not included.");
            var archived = aliases.First(row => !row.Key.StartsWith(oldPrefix, StringComparison.Ordinal));
            if (Resources.Load<Texture2D>(archived.Key) != null ||
                new CatGalleryImage {resource = archived.Key}.Texture() != Resources.Load<Texture2D>(archived.Value) ||
                Resources.Load<Texture2D>(archived.Value) == null)
                throw new Exception("Additional old pack did not resolve in the gallery texture path.");

            SceneManager.LoadScene("CatCafeTitle");
            yield return new WaitForSecondsRealtime(2);
            var extra = FindObjectOfType<PythonTitleScreen>().GetComponent<TitleExtraPanel>();
            extra.Open();
            var replay = backend.Replays.Load().entries.First(row => CafeReplayCatalog.IsOptional(row.conversation_id)
                && !string.IsNullOrEmpty(row.image_resource) && aliases.ContainsKey(row.image_resource));
            var expected = Resources.Load<Texture2D>(aliases[replay.image_resource]);
            extra.OpenReplay(replay.Key);
            yield return new WaitForEndOfFrame();
            var portrait = extra.Replay.Panel.GetComponentsInChildren<RawImage>(true).Single(row => row.name == "Portrait");
            if (!extra.Replay.Showing || expected == null || !portrait.enabled || portrait.texture != expected)
                throw new Exception("Archived replay did not show the saved portrait.");
            CaptureCanvas(extra.Replay.Panel.GetComponentInParent<Canvas>().rootCanvas, Path.Combine(output, "old-replay.png"));
            extra.Replay.Close();

            var gallery = extra.Images;
            gallery.Open();
            var unlocked = new HashSet<string>(backend.Gallery.Load().viewed);
            var item = gallery.Items.First(row => unlocked.Contains(row.key));
            gallery.OpenImage(item.key);
            yield return new WaitForEndOfFrame();
            if (!gallery.Viewer.activeInHierarchy || item.Texture() == null || gallery.Enlarged.texture != item.Texture())
                throw new Exception("Previously viewed gallery image did not open.");
            CaptureCanvas(gallery.Viewer.GetComponentInParent<Canvas>().rootCanvas, Path.Combine(output, "old-gallery.png"));
            lines.Add("Read older replay and gallery records; rendered both screens. Cross-pack aliases: " + aliases.Count);
        }

        private static void CaptureCanvas(Canvas canvas, string path)
        {
            var oldMode = canvas.renderMode;
            var oldCamera = canvas.worldCamera;
            var oldDistance = canvas.planeDistance;
            var previous = RenderTexture.active;
            var camera = new GameObject("Shared image QA camera").AddComponent<Camera>();
            var target = new RenderTexture(Screen.width, Screen.height, 24);
            var image = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 1;
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                image.Apply();
                var pixels = image.GetPixels32();
                if (!pixels.Any(pixel => pixel.r != pixels[0].r || pixel.g != pixels[0].g || pixel.b != pixels[0].b))
                    throw new Exception("Captured seat history was blank.");
                File.WriteAllBytes(path, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                canvas.renderMode = oldMode;
                canvas.worldCamera = oldCamera;
                canvas.planeDistance = oldDistance;
                camera.targetTexture = null;
                target.Release();
                Destroy(target);
                Destroy(image);
                Destroy(camera.gameObject);
            }
        }

        IEnumerator Run()
        {
            File.WriteAllText(Path.Combine(output, "environment.txt"), SystemInfo.graphicsDeviceName + "\n" + SystemInfo.graphicsDeviceType + "\nBC7 supported=" + SystemInfo.SupportsTextureFormat(TextureFormat.BC7) + "\n" + SystemInfo.operatingSystem);
            var b = new LocalCafeBackend(Path.Combine(output, "save"));
            if (Environment.GetCommandLineArgs().Contains("--retired-image-save-check"))
            {
                bool resume = Environment.GetCommandLineArgs().Contains("--texture-resume");
                StandaloneDispatchConversationSmoke.Good(b, "load_game", slot:resume ? 3 : 2);
                CafeBackends.UseLocal(b);
                SceneManager.LoadScene("CatCafePythonState");
                yield return new WaitForSecondsRealtime(2);
                var loadedClient = FindObjectOfType<PythonStateClient>();
                var view = b.View();
                typeof(PythonStateClient).GetMethod("Accept", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(loadedClient, new object[] { view });
                var history = loadedClient.GetComponent<PythonSeatNarrativePanel>() ?? loadedClient.gameObject.AddComponent<PythonSeatNarrativePanel>();
                var expected = File.ReadAllLines(Path.Combine(output,"expected.tsv"));
                if (expected.Length != 9) throw new Exception("Incomplete retired image fixture");
                foreach (var line in expected)
                {
                    var fields = line.Split('\t');
                    var original = fields[0];
                    var replacement = Resources.Load<Texture2D>(fields[1]);
                    if (Resources.Load<Texture2D>(original) != null || replacement == null || SharedPackImages.Load(original) != replacement)
                        throw new Exception("Retired image exclusion or redirect failed: " + fields[2]);
                    if (original.StartsWith("CatCafeContent/People/", StringComparison.Ordinal))
                    {
                        if (HumanPortraitResources.Resolve(fields[2]) != replacement) throw new Exception("Wrong customer portrait");
                        continue;
                    }
                    var receipt = b.Narratives.Rows.First(row => row.image_resource == original);
                    var savedCat = view.cats.First(row => row.cat_id == receipt.cat_id);
                    history.Open(new PythonSeatState { narrative_image=receipt.image_resource, narrative=receipt.text,
                        customer_name=b.Details.CustomerName(receipt.customer_id) },0,savedCat);
                    if (!history.panel.activeInHierarchy || !history.picture.enabled || history.picture.texture != replacement)
                        throw new Exception("Wrong seat history image: " + fields[2]);
                    if (new CatGalleryImage {resource=original}.Texture() != replacement) throw new Exception("Wrong gallery image");
                    history.panel.SetActive(false);
                }
                if (!resume) StandaloneDispatchConversationSmoke.Good(b,"save_game",slot:3);
                File.WriteAllText(Path.Combine(output,resume ? "resume.txt" : "result.txt"),"PASS: 9 retired resources absent; 8 saved cat image paths rendered; customer portrait resolved.");
                Application.Quit(0);
                yield break;
            }
            if (Environment.GetCommandLineArgs().Contains("--shared-pair-old-saves"))
            {
                var verification = VerifySharedPair(b);
                while (verification.MoveNext()) yield return verification.Current;
                Application.Quit(0);
                yield break;
            }
            if (Environment.GetCommandLineArgs().Contains("--texture-resume"))
            {
                string saved = File.ReadAllText(Path.Combine(output, "saved-image.txt"));
                StandaloneDispatchConversationSmoke.Good(b, "load_game", slot:2);
                if (b.Narratives.Rows.Last().image_resource != saved || SharedPackImages.Load(saved) == null) throw new Exception("Historical image receipt changed across process");
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
                if (SharedPackImages.Load(resource) == null) throw new Exception("Historical resource missing: " + resource);
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
