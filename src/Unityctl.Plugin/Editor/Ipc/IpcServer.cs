#if UNITY_EDITOR
using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Ipc
{
    /// <summary>
    /// Named Pipe IPC server for Unity Editor.
    /// Listens on a background thread; dispatches commands on the main thread via EditorApplication.update.
    /// Singleton — use IpcServer.Instance.
    /// </summary>
    public sealed class IpcServer
    {
        private const int MaxServerInstances = 4;
        private const int PipeBusyRetryDelayMs = 250;
        private const int ErrorPipeBusy = 231;

        private static readonly Lazy<IpcServer> _lazy = new Lazy<IpcServer>(() => new IpcServer());
        public static IpcServer Instance => _lazy.Value;

        private Thread _listenThread;
        private volatile bool _stopping;
        private string _pipeName;
        private string _projectPath;
        private NamedPipeServerStream _currentPipe;
        private readonly object _lock = new object();
        private TaskCompletionSource<bool> _shutdownCompletion = CreateShutdownCompletion();

        private readonly ConcurrentQueue<PendingWork> _mainThreadQueue = new ConcurrentQueue<PendingWork>();

        /// <summary>Whether the IPC server is currently running.</summary>
        public bool IsRunning { get; private set; }

        private IpcServer() { }

        /// <summary>
        /// Start the IPC server. Idempotent — safe to call multiple times.
        /// Does nothing in batchmode.
        /// </summary>
        public void Start(string projectPath)
        {
            if (Application.isBatchMode) return;

            lock (_lock)
            {
                var pipeName = PipeNameHelper.GetPipeName(projectPath);

                // Already running with same pipe name
                if (IsRunning && _pipeName == pipeName) return;

                // Different project or not running — stop existing, start new
                if (IsRunning) StopInternal();

                _projectPath = projectPath;
                _pipeName = pipeName;
                _stopping = false;
                _shutdownCompletion = CreateShutdownCompletion();

                _listenThread = new Thread(ListenLoop)
                {
                    Name = "unityctl-ipc",
                    IsBackground = true
                };
                _listenThread.Start();

                EditorApplication.update -= PumpMainThreadQueue;
                EditorApplication.update += PumpMainThreadQueue;

                AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
                AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;

                EditorApplication.quitting -= OnQuitting;
                EditorApplication.quitting += OnQuitting;

                IsRunning = true;
                Debug.Log($"[unityctl] IPC server started on pipe: {_pipeName}");
            }
        }

        /// <summary>Stop the IPC server gracefully.</summary>
        public void Stop()
        {
            lock (_lock)
            {
                StopInternal();
            }
        }

        private void StopInternal()
        {
            if (!IsRunning) return;

            _stopping = true;
            _shutdownCompletion.TrySetResult(true);

            // Dispose current pipe to unblock WaitForConnection
            try { _currentPipe?.Dispose(); } catch { }

            if (_listenThread != null && _listenThread.IsAlive)
                _listenThread.Join(3000);

            EditorApplication.update -= PumpMainThreadQueue;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            EditorApplication.quitting -= OnQuitting;

            // Cancel and drain remaining queued requests so listener threads do not block.
            while (_mainThreadQueue.TryDequeue(out var pending))
            {
                pending.WorkItem.Cancel();
            }

            IsRunning = false;
            Debug.Log("[unityctl] IPC server stopped");
        }

        private void OnBeforeAssemblyReload()
        {
            Stop();
        }

        private void OnQuitting()
        {
            Stop();
        }

        /// <summary>
        /// Called after domain reload. Re-registers lifecycle hooks and restarts if needed.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void OnAfterAssemblyReload()
        {
            // If the server was running before reload, the static singleton is re-created.
            // Bootstrap will call Start() again via UnityctlBootstrap.
        }

        /// <summary>
        /// Background thread: accepts one connection at a time, reads request, queues for main thread.
        /// </summary>
        private void ListenLoop()
        {
            while (!_stopping)
            {
                NamedPipeServerStream pipe = null;
                try
                {
                    pipe = new NamedPipeServerStream(
                        _pipeName,
                        PipeDirection.InOut,
                        MaxServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.None);

                    _currentPipe = pipe;
                    pipe.WaitForConnection();

                    if (_stopping) break;

                    // Read request
                    var requestJson = MessageFraming.ReadMessage(pipe);
                    var request = JsonConvert.DeserializeObject<CommandRequest>(requestJson);

                    if (request == null)
                    {
                        var errorResponse = CommandResponse.Fail(StatusCode.InvalidParameters, "Failed to deserialize request");
                        var errorJson = JsonConvert.SerializeObject(errorResponse);
                        MessageFraming.WriteMessage(pipe, errorJson);
                        continue;
                    }

                    // Dispatch on main thread
                    var workItem = new WorkItem();
                    _mainThreadQueue.Enqueue(new PendingWork(request, pipe, workItem));

                    // Wait for main thread to process, shutdown, or safety timeout.
                    var completedTask = Task.WhenAny(
                        workItem.Completion,
                        _shutdownCompletion.Task,
                        Task.Delay(TimeSpan.FromMinutes(10)))
                        .GetAwaiter()
                        .GetResult();

                    if (completedTask != workItem.Completion)
                    {
                        if (completedTask == _shutdownCompletion.Task)
                            workItem.Cancel();
                        else
                            workItem.Cancel("IPC request timed out waiting for Unity main thread.");
                    }

                    var response = workItem.Completion.GetAwaiter().GetResult();
                    if (response != null)
                    {
                        var responseJson = JsonConvert.SerializeObject(response);
                        try
                        {
                            if (pipe.IsConnected)
                                MessageFraming.WriteMessage(pipe, responseJson);
                        }
                        catch (IOException)
                        {
                            // Client disconnected before response — acceptable
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Normal shutdown path — Stop() disposed the pipe
                    break;
                }
                catch (IOException ex)
                {
                    if (!_stopping && IsPipeBusy(ex))
                    {
                        Thread.Sleep(PipeBusyRetryDelayMs);
                        continue;
                    }

                    if (!_stopping)
                        Debug.LogWarning($"[unityctl] IPC connection error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    if (!_stopping)
                        Debug.LogError($"[unityctl] IPC server error: {ex}");
                }
                finally
                {
                    try { pipe?.Dispose(); } catch { }
                    _currentPipe = null;
                }
            }
        }

        /// <summary>
        /// Pumped every editor frame via EditorApplication.update.
        /// Dequeues pending work and executes command handlers on the main thread.
        /// </summary>
        private void PumpMainThreadQueue()
        {
            while (_mainThreadQueue.TryDequeue(out var pending))
            {
                if (_stopping)
                {
                    pending.WorkItem.Cancel();
                    continue;
                }

                try
                {
                    var response = IpcRequestRouter.Route(pending.Request);
                    pending.WorkItem.TryComplete(response);
                }
                catch (Exception ex)
                {
                    pending.WorkItem.TryComplete(CommandResponse.Fail(
                        StatusCode.UnknownError,
                        $"Handler exception: {ex.Message}",
                        new System.Collections.Generic.List<string> { ex.StackTrace }));
                }
            }
        }

        /// <summary>Work item for cross-thread signaling.</summary>
        private sealed class WorkItem
        {
            private readonly TaskCompletionSource<CommandResponse> _completion =
                new TaskCompletionSource<CommandResponse>();

            public Task<CommandResponse> Completion => _completion.Task;

            public bool TryComplete(CommandResponse response)
            {
                return _completion.TrySetResult(response);
            }

            public void Cancel(string message = "IPC server is stopping.")
            {
                _completion.TrySetResult(CommandResponse.Fail(StatusCode.Busy, message));
            }
        }

        /// <summary>Pending work queued for main thread execution.</summary>
        private sealed class PendingWork
        {
            public readonly CommandRequest Request;
            public readonly NamedPipeServerStream Pipe;
            public readonly WorkItem WorkItem;

            public PendingWork(CommandRequest request, NamedPipeServerStream pipe, WorkItem workItem)
            {
                Request = request;
                Pipe = pipe;
                WorkItem = workItem;
            }
        }

        private static TaskCompletionSource<bool> CreateShutdownCompletion()
        {
            return new TaskCompletionSource<bool>();
        }

        private static bool IsPipeBusy(IOException exception)
        {
            return (exception.HResult & 0xFFFF) == ErrorPipeBusy;
        }
    }
}
#endif
