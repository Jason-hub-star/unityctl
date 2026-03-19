using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class DefineSymbolsSetHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.DefineSymbolsSet;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var symbolsParam = request.GetParam("symbols", null);
            if (symbolsParam == null)
                return InvalidParameters("Parameter 'symbols' is required.");

            var targetParam = request.GetParam("target", null);

            NamedBuildTarget namedTarget;
            if (!string.IsNullOrEmpty(targetParam))
            {
                namedTarget = NamedBuildTarget.FromBuildTargetGroup(
                    (BuildTargetGroup)System.Enum.Parse(typeof(BuildTargetGroup), targetParam, true));
            }
            else
            {
                namedTarget = NamedBuildTarget.FromBuildTargetGroup(
                    BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
            }

            PlayerSettings.SetScriptingDefineSymbols(namedTarget, symbolsParam);

            // Read back the symbols to confirm
            PlayerSettings.GetScriptingDefineSymbols(namedTarget, out var confirmed);

            var arr = new JArray();
            foreach (var s in confirmed)
                arr.Add(s);

            return Ok($"Define symbols set: {symbolsParam}", new JObject
            {
                ["target"] = namedTarget.TargetName,
                ["symbols"] = arr,
                ["count"] = confirmed.Length,
                ["raw"] = string.Join(";", confirmed)
            });
#else
            return NotInEditor();
#endif
        }
    }
}
