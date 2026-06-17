#nullable enable

using System.Text.Json.Serialization;

namespace Unityctl.Shared.Models;

/// <summary>
/// Represents the IPC state of a Unity Editor instance, read from the state file
/// written by the plugin. Fields match the JSON structure on disk: pipeName, pid,
/// unityVersion, state, updatedAtUtc (ISO-8601 round-trip format).
/// </summary>
public sealed class IpcState
{
    [JsonPropertyName("pipeName")]
    public string? PipeName { get; set; }

    [JsonPropertyName("pid")]
    public int Pid { get; set; }

    [JsonPropertyName("unityVersion")]
    public string? UnityVersion { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("updatedAtUtc")]
    public DateTime UpdatedAtUtc { get; set; }
}

/// <summary>
/// String constants for IPC state values. Must match plugin's IpcStateValues.
/// </summary>
public static class IpcStateValues
{
    public const string Starting = "starting";
    public const string Ready = "ready";
    public const string Reloading = "reloading";
    public const string Stopped = "stopped";
}
