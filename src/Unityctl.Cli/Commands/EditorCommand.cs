using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class EditorCommand
{
    public static void Pause(string project, string action = "toggle", bool json = false)
    {
        var request = CreatePauseRequest(action);
        CommandRunner.Execute(project, request, json);
    }

    public static void FocusGameView(string project, bool json = false)
    {
        var request = CreateFocusGameViewRequest();
        CommandRunner.Execute(project, request, json);
    }

    public static void FocusSceneView(string project, bool json = false)
    {
        var request = CreateFocusSceneViewRequest();
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreatePauseRequest(string action = "toggle")
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.EditorPause,
            Parameters = new JsonObject
            {
                ["action"] = action
            }
        };
    }

    internal static CommandRequest CreateFocusGameViewRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.EditorFocusGameView,
            Parameters = new JsonObject()
        };
    }

    internal static CommandRequest CreateFocusSceneViewRequest()
    {
        return new CommandRequest
        {
            Command = WellKnownCommands.EditorFocusSceneView,
            Parameters = new JsonObject()
        };
    }
}
