using System.Text.Json.Serialization;

namespace Unityctl.Shared.Models;

public sealed class CheckResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("errors")]
    public List<CompileMessage>? Errors { get; set; }

    [JsonPropertyName("warnings")]
    public List<CompileMessage>? Warnings { get; set; }
}

public sealed class CompileMessage
{
    [JsonPropertyName("file")]
    public string? File { get; set; }

    [JsonPropertyName("line")]
    public int Line { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
