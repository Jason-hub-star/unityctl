#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class BuildTargetSwitchHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.BuildTargetSwitch;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            if (UnityEngine.Application.isBatchMode)
            {
                return Fail(StatusCode.InvalidParameters,
                    "build-target-switch is IPC-only. Unity 공식 문서 기준 target switching APIs are not reliable in batch mode.");
            }

            var targetName = request.GetParam("target", null);
            if (!BuildProfileUtility.TryParseBuildTarget(targetName, out var target, out var canonicalTarget))
            {
                return InvalidParameters(
                    $"Unknown build target: {targetName}. Valid targets: {BuildProfileUtility.SupportedTargetList}");
            }

            var activeProfile = BuildProfileUtility.GetActiveProfileSummary();
            var activeTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            var previousTarget = BuildProfileUtility.ToCanonicalName(activeTarget);
            var changed = activeTarget != target;
            var stableNow = !UnityEditor.EditorApplication.isCompiling && !UnityEditor.EditorApplication.isUpdating;

            if (!changed && stableNow)
            {
                var profile = BuildProfileUtility.GetActiveProfileSummary();
                return Ok("Active build target already selected", new JObject
                {
                    ["previousTarget"] = previousTarget,
                    ["target"] = canonicalTarget,
                    ["previousProfile"] = activeProfile,
                    ["profile"] = profile,
                    ["changed"] = false,
                    ["stabilized"] = true,
                    ["durationMs"] = 0
                });
            }

            BuildTransitionStateStore.CreateRunning(
                request.requestId,
                CommandName,
                canonicalTarget,
                BuildProfileUtility.GetPlatformProfileId(canonicalTarget),
                changed,
                activeProfile);
            AsyncOperationRegistry.Register(request.requestId, CommandName);

            CommandResponse failure = null;
            try
            {
                UnityEngine.Debug.Log($"[unityctl] Starting build target switch to {canonicalTarget} (requestId={request.requestId})");
                if (!BuildProfileUtility.TrySwitchActiveBuildTarget(target, out var error))
                {
                    failure = Fail(StatusCode.BuildFailed,
                        error ?? $"Failed to switch active build target to {canonicalTarget}.");
                }
                else
                {
                    UnityEngine.Debug.Log($"[unityctl] Build target switch requested successfully for {canonicalTarget} (requestId={request.requestId})");
                }
            }
            catch (System.Exception ex)
            {
                failure = Fail(StatusCode.BuildFailed,
                    $"Build target switch failed: {ex.Message}",
                    errors: GetStackTrace(ex));
            }

            if (failure != null)
            {
                return BuildTransitionUtility.CompleteFailure(
                    request.requestId,
                    $"Build target switch failed for {canonicalTarget}",
                    failure);
            }

            return Ok(StatusCode.Accepted,
                "Build target switch started. Poll 'build-target-switch-result' with requestId.",
                new JObject
                {
                    ["requestId"] = request.requestId,
                    ["target"] = canonicalTarget,
                    ["changed"] = changed
                });
        }
    }
}
#endif
