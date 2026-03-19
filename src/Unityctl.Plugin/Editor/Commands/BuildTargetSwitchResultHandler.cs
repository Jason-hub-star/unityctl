#if UNITY_EDITOR
using System;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class BuildTargetSwitchResultHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.BuildTargetSwitchResult;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var requestId = request.GetParam("requestId", null);
            if (string.IsNullOrEmpty(requestId))
                return InvalidParameters("Missing required parameter: requestId");

            return BuildTransitionUtility.GetPollingResponse(
                requestId,
                "build target switch",
                "Active build target switched",
                (state, activeProfile, currentTarget) => new JObject
                {
                    ["previousTarget"] = state.PreviousProfile?["target"],
                    ["target"] = state.RequestedTarget,
                    ["previousProfile"] = state.PreviousProfile,
                    ["profile"] = activeProfile,
                    ["changed"] = state.Changed,
                    ["stabilized"] = true,
                    ["durationMs"] = (long)(DateTime.UtcNow - state.StartedAtUtc).TotalMilliseconds
                });
        }
    }
}
#endif
