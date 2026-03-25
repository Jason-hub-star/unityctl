using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Unityctl.Plugin.Editor.Commands
{
#if UNITY_EDITOR
    /// <summary>
    /// Bridge-managed collector that caches CompilerMessage[] with file/line/column/message detail.
    /// Persists to Library/Unityctl/compile-errors/latest.json for reload safety.
    /// </summary>
    internal static class ScriptCompilationCollector
    {
        private static readonly string PersistDir =
            System.IO.Path.Combine(UnityEngine.Application.dataPath, "../Library/Unityctl/compile-errors");

        private static readonly string PersistPath =
            System.IO.Path.Combine(PersistDir, "latest.json");

        private static readonly List<CompileError> PendingErrors = new List<CompileError>();
        private static readonly List<CompileError> PendingWarnings = new List<CompileError>();

        private static JObject _latestResult;
        private static readonly object Lock = new object();
        private static bool _subscribed;

        public static void EnsureSubscribed()
        {
            if (_subscribed)
                return;

            UnityEditor.Compilation.CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
            UnityEditor.Compilation.CompilationPipeline.compilationFinished += OnCompilationFinished;
            _subscribed = true;
        }

        public static void ResetSubscriptions()
        {
            if (!_subscribed)
                return;

            UnityEditor.Compilation.CompilationPipeline.assemblyCompilationFinished -= OnAssemblyCompilationFinished;
            UnityEditor.Compilation.CompilationPipeline.compilationFinished -= OnCompilationFinished;
            _subscribed = false;
        }

        public static JObject GetLatestResult()
        {
            lock (Lock)
            {
                if (_latestResult != null)
                    return _latestResult.DeepClone() as JObject;
            }

            // Try reading persisted file
            try
            {
                if (System.IO.File.Exists(PersistPath))
                {
                    var json = System.IO.File.ReadAllText(PersistPath);
                    var result = JObject.Parse(json);
                    lock (Lock) { _latestResult = result; }
                    return result.DeepClone() as JObject;
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[unityctl] Failed to read compile errors cache: {ex.Message}");
            }

            return null;
        }

        private static void OnAssemblyCompilationFinished(string assemblyPath, UnityEditor.Compilation.CompilerMessage[] messages)
        {
            if (messages == null) return;

            var assemblyName = System.IO.Path.GetFileNameWithoutExtension(assemblyPath);

            lock (Lock)
            {
                foreach (var msg in messages)
                {
                    var entry = new CompileError
                    {
                        File = msg.file ?? "",
                        Line = msg.line,
                        Column = msg.column,
                        Message = msg.message ?? "",
                        Assembly = assemblyName
                    };

                    // Parse CS error code from message (e.g. "error CS1002: ; expected")
                    entry.Code = ParseErrorCode(msg.message);

                    if (msg.type == UnityEditor.Compilation.CompilerMessageType.Error)
                        PendingErrors.Add(entry);
                    else
                        PendingWarnings.Add(entry);
                }
            }
        }

        private static void OnCompilationFinished(object context)
        {
            lock (Lock)
            {
                var errors = new JArray();
                foreach (var e in PendingErrors)
                    errors.Add(e.ToJson());

                var warnings = new JArray();
                foreach (var w in PendingWarnings)
                    warnings.Add(w.ToJson());

                _latestResult = new JObject
                {
                    ["errors"] = errors,
                    ["warnings"] = warnings,
                    ["errorCount"] = PendingErrors.Count,
                    ["warningCount"] = PendingWarnings.Count,
                    ["compiledAt"] = DateTime.UtcNow.ToString("o"),
                    ["isStale"] = false
                };

                PendingErrors.Clear();
                PendingWarnings.Clear();
            }

            // Persist to disk
            try
            {
                if (!System.IO.Directory.Exists(PersistDir))
                    System.IO.Directory.CreateDirectory(PersistDir);
                System.IO.File.WriteAllText(PersistPath,
                    JsonConvert.SerializeObject(_latestResult, Formatting.Indented));
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[unityctl] Failed to persist compile errors: {ex.Message}");
            }
        }

        private static string ParseErrorCode(string message)
        {
            if (string.IsNullOrEmpty(message)) return "";
            // Pattern: "error CS1002:" or "warning CS0219:"
            var idx = message.IndexOf("CS", StringComparison.Ordinal);
            if (idx < 0) return "";
            var end = idx + 2;
            while (end < message.Length && char.IsDigit(message[end])) end++;
            return end > idx + 2 ? message.Substring(idx, end - idx) : "";
        }

        private struct CompileError
        {
            public string File;
            public int Line;
            public int Column;
            public string Code;
            public string Message;
            public string Assembly;

            public JObject ToJson()
            {
                return new JObject
                {
                    ["file"] = File,
                    ["line"] = Line,
                    ["column"] = Column,
                    ["code"] = Code,
                    ["message"] = Message,
                    ["assembly"] = Assembly
                };
            }
        }
    }
#endif
}
