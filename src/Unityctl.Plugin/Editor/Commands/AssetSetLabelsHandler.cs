using System;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class AssetSetLabelsHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.AssetSetLabels;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var path = request.GetParam("path", null);
            if (string.IsNullOrEmpty(path))
            {
                return InvalidParameters("Parameter 'path' is required.");
            }

            var labelsParam = request.GetParam("labels", null);
            if (labelsParam == null)
            {
                return InvalidParameters("Parameter 'labels' is required.");
            }

            var asset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null)
            {
                return Fail(StatusCode.NotFound, $"Asset not found at: {path}");
            }

            string[] labelsArray;
            if (string.IsNullOrEmpty(labelsParam))
            {
                labelsArray = Array.Empty<string>();
            }
            else
            {
                labelsArray = labelsParam.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < labelsArray.Length; i++)
                {
                    labelsArray[i] = labelsArray[i].Trim();
                }
            }

            UnityEditor.Undo.RecordObject(asset, "Set Labels");
            UnityEditor.AssetDatabase.SetLabels(asset, labelsArray);

            var resultLabels = new JArray();
            foreach (var label in labelsArray)
            {
                resultLabels.Add(label);
            }

            return Ok($"Labels set on '{path}'", new JObject
            {
                ["path"] = path,
                ["labels"] = resultLabels
            });
#else
            return NotInEditor();
#endif
        }
    }
}
