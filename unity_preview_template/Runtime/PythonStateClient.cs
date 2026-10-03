using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace CatCafePreview
{
    [Serializable] public class PythonCatState
    {
        public string cat_id, content_cat_id, name, health_status, activity;
        public float stamina, max_stamina;
        public bool working;
    }
    [Serializable] public class PythonCafeState
    {
        public int version, day, tick;
        public float funds;
        public PythonCatState[] cats;
    }
    [RequireComponent(typeof(ContentPreview))]
    public class PythonStateClient : MonoBehaviour
    {
        [SerializeField] private int port = 8190;
        private IEnumerator Start()
        {
            // ContentPreview.Start binds the saved UI before the first response.
            yield return null;
            var preview = GetComponent<ContentPreview>();
            while (true)
            {
                using (var request = UnityWebRequest.Get("http://127.0.0.1:" + port + "/state"))
                {
                    request.timeout = 3;
                    yield return request.SendWebRequest();
                    if (request.result != UnityWebRequest.Result.Success) preview.PythonConnectionError(request.error);
                    else
                    {
                        try
                        {
                            var state = JsonUtility.FromJson<PythonCafeState>(request.downloadHandler.text);
                            Validate(state);
                            preview.ApplyPythonState(state);
                        }
                        catch (Exception ex) { preview.PythonConnectionError(ex.Message); }
                    }
                }
                yield return new WaitForSecondsRealtime(2);
            }
        }
        public static void Validate(PythonCafeState state)
        {
            if (state == null || state.version != 1 || state.cats == null || state.cats.Length == 0)
                throw new ArgumentException("ゲーム状態の形式が不正です。");
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var cat in state.cats)
                if (cat == null || string.IsNullOrEmpty(cat.cat_id) || !ids.Add(cat.cat_id)
                    || ! (cat.max_stamina > 0) || float.IsInfinity(cat.max_stamina)
                    || !(cat.stamina >= 0 && cat.stamina <= cat.max_stamina))
                    throw new ArgumentException("猫の状態が不正です。");
        }
    }
}
