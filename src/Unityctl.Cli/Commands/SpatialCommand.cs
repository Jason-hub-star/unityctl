using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

/// <summary>
/// Spatial grounding commands: turn scene geometry into measured facts
/// (world bounds, orientation, spatial predicates) so an agent can reason
/// about space without a screenshot.
/// </summary>
public static class SpatialCommand
{
    internal static readonly string[] ValidPredicates =
        ["covers", "inside", "on-top-of", "overlaps", "aligned"];

    public static void Describe(string project, string target, bool full = false, bool json = false)
    {
        var request = CreateDescribeRequest(target, full);
        CommandRunner.Execute(project, request, json);
    }

    public static void Check(string project, string subject, string predicate, string target, bool json = false)
    {
        var request = CreateCheckRequest(subject, predicate, target);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateDescribeRequest(string target, bool full = false)
    {
        if (string.IsNullOrWhiteSpace(target))
            throw new ArgumentException("target must not be empty", nameof(target));

        var parameters = new JsonObject { ["target"] = target };
        if (full) parameters["full"] = true;

        return new CommandRequest
        {
            Command = WellKnownCommands.SpatialDescribe,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateCheckRequest(string subject, string predicate, string target)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("subject must not be empty", nameof(subject));
        if (string.IsNullOrWhiteSpace(target))
            throw new ArgumentException("target must not be empty", nameof(target));
        if (string.IsNullOrWhiteSpace(predicate))
            throw new ArgumentException("predicate must not be empty", nameof(predicate));

        var normalized = predicate.Trim().ToLowerInvariant();
        if (Array.IndexOf(ValidPredicates, normalized) < 0)
            throw new ArgumentException(
                $"predicate must be one of: {string.Join(", ", ValidPredicates)}", nameof(predicate));

        return new CommandRequest
        {
            Command = WellKnownCommands.SpatialCheck,
            Parameters = new JsonObject
            {
                ["subject"] = subject,
                ["predicate"] = normalized,
                ["target"] = target
            }
        };
    }
}
