using System.IO.Pipes;
using Unityctl.Shared;
using Unityctl.Shared.Protocol;
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

    public async Task<CommandResponse> SendAsync(CommandRequest request, CancellationToken ct = default)
    {
        try
        {
            var pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await using (pipe.ConfigureAwait(false))
            {
                await pipe.ConnectAsync(Constants.IpcConnectTimeoutMs, ct).ConfigureAwait(false);
                return await MessageFraming.SendReceiveAsync(pipe, request, ct).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            throw; // Let cancellation propagate
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
        // Phase 3C: streaming implementation
        return null;
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
