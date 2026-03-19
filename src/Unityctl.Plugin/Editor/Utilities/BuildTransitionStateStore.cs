#if UNITY_EDITOR
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Utilities
{
    internal sealed class BuildTransitionState
    {
        public string RequestId = string.Empty;
        public string Command = string.Empty;
        public string Status = "running";
        public string RequestedTarget = string.Empty;
        public string RequestedProfileId = string.Empty;
        public bool Changed;
        public int StableHitCount;
        public DateTime StartedAtUtc;
        public JObject PreviousProfile;
        public StoredResponse Response;
    }

    internal sealed class StoredResponse
    {
        public int StatusCode;
        public bool Success;
        public string Message;
        public JObject Data;
        public System.Collections.Generic.List<string> Errors;
    }

    internal static class BuildTransitionStateStore
    {
        private static readonly TimeSpan DefaultRunningTtl = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan DefaultCompletedTtl = TimeSpan.FromHours(24);

        public static string DirectoryPath
        {
            get
            {
                return Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../Library/Unityctl/build-state"));
            }
        }

        public static BuildTransitionState CreateRunning(
            string requestId,
            string command,
            string requestedTarget,
            string requestedProfileId,
            bool changed,
            JObject previousProfile)
        {
            PruneDefault();

            var state = new BuildTransitionState
            {
                RequestId = requestId,
                Command = command,
                RequestedTarget = requestedTarget,
                RequestedProfileId = requestedProfileId,
                Changed = changed,
                PreviousProfile = previousProfile,
                StartedAtUtc = DateTime.UtcNow
            };

            Save(state);
            return state;
        }

        public static BuildTransitionState Load(string requestId)
        {
            PruneDefault();

            var path = GetPath(requestId);
            if (!File.Exists(path))
                return null;

            try
            {
                return JsonConvert.DeserializeObject<BuildTransitionState>(File.ReadAllText(path));
            }
            catch
            {
                return null;
            }
        }

        public static void Save(BuildTransitionState state)
        {
            if (!Directory.Exists(DirectoryPath))
                Directory.CreateDirectory(DirectoryPath);

            File.WriteAllText(GetPath(state.RequestId), JsonConvert.SerializeObject(state, Formatting.Indented));
        }

        public static CommandResponse ToCommandResponse(BuildTransitionState state)
        {
            var response = state.Response;
            if (response == null)
                return CommandResponse.Fail(StatusCode.NotFound, $"No transition result found for requestId: {state.RequestId}");

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

        public static void Complete(BuildTransitionState state, CommandResponse response)
        {
            state.Status = "completed";
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

        public static void Prune(TimeSpan runningTtl, TimeSpan completedTtl)
        {
            if (!Directory.Exists(DirectoryPath))
                return;

            foreach (var path in Directory.GetFiles(DirectoryPath, "*.json"))
            {
                try
                {
                    var state = JsonConvert.DeserializeObject<BuildTransitionState>(File.ReadAllText(path));
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
