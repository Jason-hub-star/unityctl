using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class LightingCommand
{
    public static void Bake(string project, int timeout = 3600, bool json = false)
    {
        var exitCode = BakeAsync(project, timeout, json).GetAwaiter().GetResult();
        Environment.Exit(exitCode);
    }

    internal static async Task<int> BakeAsync(string project, int timeout, bool json)
    {
        if (timeout <= 0)
            throw new ArgumentOutOfRangeException(nameof(timeout), "timeout must be greater than zero");

        var request = CreateBakeRequest();

        var response = await IpcOnlyAsyncCommandRunner.ExecuteAsync(
            project,
            request,
            pollCommand: WellKnownCommands.LightingBakeResult,
            timeoutSeconds: timeout,
            reconnectMessage: "Waiting for lighting bake to complete...");

        CommandRunner.PrintResponse(project, response, json);
        return CommandRunner.GetExitCode(response);
    }

    public static void Cancel(string project, bool json = false)
    {
        var request = CreateCancelRequest();
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

    public static void SetSettings(string project, string property, string value, bool json = false)
    {
        var request = CreateSetSettingsRequest(property, value);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateBakeRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.LightingBake,
            Parameters = new JsonObject()
        };
    }

    internal static CommandRequest CreateCancelRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.LightingCancel,
            Parameters = new JsonObject()
        };
    }

    internal static CommandRequest CreateClearRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.LightingClear,
            Parameters = new JsonObject()
        };
    }

    internal static CommandRequest CreateGetSettingsRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.LightingGetSettings,
            Parameters = new JsonObject()
        };
    }

    internal static CommandRequest CreateSetSettingsRequest(string property, string value)
    {
        if (string.IsNullOrWhiteSpace(property))
            throw new ArgumentException("property must not be empty", nameof(property));

        return new CommandRequest
        {
            Command = WellKnownCommands.LightingSetSettings,
            Parameters = new JsonObject
            {
                ["property"] = property,
                ["value"] = value
            }
        };
    }
}
