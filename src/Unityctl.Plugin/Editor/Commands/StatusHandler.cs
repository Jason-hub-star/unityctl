using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class StatusHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.Status;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var isCompiling = UnityEditor.EditorApplication.isCompiling;
            var isPlaying = UnityEditor.EditorApplication.isPlaying;
            var isUpdating = UnityEditor.EditorApplication.isUpdating;

            StatusCode code;
            if (isCompiling)
                code = StatusCode.Compiling;
            else if (isUpdating)
                code = StatusCode.Reloading;
            else if (isPlaying)
                code = StatusCode.EnteringPlayMode;
            else
                code = StatusCode.Ready;

            var data = new JObject
            {
                ["isCompiling"] = isCompiling,
                ["isPlaying"] = isPlaying,
                ["isUpdating"] = isUpdating,
                ["projectPath"] = UnityEngine.Application.dataPath,
                ["unityVersion"] = UnityEngine.Application.unityVersion,
                ["platform"] = UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString()
            };

            return Ok(code, code.ToString(), data);
        }
    }
}
