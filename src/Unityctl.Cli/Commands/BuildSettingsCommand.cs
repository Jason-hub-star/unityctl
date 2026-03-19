using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class BuildSettingsCommand
{
    public static void GetScenes(string project, bool json = false)
    {
        var request = CreateGetScenesRequest();
        CommandRunner.Execute(project, request, json);
    }

    public static void SetScenes(string project, string scenes, bool json = false)
    {
        var request = CreateSetScenesRequest(scenes);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateGetScenesRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.BuildSettingsGetScenes,
            Parameters = new JsonObject()
        };
    }

    internal static CommandRequest CreateSetScenesRequest(string scenes)
    {
        if (string.IsNullOrWhiteSpace(scenes))
            throw new ArgumentException("scenes must not be empty", nameof(scenes));

        return new CommandRequest
        {
            Command = WellKnownCommands.BuildSettingsSetScenes,
            Parameters = new JsonObject { ["scenes"] = scenes }
        };
    }
}
