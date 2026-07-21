using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    /// <summary>
    /// Triggers AssetDatabase.Refresh() asynchronously via delayCall.
    /// Returns Ready immediately once refresh is successfully scheduled.
    /// IPC-only — batch mode cannot guarantee execution after response.
    /// </summary>
    public class AssetRefreshHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.AssetRefresh;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            if (UnityEngine.Application.isBatchMode)
            {
                return Fail(StatusCode.InvalidParameters,
                    "asset-refresh is IPC-only. Batch mode cannot guarantee execution after response.");
            }

            var requestId = request.requestId;

            // Two update ticks so the response can flush before domain reload side
            // effects begin. update-based dispatch, not delayCall — delayCall never
            // flushes on unattended (unfocused/locked-screen) editors.
            Utilities.MainThreadDispatch.RunDeferred(() =>
            {
                try
                {
                    UnityEditor.AssetDatabase.Refresh();
                }
                catch (System.Exception e)
                {
                    UnityEngine.Debug.LogError($"unityctl asset-refresh delayed execution failed: {e}");
                }
            }, delayTicks: 2);

            return Ok("Asset refresh scheduled", new JObject
            {
                ["requestId"] = requestId,
                ["status"] = "scheduled"
            });
#else
            return NotInEditor();
#endif
        }
    }
}
