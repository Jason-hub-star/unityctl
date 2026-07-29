using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Reflection;

namespace Unityctl.Shared;

public static class Constants
{
    private static readonly string? InformationalVersion =
        typeof(Constants).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

    public static string Version => NormalizeVersion(InformationalVersion) ?? "0.6.1";
    public const string PipePrefix = "unityctl_";
    public const int DefaultTimeoutMs = 120_000;
    public const int PingTimeoutMs = 10_000;
    public const int BatchModeTimeoutMs = 600_000;
    public const int IpcConnectTimeoutMs = 5_000;
    public const int IpcMessageTimeoutMs = 30_000;
    public const int AsyncCommandDefaultTimeoutSeconds = 300;
    public const string PluginPackageName = "com.unityctl.bridge";
    public const string BatchEntryMethod = "Unityctl.Plugin.Editor.BatchMode.UnityctlBatchEntry.Execute";
    public const string SessionsDirectory = "sessions";
    public const string SessionActiveFile = "active.json";
    public const string SessionHistoryFile = "history.ndjson";
    public const int SessionTtlDays = 7;
    public const string FlightLogDirectory = "flight-log";
    public const string IpcStateFileRelativePath = "Library/Unityctl/ipc-state.json";
    public const int IpcReloadWaitMs = 60_000;
    public const int IpcReloadPollMs = 750;
    public const int IpcStateStalenessMs = 15_000;

    // A reloading/starting editor cannot refresh its state file while its managed
    // code is being torn down and rebuilt, so updatedAtUtc is frozen at reload start.
    // Trust the reloading/starting state for the whole reload budget (plus margin)
    // instead of the 15s liveness window used for a ready editor. Otherwise any domain
    // reload longer than 15s is misread as a dead editor and the client drops IPC to
    // spawn a batch process mid-reload. Must stay above IpcReloadWaitMs.
    public const int IpcReloadStaleMs = 90_000;

    /// <summary>
    /// Normalize a project path for deterministic pipe name generation.
    /// Handles drive letter case, slash direction, trailing slashes.
    /// </summary>
    public static string NormalizeProjectPath(string projectPath)
    {
        var full = Path.GetFullPath(projectPath);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            full = full.ToLowerInvariant();
        full = full.Replace('\\', '/');
        full = full.TrimEnd('/');
        return full;
    }

    /// <summary>
    /// Compute a deterministic pipe name from a project path.
    /// Both CLI and Plugin must use this same function.
    /// </summary>
    public static string GetPipeName(string projectPath)
    {
        var normalized = NormalizeProjectPath(projectPath);
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
        var hex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        return $"{PipePrefix}{hex.Substring(0, 16)}";
    }

    /// <summary>
    /// Get the unityctl config directory (~/.unityctl/).
    /// </summary>
    public static string GetConfigDirectory()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".unityctl");
    }

    private static string? NormalizeVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
            return null;

        var plusIndex = version.IndexOf('+');
        return plusIndex >= 0 ? version[..plusIndex] : version;
    }
}
