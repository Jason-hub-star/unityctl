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
            var isPaused = UnityEditor.EditorApplication.isPaused;
            var isUpdating = UnityEditor.EditorApplication.isUpdating;
            var willChangePlaymode = UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode;

            StatusCode code;
            string state;
            if (isCompiling)
            {
                code = StatusCode.Compiling;
                state = "Compiling";
            }
            else if (isUpdating)
            {
                code = StatusCode.Reloading;
                state = "Reloading";
            }
            else if (isPlaying)
            {
                code = StatusCode.EnteringPlayMode;
                state = isPaused ? "PlayingPaused" : "Playing";
            }
            else if (willChangePlaymode && !isPlaying)
            {
                code = StatusCode.EnteringPlayMode;
                state = "EnteringPlayMode";
            }
            else
            {
                code = StatusCode.Ready;
                state = "Ready";
            }

            var data = new JObject
            {
                ["state"] = state,
                ["isCompiling"] = isCompiling,
                ["isPlaying"] = isPlaying,
                ["isPaused"] = isPaused,
                ["isUpdating"] = isUpdating,
                ["isEnteringPlayMode"] = willChangePlaymode && !isPlaying,
                ["isDomainReloading"] = isUpdating || (willChangePlaymode && !isPlaying),
                ["bridgeLoaded"] = true,
                ["ipcPipePresent"] = true,
                ["projectPath"] = UnityEngine.Application.dataPath,
                ["unityVersion"] = UnityEngine.Application.unityVersion,
                ["platform"] = UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString()
            };

            return Ok(code, state, data);
        }
    }
}
