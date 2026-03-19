using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ConsoleClearHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ConsoleClear;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.ClearDeveloperConsole();
            // Also clear the Editor log entries
            var logEntriesType = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
            if (logEntriesType != null)
            {
                var clearMethod = logEntriesType.GetMethod("Clear",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                clearMethod?.Invoke(null, null);
            }

            return Ok("Console cleared", new JObject
            {
                ["cleared"] = true
            });
#else
            return NotInEditor();
#endif
        }
    }
}
