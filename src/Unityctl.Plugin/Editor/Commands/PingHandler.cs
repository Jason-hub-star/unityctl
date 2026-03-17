using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class PingHandler : IUnityctlCommand
    {
        public string CommandName => "ping";

        public CommandResponse Execute(CommandRequest request)
        {
            return CommandResponse.Ok("pong", new System.Collections.Generic.Dictionary<string, object>
            {
                ["version"] = "0.1.0",
#if UNITY_EDITOR
                ["unityVersion"] = UnityEngine.Application.unityVersion
#endif
            });
        }
    }
}
