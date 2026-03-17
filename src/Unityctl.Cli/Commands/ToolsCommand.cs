using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Unityctl.Cli.Commands;

public static class ToolsCommand
{
    public static void Execute(bool json = false)
    {
        var tools = GetToolDefinitions();

        if (json)
        {
            var array = new JsonArray();
            foreach (var tool in tools)
                array.Add(JsonSerializer.SerializeToNode(tool, ToolsJsonContext.Default.ToolInfo));
            Console.WriteLine(array.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = true
            }));
        }
        else
        {
            Console.WriteLine($"unityctl v{Unityctl.Shared.Constants.Version} — Available tools ({tools.Length}):\n");
            foreach (var tool in tools)
            {
                Console.WriteLine($"  {tool.Name,-24} {tool.Description}");
                if (tool.Parameters.Length > 0)
                {
                    foreach (var p in tool.Parameters)
                    {
                        var req = p.Required ? " (required)" : "";
                        Console.WriteLine($"    --{p.Name,-20} {p.Type,-8} {p.Description}{req}");
                    }
                }
                Console.WriteLine();
            }
        }
    }

    private static ToolInfo[] GetToolDefinitions() =>
    [
        new ToolInfo
        {
            Name = "init",
            Description = "Install the unityctl plugin into a Unity project",
            Category = "setup",
            Parameters =
            [
                new ToolParameter { Name = "project", Type = "string", Description = "Path to Unity project", Required = true },
                new ToolParameter { Name = "source", Type = "string", Description = "Custom plugin source path", Required = false }
            ]
        },
        new ToolInfo
        {
            Name = "editor list",
            Description = "Discover installed Unity editors",
            Category = "discovery",
            Parameters =
            [
                new ToolParameter { Name = "json", Type = "bool", Description = "Output as JSON", Required = false }
            ]
        },
        new ToolInfo
        {
            Name = "status",
            Description = "Check Unity editor and project status",
            Category = "query",
            Parameters =
            [
                new ToolParameter { Name = "project", Type = "string", Description = "Path to Unity project", Required = true },
                new ToolParameter { Name = "wait", Type = "bool", Description = "Retry until editor responds", Required = false },
                new ToolParameter { Name = "json", Type = "bool", Description = "Output as JSON", Required = false }
            ]
        },
        new ToolInfo
        {
            Name = "build",
            Description = "Build a Unity project for a target platform",
            Category = "action",
            Parameters =
            [
                new ToolParameter { Name = "project", Type = "string", Description = "Path to Unity project", Required = true },
                new ToolParameter { Name = "target", Type = "string", Description = "Build target (StandaloneWindows64, OSX, Linux, Android, iOS, WebGL)", Required = false },
                new ToolParameter { Name = "output", Type = "string", Description = "Output path for build artifacts", Required = false },
                new ToolParameter { Name = "json", Type = "bool", Description = "Output as JSON", Required = false }
            ]
        },
        new ToolInfo
        {
            Name = "test",
            Description = "Run Unity tests (EditMode or PlayMode)",
            Category = "action",
            Parameters =
            [
                new ToolParameter { Name = "project", Type = "string", Description = "Path to Unity project", Required = true },
                new ToolParameter { Name = "mode", Type = "string", Description = "Test mode: edit or play", Required = false },
                new ToolParameter { Name = "filter", Type = "string", Description = "Test name filter", Required = false },
                new ToolParameter { Name = "json", Type = "bool", Description = "Output as JSON", Required = false }
            ]
        },
        new ToolInfo
        {
            Name = "check",
            Description = "Run compilation check on a Unity project",
            Category = "action",
            Parameters =
            [
                new ToolParameter { Name = "project", Type = "string", Description = "Path to Unity project", Required = true },
                new ToolParameter { Name = "type", Type = "string", Description = "Check type: compile", Required = false },
                new ToolParameter { Name = "json", Type = "bool", Description = "Output as JSON", Required = false }
            ]
        },
        new ToolInfo
        {
            Name = "tools",
            Description = "List all available tools with parameters (this command)",
            Category = "meta",
            Parameters =
            [
                new ToolParameter { Name = "json", Type = "bool", Description = "Output as machine-readable JSON", Required = false }
            ]
        }
    ];
}

public sealed class ToolInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("parameters")]
    public ToolParameter[] Parameters { get; set; } = [];
}

public sealed class ToolParameter
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("required")]
    public bool Required { get; set; }
}

[JsonSerializable(typeof(ToolInfo))]
[JsonSerializable(typeof(ToolParameter))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault)]
internal partial class ToolsJsonContext : JsonSerializerContext
{
}
