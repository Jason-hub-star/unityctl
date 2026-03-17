using System.Text.Json.Nodes;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Execution;

/// <summary>
/// Polls for async command completion after receiving an Accepted response.
/// Uses delegate injection for testability.
/// </summary>
public static class AsyncCommandRunner
{
    private const int InitialDelayMs = 500;
    private const int PollIntervalMs = 1000;

    /// <summary>
    /// Execute a command and poll for completion if it returns Accepted.
    /// </summary>
    /// <param name="project">Unity project path.</param>
    /// <param name="request">The initial command request.</param>
    /// <param name="executor">Delegate that sends a command and returns a response.</param>
    /// <param name="timeoutSeconds">Maximum seconds to wait for completion.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task<CommandResponse> ExecuteAsync(
        string project,
        CommandRequest request,
        Func<string, CommandRequest, CancellationToken, Task<CommandResponse>> executor,
        int timeoutSeconds = 300,
        CancellationToken ct = default)
    {
        var response = await executor(project, request, ct);

        if (response.StatusCode != StatusCode.Accepted)
            return response;

        // Extract requestId from response
        var requestId = response.RequestId;
        if (string.IsNullOrEmpty(requestId))
        {
            // Try from data as fallback
            requestId = response.Data?["requestId"]?.GetValue<string>();
        }

        if (string.IsNullOrEmpty(requestId))
            return response;

        // Poll loop
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        var pollRequest = new CommandRequest
        {
            Command = WellKnownCommands.TestResult,
            Parameters = new JsonObject
            {
                ["requestId"] = requestId
            }
        };

        try
        {
            await Task.Delay(InitialDelayMs, linkedCts.Token);

            while (!linkedCts.Token.IsCancellationRequested)
            {
                var pollResponse = await executor(project, pollRequest, linkedCts.Token);

                if (pollResponse.StatusCode != StatusCode.Accepted)
                    return pollResponse;

                await Task.Delay(PollIntervalMs, linkedCts.Token);
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            return CommandResponse.Fail(
                StatusCode.TestFailed,
                $"Test execution timed out after {timeoutSeconds}s");
        }

        // Caller cancelled
        throw new OperationCanceledException(ct);
    }
}
