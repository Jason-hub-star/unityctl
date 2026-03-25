using System.Text.Json.Nodes;
using Unityctl.Shared;

namespace Unityctl.Core.Setup;

public sealed class PluginInstallationInfo
{
    public bool EmbeddedInstalled { get; init; }
    public string? EmbeddedPath { get; init; }
    public string? EmbeddedVersion { get; init; }
    public bool EmbeddedOwnedByUnityctl { get; init; }
    public string? ManifestSource { get; init; }
    public string? ManifestSourceKind { get; init; }
    public PluginProjectSettings? Settings { get; init; }

    public bool PluginInstalled => EmbeddedInstalled || !string.IsNullOrWhiteSpace(ManifestSource);
    public bool BridgeEnabled => Settings?.Enabled ?? false;
    public string? InstalledVersion => Settings?.InstalledVersion ?? EmbeddedVersion;

    public string? EffectiveSourceKind
        => EmbeddedInstalled ? PluginSourceLocator.EmbeddedSourceKind : ManifestSourceKind;

    public string? EffectiveSource
        => EmbeddedInstalled
            ? PluginProjectPaths.GetEmbeddedSourceToken()
            : ManifestSource;
}

public static class PluginInstallationInspector
{
    public static PluginInstallationInfo Inspect(string projectPath)
    {
        var fullProjectPath = Path.GetFullPath(projectPath);
        var embeddedPackageJsonPath = PluginProjectPaths.GetEmbeddedPackageJsonPath(fullProjectPath);
        var embeddedInstalled = File.Exists(embeddedPackageJsonPath);
        var manifestSource = ReadManifestSource(fullProjectPath);
        var settings = PluginProjectSettingsFile.TryRead(fullProjectPath);
        var metadata = PluginProjectSettingsFile.TryReadInstallMetadata(fullProjectPath);

        return new PluginInstallationInfo
        {
            EmbeddedInstalled = embeddedInstalled,
            EmbeddedPath = embeddedInstalled ? PluginProjectPaths.GetEmbeddedPackagePath(fullProjectPath) : null,
            EmbeddedVersion = embeddedInstalled ? ReadPackageVersion(embeddedPackageJsonPath) : null,
            EmbeddedOwnedByUnityctl = metadata != null,
            ManifestSource = manifestSource,
            ManifestSourceKind = PluginSourceLocator.ClassifyPackageSource(manifestSource),
            Settings = settings
        };
    }

    public static string? ReadManifestSource(string projectPath)
    {
        var manifestPath = PluginProjectPaths.GetManifestPath(projectPath);
        if (!File.Exists(manifestPath))
            return null;

        try
        {
            var manifest = JsonNode.Parse(File.ReadAllText(manifestPath)) as JsonObject;
            var dependencies = manifest?["dependencies"] as JsonObject;
            if (dependencies == null
                || !dependencies.TryGetPropertyValue(Constants.PluginPackageName, out var sourceNode)
                || sourceNode is not JsonValue sourceValue)
            {
                return null;
            }

            return sourceValue.TryGetValue<string>(out var value)
                ? value
                : sourceNode.ToJsonString();
        }
        catch
        {
            return null;
        }
    }

    public static string? ReadPackageVersion(string packageJsonPath)
    {
        if (!File.Exists(packageJsonPath))
            return null;

        try
        {
            var json = JsonNode.Parse(File.ReadAllText(packageJsonPath)) as JsonObject;
            return json?["version"]?.GetValue<string>();
        }
        catch
        {
            return null;
        }
    }
}
