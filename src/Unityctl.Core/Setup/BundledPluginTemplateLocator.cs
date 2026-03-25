namespace Unityctl.Core.Setup;

public static class BundledPluginTemplateLocator
{
    public static bool TryResolveTemplateDirectory(
        out string templateDirectory,
        out string? error,
        string? baseDirectory = null)
    {
        templateDirectory = string.Empty;
        error = null;

        var root = Path.GetFullPath(baseDirectory ?? AppContext.BaseDirectory);
        var candidate = Path.Combine(root, PluginProjectPaths.BundledTemplateDirectoryName);
        var packageJsonPath = Path.Combine(candidate, "package.json");

        if (!Directory.Exists(candidate) || !File.Exists(packageJsonPath))
        {
            error = $"Bundled plugin template not found under '{candidate}'. Reinstall unityctl or rebuild the CLI artifact.";
            return false;
        }

        templateDirectory = candidate;
        return true;
    }
}
