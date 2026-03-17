using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class BuildCommand
{
    public static void Execute(string project, string target = "StandaloneWindows64", string? output = null, bool json = false)
    {
        var request = new CommandRequest
        {
            Command = WellKnownCommands.Build,
            Parameters = new JsonObject
            {
                ["target"] = target,
                ["outputPath"] = output
            }
        };

        CommandRunner.Execute(project, request, json);
    }
}
