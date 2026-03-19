#if UNITY_EDITOR
using System;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Commands;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Utilities
{
    internal static class BuildTransitionUtility
    {
        public static CommandResponse CompleteFailure(string requestId, string logContext, CommandResponse failure)
        {
            UnityEngine.Debug.LogError("[unityctl] " + logContext + ": " + failure.message);
            failure.requestId = requestId;

            var state = BuildTransitionStateStore.Load(requestId);
            if (state != null)
                BuildTransitionStateStore.Complete(state, failure);

            AsyncOperationRegistry.Complete(requestId, failure);
            return failure;
        }

        public static CommandResponse GetPollingResponse(
            string requestId,
            string operationName,
            string successMessage,
            Func<BuildTransitionState, JObject, string, JObject> successDataFactory)
        {
            var state = BuildTransitionStateStore.Load(requestId);
            if (state == null)
                return CommandResponse.Fail(StatusCode.NotFound, $"No {operationName} found for requestId: {requestId}");

            if (string.Equals(state.Status, "completed", StringComparison.OrdinalIgnoreCase))
                return BuildTransitionStateStore.ToCommandResponse(state);

            var activeProfile = BuildProfileUtility.GetActiveProfileSummary();
            var currentTarget = BuildProfileUtility.ToCanonicalName(UnityEditor.EditorUserBuildSettings.activeBuildTarget);
            var isStable = !UnityEditor.EditorApplication.isCompiling && !UnityEditor.EditorApplication.isUpdating;
            var currentProfileId = activeProfile == null ? null : activeProfile["id"] == null ? null : activeProfile["id"].Value<string>();

            var matchesTarget = string.Equals(currentTarget, state.RequestedTarget, StringComparison.Ordinal);
            var matchesProfile = string.Equals(currentProfileId, state.RequestedProfileId, StringComparison.Ordinal);

            if (matchesTarget && matchesProfile && isStable)
                state.StableHitCount++;
            else
                state.StableHitCount = 0;

            BuildTransitionStateStore.Save(state);

            if (state.StableHitCount >= 2)
            {
                var response = CommandResponse.Ok(successMessage, successDataFactory(state, activeProfile, currentTarget));
                response.requestId = requestId;
                BuildTransitionStateStore.Complete(state, response);
                AsyncOperationRegistry.Complete(requestId, response);
                return response;
            }

            return new CommandResponse
            {
                statusCode = (int)StatusCode.Accepted,
                success = true,
                message = operationName + " still stabilizing",
                requestId = requestId,
                data = new JObject
                {
                    ["requestId"] = requestId,
                    ["requestedProfileId"] = state.RequestedProfileId,
                    ["currentProfileId"] = currentProfileId,
                    ["requestedTarget"] = state.RequestedTarget,
                    ["currentTarget"] = currentTarget,
                    ["stableHitCount"] = state.StableHitCount,
                    ["isCompiling"] = UnityEditor.EditorApplication.isCompiling,
                    ["isUpdating"] = UnityEditor.EditorApplication.isUpdating,
                    ["elapsedMs"] = (long)(DateTime.UtcNow - state.StartedAtUtc).TotalMilliseconds
                }
            };
        }
    }
}
#endif
