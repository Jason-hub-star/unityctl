using Unityctl.Plugin.Editor.Commands;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Ipc
{
    /// <summary>
    /// Routes incoming IPC requests to the appropriate command handler.
    /// Used by both IPC server (Phase 2) and BatchMode entry point.
    /// </summary>
    public static class IpcRequestRouter
    {
        public static CommandResponse Route(CommandRequest request)
        {
            return CommandRegistry.Dispatch(request);
        }

        public static CommandResponse Route(string command, CommandRequest request)
        {
            request.command = command;
            return CommandRegistry.Dispatch(request);
        }
    }
}
