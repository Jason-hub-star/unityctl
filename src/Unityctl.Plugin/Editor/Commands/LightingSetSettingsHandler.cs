#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class LightingSetSettingsHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.LightingSetSettings;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var propertyPath = request.GetParam("property", null);
            var valueStr = request.GetParam("value", null);

            if (string.IsNullOrEmpty(propertyPath))
                return InvalidParameters("Missing required parameter: property");
            if (valueStr == null)
                return InvalidParameters("Missing required parameter: value");

            var settings = UnityEditor.Lightmapping.lightingSettings;
            if (settings == null)
            {
                return Fail(StatusCode.NotFound,
                    "No LightingSettings assigned to current scene. Open Lighting window to create one.");
            }

            var so = new UnityEditor.SerializedObject(settings);
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

            return Ok($"Lighting setting '{propertyPath}' updated", new JObject
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

                default:
                    error = $"Unsupported property type: {prop.propertyType}";
                    return false;
            }
        }
    }
}
#endif
