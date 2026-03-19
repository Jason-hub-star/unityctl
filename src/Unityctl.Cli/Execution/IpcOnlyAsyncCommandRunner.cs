using System.Text.Json.Nodes;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Execution;

internal static class IpcOnlyAsyncCommandRunner
{
    internal static Task<CommandResponse> ExecuteAsync(
        string project,
        CommandRequest request,
        string pollCommand,
        int timeoutSeconds,
        string reconnectMessage,
        CancellationToken ct = default)
    {
        return AsyncCommandRunner.ExecuteAsync(
            project,
            request,
            async (proj, req, cancellationToken) =>
            {
                var ipc = new IpcTransport(proj);
                var isPoll = string.Equals(req.Command, pollCommand, StringComparison.Ordinal);

                if (!await ipc.ProbeAsync(cancellationToken).ConfigureAwait(false))
                {
                    if (isPoll)
                    {
                        return new CommandResponse
                        {
                            StatusCode = StatusCode.Accepted,
                            Success = true,
                            Message = reconnectMessage,
                            Data = new JsonObject
                            {
                                ["requestId"] = req.Parameters?["requestId"]?.GetValue<string>()
                            }
                        };
                    }

                    return CommandResponse.Fail(
                        StatusCode.InvalidParameters,
                        $"{request.Command} is IPC-only. Open the Unity project in the Editor and retry.");
                }

                var response = await ipc.SendAsync(req, cancellationToken).ConfigureAwait(false);
                if (!isPoll && response.StatusCode is StatusCode.UnknownError or StatusCode.Busy)
                {
                    var requestId = req.RequestId;
                    if (!string.IsNullOrWhiteSpace(requestId) && BuildStateFileExists(proj, requestId))
                    {
                        return new CommandResponse
                        {
                            StatusCode = StatusCode.Accepted,
                            Success = true,
                            Message = reconnectMessage,
                            Data = new JsonObject
                            {
                                ["requestId"] = requestId
                            },
                            RequestId = requestId
                        };
                    }
                }

                if (isPoll && response.StatusCode is StatusCode.UnknownError or StatusCode.Busy)
                {
                    return new CommandResponse
                    {
                        StatusCode = StatusCode.Accepted,
                        Success = true,
                        Message = reconnectMessage,
                        Data = new JsonObject
                        {
                            ["requestId"] = req.Parameters?["requestId"]?.GetValue<string>()
                        }
                    };
                }

                return response;
            },
            pollCommand: pollCommand,
            timeoutSeconds: timeoutSeconds,
            timeoutStatusCode: StatusCode.Busy,
            timeoutMessage: $"{request.Command} timed out after {timeoutSeconds}s",
            ct: ct);
    }

    private static bool BuildStateFileExists(string project, string requestId)
    {
        var path = Path.Combine(Path.GetFullPath(project), "Library", "Unityctl", "build-state", requestId + ".json");
        return File.Exists(path);
    }
}
