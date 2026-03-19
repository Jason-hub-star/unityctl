using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class TagAddHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.TagAdd;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var name = request.GetParam("name", null);
            if (string.IsNullOrEmpty(name))
                return InvalidParameters("Parameter 'name' is required.");

            var existing = UnityEditorInternal.InternalEditorUtility.tags;
            foreach (var tag in existing)
            {
                if (string.Equals(tag, name, System.StringComparison.OrdinalIgnoreCase))
                    return Fail(StatusCode.InvalidParameters, $"Tag '{name}' already exists.");
            }

            UnityEditorInternal.InternalEditorUtility.AddTag(name);

            return Ok($"Tag '{name}' added", new JObject
            {
                ["tag"] = name
            });
#else
            return NotInEditor();
#endif
        }
    }
}
