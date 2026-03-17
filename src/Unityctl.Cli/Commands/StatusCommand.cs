using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class StatusCommand
{
    public static void Execute(string project, bool wait = false, bool json = false)
    {
        var request = new CommandRequest { Command = WellKnownCommands.Status };
        CommandRunner.Execute(project, request, json, retry: wait);
    }
}
