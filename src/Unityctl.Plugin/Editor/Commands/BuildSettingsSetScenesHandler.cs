using System;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class BuildSettingsSetScenesHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.BuildSettingsSetScenes;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var scenesParam = request.GetParam("scenes", null);
            if (string.IsNullOrEmpty(scenesParam))
            {
                return InvalidParameters("Parameter 'scenes' is required.");
            }

            var scenePaths = scenesParam.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < scenePaths.Length; i++)
            {
                scenePaths[i] = scenePaths[i].Trim();
            }

            // Validate each scene path
            for (int i = 0; i < scenePaths.Length; i++)
            {
                var scenePath = scenePaths[i];
                var sceneAsset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(scenePath);
                if (!(sceneAsset is UnityEngine.SceneManagement.Scene) &&
                    sceneAsset != null && sceneAsset.GetType().Name != "SceneAsset")
                {
                    // More robust check: verify it's a .unity file
                    if (!scenePath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                    {
                        return Fail(StatusCode.InvalidParameters,
                            $"Path '{scenePath}' does not appear to be a scene file (.unity).");
                    }
                }

                if (sceneAsset == null)
                {
                    return Fail(StatusCode.NotFound,
                        $"Scene asset not found at: {scenePath}");
                }
            }

            var newScenes = new UnityEditor.EditorBuildSettingsScene[scenePaths.Length];
            for (int i = 0; i < scenePaths.Length; i++)
            {
                newScenes[i] = new UnityEditor.EditorBuildSettingsScene(scenePaths[i], true);
            }

            UnityEditor.EditorBuildSettings.scenes = newScenes;

            var scenesArray = new JArray();
            for (int i = 0; i < newScenes.Length; i++)
            {
                scenesArray.Add(new JObject
                {
                    ["order"] = i,
                    ["path"] = newScenes[i].path,
                    ["enabled"] = newScenes[i].enabled
                });
            }

            return Ok("Build Settings scenes updated", new JObject
            {
                ["totalCount"] = newScenes.Length,
                ["scenes"] = scenesArray
            });
#else
            return NotInEditor();
#endif
        }
    }
}
