using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class SceneHierarchyHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.SceneHierarchy;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var scenePath = request.GetParam("scenePath", null);
            var includeInactive = request.GetParam<bool>("includeInactive");

            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var sceneSetup = SceneExplorationUtility.BuildSceneSetup(activeScene);
            var scenes = new JArray();

            var sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
            for (var i = 0; i < sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(scenePath)
                    && !string.Equals(scene.path, scenePath, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                scenes.Add(new JObject
                {
                    ["path"] = scene.path,
                    ["name"] = scene.name,
                    ["isActive"] = scene.path == activeScene.path,
                    ["isDirty"] = scene.isDirty,
                    ["rootCount"] = scene.rootCount,
                    ["roots"] = SceneExplorationUtility.BuildHierarchyRoots(scene, includeInactive)
                });
            }

            return Ok("Scene hierarchy captured", new JObject
            {
                ["sceneSetup"] = sceneSetup,
                ["scenes"] = scenes
            });
#else
            return NotInEditor();
#endif
        }
    }
}
