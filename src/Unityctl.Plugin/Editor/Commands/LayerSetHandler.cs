using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class LayerSetHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.LayerSet;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var index = request.GetParam<int>("index");
            var name = request.GetParam("name", null);

            if (string.IsNullOrEmpty(name))
                return InvalidParameters("Parameter 'name' is required.");
            if (index < 8 || index > 31)
                return InvalidParameters("Parameter 'index' must be between 8 and 31 (user layers only).");

            var tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layersProp = tagManager.FindProperty("layers");

            var undoName = $"unityctl: layer-set: [{index}] = {name}";

            using (new UndoScope(undoName))
            {
                Undo.RecordObject(tagManager.targetObject, undoName);
                var element = layersProp.GetArrayElementAtIndex(index);
                element.stringValue = name;
                tagManager.ApplyModifiedProperties();
            }

            return Ok($"Layer [{index}] = '{name}'", new JObject
            {
                ["index"] = index,
                ["name"] = name,
                ["undoGroupName"] = undoName
            });
#else
            return NotInEditor();
#endif
        }
    }
}
