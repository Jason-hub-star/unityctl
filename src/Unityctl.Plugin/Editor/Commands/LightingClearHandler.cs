#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class LightingClearHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.LightingClear;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            UnityEditor.Lightmapping.Clear();

            return Ok("Lightmap data cleared", new JObject
            {
                ["cleared"] = true
            });
        }
    }
}
#endif
