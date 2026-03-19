using System;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class GameObjectFindHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.GameObjectFind;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var name = request.GetParam("name", null);
            var tag = request.GetParam("tag", null);
            var layer = request.GetParam("layer", null);
            var component = request.GetParam("component", null);
            var scene = request.GetParam("scene", null);
            var includeInactive = request.GetParam<bool>("includeInactive");
            var limit = request.GetParam<int>("limit");

            int? layerIndex = null;
            if (!string.IsNullOrWhiteSpace(layer))
            {
                if (int.TryParse(layer, out var parsedLayer))
                {
                    layerIndex = parsedLayer;
                }
                else
                {
                    parsedLayer = UnityEngine.LayerMask.NameToLayer(layer);
                    if (parsedLayer < 0)
                    {
                        return InvalidParameters($"Unknown layer: '{layer}'.");
                    }

                    layerIndex = parsedLayer;
                }
            }

            var results = new JArray();
            var sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
            for (var i = 0; i < sceneCount; i++)
            {
                var loadedScene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!loadedScene.isLoaded)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(scene)
                    && !string.Equals(loadedScene.path, scene, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                foreach (var root in loadedScene.GetRootGameObjects())
                {
                    if (Traverse(root, string.Empty, results, name, tag, layerIndex, component, includeInactive, limit))
                    {
                        return Ok($"Found {results.Count} GameObject(s)", new JObject
                        {
                            ["results"] = results
                        });
                    }
                }
            }

            return Ok($"Found {results.Count} GameObject(s)", new JObject
            {
                ["results"] = results
            });
#else
            return NotInEditor();
#endif
        }

#if UNITY_EDITOR
        private static bool Traverse(
            UnityEngine.GameObject gameObject,
            string parentPath,
            JArray results,
            string name,
            string tag,
            int? layerIndex,
            string component,
            bool includeInactive,
            int limit)
        {
            if (!includeInactive && !gameObject.activeSelf)
            {
                return false;
            }

            var hierarchyPath = SceneExplorationUtility.GetHierarchyPath(gameObject, parentPath);
            if (Matches(gameObject, hierarchyPath, name, tag, layerIndex, component))
            {
                results.Add(SceneExplorationUtility.CreateGameObjectSummary(gameObject, hierarchyPath));
                if (limit > 0 && results.Count >= limit)
                {
                    return true;
                }
            }

            for (var i = 0; i < gameObject.transform.childCount; i++)
            {
                if (Traverse(
                    gameObject.transform.GetChild(i).gameObject,
                    hierarchyPath,
                    results,
                    name,
                    tag,
                    layerIndex,
                    component,
                    includeInactive,
                    limit))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool Matches(
            UnityEngine.GameObject gameObject,
            string hierarchyPath,
            string name,
            string tag,
            int? layerIndex,
            string component)
        {
            if (!string.IsNullOrWhiteSpace(name)
                && gameObject.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(tag)
                && !string.Equals(gameObject.tag, tag, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (layerIndex.HasValue && gameObject.layer != layerIndex.Value)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(component))
            {
                var matched = false;
                foreach (var candidate in gameObject.GetComponents<UnityEngine.Component>())
                {
                    if (candidate == null)
                    {
                        continue;
                    }

                    var type = candidate.GetType();
                    if (string.Equals(type.Name, component, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(type.FullName, component, StringComparison.OrdinalIgnoreCase))
                    {
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    return false;
                }
            }

            return !string.IsNullOrWhiteSpace(hierarchyPath);
        }
#endif
    }
}
