using Unityctl.Core.Discovery;
using Unityctl.Core.Platform;
using Unityctl.Core.Retry;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Transport;

namespace Unityctl.Core.Transport;

/// <summary>
/// Transport orchestrator: selects IPC (fast) → Batch (fallback),
/// applies retry policy, records to flight log.
/// </summary>
public sealed class CommandExecutor
{
    private readonly IPlatformServices _platform;
    private readonly UnityEditorDiscovery _discovery;
    private readonly RetryPolicy _retryPolicy;

    public CommandExecutor(IPlatformServices platform, UnityEditorDiscovery discovery, RetryPolicy? retryPolicy = null)
    {
        _platform = platform;
        _discovery = discovery;
        _retryPolicy = retryPolicy ?? new RetryPolicy();
    }

    /// <summary>
    /// Execute a command against a Unity project, selecting the best transport.
    /// Priority: IPC (if editor running) → Batch (spawn new editor).
    /// </summary>
    public async Task<CommandResponse> ExecuteAsync(
        string projectPath,
        CommandRequest request,
        bool retry = false,
        CancellationToken ct = default)
    {
        projectPath = Path.GetFullPath(projectPath);

        if (retry)
        {
            return await _retryPolicy.ExecuteWithRetryAsync(
                () => ExecuteOnceAsync(projectPath, request, ct), ct);
        }

        return await ExecuteOnceAsync(projectPath, request, ct);
    }

    private async Task<CommandResponse> ExecuteOnceAsync(
        string projectPath,
        CommandRequest request,
        CancellationToken ct)
    {
        // IPC first: probe checks if an Editor with IPC server is running
        await using var ipc = new IpcTransport(projectPath);
        if (await ipc.ProbeAsync(ct))
        {
            return await ipc.SendAsync(request, ct);
        }

        // Fallback: batch transport (only when probe fails, NOT on SendAsync failure)
        await using var batch = new BatchTransport(_platform, _discovery, projectPath);
        return await batch.SendAsync(request, ct);
    }

    /// <summary>
    /// Subscribe to a streaming channel (requires IPC).
    /// </summary>
    public IAsyncEnumerable<EventEnvelope>? WatchAsync(
        string projectPath, string channel, CancellationToken ct = default)
    {
        // Phase 3C: IPC streaming
        var ipc = new IpcTransport(projectPath);
        return ipc.SubscribeAsync(channel, ct);
    }
}
