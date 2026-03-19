using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public class TagListHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.TagList;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var tags = UnityEditorInternal.InternalEditorUtility.tags;
            var arr = new JArray();
            foreach (var tag in tags)
                arr.Add(tag);

            return Ok($"Found {tags.Length} tags", new JObject
            {
                ["tags"] = arr,
                ["count"] = tags.Length
            });
#else
            return NotInEditor();
#endif
        }
    }
}
