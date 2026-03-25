using System.Text.Json.Nodes;
using Unityctl.Cli.Commands;
using Unityctl.Core.Setup;
using Xunit;

namespace Unityctl.Cli.Tests;

public sealed class DetachCommandTests
{
    [CliTestFact]
    public void Detach_RemovesEmbeddedInstallSettingsAndCaches()
    {
        using var project = new TemporaryDetachProject();

        var response = DetachCommand.Detach(project.Path, cleanCache: false);

        Assert.True(response.Success);
        Assert.False(Directory.Exists(PluginProjectPaths.GetEmbeddedPackagePath(project.Path)));
        Assert.False(File.Exists(PluginProjectPaths.GetProjectSettingsPath(project.Path)));
        Assert.False(File.Exists(Path.Combine(project.Path, "Library", "ScriptAssemblies", "UnityctlBridge.dll")));
        Assert.False(Directory.Exists(PluginProjectPaths.GetUnityctlLibraryDirectory(project.Path)));
        Assert.True(Directory.Exists(PluginProjectPaths.GetBeeDirectory(project.Path)));
    }

    [CliTestFact]
    public void Detach_WithCleanCache_RemovesManifestDependencyAndExtraCaches()
    {
        using var project = new TemporaryDetachProject(includeManifestDependency: true);

        var response = DetachCommand.Detach(project.Path, cleanCache: true);

        Assert.True(response.Success);
        var manifest = JsonNode.Parse(File.ReadAllText(project.ManifestPath))!.AsObject();
        Assert.False(manifest["dependencies"]!.AsObject().ContainsKey("com.unityctl.bridge"));
        Assert.False(Directory.Exists(PluginProjectPaths.GetBeeDirectory(project.Path)));
        Assert.False(File.Exists(PluginProjectPaths.GetPackageManagerResolutionPath(project.Path)));
    }

    private sealed class TemporaryDetachProject : IDisposable
    {
        public TemporaryDetachProject(bool includeManifestDependency = false)
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"unityctl-detach-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
            Directory.CreateDirectory(System.IO.Path.Combine(Path, "Packages"));
            Directory.CreateDirectory(System.IO.Path.Combine(Path, "ProjectSettings"));
            Directory.CreateDirectory(System.IO.Path.Combine(Path, "Library", "ScriptAssemblies"));
            Directory.CreateDirectory(System.IO.Path.Combine(Path, "Library", "Unityctl", "build-state"));
            Directory.CreateDirectory(System.IO.Path.Combine(Path, "Library", "Bee"));
            Directory.CreateDirectory(System.IO.Path.Combine(Path, "Library", "PackageManager"));
            Directory.CreateDirectory(System.IO.Path.Combine(Path, "Packages", "com.unityctl.bridge", "Editor"));

            ManifestPath = System.IO.Path.Combine(Path, "Packages", "manifest.json");
            File.WriteAllText(ManifestPath, includeManifestDependency
                ? """
{
  "dependencies": {
    "com.unityctl.bridge": "file:C:/repo/src/Unityctl.Plugin"
  }
}
"""
                : """
{
  "dependencies": {
    "com.unity.ugui": "2.0.0"
  }
}
""");

            File.WriteAllText(System.IO.Path.Combine(Path, "Packages", "com.unityctl.bridge", "package.json"), """
{
  "name": "com.unityctl.bridge",
  "version": "1.2.3"
}
""");
            File.WriteAllText(PluginProjectPaths.GetEmbeddedInstallMetadataPath(Path), """
{
  "installSourceKind": "embedded",
  "installedVersion": "1.2.3"
}
""");
            File.WriteAllText(PluginProjectPaths.GetProjectSettingsPath(Path), """
{
  "enabled": true,
  "installSourceKind": "embedded",
  "installedVersion": "1.2.3"
}
""");
            File.WriteAllText(System.IO.Path.Combine(Path, "Library", "ScriptAssemblies", "UnityctlBridge.dll"), "dll");
            File.WriteAllText(System.IO.Path.Combine(Path, "Library", "ScriptAssemblies", "UnityctlBridge.pdb"), "pdb");
            File.WriteAllText(System.IO.Path.Combine(Path, "Library", "Unityctl", "build-state", "state.json"), "{}");
            File.WriteAllText(System.IO.Path.Combine(Path, "Library", "PackageManager", "projectResolution.json"), "{}");
        }

        public string ManifestPath { get; }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
