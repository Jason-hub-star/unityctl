using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class NavMeshCommand
{
    public static void Bake(string project, bool json = false)
    {
        var request = CreateBakeRequest();
        CommandRunner.Execute(project, request, json);
    }

    public static void Clear(string project, bool json = false)
    {
        var request = CreateClearRequest();
        CommandRunner.Execute(project, request, json);
    }

    public static void GetSettings(string project, bool json = false)
    {
        var request = CreateGetSettingsRequest();
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateBakeRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.NavMeshBake,
            Parameters = new JsonObject()
        };
    }

    internal static CommandRequest CreateClearRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.NavMeshClear,
            Parameters = new JsonObject()
        };
    }

    internal static CommandRequest CreateGetSettingsRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.NavMeshGetSettings,
            Parameters = new JsonObject()
        };
    }
}
