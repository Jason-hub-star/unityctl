using System;
using System.IO;
using System.Linq;
using Unityctl.Plugin.Editor.Commands;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.BatchMode
{
    /// <summary>
    /// Unity batchmode entry point.
    /// Called via: -executeMethod Unityctl.Plugin.Editor.BatchMode.UnityctlBatchEntry.Execute
    /// CLI passes: -- --unityctl-command &lt;cmd&gt; --unityctl-request &lt;path&gt; --unityctl-response &lt;path&gt;
    /// </summary>
    public static class UnityctlBatchEntry
    {
        public static void Execute()
        {
            string command = null;
            string requestPath = null;
            string responsePath = null;

            var args = Environment.GetCommandLineArgs();
            bool afterSeparator = false;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--")
                {
                    afterSeparator = true;
                    continue;
                }
                if (!afterSeparator) continue;

                if (args[i] == "--unityctl-command" && i + 1 < args.Length)
                    command = args[++i];
                else if (args[i] == "--unityctl-request" && i + 1 < args.Length)
                    requestPath = args[++i];
                else if (args[i] == "--unityctl-response" && i + 1 < args.Length)
                    responsePath = args[++i];
            }

            if (string.IsNullOrEmpty(responsePath))
            {
                Log("ERROR: --unityctl-response path not provided");
                Quit(1);
                return;
            }

            CommandResponse response;

            try
            {
                CommandRegistry.Initialize();

                CommandRequest request;
                if (!string.IsNullOrEmpty(requestPath) && File.Exists(requestPath))
                {
                    var json = File.ReadAllText(requestPath);
#if UNITY_EDITOR
                    request = UnityEngine.JsonUtility.FromJson<CommandRequest>(json);
#else
                    request = new CommandRequest { command = command ?? "ping" };
#endif
                }
                else
                {
                    request = new CommandRequest
                    {
                        command = command ?? "ping",
                        requestId = Guid.NewGuid().ToString("N")
                    };
                }

                if (!string.IsNullOrEmpty(command))
                    request.command = command;

                Log($"Executing command: {request.command}");
                response = CommandRegistry.Dispatch(request);
            }
            catch (Exception e)
            {
                response = CommandResponse.Fail(
                    StatusCode.UnknownError,
                    $"BatchEntry fatal error: {e.Message}",
                    new System.Collections.Generic.List<string> { e.StackTrace });
            }

            try
            {
#if UNITY_EDITOR
                var responseJson = UnityEngine.JsonUtility.ToJson(response, true);
#else
                var responseJson = System.Text.Json.JsonSerializer.Serialize(response);
#endif
                var dir = Path.GetDirectoryName(responsePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(responsePath, responseJson);
                Log($"Response written to: {responsePath}");
            }
            catch (Exception e)
            {
                Log($"ERROR writing response: {e.Message}");
            }

            Quit(response.success ? 0 : 1);
        }

        private static void Log(string message)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log($"[unityctl] {message}");
#else
            Console.WriteLine($"[unityctl] {message}");
#endif
        }

        private static void Quit(int exitCode)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.Exit(exitCode);
#else
            Environment.Exit(exitCode);
#endif
        }
    }
}
