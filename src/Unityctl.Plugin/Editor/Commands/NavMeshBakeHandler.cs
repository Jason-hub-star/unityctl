#if UNITY_EDITOR
using System;
using System.Reflection;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class NavMeshBakeHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.NavMeshBake;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            if (!TryInvokeEditorNavMeshMethod("BuildNavMesh", out var error))
                return Fail(StatusCode.InvalidParameters, error);

            return Ok("NavMesh bake completed", new JObject
            {
                ["baked"] = true
            });
        }

        private static bool TryInvokeEditorNavMeshMethod(string methodName, out string error)
        {
            error = null;

            var candidateTypes = new[]
            {
                "UnityEditor.AI.NavMeshBuilder, UnityEditor",
                "UnityEngine.AI.NavMeshBuilder, UnityEngine.AIModule"
            };

            foreach (var candidateType in candidateTypes)
            {
                var type = Type.GetType(candidateType, throwOnError: false);
                if (type == null) continue;

                var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                if (method == null) continue;

                method.Invoke(null, null);
                return true;
            }

            error = $"NavMesh bake is not supported by the current Unity API surface. Missing method: {methodName}.";
            return false;
        }
    }
}
#endif
