using System.IO.Pipes;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Unityctl.Shared;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;
using Unityctl.Shared.Transport;

namespace Unityctl.Core.Transport;

/// <summary>
/// IPC transport: communicates with running Unity Editor via named pipe.
/// Each method creates its own connection (connect-per-call).
/// </summary>
public sealed class IpcTransport : ITransport
{
    private readonly string _pipeName;

    public string Name => "ipc";
    public TransportCapability Capabilities =>
        TransportCapability.Command | TransportCapability.Streaming |
        TransportCapability.Bidirectional | TransportCapability.LowLatency;

    public IpcTransport(string projectPath)
    {
        _pipeName = Constants.GetPipeName(projectPath);
    }

    /// <summary>Internal constructor for tests — uses raw pipe name instead of hashing projectPath.</summary>
    internal IpcTransport(string pipeName, bool useRawPipeName)
    {
        _pipeName = useRawPipeName ? pipeName : Constants.GetPipeName(pipeName);
    }

    public async Task<CommandResponse> SendAsync(CommandRequest request, CancellationToken ct = default)
    {
        try
        {
            var pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await using (pipe.ConfigureAwait(false))
            {
                await pipe.ConnectAsync(Constants.IpcConnectTimeoutMs, ct).ConfigureAwait(false);

                // Message-level timeout: 30s to prevent hanging on partial server writes
                using var messageCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                messageCts.CancelAfter(TimeSpan.FromMilliseconds(Constants.IpcMessageTimeoutMs));

                return await MessageFraming.SendReceiveAsync(pipe, request, messageCts.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Message timeout (not user cancellation)
            return CommandResponse.Fail(StatusCode.Busy,
                "IPC message timed out (30s). Unity Editor may be frozen or in domain reload. Try again.");
        }
        catch (OperationCanceledException)
        {
            throw; // User cancellation propagates
        }
        catch (TimeoutException)
        {
            return CommandResponse.Fail(StatusCode.Busy, "IPC connection timed out. Unity Editor may be busy.");
        }
        catch (IOException ex)
        {
            return CommandResponse.Fail(StatusCode.UnknownError, $"IPC communication error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return CommandResponse.Fail(StatusCode.UnknownError, $"IPC error: {ex.Message}");
        }
    }

    public IAsyncEnumerable<EventEnvelope>? SubscribeAsync(string channel, CancellationToken ct = default)
    {
        return SubscribeAsyncCore(channel, ct);
    }

    private async IAsyncEnumerable<EventEnvelope> SubscribeAsyncCore(
        string channel,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var pipe = await ConnectAndHandshakeAsync(channel, ct).ConfigureAwait(false);
        if (pipe == null) yield break;

        await using (pipe)
        {
            while (!ct.IsCancellationRequested)
            {
                var json = await TryReadNextMessageAsync(pipe, ct).ConfigureAwait(false);
                if (json == null) yield break;

                EventEnvelope? envelope;
                try
                {
                    envelope = JsonSerializer.Deserialize(json, UnityctlJsonContext.Default.EventEnvelope);
                }
                catch
                {
                    continue; // skip malformed messages
                }

                if (envelope == null) continue;
                if (envelope.Channel == "_close") yield break;

                yield return envelope;
            }
        }
    }

    private async Task<NamedPipeClientStream?> ConnectAndHandshakeAsync(string channel, CancellationToken ct)
    {
        NamedPipeClientStream? pipe = null;
        try
        {
            pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await pipe.ConnectAsync(Constants.IpcConnectTimeoutMs, ct).ConfigureAwait(false);

            var request = new CommandRequest
            {
                Command = WellKnownCommands.Watch,
                Parameters = new JsonObject { ["channel"] = channel }
            };
            var requestJson = JsonSerializer.Serialize(request, UnityctlJsonContext.Default.CommandRequest);
            await MessageFraming.WriteMessageAsync(pipe, requestJson, ct).ConfigureAwait(false);

            var responseJson = await MessageFraming.ReadMessageAsync(pipe, ct).ConfigureAwait(false);
            if (responseJson == null) { await pipe.DisposeAsync(); return null; }
            var response = JsonSerializer.Deserialize(responseJson, UnityctlJsonContext.Default.CommandResponse);
            if (response?.Success == true) return pipe;

            await pipe.DisposeAsync().ConfigureAwait(false);
            return null;
        }
        catch
        {
            if (pipe != null) await pipe.DisposeAsync().ConfigureAwait(false);
            return null;
        }
    }

    private static async Task<string?> TryReadNextMessageAsync(NamedPipeClientStream pipe, CancellationToken ct)
    {
        try
        {
            return await MessageFraming.ReadMessageAsync(pipe, ct).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> ProbeAsync(CancellationToken ct = default)
    {
        try
        {
            var pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await using (pipe.ConfigureAwait(false))
            {
                await pipe.ConnectAsync(1000, ct).ConfigureAwait(false);
                return true;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
