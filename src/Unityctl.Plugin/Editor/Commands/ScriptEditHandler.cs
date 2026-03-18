using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ScriptEditHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ScriptEdit;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var path = request.GetParam("path", null);
            var content = request.GetParam("content", null);

            if (string.IsNullOrEmpty(path))
                return InvalidParameters("Parameter 'path' is required.");
            if (content == null)
                return InvalidParameters("Parameter 'content' is required.");

            // Validate .cs extension
            if (!path.EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase))
                return InvalidParameters("Path must end with .cs");

            // Validate Assets/ prefix
            if (!path.StartsWith("Assets/") && !path.StartsWith("Assets\\"))
                return InvalidParameters("Path must be under Assets/");

            // Check file exists
            var fullPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath), path);
            if (!System.IO.File.Exists(fullPath))
                return Fail(StatusCode.NotFound, $"Script not found: {path}");

            System.IO.File.WriteAllText(fullPath, content, System.Text.Encoding.UTF8);
            UnityEditor.AssetDatabase.ImportAsset(path);

            return Ok($"Updated script at '{path}'", new JObject
            {
                ["path"] = path,
                ["bytesWritten"] = System.Text.Encoding.UTF8.GetByteCount(content)
            });
#else
            return NotInEditor();
#endif
        }
    }
}
