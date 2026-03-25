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
    /// Initializes unityctl only for projects that explicitly enabled the bridge via CLI install.
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
        private static bool _startScheduled;
        private static bool _bridgeStarted;
        private static string _projectPath;

        static UnityctlBootstrap()
        {
            if (Application.isBatchMode)
                return;

            _projectPath = Path.GetDirectoryName(Application.dataPath);
            var settings = UnityctlProjectSettingsStore.Load(_projectPath);
            if (settings == null || !settings.Enabled)
                return;

            EditorApplication.delayCall += ScheduleStart;
        }

        private static void ScheduleStart()
        {
            if (_bridgeStarted || _startScheduled)
                return;

            _startScheduled = true;
            EditorApplication.update += WaitForEditorReadyAndStart;
        }

        private static void WaitForEditorReadyAndStart()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            EditorApplication.update -= WaitForEditorReadyAndStart;
            _startScheduled = false;
            StartBridge();
        }

        private static void StartBridge()
        {
            if (_bridgeStarted)
                return;

            CommandRegistry.Initialize();
            ScriptCompilationCollector.EnsureSubscribed();

            IpcServer.Instance.Start(_projectPath);
            PlayModeTestResultRecovery.RestorePendingPlayModeRuns();

            EditorApplication.update -= PruneUpdate;
            EditorApplication.update += PruneUpdate;
            _lastPruneTime = EditorApplication.timeSinceStartup;
            _bridgeStarted = true;

            Debug.Log($"[unityctl] Bridge initialized — Unity {Application.unityVersion}");
        }

        private static void PruneUpdate()
        {
            if (!_bridgeStarted)
                return;

            if (EditorApplication.timeSinceStartup - _lastPruneTime < PruneIntervalSeconds)
                return;

            _lastPruneTime = EditorApplication.timeSinceStartup;
            AsyncOperationRegistry.Prune(PruneRunningTtl, PruneCompletedTtl);
            BuildTransitionStateStore.Prune(BuildTransitionRunningTtl, BuildTransitionCompletedTtl);
            TestRunStateStore.PruneDefault();
        }
    }
}
#endif
