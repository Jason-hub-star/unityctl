#if UNITY_EDITOR
using System;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class TestResultHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.TestResult;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var requestId = request.GetParam("requestId", null);
            if (string.IsNullOrEmpty(requestId))
                return InvalidParameters("Missing required parameter: requestId");

            var state = AsyncOperationRegistry.TryGet(requestId);
            if (state == null)
                return InvalidParameters($"No async operation found for requestId: {requestId}");

            if (state.Status == AsyncStatus.Running)
            {
                var elapsed = (DateTime.UtcNow - state.StartedAt).TotalSeconds;
                var data = new JObject
                {
                    ["requestId"] = requestId,
                    ["elapsed"] = Math.Round(elapsed, 1)
                };
                return Ok(StatusCode.Accepted, $"Tests still running... ({elapsed:F1}s elapsed)", data);
            }

            // Completed — return stored response (idempotent, no removal)
            return state.Response;
        }
    }
}
#endif
