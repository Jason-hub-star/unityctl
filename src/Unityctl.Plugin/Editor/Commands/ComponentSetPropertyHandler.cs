using System;
using Newtonsoft.Json.Linq;
using UnityEditor.SceneManagement;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ComponentSetPropertyHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ComponentSetProperty;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var componentId = request.GetParam("componentId", null);
            var propertyPath = request.GetParam("property", null);
            var valueStr = request.GetParam("value", null);

            if (string.IsNullOrEmpty(componentId))
                return InvalidParameters("Parameter 'componentId' is required.");
            if (string.IsNullOrEmpty(propertyPath))
                return InvalidParameters("Parameter 'property' is required.");
            if (valueStr == null)
                return InvalidParameters("Parameter 'value' is required.");

            var component = GlobalObjectIdResolver.Resolve<UnityEngine.Component>(componentId);
            if (component == null)
                return Fail(StatusCode.NotFound, $"Component not found: {componentId}");

            var prefabReject = PrefabGuard.RejectIfPrefab(component);
            if (prefabReject != null) return prefabReject;

            var undoName = $"unityctl: component-set-property: {component.GetType().Name}.{propertyPath}";

            using (new UndoScope(undoName))
            {
                var so = new UnityEditor.SerializedObject(component);
                var prop = so.FindProperty(propertyPath);

                if (prop == null)
                    return InvalidParameters(
                        $"Property '{propertyPath}' not found on {component.GetType().Name}.");

                if (!SetPropertyValue(prop, valueStr))
                    return InvalidParameters(
                        $"Failed to set '{propertyPath}' to '{valueStr}'. " +
                        $"Property type: {prop.propertyType}");

                so.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(component.gameObject.scene);

                // Read back the value
                so.Update();
                prop = so.FindProperty(propertyPath);
                var readBack = ReadPropertyValue(prop);

                return Ok($"{component.GetType().Name}.{propertyPath} = {readBack}", new JObject
                {
                    ["componentGlobalObjectId"] = componentId,
                    ["componentType"] = component.GetType().FullName,
                    ["property"] = propertyPath,
                    ["value"] = readBack,
                    ["gameObjectName"] = component.gameObject.name,
                    ["scenePath"] = component.gameObject.scene.path,
                    ["sceneDirty"] = true,
                    ["undoGroupName"] = undoName
                });
            }
#else
            return NotInEditor();
#endif
        }

#if UNITY_EDITOR
        // Accept the array form [x,y,z] for vector/color values, consistent with
        // mesh create-primitive's --position/--scale. Agents naturally reuse that
        // format, so support it alongside the {"x":..,"y":..} object form.
        private static bool TryReadFloatArray(JToken token, int count, out float[] vals)
        {
            vals = null;
            if (token is JArray arr && arr.Count >= count)
            {
                var parsed = new float[count];
                for (int i = 0; i < count; i++)
                    parsed[i] = arr[i].Value<float>();
                vals = parsed;
                return true;
            }
            return false;
        }

        private static bool SetPropertyValue(UnityEditor.SerializedProperty prop, string valueStr)
        {
            try
            {
                // Try parsing as JSON for vector/compound types
                JToken jsonValue = null;
                try { jsonValue = JToken.Parse(valueStr); }
                catch { /* not JSON, treat as raw string */ }

                switch (prop.propertyType)
                {
                    case UnityEditor.SerializedPropertyType.Integer:
                        prop.intValue = int.Parse(valueStr);
                        return true;

                    case UnityEditor.SerializedPropertyType.Boolean:
                        prop.boolValue = bool.Parse(valueStr);
                        return true;

                    case UnityEditor.SerializedPropertyType.Float:
                        prop.floatValue = float.Parse(valueStr);
                        return true;

                    case UnityEditor.SerializedPropertyType.String:
                        // If JSON string literal, unwrap quotes
                        if (jsonValue is JValue jv && jv.Type == JTokenType.String)
                            prop.stringValue = jv.Value<string>();
                        else
                            prop.stringValue = valueStr;
                        return true;

                    case UnityEditor.SerializedPropertyType.Enum:
                        if (int.TryParse(valueStr, out var enumIndex))
                            prop.enumValueIndex = enumIndex;
                        else
                        {
                            // Try matching enum name
                            var names = prop.enumNames;
                            for (int i = 0; i < names.Length; i++)
                            {
                                if (string.Equals(names[i], valueStr, StringComparison.OrdinalIgnoreCase))
                                {
                                    prop.enumValueIndex = i;
                                    return true;
                                }
                            }
                            return false;
                        }
                        return true;

                    case UnityEditor.SerializedPropertyType.Color:
                        if (jsonValue is JObject colorObj)
                        {
                            prop.colorValue = new UnityEngine.Color(
                                colorObj.Value<float>("r"),
                                colorObj.Value<float>("g"),
                                colorObj.Value<float>("b"),
                                colorObj.Value<float>("a"));
                            return true;
                        }
                        if (TryReadFloatArray(jsonValue, 3, out var colArr))
                        {
                            var a = jsonValue is JArray ca && ca.Count >= 4 ? ca[3].Value<float>() : 1f;
                            prop.colorValue = new UnityEngine.Color(colArr[0], colArr[1], colArr[2], a);
                            return true;
                        }
                        return false;

                    case UnityEditor.SerializedPropertyType.Vector2:
                        if (jsonValue is JObject v2Obj)
                        {
                            prop.vector2Value = new UnityEngine.Vector2(
                                v2Obj.Value<float>("x"),
                                v2Obj.Value<float>("y"));
                            return true;
                        }
                        if (TryReadFloatArray(jsonValue, 2, out var v2Arr))
                        {
                            prop.vector2Value = new UnityEngine.Vector2(v2Arr[0], v2Arr[1]);
                            return true;
                        }
                        return false;

                    case UnityEditor.SerializedPropertyType.Vector3:
                        if (jsonValue is JObject v3Obj)
                        {
                            prop.vector3Value = new UnityEngine.Vector3(
                                v3Obj.Value<float>("x"),
                                v3Obj.Value<float>("y"),
                                v3Obj.Value<float>("z"));
                            return true;
                        }
                        if (TryReadFloatArray(jsonValue, 3, out var v3Arr))
                        {
                            prop.vector3Value = new UnityEngine.Vector3(v3Arr[0], v3Arr[1], v3Arr[2]);
                            return true;
                        }
                        return false;

                    case UnityEditor.SerializedPropertyType.Vector4:
                        if (jsonValue is JObject v4Obj)
                        {
                            prop.vector4Value = new UnityEngine.Vector4(
                                v4Obj.Value<float>("x"),
                                v4Obj.Value<float>("y"),
                                v4Obj.Value<float>("z"),
                                v4Obj.Value<float>("w"));
                            return true;
                        }
                        if (TryReadFloatArray(jsonValue, 4, out var v4Arr))
                        {
                            prop.vector4Value = new UnityEngine.Vector4(v4Arr[0], v4Arr[1], v4Arr[2], v4Arr[3]);
                            return true;
                        }
                        return false;

                    case UnityEditor.SerializedPropertyType.Quaternion:
                        if (jsonValue is JObject qObj)
                        {
                            prop.quaternionValue = new UnityEngine.Quaternion(
                                qObj.Value<float>("x"),
                                qObj.Value<float>("y"),
                                qObj.Value<float>("z"),
                                qObj.Value<float>("w"));
                            return true;
                        }
                        if (TryReadFloatArray(jsonValue, 4, out var qArr))
                        {
                            prop.quaternionValue = new UnityEngine.Quaternion(qArr[0], qArr[1], qArr[2], qArr[3]);
                            return true;
                        }
                        return false;

                    case UnityEditor.SerializedPropertyType.Rect:
                        if (jsonValue is JObject rObj)
                        {
                            prop.rectValue = new UnityEngine.Rect(
                                rObj.Value<float>("x"),
                                rObj.Value<float>("y"),
                                rObj.Value<float>("width"),
                                rObj.Value<float>("height"));
                            return true;
                        }
                        return false;

                    case UnityEditor.SerializedPropertyType.Bounds:
                        if (jsonValue is JObject bObj)
                        {
                            prop.boundsValue = new UnityEngine.Bounds(
                                new UnityEngine.Vector3(
                                    bObj["center"]?.Value<float>("x") ?? 0,
                                    bObj["center"]?.Value<float>("y") ?? 0,
                                    bObj["center"]?.Value<float>("z") ?? 0),
                                new UnityEngine.Vector3(
                                    bObj["size"]?.Value<float>("x") ?? 0,
                                    bObj["size"]?.Value<float>("y") ?? 0,
                                    bObj["size"]?.Value<float>("z") ?? 0));
                            return true;
                        }
                        return false;

                    case UnityEditor.SerializedPropertyType.LayerMask:
                        prop.intValue = int.Parse(valueStr);
                        return true;

                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private static string ReadPropertyValue(UnityEditor.SerializedProperty prop)
        {
            if (prop == null) return "null";

            switch (prop.propertyType)
            {
                case UnityEditor.SerializedPropertyType.Integer:
                    return prop.intValue.ToString();
                case UnityEditor.SerializedPropertyType.Boolean:
                    return prop.boolValue.ToString();
                case UnityEditor.SerializedPropertyType.Float:
                    return prop.floatValue.ToString();
                case UnityEditor.SerializedPropertyType.String:
                    return prop.stringValue;
                case UnityEditor.SerializedPropertyType.Enum:
                    return prop.enumNames[prop.enumValueIndex];
                case UnityEditor.SerializedPropertyType.Color:
                    var c = prop.colorValue;
                    return $"{{\"r\":{c.r},\"g\":{c.g},\"b\":{c.b},\"a\":{c.a}}}";
                case UnityEditor.SerializedPropertyType.Vector2:
                    var v2 = prop.vector2Value;
                    return $"{{\"x\":{v2.x},\"y\":{v2.y}}}";
                case UnityEditor.SerializedPropertyType.Vector3:
                    var v3 = prop.vector3Value;
                    return $"{{\"x\":{v3.x},\"y\":{v3.y},\"z\":{v3.z}}}";
                case UnityEditor.SerializedPropertyType.Vector4:
                    var v4 = prop.vector4Value;
                    return $"{{\"x\":{v4.x},\"y\":{v4.y},\"z\":{v4.z},\"w\":{v4.w}}}";
                case UnityEditor.SerializedPropertyType.Quaternion:
                    var q = prop.quaternionValue;
                    return $"{{\"x\":{q.x},\"y\":{q.y},\"z\":{q.z},\"w\":{q.w}}}";
                case UnityEditor.SerializedPropertyType.LayerMask:
                    return prop.intValue.ToString();
                default:
                    return prop.type;
            }
        }
#endif
    }
}
