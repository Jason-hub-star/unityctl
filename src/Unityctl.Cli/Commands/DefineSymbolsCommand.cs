using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class DefineSymbolsCommand
{
    public static void Get(string project, string? target = null, bool json = false)
    {
        var request = CreateGetRequest(target);
        CommandRunner.Execute(project, request, json);
    }

    public static void Set(string project, string symbols, string? target = null, bool json = false)
    {
        var request = CreateSetRequest(symbols, target);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateGetRequest(string? target)
    {
        var parameters = new JsonObject();
        if (!string.IsNullOrWhiteSpace(target)) parameters["target"] = target;

        return new CommandRequest
        {
            Command = WellKnownCommands.DefineSymbolsGet,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateSetRequest(string symbols, string? target)
    {
        if (symbols is null)
            throw new ArgumentNullException(nameof(symbols));

        var parameters = new JsonObject { ["symbols"] = symbols };
        if (!string.IsNullOrWhiteSpace(target)) parameters["target"] = target;

        return new CommandRequest
        {
            Command = WellKnownCommands.DefineSymbolsSet,
            Parameters = parameters
        };
    }
}
