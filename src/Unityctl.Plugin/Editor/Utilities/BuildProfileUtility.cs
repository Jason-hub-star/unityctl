#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Unityctl.Plugin.Editor.Utilities
{
    internal static class BuildProfileUtility
    {
        private const string BuildProfileTypeName = "UnityEditor.Build.Profile.BuildProfile, UnityEditor.BuildProfileModule";

        private sealed class TargetMapping
        {
            public string CanonicalName = string.Empty;
            public UnityEditor.BuildTarget Target;
            public string[] Aliases = Array.Empty<string>();
        }

        private static readonly TargetMapping[] SupportedTargets =
        {
            new TargetMapping
            {
                CanonicalName = "StandaloneWindows64",
                Target = UnityEditor.BuildTarget.StandaloneWindows64,
                Aliases = new[] { "StandaloneWindows64", "win64" }
            },
            new TargetMapping
            {
                CanonicalName = "StandaloneWindows",
                Target = UnityEditor.BuildTarget.StandaloneWindows,
                Aliases = new[] { "StandaloneWindows", "win32" }
            },
            new TargetMapping
            {
                CanonicalName = "StandaloneOSX",
                Target = UnityEditor.BuildTarget.StandaloneOSX,
                Aliases = new[] { "StandaloneOSX", "macos" }
            },
            new TargetMapping
            {
                CanonicalName = "StandaloneLinux64",
                Target = UnityEditor.BuildTarget.StandaloneLinux64,
                Aliases = new[] { "StandaloneLinux64", "linux64" }
            },
            new TargetMapping
            {
                CanonicalName = "Android",
                Target = UnityEditor.BuildTarget.Android,
                Aliases = new[] { "Android" }
            },
            new TargetMapping
            {
                CanonicalName = "iOS",
                Target = UnityEditor.BuildTarget.iOS,
                Aliases = new[] { "iOS", "ios" }
            },
            new TargetMapping
            {
                CanonicalName = "WebGL",
                Target = UnityEditor.BuildTarget.WebGL,
                Aliases = new[] { "WebGL", "webgl" }
            }
        };

        public static string SupportedTargetList => string.Join(", ", SupportedTargets.Select(x => x.CanonicalName));

        public static bool TryParseBuildTarget(string targetName, out UnityEditor.BuildTarget target, out string canonicalName)
        {
            target = default(UnityEditor.BuildTarget);
            canonicalName = string.Empty;

            if (string.IsNullOrWhiteSpace(targetName))
                return false;

            foreach (var mapping in SupportedTargets)
            {
                if (mapping.Aliases.Any(alias => string.Equals(alias, targetName, StringComparison.OrdinalIgnoreCase)))
                {
                    target = mapping.Target;
                    canonicalName = mapping.CanonicalName;
                    return true;
                }
            }

            return false;
        }

        public static string ToCanonicalName(UnityEditor.BuildTarget target)
        {
            foreach (var mapping in SupportedTargets)
            {
                if (mapping.Target == target)
                    return mapping.CanonicalName;
            }

            return target.ToString();
        }

        public static IEnumerable<UnityEditor.BuildTarget> EnumerateListTargets(UnityEditor.BuildTarget activeTarget)
        {
            var targets = SupportedTargets.Select(x => x.Target).ToList();
            if (!targets.Contains(activeTarget))
                targets.Add(activeTarget);

            return targets;
        }

        public static string GetPlatformProfileId(string canonicalTarget)
            => "platform:" + canonicalTarget;

        public static JObject CreatePlatformSummary(UnityEditor.BuildTarget target, bool active)
        {
            var canonical = ToCanonicalName(target);
            return new JObject
            {
                ["id"] = GetPlatformProfileId(canonical),
                ["kind"] = "platform-profile",
                ["name"] = canonical,
                ["target"] = canonical,
                ["path"] = null,
                ["active"] = active
            };
        }

        public static JObject GetActiveProfileSummary()
        {
            var activeTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            var activeBuildProfile = GetActiveBuildProfileObject();
            if (activeBuildProfile == null)
                return CreatePlatformSummary(activeTarget, active: true);

            return CreateCustomSummary(activeBuildProfile, active: true);
        }

        public static IEnumerable<JObject> GetCustomProfileSummaries(string activeProfilePath)
        {
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:BuildProfile"))
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                var asset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path);
                if (asset == null)
                    continue;

                var summary = CreateCustomSummary(asset, string.Equals(path, activeProfilePath, StringComparison.Ordinal));
                if (summary != null)
                    yield return summary;
            }
        }

        public static string GetActiveProfilePath()
        {
            var active = GetActiveBuildProfileObject();
            if (active == null)
                return null;

            var path = UnityEditor.AssetDatabase.GetAssetPath(active);
            return string.IsNullOrWhiteSpace(path) ? null : path;
        }

        public static bool TrySetActiveBuildProfile(UnityEngine.Object profileAsset, out string error)
        {
            error = null;
            var type = GetBuildProfileType();
            if (type == null)
            {
                error = "Unity BuildProfile API is unavailable in this Editor version.";
                return false;
            }

            if (profileAsset == null || !type.IsInstanceOfType(profileAsset))
            {
                error = "The provided asset is not a BuildProfile.";
                return false;
            }

            try
            {
                var method = type.GetMethod("SetActiveBuildProfile", BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                {
                    error = "BuildProfile.SetActiveBuildProfile is unavailable in this Editor version.";
                    return false;
                }

                var result = method.Invoke(null, new object[] { profileAsset });
                if (method.ReturnType == typeof(bool) && result is bool success && !success)
                {
                    error = "BuildProfile.SetActiveBuildProfile returned false.";
                    return false;
                }

                return true;
            }
            catch (TargetInvocationException tie)
            {
                error = tie.InnerException?.Message ?? tie.Message;
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static UnityEngine.Object LoadBuildProfileAsset(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            var type = GetBuildProfileType();
            if (type == null)
                return null;

            return UnityEditor.AssetDatabase.LoadAssetAtPath(path, type);
        }

        public static bool TrySwitchActiveBuildTarget(UnityEditor.BuildTarget target, out string error)
        {
            error = null;
            try
            {
                var group = UnityEditor.BuildPipeline.GetBuildTargetGroup(target);
                var success = UnityEditor.EditorUserBuildSettings.SwitchActiveBuildTarget(group, target);
                if (!success)
                {
                    error = $"SwitchActiveBuildTarget returned false for {ToCanonicalName(target)}.";
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static UnityEngine.Object GetActiveBuildProfileObject()
        {
            var type = GetBuildProfileType();
            if (type == null)
                return null;

            try
            {
                var method = type.GetMethod("GetActiveBuildProfile", BindingFlags.Public | BindingFlags.Static);
                if (method == null)
                    return null;

                return method.Invoke(null, null) as UnityEngine.Object;
            }
            catch
            {
                return null;
            }
        }

        private static Type GetBuildProfileType()
        {
            return Type.GetType(BuildProfileTypeName, throwOnError: false);
        }

        private static JObject CreateCustomSummary(UnityEngine.Object asset, bool active)
        {
            var path = UnityEditor.AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrWhiteSpace(path))
                return null;

            var serializedObject = new UnityEditor.SerializedObject(asset);
            var buildTargetProperty = serializedObject.FindProperty("m_BuildTarget");
            var target = buildTargetProperty == null
                ? UnityEditor.EditorUserBuildSettings.activeBuildTarget
                : (UnityEditor.BuildTarget)buildTargetProperty.intValue;

            return new JObject
            {
                ["id"] = path,
                ["kind"] = "build-profile",
                ["name"] = asset.name,
                ["target"] = ToCanonicalName(target),
                ["path"] = path,
                ["active"] = active
            };
        }
    }
}
#endif
