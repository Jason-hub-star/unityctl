using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class BuildProfileCommand
{
    public static void List(string project, bool json = false)
    {
        var request = CreateListRequest();
        CommandRunner.Execute(project, request, json);
    }

    public static void GetActive(string project, bool json = false)
    {
        var request = CreateGetActiveRequest();
        CommandRunner.Execute(project, request, json);
    }

    public static void SetActive(string project, string profile, int timeout = 900, bool json = false)
    {
        var exitCode = SetActiveAsync(project, profile, timeout, json).GetAwaiter().GetResult();
        Environment.Exit(exitCode);
    }

    internal static async Task<int> SetActiveAsync(string project, string profile, int timeout, bool json)
    {
        if (timeout <= 0)
            throw new ArgumentOutOfRangeException(nameof(timeout), "timeout must be greater than zero");

        var request = CreateSetActiveRequest(profile);
        var response = await IpcOnlyAsyncCommandRunner.ExecuteAsync(
            project,
            request,
            pollCommand: WellKnownCommands.BuildProfileSetActiveResult,
            timeoutSeconds: timeout,
            reconnectMessage: "Waiting for Unity Editor IPC server to reconnect after build profile switch...");

        CommandRunner.PrintResponse(project, response, json);
        return CommandRunner.GetExitCode(response);
    }

    internal static CommandRequest CreateListRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.BuildProfileList,
            Parameters = new JsonObject()
        };
    }

    internal static CommandRequest CreateGetActiveRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.BuildProfileGetActive,
            Parameters = new JsonObject()
        };
    }

    internal static CommandRequest CreateSetActiveRequest(string profile)
    {
        if (string.IsNullOrWhiteSpace(profile))
            throw new ArgumentException("profile must not be empty", nameof(profile));

        return new CommandRequest
        {
            Command = WellKnownCommands.BuildProfileSetActive,
            Parameters = new JsonObject
            {
                ["profile"] = profile
            }
        };
    }
}
