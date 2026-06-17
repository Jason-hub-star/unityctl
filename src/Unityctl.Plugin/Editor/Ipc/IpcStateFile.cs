#if UNITY_EDITOR
using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Unityctl.Plugin.Editor.Ipc
{
    /// <summary>
    /// Manages IPC state file on disk for client probe detection.
    /// Plugin writes editor state (ready/reloading/stopped) to a JSON file,
    /// allowing clients to detect reload periods without connecting the pipe.
    /// Never throws — all exceptions are swallowed.
    /// </summary>
    public static class IpcStateFile
    {
        private const string IpcStateFileName = "ipc-state.json";
        private const string IpcStateDirectory = "Library/Unityctl";

        /// <summary>
        /// IPC state values as string constants.
        /// </summary>
        public static class IpcStateValues
        {
            public const string Starting = "starting";
            public const string Ready = "ready";
            public const string Reloading = "reloading";
            public const string Stopped = "stopped";
        }

        /// <summary>
        /// Compute the full path to the IPC state file for a given project path.
        /// Uses same normalization as PipeNameHelper to ensure client and plugin agree.
        /// </summary>
        public static string GetFilePath(string projectPath)
        {
            var normalized = PipeNameHelper.NormalizeProjectPath(projectPath);
            // Convert normalized path back to platform-native format for file operations
            var nativePath = normalized.Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(nativePath, IpcStateDirectory, IpcStateFileName);
        }

        /// <summary>
        /// Write the current IPC state to disk.
        /// Creates directory if missing, writes atomically (temp → move), and swallows all exceptions.
        /// </summary>
        public static void Write(string projectPath, string pipeName, string state)
        {
            try
            {
                var filePath = GetFilePath(projectPath);
                var directory = Path.GetDirectoryName(filePath);

                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var stateObject = new JObject
                {
                    ["pipeName"] = pipeName,
                    ["pid"] = System.Diagnostics.Process.GetCurrentProcess().Id,
                    ["unityVersion"] = UnityEngine.Application.unityVersion,
                    ["state"] = state,
                    ["updatedAtUtc"] = DateTime.UtcNow.ToString("o")
                };

                var json = stateObject.ToString(Newtonsoft.Json.Formatting.None);

                // Atomic write: write to temp, then move
                var tempPath = filePath + ".tmp";
                File.WriteAllText(tempPath, json);
                File.Delete(filePath);  // Delete old file first for cross-platform compatibility
                File.Move(tempPath, filePath);
            }
            catch
            {
                // State file write failures must never break IPC
            }
        }

        /// <summary>
        /// Update only the updatedAtUtc timestamp without changing state (heartbeat).
        /// Swallows all exceptions.
        /// </summary>
        public static void Touch(string projectPath)
        {
            try
            {
                var filePath = GetFilePath(projectPath);

                if (!File.Exists(filePath))
                    return;

                var json = File.ReadAllText(filePath);
                var stateObject = JObject.Parse(json);

                stateObject["updatedAtUtc"] = DateTime.UtcNow.ToString("o");

                var updatedJson = stateObject.ToString(Newtonsoft.Json.Formatting.None);

                // Atomic write
                var tempPath = filePath + ".tmp";
                File.WriteAllText(tempPath, updatedJson);
                File.Delete(filePath);
                File.Move(tempPath, filePath);
            }
            catch
            {
                // State file touch failures must never break IPC heartbeat
            }
        }
    }
}
#endif
