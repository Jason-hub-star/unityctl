using Unityctl.Cli.Output;
using Unityctl.Core.Discovery;
using Unityctl.Core.Platform;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Execution;

public static class CommandRunner
{
    public static void Execute(string project, CommandRequest request, bool json = false, bool retry = false)
    {
        var exitCode = ExecuteAsync(project, request, json, retry).GetAwaiter().GetResult();
        Environment.Exit(exitCode);
    }

    internal static async Task<int> ExecuteAsync(string project, CommandRequest request, bool json, bool retry)
    {
        var platform = PlatformFactory.Create();
        var discovery = new UnityEditorDiscovery(platform);
        var executor = new CommandExecutor(platform, discovery);

        var response = await executor.ExecuteAsync(project, request, retry: retry);
        PrintResponse(response, json);

        return GetExitCode(response);
    }

    internal static void PrintResponse(CommandResponse response, bool json)
    {
        if (json)
        {
            JsonOutput.PrintResponse(response);
            return;
        }

        ConsoleOutput.PrintResponse(response);
        if (!response.Success)
            ConsoleOutput.PrintRecovery(response.StatusCode);
    }

    internal static int GetExitCode(CommandResponse response)
        => response.Success ? 0 : 1;
}
