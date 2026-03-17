#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Unityctl.Plugin.Editor.Commands;

namespace Unityctl.Plugin.Editor.Bootstrap
{
    /// <summary>
    /// Auto-initializes unityctl when Unity Editor loads.
    /// Registers commands and starts IPC server (Phase 2).
    /// </summary>
    [InitializeOnLoad]
    public static class UnityctlBootstrap
    {
        static UnityctlBootstrap()
        {
            CommandRegistry.Initialize();
            Debug.Log($"[unityctl] Bridge initialized — Unity {Application.unityVersion}");
        }
    }
}
#endif
