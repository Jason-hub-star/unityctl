using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Cli.Output;
using Unityctl.Core.Discovery;
using Unityctl.Core.Platform;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class TestCommand
{
    public static void Execute(
        string project,
        string mode = "edit",
        string? filter = null,
        bool wait = true,
        int timeout = 300,
        bool json = false)
    {
        var exitCode = ExecuteAsync(project, mode, filter, wait, timeout, json).GetAwaiter().GetResult();
        Environment.Exit(exitCode);
    }

    public static void Result(string project, string requestId, bool json = false)
    {
        var request = CreateResultRequest(requestId);
        CommandRunner.Execute(project, request, json, retry: false);
    }

    internal static async Task<int> ExecuteAsync(
        string project,
        string mode,
        string? filter,
        bool wait,
        int timeout,
        bool json)
    {
        var isPlayMode = mode.Equals("play", StringComparison.OrdinalIgnoreCase)
                         || mode.Equals("playmode", StringComparison.OrdinalIgnoreCase);

        // PlayMode + --wait: force no-wait with warning
        if (isPlayMode && wait)
        {
            Console.Error.WriteLine(
                "[unityctl] Warning: PlayMode tests are started asynchronously. Use `unityctl test-result --project <project> --request-id <id> --json` to poll results.");
            wait = false;
        }

        var request = new CommandRequest
        {
            Command = WellKnownCommands.Test,
            Parameters = new JsonObject
            {
                ["mode"] = mode,
                ["filter"] = filter
            }
        };

        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var executor = new CommandExecutor(platform, discovery);

        CommandResponse response;

        if (wait)
        {
            response = await AsyncCommandRunner.ExecuteAsync(
                project,
                request,
                async (proj, req, ct) => await executor.ExecuteAsync(proj, req, ct: ct),
                pollCommand: WellKnownCommands.TestResult,
                timeoutSeconds: timeout);
        }
        else
        {
            response = await executor.ExecuteAsync(project, request);
        }

        CommandRunner.PrintResponse(response, json);
        return CommandRunner.GetExitCode(response);
    }

    internal static CommandRequest CreateResultRequest(string requestId)
    {
        if (string.IsNullOrWhiteSpace(requestId))
            throw new ArgumentException("requestId must not be empty", nameof(requestId));

        return new CommandRequest
        {
            Command = WellKnownCommands.TestResult,
            Parameters = new JsonObject
            {
                ["requestId"] = requestId
            }
        };
    }
}
