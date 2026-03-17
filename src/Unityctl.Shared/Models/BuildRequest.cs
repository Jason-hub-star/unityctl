using System.Text.Json.Serialization;

namespace Unityctl.Shared.Models;

public sealed class BuildRequest
{
    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;

    [JsonPropertyName("outputPath")]
    public string? OutputPath { get; set; }

    [JsonPropertyName("scenes")]
    public List<string>? Scenes { get; set; }
}

public sealed class BuildResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("outputPath")]
    public string? OutputPath { get; set; }

    [JsonPropertyName("totalSize")]
    public long TotalSize { get; set; }

    [JsonPropertyName("errors")]
    public List<string>? Errors { get; set; }

    [JsonPropertyName("warnings")]
    public List<string>? Warnings { get; set; }
}
