using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

/// <summary>
/// Talks to a development Player build's runtime bridge. The player writes a
/// discovery state file (unityctl-runtime.json under persistentDataPath; the
/// exact path is printed in the Player log) — these verbs read it for the pipe
/// name and send runtime-* commands over the same IPC framing as the editor.
/// Runtime command names are intentionally not in WellKnownCommands: that
/// catalog is the editor transport surface with plugin-handler guardrails.
/// </summary>
public static class RuntimeCommand
{
    internal const string StatusCommand = "runtime-status";
    internal const string LogsCommand = "runtime-logs";

    public static void Status(string stateFile, bool json = false)
        => Run(stateFile, CreateStatusRequest(), json);

    public static void Logs(string stateFile, int? limit = null, string? severity = null, bool json = false)
        => Run(stateFile, CreateLogsRequest(limit, severity), json);

    internal static CommandRequest CreateStatusRequest()
        => new() { Command = StatusCommand, Parameters = new JsonObject() };

    internal static CommandRequest CreateLogsRequest(int? limit = null, string? severity = null)
    {
        var parameters = new JsonObject();
        if (limit.HasValue)
            parameters["limit"] = limit.Value;
        if (!string.IsNullOrWhiteSpace(severity))
            parameters["severity"] = severity;

        return new CommandRequest { Command = LogsCommand, Parameters = parameters };
    }

    internal static string? ReadPipeName(string stateFilePath)
    {
        if (string.IsNullOrWhiteSpace(stateFilePath) || !File.Exists(stateFilePath))
            return null;

        try
        {
            var state = JsonNode.Parse(File.ReadAllText(stateFilePath));
            return state?["pipeName"]?.GetValue<string>();
        }
        catch
        {
            return null;
        }
    }

    private static void Run(string stateFile, CommandRequest request, bool json)
    {
        var pipeName = ReadPipeName(stateFile);
        if (pipeName == null)
        {
            Console.Error.WriteLine(
                $"Error: Could not read runtime state file: {stateFile}. " +
                "Launch a Development Build with the unityctl plugin — the player logs the state file path on startup.");
            Environment.Exit(1);
            return;
        }

        var transport = new IpcTransport(pipeName, useRawPipeName: true);
        var response = transport.SendAsync(request).GetAwaiter().GetResult();
        CommandRunner.PrintResponse(string.Empty, response, json);
        Environment.Exit(CommandRunner.GetExitCode(response));
    }
}
