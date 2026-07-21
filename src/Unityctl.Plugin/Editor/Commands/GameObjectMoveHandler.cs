using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class GameObjectMoveHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.GameObjectMove;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var id = request.GetParam("id", null);
            var parentId = request.GetParam("parent", null);

            if (string.IsNullOrEmpty(id))
                return InvalidParameters("Parameter 'id' is required.");
            if (string.IsNullOrEmpty(parentId))
                return InvalidParameters("Parameter 'parent' is required.");

            var go = GlobalObjectIdResolver.ResolveGameObject(id);
            if (go == null)
                return Fail(StatusCode.NotFound, $"GameObject not found: {id}");

            var newParent = GlobalObjectIdResolver.ResolveGameObject(parentId);
            if (newParent == null)
                return Fail(StatusCode.NotFound, $"Parent not found: {parentId}");

            // Prefab guard for both objects
            var prefabReject = PrefabGuard.RejectIfPrefab(go);
            if (prefabReject != null) return prefabReject;
            prefabReject = PrefabGuard.RejectIfPrefab(newParent);
            if (prefabReject != null) return prefabReject;

            // Same-scene guard
            if (go.scene != newParent.scene)
                return InvalidParameters(
                    "Cross-scene reparenting is not supported. Both objects must be in the same scene.");

            var undoName = $"unityctl: gameobject-move: {go.name} → {newParent.name}";

            using (new UndoScope(undoName))
            {
                UnityEditor.Undo.SetTransformParent(go.transform, newParent.transform, undoName);
                EditorSceneManager.MarkSceneDirty(go.scene);
            }

            return Ok($"Moved '{go.name}' under '{newParent.name}'", new JObject
            {
                ["globalObjectId"] = id,
                ["name"] = go.name,
                ["parentName"] = newParent.name,
                ["scenePath"] = go.scene.path,
                ["sceneDirty"] = true,
                ["undoGroupName"] = undoName
            });
#else
            return NotInEditor();
#endif
        }
    }
}
