using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class MeshCommand
{
    public static void CreatePrimitive(string project, string type, string? name = null,
        string? position = null, string? rotation = null, string? scale = null,
        string? material = null, string? parent = null, bool json = false)
    {
        var request = CreatePrimitiveRequest(type, name, position, rotation, scale, material, parent);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreatePrimitiveRequest(string type, string? name = null,
        string? position = null, string? rotation = null, string? scale = null,
        string? material = null, string? parent = null)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("type must not be empty", nameof(type));

        var parameters = new JsonObject { ["type"] = type };
        if (!string.IsNullOrWhiteSpace(name)) parameters["name"] = name;
        if (!string.IsNullOrWhiteSpace(position)) parameters["position"] = position;
        if (!string.IsNullOrWhiteSpace(rotation)) parameters["rotation"] = rotation;
        if (!string.IsNullOrWhiteSpace(scale)) parameters["scale"] = scale;
        if (!string.IsNullOrWhiteSpace(material)) parameters["material"] = material;
        if (!string.IsNullOrWhiteSpace(parent)) parameters["parent"] = parent;

        return new CommandRequest
        {
            Command = WellKnownCommands.MeshCreatePrimitive,
            Parameters = parameters
        };
    }
}
