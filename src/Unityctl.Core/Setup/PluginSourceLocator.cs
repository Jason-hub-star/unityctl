namespace Unityctl.Core.Setup;

public static class PluginSourceLocator
{
    private const string PluginPackageFileName = "package.json";

    public static bool TryResolvePackageSource(
        string? source,
        out string packageSource,
        out string? resolvedDirectory,
        out string? error,
        string? baseDirectory = null)
    {
        packageSource = string.Empty;
        resolvedDirectory = null;
        error = null;

        var candidateDirectory = string.IsNullOrWhiteSpace(source)
            ? TryResolveWorkspacePluginDirectory(baseDirectory)
            : GetCandidateDirectory(source, baseDirectory);

        if (candidateDirectory == null)
        {
            error = string.IsNullOrWhiteSpace(source)
                ? "Could not locate src/Unityctl.Plugin from the current unityctl workspace."
                : $"Plugin source '{source}' could not be resolved.";
            return false;
        }

        if (!TryValidatePluginDirectory(candidateDirectory, out resolvedDirectory, out error))
            return false;

        packageSource = $"file:{resolvedDirectory!.Replace('\\', '/')}";
        return true;
    }

    public static string? TryResolveWorkspacePluginDirectory(string? baseDirectory = null)
    {
        foreach (var startDirectory in EnumerateSearchRoots(baseDirectory))
        {
            var current = new DirectoryInfo(startDirectory);
            while (current != null)
            {
                var workspaceSolutionPath = Path.Combine(current.FullName, "unityctl.slnx");
                if (!File.Exists(workspaceSolutionPath))
                {
                    current = current.Parent;
                    continue;
                }

                var candidateDirectory = Path.Combine(current.FullName, "src", "Unityctl.Plugin");
                if (TryValidatePluginDirectory(candidateDirectory, out var resolvedDirectory, out _))
                    return resolvedDirectory;

                current = current.Parent;
            }
        }

        return null;
    }

    private static IEnumerable<string> EnumerateSearchRoots(string? baseDirectory)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in new[] { baseDirectory, AppContext.BaseDirectory, Environment.CurrentDirectory })
        {
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            var fullPath = Path.GetFullPath(candidate);
            if (seen.Add(fullPath))
                yield return fullPath;
        }
    }

    private static string? GetCandidateDirectory(string source, string? baseDirectory)
    {
        var pathPart = source.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
            ? source["file:".Length..]
            : source;

        if (string.IsNullOrWhiteSpace(pathPart))
            return null;

        return string.IsNullOrWhiteSpace(baseDirectory)
            ? Path.GetFullPath(pathPart)
            : Path.GetFullPath(pathPart, Path.GetFullPath(baseDirectory));
    }

    private static bool TryValidatePluginDirectory(
        string candidateDirectory,
        out string? resolvedDirectory,
        out string? error)
    {
        resolvedDirectory = null;
        error = null;

        var fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(candidateDirectory));
        if (!Directory.Exists(fullPath))
        {
            error = $"Plugin source directory not found: {fullPath}";
            return false;
        }

        var packageJsonPath = Path.Combine(fullPath, PluginPackageFileName);
        if (!File.Exists(packageJsonPath))
        {
            error = $"Plugin source directory is missing {PluginPackageFileName}: {fullPath}";
            return false;
        }

        resolvedDirectory = fullPath;
        return true;
    }
}
