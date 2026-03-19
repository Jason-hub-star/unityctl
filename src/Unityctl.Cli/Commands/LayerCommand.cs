using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class LayerCommand
{
    public static void List(string project, bool json = false)
    {
        var request = CreateListRequest();
        CommandRunner.Execute(project, request, json);
    }

    public static void Set(string project, int index, string name, bool json = false)
    {
        var request = CreateSetRequest(index, name);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateListRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.LayerList,
            Parameters = new JsonObject()
        };
    }

    internal static CommandRequest CreateSetRequest(int index, string name)
    {
        if (index < 0 || index > 31)
            throw new ArgumentException("index must be between 0 and 31", nameof(index));
        if (index < 8)
            throw new ArgumentException("Built-in layers 0-7 cannot be modified", nameof(index));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("name must not be empty", nameof(name));

        return new CommandRequest
        {
            Command = WellKnownCommands.LayerSet,
            Parameters = new JsonObject
            {
                ["index"] = index,
                ["name"] = name
            }
        };
    }
}
