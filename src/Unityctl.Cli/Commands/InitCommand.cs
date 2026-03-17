using System.Text.Json;
using System.Text.Json.Nodes;

namespace Unityctl.Cli.Commands;

public static class InitCommand
{
    public static void Execute(string project, string? source = null)
    {
        var projectPath = Path.GetFullPath(project);
        var manifestPath = Path.Combine(projectPath, "Packages", "manifest.json");

        if (!File.Exists(manifestPath))
        {
            Console.Error.WriteLine($"ERROR: manifest.json not found at {manifestPath}");
            Console.Error.WriteLine("Is this a valid Unity project?");
            Environment.Exit(1);
            return;
        }

        var manifestJson = File.ReadAllText(manifestPath);
        var manifest = JsonNode.Parse(manifestJson);
        if (manifest == null)
        {
            Console.Error.WriteLine("ERROR: Failed to parse manifest.json");
            Environment.Exit(1);
            return;
        }

        var dependencies = manifest["dependencies"]?.AsObject();
        if (dependencies == null)
        {
            Console.Error.WriteLine("ERROR: manifest.json has no 'dependencies' object");
            Environment.Exit(1);
            return;
        }

        const string packageName = Unityctl.Shared.Constants.PluginPackageName;

        if (dependencies.ContainsKey(packageName))
        {
            Console.WriteLine($"{packageName} is already in manifest.json");
            return;
        }

        var packageSource = source ?? "file:../../unityctl/src/Unityctl.Plugin";
        dependencies.Add(packageName, JsonValue.Create(packageSource));

        var options = new JsonSerializerOptions { WriteIndented = true };
        var output = manifest.ToJsonString(options);
        File.WriteAllText(manifestPath, output);

        Console.WriteLine($"Added {packageName} to {manifestPath}");
        Console.WriteLine("Unity will import the plugin on next Editor open or domain reload.");
    }
}
