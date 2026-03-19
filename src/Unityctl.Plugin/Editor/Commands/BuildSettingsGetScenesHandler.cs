using System.Linq;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class BuildSettingsGetScenesHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.BuildSettingsGetScenes;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var scenes = UnityEditor.EditorBuildSettings.scenes
                .Select((scene, index) => new JObject
                {
                    ["order"] = index,
                    ["path"] = scene.path,
                    ["enabled"] = scene.enabled
                })
                .ToArray();

            var sceneArray = new JArray();
            foreach (var scene in scenes)
            {
                sceneArray.Add(scene);
            }

            return Ok("Build Settings scenes captured", new JObject
            {
                ["totalCount"] = scenes.Length,
                ["enabledCount"] = scenes.Count(scene => scene["enabled"]?.Value<bool>() == true),
                ["scenes"] = sceneArray
            });
#else
            return NotInEditor();
#endif
        }
    }
}
