using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class AssetGetLabelsHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.AssetGetLabels;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var path = request.GetParam("path", null);
            if (string.IsNullOrEmpty(path))
            {
                return InvalidParameters("Parameter 'path' is required.");
            }

            var asset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null)
            {
                return Fail(StatusCode.NotFound, $"Asset not found at: {path}");
            }

            var labels = UnityEditor.AssetDatabase.GetLabels(asset);
            var labelsArray = new JArray();
            foreach (var label in labels)
            {
                labelsArray.Add(label);
            }

            return Ok($"Labels for '{path}'", new JObject
            {
                ["path"] = path,
                ["labels"] = labelsArray
            });
#else
            return NotInEditor();
#endif
        }
    }
}
