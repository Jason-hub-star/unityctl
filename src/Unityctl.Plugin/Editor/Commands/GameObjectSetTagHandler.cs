using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class GameObjectSetTagHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.GameObjectSetTag;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var id = request.GetParam("id", null);
            if (string.IsNullOrEmpty(id))
                return InvalidParameters("Parameter 'id' is required.");

            var tag = request.GetParam("tag", null);
            if (string.IsNullOrEmpty(tag))
                return InvalidParameters("Parameter 'tag' is required.");

            var go = GlobalObjectIdResolver.Resolve<UnityEngine.GameObject>(id);
            if (go == null)
                return Fail(StatusCode.NotFound, $"GameObject not found: {id}");

            var prefabReject = PrefabGuard.RejectIfPrefab(go);
            if (prefabReject != null) return prefabReject;

            var undoName = $"unityctl: gameobject-set-tag: {go.name} → {tag}";

            using (new UndoScope(undoName))
            {
                UnityEditor.Undo.RecordObject(go, undoName);
                go.tag = tag;
                EditorSceneManager.MarkSceneDirty(go.scene);
            }

            return Ok($"'{go.name}' tag = {tag}", new JObject
            {
                ["globalObjectId"] = id,
                ["name"] = go.name,
                ["tag"] = tag,
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
