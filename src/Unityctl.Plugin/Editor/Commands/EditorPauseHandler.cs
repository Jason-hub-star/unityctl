#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class EditorPauseHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.EditorPause;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var action = request.GetParam("action", "toggle");

            switch (action?.ToLowerInvariant())
            {
                case "pause":
                    UnityEditor.EditorApplication.isPaused = true;
                    break;
                case "unpause":
                    UnityEditor.EditorApplication.isPaused = false;
                    break;
                case "toggle":
                default:
                    UnityEditor.EditorApplication.isPaused = !UnityEditor.EditorApplication.isPaused;
                    break;
            }

            var isPaused = UnityEditor.EditorApplication.isPaused;

            return Ok(isPaused ? "Editor paused" : "Editor unpaused", new JObject
            {
                ["isPaused"] = isPaused
            });
        }
    }
}
#endif
