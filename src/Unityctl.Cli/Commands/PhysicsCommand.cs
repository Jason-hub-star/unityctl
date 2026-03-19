using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class PhysicsCommand
{
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

    public static void GetCollisionMatrix(string project, bool json = false)
    {
        var request = CreateGetCollisionMatrixRequest();
        CommandRunner.Execute(project, request, json);
    }

    public static void SetCollisionMatrix(string project, string layer1, string layer2, string ignore, bool json = false)
    {
        var request = CreateSetCollisionMatrixRequest(layer1, layer2, ignore);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateGetSettingsRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.PhysicsGetSettings,
            Parameters = new JsonObject()
        };
    }

    internal static CommandRequest CreateSetSettingsRequest(string property, string value)
    {
        if (string.IsNullOrWhiteSpace(property))
            throw new ArgumentException("property must not be empty", nameof(property));

        return new CommandRequest
        {
            Command = WellKnownCommands.PhysicsSetSettings,
            Parameters = new JsonObject
            {
                ["property"] = property,
                ["value"] = value
            }
        };
    }

    internal static CommandRequest CreateGetCollisionMatrixRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.PhysicsGetCollisionMatrix,
            Parameters = new JsonObject()
        };
    }

    internal static CommandRequest CreateSetCollisionMatrixRequest(string layer1, string layer2, string ignore)
    {
        if (string.IsNullOrWhiteSpace(layer1))
            throw new ArgumentException("layer1 must not be empty", nameof(layer1));
        if (string.IsNullOrWhiteSpace(layer2))
            throw new ArgumentException("layer2 must not be empty", nameof(layer2));

        return new CommandRequest
        {
            Command = WellKnownCommands.PhysicsSetCollisionMatrix,
            Parameters = new JsonObject
            {
                ["layer1"] = layer1,
                ["layer2"] = layer2,
                ["ignore"] = ignore
            }
        };
    }
}
