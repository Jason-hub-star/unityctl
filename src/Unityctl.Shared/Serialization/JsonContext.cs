using System.Text.Json;
using System.Text.Json.Serialization;
using Unityctl.Shared.Protocol;

namespace Unityctl.Shared.Serialization;

[JsonSerializable(typeof(CommandRequest))]
[JsonSerializable(typeof(CommandResponse))]
[JsonSerializable(typeof(StatusCode))]
[JsonSerializable(typeof(Dictionary<string, object?>))]
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
public partial class UnityctlJsonContext : JsonSerializerContext
{
}
