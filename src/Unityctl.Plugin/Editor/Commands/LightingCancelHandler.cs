#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class LightingCancelHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.LightingCancel;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            if (!UnityEditor.Lightmapping.isRunning)
            {
                return Ok("No lightmap bake is currently running", new JObject
                {
                    ["wasRunning"] = false
                });
            }

            UnityEditor.Lightmapping.Cancel();

            return Ok("Lighting bake cancelled", new JObject
            {
                ["wasRunning"] = true,
                ["cancelled"] = true
            });
        }
    }
}
#endif
