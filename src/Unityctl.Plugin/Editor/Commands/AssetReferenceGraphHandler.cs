using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class AssetReferenceGraphHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.AssetReferenceGraph;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var targetPath = request.GetParam("path", null);
            if (string.IsNullOrEmpty(targetPath))
            {
                return InvalidParameters("Parameter 'path' is required.");
            }

            var targetGuid = UnityEditor.AssetDatabase.AssetPathToGUID(targetPath);
            var isFolder = UnityEditor.AssetDatabase.IsValidFolder(targetPath);
            if (string.IsNullOrEmpty(targetGuid) && !isFolder)
            {
                return Fail(StatusCode.NotFound, $"Asset not found at: {targetPath}");
            }

            var scanRoots = AssetReferenceGraphUtility.GetExistingScanRoots();
            var candidatePaths = AssetReferenceGraphUtility.EnumerateCandidatePaths(scanRoots);
            var references = new JArray();

            foreach (var candidatePath in candidatePaths)
            {
                if (string.Equals(candidatePath, targetPath, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var match = AssetReferenceGraphUtility.FindReferenceMatch(candidatePath, targetPath);
                if (match == null)
                {
                    continue;
                }

                var reference = AssetReferenceGraphUtility.BuildAssetMetadata(match.Path);
                reference["relation"] = match.Relation;
                references.Add(reference);
            }

            var roots = new JArray();
            foreach (var root in scanRoots)
            {
                roots.Add(root);
            }

            return Ok($"Found {references.Count} reverse reference" + (references.Count == 1 ? string.Empty : "s"), new JObject
            {
                ["target"] = AssetReferenceGraphUtility.BuildAssetMetadata(targetPath),
                ["scan"] = new JObject
                {
                    ["roots"] = roots,
                    ["scannedAssetCount"] = candidatePaths.Length
                },
                ["references"] = references,
                ["referenceCount"] = references.Count
            });
#else
            return NotInEditor();
#endif
        }
    }
}
