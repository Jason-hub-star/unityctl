using System;
using System.Collections.Concurrent;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Unityctl.Plugin.Runtime
{
    /// <summary>
    /// Player-side bridge bootstrap. Development builds only — never the editor
    /// (the editor has its own IPC server) and never release players.
    /// Starts the pipe server, captures logs, and writes a discovery state file
    /// (path logged to the Player log) that the CLI reads to find the pipe.
    /// </summary>
    public static class RuntimeBridge
    {
        public const string StateFileName = "unityctl-runtime.json";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (Application.isEditor || !Debug.isDebugBuild)
                return;

            var host = new GameObject("UnityctlRuntimeBridge")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<RuntimeBridgePump>();
        }
    }

    /// <summary>
    /// Owns the runtime pipe server lifecycle and pumps queued command work on
    /// the main thread from Update() — player loops always tick, so no editor
    /// style delayCall/update caveats apply here.
    /// </summary>
    public sealed class RuntimeBridgePump : MonoBehaviour
    {
        private const int MaxLogEntries = 200;

        private RuntimePipeServer _server;
        private string _stateFilePath;
        private readonly ConcurrentQueue<JObject> _logs = new ConcurrentQueue<JObject>();
        private int _logCount;

        private void OnEnable()
        {
            Application.logMessageReceivedThreaded += OnLogMessage;

            _server = new RuntimePipeServer(HandleCommandOnMainThread);
            _server.Start();

            _stateFilePath = Path.Combine(Application.persistentDataPath, RuntimeBridge.StateFileName);
            var state = new JObject
            {
                ["pipeName"] = _server.PipeName,
                ["pid"] = System.Diagnostics.Process.GetCurrentProcess().Id,
                ["unityVersion"] = Application.unityVersion,
                ["productName"] = Application.productName,
                ["platform"] = Application.platform.ToString(),
                ["startedAtUtc"] = DateTime.UtcNow.ToString("o")
            };
            File.WriteAllText(_stateFilePath, state.ToString());
            Debug.Log($"[unityctl] Runtime bridge on pipe '{_server.PipeName}', state file: {_stateFilePath}");
        }

        private void Update()
        {
            _server?.PumpMainThread();
        }

        private void OnDestroy()
        {
            Application.logMessageReceivedThreaded -= OnLogMessage;
            _server?.Stop();
            _server = null;
            try
            {
                if (_stateFilePath != null && File.Exists(_stateFilePath))
                    File.Delete(_stateFilePath);
            }
            catch { }
        }

        private void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            _logs.Enqueue(new JObject
            {
                ["time"] = DateTime.UtcNow.ToString("o"),
                ["type"] = type.ToString(),
                ["message"] = condition
            });
            if (System.Threading.Interlocked.Increment(ref _logCount) > MaxLogEntries
                && _logs.TryDequeue(out _))
            {
                System.Threading.Interlocked.Decrement(ref _logCount);
            }
        }

        private JObject HandleCommandOnMainThread(string command, JObject parameters)
        {
            switch (command)
            {
                case "runtime-status":
                    return new JObject
                    {
                        ["productName"] = Application.productName,
                        ["unityVersion"] = Application.unityVersion,
                        ["platform"] = Application.platform.ToString(),
                        ["scene"] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                        ["playTimeSeconds"] = Time.realtimeSinceStartup,
                        ["fps"] = Time.smoothDeltaTime > 0f ? 1f / Time.smoothDeltaTime : 0f,
                        ["targetFrameRate"] = Application.targetFrameRate,
                        ["logCount"] = _logCount
                    };

                case "runtime-logs":
                {
                    var limit = parameters?["limit"]?.Value<int?>() ?? 50;
                    var severity = parameters?["severity"]?.Value<string>();
                    var entries = new JArray();
                    foreach (var entry in _logs)
                    {
                        if (severity != null && !string.Equals(
                                entry["type"]?.Value<string>(), severity, StringComparison.OrdinalIgnoreCase))
                            continue;
                        entries.Add(entry);
                    }
                    while (entries.Count > limit)
                        entries.RemoveAt(0);
                    return new JObject { ["entries"] = entries, ["total"] = _logCount };
                }

                default:
                    return null;
            }
        }
    }
}
