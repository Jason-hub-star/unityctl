using System.Text.Json;
using System.Text.Json.Nodes;
using Unityctl.Core.Setup;

namespace Unityctl.Cli.Commands;

public static class InitCommand
{
    public static void Execute(string project, string? source = null)
        => Execute(project, source, bundledBaseDirectory: null);

    internal static void Execute(string project, string? source, string? bundledBaseDirectory)
    {
        var projectPath = Path.GetFullPath(project);
        var manifestPath = PluginProjectPaths.GetManifestPath(projectPath);

        if (!File.Exists(manifestPath))
        {
            Fail($"manifest.json not found at {manifestPath}", "Is this a valid Unity project?");
            return;
        }

        var manifest = JsonNode.Parse(File.ReadAllText(manifestPath)) as JsonObject;
        var dependencies = manifest?["dependencies"]?.AsObject();
        if (manifest == null)
        {
            Fail("Failed to parse manifest.json");
            return;
        }

        if (dependencies == null)
        {
            Fail("manifest.json has no 'dependencies' object");
            return;
        }

        var installInfo = PluginInstallationInspector.Inspect(projectPath);

        if (string.IsNullOrWhiteSpace(source))
        {
            InstallEmbedded(projectPath, manifestPath, installInfo, bundledBaseDirectory);
            return;
        }

        if (!PluginSourceLocator.TryResolvePackageSource(
                source,
                out var packageSource,
                out var resolvedDirectory,
                out var error,
                bundledBaseDirectory))
        {
            Fail(error ?? "Plugin source could not be resolved.",
                "Tip: --source may be either a local Unityctl.Plugin folder or a Unity UPM Git URL like https://github.com/<owner>/<repo>.git?path=/src/Unityctl.Plugin#<tag>.");
            return;
        }

        InstallExplicit(projectPath, manifestPath, manifest, dependencies, installInfo, packageSource, resolvedDirectory);
    }

    private static void InstallEmbedded(
        string projectPath,
        string manifestPath,
        PluginInstallationInfo installInfo,
        string? bundledBaseDirectory)
    {
        if (installInfo.EmbeddedInstalled)
        {
            WriteProjectSettings(projectPath, PluginSourceLocator.EmbeddedSourceKind, installInfo.EmbeddedVersion ?? Unityctl.Shared.Constants.Version);
            PluginProjectSettingsFile.WriteInstallMetadata(projectPath, CreateInstallMetadata(PluginSourceLocator.EmbeddedSourceKind, installInfo.EmbeddedVersion ?? Unityctl.Shared.Constants.Version));
            Console.WriteLine($"Embedded {Unityctl.Shared.Constants.PluginPackageName} is already installed at {installInfo.EmbeddedPath}");
            return;
        }

        if (!string.IsNullOrWhiteSpace(installInfo.ManifestSource))
        {
            Fail(
                $"Existing {installInfo.ManifestSourceKind ?? "plugin"} install detected: {installInfo.ManifestSource}",
                $"Default `unityctl init` now installs an embedded package. Run `unityctl detach --project \"{projectPath}\" --clean-cache` and then `unityctl init --project \"{projectPath}\"` to migrate.");
            return;
        }

        if (!BundledPluginTemplateLocator.TryResolveTemplateDirectory(
                out var templateDirectory,
                out var error,
                bundledBaseDirectory))
        {
            Fail(error ?? "Bundled plugin template could not be resolved.");
            return;
        }

        var destinationDirectory = PluginProjectPaths.GetEmbeddedPackagePath(projectPath);
        if (Directory.Exists(destinationDirectory) && !File.Exists(PluginProjectPaths.GetEmbeddedPackageJsonPath(projectPath)))
        {
            Fail(
                $"Embedded package destination already exists and is not a valid unityctl package: {destinationDirectory}",
                $"Remove the directory manually or run `unityctl detach --project \"{projectPath}\"` before retrying.");
            return;
        }

        if (Directory.Exists(destinationDirectory))
            Directory.Delete(destinationDirectory, recursive: true);

        CopyDirectory(templateDirectory, destinationDirectory);

        var installedVersion = PluginInstallationInspector.ReadPackageVersion(PluginProjectPaths.GetEmbeddedPackageJsonPath(projectPath))
            ?? Unityctl.Shared.Constants.Version;
        WriteProjectSettings(projectPath, PluginSourceLocator.EmbeddedSourceKind, installedVersion);
        PluginProjectSettingsFile.WriteInstallMetadata(projectPath, CreateInstallMetadata(PluginSourceLocator.EmbeddedSourceKind, installedVersion));

        Console.WriteLine($"Installed embedded {Unityctl.Shared.Constants.PluginPackageName} into {destinationDirectory}");
        Console.WriteLine($"Manifest left unchanged at {manifestPath}");
        Console.WriteLine("Unity will import the embedded plugin on next Editor open or refresh.");
    }

    private static void InstallExplicit(
        string projectPath,
        string manifestPath,
        JsonObject manifest,
        JsonObject dependencies,
        PluginInstallationInfo installInfo,
        string packageSource,
        string? resolvedDirectory)
    {
        if (installInfo.EmbeddedInstalled)
        {
            Fail(
                $"Embedded install already exists at {installInfo.EmbeddedPath}",
                $"Run `unityctl detach --project \"{projectPath}\" --clean-cache` before switching to an explicit source install.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(installInfo.ManifestSource))
        {
            if (SourcesMatch(installInfo.ManifestSource, packageSource))
            {
                WriteProjectSettings(projectPath, PluginSourceLocator.ClassifyPackageSource(packageSource) ?? PluginSourceLocator.UnknownSourceKind, Unityctl.Shared.Constants.Version);
                Console.WriteLine($"{Unityctl.Shared.Constants.PluginPackageName} is already installed from {packageSource}");
                return;
            }

            Fail(
                $"Different plugin source already configured: {installInfo.ManifestSource}",
                $"Run `unityctl detach --project \"{projectPath}\" --clean-cache` before switching sources.");
            return;
        }

        dependencies[Unityctl.Shared.Constants.PluginPackageName] = JsonValue.Create(packageSource);
        SaveManifest(manifestPath, manifest);

        WriteProjectSettings(projectPath, PluginSourceLocator.ClassifyPackageSource(packageSource) ?? PluginSourceLocator.UnknownSourceKind, Unityctl.Shared.Constants.Version);

        Console.WriteLine($"Added {Unityctl.Shared.Constants.PluginPackageName} to {manifestPath}");
        Console.WriteLine($"Package source: {packageSource}");
        if (!string.IsNullOrWhiteSpace(resolvedDirectory))
            Console.WriteLine($"Resolved plugin directory: {resolvedDirectory}");
        Console.WriteLine("Unity will import the plugin on next Editor open or domain reload.");
    }

    private static void SaveManifest(string manifestPath, JsonObject manifest)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(manifestPath, manifest.ToJsonString(options));
    }

    private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (var directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, directory);
            Directory.CreateDirectory(Path.Combine(destinationDirectory, relativePath));
        }

        foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, file);
            var destinationPath = Path.Combine(destinationDirectory, relativePath);
            var destinationParent = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrWhiteSpace(destinationParent))
                Directory.CreateDirectory(destinationParent);
            File.Copy(file, destinationPath, overwrite: true);
        }
    }

    private static PluginInstallMetadata CreateInstallMetadata(string installSourceKind, string installedVersion)
    {
        return new PluginInstallMetadata
        {
            InstallSourceKind = installSourceKind,
            InstalledVersion = installedVersion,
            InstalledAtUtc = DateTimeOffset.UtcNow.ToString("O")
        };
    }

    private static void WriteProjectSettings(string projectPath, string installSourceKind, string installedVersion)
    {
        PluginProjectSettingsFile.Write(projectPath, new PluginProjectSettings
        {
            Enabled = true,
            InstallSourceKind = installSourceKind,
            InstalledVersion = installedVersion
        });
    }

    private static bool SourcesMatch(string configuredSource, string requestedSource)
    {
        var configuredKind = PluginSourceLocator.ClassifyPackageSource(configuredSource);
        var requestedKind = PluginSourceLocator.ClassifyPackageSource(requestedSource);

        if (!string.Equals(configuredKind, requestedKind, StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.Equals(configuredKind, PluginSourceLocator.LocalFileSourceKind, StringComparison.OrdinalIgnoreCase))
        {
            var configuredPath = NormalizeLocalSource(configuredSource);
            var requestedPath = NormalizeLocalSource(requestedSource);
            return string.Equals(configuredPath, requestedPath, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(configuredSource.Trim(), requestedSource.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeLocalSource(string source)
    {
        var path = source.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
            ? source["file:".Length..]
            : source;
        return Path.GetFullPath(path);
    }

    private static void Fail(string message, string? tip = null)
    {
        Console.Error.WriteLine($"ERROR: {message}");
        if (!string.IsNullOrWhiteSpace(tip))
            Console.Error.WriteLine(tip);
        Environment.Exit(1);
    }
}
