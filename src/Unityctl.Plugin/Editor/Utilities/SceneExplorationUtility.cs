#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unityctl.Plugin.Editor.Utilities
{
    internal static class SceneExplorationUtility
    {
        internal readonly struct ExplorationContext
        {
            public ExplorationContext(List<Object> allObjects, List<(JObject entry, string field)> pendingIds)
            {
                AllObjects = allObjects;
                PendingIds = pendingIds;
            }

            public List<Object> AllObjects { get; }

            public List<(JObject entry, string field)> PendingIds { get; }
        }

        public static JArray BuildSceneSetup(Scene activeScene)
        {
            var sceneSetup = new JArray();
            var sceneCount = SceneManager.sceneCount;
            for (var i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                sceneSetup.Add(new JObject
                {
                    ["path"] = scene.path,
                    ["isLoaded"] = scene.isLoaded,
                    ["isActive"] = scene.path == activeScene.path
                });
            }

            return sceneSetup;
        }

        public static void PopulateGlobalObjectIds(ExplorationContext context)
        {
            if (context.AllObjects.Count == 0)
            {
                return;
            }

            var ids = new GlobalObjectId[context.AllObjects.Count];
            GlobalObjectId.GetGlobalObjectIdsSlow(context.AllObjects.ToArray(), ids);

            for (var i = 0; i < context.PendingIds.Count; i++)
            {
                var (entry, field) = context.PendingIds[i];
                entry[field] = ids[i].ToString();
            }
        }

        public static string GetHierarchyPath(GameObject gameObject, string parentPath)
        {
            return string.IsNullOrEmpty(parentPath)
                ? gameObject.name
                : parentPath + "/" + gameObject.name;
        }

        public static string GetHierarchyPath(GameObject gameObject)
        {
            var segments = new List<string>();
            var current = gameObject.transform;
            while (current != null)
            {
                segments.Add(current.name);
                current = current.parent;
            }

            var builder = new StringBuilder();
            for (var i = segments.Count - 1; i >= 0; i--)
            {
                if (builder.Length > 0)
                {
                    builder.Append('/');
                }

                builder.Append(segments[i]);
            }

            return builder.ToString();
        }

        public static JArray GetComponentTypeNames(GameObject gameObject)
        {
            var componentTypes = new JArray();
            foreach (var component in gameObject.GetComponents<Component>())
            {
                if (component == null)
                {
                    continue;
                }

                componentTypes.Add(component.GetType().FullName ?? component.GetType().Name);
            }

            return componentTypes;
        }

        public static JObject CreateGameObjectSummary(GameObject gameObject, string hierarchyPath)
        {
            return new JObject
            {
                ["globalObjectId"] = GlobalObjectIdResolver.GetId(gameObject),
                ["name"] = gameObject.name,
                ["scenePath"] = hierarchyPath,
                ["sceneAssetPath"] = gameObject.scene.path,
                ["activeSelf"] = gameObject.activeSelf,
                ["layer"] = gameObject.layer,
                ["tag"] = gameObject.tag,
                ["componentTypes"] = GetComponentTypeNames(gameObject)
            };
        }

        public static JObject? CreateHierarchyNode(GameObject gameObject, string parentPath, bool includeInactive)
        {
            if (!includeInactive && !gameObject.activeSelf)
            {
                return null;
            }

            var hierarchyPath = GetHierarchyPath(gameObject, parentPath);
            var children = new JArray();
            for (var i = 0; i < gameObject.transform.childCount; i++)
            {
                var childNode = CreateHierarchyNode(gameObject.transform.GetChild(i).gameObject, hierarchyPath, includeInactive);
                if (childNode != null)
                {
                    children.Add(childNode);
                }
            }

            return new JObject
            {
                ["globalObjectId"] = GlobalObjectIdResolver.GetId(gameObject),
                ["name"] = gameObject.name,
                ["activeSelf"] = gameObject.activeSelf,
                ["layer"] = gameObject.layer,
                ["tag"] = gameObject.tag,
                ["scenePath"] = hierarchyPath,
                ["componentTypes"] = GetComponentTypeNames(gameObject),
                ["children"] = children
            };
        }

        public static JArray BuildHierarchyRoots(Scene scene, bool includeInactive)
        {
            var roots = new JArray();
            foreach (var root in scene.GetRootGameObjects())
            {
                var node = CreateHierarchyNode(root, string.Empty, includeInactive);
                if (node != null)
                {
                    roots.Add(node);
                }
            }

            return roots;
        }

        public static JObject CreateComponentSummary(Component component)
        {
            return new JObject
            {
                ["globalObjectId"] = GlobalObjectIdResolver.GetId(component),
                ["typeName"] = component.GetType().FullName ?? component.GetType().Name,
                ["enabled"] = component is Behaviour behaviour ? behaviour.enabled : true
            };
        }

        public static JObject CreateGameObjectEntry(
            GameObject gameObject,
            string hierarchyPath,
            bool includeProperties,
            ExplorationContext context)
        {
            var components = new JArray();
            foreach (var component in gameObject.GetComponents<Component>())
            {
                if (component == null)
                {
                    continue;
                }

                var componentEntry = CreateComponentEntry(component, includeProperties, context);
                components.Add(componentEntry);
            }

            var gameObjectEntry = new JObject
            {
                ["globalObjectId"] = string.Empty,
                ["name"] = gameObject.name,
                ["activeSelf"] = gameObject.activeSelf,
                ["layer"] = gameObject.layer,
                ["tag"] = gameObject.tag,
                ["scenePath"] = hierarchyPath,
                ["components"] = components
            };

            context.PendingIds.Add((gameObjectEntry, "globalObjectId"));
            context.AllObjects.Add(gameObject);
            return gameObjectEntry;
        }

        public static JObject CreateComponentEntry(
            Component component,
            bool includeProperties,
            ExplorationContext context)
        {
            var componentEntry = new JObject
            {
                ["globalObjectId"] = string.Empty,
                ["typeName"] = component.GetType().FullName ?? component.GetType().Name,
                ["enabled"] = component is Behaviour behaviour ? behaviour.enabled : true
            };

            if (includeProperties)
            {
                componentEntry["properties"] = SerializedPropertyJsonUtility.GetVisibleProperties(component);
            }

            context.PendingIds.Add((componentEntry, "globalObjectId"));
            context.AllObjects.Add(component);
            return componentEntry;
        }

        public static void TraverseGameObject(
            GameObject gameObject,
            string parentPath,
            JArray output,
            bool includeInactive,
            bool includeProperties,
            ExplorationContext context)
        {
            if (!includeInactive && !gameObject.activeSelf)
            {
                return;
            }

            var hierarchyPath = GetHierarchyPath(gameObject, parentPath);

            output.Add(CreateGameObjectEntry(gameObject, hierarchyPath, includeProperties, context));

            for (var i = 0; i < gameObject.transform.childCount; i++)
            {
                TraverseGameObject(
                    gameObject.transform.GetChild(i).gameObject,
                    hierarchyPath,
                    output,
                    includeInactive,
                    includeProperties,
                    context);
            }
        }
    }
}
#endif
