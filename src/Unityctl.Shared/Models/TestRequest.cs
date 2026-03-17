using System.Text.Json.Serialization;

namespace Unityctl.Shared.Models;

public sealed class TestRequest
{
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "edit";

    [JsonPropertyName("filter")]
    public string? Filter { get; set; }
}

public sealed class TestResult
{
    [JsonPropertyName("passed")]
    public int Passed { get; set; }

    [JsonPropertyName("failed")]
    public int Failed { get; set; }

    [JsonPropertyName("skipped")]
    public int Skipped { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("failures")]
    public List<TestFailure>? Failures { get; set; }
}

public sealed class TestFailure
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
