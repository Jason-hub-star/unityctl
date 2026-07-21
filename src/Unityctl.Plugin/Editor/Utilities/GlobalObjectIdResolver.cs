#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Unityctl.Plugin.Editor.Utilities
{
    /// <summary>
    /// Resolves GlobalObjectId strings to Unity Objects.
    /// Primary key for all write operations.
    /// </summary>
    public static class GlobalObjectIdResolver
    {
        /// <summary>
        /// Resolve a GlobalObjectId string to a specific Unity Object type.
        /// Returns null if parsing fails or the object is not found/not the expected type.
        /// </summary>
        public static T Resolve<T>(string globalObjectIdString) where T : Object
        {
            if (string.IsNullOrEmpty(globalObjectIdString))
                return null;

            if (!GlobalObjectId.TryParse(globalObjectIdString, out var gid))
                return null;

            var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
            return obj as T;
        }

        /// <summary>
        /// Resolve a GlobalObjectId string to any Unity Object.
        /// </summary>
        public static Object Resolve(string globalObjectIdString)
        {
            return Resolve<Object>(globalObjectIdString);
        }

        /// <summary>
        /// Resolve a GameObject reference of any supported form: GlobalObjectId
        /// string, hierarchy path ("Parent/Child", root-anchored), or plain name.
        /// Name/path lookup covers inactive objects in all loaded scenes.
        /// Returns null when nothing matches.
        /// </summary>
        public static GameObject ResolveGameObject(string reference)
        {
            if (string.IsNullOrEmpty(reference))
                return null;

            var byId = Resolve<GameObject>(reference);
            if (byId != null)
                return byId;

            // Active-object fast path; also handles "Parent/Child" paths.
            var found = GameObject.Find(reference);
            if (found != null)
                return found;

            var segments = reference.Split('/');
            for (var i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    var byPath = MatchPath(root.transform, segments, 0);
                    if (byPath != null)
                        return byPath.gameObject;

                    if (segments.Length == 1)
                    {
                        var byName = FindByNameRecursive(root.transform, reference);
                        if (byName != null)
                            return byName.gameObject;
                    }
                }
            }

            return null;
        }

        private static Transform MatchPath(Transform node, string[] segments, int depth)
        {
            if (node.name != segments[depth])
                return null;
            if (depth == segments.Length - 1)
                return node;

            foreach (Transform child in node)
            {
                var match = MatchPath(child, segments, depth + 1);
                if (match != null)
                    return match;
            }
            return null;
        }

        private static Transform FindByNameRecursive(Transform node, string name)
        {
            if (node.name == name)
                return node;

            foreach (Transform child in node)
            {
                var match = FindByNameRecursive(child, name);
                if (match != null)
                    return match;
            }
            return null;
        }

        /// <summary>
        /// Get the GlobalObjectId string for a Unity Object.
        /// </summary>
        public static string GetId(Object obj)
        {
            if (obj == null) return null;
            var gid = GlobalObjectId.GetGlobalObjectIdSlow(obj);
            return gid.ToString();
        }
    }
}
#endif
