using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class TagCommand
{
    public static void List(string project, bool json = false)
    {
        var request = CreateListRequest();
        CommandRunner.Execute(project, request, json);
    }

    public static void Add(string project, string name, bool json = false)
    {
        var request = CreateAddRequest(name);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateListRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.TagList,
            Parameters = new JsonObject()
        };
    }

    internal static CommandRequest CreateAddRequest(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("name must not be empty", nameof(name));

        return new CommandRequest
        {
            Command = WellKnownCommands.TagAdd,
            Parameters = new JsonObject { ["name"] = name }
        };
    }
}
