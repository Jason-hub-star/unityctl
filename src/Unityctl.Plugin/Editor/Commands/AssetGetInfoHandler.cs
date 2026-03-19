using System.IO;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class AssetGetInfoHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.AssetGetInfo;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var path = request.GetParam("path", null);
            if (string.IsNullOrEmpty(path))
            {
                return InvalidParameters("Parameter 'path' is required.");
            }

            var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            var isFolder = UnityEditor.AssetDatabase.IsValidFolder(path);
            var fullPath = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", path.Replace('/', Path.DirectorySeparatorChar)));
            var existsOnDisk = File.Exists(fullPath) || Directory.Exists(fullPath);
            if (string.IsNullOrEmpty(guid) && !isFolder)
            {
                return Fail(StatusCode.NotFound, $"Asset not found at: {path}");
            }

            var asset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path);
            var mainAssetType = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(path);
            if (!existsOnDisk && !isFolder && asset == null && (mainAssetType == null || string.IsNullOrEmpty(mainAssetType.FullName)))
            {
                return Fail(StatusCode.NotFound, $"Asset not found at: {path}");
            }

            var labels = new JArray();
            if (asset != null)
            {
                foreach (var label in UnityEditor.AssetDatabase.GetLabels(asset))
                {
                    labels.Add(label);
                }
            }

            return Ok($"Asset '{path}'", new JObject
            {
                ["path"] = path,
                ["guid"] = guid,
                ["name"] = Path.GetFileNameWithoutExtension(path.TrimEnd('/')),
                ["mainAssetType"] = mainAssetType != null ? mainAssetType.FullName ?? mainAssetType.Name : string.Empty,
                ["isFolder"] = isFolder,
                ["labels"] = labels
            });
#else
            return NotInEditor();
#endif
        }
    }
}
