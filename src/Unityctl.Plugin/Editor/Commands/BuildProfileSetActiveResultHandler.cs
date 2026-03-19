#if UNITY_EDITOR
using System;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class BuildProfileSetActiveResultHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.BuildProfileSetActiveResult;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var requestId = request.GetParam("requestId", null);
            if (string.IsNullOrEmpty(requestId))
                return InvalidParameters("Missing required parameter: requestId");

            return BuildTransitionUtility.GetPollingResponse(
                requestId,
                "build profile switch",
                "Active build profile updated",
                (state, activeProfile, currentTarget) => new JObject
                {
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
