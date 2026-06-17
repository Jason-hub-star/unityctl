#nullable enable

using System.Text.Json;
using Unityctl.Shared;
using Unityctl.Shared.Models;
using Unityctl.Shared.Serialization;

namespace Unityctl.Core.Transport;

/// <summary>
/// Reads the IPC state file written by the plugin.
/// Returns null if file is missing, unparseable, or unreadable.
/// Never throws.
/// </summary>
public interface IIpcStateReader
{
    IpcState? Read(string projectPath);
}

/// <summary>
/// Production implementation of IIpcStateReader.
/// Computes the state file path using the same logic as the plugin,
/// then deserializes with System.Text.Json source-gen context.
/// </summary>
public sealed class IpcStateReader : IIpcStateReader
{
    /// <summary>
    /// Read the IPC state file for a given project path.
    /// Returns null if file missing, unparseable, or unreadable (exceptions swallowed).
    /// </summary>
    public IpcState? Read(string projectPath)
    {
        try
        {
            var filePath = ComputeStatePath(projectPath);
            if (!File.Exists(filePath))
                return null;

            var json = File.ReadAllText(filePath);
            var state = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.IpcState);
            return state;
        }
        catch
        {
            // State file read failures must not break command execution
            return null;
        }
    }

    /// <summary>
    /// Compute the state file path using the same logic as the plugin.
    /// Uses Constants.NormalizeProjectPath() for deterministic path construction.
    /// </summary>
    private static string ComputeStatePath(string projectPath)
    {
        var normalized = Constants.NormalizeProjectPath(Path.GetFullPath(projectPath));
        // Convert normalized path back to platform-native format for file operations
        var nativePath = normalized.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(nativePath, "Library", "Unityctl", "ipc-state.json");
    }
}

/// <summary>
/// Extension methods for IpcState freshness checks.
/// </summary>
public static class IpcStateExtensions
{
    /// <summary>
    /// Check if the state is fresh (timestamp within stalenessMs).
    /// </summary>
    public static bool IsFresh(this IpcState state, int stalenessMs = Constants.IpcStateStalenessMs)
    {
        var ageMs = (DateTime.UtcNow - state.UpdatedAtUtc).TotalMilliseconds;
        return ageMs >= 0 && ageMs <= stalenessMs;
    }

    /// <summary>
    /// Check if state is reloading and fresh.
    /// </summary>
    public static bool IsReloadingFresh(this IpcState state, int stalenessMs = Constants.IpcStateStalenessMs)
    {
        return state.State == IpcStateValues.Reloading && state.IsFresh(stalenessMs);
    }

    /// <summary>
    /// Check if state is starting and fresh.
    /// </summary>
    public static bool IsStartingFresh(this IpcState state, int stalenessMs = Constants.IpcStateStalenessMs)
    {
        return state.State == IpcStateValues.Starting && state.IsFresh(stalenessMs);
    }

    /// <summary>
    /// Check if state is stopped and fresh.
    /// </summary>
    public static bool IsStoppedFresh(this IpcState state, int stalenessMs = Constants.IpcStateStalenessMs)
    {
        return state.State == IpcStateValues.Stopped && state.IsFresh(stalenessMs);
    }
}
