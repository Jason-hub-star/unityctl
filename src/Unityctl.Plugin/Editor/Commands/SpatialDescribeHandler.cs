#if UNITY_EDITOR
using Newtonsoft.Json.Linq;
using UnityEngine;
using Unityctl.Plugin.Editor.Shared;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Commands
{
    public class SpatialDescribeHandler : CommandHandlerBase
    {
        public override string CommandName => WellKnownCommands.SpatialDescribe;

        protected override CommandResponse ExecuteInEditor(CommandRequest request)
        {
            var target = request.GetParam("target", null);
            if (string.IsNullOrEmpty(target))
                return InvalidParameters("Parameter 'target' is required.");

            var go = SpatialGeometryUtility.ResolveTarget(target);
            if (go == null)
                return Fail(StatusCode.NotFound, $"GameObject not found: {target}");

            var full = request.GetParam<bool>("full");
            var world = SpatialGeometryUtility.ComputeWorldBounds(go, out var source);
            var hasTrue = SpatialGeometryUtility.TryComputeTrueSize(go, out var trueSize);

            // True (rotation-invariant) dimensions decide the thin/long axis when a mesh
            // exists; otherwise fall back to the world AABB size.
            var sizeForAxes = hasTrue ? trueSize : world.size;
            var thin = SpatialGeometryUtility.ThinAxisIndex(sizeForAxes);
            var longAxis = SpatialGeometryUtility.LongAxisIndex(sizeForAxes);
            var thinWorldDir = hasTrue
                ? (go.transform.rotation * SpatialGeometryUtility.LocalAxisUnit(thin))
                : SpatialGeometryUtility.LocalAxisUnit(thin);
            var pivotOffset = go.transform.position - world.center;

            var worldBounds = new JObject
            {
                ["center"] = SpatialGeometryUtility.Vec(world.center),
                ["size"] = SpatialGeometryUtility.Vec(world.size)
            };

            var data = new JObject
            {
                ["globalObjectId"] = GlobalObjectIdResolver.GetId(go),
                ["name"] = go.name,
                ["hierarchyPath"] = SceneExplorationUtility.GetHierarchyPath(go),
                ["boundsSource"] = source,
                ["worldBounds"] = worldBounds,
                ["thinAxis"] = SpatialGeometryUtility.AxisName(thin),
                ["longAxis"] = SpatialGeometryUtility.AxisName(longAxis),
                // Surface normal = world direction the flat (thin-axis) face points.
                ["surfaceNormal"] = SpatialGeometryUtility.WorldAxisLabel(thinWorldDir),
                ["pivotOffsetMagnitude"] = pivotOffset.magnitude
            };

            if (hasTrue)
                data["trueSize"] = SpatialGeometryUtility.Vec(trueSize);

            if (full)
            {
                worldBounds["min"] = SpatialGeometryUtility.Vec(world.min);
                worldBounds["max"] = SpatialGeometryUtility.Vec(world.max);
                data["position"] = SpatialGeometryUtility.Vec(go.transform.position);
                data["rotationEuler"] = SpatialGeometryUtility.Vec(go.transform.rotation.eulerAngles);
                data["lossyScale"] = SpatialGeometryUtility.Vec(go.transform.lossyScale);
                data["pivotOffset"] = SpatialGeometryUtility.Vec(pivotOffset);
                data["surfaceNormalVector"] = SpatialGeometryUtility.Vec(thinWorldDir);
            }

            return Ok($"Spatial description of '{go.name}'", data);
        }
    }
}
#endif
