#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class LightingGetSettingsHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.LightingGetSettings;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var settings = UnityEditor.Lightmapping.lightingSettings;
            if (settings == null)
            {
                return Ok("No LightingSettings assigned to current scene (using defaults)", new JObject
                {
                    ["hasSettings"] = false
                });
            }

            var so = new UnityEditor.SerializedObject(settings);
            var data = new JObject();
            var iterator = so.GetIterator();
            if (iterator.NextVisible(true))
            {
                do
                {
                    if (iterator.name == "m_ObjectHideFlags" || iterator.name == "m_Script")
                        continue;

                    data[iterator.propertyPath] = GetPropertyValue(iterator);
                } while (iterator.NextVisible(false));
            }

            return Ok("Lighting settings retrieved", new JObject
            {
                ["hasSettings"] = true,
                ["name"] = settings.name,
                ["settings"] = data
            });
        }

        private static JToken GetPropertyValue(UnityEditor.SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case UnityEditor.SerializedPropertyType.Integer:
                    return prop.intValue;
                case UnityEditor.SerializedPropertyType.Boolean:
                    return prop.boolValue;
                case UnityEditor.SerializedPropertyType.Float:
                    return prop.floatValue;
                case UnityEditor.SerializedPropertyType.String:
                    return prop.stringValue ?? "";
                case UnityEditor.SerializedPropertyType.Enum:
                    return prop.enumValueIndex;
                default:
                    return prop.propertyType.ToString();
            }
        }
    }
}
#endif
