using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class GameObjectDeleteHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.GameObjectDelete;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var id = request.GetParam("id", null);
            if (string.IsNullOrEmpty(id))
                return InvalidParameters("Parameter 'id' is required.");

            var go = GlobalObjectIdResolver.ResolveGameObject(id);
            if (go == null)
                return Fail(StatusCode.NotFound, $"GameObject not found: {id}");

            var prefabReject = PrefabGuard.RejectIfPrefab(go);
            if (prefabReject != null) return prefabReject;

            var goName = go.name;
            var scenePath = go.scene.path;
            var undoName = $"unityctl: gameobject-delete: {goName}";

            using (new UndoScope(undoName))
            {
                UnityEditor.Undo.DestroyObjectImmediate(go);
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
                if (scene.IsValid())
                    EditorSceneManager.MarkSceneDirty(scene);
            }

            return Ok($"Deleted '{goName}'", new JObject
            {
                ["deletedName"] = goName,
                ["scenePath"] = scenePath,
                ["sceneDirty"] = true,
                ["undoGroupName"] = undoName
            });
#else
            return NotInEditor();
#endif
        }
    }
}
