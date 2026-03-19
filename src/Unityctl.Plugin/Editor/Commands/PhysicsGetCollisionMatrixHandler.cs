#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using UnityEngine;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class PhysicsGetCollisionMatrixHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.PhysicsGetCollisionMatrix;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var layers = new JArray();
            var ignoredPairs = new JArray();

            for (int i = 0; i < 32; i++)
            {
                var layerName = LayerMask.LayerToName(i);
                var collidesWith = new JArray();

                for (int j = 0; j < 32; j++)
                {
                    if (!Physics.GetIgnoreLayerCollision(i, j))
                    {
                        collidesWith.Add(j);
                    }
                }

                layers.Add(new JObject
                {
                    ["index"] = i,
                    ["name"] = layerName ?? "",
                    ["collidesWith"] = collidesWith
                });
            }

            // Collect ignored pairs (upper triangle only to avoid duplicates)
            for (int i = 0; i < 32; i++)
            {
                for (int j = i; j < 32; j++)
                {
                    if (Physics.GetIgnoreLayerCollision(i, j))
                    {
                        ignoredPairs.Add(new JObject
                        {
                            ["layer1"] = i,
                            ["layer1Name"] = LayerMask.LayerToName(i) ?? "",
                            ["layer2"] = j,
                            ["layer2Name"] = LayerMask.LayerToName(j) ?? ""
                        });
                    }
                }
            }

            return Ok("Collision matrix retrieved", new JObject
            {
                ["layers"] = layers,
                ["ignoredPairs"] = ignoredPairs,
                ["totalLayers"] = 32
            });
        }
    }
}
#endif
