#if UNITY_EDITOR
using System;
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ScriptValidateResultHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ScriptValidateResult;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var requestId = request.GetParam("requestId", null);
            if (string.IsNullOrEmpty(requestId))
                return InvalidParameters("Missing required parameter: requestId");

            var state = AsyncOperationRegistry.TryGet(requestId);
            if (state != null)
            {
                if (state.Status == AsyncStatus.Running)
                {
                    var elapsed = (DateTime.UtcNow - state.StartedAt).TotalSeconds;
                    var data = new JObject
                    {
                        ["requestId"] = requestId,
                        ["elapsed"] = Math.Round(elapsed, 1)
                    };
                    return Ok(StatusCode.Accepted, $"Script validation in progress... ({elapsed:F1}s elapsed)", data);
                }

                // Completed — return stored response (idempotent, no removal)
                return state.Response;
            }

            // Check file-based result (reload-safe fallback)
            var filePath = System.IO.Path.Combine(UnityEngine.Application.dataPath,
                $"../Library/Unityctl/script-validation/{requestId}.json");
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    var json = System.IO.File.ReadAllText(filePath);
                    var fileData = JObject.Parse(json);
                    var succeeded = fileData["succeeded"]?.Value<bool>() ?? false;

                    if (succeeded)
                        return Ok("Compilation succeeded (from persisted result)", fileData);
                    else
                    {
                        var errorCount = fileData["errorCount"]?.Value<int>() ?? 0;
                        var warningCount = fileData["warningCount"]?.Value<int>() ?? 0;
                        var resp = CommandResponse.Fail(StatusCode.BuildFailed,
                            $"Compilation failed: {errorCount} errors, {warningCount} warnings (from persisted result)");
                        resp.data = fileData;
                        return resp;
                    }
                }
                catch
                {
                    // Fall through to not found
                }
            }

            return Fail(StatusCode.NotFound, $"No validation result found for requestId: {requestId}");
        }
    }
}
#endif
