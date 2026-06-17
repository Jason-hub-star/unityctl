#nullable enable

using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

/// <summary>
/// Describes a C# type by reflecting on its members, Unity specifics, and generating documentation links.
/// </summary>
public static class TypeCommand
{
    /// <summary>
    /// Describe a live C# type from the Unity Editor.
    /// </summary>
    /// <param name="project">Unity project path</param>
    /// <param name="typeName">Fully-qualified or simple type name</param>
    /// <param name="full">Return full member signatures (default: false for summary mode)</param>
    /// <param name="maxMembers">Cap members per category (optional)</param>
    /// <param name="json">Output as JSON</param>
    public static void Describe(string project, string typeName, bool full = false, int? maxMembers = null, bool json = false)
    {
        var request = CreateDescribeRequest(typeName, full, maxMembers);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateDescribeRequest(string typeName, bool full = false, int? maxMembers = null)
    {
        if (string.IsNullOrWhiteSpace(typeName))
            throw new ArgumentException("typeName must not be empty", nameof(typeName));

        var parameters = new JsonObject { ["typeName"] = typeName };

        if (full)
            parameters["full"] = true;
        if (maxMembers.HasValue)
            parameters["maxMembers"] = maxMembers.Value;

        return new CommandRequest
        {
            Command = WellKnownCommands.DescribeType,
            Parameters = parameters
        };
    }
}
