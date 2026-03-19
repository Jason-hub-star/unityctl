using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class ConsoleGetCountHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.ConsoleGetCount;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            int errors = 0, warnings = 0, logs = 0;

            var logEntriesType = System.Type.GetType("UnityEditor.LogEntries, UnityEditor");
            if (logEntriesType != null)
            {
                var getCountMethod = logEntriesType.GetMethod("GetCountsByType",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (getCountMethod != null)
                {
                    var args = new object[] { 0, 0, 0 };
                    getCountMethod.Invoke(null, args);
                    errors = (int)args[0];
                    warnings = (int)args[1];
                    logs = (int)args[2];
                }
            }

            return Ok($"Console: {logs} logs, {warnings} warnings, {errors} errors", new JObject
            {
                ["logs"] = logs,
                ["warnings"] = warnings,
                ["errors"] = errors,
                ["total"] = logs + warnings + errors
            });
#else
            return NotInEditor();
#endif
        }
    }
}
