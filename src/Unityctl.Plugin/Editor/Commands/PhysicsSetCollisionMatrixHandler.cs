#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using UnityEngine;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class PhysicsSetCollisionMatrixHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.PhysicsSetCollisionMatrix;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var layer1Str = request.GetParam("layer1", null);
            var layer2Str = request.GetParam("layer2", null);
            var ignoreStr = request.GetParam("ignore", null);

            if (string.IsNullOrEmpty(layer1Str))
                return InvalidParameters("Missing required parameter: layer1");
            if (string.IsNullOrEmpty(layer2Str))
                return InvalidParameters("Missing required parameter: layer2");
            if (string.IsNullOrEmpty(ignoreStr))
                return InvalidParameters("Missing required parameter: ignore");

            if (!ResolveLayer(layer1Str, out var layer1Index, out var error1))
                return Fail(StatusCode.InvalidParameters, error1);
            if (!ResolveLayer(layer2Str, out var layer2Index, out var error2))
                return Fail(StatusCode.InvalidParameters, error2);
            if (!bool.TryParse(ignoreStr, out var ignore))
                return InvalidParameters($"Cannot parse '{ignoreStr}' as boolean");

            Physics.IgnoreLayerCollision(layer1Index, layer2Index, ignore);

            // Read back to confirm
            var confirmed = Physics.GetIgnoreLayerCollision(layer1Index, layer2Index);

            return Ok(
                ignore
                    ? $"Layer {layer1Index} and {layer2Index} collision disabled"
                    : $"Layer {layer1Index} and {layer2Index} collision enabled",
                new JObject
                {
                    ["layer1"] = layer1Index,
                    ["layer1Name"] = LayerMask.LayerToName(layer1Index) ?? "",
                    ["layer2"] = layer2Index,
                    ["layer2Name"] = LayerMask.LayerToName(layer2Index) ?? "",
                    ["ignore"] = ignore,
                    ["confirmed"] = confirmed == ignore,
                    ["undoSupported"] = false
                });
        }

        private static bool ResolveLayer(string input, out int index, out string error)
        {
            error = "";
            if (int.TryParse(input, out index))
            {
                if (index < 0 || index > 31)
                {
                    error = $"Layer index {index} is out of range (0-31)";
                    return false;
                }
                return true;
            }

            index = LayerMask.NameToLayer(input);
            if (index < 0)
            {
                error = $"Layer name '{input}' not found";
                return false;
            }
            return true;
        }
    }
}
#endif
