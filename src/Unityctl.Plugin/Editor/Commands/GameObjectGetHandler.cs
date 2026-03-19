using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class GameObjectGetHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.GameObjectGet;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
#if UNITY_EDITOR
            var id = request.GetParam("id", null);
            if (string.IsNullOrEmpty(id))
            {
                return InvalidParameters("Parameter 'id' is required.");
            }

            var gameObject = GlobalObjectIdResolver.Resolve<UnityEngine.GameObject>(id);
            if (gameObject == null)
            {
                return Fail(StatusCode.NotFound, $"GameObject not found: {id}");
            }

            var components = new JArray();
            foreach (var component in gameObject.GetComponents<UnityEngine.Component>())
            {
                if (component == null)
                {
                    continue;
                }

                components.Add(SceneExplorationUtility.CreateComponentSummary(component));
            }

            return Ok($"GameObject '{gameObject.name}'", new JObject
            {
                ["globalObjectId"] = id,
                ["name"] = gameObject.name,
                ["scenePath"] = SceneExplorationUtility.GetHierarchyPath(gameObject),
                ["sceneAssetPath"] = gameObject.scene.path,
                ["activeSelf"] = gameObject.activeSelf,
                ["layer"] = gameObject.layer,
                ["tag"] = gameObject.tag,
                ["components"] = components
            });
#else
            return NotInEditor();
#endif
        }
    }
}
