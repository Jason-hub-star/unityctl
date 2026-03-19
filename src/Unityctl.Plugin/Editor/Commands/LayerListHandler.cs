using Newtonsoft.Json.Linq;
using UnityEngine;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class LayerListHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.LayerList;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var layers = new JArray();
            for (var i = 0; i < 32; i++)
            {
                var layerName = LayerMask.LayerToName(i);
                layers.Add(new JObject
                {
                    ["index"] = i,
                    ["name"] = layerName,
                    ["builtIn"] = i < 8
                });
            }

            return Ok("Listed 32 layer slots", new JObject
            {
                ["layers"] = layers,
                ["count"] = 32
            });
#else
            return NotInEditor();
#endif
        }
    }
}
