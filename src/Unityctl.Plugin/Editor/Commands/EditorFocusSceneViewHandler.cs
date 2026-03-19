#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class EditorFocusSceneViewHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.EditorFocusSceneView;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var sceneView = UnityEditor.SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                return Fail(StatusCode.NotFound, "No active SceneView found");
            }

            sceneView.Focus();

            return Ok("Scene View focused", new JObject
            {
                ["focused"] = true
            });
        }
    }
}
#endif
