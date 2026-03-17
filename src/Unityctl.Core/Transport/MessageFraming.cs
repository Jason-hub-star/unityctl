using System.Buffers;
using System.Text;
using System.Text.Json;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;

namespace Unityctl.Core.Transport;

/// <summary>
/// Async length-prefixed message framing for IPC transport.
/// Wire format: [4 bytes int32 LE: payload length] [N bytes UTF-8: JSON body]
/// </summary>
internal static class MessageFraming
{
    private const int MaxMessageSize = 10 * 1024 * 1024; // 10 MB

    internal static async Task<CommandResponse> SendReceiveAsync(
        Stream stream, CommandRequest request, CancellationToken ct)
    {
        var requestJson = JsonSerializer.Serialize(request, UnityctlJsonContext.Default.CommandRequest);
        await WriteMessageAsync(stream, requestJson, ct);

        var responseJson = await ReadMessageAsync(stream, ct);
        var response = JsonSerializer.Deserialize(responseJson, UnityctlJsonContext.Default.CommandResponse);
        return response ?? CommandResponse.Fail(StatusCode.UnknownError, "Null response from IPC server");
    }

    private static async Task WriteMessageAsync(Stream stream, string json, CancellationToken ct)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(json);
        if (bodyBytes.Length > MaxMessageSize)
            throw new InvalidOperationException($"Message too large: {bodyBytes.Length} bytes (max {MaxMessageSize})");

        var header = BitConverter.GetBytes(bodyBytes.Length);
        await stream.WriteAsync(header.AsMemory(0, 4), ct);
        await stream.WriteAsync(bodyBytes.AsMemory(), ct);
        await stream.FlushAsync(ct);
    }

    private static async Task<string> ReadMessageAsync(Stream stream, CancellationToken ct)
    {
        var headerBuf = new byte[4];
        await ReadExactAsync(stream, headerBuf.AsMemory(), ct);

        int length = BitConverter.ToInt32(headerBuf, 0);
        if (length <= 0 || length > MaxMessageSize)
            throw new InvalidOperationException($"Invalid message length: {length}");

        var bodyBuf = new byte[length];
        await ReadExactAsync(stream, bodyBuf.AsMemory(), ct);

        return Encoding.UTF8.GetString(bodyBuf);
    }

    private static async Task ReadExactAsync(Stream stream, Memory<byte> buffer, CancellationToken ct)
    {
        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer[totalRead..], ct);
            if (read == 0)
                throw new EndOfStreamException("Pipe closed before full message was read.");
            totalRead += read;
        }
    }
}
