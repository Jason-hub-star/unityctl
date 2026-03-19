using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class ComponentCommand
{
    public static void Get(string project, string componentId, string? property = null, bool json = false)
    {
        var request = CreateGetRequest(componentId, property);
        CommandRunner.Execute(project, request, json);
    }

    public static void Add(string project, string id, string type, bool json = false)
    {
        var request = CreateAddRequest(id, type);
        CommandRunner.Execute(project, request, json);
    }

    public static void Remove(string project, string componentId, bool json = false)
    {
        var request = CreateRemoveRequest(componentId);
        CommandRunner.Execute(project, request, json);
    }

    public static void SetProperty(string project, string componentId, string property, string value, bool json = false)
    {
        var request = CreateSetPropertyRequest(componentId, property, value);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateGetRequest(string componentId, string? property)
    {
        if (string.IsNullOrWhiteSpace(componentId))
            throw new ArgumentException("componentId must not be empty", nameof(componentId));

        var parameters = new JsonObject
        {
            ["componentId"] = componentId
        };

        if (!string.IsNullOrWhiteSpace(property))
            parameters["property"] = property;

        return new CommandRequest
        {
            Command = WellKnownCommands.ComponentGet,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateAddRequest(string id, string type)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("id must not be empty", nameof(id));
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("type must not be empty", nameof(type));

        return new CommandRequest
        {
            Command = WellKnownCommands.ComponentAdd,
            Parameters = new JsonObject
            {
                ["id"] = id,
                ["type"] = type
            }
        };
    }

    internal static CommandRequest CreateRemoveRequest(string componentId)
    {
        if (string.IsNullOrWhiteSpace(componentId))
            throw new ArgumentException("componentId must not be empty", nameof(componentId));

        return new CommandRequest
        {
            Command = WellKnownCommands.ComponentRemove,
            Parameters = new JsonObject { ["componentId"] = componentId }
        };
    }

    internal static CommandRequest CreateSetPropertyRequest(string componentId, string property, string value)
    {
        if (string.IsNullOrWhiteSpace(componentId))
            throw new ArgumentException("componentId must not be empty", nameof(componentId));
        if (string.IsNullOrWhiteSpace(property))
            throw new ArgumentException("property must not be empty", nameof(property));
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        return new CommandRequest
        {
            Command = WellKnownCommands.ComponentSetProperty,
            Parameters = new JsonObject
            {
                ["componentId"] = componentId,
                ["property"] = property,
                ["value"] = value
            }
        };
    }
}
