#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class BuildProfileSetActiveHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.BuildProfileSetActive;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            if (UnityEngine.Application.isBatchMode)
            {
                return Fail(StatusCode.InvalidParameters,
                    "build-profile-set-active is IPC-only. Unity 공식 문서 기준 BuildProfile.SetActiveBuildProfile is not reliable in batch mode.");
            }

            var profileRef = request.GetParam("profile", null);
            if (string.IsNullOrWhiteSpace(profileRef))
                return InvalidParameters("Parameter 'profile' is required.");

            var previousProfile = BuildProfileUtility.GetActiveProfileSummary();
            var previousProfileId = previousProfile?["id"]?.Value<string>();
            var changed = !string.Equals(previousProfileId, profileRef, System.StringComparison.Ordinal);
            var stableNow = !UnityEditor.EditorApplication.isCompiling && !UnityEditor.EditorApplication.isUpdating;

            string requestedTarget;
            if (profileRef.StartsWith("platform:", System.StringComparison.OrdinalIgnoreCase))
            {
                var targetName = profileRef.Substring("platform:".Length);
                if (!BuildProfileUtility.TryParseBuildTarget(targetName, out var _, out requestedTarget))
                {
                    return InvalidParameters(
                        $"Unknown platform profile ref: {profileRef}. Valid targets: {BuildProfileUtility.SupportedTargetList}");
                }
            }
            else
            {
                var asset = BuildProfileUtility.LoadBuildProfileAsset(profileRef);
                if (asset == null)
                    return Fail(StatusCode.NotFound, $"BuildProfile asset not found at: {profileRef}");

                var serializedObject = new UnityEditor.SerializedObject(asset);
                var targetProperty = serializedObject.FindProperty("m_BuildTarget");
                requestedTarget = targetProperty == null
                    ? BuildProfileUtility.ToCanonicalName(UnityEditor.EditorUserBuildSettings.activeBuildTarget)
                    : BuildProfileUtility.ToCanonicalName((UnityEditor.BuildTarget)targetProperty.intValue);
            }

            if (!changed && stableNow)
            {
                var profile = BuildProfileUtility.GetActiveProfileSummary();
                return Ok("Active build profile already selected", new JObject
                {
                    ["previousProfile"] = previousProfile,
                    ["profile"] = profile,
                    ["changed"] = false,
                    ["stabilized"] = true,
                    ["durationMs"] = 0
                });
            }

            BuildTransitionStateStore.CreateRunning(
                request.requestId,
                CommandName,
                requestedTarget,
                profileRef,
                changed,
                previousProfile);
            AsyncOperationRegistry.Register(request.requestId, CommandName);

            CommandResponse failure = null;
            try
            {
                if (profileRef.StartsWith("platform:", System.StringComparison.OrdinalIgnoreCase))
                {
                    var targetName = profileRef.Substring("platform:".Length);
                    BuildProfileUtility.TryParseBuildTarget(targetName, out var target, out var canonicalTarget);
                    UnityEngine.Debug.Log($"[unityctl] Starting build profile switch via platform target {canonicalTarget} (requestId={request.requestId})");
                    if (!BuildProfileUtility.TrySwitchActiveBuildTarget(target, out var error))
                    {
                        failure = Fail(StatusCode.BuildFailed,
                            error ?? $"Failed to switch active build target to {canonicalTarget}.");
                    }
                    else
                    {
                        UnityEngine.Debug.Log($"[unityctl] Build profile platform switch requested successfully for {canonicalTarget} (requestId={request.requestId})");
                    }
                }
                else
                {
                    var asset = BuildProfileUtility.LoadBuildProfileAsset(profileRef);
                    if (asset == null)
                    {
                        failure = Fail(StatusCode.NotFound, $"BuildProfile asset not found at: {profileRef}");
                    }
                    else
                    {
                        UnityEngine.Debug.Log($"[unityctl] Starting build profile switch to {profileRef} (requestId={request.requestId})");
                        if (!BuildProfileUtility.TrySetActiveBuildProfile(asset, out var error))
                        {
                            failure = Fail(StatusCode.BuildFailed, error ?? $"Failed to set active build profile: {profileRef}");
                        }
                        else
                        {
                            UnityEngine.Debug.Log($"[unityctl] Build profile switch requested successfully for {profileRef} (requestId={request.requestId})");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                failure = Fail(StatusCode.BuildFailed,
                    $"Build profile switch failed: {ex.Message}",
                    errors: GetStackTrace(ex));
            }

            if (failure != null)
            {
                return BuildTransitionUtility.CompleteFailure(
                    request.requestId,
                    $"Build profile switch failed for {profileRef}",
                    failure);
            }

            return Ok(StatusCode.Accepted,
                "Build profile switch started. Poll 'build-profile-set-active-result' with requestId.",
                new JObject
                {
                    ["requestId"] = request.requestId,
                    ["profile"] = profileRef,
                    ["changed"] = changed
                });
        }
    }
}
#endif
