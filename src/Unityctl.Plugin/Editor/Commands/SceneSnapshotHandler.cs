using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class SceneSnapshotHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.SceneSnapshot;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var scenePath = request.GetParam("scenePath", null);
            var includeInactive = request.GetParam<bool>("includeInactive");

            var allObjects = new List<UnityEngine.Object>();
            var pendingIds = new List<(JObject entry, string field)>();
            var explorationContext = new SceneExplorationUtility.ExplorationContext(allObjects, pendingIds);

            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var sceneSetup = SceneExplorationUtility.BuildSceneSetup(activeScene);
            var scenes = new JArray();

            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                if (scenePath != null && scene.path != scenePath) continue;

                var gameObjectsArray = new JArray();
                var rootObjects = scene.GetRootGameObjects();

                foreach (var root in rootObjects)
                {
                    SceneExplorationUtility.TraverseGameObject(
                        root,
                        string.Empty,
                        gameObjectsArray,
                        includeInactive,
                        includeProperties: true,
                        explorationContext);
                }

                scenes.Add(new JObject
                {
                    ["path"] = scene.path,
                    ["name"] = scene.name,
                    ["isDirty"] = scene.isDirty,
                    ["gameObjects"] = gameObjectsArray
                });
            }

            SceneExplorationUtility.PopulateGlobalObjectIds(explorationContext);

            var data = new JObject
            {
                ["timestamp"] = DateTime.UtcNow.ToString("o"),
                ["unityVersion"] = UnityEngine.Application.unityVersion,
                ["projectPath"] = UnityEngine.Application.dataPath.Replace("/Assets", string.Empty),
                ["sceneSetup"] = sceneSetup,
                ["scenes"] = scenes
            };

            return Ok("Scene snapshot captured", data);
#else
            return NotInEditor();
#endif
        }

        protected override CommandResponse HandleException(Exception exception)
        {
            return Fail(StatusCode.UnknownError, $"Scene snapshot failed: {exception.Message}",
                errors: GetStackTrace(exception));
        }
    }
}
