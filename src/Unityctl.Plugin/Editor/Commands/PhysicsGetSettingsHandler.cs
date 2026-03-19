#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class PhysicsGetSettingsHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.PhysicsGetSettings;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/DynamicsManager.asset");
            if (assets == null || assets.Length == 0)
            {
                return Fail(StatusCode.NotFound, "DynamicsManager.asset not found");
            }

            var so = new UnityEditor.SerializedObject(assets[0]);
            var data = new JObject();
            var iterator = so.GetIterator();
            if (iterator.NextVisible(true))
            {
                do
                {
                    if (iterator.name == "m_ObjectHideFlags" || iterator.name == "m_Script")
                        continue;

                    // Skip m_LayerCollisionMatrix — use physics get-collision-matrix instead
                    if (iterator.propertyPath.StartsWith("m_LayerCollisionMatrix"))
                        continue;

                    data[iterator.propertyPath] = GetPropertyValue(iterator);
                } while (iterator.NextVisible(false));
            }

            return Ok("Physics settings retrieved", new JObject
            {
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
                case UnityEditor.SerializedPropertyType.Vector3:
                    var v = prop.vector3Value;
                    return new JArray(v.x, v.y, v.z);
                default:
                    return prop.propertyType.ToString();
            }
        }
    }
}
#endif
