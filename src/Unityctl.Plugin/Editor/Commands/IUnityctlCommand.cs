using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public interface IUnityctlCommand
    {
        string CommandName { get; }
        CommandResponse Execute(CommandRequest request);
    }
}
