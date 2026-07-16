#if UNITY_EDITOR
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class SpatialCheckHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.SpatialCheck;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var subject = request.GetParam("subject", null);
            var predicate = request.GetParam("predicate", null);
            var target = request.GetParam("target", null);

            if (string.IsNullOrEmpty(subject))
                return InvalidParameters("Parameter 'subject' is required.");
            if (string.IsNullOrEmpty(predicate))
                return InvalidParameters("Parameter 'predicate' is required.");
            if (string.IsNullOrEmpty(target))
                return InvalidParameters("Parameter 'target' is required.");

            var a = SpatialGeometryUtility.ResolveTarget(subject);
            if (a == null)
                return Fail(StatusCode.NotFound, $"Subject GameObject not found: {subject}");

            var b = SpatialGeometryUtility.ResolveTarget(target);
            if (b == null)
                return Fail(StatusCode.NotFound, $"Target GameObject not found: {target}");

            var normalized = predicate.Trim().ToLowerInvariant();
            var result = SpatialGeometryUtility.EvaluatePredicate(normalized, a, b);
            result["predicate"] = normalized;
            result["subject"] = a.name;
            result["target"] = b.name;

            var pass = result.Value<bool>("pass");
            return Ok($"{a.name} {normalized} {b.name}: {(pass ? "PASS" : "FAIL")}", result);
        }
    }
}
#endif
