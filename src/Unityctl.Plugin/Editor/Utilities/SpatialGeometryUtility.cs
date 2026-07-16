#if UNITY_EDITOR
using System;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Unityctl.Plugin.Editor.Utilities
{
    /// <summary>
    /// Turns scene geometry into measured spatial facts (world AABB, true oriented
    /// dimensions, surface normal, predicate checks) so an agent can reason about
    /// space from numbers instead of a screenshot. World bounds come from Renderer
    /// (visual extent); true dimensions and orientation come from the mesh's local
    /// bounds transformed by rotation/scale, so a rotated slab is not mistaken for
    /// a fat box.
    /// </summary>
    public static class SpatialGeometryUtility
    {
        /// <summary>
        /// Resolve a target string to a GameObject. Tries GlobalObjectId first, then
        /// an exact name match, then a hierarchy-path suffix match, across loaded scenes.
        /// Returns null if nothing matches.
        /// </summary>
        public static GameObject ResolveTarget(string target)
        {
            if (string.IsNullOrWhiteSpace(target))
                return null;

            var byId = GlobalObjectIdResolver.Resolve<GameObject>(target);
            if (byId != null)
                return byId;

            GameObject exactName = null;
            GameObject pathMatch = null;
            var sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
            for (var i = 0; i < sceneCount && (exactName == null || pathMatch == null); i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded)
                    continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    Search(root, string.Empty, target, ref exactName, ref pathMatch);
                    if (exactName != null && pathMatch != null)
                        break;
                }
            }

            return exactName ?? pathMatch;
        }

        private static void Search(GameObject go, string parentPath, string target,
            ref GameObject exactName, ref GameObject pathMatch)
        {
            var path = SceneExplorationUtility.GetHierarchyPath(go, parentPath);
            if (exactName == null && string.Equals(go.name, target, StringComparison.Ordinal))
                exactName = go;
            if (pathMatch == null && !string.IsNullOrEmpty(path)
                && path.EndsWith(target, StringComparison.Ordinal))
                pathMatch = go;

            for (var i = 0; i < go.transform.childCount; i++)
                Search(go.transform.GetChild(i).gameObject, path, target, ref exactName, ref pathMatch);
        }

        /// <summary>
        /// World-space axis-aligned bounds from renderers (fallback: colliders, then a
        /// zero-size point at the transform). <paramref name="source"/> records which.
        /// </summary>
        public static Bounds ComputeWorldBounds(GameObject go, out string source)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(includeInactive: true);
            Bounds? acc = null;
            foreach (var r in renderers)
                acc = acc == null ? r.bounds : Encapsulate(acc.Value, r.bounds);
            if (acc != null) { source = "renderer"; return acc.Value; }

            var colliders = go.GetComponentsInChildren<Collider>(includeInactive: true);
            foreach (var c in colliders)
                acc = acc == null ? c.bounds : Encapsulate(acc.Value, c.bounds);
            if (acc != null) { source = "collider"; return acc.Value; }

            source = "transform";
            return new Bounds(go.transform.position, Vector3.zero);
        }

        private static Bounds Encapsulate(Bounds a, Bounds b)
        {
            a.Encapsulate(b);
            return a;
        }

        /// <summary>
        /// True (rotation-invariant) dimensions from the mesh's local bounds scaled by
        /// lossyScale. Returns false when the object has no mesh (orientation unknown).
        /// </summary>
        public static bool TryComputeTrueSize(GameObject go, out Vector3 trueSize)
        {
            var mf = go.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                var m = mf.sharedMesh.bounds.size;
                var s = go.transform.lossyScale;
                trueSize = new Vector3(
                    m.x * Mathf.Abs(s.x),
                    m.y * Mathf.Abs(s.y),
                    m.z * Mathf.Abs(s.z));
                return true;
            }

            trueSize = Vector3.zero;
            return false;
        }

        /// <summary>Index (0=x,1=y,2=z) of the smallest component.</summary>
        public static int ThinAxisIndex(Vector3 size)
        {
            if (size.x <= size.y && size.x <= size.z) return 0;
            if (size.y <= size.x && size.y <= size.z) return 1;
            return 2;
        }

        /// <summary>Index of the largest component.</summary>
        public static int LongAxisIndex(Vector3 size)
        {
            if (size.x >= size.y && size.x >= size.z) return 0;
            if (size.y >= size.x && size.y >= size.z) return 1;
            return 2;
        }

        public static Vector3 LocalAxisUnit(int index)
        {
            return index == 0 ? Vector3.right : index == 1 ? Vector3.up : Vector3.forward;
        }

        /// <summary>Nearest world axis label for a direction, e.g. "+Y" / "-Z".</summary>
        public static string WorldAxisLabel(Vector3 dir)
        {
            var ax = Mathf.Abs(dir.x);
            var ay = Mathf.Abs(dir.y);
            var az = Mathf.Abs(dir.z);
            if (ax >= ay && ax >= az) return dir.x >= 0 ? "+X" : "-X";
            if (ay >= ax && ay >= az) return dir.y >= 0 ? "+Y" : "-Y";
            return dir.z >= 0 ? "+Z" : "-Z";
        }

        public static JObject Vec(Vector3 v)
        {
            return new JObject { ["x"] = v.x, ["y"] = v.y, ["z"] = v.z };
        }

        private static readonly string[] AxisNames = { "X", "Y", "Z" };

        public static string AxisName(int index) => AxisNames[index];

        /// <summary>
        /// Evaluate a spatial predicate between subject (a) and target (b). Returns a
        /// JObject with <c>pass</c> plus numeric reasons. <paramref name="predicate"/>
        /// is lowercase and pre-validated by the CLI.
        /// </summary>
        public static JObject EvaluatePredicate(string predicate, GameObject a, GameObject b)
        {
            var aB = ComputeWorldBounds(a, out _);
            var bB = ComputeWorldBounds(b, out _);
            var tol = Mathf.Max(0.001f, 0.02f * bB.size.magnitude);

            switch (predicate)
            {
                case "overlaps": return Overlaps(aB, bB);
                case "inside": return Inside(aB, bB, tol);
                case "on-top-of": return OnTopOf(aB, bB, tol);
                case "covers": return Covers(a, aB, bB, tol);
                case "aligned": return Aligned(a, b, aB, bB, tol);
                default:
                    return new JObject { ["pass"] = false, ["reason"] = $"unknown predicate '{predicate}'" };
            }
        }

        private static JObject Overlaps(Bounds a, Bounds b)
        {
            var overlap = new Vector3(
                Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x),
                Mathf.Min(a.max.y, b.max.y) - Mathf.Max(a.min.y, b.min.y),
                Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z));
            var pass = overlap.x > 0 && overlap.y > 0 && overlap.z > 0;
            var volume = pass ? overlap.x * overlap.y * overlap.z : 0f;
            return new JObject
            {
                ["pass"] = pass,
                ["overlapExtents"] = Vec(overlap),
                ["overlapVolume"] = volume,
                ["reason"] = pass
                    ? $"AABBs intersect; overlap volume {volume:0.###} m^3"
                    : "AABBs do not intersect on all three axes"
            };
        }

        private static JObject Inside(Bounds a, Bounds b, float tol)
        {
            // Positive overhang = how far subject sticks out past target on that side.
            var overMin = new Vector3(b.min.x - a.min.x, b.min.y - a.min.y, b.min.z - a.min.z);
            var overMax = new Vector3(a.max.x - b.max.x, a.max.y - b.max.y, a.max.z - b.max.z);
            var worst = Mathf.Max(
                Mathf.Max(overMin.x, overMin.y, overMin.z),
                Mathf.Max(overMax.x, overMax.y, overMax.z));
            var pass = worst <= tol;
            return new JObject
            {
                ["pass"] = pass,
                ["maxOverhang"] = worst,
                ["overhangMin"] = Vec(overMin),
                ["overhangMax"] = Vec(overMax),
                ["tolerance"] = tol,
                ["reason"] = pass
                    ? "subject bounds fully within target bounds"
                    : $"subject sticks out {worst:0.###} m past target (tolerance {tol:0.###})"
            };
        }

        private static JObject OnTopOf(Bounds a, Bounds b, float tol)
        {
            var gapY = a.min.y - b.max.y;
            var xzOverlapX = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
            var xzOverlapZ = Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z);
            var resting = Mathf.Abs(gapY) <= Mathf.Max(tol, 0.02f * b.size.y + 0.001f);
            var overlapping = xzOverlapX > 0 && xzOverlapZ > 0;
            var pass = resting && overlapping;
            return new JObject
            {
                ["pass"] = pass,
                ["gapY"] = gapY,
                ["xzOverlapX"] = xzOverlapX,
                ["xzOverlapZ"] = xzOverlapZ,
                ["reason"] = pass
                    ? $"subject rests on target (gapY {gapY:0.###} m, footprints overlap)"
                    : $"gapY {gapY:0.###} m, xz overlap ({xzOverlapX:0.###}, {xzOverlapZ:0.###}) — not resting on target"
            };
        }

        private static JObject Covers(GameObject aGo, Bounds a, Bounds b, float tol)
        {
            // Footprint spans the target opening (XZ).
            var footprintOkX = a.size.x + tol >= b.size.x;
            var footprintOkZ = a.size.z + tol >= b.size.z;

            // Positioned at/above the target top (small gap or slight overlap).
            var gapY = a.min.y - b.max.y;
            var positionOk = gapY <= Mathf.Max(tol, 0.05f) && gapY >= -0.5f * b.size.y;

            // Oriented as a horizontal lid: thin axis points along world up.
            float rotationError = -1f;
            bool orientationOk = true; // unknown-orientation objects are not penalized here
            string orientationNote;
            if (TryComputeTrueSize(aGo, out var trueSize))
            {
                var thin = ThinAxisIndex(trueSize);
                var thinWorld = aGo.transform.rotation * LocalAxisUnit(thin);
                rotationError = Vector3.Angle(thinWorld, Vector3.up);
                if (rotationError > 90f) rotationError = 180f - rotationError; // treat ±up the same
                orientationOk = rotationError <= 15f;
                orientationNote = orientationOk
                    ? $"lid orientation ok (thin axis {rotationError:0.#}° off vertical)"
                    : $"thin axis is {rotationError:0.#}° off vertical — subject is standing, not lying flat";
            }
            else
            {
                orientationNote = "orientation unknown (no mesh); checked footprint and position only";
            }

            var pass = footprintOkX && footprintOkZ && positionOk && orientationOk;
            var reasons = new JArray();
            if (!footprintOkX) reasons.Add($"footprint X {a.size.x:0.###} < needed {b.size.x:0.###}");
            if (!footprintOkZ) reasons.Add($"footprint Z {a.size.z:0.###} < needed {b.size.z:0.###}");
            if (!positionOk) reasons.Add($"gapY {gapY:0.###} m — not seated over the target top");
            if (rotationError > 15f) reasons.Add(orientationNote);
            if (pass) reasons.Add("footprint spans target, seated on top, lying flat");

            return new JObject
            {
                ["pass"] = pass,
                ["footprint"] = new JObject { ["x"] = a.size.x, ["z"] = a.size.z },
                ["footprintNeeded"] = new JObject { ["x"] = b.size.x, ["z"] = b.size.z },
                ["gapY"] = gapY,
                ["rotationErrorFromHorizontal"] = rotationError,
                ["orientationNote"] = orientationNote,
                ["reason"] = string.Join("; ", reasons.ToObject<string[]>())
            };
        }

        private static JObject Aligned(GameObject aGo, GameObject bGo, Bounds a, Bounds b, float tol)
        {
            var delta = new Vector3(
                a.center.x - b.center.x,
                a.center.y - b.center.y,
                a.center.z - b.center.z);
            var axesAligned = 0;
            if (Mathf.Abs(delta.x) <= tol) axesAligned++;
            if (Mathf.Abs(delta.y) <= tol) axesAligned++;
            if (Mathf.Abs(delta.z) <= tol) axesAligned++;
            var rotationDelta = Quaternion.Angle(aGo.transform.rotation, bGo.transform.rotation);
            var pass = axesAligned >= 2 && rotationDelta <= 5f;
            return new JObject
            {
                ["pass"] = pass,
                ["centerDelta"] = Vec(delta),
                ["axesAlignedWithinTolerance"] = axesAligned,
                ["rotationDeltaDegrees"] = rotationDelta,
                ["tolerance"] = tol,
                ["reason"] = pass
                    ? $"centers coincide on {axesAligned} axes, rotation delta {rotationDelta:0.#}°"
                    : $"only {axesAligned} axes aligned (delta {delta.x:0.###},{delta.y:0.###},{delta.z:0.###}), rotation delta {rotationDelta:0.#}°"
            };
        }
    }
}
#endif
