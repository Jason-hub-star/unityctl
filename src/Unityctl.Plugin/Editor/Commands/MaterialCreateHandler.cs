using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;
namespace Unityctl.Plugin.Editor.Commands
{
    public class MaterialCreateHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.MaterialCreate;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var path = request.GetParam("path", null);
            if (string.IsNullOrEmpty(path))
                return InvalidParameters("Parameter 'path' is required.");

            var shaderName = request.GetParam("shader", "Standard");

            var shader = UnityEngine.Shader.Find(shaderName);
            if (shader == null)
                return Fail(StatusCode.InvalidParameters,
                    $"Shader not found: '{shaderName}'. Common shaders: Standard, Universal Render Pipeline/Lit, Unlit/Color");

            var material = new UnityEngine.Material(shader);
            material.name = System.IO.Path.GetFileNameWithoutExtension(path);

            // Ensure parent directory exists
            var dir = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !UnityEditor.AssetDatabase.IsValidFolder(dir))
            {
                // Create parent folders recursively
                var parts = dir.Replace("\\", "/").Split('/');
                var current = parts[0]; // "Assets"
                for (int i = 1; i < parts.Length; i++)
                {
                    var next = current + "/" + parts[i];
                    if (!UnityEditor.AssetDatabase.IsValidFolder(next))
                        UnityEditor.AssetDatabase.CreateFolder(current, parts[i]);
                    current = next;
                }
            }

            UnityEditor.AssetDatabase.CreateAsset(material, path);
            UnityEditor.AssetDatabase.SaveAssets();

            var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid))
                return Fail(StatusCode.UnknownError, $"Material created but GUID lookup failed for: {path}");

            return Ok($"Created Material at '{path}' with shader '{shaderName}'", new JObject
            {
                ["path"] = path,
                ["guid"] = guid,
                ["shader"] = shaderName
            });
#else
            return NotInEditor();
#endif
        }
    }
}
