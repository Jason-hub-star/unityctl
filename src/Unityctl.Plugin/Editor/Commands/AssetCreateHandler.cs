using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class AssetCreateHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.AssetCreate;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var path = request.GetParam("path", null);
            if (string.IsNullOrEmpty(path))
                return InvalidParameters("Parameter 'path' is required.");

            var type = request.GetParam("type", null);
            if (string.IsNullOrEmpty(type))
                return InvalidParameters("Parameter 'type' is required.");

            var assetType = ResolveAssetType(type);

            if (assetType == null)
                return Fail(StatusCode.InvalidParameters, $"Unknown type: {type}");

            if (!typeof(UnityEngine.ScriptableObject).IsAssignableFrom(assetType))
                return Fail(StatusCode.InvalidParameters,
                    $"Type '{type}' is not a ScriptableObject. Only ScriptableObject types can be created as assets.");

            var instance = UnityEngine.ScriptableObject.CreateInstance(assetType);
            if (instance == null)
                return Fail(StatusCode.UnknownError, $"Failed to create instance of type '{type}'.");

            UnityEditor.AssetDatabase.CreateAsset(instance, path);
            UnityEditor.AssetDatabase.SaveAssets();

            var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
            {
                return Fail(StatusCode.UnknownError, $"Asset created but GUID lookup failed for: {path}");
            }

            return Ok($"Created asset at '{path}'", new JObject
            {
                ["path"] = path,
                ["guid"] = guid,
                ["type"] = assetType.FullName
            });
#else
            return NotInEditor();
#endif
        }

#if UNITY_EDITOR
        private static System.Type ResolveAssetType(string type)
        {
            var resolved = System.Type.GetType(type)
                ?? System.Type.GetType($"UnityEngine.{type}, UnityEngine")
                ?? System.Type.GetType($"UnityEditor.{type}, UnityEditor");

            if (resolved != null)
                return resolved;

            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                resolved = assembly.GetType(type, throwOnError: false, ignoreCase: false);
                if (resolved != null)
                    return resolved;

                System.Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (System.Reflection.ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }

                foreach (var candidate in types)
                {
                    if (candidate == null) continue;
                    if (string.Equals(candidate.Name, type, System.StringComparison.Ordinal))
                        return candidate;
                }
            }

            return null;
        }
#endif
    }
}
