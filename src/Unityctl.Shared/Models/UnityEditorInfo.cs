using System.Text.Json.Serialization;

namespace Unityctl.Shared.Models;

public sealed class UnityEditorInfo
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    [JsonPropertyName("isRunning")]
    public bool IsRunning { get; set; }

    public override string ToString() => $"{Version} — {Location}";
}
