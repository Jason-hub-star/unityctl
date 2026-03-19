using System.Text.Json;
using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class BatchCommand
{
    public static void Execute(
        string project,
        string? commands = null,
        string? file = null,
        bool rollbackOnFailure = true,
        bool json = false)
    {
        var request = CreateExecuteRequest(commands, file, rollbackOnFailure);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateExecuteRequest(
        string? commandsJson,
        string? file,
        bool rollbackOnFailure)
    {
        var hasCommands = !string.IsNullOrWhiteSpace(commandsJson);
        var hasFile = !string.IsNullOrWhiteSpace(file);

        if (hasCommands == hasFile)
            throw new ArgumentException("Specify exactly one of commands or file.");

        var commands = hasCommands
            ? ParseCommands(commandsJson!)
            : ParseCommands(File.ReadAllText(file!));

        if (commands.Count == 0)
            throw new ArgumentException("commands must contain at least one command.", nameof(commandsJson));

        return new CommandRequest
        {
            Command = WellKnownCommands.BatchExecute,
            Parameters = new JsonObject
            {
                ["commands"] = commands,
                ["rollbackOnFailure"] = rollbackOnFailure
            }
        };
    }

    internal static JsonArray ParseCommands(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("commands JSON must not be empty.", nameof(json));

        try
        {
            var node = JsonNode.Parse(json);
            if (node is not JsonArray array)
                throw new ArgumentException("commands JSON must be a JSON array.");

            return array;
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"commands JSON is invalid: {ex.Message}", nameof(json), ex);
        }
    }
}
