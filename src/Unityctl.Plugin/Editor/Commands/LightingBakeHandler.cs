#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class LightingBakeHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.LightingBake;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            if (UnityEngine.Application.isBatchMode)
            {
                return Fail(StatusCode.InvalidParameters,
                    "lighting-bake is IPC-only. Lightmap baking requires the Editor UI.");
            }

            if (UnityEditor.Lightmapping.isRunning)
            {
                return Fail(StatusCode.Busy,
                    "A lightmap bake is already in progress.");
            }

            UnityEditor.Lightmapping.BakeAsync();

            return Ok(StatusCode.Accepted,
                "Lighting bake started. Poll 'lighting-bake-result' with requestId.",
                new JObject
                {
                    ["requestId"] = request.requestId,
                    ["progress"] = 0f
                });
        }
    }
}
#endif
