#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class LightingBakeResultHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.LightingBakeResult;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var requestId = request.GetParam("requestId", null);
            if (string.IsNullOrEmpty(requestId))
                return InvalidParameters("Missing required parameter: requestId");

            if (UnityEditor.Lightmapping.isRunning)
            {
                return Ok(StatusCode.Accepted,
                    "Lighting bake in progress",
                    new JObject
                    {
                        ["requestId"] = requestId,
                        ["progress"] = UnityEditor.Lightmapping.buildProgress
                    });
            }

            return Ok("Lighting bake completed", new JObject
            {
                ["requestId"] = requestId,
                ["progress"] = 1f,
                ["completed"] = true
            });
        }
    }
}
#endif
