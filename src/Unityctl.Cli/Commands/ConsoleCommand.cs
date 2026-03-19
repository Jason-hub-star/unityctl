using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class ConsoleCommand
{
    public static void Clear(string project, bool json = false)
    {
        var request = CreateClearRequest();
        CommandRunner.Execute(project, request, json);
    }

    public static void GetCount(string project, bool json = false)
    {
        var request = CreateGetCountRequest();
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateClearRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.ConsoleClear,
            Parameters = new JsonObject()
        };
    }

    internal static CommandRequest CreateGetCountRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.ConsoleGetCount,
            Parameters = new JsonObject()
        };
    }
}
