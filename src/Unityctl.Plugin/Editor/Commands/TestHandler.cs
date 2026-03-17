using System;
using System.Collections.Generic;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class TestHandler : IUnityctlCommand
    {
        public string CommandName => "test";

        public CommandResponse Execute(CommandRequest request)
        {
#if UNITY_EDITOR
            try
            {
                var mode = GetParam(request, "mode", "edit");
                var filter = GetParam(request, "filter", null);

                UnityEngine.Debug.Log($"[unityctl] Running tests: mode={mode}, filter={filter ?? "(all)"}");

                // Use Unity Test Framework API
                var testMode = mode.ToLowerInvariant() switch
                {
                    "edit" or "editmode" => UnityEngine.TestTools.TestPlatform.EditMode,
                    "play" or "playmode" => UnityEngine.TestTools.TestPlatform.PlayMode,
                    _ => UnityEngine.TestTools.TestPlatform.EditMode
                };

                // For batchmode, we use the command-line test runner approach
                // The results are collected via TestRunnerApi callbacks
                var api = UnityEditor.TestTools.TestRunner.Api.ScriptableObject
                    .CreateInstance<UnityEditor.TestTools.TestRunner.Api.TestRunnerApi>();

                var executionSettings = new UnityEditor.TestTools.TestRunner.Api.ExecutionSettings
                {
                    filters = new[]
                    {
                        new UnityEditor.TestTools.TestRunner.Api.Filter
                        {
                            testMode = testMode,
                            testNames = string.IsNullOrEmpty(filter) ? null : new[] { filter }
                        }
                    }
                };

                // Synchronous execution in batchmode
                var resultCollector = new TestResultCollector();
                api.RegisterCallbacks(resultCollector);
                api.Execute(executionSettings);

                // In batchmode, tests run synchronously
                // The results will be available in the collector after Execute returns

                return CommandResponse.Ok($"Tests started (mode={mode})", new Dictionary<string, object>
                {
                    ["mode"] = mode,
                    ["filter"] = filter ?? "(all)"
                });
            }
            catch (Exception e)
            {
                return CommandResponse.Fail(StatusCode.TestFailed,
                    $"Test execution failed: {e.Message}",
                    new List<string> { e.StackTrace });
            }
#else
            return CommandResponse.Fail(StatusCode.UnknownError, "Not running in Unity Editor");
#endif
        }

        private static string GetParam(CommandRequest request, string key, string defaultValue)
        {
            if (request.parameters != null && request.parameters.TryGetValue(key, out var val) && val != null)
                return val.ToString();
            return defaultValue;
        }
    }

#if UNITY_EDITOR
    internal class TestResultCollector : UnityEditor.TestTools.TestRunner.Api.ICallbacks
    {
        public int Passed { get; private set; }
        public int Failed { get; private set; }
        public int Skipped { get; private set; }
        public List<string> Failures { get; } = new();

        public void RunStarted(UnityEditor.TestTools.TestRunner.Api.ITestAdaptor testsToRun) { }

        public void RunFinished(UnityEditor.TestTools.TestRunner.Api.ITestResultAdaptor result)
        {
            UnityEngine.Debug.Log($"[unityctl] Tests finished: Passed={Passed}, Failed={Failed}, Skipped={Skipped}");
        }

        public void TestStarted(UnityEditor.TestTools.TestRunner.Api.ITestAdaptor test) { }

        public void TestFinished(UnityEditor.TestTools.TestRunner.Api.ITestResultAdaptor result)
        {
            switch (result.TestStatus)
            {
                case UnityEditor.TestTools.TestRunner.Api.TestStatus.Passed:
                    Passed++;
                    break;
                case UnityEditor.TestTools.TestRunner.Api.TestStatus.Failed:
                    Failed++;
                    Failures.Add($"{result.Test.FullName}: {result.Message}");
                    break;
                case UnityEditor.TestTools.TestRunner.Api.TestStatus.Skipped:
                    Skipped++;
                    break;
            }
        }
    }
#endif
}
