#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Unityctl.Plugin.Editor.Utilities
{
    internal static class AssetReferenceGraphUtility
    {
        internal sealed class ReferenceMatch
        {
            public string Path { get; set; }

            public string Relation { get; set; }
        }

        public static string[] GetExistingScanRoots()
        {
            var roots = new List<string>();
            if (UnityEditor.AssetDatabase.IsValidFolder("Assets"))
            {
                roots.Add("Assets");
            }

            if (UnityEditor.AssetDatabase.IsValidFolder("Packages"))
            {
                roots.Add("Packages");
            }

            return roots.ToArray();
        }

        public static string[] EnumerateCandidatePaths(string[] scanRoots)
        {
            if (scanRoots.Length == 0)
            {
                return Array.Empty<string>();
            }

            var guids = UnityEditor.AssetDatabase.FindAssets("t:Object", scanRoots);
            return guids
                .Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static ReferenceMatch? FindReferenceMatch(string candidatePath, string targetPath)
        {
            var directDependencies = UnityEditor.AssetDatabase.GetDependencies(candidatePath, recursive: false);
            if (ContainsPath(directDependencies, targetPath))
            {
                return new ReferenceMatch
                {
                    Path = candidatePath,
                    Relation = "direct"
                };
            }

            var recursiveDependencies = UnityEditor.AssetDatabase.GetDependencies(candidatePath, recursive: true);
            if (ContainsPath(recursiveDependencies, targetPath))
            {
                return new ReferenceMatch
                {
                    Path = candidatePath,
                    Relation = "transitive"
                };
            }

            return null;
        }

        public static JObject BuildAssetMetadata(string path)
        {
            var guid = UnityEditor.AssetDatabase.AssetPathToGUID(path);
            var mainAssetType = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(path);
            var labels = new JArray();

            var asset = UnityEditor.AssetDatabase.LoadMainAssetAtPath(path);
            if (asset != null)
            {
                foreach (var label in UnityEditor.AssetDatabase.GetLabels(asset))
                {
                    labels.Add(label);
                }
            }

            return new JObject
            {
                ["path"] = path,
                ["guid"] = guid,
                ["mainAssetType"] = mainAssetType != null ? mainAssetType.FullName ?? mainAssetType.Name : string.Empty,
                ["labels"] = labels
            };
        }

        private static bool ContainsPath(IEnumerable<string> dependencies, string targetPath)
        {
            foreach (var dependency in dependencies)
            {
                if (string.Equals(dependency, targetPath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif
