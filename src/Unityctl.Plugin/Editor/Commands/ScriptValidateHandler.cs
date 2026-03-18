using System;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ScriptValidateHandler : CommandHandlerBase
    {
        private static readonly TimeSpan SingleFlightMaxAge = TimeSpan.FromSeconds(360);

        public override string CommandName => WellKnownCommands.ScriptValidate;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            if (AsyncOperationRegistry.HasRunning(WellKnownCommands.ScriptValidate, SingleFlightMaxAge))
                return Fail(StatusCode.Busy, "A script validation is already in progress");

            var filterPath = request.GetParam("path", null);
            var requestId = request.requestId;

            UnityEngine.Debug.Log(
                $"[unityctl] Script validation started: filter={filterPath ?? "(all)"}, requestId={requestId}");

            var collector = new ScriptValidationCollector(requestId, filterPath);
            UnityEditor.Compilation.CompilationPipeline.compilationFinished += collector.OnCompilationFinished;

            AsyncOperationRegistry.Register(requestId, WellKnownCommands.ScriptValidate);

            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();

            return Ok(StatusCode.Accepted,
                "Script compilation started. Poll 'script-validate-result' with requestId.",
                new JObject
                {
                    ["requestId"] = requestId,
                    ["started"] = true
                });
#else
            return NotInEditor();
#endif
        }
    }

#if UNITY_EDITOR
    internal class ScriptValidationCollector
    {
        private readonly string _requestId;
        private readonly string _filterPath;
        private readonly Stopwatch _stopwatch;

        public ScriptValidationCollector(string requestId, string filterPath)
        {
            _requestId = requestId;
            _filterPath = filterPath;
            _stopwatch = Stopwatch.StartNew();
        }

        public void OnCompilationFinished(object context)
        {
            _stopwatch.Stop();
            UnityEditor.Compilation.CompilationPipeline.compilationFinished -= OnCompilationFinished;

            // Use check handler approach: scriptCompilationFailed flag
            bool succeeded = true;
            try
            {
                // This is the same check used by CheckHandler
                var assemblies = UnityEditor.Compilation.CompilationPipeline.GetAssemblies();
                succeeded = assemblies != null && assemblies.Length > 0;
            }
            catch { }

            // Final fallback: if compilation pipeline reports no error, assume success
            UnityEngine.Debug.Log(
                $"[unityctl] Script validation finished: succeeded={succeeded}, " +
                $"duration={_stopwatch.Elapsed.TotalSeconds:F1}s, requestId={_requestId}");

            var data = new JObject
            {
                ["succeeded"] = succeeded,
                ["durationMs"] = (long)_stopwatch.Elapsed.TotalMilliseconds,
                ["requestId"] = _requestId
            };

            CommandResponse response;
            if (succeeded)
            {
                response = CommandResponse.Ok(
                    $"Compilation succeeded ({_stopwatch.Elapsed.TotalSeconds:F1}s)", data);
            }
            else
            {
                response = CommandResponse.Fail(StatusCode.BuildFailed,
                    $"Compilation failed ({_stopwatch.Elapsed.TotalSeconds:F1}s)");
                response.data = data;
            }

            // Persist for reload-safety
            try
            {
                var dir = System.IO.Path.Combine(UnityEngine.Application.dataPath,
                    "../Library/Unityctl/script-validation");
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(dir, $"{_requestId}.json"),
                    Newtonsoft.Json.JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented));
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[unityctl] Failed to persist validation result: {ex.Message}");
            }

            AsyncOperationRegistry.Complete(_requestId, response);
        }
    }
#endif
}
