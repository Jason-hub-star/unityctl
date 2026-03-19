using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Build;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class DefineSymbolsGetHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.DefineSymbolsGet;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
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

            PlayerSettings.GetScriptingDefineSymbols(namedTarget, out var symbols);

            var arr = new JArray();
            foreach (var s in symbols)
                arr.Add(s);

            return Ok($"Define symbols: {string.Join(";", symbols)}", new JObject
            {
                ["target"] = namedTarget.TargetName,
                ["symbols"] = arr,
                ["count"] = symbols.Length,
                ["raw"] = string.Join(";", symbols)
            });
#else
            return NotInEditor();
#endif
        }
    }
}
