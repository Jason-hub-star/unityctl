using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class SceneCreateHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.SceneCreate;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var path = request.GetParam("path", null);
            var template = request.GetParam("template", "default");
            var mode = request.GetParam("mode", "single");
            var dirtyPolicy = ResolveDirtyPolicy(request);
            var force = request.GetParam<bool>("force");
            var saveCurrentModified = request.GetParam<bool>("saveCurrentModified");

            if (string.IsNullOrWhiteSpace(path))
                return InvalidParameters("Parameter 'path' is required.");

            if (!path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                return InvalidParameters("Scene path must end with '.unity'.");

            var directory = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(directory))
                return InvalidParameters("Scene path must include a folder under Assets/ (e.g. Assets/Scenes/Game.unity).");
            if (!directory.Equals("Assets", StringComparison.Ordinal)
                && !directory.StartsWith("Assets/", StringComparison.Ordinal))
                return InvalidParameters($"Scene directory must be under Assets/: {directory}");
            // mkdir -p: agents organize scenes into folders, so create missing parents
            // instead of failing (Unity's AssetDatabase requires each segment to exist).
            if (!UnityEditor.AssetDatabase.IsValidFolder(directory))
                EnsureAssetFolder(directory);

            if (UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(path) != null)
                return InvalidParameters($"Scene already exists: {path}");

            if (!TryParseSceneSetup(template, out var setup))
                return InvalidParameters($"Invalid template '{template}'. Must be 'default' or 'empty'.");

            if (!TryParseNewSceneMode(mode, out var newSceneMode))
                return InvalidParameters($"Invalid mode '{mode}'. Must be 'single' or 'additive'.");

            if (newSceneMode == NewSceneMode.Single)
            {
                var dirtyScenes = GetDirtyLoadedScenePaths();
                if (dirtyScenes.Count > 0)
                {
                    if (dirtyPolicy == "save" || saveCurrentModified)
                    {
                        if (!EditorSceneManager.SaveOpenScenes())
                            return Fail(StatusCode.UnknownError, "Failed to save dirty scenes before creating a new scene.");
                    }
                    else if (dirtyPolicy == "discard" || force)
                    {
                        // Explicit discard; NewScene(single) replaces current setup.
                    }
                    else
                    {
                        return Fail(
                            StatusCode.InvalidParameters,
                            "Dirty loaded scenes exist. Retry with dirtyPolicy=save or dirtyPolicy=discard.",
                            new JObject
                            {
                                ["dirtyScenes"] = JArray.FromObject(dirtyScenes),
                                ["dirtyPolicy"] = dirtyPolicy,
                                ["retryExamples"] = new JArray
                                {
                                    "scene create --dirty-policy save",
                                    "scene create --dirty-policy discard"
                                }
                            });
                    }
                }
            }

            var scene = EditorSceneManager.NewScene(setup, newSceneMode);
            if (!scene.IsValid() || !scene.isLoaded)
                return Fail(StatusCode.UnknownError, "Failed to create a new scene.");

            if (!EditorSceneManager.SaveScene(scene, path))
                return Fail(StatusCode.UnknownError, $"Failed to save new scene: {path}");

            return Ok($"Created scene '{path}'", new JObject
            {
                ["scenePath"] = path,
                ["sceneName"] = scene.name,
                ["template"] = template,
                ["mode"] = mode,
                ["dirtyPolicy"] = dirtyPolicy,
                ["isLoaded"] = scene.isLoaded,
                ["isActive"] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == scene.path
            });
#else
            return NotInEditor();
#endif
        }

#if UNITY_EDITOR
        private static void EnsureAssetFolder(string folder)
        {
            // folder like "Assets/VampireSurvivors/Scenes" — create each missing segment.
            var parts = folder.Split('/');
            var current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i])) continue;
                var next = current + "/" + parts[i];
                if (!UnityEditor.AssetDatabase.IsValidFolder(next))
                    UnityEditor.AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static bool TryParseSceneSetup(string? template, out NewSceneSetup setup)
        {
            if (string.Equals(template, "empty", StringComparison.OrdinalIgnoreCase))
            {
                setup = NewSceneSetup.EmptyScene;
                return true;
            }

            if (string.IsNullOrWhiteSpace(template) || string.Equals(template, "default", StringComparison.OrdinalIgnoreCase))
            {
                setup = NewSceneSetup.DefaultGameObjects;
                return true;
            }

            setup = NewSceneSetup.DefaultGameObjects;
            return false;
        }

        private static bool TryParseNewSceneMode(string? mode, out NewSceneMode newSceneMode)
        {
            if (string.Equals(mode, "additive", StringComparison.OrdinalIgnoreCase))
            {
                newSceneMode = NewSceneMode.Additive;
                return true;
            }

            if (string.IsNullOrWhiteSpace(mode) || string.Equals(mode, "single", StringComparison.OrdinalIgnoreCase))
            {
                newSceneMode = NewSceneMode.Single;
                return true;
            }

            newSceneMode = NewSceneMode.Single;
            return false;
        }

        private static string ResolveDirtyPolicy(CommandRequest request)
        {
            var dirtyPolicy = request.GetParam("dirtyPolicy", null);
            if (!string.IsNullOrWhiteSpace(dirtyPolicy))
                return dirtyPolicy.Trim().ToLowerInvariant();
            if (request.GetParam<bool>("saveCurrentModified"))
                return "save";
            if (request.GetParam<bool>("force"))
                return "discard";
            return "fail";
        }

        private static List<string> GetDirtyLoadedScenePaths()
        {
            var dirtyScenes = new List<string>();
            var sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (scene.isLoaded && scene.isDirty)
                    dirtyScenes.Add(scene.path);
            }

            return dirtyScenes;
        }
#endif
    }
}
