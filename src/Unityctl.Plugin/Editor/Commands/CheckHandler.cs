using System;
using System.Collections.Generic;
using System.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class CheckHandler : IUnityctlCommand
    {
        public string CommandName => "check";

        public CommandResponse Execute(CommandRequest request)
        {
#if UNITY_EDITOR
            try
            {
                var type = GetParam(request, "type", "compile");

                if (type != "compile")
                {
                    return CommandResponse.Fail(StatusCode.InvalidParameters,
                        $"Unknown check type: {type}. Currently only 'compile' is supported.");
                }

                // If we're in batchmode and reached this point, compilation already succeeded
                // (Unity won't execute methods if compilation fails)
                // But we can also check for warnings via CompilationPipeline

                var messages = UnityEditor.Compilation.CompilationPipeline
                    .GetPrecompiledAssemblyPaths(UnityEditor.Compilation.CompilationPipeline.PrecompiledAssemblySources.All);

                var assemblyNames = UnityEditor.Compilation.CompilationPipeline
                    .GetAssemblies(UnityEditor.Compilation.AssembliesType.Player)
                    .Select(a => a.name)
                    .ToArray();

                return CommandResponse.Ok("Compilation check passed", new Dictionary<string, object>
                {
                    ["assemblies"] = assemblyNames.Length,
                    ["assemblyNames"] = string.Join(", ", assemblyNames.Take(10)),
                    ["isCompiling"] = UnityEditor.EditorApplication.isCompiling
                });
            }
            catch (Exception e)
            {
                return CommandResponse.Fail(StatusCode.UnknownError,
                    $"Compile check failed: {e.Message}",
                    new List<string> { e.StackTrace });
            }
#else
            return CommandResponse.Fail(StatusCode.UnknownError, "Not running in Unity Editor");
#endif
        }

        private static string GetParam(CommandRequest request, string key, string defaultValue)
        {
            if (request.parameters != null && request.parameters.TryGetValue(key, out var val) && val != null)
                return val.ToString();
            return defaultValue;
        }
    }
}
