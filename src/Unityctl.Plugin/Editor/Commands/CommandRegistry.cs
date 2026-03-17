using System;
using System.Collections.Generic;
using System.Linq;
using Unityctl.Plugin.Editor.Shared;

namespace Unityctl.Plugin.Editor.Commands
{
    public static class CommandRegistry
    {
        private static Dictionary<string, IUnityctlCommand> _commands;

        public static void Initialize()
        {
            _commands = new Dictionary<string, IUnityctlCommand>(StringComparer.OrdinalIgnoreCase);

#if UNITY_EDITOR
            // TypeCache 기반 자동 탐색
            var commandTypes = UnityEditor.TypeCache.GetTypesDerivedFrom<IUnityctlCommand>();
            foreach (var type in commandTypes)
            {
                if (type.IsAbstract || type.IsInterface) continue;
                try
                {
                    var instance = (IUnityctlCommand)Activator.CreateInstance(type);
                    _commands[instance.CommandName] = instance;
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogWarning($"[unityctl] Failed to register command from {type.Name}: {e.Message}");
                }
            }
            UnityEngine.Debug.Log($"[unityctl] Registered {_commands.Count} commands: {string.Join(", ", _commands.Keys)}");
#else
            // Non-Unity fallback: manual registration
            Register(new PingHandler());
            Register(new StatusHandler());
#endif
        }

        public static void Register(IUnityctlCommand command)
        {
            _commands ??= new Dictionary<string, IUnityctlCommand>(StringComparer.OrdinalIgnoreCase);
            _commands[command.CommandName] = command;
        }

        public static CommandResponse Dispatch(CommandRequest request)
        {
            if (_commands == null) Initialize();

            if (string.IsNullOrEmpty(request.command))
                return CommandResponse.Fail(StatusCode.InvalidParameters, "Command name is empty");

            if (!_commands.TryGetValue(request.command, out var handler))
                return CommandResponse.Fail(StatusCode.CommandNotFound, $"Unknown command: {request.command}");

            try
            {
                var response = handler.Execute(request);
                response.requestId = request.requestId;
                return response;
            }
            catch (Exception e)
            {
                return CommandResponse.Fail(StatusCode.UnknownError, $"Command '{request.command}' threw: {e.Message}",
                    new List<string> { e.StackTrace });
            }
        }
    }
}
