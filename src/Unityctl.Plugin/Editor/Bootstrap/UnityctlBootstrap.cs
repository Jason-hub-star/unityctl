#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Unityctl.Plugin.Editor.Commands;
using Unityctl.Plugin.Editor.Ipc;
using Unityctl.Plugin.Editor.Utilities;

namespace Unityctl.Plugin.Editor.Bootstrap
{
    /// <summary>
    /// Auto-initializes unityctl when Unity Editor loads.
    /// Registers commands and starts IPC server.
    /// </summary>
    [InitializeOnLoad]
    public static class UnityctlBootstrap
    {
        private static readonly TimeSpan PruneRunningTtl = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan PruneCompletedTtl = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan BuildTransitionRunningTtl = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan BuildTransitionCompletedTtl = TimeSpan.FromHours(24);
        private static readonly double PruneIntervalSeconds = 60.0;
        private static double _lastPruneTime;

        static UnityctlBootstrap()
        {
            CommandRegistry.Initialize();

            if (!Application.isBatchMode)
            {
                var projectPath = Path.GetDirectoryName(Application.dataPath);
                IpcServer.Instance.Start(projectPath);

                EditorApplication.update += PruneUpdate;
                _lastPruneTime = EditorApplication.timeSinceStartup;
            }

            Debug.Log($"[unityctl] Bridge initialized — Unity {Application.unityVersion}");
        }

        private static void PruneUpdate()
        {
            if (EditorApplication.timeSinceStartup - _lastPruneTime < PruneIntervalSeconds)
                return;

            _lastPruneTime = EditorApplication.timeSinceStartup;
            AsyncOperationRegistry.Prune(PruneRunningTtl, PruneCompletedTtl);
            BuildTransitionStateStore.Prune(BuildTransitionRunningTtl, BuildTransitionCompletedTtl);
        }
    }
}
#endif
