using Unityctl.Plugin.Editor.Shared;
using System.Collections.Generic;

namespace Unityctl.Plugin.Editor.Commands
{
    public class StatusHandler : IUnityctlCommand
    {
        public string CommandName => "status";

        public CommandResponse Execute(CommandRequest request)
        {
#if UNITY_EDITOR
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

            var data = new Dictionary<string, object>
            {
                ["isCompiling"] = isCompiling,
                ["isPlaying"] = isPlaying,
                ["isUpdating"] = isUpdating,
                ["projectPath"] = UnityEngine.Application.dataPath,
                ["unityVersion"] = UnityEngine.Application.unityVersion,
                ["platform"] = UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString()
            };

            return new CommandResponse
            {
                statusCode = (int)code,
                success = true,
                message = code.ToString(),
                data = data
            };
#else
            return CommandResponse.Fail(StatusCode.UnknownError, "Not running in Unity Editor");
#endif
        }
    }
}
