#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Unityctl.Plugin.Editor.Ipc;
using Unityctl.Plugin.Editor.Utilities;
using Debug = UnityEngine.Debug;

namespace Unityctl.Plugin.Editor.Windows
{
    /// <summary>
    /// The Unity-side entry point: shows bridge/IPC health and wires an MCP client
    /// to this project without hand-editing JSON.
    /// </summary>
    public sealed class UnityctlStatusWindow : EditorWindow
    {
        private static readonly string[] Clients = { "claude-code", "cursor", "vscode", "codex" };

        private string _projectPath;
        private string _lastOutput;
        private bool _lastOutputIsError;

        [MenuItem("Window/unityctl/Status")]
        public static void Open()
        {
            var window = GetWindow<UnityctlStatusWindow>(false, "unityctl", true);
            window.minSize = new Vector2(420f, 320f);
            window.Show();
        }

        private void OnEnable()
        {
            _projectPath = Path.GetDirectoryName(Application.dataPath);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Bridge", EditorStyles.boldLabel);

            var settings = UnityctlProjectSettingsStore.Load(_projectPath);
            var enabled = settings != null && settings.Enabled;
            var running = IpcServer.Instance.IsRunning;

            Row("Plugin enabled", enabled ? "yes" : "no — run `unityctl init --project <path>`");
            Row("IPC server", running ? "running" : "stopped");
            Row("Pipe", PipeNameHelper.GetPipeName(_projectPath));
            Row("Unity", Application.unityVersion);
            Row("Project", _projectPath);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Connect an AI client", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Writes the unityctl MCP server entry into the client's config, merging with "
                + "whatever is already there. Requires the unityctl CLI on your PATH.",
                MessageType.None);

            foreach (var client in Clients)
            {
                if (GUILayout.Button("Configure " + client))
                    RunInstall(client);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Preview without writing (claude-code)"))
                RunInstall("claude-code", dryRun: true);

            if (!string.IsNullOrEmpty(_lastOutput))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Result", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(_lastOutput, _lastOutputIsError ? MessageType.Error : MessageType.Info);
            }
        }

        private static void Row(string label, string value)
        {
            EditorGUILayout.LabelField(label, string.IsNullOrEmpty(value) ? "-" : value);
        }

        private void RunInstall(string client, bool dryRun = false)
        {
            var arguments = "mcp install --client " + client + " --project \"" + _projectPath + "\"";
            if (dryRun)
                arguments += " --dry-run";

            if (TryRunCli(arguments, out var output, out var error))
            {
                _lastOutput = string.IsNullOrWhiteSpace(output) ? "Done." : output.Trim();
                _lastOutputIsError = false;
            }
            else
            {
                // Never leave the user stuck: hand them the exact command to paste.
                _lastOutput = error + "\n\nRun this yourself:\n  unityctl " + arguments;
                _lastOutputIsError = true;
            }

            Repaint();
        }

        private static bool TryRunCli(string arguments, out string output, out string error)
        {
            output = string.Empty;
            error = string.Empty;

            // A GUI-launched Editor does not inherit the user's shell PATH, so go
            // through a login shell on Unix and cmd on Windows.
            ProcessStartInfo startInfo;
#if UNITY_EDITOR_WIN
            startInfo = new ProcessStartInfo("cmd.exe", "/c unityctl " + arguments);
#else
            var shell = Environment.GetEnvironmentVariable("SHELL");
            if (string.IsNullOrEmpty(shell))
                shell = "/bin/sh";
            startInfo = new ProcessStartInfo(shell, "-lc \"unityctl " + arguments.Replace("\"", "\\\"") + "\"");
#endif
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;

            try
            {
                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        error = "Could not start the unityctl CLI.";
                        return false;
                    }

                    output = process.StandardOutput.ReadToEnd();
                    var stderr = process.StandardError.ReadToEnd();
                    process.WaitForExit(30000);

                    if (process.HasExited && process.ExitCode == 0)
                        return true;

                    error = string.IsNullOrWhiteSpace(stderr)
                        ? "unityctl exited with a non-zero status."
                        : stderr.Trim();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[unityctl] MCP install failed: " + ex.Message);
                error = "unityctl CLI not found on PATH (" + ex.Message + ")";
                return false;
            }
        }
    }
}
#endif
