using System.Text.Json;
using System.Text.Json.Nodes;
using Unityctl.Core.Setup;

namespace Unityctl.Cli.Commands;

public static class McpCommand
{
    public static void Install(
        string client,
        string? project = null,
        string command = McpClientConfigInstaller.DefaultCommand,
        bool dryRun = false,
        bool json = false)
    {
        var installer = new McpClientConfigInstaller();
        var result = installer.Install(client, project, command, dryRun);
        Print(result, dryRun, json);

        if (!result.Success)
            Environment.Exit(1);
    }

    internal static void Print(McpInstallResult result, bool dryRun, bool json)
    {
        if (json)
        {
            Console.WriteLine(BuildPayload(result, dryRun).ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            return;
        }

        if (!result.Success)
        {
            Console.Error.WriteLine(result.Message);
            if (result.Candidates is { Count: > 0 })
                Console.Error.WriteLine($"Candidates: {string.Join(", ", result.Candidates)}");
            return;
        }

        Console.WriteLine(result.Message);
        if (result.AlreadyPresent)
        {
            Console.WriteLine($"  (replaced the existing '{McpClientConfigInstaller.ServerName}' entry; other servers untouched)");
            if (!string.IsNullOrWhiteSpace(result.PreviousEntry))
            {
                Console.WriteLine("  previous entry (restore by hand if it was customised):");
                foreach (var line in result.PreviousEntry.Split('\n'))
                    Console.WriteLine("    " + line.TrimEnd());
            }
        }

        if (dryRun)
        {
            Console.WriteLine();
            Console.WriteLine(result.Entry.TrimEnd());
        }
    }

    internal static JsonObject BuildPayload(McpInstallResult result, bool dryRun)
    {
        var data = new JsonObject
        {
            ["client"] = result.Client,
            ["configPath"] = result.ConfigPath,
            ["format"] = result.Format.ToString(),
            ["dryRun"] = dryRun,
            ["fileCreated"] = result.FileCreated,
            ["alreadyPresent"] = result.AlreadyPresent,
            // Only the entry we own — never the whole config file. See McpInstallResult.Entry.
            ["entry"] = result.Entry,
            ["configBytes"] = result.Content.Length
        };

        if (!string.IsNullOrWhiteSpace(result.PreviousEntry))
            data["previousEntry"] = result.PreviousEntry;

        if (result.Candidates is { Count: > 0 })
        {
            var candidates = new JsonArray();
            foreach (var candidate in result.Candidates)
                candidates.Add(candidate);
            data["candidates"] = candidates;
        }

        return new JsonObject
        {
            ["success"] = result.Success,
            ["message"] = result.Message,
            ["data"] = data
        };
    }
}
