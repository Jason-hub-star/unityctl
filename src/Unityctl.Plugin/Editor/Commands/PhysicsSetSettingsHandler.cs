#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class PhysicsSetSettingsHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.PhysicsSetSettings;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var propertyPath = request.GetParam("property", null);
            var valueStr = request.GetParam("value", null);

            if (string.IsNullOrEmpty(propertyPath))
                return InvalidParameters("Missing required parameter: property");
            if (valueStr == null)
                return InvalidParameters("Missing required parameter: value");

            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/DynamicsManager.asset");
            if (assets == null || assets.Length == 0)
            {
                return Fail(StatusCode.NotFound, "DynamicsManager.asset not found");
            }

            var target = assets[0];
            UnityEditor.Undo.RecordObject(target, "unityctl: physics set-settings");

            var so = new UnityEditor.SerializedObject(target);
            var prop = so.FindProperty(propertyPath);
            if (prop == null)
            {
                return Fail(StatusCode.NotFound,
                    $"Property not found: {propertyPath}");
            }

            if (!SetPropertyValue(prop, valueStr, out var error))
            {
                return Fail(StatusCode.InvalidParameters, error);
            }

            so.ApplyModifiedProperties();

            return Ok($"Physics setting '{propertyPath}' updated", new JObject
            {
                ["property"] = propertyPath,
                ["value"] = valueStr,
                ["applied"] = true
            });
        }

        private static bool SetPropertyValue(UnityEditor.SerializedProperty prop, string value, out string error)
        {
            error = "";
            switch (prop.propertyType)
            {
                case UnityEditor.SerializedPropertyType.Integer:
                    if (!int.TryParse(value, out var intVal))
                    {
                        error = $"Cannot parse '{value}' as integer";
                        return false;
                    }
                    prop.intValue = intVal;
                    return true;

                case UnityEditor.SerializedPropertyType.Boolean:
                    if (!bool.TryParse(value, out var boolVal))
                    {
                        error = $"Cannot parse '{value}' as boolean";
                        return false;
                    }
                    prop.boolValue = boolVal;
                    return true;

                case UnityEditor.SerializedPropertyType.Float:
                    if (!float.TryParse(value, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out var floatVal))
                    {
                        error = $"Cannot parse '{value}' as float";
                        return false;
                    }
                    prop.floatValue = floatVal;
                    return true;

                case UnityEditor.SerializedPropertyType.String:
                    prop.stringValue = value;
                    return true;

                case UnityEditor.SerializedPropertyType.Enum:
                    if (!int.TryParse(value, out var enumIdx))
                    {
                        error = $"Cannot parse '{value}' as enum index (integer)";
                        return false;
                    }
                    prop.enumValueIndex = enumIdx;
                    return true;

                case UnityEditor.SerializedPropertyType.Vector3:
                    try
                    {
                        var arr = JArray.Parse(value);
                        if (arr.Count != 3)
                        {
                            error = "Vector3 value must be a JSON array with 3 elements [x,y,z]";
                            return false;
                        }
                        prop.vector3Value = new UnityEngine.Vector3(
                            (float)arr[0], (float)arr[1], (float)arr[2]);
                        return true;
                    }
                    catch (System.Exception ex)
                    {
                        error = $"Cannot parse '{value}' as Vector3: {ex.Message}";
                        return false;
                    }

                default:
                    error = $"Unsupported property type: {prop.propertyType}";
                    return false;
            }
        }
    }
}
#endif
