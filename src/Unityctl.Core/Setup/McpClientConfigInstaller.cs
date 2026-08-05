using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Unityctl.Core.Setup;

/// <summary>
/// Config file shape used by a given MCP client. The three shapes are not
/// interchangeable: the JSON clients disagree on the top-level key, and Codex
/// stores servers in TOML.
/// </summary>
public enum McpConfigFormat
{
    /// <summary>JSON with a top-level <c>mcpServers</c> object (Claude Code, Cursor).</summary>
    JsonMcpServers,

    /// <summary>JSON with a top-level <c>servers</c> object and an explicit <c>type</c> (VS Code).</summary>
    JsonServers,

    /// <summary>TOML with an <c>[mcp_servers.&lt;name&gt;]</c> table (Codex).</summary>
    Toml
}

/// <param name="Entry">
/// Just the unityctl server entry that was written. Reported instead of
/// <paramref name="Content"/> because a real client config (e.g. ~/.claude.json)
/// is hundreds of KB of unrelated user state — echoing it back would blow the
/// agent's context and leak personal settings.
/// </param>
/// <param name="Content">The full merged file text. Written to disk; not reported.</param>
public sealed record McpInstallResult(
    bool Success,
    string Client,
    string ConfigPath,
    McpConfigFormat Format,
    bool FileCreated,
    bool AlreadyPresent,
    string Entry,
    string Content,
    string Message,
    IReadOnlyList<string>? Candidates = null);

/// <summary>
/// Writes the unityctl MCP server entry into an AI client's config file,
/// merging into whatever is already there instead of replacing the file.
/// </summary>
public sealed class McpClientConfigInstaller
{
    public const string ServerName = "unityctl";
    public const string DefaultCommand = "unityctl-mcp";

    public static readonly IReadOnlyList<string> SupportedClients =
        ["claude-code", "codex", "cursor", "vscode"];

    private readonly string _homeDirectory;

    public McpClientConfigInstaller(string? homeDirectory = null)
    {
        _homeDirectory = homeDirectory
            ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    public McpInstallResult Install(
        string client,
        string? projectPath = null,
        string command = DefaultCommand,
        bool dryRun = false)
    {
        var normalized = (client ?? string.Empty).Trim().ToLowerInvariant();
        if (!SupportedClients.Contains(normalized))
        {
            return Failure(
                normalized,
                $"Unknown client '{client}'. Supported: {string.Join(", ", SupportedClients)}",
                SupportedClients);
        }

        if (!TryResolveTarget(normalized, projectPath, out var configPath, out var format, out var error))
            return Failure(normalized, error!, null);

        var existed = File.Exists(configPath);
        string existing;
        try
        {
            existing = existed ? File.ReadAllText(configPath) : string.Empty;
        }
        catch (Exception ex)
        {
            return Failure(normalized, $"Cannot read {configPath}: {ex.Message}", null);
        }

        string merged;
        string entry;
        bool alreadyPresent;
        try
        {
            if (format == McpConfigFormat.Toml)
                merged = MergeToml(existing, command, out alreadyPresent, out entry);
            else
                merged = MergeJson(existing, format, command, out alreadyPresent, out entry);
        }
        catch (JsonException ex)
        {
            // Refuse to clobber a config we cannot parse — a broken merge is worse
            // than no merge.
            return Failure(normalized, $"{configPath} is not valid JSON ({ex.Message}). Fix it or edit it by hand.", null);
        }

        if (!dryRun)
        {
            try
            {
                var directory = Path.GetDirectoryName(configPath);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);
                File.WriteAllText(configPath, merged);
            }
            catch (Exception ex)
            {
                return Failure(normalized, $"Cannot write {configPath}: {ex.Message}", null);
            }
        }

        var verb = dryRun ? "Would write" : (existed ? "Updated" : "Created");
        return new McpInstallResult(
            Success: true,
            Client: normalized,
            ConfigPath: configPath,
            Format: format,
            FileCreated: !existed,
            AlreadyPresent: alreadyPresent,
            Entry: entry,
            Content: merged,
            Message: $"{verb} {ServerName} MCP server entry in {configPath}");
    }

    private bool TryResolveTarget(
        string client,
        string? projectPath,
        out string configPath,
        out McpConfigFormat format,
        out string? error)
    {
        error = null;
        var project = string.IsNullOrWhiteSpace(projectPath) ? null : Path.GetFullPath(projectPath);

        switch (client)
        {
            case "claude-code":
                format = McpConfigFormat.JsonMcpServers;
                configPath = project is null
                    ? Path.Combine(_homeDirectory, ".claude.json")
                    : Path.Combine(project, ".mcp.json");
                return true;

            case "cursor":
                format = McpConfigFormat.JsonMcpServers;
                configPath = project is null
                    ? Path.Combine(_homeDirectory, ".cursor", "mcp.json")
                    : Path.Combine(project, ".cursor", "mcp.json");
                return true;

            case "vscode":
                // VS Code's user-level MCP config lives inside a profile directory
                // whose location varies by OS and profile. Only the workspace file
                // is addressable without guessing, so require --project.
                format = McpConfigFormat.JsonServers;
                if (project is null)
                {
                    configPath = string.Empty;
                    error = "vscode requires --project (VS Code stores MCP servers per workspace in .vscode/mcp.json)";
                    return false;
                }
                configPath = Path.Combine(project, ".vscode", "mcp.json");
                return true;

            case "codex":
                format = McpConfigFormat.Toml;
                configPath = project is null
                    ? Path.Combine(_homeDirectory, ".codex", "config.toml")
                    : Path.Combine(project, ".codex", "config.toml");
                return true;

            default:
                configPath = string.Empty;
                format = McpConfigFormat.JsonMcpServers;
                error = $"Unknown client '{client}'";
                return false;
        }
    }

    private static string MergeJson(
        string existing,
        McpConfigFormat format,
        string command,
        out bool alreadyPresent,
        out string entry)
    {
        var key = format == McpConfigFormat.JsonServers ? "servers" : "mcpServers";

        var root = string.IsNullOrWhiteSpace(existing)
            ? new JsonObject()
            : JsonNode.Parse(existing) as JsonObject
              ?? throw new JsonException("root is not a JSON object");

        if (root[key] is not JsonObject servers)
        {
            servers = new JsonObject();
            root[key] = servers;
        }

        alreadyPresent = servers.ContainsKey(ServerName);

        var serverEntry = new JsonObject { ["command"] = command };
        if (format == McpConfigFormat.JsonServers)
            serverEntry["type"] = "stdio";

        servers[ServerName] = serverEntry;

        entry = new JsonObject { [key] = new JsonObject { [ServerName] = serverEntry.DeepClone() } }
            .ToJsonString(new JsonSerializerOptions { WriteIndented = true });

        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine;
    }

    private static string MergeToml(string existing, string command, out bool alreadyPresent, out string entry)
    {
        // ponytail: line-level TOML editing, not a TOML parser. Ceiling — it only
        // understands the one table it owns ([mcp_servers.unityctl]) and rewrites
        // that block wholesale; every other line is passed through untouched.
        // Upgrade path: swap in a real TOML library if we ever need to read back
        // arbitrary user values.
        var header = $"[mcp_servers.{ServerName}]";
        var block = new StringBuilder()
            .Append(header).Append(Environment.NewLine)
            .Append("command = \"").Append(command).Append('"').Append(Environment.NewLine)
            .ToString();
        entry = block;

        if (string.IsNullOrWhiteSpace(existing))
        {
            alreadyPresent = false;
            return block;
        }

        var lines = existing.Replace("\r\n", "\n").Split('\n');
        var start = Array.FindIndex(lines, line => line.Trim() == header);
        alreadyPresent = start >= 0;

        if (!alreadyPresent)
        {
            var trailing = existing.EndsWith('\n') ? string.Empty : Environment.NewLine;
            return existing + trailing + Environment.NewLine + block;
        }

        // Replace from the header up to (not including) the next table header.
        var end = start + 1;
        while (end < lines.Length && !lines[end].TrimStart().StartsWith('['))
            end++;

        var rebuilt = new StringBuilder();
        for (var i = 0; i < start; i++)
            rebuilt.Append(lines[i]).Append(Environment.NewLine);
        rebuilt.Append(block);
        for (var i = end; i < lines.Length; i++)
        {
            // Drop the trailing empty element produced by a final newline.
            if (i == lines.Length - 1 && lines[i].Length == 0)
                continue;
            rebuilt.Append(lines[i]).Append(Environment.NewLine);
        }

        return rebuilt.ToString();
    }

    private static McpInstallResult Failure(string client, string message, IReadOnlyList<string>? candidates) =>
        new(
            Success: false,
            Client: client,
            ConfigPath: string.Empty,
            Format: McpConfigFormat.JsonMcpServers,
            FileCreated: false,
            AlreadyPresent: false,
            Entry: string.Empty,
            Content: string.Empty,
            Message: message,
            Candidates: candidates);
}
