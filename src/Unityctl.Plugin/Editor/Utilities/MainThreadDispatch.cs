#if UNITY_EDITOR
using System;
using UnityEditor;

namespace Unityctl.Plugin.Editor.Utilities
{
    /// <summary>
    /// Runs deferred actions on the editor main thread via EditorApplication.update.
    /// Use instead of EditorApplication.delayCall: delayCall is flushed with GUI
    /// repaints, which never happen while the editor is unfocused or the screen is
    /// locked, whereas update keeps ticking on unattended editors.
    /// </summary>
    public static class MainThreadDispatch
    {
        /// <summary>
        /// Run <paramref name="action"/> once after <paramref name="delayTicks"/> editor
        /// update ticks (minimum 1). Exceptions are logged, not rethrown.
        /// </summary>
        public static void RunDeferred(Action action, int delayTicks = 1)
        {
            if (action == null) return;

            var remaining = delayTicks < 1 ? 1 : delayTicks;
            EditorApplication.CallbackFunction callback = null;
            callback = () =>
            {
                if (--remaining > 0) return;
                EditorApplication.update -= callback;
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[unityctl] Deferred main-thread action failed: {ex}");
                }
            };
            EditorApplication.update += callback;
        }
    }
}
#endif
