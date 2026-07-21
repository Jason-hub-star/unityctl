#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;

namespace Unityctl.Plugin.Editor.Utilities
{
    /// <summary>
    /// Resolves user-supplied property names to SerializedProperty paths.
    /// Agents guess friendly names ("mass") while Unity serializes "m_Mass" —
    /// try exact, then m_PascalCase, then a case-insensitive top-level scan,
    /// so both spellings work and failures can list what exists.
    /// </summary>
    public static class SerializedPropertyResolver
    {
        /// <summary>
        /// Find a property by exact path, m_PascalCase mapping, or
        /// case-insensitive top-level name match. Sets <paramref name="resolvedPath"/>
        /// to the actual serialized path when found; null otherwise.
        /// </summary>
        public static SerializedProperty FindFlexible(SerializedObject serializedObject, string requested, out string resolvedPath)
        {
            resolvedPath = null;
            if (serializedObject == null || string.IsNullOrEmpty(requested))
                return null;

            var exact = serializedObject.FindProperty(requested);
            if (exact != null)
            {
                resolvedPath = requested;
                return exact;
            }

            // Dotted paths are already explicit; only map simple names.
            if (!requested.Contains("."))
            {
                var prefixed = "m_" + char.ToUpperInvariant(requested[0]) + requested.Substring(1);
                var byPrefix = serializedObject.FindProperty(prefixed);
                if (byPrefix != null)
                {
                    resolvedPath = prefixed;
                    return byPrefix;
                }

                foreach (var path in TopLevelPaths(serializedObject, int.MaxValue))
                {
                    var stripped = path.StartsWith("m_", StringComparison.Ordinal) ? path.Substring(2) : path;
                    if (string.Equals(path, requested, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(stripped, requested, StringComparison.OrdinalIgnoreCase))
                    {
                        resolvedPath = path;
                        return serializedObject.FindProperty(path);
                    }
                }
            }

            return null;
        }

        /// <summary>Top-level serialized property paths, capped at <paramref name="max"/>.</summary>
        public static List<string> TopLevelPaths(SerializedObject serializedObject, int max)
        {
            var paths = new List<string>();
            var iterator = serializedObject.GetIterator();
            if (iterator.NextVisible(enterChildren: true))
            {
                do
                {
                    paths.Add(iterator.propertyPath);
                }
                while (paths.Count < max && iterator.NextVisible(enterChildren: false));
            }
            return paths;
        }
    }
}
#endif
