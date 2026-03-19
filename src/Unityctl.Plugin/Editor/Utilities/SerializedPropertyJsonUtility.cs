#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace Unityctl.Plugin.Editor.Utilities
{
    internal static class SerializedPropertyJsonUtility
    {
        public static JToken ToJsonValue(SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return prop.intValue;
                case SerializedPropertyType.Boolean:
                    return prop.boolValue;
                case SerializedPropertyType.Float:
                    return prop.floatValue;
                case SerializedPropertyType.String:
                    return prop.stringValue ?? string.Empty;
                case SerializedPropertyType.Color:
                    return new JObject
                    {
                        ["r"] = prop.colorValue.r,
                        ["g"] = prop.colorValue.g,
                        ["b"] = prop.colorValue.b,
                        ["a"] = prop.colorValue.a
                    };
                case SerializedPropertyType.Vector2:
                    return new JObject
                    {
                        ["x"] = prop.vector2Value.x,
                        ["y"] = prop.vector2Value.y
                    };
                case SerializedPropertyType.Vector3:
                    return new JObject
                    {
                        ["x"] = prop.vector3Value.x,
                        ["y"] = prop.vector3Value.y,
                        ["z"] = prop.vector3Value.z
                    };
                case SerializedPropertyType.Vector4:
                    return new JObject
                    {
                        ["x"] = prop.vector4Value.x,
                        ["y"] = prop.vector4Value.y,
                        ["z"] = prop.vector4Value.z,
                        ["w"] = prop.vector4Value.w
                    };
                case SerializedPropertyType.Quaternion:
                    return new JObject
                    {
                        ["x"] = prop.quaternionValue.x,
                        ["y"] = prop.quaternionValue.y,
                        ["z"] = prop.quaternionValue.z,
                        ["w"] = prop.quaternionValue.w
                    };
                case SerializedPropertyType.Enum:
                    return prop.enumValueIndex;
                case SerializedPropertyType.ObjectReference:
                    return prop.objectReferenceValue != null
                        ? (JToken)prop.objectReferenceValue.name
                        : JValue.CreateNull();
                case SerializedPropertyType.LayerMask:
                    return prop.intValue;
                case SerializedPropertyType.ArraySize:
                    return prop.intValue;
                case SerializedPropertyType.Rect:
                    return new JObject
                    {
                        ["x"] = prop.rectValue.x,
                        ["y"] = prop.rectValue.y,
                        ["width"] = prop.rectValue.width,
                        ["height"] = prop.rectValue.height
                    };
                case SerializedPropertyType.Bounds:
                    return new JObject
                    {
                        ["centerX"] = prop.boundsValue.center.x,
                        ["centerY"] = prop.boundsValue.center.y,
                        ["centerZ"] = prop.boundsValue.center.z,
                        ["extentsX"] = prop.boundsValue.extents.x,
                        ["extentsY"] = prop.boundsValue.extents.y,
                        ["extentsZ"] = prop.boundsValue.extents.z
                    };
                default:
                    return prop.ToString();
            }
        }

        public static JObject GetVisibleProperties(UnityEngine.Component component)
        {
            var properties = new JObject();
            using (var serializedObject = new SerializedObject(component))
            {
                var iterator = serializedObject.GetIterator();
                while (iterator.NextVisible(true))
                {
                    var value = ToJsonValue(iterator);
                    if (value != null)
                    {
                        properties[iterator.propertyPath] = value;
                    }
                }
            }

            return properties;
        }
    }
}
#endif
