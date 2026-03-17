using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class CheckHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.Check;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var type = request.GetParam("type", "compile");

            if (type != "compile")
            {
                return InvalidParameters(
                    $"Unknown check type: {type}. Currently only 'compile' is supported.");
            }

            var assemblyNames = UnityEditor.Compilation.CompilationPipeline
                .GetAssemblies(UnityEditor.Compilation.AssembliesType.Player)
                .Select(a => a.name)
                .ToArray();

            var isCompiling = UnityEditor.EditorApplication.isCompiling;
            var scriptCompilationFailed = UnityEditor.EditorUtility.scriptCompilationFailed;
            var data = new JObject
            {
                ["assemblies"] = assemblyNames.Length,
                ["assemblyNames"] = string.Join(", ", assemblyNames.Take(10)),
                ["isCompiling"] = isCompiling,
                ["scriptCompilationFailed"] = scriptCompilationFailed
            };

            if (isCompiling)
            {
                return Fail(
                    StatusCode.Compiling,
                    "Script compilation is still in progress.",
                    data);
            }

            if (scriptCompilationFailed)
            {
                return Fail(
                    StatusCode.UnknownError,
                    "Compilation check failed. Resolve Unity compiler errors and try again.",
                    data);
            }

            return Ok("Compilation check passed", data);
        }

        protected override CommandResponse HandleException(Exception exception)
        {
            return Fail(
                StatusCode.UnknownError,
                $"Compile check failed: {exception.Message}",
                errors: GetStackTrace(exception));
        }
    }
}
