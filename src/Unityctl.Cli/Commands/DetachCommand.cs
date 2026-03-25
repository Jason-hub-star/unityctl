using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Core.Setup;
using Unityctl.Shared;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class DetachCommand
{
    public static void Execute(string project, bool cleanCache = false, bool json = false)
    {
        var projectPath = Path.GetFullPath(project);
        var response = Detach(projectPath, cleanCache);
        CommandRunner.PrintResponse(projectPath, response, json);
        Environment.ExitCode = CommandRunner.GetExitCode(response);
    }

    internal static CommandResponse Detach(string projectPath, bool cleanCache)
    {
        var removed = new JsonArray();
        var skipped = new JsonArray();

        try
        {
            RemoveEmbeddedInstall(projectPath, removed, skipped);
            RemoveManifestDependency(projectPath, removed, skipped);
            RemoveFileIfExists(PluginProjectPaths.GetProjectSettingsPath(projectPath), removed, skipped);
            RemoveFileIfExists(Path.Combine(PluginProjectPaths.GetScriptAssembliesDirectory(projectPath), "UnityctlBridge.dll"), removed, skipped);
            RemoveFileIfExists(Path.Combine(PluginProjectPaths.GetScriptAssembliesDirectory(projectPath), "UnityctlBridge.pdb"), removed, skipped);
            RemoveDirectoryIfExists(PluginProjectPaths.GetUnityctlLibraryDirectory(projectPath), removed, skipped);

            if (cleanCache)
            {
                RemoveDirectoryIfExists(PluginProjectPaths.GetBeeDirectory(projectPath), removed, skipped);
                RemoveFileIfExists(PluginProjectPaths.GetPackageManagerResolutionPath(projectPath), removed, skipped);
            }

            return CommandResponse.Ok(
                $"Detached {Constants.PluginPackageName} from {projectPath}",
                new JsonObject
                {
                    ["project"] = projectPath,
                    ["cleanCache"] = cleanCache,
                    ["removed"] = removed,
                    ["skipped"] = skipped
                });
        }
        catch (Exception ex)
        {
            return CommandResponse.Fail(
                StatusCode.UnknownError,
                $"Failed to detach {Constants.PluginPackageName}: {ex.Message}",
                new List<string> { ex.StackTrace ?? string.Empty });
        }
    }

    private static void RemoveEmbeddedInstall(string projectPath, JsonArray removed, JsonArray skipped)
    {
        var embeddedPath = PluginProjectPaths.GetEmbeddedPackagePath(projectPath);
        if (!Directory.Exists(embeddedPath))
        {
            skipped.Add(embeddedPath);
            return;
        }

        if (!File.Exists(PluginProjectPaths.GetEmbeddedInstallMetadataPath(projectPath)))
        {
            skipped.Add($"{embeddedPath} (metadata not found; leaving unmanaged embedded package in place)");
            return;
        }

        Directory.Delete(embeddedPath, recursive: true);
        removed.Add(embeddedPath);
    }

    private static void RemoveManifestDependency(string projectPath, JsonArray removed, JsonArray skipped)
    {
        var manifestPath = PluginProjectPaths.GetManifestPath(projectPath);
        if (!File.Exists(manifestPath))
        {
            skipped.Add(manifestPath);
            return;
        }

        var manifest = JsonNode.Parse(File.ReadAllText(manifestPath)) as JsonObject;
        var dependencies = manifest?["dependencies"] as JsonObject;
        if (manifest == null || dependencies == null || !dependencies.ContainsKey(Constants.PluginPackageName))
        {
            skipped.Add($"{manifestPath}::{Constants.PluginPackageName}");
            return;
        }

        dependencies.Remove(Constants.PluginPackageName);
        File.WriteAllText(manifestPath, manifest.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        removed.Add($"{manifestPath}::{Constants.PluginPackageName}");
    }

    private static void RemoveFileIfExists(string path, JsonArray removed, JsonArray skipped)
    {
        if (!File.Exists(path))
        {
            skipped.Add(path);
            return;
        }

        File.Delete(path);
        removed.Add(path);
    }

    private static void RemoveDirectoryIfExists(string path, JsonArray removed, JsonArray skipped)
    {
        if (!Directory.Exists(path))
        {
            skipped.Add(path);
            return;
        }

        Directory.Delete(path, recursive: true);
        removed.Add(path);
    }
}
