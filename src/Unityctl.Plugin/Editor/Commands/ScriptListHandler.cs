#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ScriptListHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ScriptList;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var folder = request.GetParam("folder", null);
            var filter = request.GetParam("filter", null);
            var limit = request.GetParam<int>("limit");

            string[] guids;
            if (string.IsNullOrWhiteSpace(folder))
            {
                guids = UnityEditor.AssetDatabase.FindAssets("t:MonoScript");
            }
            else
            {
                guids = UnityEditor.AssetDatabase.FindAssets("t:MonoScript", new[] { folder });
            }

            var results = new JArray();
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var script = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.MonoScript>(path);

                var className = script != null ? script.GetClass()?.Name ?? script.name : "";
                var ns = script != null ? script.GetClass()?.Namespace ?? "" : "";

                // Apply name filter (case-insensitive)
                if (!string.IsNullOrEmpty(filter))
                {
                    var match = className.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0
                             || path.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!match) continue;
                }

                results.Add(new JObject
                {
                    ["guid"] = guid,
                    ["path"] = path,
                    ["className"] = className,
                    ["namespace"] = ns
                });

                if (limit > 0 && results.Count >= limit)
                {
                    break;
                }
            }

            return Ok($"Found {results.Count} script(s)", new JObject
            {
                ["results"] = results
            });
        }
    }
}
#endif
