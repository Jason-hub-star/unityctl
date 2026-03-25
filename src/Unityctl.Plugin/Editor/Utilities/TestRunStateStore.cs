#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Utilities
{
    internal sealed class TestRunState
    {
        public string RequestId = string.Empty;
        public string Command = WellKnownCommands.Test;
        public string Mode = "edit";
        public string Filter = string.Empty;
        public string Status = "running";
        public DateTime StartedAtUtc;
        public DateTime? CompletedAtUtc;
        public StoredResponse Response;
    }

    internal static class TestRunStateStore
    {
        private static readonly TimeSpan DefaultRunningTtl = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan DefaultCompletedTtl = TimeSpan.FromHours(24);

        public static string DirectoryPath
        {
            get
            {
                return Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../Library/Unityctl/test-runs"));
            }
        }

        public static TestRunState CreateRunning(string requestId, string mode, string filter)
        {
            PruneDefault();

            var state = new TestRunState
            {
                RequestId = requestId,
                Mode = string.IsNullOrWhiteSpace(mode) ? "edit" : mode,
                Filter = filter ?? string.Empty,
                StartedAtUtc = DateTime.UtcNow
            };

            Save(state);
            return state;
        }

        public static TestRunState Load(string requestId)
        {
            PruneDefault();

            var path = GetPath(requestId);
            if (!File.Exists(path))
                return null;

            try
            {
                return JsonConvert.DeserializeObject<TestRunState>(File.ReadAllText(path));
            }
            catch
            {
                return null;
            }
        }

        public static IReadOnlyList<TestRunState> LoadRunningPlayModeStates()
        {
            PruneDefault();

            if (!Directory.Exists(DirectoryPath))
                return Array.Empty<TestRunState>();

            var states = new List<TestRunState>();
            foreach (var path in Directory.GetFiles(DirectoryPath, "*.json"))
            {
                try
                {
                    var state = JsonConvert.DeserializeObject<TestRunState>(File.ReadAllText(path));
                    if (state == null)
                        continue;

                    if (string.Equals(state.Status, "running", StringComparison.OrdinalIgnoreCase)
                        && (string.Equals(state.Mode, "play", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(state.Mode, "playmode", StringComparison.OrdinalIgnoreCase)))
                    {
                        states.Add(state);
                    }
                }
                catch
                {
                    // Ignore broken state files. Prune handles cleanup.
                }
            }

            return states;
        }

        public static void Save(TestRunState state)
        {
            if (!Directory.Exists(DirectoryPath))
                Directory.CreateDirectory(DirectoryPath);

            File.WriteAllText(GetPath(state.RequestId), JsonConvert.SerializeObject(state, Formatting.Indented));
        }

        public static void Complete(TestRunState state, CommandResponse response)
        {
            state.Status = "completed";
            state.CompletedAtUtc = DateTime.UtcNow;
            state.Response = new StoredResponse
            {
                StatusCode = response.statusCode,
                Success = response.success,
                Message = response.message,
                Data = response.data,
                Errors = response.errors
            };

            Save(state);
        }

        public static CommandResponse ToCommandResponse(TestRunState state)
        {
            var response = state.Response;
            if (response == null)
                return CommandResponse.Fail(StatusCode.NotFound, $"No test result found for requestId: {state.RequestId}");

            return new CommandResponse
            {
                statusCode = response.StatusCode,
                success = response.Success,
                message = response.Message,
                data = response.Data,
                errors = response.Errors,
                requestId = state.RequestId
            };
        }

        public static void Prune(TimeSpan runningTtl, TimeSpan completedTtl)
        {
            if (!Directory.Exists(DirectoryPath))
                return;

            foreach (var path in Directory.GetFiles(DirectoryPath, "*.json"))
            {
                try
                {
                    var state = JsonConvert.DeserializeObject<TestRunState>(File.ReadAllText(path));
                    if (state == null)
                    {
                        File.Delete(path);
                        continue;
                    }

                    var age = DateTime.UtcNow - state.StartedAtUtc;
                    var ttl = string.Equals(state.Status, "completed", StringComparison.OrdinalIgnoreCase)
                        ? completedTtl
                        : runningTtl;

                    if (age > ttl)
                        File.Delete(path);
                }
                catch
                {
                    try { File.Delete(path); } catch { }
                }
            }
        }

        public static void PruneDefault()
        {
            Prune(DefaultRunningTtl, DefaultCompletedTtl);
        }

        private static string GetPath(string requestId)
        {
            return Path.Combine(DirectoryPath, requestId + ".json");
        }
    }
}
#endif
