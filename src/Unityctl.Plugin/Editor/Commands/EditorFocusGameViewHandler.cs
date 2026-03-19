#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class EditorFocusGameViewHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.EditorFocusGameView;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var success = UnityEditor.EditorApplication.ExecuteMenuItem("Window/General/Game");
            if (!success)
            {
                return Fail(StatusCode.UnknownError, "Failed to focus Game View via menu item");
            }

            return Ok("Game View focused", new JObject
            {
                ["focused"] = true
            });
        }
    }
}
#endif
