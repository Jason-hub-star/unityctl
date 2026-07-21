using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class GameObjectRenameHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.GameObjectRename;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var id = request.GetParam("id", null);
            var newName = request.GetParam("name", null);

            if (string.IsNullOrEmpty(id))
                return InvalidParameters("Parameter 'id' is required.");
            if (string.IsNullOrEmpty(newName))
                return InvalidParameters("Parameter 'name' is required.");

            var go = GlobalObjectIdResolver.ResolveGameObject(id);
            if (go == null)
                return Fail(StatusCode.NotFound, $"GameObject not found: {id}");

            var prefabReject = PrefabGuard.RejectIfPrefab(go);
            if (prefabReject != null) return prefabReject;

            var oldName = go.name;
            var undoName = $"unityctl: gameobject-rename: {oldName} → {newName}";

            using (new UndoScope(undoName))
            {
                UnityEditor.Undo.RecordObject(go, undoName);
                go.name = newName;
                EditorSceneManager.MarkSceneDirty(go.scene);
            }

            return Ok($"Renamed '{oldName}' → '{newName}'", new JObject
            {
                ["globalObjectId"] = id,
                ["oldName"] = oldName,
                ["newName"] = newName,
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
