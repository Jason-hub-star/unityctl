using System;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class PrefabInstantiateHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.PrefabInstantiate;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var path = request.GetParam("path", null);
            if (string.IsNullOrEmpty(path))
                return InvalidParameters("Parameter 'path' is required (prefab asset path).");

            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
            if (prefab == null)
                return Fail(StatusCode.NotFound, $"Prefab not found at: {path}");

            var name = request.GetParam("name", prefab.name);
            var parentId = request.GetParam("parent", null);
            var positionStr = request.GetParam("position", null);
            var rotationStr = request.GetParam("rotation", null);
            var scaleStr = request.GetParam("scale", null);

            var undoName = $"unityctl: prefab-instantiate: {name}";
            UnityEditor.Undo.IncrementCurrentGroup();
            UnityEditor.Undo.SetCurrentGroupName(undoName);

            UnityEngine.Transform parentTransform = null;
            if (!string.IsNullOrEmpty(parentId))
            {
                var parentGo = GlobalObjectIdResolver.Resolve<UnityEngine.GameObject>(parentId);
                if (parentGo == null)
                    return Fail(StatusCode.NotFound, $"Parent not found: {parentId}");

                var guard = PrefabGuard.RejectIfPrefab(parentGo);
                if (guard != null) return guard;

                parentTransform = parentGo.transform;
            }

            var instance = (UnityEngine.GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(
                prefab, parentTransform);

            if (instance == null)
                return Fail(StatusCode.UnknownError, "PrefabUtility.InstantiatePrefab returned null.");

            UnityEditor.Undo.RegisterCreatedObjectUndo(instance, undoName);
            instance.name = name;

            if (positionStr != null)
            {
                var pos = ParseVector3(positionStr);
                if (pos.HasValue) instance.transform.position = pos.Value;
            }

            if (rotationStr != null)
            {
                var rot = ParseQuaternion(rotationStr);
                if (rot.HasValue) instance.transform.rotation = rot.Value;
            }

            if (scaleStr != null)
            {
                var scale = ParseVector3(scaleStr);
                if (scale.HasValue) instance.transform.localScale = scale.Value;
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(instance.scene);

            var globalId = GlobalObjectIdResolver.GetId(instance);
            var t = instance.transform;

            return Ok($"Instantiated prefab '{name}' from {path}", new JObject
            {
                ["globalObjectId"] = globalId,
                ["name"] = instance.name,
                ["prefabPath"] = path,
                ["position"] = FormatVector3(t.position),
                ["rotation"] = FormatVector3(t.eulerAngles),
                ["scale"] = FormatVector3(t.localScale),
                ["scenePath"] = instance.scene.path,
                ["sceneDirty"] = true,
                ["undoGroupName"] = undoName
            });
#else
            return NotInEditor();
#endif
        }

#if UNITY_EDITOR
        private static UnityEngine.Vector3? ParseVector3(string json)
        {
            try
            {
                var arr = JArray.Parse(json);
                if (arr.Count >= 3)
                    return new UnityEngine.Vector3(arr[0].Value<float>(), arr[1].Value<float>(), arr[2].Value<float>());
            }
            catch { }
            return null;
        }

        private static UnityEngine.Quaternion? ParseQuaternion(string json)
        {
            try
            {
                var arr = JArray.Parse(json);
                if (arr.Count >= 3)
                    return UnityEngine.Quaternion.Euler(arr[0].Value<float>(), arr[1].Value<float>(), arr[2].Value<float>());
            }
            catch { }
            return null;
        }

        private static string FormatVector3(UnityEngine.Vector3 v)
        {
            return $"[{v.x:G},{v.y:G},{v.z:G}]";
        }
#endif
    }
}
