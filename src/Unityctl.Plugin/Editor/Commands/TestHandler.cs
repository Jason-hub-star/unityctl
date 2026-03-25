using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class TestHandler : CommandHandlerBase
    {
        private static readonly TimeSpan SingleFlightMaxAge = TimeSpan.FromSeconds(360);

        public override string CommandName => WellKnownCommands.Test;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            if (AsyncOperationRegistry.HasRunning(WellKnownCommands.Test, SingleFlightMaxAge))
            {
                return Fail(StatusCode.Busy, "A test run is already in progress");
            }

            var mode = request.GetParam("mode", "edit");
            var filter = request.GetParam("filter", null);
            var requestId = request.requestId;

            UnityEngine.Debug.Log($"[unityctl] Running tests: mode={mode}, filter={filter ?? "(all)"}, requestId={requestId}");

            var testMode = mode.ToLowerInvariant() switch
            {
                "edit" or "editmode" => UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode,
                "play" or "playmode" => UnityEditor.TestTools.TestRunner.Api.TestMode.PlayMode,
                _ => UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode
            };

            var api = UnityEngine.ScriptableObject
                .CreateInstance<UnityEditor.TestTools.TestRunner.Api.TestRunnerApi>();

            TestRunStateStore.CreateRunning(requestId, mode, filter);

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

            var resultCollector = new TestResultCollector(requestId);
            api.RegisterCallbacks(resultCollector);
            PlayModeTestResultRecovery.MarkRegistered(requestId);
            api.Execute(executionSettings);

            // Store Execute() GUID for diagnostics
            // Note: api.Execute() is void in current Unity versions; GUID retrieval is reserved for future API
            AsyncOperationRegistry.Register(requestId, WellKnownCommands.Test);

            var data = new JObject
            {
                ["mode"] = mode,
                ["filter"] = filter ?? "(all)",
                ["requestId"] = requestId,
                ["started"] = true,
                ["pollCommand"] = WellKnownCommands.TestResult,
                ["recommendedNextCommand"] = $"unityctl test-result --project \"<project>\" --request-id \"{requestId}\" --json",
                ["filterSemantics"] = "Unity Test Runner Filter.testNames exact match"
            };
            return Ok(StatusCode.Accepted, $"Tests started (mode={mode}). Poll 'test-result' with requestId to get results.", data);
#else
            return NotInEditor();
#endif
        }

        protected override CommandResponse HandleException(Exception exception)
        {
            return Fail(
                StatusCode.TestFailed,
                $"Test execution failed: {exception.Message}",
                errors: GetStackTrace(exception));
        }
    }

#if UNITY_EDITOR
    internal class TestResultCollector :
        UnityEditor.TestTools.TestRunner.Api.ICallbacks,
        UnityEditor.TestTools.TestRunner.Api.IErrorCallbacks
    {
        private readonly string _requestId;
        private readonly Stopwatch _stopwatch;
        private int _passed;
        private int _failed;
        private int _skipped;
        private readonly List<string> _failures = new();

        public TestResultCollector(string requestId)
        {
            _requestId = requestId;
            _stopwatch = Stopwatch.StartNew();
        }

        public void RunStarted(UnityEditor.TestTools.TestRunner.Api.ITestAdaptor testsToRun) { }

        public void RunFinished(UnityEditor.TestTools.TestRunner.Api.ITestResultAdaptor result)
        {
            _stopwatch.Stop();

            UnityEngine.Debug.Log(
                $"[unityctl] Tests finished: Passed={_passed}, Failed={_failed}, Skipped={_skipped}, " +
                $"Duration={_stopwatch.Elapsed.TotalSeconds:F1}s, requestId={_requestId}");

            var data = new JObject
            {
                ["passed"] = _passed,
                ["failed"] = _failed,
                ["skipped"] = _skipped,
                ["total"] = _passed + _failed + _skipped,
                ["durationMs"] = (long)_stopwatch.Elapsed.TotalMilliseconds,
                ["requestId"] = _requestId
            };

            CommandResponse response;
            if (_failed > 0)
            {
                response = CommandResponse.Fail(
                    StatusCode.TestFailed,
                    $"Tests completed: {_passed} passed, {_failed} failed, {_skipped} skipped ({_stopwatch.Elapsed.TotalSeconds:F1}s)",
                    _failures.Count > 0 ? _failures : null);
                response.data = data;
            }
            else
            {
                response = CommandResponse.Ok(
                    $"Tests completed: {_passed} passed, {_skipped} skipped ({_stopwatch.Elapsed.TotalSeconds:F1}s)",
                    data);
            }

            PersistResult(response);
            AsyncOperationRegistry.Complete(_requestId, response);
            PlayModeTestResultRecovery.MarkCompleted(_requestId);
        }

        public void TestStarted(UnityEditor.TestTools.TestRunner.Api.ITestAdaptor test) { }

        public void TestFinished(UnityEditor.TestTools.TestRunner.Api.ITestResultAdaptor result)
        {
            // Leaf-only: skip suite/fixture aggregation nodes
            if (result.Test.HasChildren) return;

            switch (result.TestStatus)
            {
                case UnityEditor.TestTools.TestRunner.Api.TestStatus.Passed:
                    _passed++;
                    break;
                case UnityEditor.TestTools.TestRunner.Api.TestStatus.Failed:
                    _failed++;
                    _failures.Add($"{result.Test.FullName}: {result.Message}");
                    break;
                case UnityEditor.TestTools.TestRunner.Api.TestStatus.Skipped:
                    _skipped++;
                    break;
            }
        }

        public void OnError(string message)
        {
            _stopwatch.Stop();

            UnityEngine.Debug.LogError(
                $"[unityctl] Test run error (pre-run failure): {message}, requestId={_requestId}");

            var response = CommandResponse.Fail(
                StatusCode.TestFailed,
                $"Test run failed before execution: {message}");

            PersistResult(response);
            AsyncOperationRegistry.Complete(_requestId, response);
            PlayModeTestResultRecovery.MarkCompleted(_requestId);
        }

        private void PersistResult(CommandResponse response)
        {
            try
            {
                var state = TestRunStateStore.Load(_requestId);
                if (state != null)
                    TestRunStateStore.Complete(state, response);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[unityctl] Failed to persist test result: {ex.Message}");
            }
        }
    }

    internal static class PlayModeTestResultRecovery
    {
        private static readonly HashSet<string> RegisteredRequestIds = new HashSet<string>(StringComparer.Ordinal);

        public static void RestorePendingPlayModeRuns()
        {
            foreach (var state in TestRunStateStore.LoadRunningPlayModeStates())
                EnsureRegistered(state);
        }

        public static void MarkRegistered(string requestId)
        {
            if (!string.IsNullOrWhiteSpace(requestId))
                RegisteredRequestIds.Add(requestId);
        }

        public static void MarkCompleted(string requestId)
        {
            if (!string.IsNullOrWhiteSpace(requestId))
                RegisteredRequestIds.Remove(requestId);
        }

        private static void EnsureRegistered(TestRunState state)
        {
            if (state == null || string.IsNullOrWhiteSpace(state.RequestId))
                return;

            if (RegisteredRequestIds.Contains(state.RequestId))
                return;

            var api = UnityEngine.ScriptableObject
                .CreateInstance<UnityEditor.TestTools.TestRunner.Api.TestRunnerApi>();

            api.RegisterCallbacks(new TestResultCollector(state.RequestId));
            RegisteredRequestIds.Add(state.RequestId);
            UnityEngine.Debug.Log($"[unityctl] Reattached play mode test callbacks for requestId={state.RequestId}");
        }
    }
#endif
}
