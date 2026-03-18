using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ScriptDeleteHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ScriptDelete;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var path = request.GetParam("path", null);

            if (string.IsNullOrEmpty(path))
                return InvalidParameters("Parameter 'path' is required.");

            // Validate .cs extension
            if (!path.EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase))
                return InvalidParameters("Path must end with .cs");

            // Validate Assets/ prefix
            if (!path.StartsWith("Assets/") && !path.StartsWith("Assets\\"))
                return InvalidParameters("Path must be under Assets/. Packages/ is not allowed.");

            // Check file exists
            var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
                return Fail(StatusCode.NotFound, $"Script not found: {path}");

            var deleted = UnityEditor.AssetDatabase.DeleteAsset(path);
            if (!deleted)
                return Fail(StatusCode.UnknownError, $"Failed to delete: {path}");

            return Ok($"Deleted script at '{path}'", new JObject
            {
                ["path"] = path,
                ["deleted"] = true
            });
#else
            return NotInEditor();
#endif
        }
    }
}
