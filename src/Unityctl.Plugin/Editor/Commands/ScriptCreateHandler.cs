using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ScriptCreateHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ScriptCreate;

        private static readonly System.Collections.Generic.Dictionary<string, string> KnownUsings =
            new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                { "MonoBehaviour", "using UnityEngine;" },
                { "ScriptableObject", "using UnityEngine;" },
                { "Editor", "using UnityEditor;" },
                { "EditorWindow", "using UnityEditor;" }
            };

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var path = request.GetParam("path", null);
            var className = request.GetParam("className", null);
            var ns = request.GetParam("namespace", null);
            var baseType = request.GetParam("baseType", "MonoBehaviour");

            if (string.IsNullOrEmpty(path))
                return InvalidParameters("Parameter 'path' is required.");
            if (string.IsNullOrEmpty(className))
                return InvalidParameters("Parameter 'className' is required.");

            // Validate .cs extension
            if (!path.EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase))
                return InvalidParameters("Path must end with .cs");

            // Validate Assets/ prefix
            if (!path.StartsWith("Assets/") && !path.StartsWith("Assets\\"))
                return InvalidParameters("Path must be under Assets/");

            // Validate filename matches className
            var fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (!string.Equals(fileName, className, System.StringComparison.Ordinal))
                return InvalidParameters($"Filename '{fileName}' does not match className '{className}'");

            // Check file doesn't already exist
            if (System.IO.File.Exists(
                System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath),
                    path)))
                return Fail(StatusCode.InvalidParameters, $"File already exists: {path}");

            // Generate source
            var source = GenerateSource(className, ns, baseType);

            // Ensure directory exists
            var fullPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(UnityEngine.Application.dataPath), path);
            var dir = System.IO.Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            System.IO.File.WriteAllText(fullPath, source, System.Text.Encoding.UTF8);
            UnityEditor.AssetDatabase.ImportAsset(path);

            return Ok($"Created script at '{path}'", new JObject
            {
                ["path"] = path,
                ["className"] = className,
                ["baseType"] = baseType,
                ["namespace"] = ns
            });
#else
            return NotInEditor();
#endif
        }

        private static string GenerateSource(string className, string ns, string baseType)
        {
            var sb = new System.Text.StringBuilder();

            // Using directive for known types
            if (KnownUsings.TryGetValue(baseType, out var usingDirective))
            {
                sb.AppendLine(usingDirective);
                sb.AppendLine();
            }

            if (!string.IsNullOrEmpty(ns))
            {
                sb.AppendLine($"namespace {ns}");
                sb.AppendLine("{");
                sb.AppendLine($"    public class {className} : {baseType}");
                sb.AppendLine("    {");
                sb.AppendLine("    }");
                sb.AppendLine("}");
            }
            else
            {
                sb.AppendLine($"public class {className} : {baseType}");
                sb.AppendLine("{");
                sb.AppendLine("}");
            }

            return sb.ToString();
        }
    }
}
