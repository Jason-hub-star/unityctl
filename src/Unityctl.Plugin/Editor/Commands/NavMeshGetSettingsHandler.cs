#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class NavMeshGetSettingsHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.NavMeshGetSettings;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var count = UnityEngine.AI.NavMesh.GetSettingsCount();
            var agents = new JArray();

            for (var i = 0; i < count; i++)
            {
                var settings = UnityEngine.AI.NavMesh.GetSettingsByIndex(i);
                agents.Add(new JObject
                {
                    ["agentTypeID"] = settings.agentTypeID,
                    ["agentTypeName"] = UnityEngine.AI.NavMesh.GetSettingsNameFromID(settings.agentTypeID),
                    ["agentRadius"] = settings.agentRadius,
                    ["agentHeight"] = settings.agentHeight,
                    ["agentSlope"] = settings.agentSlope,
                    ["agentClimb"] = settings.agentClimb,
                    ["minRegionArea"] = settings.minRegionArea,
                    ["overrideTileSize"] = settings.overrideTileSize,
                    ["tileSize"] = settings.tileSize,
                    ["overrideVoxelSize"] = settings.overrideVoxelSize,
                    ["voxelSize"] = settings.voxelSize
                });
            }

            return Ok($"NavMesh settings: {count} agent type(s)", new JObject
            {
                ["count"] = count,
                ["agents"] = agents
            });
        }
    }
}
#endif
