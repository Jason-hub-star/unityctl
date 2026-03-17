using System.Text.Json;
using Unityctl.Shared.Commands;

namespace Unityctl.Cli.Commands;

public static class ToolsCommand
{
    public static void Execute(bool json = false)
    {
        var tools = GetToolDefinitions();

        if (json)
        {
            Console.WriteLine(JsonSerializer.Serialize(tools, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault
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

    internal static CommandDefinition[] GetToolDefinitions()
        => CommandCatalog.All;
}
