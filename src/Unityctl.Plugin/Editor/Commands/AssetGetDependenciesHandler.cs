using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class AssetGetDependenciesHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.AssetGetDependencies;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var path = request.GetParam("path", null);
            var recursive = request.GetParam("recursive", true);

            if (string.IsNullOrEmpty(path))
            {
                return InvalidParameters("Parameter 'path' is required.");
            }

            var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            var isFolder = UnityEditor.AssetDatabase.IsValidFolder(path);
            if (string.IsNullOrEmpty(guid) && !isFolder)
            {
                return Fail(StatusCode.NotFound, $"Asset not found at: {path}");
            }

            var dependencies = UnityEditor.AssetDatabase.GetDependencies(path, recursive);
            var dependencyArray = new JArray();
            foreach (var dependency in dependencies)
            {
                dependencyArray.Add(dependency);
            }

            return Ok($"Found {dependencyArray.Count} dependenc" + (dependencyArray.Count == 1 ? "y" : "ies"), new JObject
            {
                ["path"] = path,
                ["recursive"] = recursive,
                ["dependencies"] = dependencyArray
            });
#else
            return NotInEditor();
#endif
        }
    }
}
