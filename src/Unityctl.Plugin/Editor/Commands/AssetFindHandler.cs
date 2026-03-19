using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class AssetFindHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.AssetFind;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var filter = request.GetParam("filter", null);
            var folder = request.GetParam("folder", null);
            var limit = request.GetParam<int>("limit");

            if (string.IsNullOrEmpty(filter))
            {
                return InvalidParameters("Parameter 'filter' is required.");
            }

            string[] guids;
            if (string.IsNullOrWhiteSpace(folder))
            {
                guids = UnityEditor.AssetDatabase.FindAssets(filter);
            }
            else
            {
                guids = UnityEditor.AssetDatabase.FindAssets(filter, new[] { folder });
            }

            var results = new JArray();
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var mainAssetType = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(path);

                results.Add(new JObject
                {
                    ["guid"] = guid,
                    ["path"] = path,
                    ["mainAssetType"] = mainAssetType != null ? mainAssetType.FullName ?? mainAssetType.Name : string.Empty,
                    ["isFolder"] = UnityEditor.AssetDatabase.IsValidFolder(path)
                });

                if (limit > 0 && results.Count >= limit)
                {
                    break;
                }
            }

            return Ok($"Found {results.Count} asset(s)", new JObject
            {
                ["results"] = results
            });
#else
            return NotInEditor();
#endif
        }
    }
}
