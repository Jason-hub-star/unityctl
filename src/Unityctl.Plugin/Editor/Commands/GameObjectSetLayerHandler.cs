using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class GameObjectSetLayerHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.GameObjectSetLayer;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var id = request.GetParam("id", null);
            if (string.IsNullOrEmpty(id))
                return InvalidParameters("Parameter 'id' is required.");

            var layerParam = request.GetParam("layer", null);
            if (string.IsNullOrEmpty(layerParam))
                return InvalidParameters("Parameter 'layer' is required.");

            int layerIndex;
            if (int.TryParse(layerParam, out layerIndex))
            {
                if (layerIndex < 0 || layerIndex > 31)
                    return InvalidParameters($"Layer index must be between 0 and 31, got {layerIndex}.");
            }
            else
            {
                layerIndex = LayerMask.NameToLayer(layerParam);
                if (layerIndex < 0)
                    return InvalidParameters($"Unknown layer name: '{layerParam}'");
            }

            var go = GlobalObjectIdResolver.Resolve<UnityEngine.GameObject>(id);
            if (go == null)
                return Fail(StatusCode.NotFound, $"GameObject not found: {id}");

            var prefabReject = PrefabGuard.RejectIfPrefab(go);
            if (prefabReject != null) return prefabReject;

            var layerName = LayerMask.LayerToName(layerIndex);
            var undoName = $"unityctl: gameobject-set-layer: {go.name} → {layerIndex} ({layerName})";

            using (new UndoScope(undoName))
            {
                UnityEditor.Undo.RecordObject(go, undoName);
                go.layer = layerIndex;
                EditorSceneManager.MarkSceneDirty(go.scene);
            }

            return Ok($"'{go.name}' layer = {layerIndex} ({layerName})", new JObject
            {
                ["globalObjectId"] = id,
                ["name"] = go.name,
                ["layer"] = layerIndex,
                ["layerName"] = layerName,
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
