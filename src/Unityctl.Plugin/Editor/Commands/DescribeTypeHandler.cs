#nullable enable
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    /// <summary>
    /// Describes a live C# type from the Unity Editor, including members, Unity specifics, and documentation links.
    /// Supports both summary (default) and full (--full) modes for token efficiency.
    /// </summary>
    public sealed class DescribeTypeHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.DescribeType;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var typeName = request.GetParam("typeName", null);
            var full = request.GetParam<bool>("full");
            // GetParam<T> is constrained to non-nullable structs, so read an int sentinel (<=0 means "no limit").
            var maxMembersRaw = request.GetParam<int>("maxMembers");
            int? maxMembers = maxMembersRaw > 0 ? maxMembersRaw : (int?)null;

            if (string.IsNullOrEmpty(typeName))
            {
                return InvalidParameters("Parameter 'typeName' is required.");
            }

            // Resolve the type: priority (1) Type.GetType, (2) TypeCache FullName, (3) simple name with fallback
            Type? resolvedType = ResolveType(typeName);
            if (resolvedType == null)
            {
                return Fail(StatusCode.NotFound, $"Type '{typeName}' not found in the current AppDomain.");
            }

            // Build the response
            var data = new JObject
            {
                ["typeName"] = resolvedType.FullName ?? resolvedType.Name,
                ["simpleName"] = resolvedType.Name,
                ["assembly"] = resolvedType.Assembly?.GetName().Name ?? "unknown",
                ["baseType"] = resolvedType.BaseType?.FullName ?? resolvedType.BaseType?.Name,
                ["namespace"] = resolvedType.Namespace,
                ["isMonoBehaviour"] = IsMonoBehaviourType(resolvedType),
                ["isScriptableObject"] = IsScriptableObjectType(resolvedType),
                ["isComponent"] = typeof(UnityEngine.Component).IsAssignableFrom(resolvedType)
            };

            // Manual link
            if (!string.IsNullOrEmpty(resolvedType.Namespace) &&
                (resolvedType.Namespace.StartsWith("UnityEngine") || resolvedType.Namespace.StartsWith("UnityEditor")))
            {
                data["docUrl"] = $"https://docs.unity3d.com/ScriptReference/{resolvedType.Name}.html";
            }

            // Reflect members
            if (full)
            {
                // Full mode: include signatures
                data["fields"] = new JArray(ReflectFields(resolvedType, maxMembers, includeSerialized: true).ToArray());
                data["properties"] = new JArray(ReflectProperties(resolvedType, maxMembers).ToArray());
                data["methods"] = new JArray(ReflectMethods(resolvedType, maxMembers).ToArray());
                data["events"] = new JArray(ReflectEvents(resolvedType, maxMembers).ToArray());
            }
            else
            {
                // Summary mode: counts + names only
                var fields = ReflectFields(resolvedType, maxMembers, includeSerialized: true);
                var properties = ReflectProperties(resolvedType, maxMembers);
                var methods = ReflectMethods(resolvedType, maxMembers);
                var events = ReflectEvents(resolvedType, maxMembers);

                data["fieldCount"] = fields.Count;
                data["fieldNames"] = new JArray(fields.Select(f => f["name"]).ToArray());
                if (maxMembers.HasValue && fields.Count >= maxMembers)
                    data["fieldsTruncated"] = true;

                data["propertyCount"] = properties.Count;
                data["propertyNames"] = new JArray(properties.Select(p => p["name"]).ToArray());
                if (maxMembers.HasValue && properties.Count >= maxMembers)
                    data["propertiesTruncated"] = true;

                data["methodCount"] = methods.Count;
                data["methodNames"] = new JArray(methods.Select(m => m["name"]).ToArray());
                if (maxMembers.HasValue && methods.Count >= maxMembers)
                    data["methodsTruncated"] = true;

                data["eventCount"] = events.Count;
                data["eventNames"] = new JArray(events.Select(e => e["name"]).ToArray());
                if (maxMembers.HasValue && events.Count >= maxMembers)
                    data["eventsTruncated"] = true;
            }

            return Ok($"Type '{resolvedType.Name}' description", data);
#else
            return NotInEditor();
#endif
        }

        /// <summary>
        /// Resolve type by name: (1) Type.GetType(fqName), (2) TypeCache FullName match, (3) simple name fallback.
        /// </summary>
        private Type? ResolveType(string typeName)
        {
            // Try direct Type.GetType
            var type = Type.GetType(typeName);
            if (type != null)
                return type;

            // Try TypeCache with FullName match (if in Unity 2020.1+)
            try
            {
                var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => GetTypesFromAssembly(a))
                    .ToList();

                // First try exact FullName match
                var exactMatch = allTypes.FirstOrDefault(t => t.FullName == typeName);
                if (exactMatch != null)
                    return exactMatch;

                // Then try simple Name match (ambiguous: return error)
                var nameMatches = allTypes.Where(t => t.Name == typeName).ToList();
                if (nameMatches.Count == 1)
                    return nameMatches[0];
                if (nameMatches.Count > 1)
                {
                    // Ambiguous: list candidates
                    var candidates = string.Join(", ", nameMatches.Select(t => t.FullName ?? t.Name));
                    throw new InvalidOperationException(
                        $"Ambiguous type name '{typeName}': {candidates}. Use fully-qualified name.");
                }
            }
            catch (ReflectionTypeLoadException rtle)
            {
                // Load what we can from rtle.Types
                var validTypes = rtle.Types?.Where(t => t != null).ToList() ?? new List<Type>();
                var exactMatch = validTypes.FirstOrDefault(t => t?.FullName == typeName);
                if (exactMatch != null)
                    return exactMatch;

                var nameMatches = validTypes.Where(t => t?.Name == typeName).ToList();
                if (nameMatches.Count == 1)
                    return nameMatches[0];
            }

            return null;
        }

        private List<Type> GetTypesFromAssembly(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes().ToList();
            }
            catch (ReflectionTypeLoadException)
            {
                return new List<Type>();
            }
        }

        private bool IsMonoBehaviourType(Type type)
        {
            try
            {
                return type.IsClass && typeof(UnityEngine.MonoBehaviour).IsAssignableFrom(type);
            }
            catch
            {
                return false;
            }
        }

        private bool IsScriptableObjectType(Type type)
        {
            try
            {
                return type.IsClass && typeof(UnityEngine.ScriptableObject).IsAssignableFrom(type);
            }
            catch
            {
                return false;
            }
        }

        private List<JObject> ReflectFields(Type type, int? maxMembers, bool includeSerialized = false)
        {
            var result = new List<JObject>();
            try
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Where(f => !f.IsSpecialName)
                    .ToList();

                if (maxMembers.HasValue)
                    fields = fields.Take(maxMembers.Value).ToList();

                foreach (var field in fields)
                {
                    var obj = new JObject { ["name"] = field.Name, ["type"] = field.FieldType.Name };
                    obj["isSerializable"] = IsSerializableType(field.FieldType);
                    result.Add(obj);
                }

                // Also include [SerializeField] private fields if includeSerialized
                if (includeSerialized)
                {
                    var privateWithSerialize = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(f => !f.IsSpecialName && f.GetCustomAttribute<UnityEngine.SerializeField>() != null)
                        .ToList();

                    if (maxMembers.HasValue)
                        privateWithSerialize = privateWithSerialize.Take(maxMembers.Value - result.Count).ToList();

                    foreach (var field in privateWithSerialize)
                    {
                        var obj = new JObject
                        {
                            ["name"] = field.Name,
                            ["type"] = field.FieldType.Name,
                            ["hasSerializeField"] = true
                        };
                        obj["isSerializable"] = IsSerializableType(field.FieldType);
                        result.Add(obj);
                    }
                }
            }
            catch
            {
                // Silently ignore reflection failures
            }
            return result;
        }

        private List<JObject> ReflectProperties(Type type, int? maxMembers)
        {
            var result = new List<JObject>();
            try
            {
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => !p.IsSpecialName && p.CanRead)
                    .ToList();

                if (maxMembers.HasValue)
                    properties = properties.Take(maxMembers.Value).ToList();

                foreach (var prop in properties)
                {
                    var obj = new JObject
                    {
                        ["name"] = prop.Name,
                        ["type"] = prop.PropertyType.Name,
                        ["canRead"] = prop.CanRead,
                        ["canWrite"] = prop.CanWrite
                    };
                    result.Add(obj);
                }
            }
            catch
            {
                // Silently ignore reflection failures
            }
            return result;
        }

        private List<JObject> ReflectMethods(Type type, int? maxMembers)
        {
            var result = new List<JObject>();
            try
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(m => !m.IsSpecialName)
                    .ToList();

                if (maxMembers.HasValue)
                    methods = methods.Take(maxMembers.Value).ToList();

                foreach (var method in methods)
                {
                    var paramTypes = string.Join(", ",
                        method.GetParameters().Select(p => p.ParameterType.Name));
                    var obj = new JObject
                    {
                        ["name"] = method.Name,
                        ["returnType"] = method.ReturnType.Name,
                        ["parameterCount"] = method.GetParameters().Length,
                        ["signature"] = $"{method.ReturnType.Name} {method.Name}({paramTypes})"
                    };
                    result.Add(obj);
                }
            }
            catch
            {
                // Silently ignore reflection failures
            }
            return result;
        }

        private List<JObject> ReflectEvents(Type type, int? maxMembers)
        {
            var result = new List<JObject>();
            try
            {
                var events = type.GetEvents(BindingFlags.Public | BindingFlags.Instance)
                    .ToList();

                if (maxMembers.HasValue)
                    events = events.Take(maxMembers.Value).ToList();

                foreach (var evt in events)
                {
                    var obj = new JObject
                    {
                        ["name"] = evt.Name,
                        ["eventHandlerType"] = evt.EventHandlerType?.Name ?? "unknown"
                    };
                    result.Add(obj);
                }
            }
            catch
            {
                // Silently ignore reflection failures
            }
            return result;
        }

        private bool IsSerializableType(Type type)
        {
            if (type == null)
                return false;

            // Unity-serializable types
            if (type.IsValueType || type == typeof(string))
                return true;

            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                return true;

            if (typeof(UnityEngine.Vector2).IsAssignableFrom(type) ||
                typeof(UnityEngine.Vector3).IsAssignableFrom(type) ||
                typeof(UnityEngine.Color).IsAssignableFrom(type))
                return true;

            return false;
        }
    }
}

#endif
