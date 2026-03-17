using System.Text.Json.Serialization;

namespace Unityctl.Shared.Protocol;

public sealed class CommandRequest
{
    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public Dictionary<string, object?>? Parameters { get; set; }

    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = Guid.NewGuid().ToString("N");
}
