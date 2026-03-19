#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class BuildProfileGetActiveHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.BuildProfileGetActive;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            BuildTransitionStateStore.PruneDefault();

            var profile = BuildProfileUtility.GetActiveProfileSummary();
            if (profile == null)
                return Fail(StatusCode.NotFound, "No active build profile could be determined.");

            return Ok("Active build profile captured", new JObject
            {
                ["profile"] = profile
            });
        }
    }
}
#endif
