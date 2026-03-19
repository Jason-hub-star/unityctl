using System;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class MeshCreatePrimitiveHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.MeshCreatePrimitive;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var typeStr = request.GetParam("type", null);
            if (string.IsNullOrEmpty(typeStr))
                return InvalidParameters("Parameter 'type' is required. Valid: Cube, Sphere, Plane, Cylinder, Capsule, Quad");

            if (!TryParsePrimitiveType(typeStr, out var primitiveType))
                return InvalidParameters($"Unknown primitive type: '{typeStr}'. Valid: Cube, Sphere, Plane, Cylinder, Capsule, Quad");

            var name = request.GetParam("name", typeStr);
            var positionStr = request.GetParam("position", null);
            var rotationStr = request.GetParam("rotation", null);
            var scaleStr = request.GetParam("scale", null);
            var materialPath = request.GetParam("material", null);
            var parentId = request.GetParam("parent", null);

            // Create primitive (ObjectFactory auto-registers Undo + applies Presets)
            var undoName = $"unityctl: mesh-create-primitive: {name}";
            UnityEditor.Undo.IncrementCurrentGroup();
            UnityEditor.Undo.SetCurrentGroupName(undoName);

            var go = UnityEditor.ObjectFactory.CreatePrimitive(primitiveType);
            go.name = name;

            // Apply transform
            if (positionStr != null)
            {
                var pos = ParseVector3(positionStr);
                if (pos.HasValue) go.transform.position = pos.Value;
            }

            if (rotationStr != null)
            {
                var rot = ParseQuaternion(rotationStr);
                if (rot.HasValue) go.transform.rotation = rot.Value;
            }

            if (scaleStr != null)
            {
                var scale = ParseVector3(scaleStr);
                if (scale.HasValue) go.transform.localScale = scale.Value;
            }

            // Apply material
            if (!string.IsNullOrEmpty(materialPath))
            {
                var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(materialPath);
                if (mat == null)
                    return Fail(StatusCode.NotFound, $"Material not found: {materialPath}");

                var renderer = go.GetComponent<UnityEngine.MeshRenderer>();
                if (renderer != null)
                    renderer.sharedMaterial = mat;
            }

            // Set parent
            if (!string.IsNullOrEmpty(parentId))
            {
                var parent = GlobalObjectIdResolver.Resolve<UnityEngine.GameObject>(parentId);
                if (parent == null)
                    return Fail(StatusCode.NotFound, $"Parent not found: {parentId}");
                go.transform.SetParent(parent.transform, true);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);

            var globalId = GlobalObjectIdResolver.GetId(go);
            var t = go.transform;

            return Ok($"Created {typeStr} primitive '{name}'", new JObject
            {
                ["globalObjectId"] = globalId,
                ["name"] = go.name,
                ["type"] = typeStr,
                ["position"] = FormatVector3(t.position),
                ["rotation"] = FormatVector3(t.eulerAngles),
                ["scale"] = FormatVector3(t.localScale),
                ["scenePath"] = go.scene.path,
                ["sceneDirty"] = true,
                ["undoGroupName"] = undoName
            });
#else
            return NotInEditor();
#endif
        }

#if UNITY_EDITOR
        private static bool TryParsePrimitiveType(string input, out UnityEngine.PrimitiveType result)
        {
            result = default;
            return input.ToLowerInvariant() switch
            {
                "cube" => Assign(out result, UnityEngine.PrimitiveType.Cube),
                "sphere" => Assign(out result, UnityEngine.PrimitiveType.Sphere),
                "plane" => Assign(out result, UnityEngine.PrimitiveType.Plane),
                "cylinder" => Assign(out result, UnityEngine.PrimitiveType.Cylinder),
                "capsule" => Assign(out result, UnityEngine.PrimitiveType.Capsule),
                "quad" => Assign(out result, UnityEngine.PrimitiveType.Quad),
                _ => false
            };
        }

        private static bool Assign(out UnityEngine.PrimitiveType result, UnityEngine.PrimitiveType value)
        {
            result = value;
            return true;
        }

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
                // Support euler angles as [x,y,z]
                var arr = JArray.Parse(json);
                if (arr.Count >= 3)
                    return UnityEngine.Quaternion.Euler(arr[0].Value<float>(), arr[1].Value<float>(), arr[2].Value<float>());
            }
            catch
            {
                try
                {
                    // Support quaternion as {"x":..,"y":..,"z":..,"w":..}
                    var obj = JObject.Parse(json);
                    return new UnityEngine.Quaternion(
                        obj.Value<float>("x"), obj.Value<float>("y"),
                        obj.Value<float>("z"), obj.Value<float>("w"));
                }
                catch { }
            }
            return null;
        }

        private static string FormatVector3(UnityEngine.Vector3 v)
        {
            return $"[{v.x:G},{v.y:G},{v.z:G}]";
        }
#endif
    }
}
