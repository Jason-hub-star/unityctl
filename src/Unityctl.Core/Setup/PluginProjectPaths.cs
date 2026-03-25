using Unityctl.Shared;

namespace Unityctl.Core.Setup;

public static class PluginProjectPaths
{
    public const string SettingsFileName = "UnityctlSettings.asset";
    public const string InstallMetadataFileName = "unityctl-install.json";
    public const string BundledTemplateDirectoryName = "unityctl-plugin-template";
    public const string EmbeddedSourcePrefix = "embedded:";

    public static string GetManifestPath(string projectPath)
        => Path.Combine(Path.GetFullPath(projectPath), "Packages", "manifest.json");

    public static string GetPackagesLockPath(string projectPath)
        => Path.Combine(Path.GetFullPath(projectPath), "Packages", "packages-lock.json");

    public static string GetEmbeddedPackagePath(string projectPath)
        => Path.Combine(Path.GetFullPath(projectPath), "Packages", Constants.PluginPackageName);

    public static string GetEmbeddedPackageRelativePath()
        => Path.Combine("Packages", Constants.PluginPackageName);

    public static string GetEmbeddedPackageJsonPath(string projectPath)
        => Path.Combine(GetEmbeddedPackagePath(projectPath), "package.json");

    public static string GetEmbeddedInstallMetadataPath(string projectPath)
        => Path.Combine(GetEmbeddedPackagePath(projectPath), InstallMetadataFileName);

    public static string GetProjectSettingsDirectory(string projectPath)
        => Path.Combine(Path.GetFullPath(projectPath), "ProjectSettings");

    public static string GetProjectSettingsPath(string projectPath)
        => Path.Combine(GetProjectSettingsDirectory(projectPath), SettingsFileName);

    public static string GetScriptAssembliesDirectory(string projectPath)
        => Path.Combine(Path.GetFullPath(projectPath), "Library", "ScriptAssemblies");

    public static string GetUnityctlLibraryDirectory(string projectPath)
        => Path.Combine(Path.GetFullPath(projectPath), "Library", "Unityctl");

    public static string GetBeeDirectory(string projectPath)
        => Path.Combine(Path.GetFullPath(projectPath), "Library", "Bee");

    public static string GetPackageManagerResolutionPath(string projectPath)
        => Path.Combine(Path.GetFullPath(projectPath), "Library", "PackageManager", "projectResolution.json");

    public static string GetEmbeddedSourceToken()
        => $"{EmbeddedSourcePrefix}{GetEmbeddedPackageRelativePath().Replace('\\', '/')}";
}
