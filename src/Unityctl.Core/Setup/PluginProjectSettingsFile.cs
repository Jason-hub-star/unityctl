using System.Text.Json;

namespace Unityctl.Core.Setup;

public sealed class PluginProjectSettings
{
    public bool Enabled { get; set; }
    public string? InstallSourceKind { get; set; }
    public string? InstalledVersion { get; set; }
}

public sealed class PluginInstallMetadata
{
    public string? InstallSourceKind { get; set; }
    public string? InstalledVersion { get; set; }
    public string? InstalledAtUtc { get; set; }
}

public static class PluginProjectSettingsFile
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public static PluginProjectSettings? TryRead(string projectPath)
        => TryReadJson<PluginProjectSettings>(PluginProjectPaths.GetProjectSettingsPath(projectPath));

    public static void Write(string projectPath, PluginProjectSettings settings)
    {
        var path = PluginProjectPaths.GetProjectSettingsPath(projectPath);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(path, JsonSerializer.Serialize(settings, JsonOptions));
    }

    public static PluginInstallMetadata? TryReadInstallMetadata(string projectPath)
        => TryReadJson<PluginInstallMetadata>(PluginProjectPaths.GetEmbeddedInstallMetadataPath(projectPath));

    public static void WriteInstallMetadata(string projectPath, PluginInstallMetadata metadata)
    {
        var path = PluginProjectPaths.GetEmbeddedInstallMetadataPath(projectPath);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(path, JsonSerializer.Serialize(metadata, JsonOptions));
    }

    private static T? TryReadJson<T>(string path)
    {
        if (!File.Exists(path))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions);
        }
        catch
        {
            return default;
        }
    }
}
