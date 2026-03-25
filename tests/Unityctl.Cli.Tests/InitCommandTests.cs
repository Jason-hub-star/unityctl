using System.Text.Json.Nodes;
using Unityctl.Cli.Commands;
using Unityctl.Core.Setup;
using Xunit;

namespace Unityctl.Cli.Tests;

[Collection("ConsoleOutput")]
public class InitCommandTests
{
    [CliTestFact]
    public void Execute_WithBundledTemplate_InstallsEmbeddedPackageAndSettings()
    {
        using var tempProject = new TemporaryProject();
        using var bundleRoot = new TemporaryDirectory();
        var templateDirectory = Path.Combine(bundleRoot.Path, PluginProjectPaths.BundledTemplateDirectoryName);
        Directory.CreateDirectory(Path.Combine(templateDirectory, "Editor"));
        File.WriteAllText(Path.Combine(templateDirectory, "package.json"), """
{
  "name": "com.unityctl.bridge",
  "version": "9.9.9"
}
""");
        File.WriteAllText(Path.Combine(templateDirectory, "Editor", "Dummy.cs"), "// dummy");

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            InitCommand.Execute(tempProject.Path, source: null, bundledBaseDirectory: bundleRoot.Path);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        Assert.True(File.Exists(Path.Combine(tempProject.Path, "Packages", "com.unityctl.bridge", "package.json")));
        Assert.True(File.Exists(Path.Combine(tempProject.Path, "Packages", "com.unityctl.bridge", "Editor", "Dummy.cs")));
        Assert.True(File.Exists(PluginProjectPaths.GetProjectSettingsPath(tempProject.Path)));
        Assert.True(File.Exists(PluginProjectPaths.GetEmbeddedInstallMetadataPath(tempProject.Path)));

        var manifest = JsonNode.Parse(File.ReadAllText(tempProject.ManifestPath))!.AsObject();
        Assert.False(manifest["dependencies"]!.AsObject().ContainsKey("com.unityctl.bridge"));

        var output = writer.ToString();
        Assert.Contains("Installed embedded com.unityctl.bridge", output);
    }

    [CliTestFact]
    public void Execute_WithExplicitGitSource_WritesManifestDependencyAndSettings()
    {
        using var tempProject = new TemporaryProject();
        const string gitSource = "https://github.com/kimjuyoung1127/unityctl.git?path=/src/Unityctl.Plugin#v0.2.0";

        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            InitCommand.Execute(tempProject.Path, gitSource, bundledBaseDirectory: null);
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        var manifest = JsonNode.Parse(File.ReadAllText(tempProject.ManifestPath))!.AsObject();
        var dependencies = manifest["dependencies"]!.AsObject();

        Assert.Equal(gitSource, dependencies["com.unityctl.bridge"]?.GetValue<string>());
        Assert.True(File.Exists(PluginProjectPaths.GetProjectSettingsPath(tempProject.Path)));

        var output = writer.ToString();
        Assert.Contains("Added com.unityctl.bridge", output);
        Assert.Contains($"Package source: {gitSource}", output);
        Assert.DoesNotContain("Resolved plugin directory:", output);
    }

    private sealed class TemporaryProject : IDisposable
    {
        public TemporaryProject()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"unityctl-init-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(System.IO.Path.Combine(Path, "Packages"));
            Directory.CreateDirectory(System.IO.Path.Combine(Path, "ProjectSettings"));
            ManifestPath = System.IO.Path.Combine(Path, "Packages", "manifest.json");
            File.WriteAllText(ManifestPath, """
{
  "dependencies": {
    "com.unity.ugui": "2.0.0"
  }
}
""");
        }

        public string ManifestPath { get; }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"unityctl-init-bundle-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
