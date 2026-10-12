using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace CatCafePreview
{
    // Shared window between screens and simulation. HTTP is one adapter.
    public interface ICafeBackend
    {
        IEnumerator Request(string route, string commandJson, Action<CafeResponse> completed, int timeout = 3);
    }
    public sealed class CafeResponse
    {
        public UnityWebRequest.Result result;
        public string text, error;
    }
    public sealed class HttpCafeBackend : ICafeBackend
    {
        private readonly int port;
        public HttpCafeBackend(int port) { this.port = port; }
        public IEnumerator Request(string route, string commandJson, Action<CafeResponse> completed, int timeout = 3)
        {
            using (var request = new UnityWebRequest("http://127.0.0.1:" + port + route, commandJson == null ? "GET" : "POST"))
            {
                request.downloadHandler = new DownloadHandlerBuffer(); request.timeout = timeout;
                if (commandJson != null) { request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(commandJson)); request.SetRequestHeader("Content-Type", "application/json"); }
                yield return request.SendWebRequest();
                completed(new CafeResponse { result = request.result, text = request.downloadHandler.text, error = request.error });
            }
        }
    }
    // Keeps the existing coroutine/retry semantics while removing URL knowledge from UI.
    public sealed class CafeRequest : IDisposable
    {
        private readonly ICafeBackend backend;
        private readonly string route, body;
        private CafeResponse response;
        public int timeout = 3;
        public CafeRequest(ICafeBackend backend, string route, string body = null) { this.backend = backend; this.route = route; this.body = body; }
        public UnityWebRequest.Result result => response.result;
        public string Text => response.text;
        public string error => response.error;
        public IEnumerator SendWebRequest() { yield return backend.Request(route, body, value => response = value, timeout); }
        public void Dispose() { }
    }
    public static class CafeBackends
    {
        public static bool Standalone = true;
        private static ICafeBackend local;
        public static ICafeBackend Resolve(int port) => Standalone ? Local : new HttpCafeBackend(port);
        public static ICafeBackend Local => local ?? (local = new LocalCafeBackend());
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { local = null; Standalone = !Array.Exists(Environment.GetCommandLineArgs(), value => value == "--catcafe-http"); }
        // Session ownership survives title/business scene transitions.
        public static void UseLocal(ICafeBackend backend) { local = backend ?? throw new ArgumentNullException(nameof(backend)); Standalone = true; }
    }
}
