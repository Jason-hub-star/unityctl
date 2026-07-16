using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class PlayModeHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.PlayMode;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var action = request.GetParam("action", "").ToLowerInvariant();

            switch (action)
            {
                case "start":
                    if (UnityEditor.EditorApplication.isPlaying)
                        return Ok("Already in play mode", PlayModeData(true));
                    UnityEditor.EditorApplication.isPlaying = true;
                    return Ok("Play mode started", PlayModeData(true));

                case "stop":
                    if (!UnityEditor.EditorApplication.isPlaying)
                    {
                        UnityEditor.EditorApplication.isPaused = false;
                        return Ok("Already stopped", PlayModeData(false));
                    }
                    UnityEditor.EditorApplication.isPaused = false;
                    UnityEditor.EditorApplication.isPlaying = false;
                    return Ok("Play mode stopped", PlayModeData(false));

                case "pause":
                    UnityEditor.EditorApplication.isPaused = !UnityEditor.EditorApplication.isPaused;
                    var paused = UnityEditor.EditorApplication.isPaused;
                    return Ok(paused ? "Paused" : "Unpaused", new JObject
                    {
                        ["isPlaying"] = UnityEditor.EditorApplication.isPlaying,
                        ["isPaused"] = paused
                    });

                case "step":
                    // Deterministic, focus-independent frame advance so an agent can
                    // verify time-based gameplay without the unfocused play-mode throttle.
                    if (!UnityEditor.EditorApplication.isPlaying)
                        return InvalidParameters("play step requires play mode. Run 'play start' first.");
                    var frames = request.GetParam<int>("frames");
                    if (frames <= 0) frames = 1;
                    UnityEditor.EditorApplication.isPaused = true;
                    for (int i = 0; i < frames; i++)
                        UnityEditor.EditorApplication.Step();
                    return Ok($"Stepped {frames} frame(s)", new JObject
                    {
                        ["isPlaying"] = true,
                        ["isPaused"] = UnityEditor.EditorApplication.isPaused,
                        ["framesRequested"] = frames
                    });

                default:
                    return InvalidParameters(
                        $"Unknown action: '{action}'. Valid actions: start, stop, pause, step");
            }
#else
            return NotInEditor();
#endif
        }

#if UNITY_EDITOR
        private static JObject PlayModeData(bool isPlaying)
        {
            return new JObject
            {
                ["isPlaying"] = isPlaying,
                ["isPaused"] = UnityEditor.EditorApplication.isPaused
            };
        }
#endif
    }
}
