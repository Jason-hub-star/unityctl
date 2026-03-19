using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class BuildTargetCommand
{
    public static void Switch(string project, string target, int timeout = 900, bool json = false)
    {
        var exitCode = SwitchAsync(project, target, timeout, json).GetAwaiter().GetResult();
        Environment.Exit(exitCode);
    }

    internal static async Task<int> SwitchAsync(string project, string target, int timeout, bool json)
    {
        if (timeout <= 0)
            throw new ArgumentOutOfRangeException(nameof(timeout), "timeout must be greater than zero");

        var request = CreateSwitchRequest(target);
        var response = await IpcOnlyAsyncCommandRunner.ExecuteAsync(
            project,
            request,
            pollCommand: WellKnownCommands.BuildTargetSwitchResult,
            timeoutSeconds: timeout,
            reconnectMessage: "Waiting for Unity Editor IPC server to reconnect after build target switch...");

        CommandRunner.PrintResponse(project, response, json);
        return CommandRunner.GetExitCode(response);
    }

    internal static CommandRequest CreateSwitchRequest(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
            throw new ArgumentException("target must not be empty", nameof(target));

        return new CommandRequest
        {
            Command = WellKnownCommands.BuildTargetSwitch,
            Parameters = new JsonObject
            {
                ["target"] = target
            }
        };
    }
}
