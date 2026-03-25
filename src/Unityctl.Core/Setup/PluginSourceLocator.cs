namespace Unityctl.Core.Setup;

public static class PluginSourceLocator
{
    private const string PluginPackageFileName = "package.json";
    public const string EmbeddedSourceKind = "embedded";
    public const string LocalFileSourceKind = "local-file";
    public const string GitSourceKind = "git";
    public const string RemoteUrlSourceKind = "remote-url";
    public const string UnknownSourceKind = "unknown";

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

        if (string.IsNullOrWhiteSpace(source))
        {
            error = "Plugin source is required for explicit local or Git installs.";
            return false;
        }

        if (LooksLikeRemotePackageSource(source.Trim()))
            return TryResolveRemotePackageSource(source, out packageSource, out error);

        var candidateDirectory = GetCandidateDirectory(source, baseDirectory);

        if (candidateDirectory == null)
        {
            error = $"Plugin source '{source}' could not be resolved.";
            return false;
        }

        if (!TryValidatePluginDirectory(candidateDirectory, out resolvedDirectory, out error))
            return false;

        packageSource = $"file:{resolvedDirectory!.Replace('\\', '/')}";
        return true;
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

    private static bool TryResolveRemotePackageSource(
        string? source,
        out string packageSource,
        out string? error)
    {
        packageSource = string.Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(source))
            return false;

        var trimmed = source.Trim();
        if (!LooksLikeRemotePackageSource(trimmed))
            return false;

        if (!TryValidateRemotePackageSource(trimmed, out error))
            return false;

        packageSource = trimmed;
        return true;
    }

    public static string? ClassifyPackageSource(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return null;

        var trimmed = source.Trim();
        if (trimmed.StartsWith(PluginProjectPaths.EmbeddedSourcePrefix, StringComparison.OrdinalIgnoreCase))
            return EmbeddedSourceKind;

        if (trimmed.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            return LocalFileSourceKind;

        if (trimmed.Contains(".git", StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith("git@", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("?path=", StringComparison.OrdinalIgnoreCase))
        {
            return GitSourceKind;
        }

        if (trimmed.Contains("://", StringComparison.Ordinal))
            return RemoteUrlSourceKind;

        return UnknownSourceKind;
    }

    private static bool LooksLikeRemotePackageSource(string source)
    {
        if (source.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
            return false;

        return source.Contains("://", StringComparison.Ordinal) || source.StartsWith("git@", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryValidateRemotePackageSource(string source, out string? error)
    {
        error = null;

        if (source.StartsWith("git@", StringComparison.OrdinalIgnoreCase))
        {
            if (!source.Contains(":", StringComparison.Ordinal) || !source.Contains(".git", StringComparison.OrdinalIgnoreCase))
            {
                error = "Remote plugin source must be a valid Git URL. Expected '.git' and a repository path.";
                return false;
            }

            return true;
        }

        if (!Uri.TryCreate(source, UriKind.Absolute, out var uri))
        {
            error = $"Plugin source '{source}' could not be parsed as a local path or Git URL.";
            return false;
        }

        if (uri.Scheme.Equals(Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && !uri.Scheme.Equals("ssh", StringComparison.OrdinalIgnoreCase))
        {
            error = $"Remote plugin source scheme '{uri.Scheme}' is not supported. Use https://, http://, ssh://, or a local path.";
            return false;
        }

        if (!source.Contains(".git", StringComparison.OrdinalIgnoreCase))
        {
            error = "Remote plugin source must be a Unity-compatible Git URL ending in '.git' (query and fragment are allowed).";
            return false;
        }

        return true;
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
