#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public enum AsyncStatus { Running, Completed }

    public sealed class AsyncOperationState
    {
        public string RequestId;
        public string Command;
        public AsyncStatus Status;
        public DateTime StartedAt;
        public DateTime? CompletedAt;
        public string RunGuid;
        public CommandResponse Response;
    }

    public static class AsyncOperationRegistry
    {
        private static readonly Dictionary<string, AsyncOperationState> _operations =
            new Dictionary<string, AsyncOperationState>();
        private static readonly object _lock = new object();

        public static void Register(string requestId, string command, string runGuid = null)
        {
            lock (_lock)
            {
                _operations[requestId] = new AsyncOperationState
                {
                    RequestId = requestId,
                    Command = command,
                    Status = AsyncStatus.Running,
                    StartedAt = DateTime.UtcNow,
                    RunGuid = runGuid
                };
            }
        }

        public static void Complete(string requestId, CommandResponse response)
        {
            lock (_lock)
            {
                if (_operations.TryGetValue(requestId, out var state))
                {
                    state.Status = AsyncStatus.Completed;
                    state.CompletedAt = DateTime.UtcNow;
                    state.Response = response;
                }
            }
        }

        public static AsyncOperationState TryGet(string requestId)
        {
            lock (_lock)
            {
                return _operations.TryGetValue(requestId, out var state) ? state : null;
            }
        }

        public static bool HasRunning(string command, TimeSpan maxAge)
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                foreach (var state in _operations.Values)
                {
                    if (state.Command == command
                        && state.Status == AsyncStatus.Running
                        && (now - state.StartedAt) < maxAge)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public static void Prune(TimeSpan runningTtl, TimeSpan completedTtl)
        {
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                var toRemove = new List<string>();
                foreach (var kvp in _operations)
                {
                    var state = kvp.Value;
                    if (state.Status == AsyncStatus.Running && (now - state.StartedAt) > runningTtl)
                        toRemove.Add(kvp.Key);
                    else if (state.Status == AsyncStatus.Completed && state.CompletedAt.HasValue
                             && (now - state.CompletedAt.Value) > completedTtl)
                        toRemove.Add(kvp.Key);
                }
                foreach (var key in toRemove)
                    _operations.Remove(key);
            }
        }
    }
}
#endif
