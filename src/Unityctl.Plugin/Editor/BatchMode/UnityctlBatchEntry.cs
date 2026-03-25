using System;
using System.IO;
using Unityctl.Plugin.Editor.Commands;
using Unityctl.Plugin.Editor.Shared;
using Newtonsoft.Json;

namespace Unityctl.Plugin.Editor.BatchMode
{
    /// <summary>
    /// Unity batchmode entry point.
    /// Called via: -executeMethod Unityctl.Plugin.Editor.BatchMode.UnityctlBatchEntry.Execute
    /// CLI passes: -- --unityctl-command &lt;cmd&gt; --unityctl-request &lt;path&gt; --unityctl-response &lt;path&gt;
    /// </summary>
    public static class UnityctlBatchEntry
    {
        private const int BatchTimeoutSeconds = 300;

        private static string _pendingResponsePath;
        private static string _pendingRequestId;
        private static DateTime _pollStartedAt;

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
                ScriptCompilationCollector.EnsureSubscribed();

                CommandRequest request;
                if (!string.IsNullOrEmpty(requestPath) && File.Exists(requestPath))
                {
                    var json = File.ReadAllText(requestPath);
                    request = JsonConvert.DeserializeObject<CommandRequest>(json);
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

                // If Accepted, wait for async completion via polling
                if (response.statusCode == (int)StatusCode.Accepted && !string.IsNullOrEmpty(response.requestId))
                {
                    Log($"Accepted — waiting for async completion (requestId={response.requestId})");
                    _pendingResponsePath = responsePath;
                    _pendingRequestId = response.requestId;
                    _pollStartedAt = DateTime.UtcNow;
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.update += PollUpdate;
#endif
                    return; // Do NOT quit yet — PollUpdate will write response and quit
                }
            }
            catch (Exception e)
            {
                response = CommandResponse.Fail(
                    StatusCode.UnknownError,
                    $"BatchEntry fatal error: {e.Message}",
                    new System.Collections.Generic.List<string> { e.StackTrace });
            }

            WriteResponseAndQuit(responsePath, response);
        }

#if UNITY_EDITOR
        private static void PollUpdate()
        {
            var state = AsyncOperationRegistry.TryGet(_pendingRequestId);
            if (state != null && state.Status == AsyncStatus.Completed)
            {
                UnityEditor.EditorApplication.update -= PollUpdate;
                Log($"Async operation completed (requestId={_pendingRequestId})");
                WriteResponseAndQuit(_pendingResponsePath, state.Response);
                return;
            }

            // Timeout check
            if ((DateTime.UtcNow - _pollStartedAt).TotalSeconds > BatchTimeoutSeconds)
            {
                UnityEditor.EditorApplication.update -= PollUpdate;
                Log($"Async operation timed out after {BatchTimeoutSeconds}s (requestId={_pendingRequestId})");
                var timeout = CommandResponse.Fail(
                    StatusCode.TestFailed,
                    $"Test execution timed out after {BatchTimeoutSeconds}s");
                WriteResponseAndQuit(_pendingResponsePath, timeout);
            }
        }
#endif

        private static void WriteResponseAndQuit(string responsePath, CommandResponse response)
        {
            try
            {
                var responseJson = JsonConvert.SerializeObject(response, Formatting.Indented);
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
