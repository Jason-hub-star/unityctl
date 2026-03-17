using System.Text.Json.Serialization;

namespace Unityctl.Shared.Protocol;

public sealed class CommandResponse
{
    [JsonPropertyName("statusCode")]
    public StatusCode StatusCode { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public Dictionary<string, object?>? Data { get; set; }

    [JsonPropertyName("errors")]
    public List<string>? Errors { get; set; }

    [JsonPropertyName("requestId")]
    public string? RequestId { get; set; }

    public static CommandResponse Ok(string? message = null, Dictionary<string, object?>? data = null) =>
        new()
        {
            StatusCode = StatusCode.Ready,
            Success = true,
            Message = message,
            Data = data
        };

    public static CommandResponse Fail(StatusCode code, string message, List<string>? errors = null) =>
        new()
        {
            StatusCode = code,
            Success = false,
            Message = message,
            Errors = errors
        };
}
