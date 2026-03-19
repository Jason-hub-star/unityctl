#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class BuildProfileListHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.BuildProfileList;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            BuildTransitionStateStore.PruneDefault();

            var activeProfile = BuildProfileUtility.GetActiveProfileSummary();
            var activeProfileId = activeProfile?["id"]?.Value<string>();
            var activeProfilePath = BuildProfileUtility.GetActiveProfilePath();
            var activeTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;

            var profiles = new JArray();
            foreach (var target in BuildProfileUtility.EnumerateListTargets(activeTarget))
            {
                profiles.Add(BuildProfileUtility.CreatePlatformSummary(
                    target,
                    string.Equals(
                        BuildProfileUtility.GetPlatformProfileId(BuildProfileUtility.ToCanonicalName(target)),
                        activeProfileId,
                        System.StringComparison.Ordinal)));
            }

            foreach (var profile in BuildProfileUtility.GetCustomProfileSummaries(activeProfilePath))
            {
                profiles.Add(profile);
            }

            return Ok("Build profiles captured", new JObject
            {
                ["count"] = profiles.Count,
                ["activeProfileId"] = activeProfileId,
                ["profiles"] = profiles
            });
        }
    }
}
#endif
