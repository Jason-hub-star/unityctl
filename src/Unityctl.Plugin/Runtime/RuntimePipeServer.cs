using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Unityctl.Plugin.Runtime
{
    /// <summary>
    /// Minimal Named Pipe server for development Player builds. Same wire
    /// contract as the editor bridge — [4-byte LE length][UTF-8 JSON], response
    /// shaped like CommandResponse — so the CLI client code works unchanged.
    /// Commands execute on the main thread via RuntimeBridgePump.Update().
    /// </summary>
    public sealed class RuntimePipeServer
    {
        private const int MaxMessageBytes = 10 * 1024 * 1024;
        private const int MaxServerInstances = 4;
        private const int MainThreadTimeoutMs = 10000;

        /// <summary>Executes a command on the main thread; null result = unknown command.</summary>
        public delegate JObject CommandHandler(string command, JObject parameters);

        private readonly CommandHandler _handler;
        private readonly ConcurrentQueue<PendingWork> _mainThreadQueue = new ConcurrentQueue<PendingWork>();
        private Thread _listenThread;
        private NamedPipeServerStream _listenPipe;
        private volatile bool _stopping;

        public string PipeName { get; }

        public RuntimePipeServer(CommandHandler handler)
        {
            _handler = handler;
            PipeName = $"unityctl_rt_{System.Diagnostics.Process.GetCurrentProcess().Id}";
        }

        public void Start()
        {
            _stopping = false;
            _listenThread = new Thread(ListenLoop)
            {
                Name = "unityctl-runtime-ipc",
                IsBackground = true
            };
            _listenThread.Start();
        }

        public void Stop()
        {
            _stopping = true;
            try { _listenPipe?.Dispose(); } catch { }
            while (_mainThreadQueue.TryDequeue(out var pending))
                pending.Completion.TrySetResult(null);
        }

        /// <summary>Called from the main thread every frame.</summary>
        public void PumpMainThread()
        {
            while (_mainThreadQueue.TryDequeue(out var pending))
            {
                JObject data = null;
                string error = null;
                try
                {
                    data = _handler(pending.Command, pending.Parameters);
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }

                pending.Completion.TrySetResult(BuildResponse(pending, data, error));
            }
        }

        private static JObject BuildResponse(PendingWork pending, JObject data, string error)
        {
            if (error != null)
            {
                return new JObject
                {
                    ["statusCode"] = 1,
                    ["success"] = false,
                    ["message"] = $"Handler exception: {error}",
                    ["requestId"] = pending.RequestId
                };
            }

            if (data == null)
            {
                return new JObject
                {
                    ["statusCode"] = 102,
                    ["success"] = false,
                    ["message"] = $"Unknown runtime command: {pending.Command}. Available: runtime-status, runtime-logs",
                    ["requestId"] = pending.RequestId
                };
            }

            return new JObject
            {
                ["statusCode"] = 0,
                ["success"] = true,
                ["message"] = pending.Command,
                ["data"] = data,
                ["requestId"] = pending.RequestId
            };
        }

        private void ListenLoop()
        {
            while (!_stopping)
            {
                NamedPipeServerStream pipe = null;
                try
                {
                    pipe = new NamedPipeServerStream(
                        PipeName, PipeDirection.InOut, MaxServerInstances,
                        PipeTransmissionMode.Byte, PipeOptions.None);
                    _listenPipe = pipe;
                    pipe.WaitForConnection();
                    _listenPipe = null;

                    if (_stopping) break;

                    HandleConnection(pipe);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    if (!_stopping)
                        Debug.LogWarning($"[unityctl] Runtime IPC error: {ex.Message}");
                }
                finally
                {
                    try { pipe?.Dispose(); } catch { }
                    _listenPipe = null;
                }
            }
        }

        private void HandleConnection(NamedPipeServerStream pipe)
        {
            var requestJson = ReadMessage(pipe);
            var request = JObject.Parse(requestJson);
            var pending = new PendingWork
            {
                Command = request["command"]?.Value<string>() ?? string.Empty,
                Parameters = request["parameters"] as JObject,
                RequestId = request["requestId"]?.Value<string>()
            };
            _mainThreadQueue.Enqueue(pending);

            var completed = pending.Completion.Task.Wait(MainThreadTimeoutMs);
            var response = completed && pending.Completion.Task.Result != null
                ? pending.Completion.Task.Result
                : new JObject
                {
                    ["statusCode"] = 1,
                    ["success"] = false,
                    ["message"] = "Runtime bridge timed out waiting for the player main thread.",
                    ["requestId"] = pending.RequestId
                };

            WriteMessage(pipe, response.ToString(Newtonsoft.Json.Formatting.None));
        }

        private static string ReadMessage(Stream stream)
        {
            var lengthBytes = ReadExact(stream, 4);
            var length = BitConverter.ToInt32(lengthBytes, 0);
            if (length <= 0 || length > MaxMessageBytes)
                throw new IOException($"Invalid message length: {length}");
            return Encoding.UTF8.GetString(ReadExact(stream, length));
        }

        private static byte[] ReadExact(Stream stream, int count)
        {
            var buffer = new byte[count];
            var offset = 0;
            while (offset < count)
            {
                var read = stream.Read(buffer, offset, count - offset);
                if (read <= 0)
                    throw new IOException("Pipe closed while reading.");
                offset += read;
            }
            return buffer;
        }

        private static void WriteMessage(Stream stream, string json)
        {
            var payload = Encoding.UTF8.GetBytes(json);
            stream.Write(BitConverter.GetBytes(payload.Length), 0, 4);
            stream.Write(payload, 0, payload.Length);
            stream.Flush();
        }

        private sealed class PendingWork
        {
            public string Command;
            public JObject Parameters;
            public string RequestId;
            public readonly System.Threading.Tasks.TaskCompletionSource<JObject> Completion =
                new System.Threading.Tasks.TaskCompletionSource<JObject>();
        }
    }
}
