using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class GameObjectSetActiveHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.GameObjectSetActive;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var id = request.GetParam("id", null);
            if (string.IsNullOrEmpty(id))
                return InvalidParameters("Parameter 'id' is required.");

            var active = request.GetParam<bool>("active");

            var go = GlobalObjectIdResolver.ResolveGameObject(id);
            if (go == null)
                return Fail(StatusCode.NotFound, $"GameObject not found: {id}");

            var prefabReject = PrefabGuard.RejectIfPrefab(go);
            if (prefabReject != null) return prefabReject;

            var undoName = $"unityctl: gameobject-set-active: {go.name} → {active}";

            using (new UndoScope(undoName))
            {
                UnityEditor.Undo.RecordObject(go, undoName);
                go.SetActive(active);
                EditorSceneManager.MarkSceneDirty(go.scene);
            }

            return Ok($"'{go.name}' active = {active}", new JObject
            {
                ["globalObjectId"] = id,
                ["name"] = go.name,
                ["active"] = active,
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
