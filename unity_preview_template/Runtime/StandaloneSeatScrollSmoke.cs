using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace CatCafePreview
{
    // Opt-in, isolated QA. View fixtures and UGUI dispatch are explicitly distinct from physical input.
    public sealed class StandaloneSeatScrollSmoke : MonoBehaviour
    {
        string output;
        int checks;
        LocalCafeBackend backend;
        PythonStateClient client;
        PythonServiceScreen service;
        ScrollRect scroll;
        PythonCafeState fixture;
        bool physical;
        string lastDialog;
        int physicalShots;
        static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "--catcafe-seat-scroll");
            if (i < 0) return;
            var qa = new GameObject("Seat scrolling QA").AddComponent<StandaloneSeatScrollSmoke>();
            DontDestroyOnLoad(qa);
            qa.output = Path.GetFullPath(args[i + 1]);
            Directory.CreateDirectory(qa.output);
            Application.runInBackground = true;
            qa.StartCoroutine(qa.Guard());
        }

        void Check(bool ok, string label)
        {
            checks++;
            File.AppendAllText(Path.Combine(output, "checks.txt"), (ok ? "PASS " : "FAIL ") + label + "\n");
            if (!ok) throw new Exception(label);
        }

        void LateUpdate()
        {
            if (!physical) return;
            if (Input.GetMouseButtonDown(0) || Input.mouseScrollDelta != Vector2.zero)
                File.AppendAllText(Path.Combine(output, "physical-input.txt"), Screen.width + "x" + Screen.height + " mouse=" + Input.mousePosition + " wheel=" + Input.mouseScrollDelta + " offset=" + service.seatContent.anchoredPosition + "\n");
            var reader = client.GetComponent<PythonSeatNarrativePanel>();
            string dialog = reader != null && reader.panel != null && reader.panel.activeSelf ? reader.title.text : "closed";
            if (dialog == lastDialog) return;
            lastDialog = dialog;
            File.AppendAllText(Path.Combine(output, "physical-input.txt"), Screen.width + "x" + Screen.height + " dialog=" + dialog + " offset=" + service.seatContent.anchoredPosition + "\n");
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "physical-dialog-" + Screen.width + "-" + (++physicalShots) + ".png"));
        }

        IEnumerator Guard()
        {
            var work = Run();
            while (true)
            {
                bool more;
                try { more = work.MoveNext(); }
                catch (Exception e) { File.WriteAllText(Path.Combine(output, "failure.txt"), e.ToString()); Debug.LogException(e); Application.Quit(1); yield break; }
                if (!more) break;
                yield return work.Current;
            }
            File.WriteAllText(Path.Combine(output, "result.txt"), "PASS " + checks + " checks. View fixtures + UGUI raycast/scroll dispatch; physical input recorded separately.");
            Application.Quit(0);
        }

        void Apply(PythonCafeState state)
        {
            typeof(PythonStateClient).GetMethod("Accept", Hidden).Invoke(client, new object[] { state });
            service.ShowSeats();
            Canvas.ForceUpdateCanvases();
        }

        PythonCafeState Fixture(int count)
        {
            var view = JsonUtility.FromJson<PythonCafeState>(JsonUtility.ToJson(backend.View()));
            var cats = view.cats;
            Check(cats.Length >= count, "Fixture has enough distinct cats for " + count);
            string[] ids = { "guest-1", "guest-2", "guest-3", "guest-4", "expanded-guest-2", "expanded-guest-3", "expanded-guest-4", "expanded-guest-5" };
            string[] names = { "佐藤さん", "鈴木さん", "高橋さん", "田中さん", "前田さん", "藤田さん", "石田さん", "上田さん" };
            view.seats = Enumerable.Range(0, count).Select(i => new PythonSeatState {
                seat_id = "seat-" + (i + 1), customer_id = ids[i], customer_name = names[i], cat_id = cats[i].cat_id,
                latest_action = "通常の交流", cat_reaction = "猫の様子を確認しています。", narrative = "席" + (i + 1) + "の検証用履歴：" + names[i] + " / " + cats[i].name
            }).ToArray();
            view.customers = view.seats.Select(s => new PythonCustomerState { customer_id = s.customer_id, name = s.customer_name, seat_id = s.seat_id, status = "seated" }).ToArray();
            return view;
        }

        Vector2 Center(RectTransform rect) { var c = new Vector3[4]; rect.GetWorldCorners(c); return (c[0] + c[2]) * .5f; }
        bool Inside(RectTransform child)
        {
            var c = new Vector3[4]; child.GetWorldCorners(c);
            var bounds = scroll.viewport.rect; bounds.xMin -= .2f; bounds.xMax += .2f; bounds.yMin -= .2f; bounds.yMax += .2f;
            return c.All(p => bounds.Contains(scroll.viewport.InverseTransformPoint(p)));
        }
        GameObject Hit(Vector2 point)
        {
            var data = new PointerEventData(EventSystem.current) { position = point };
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(data, hits);
            return hits.Count == 0 ? null : hits[0].gameObject;
        }
        void Click(Button button)
        {
            var point = Center(button.GetComponent<RectTransform>());
            var hit = Hit(point);
            Check(hit != null && ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit) == button.gameObject, "Raycast reaches " + button.name);
            ExecuteEvents.ExecuteHierarchy(hit, new PointerEventData(EventSystem.current) { position = point, button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
        }
        void Wheel(float delta)
        {
            var point = Center(scroll.viewport);
            ExecuteEvents.ExecuteHierarchy(Hit(point), new PointerEventData(EventSystem.current) { position = point, scrollDelta = new Vector2(0, delta) }, ExecuteEvents.scrollHandler);
            Canvas.ForceUpdateCanvases();
        }
        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, name + ".png"));
            yield return null;
        }
        void Record(string label)
        {
            var rect = service.GetSeatCard(fixture.seats.Length - 1).GetComponent<RectTransform>();
            File.AppendAllText(Path.Combine(output, "dimensions.txt"), label + " Screen=" + Screen.width + "x" + Screen.height +
                " viewport=" + scroll.viewport.rect + " content=" + service.seatContent.rect + " offset=" + service.seatContent.anchoredPosition +
                " lastCard=" + rect.rect + " lastButtonCenter=" + Center(service.GetSeatCard(fixture.seats.Length - 1).details.GetComponent<RectTransform>()) + "\n");
        }

        IEnumerator Run()
        {
            backend = new LocalCafeBackend(Path.Combine(output, "save"));
            var response = StandaloneGrowthBatchSmoke.Send(backend, "load_game", slot: 1);
            Check(response.result == UnityEngine.Networking.UnityWebRequest.Result.Success, "Load isolated eight-seat save");
            Check(backend.View().seats.Length == 2 + backend.Simulation.Rules.expansion_costs.Length && backend.View().seats.Length == 8, "Configured maximum = initial 2 + six expansion costs");
            CafeBackends.UseLocal(backend);
            SceneManager.LoadScene("CatCafePythonState");
            yield return new WaitForSecondsRealtime(2);
            client = FindObjectOfType<PythonStateClient>(); service = client.GetComponent<PythonServiceScreen>();
            // Keep view fixtures stable across rendered frames. Real backend updates are applied below.
            client.StopAllCoroutines();
            foreach (var size in new[] { new Vector2Int(1280, 800), new Vector2Int(1024, 768) })
            {
                Screen.SetResolution(size.x, size.y, false); yield return new WaitForSecondsRealtime(.7f);
                Check(Screen.width == size.x && Screen.height == size.y, "Actual drawing resolution " + size);
                foreach (int count in Enumerable.Range(2, 7))
                {
                    fixture = Fixture(count); Apply(fixture); yield return null; Canvas.ForceUpdateCanvases();
                    for (int i = 0; i < count; i++)
                    {
                        var card = service.GetSeatCard(i);
                        Check(card.customerIcon.GetComponentInChildren<RawImage>().texture == HumanPortraitResources.Resolve(fixture.seats[i].customer_id) && card.picture.texture != null && card.stateText.fontSize == 15 && card.customerIcon.GetComponent<RectTransform>().rect.width == 24, "Preserved portrait/cat/text size at seat " + (i + 1));
                    }
                    scroll = service.seatPanel.GetComponent<ScrollRect>(); scroll.verticalNormalizedPosition = 1; Canvas.ForceUpdateCanvases();
                    Check(Inside(service.GetSeatCard(0).GetComponent<RectTransform>()), size + " " + count + " seats: first full card");
                    string before = JsonUtility.ToJson(backend.Simulation.State);
                    Wheel(-3); Check(before == JsonUtility.ToJson(backend.Simulation.State), "Wheel preserves simulation and stopped automatic state");
                    if (count > 4) Check(service.seatContent.anchoredPosition.y >= 119, "Wheel moves 120 logical pixels");
                    // ScrollRect synchronizes its scrollbar in LateUpdate, as with physical input.
                    yield return null;
                    scroll.verticalScrollbar.value = 0; Canvas.ForceUpdateCanvases();
                    for (int i = count > 4 ? 4 : 0; i < count; i++) Check(Inside(service.GetSeatCard(i).GetComponent<RectTransform>()), size + " full bottom card " + (i + 1));
                    Record(size.x + " count=" + count);
                    if (count == 2 || count == 4 || count == 5 || count == 8) yield return Shot("bottom-" + count + "-" + size.x);
                    foreach (int index in new[] { 0, count / 2, count - 1 }.Distinct())
                    {
                        scroll.verticalNormalizedPosition = index < 4 ? 1 : 0; Canvas.ForceUpdateCanvases();
                        float offset = service.seatContent.anchoredPosition.y;
                        Click(service.GetSeatCard(index).details);
                        yield return null; Canvas.ForceUpdateCanvases();
                        var reader = client.GetComponent<PythonSeatNarrativePanel>();
                        var seat = fixture.seats[index]; var cat = fixture.cats.First(c => c.cat_id == seat.cat_id);
                        Check(reader.panel.activeSelf && reader.title.text.Contains("席 " + (index + 1)) && reader.title.text.Contains(seat.customer_name) && reader.title.text.Contains(cat.name), "Correct customer/cat/seat " + (index + 1));
                        Wheel(-3); Check(Math.Abs(offset - service.seatContent.anchoredPosition.y) < .1f, "Modal blocks scroll through to seats");
                        if (count == 8 && index == 7) yield return Shot("detail-last-" + size.x);
                        Click(reader.panel.transform.Find("Card/Close").GetComponent<Button>());
                        yield return null;
                        Check(Math.Abs(offset - service.seatContent.anchoredPosition.y) < .1f, "Details close preserves scroll position");
                    }
                    scroll.verticalNormalizedPosition = 0; Canvas.ForceUpdateCanvases();
                    float savedOffset = service.seatContent.anchoredPosition.y;
                    service.ShowCats(); service.ShowSeats(); Apply(fixture); yield return null;
                    Check(Math.Abs(savedOffset - service.seatContent.anchoredPosition.y) < .1f, "Tab and update preserve scroll position");
                    Wheel(100); Check(service.seatContent.anchoredPosition.y < 1, "Wheel reaches upper clamp");
                    Wheel(-100); Check(Inside(service.GetSeatCard(count - 1).GetComponent<RectTransform>()), "Wheel reaches lower clamp");
                }
                // Update a scrolled card and ensure the listener and portrait are replaced together.
                var last = fixture.seats[7]; last.customer_id = "guest-1"; last.customer_name = "佐藤さん"; last.cat_id = fixture.cats[0].cat_id;
                Apply(fixture); yield return null; Click(service.GetSeatCard(7).details); yield return null;
                Check(client.GetComponent<PythonSeatNarrativePanel>().title.text.Contains("佐藤さん"), "Customer replacement updates click target");
                Check(service.GetSeatCard(7).customerIcon.GetComponentInChildren<RawImage>().texture == HumanPortraitResources.Resolve("guest-1"), "Replacement portrait matches current customer");
                client.GetComponent<PythonSeatNarrativePanel>().panel.SetActive(false);
                last.customer_id = last.cat_id = null; Apply(fixture); yield return null;
                Check(!service.GetSeatCard(7).customerIcon.activeSelf && !service.GetSeatCard(7).picture.enabled, "Vacant last seat clears both images");
                Click(service.GetSeatCard(7).details); yield return null; Check(client.GetComponent<PythonSeatNarrativePanel>().title.text.Contains("空席"), "Vacant seat opens current empty details");
                client.GetComponent<PythonSeatNarrativePanel>().panel.SetActive(false);
            }
            // Real backend update and save reload (no view fixture for this check).
            Apply(backend.View()); yield return null; scroll.verticalNormalizedPosition = 0; Canvas.ForceUpdateCanvases();
            string beforeAutomatic = JsonUtility.ToJson(backend.Simulation.State);
            client.ToggleAutomatic(); Check(client.Automatic, "Automatic can start");
            Wheel(3); Check(client.Automatic && beforeAutomatic == JsonUtility.ToJson(backend.Simulation.State), "Wheel alone leaves automatic running and simulation unchanged");
            client.ToggleAutomatic(); Check(!client.Automatic, "Automatic can stop");
            backend.Simulation.Execute("advance_business"); Apply(backend.View()); yield return null;
            for (int i = 0; i < 8; i++)
            {
                var seat = backend.View().seats[i]; var card = service.GetSeatCard(i);
                Check(card.state?.cat_id == seat.cat_id && card.customerIcon.activeSelf == !string.IsNullOrEmpty(seat.customer_id) && (string.IsNullOrEmpty(seat.customer_id) || card.stateText.text.Contains(seat.customer_name)), "Business update maps seat " + (i + 1));
            }
            Check(StandaloneGrowthBatchSmoke.Send(backend, "save_game", slot: 2).result == UnityEngine.Networking.UnityWebRequest.Result.Success, "Save after scroll");
            var restored = new LocalCafeBackend(Path.Combine(output, "save"));
            Check(StandaloneGrowthBatchSmoke.Send(restored, "load_game", slot: 2).result == UnityEngine.Networking.UnityWebRequest.Result.Success, "Reload in independent backend");
            Check(string.Join("|", backend.View().seats.Select(s => JsonUtility.ToJson(s))) == string.Join("|", restored.View().seats.Select(s => JsonUtility.ToJson(s))), "Reload preserves seats");
            Apply(restored.View()); yield return null; scroll.verticalNormalizedPosition = 0; Canvas.ForceUpdateCanvases(); Check(Inside(service.GetSeatCard(7).GetComponent<RectTransform>()), "Reload keeps bottom reachable");
            // Optional visible physical input session. Freeze the backend; only user input changes UI.
            if (Environment.GetCommandLineArgs().Contains("--seat-scroll-hold"))
            {
                physical = true;
                fixture = Fixture(8); Apply(fixture); scroll.verticalNormalizedPosition = 1; yield return null;
                foreach (var size in new[] { new Vector2Int(1280, 800), new Vector2Int(1024, 768) })
                {
                    Screen.SetResolution(size.x, size.y, false); yield return new WaitForSecondsRealtime(.8f);
                    scroll.verticalNormalizedPosition = 1; Canvas.ForceUpdateCanvases();
                    Record("PHYSICAL_READY");
                    File.WriteAllText(Path.Combine(output, "ready-" + size.x + ".txt"), "Actual Screen=" + Screen.width + "x" + Screen.height);
                    while (!File.Exists(Path.Combine(output, "done-" + size.x + ".txt"))) yield return null;
                    Record("PHYSICAL_DONE"); yield return Shot("physical-after-" + size.x);
                }
            }
        }
    }
}
