using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class GameObjectCreateHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.GameObjectCreate;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var name = request.GetParam("name", null);
            if (string.IsNullOrEmpty(name))
                return InvalidParameters("Parameter 'name' is required.");

            var parentId = request.GetParam("parent", null);
            var scenePath = request.GetParam("scene", null);

            UnityEngine.Transform parentTransform = null;
            UnityEngine.SceneManagement.Scene targetScene;

            // Determine parent and scene
            if (!string.IsNullOrEmpty(parentId))
            {
                var parentGo = GlobalObjectIdResolver.ResolveGameObject(parentId);
                if (parentGo == null)
                    return Fail(StatusCode.NotFound, $"Parent not found: {parentId}");

                var prefabReject = PrefabGuard.RejectIfPrefab(parentGo);
                if (prefabReject != null) return prefabReject;

                parentTransform = parentGo.transform;
                targetScene = parentGo.scene;
            }
            else if (!string.IsNullOrEmpty(scenePath))
            {
                targetScene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
                if (!targetScene.IsValid() || !targetScene.isLoaded)
                    return Fail(StatusCode.NotFound, $"Scene not loaded: {scenePath}");
            }
            else
            {
                targetScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            }

            var undoName = $"unityctl: gameobject-create: {name}";
            using (new UndoScope(undoName))
            {
                var go = new UnityEngine.GameObject(name);
                UnityEditor.Undo.RegisterCreatedObjectUndo(go, undoName);

                if (parentTransform != null)
                {
                    UnityEditor.Undo.SetTransformParent(go.transform, parentTransform, undoName);
                }
                else
                {
                    // Move to target scene if not the active scene
                    var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                    if (targetScene != activeScene)
                    {
                        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, targetScene);
                    }
                }

                EditorSceneManager.MarkSceneDirty(go.scene);

                var globalId = GlobalObjectIdResolver.GetId(go);
                return Ok($"Created '{name}'", new JObject
                {
                    ["globalObjectId"] = globalId,
                    ["name"] = go.name,
                    ["scenePath"] = go.scene.path,
                    ["sceneDirty"] = true,
                    ["undoGroupName"] = undoName
                });
            }
#else
            return NotInEditor();
#endif
        }
    }
}
