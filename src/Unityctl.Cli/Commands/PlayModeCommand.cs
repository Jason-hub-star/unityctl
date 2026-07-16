using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class PlayModeCommand
{
    public static void Execute(string project, string action, bool json = false)
    {
        var request = CreateRequest(action);
        CommandRunner.Execute(project, request, json);
    }

    public static void Step(string project, int frames = 1, bool json = false)
    {
        var request = CreateStepRequest(frames);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateStepRequest(int frames)
    {
        var parameters = new JsonObject { ["action"] = "step" };
        if (frames > 1) parameters["frames"] = frames;
        return new CommandRequest
        {
            Command = WellKnownCommands.PlayMode,
            Parameters = parameters
        };
    }

    internal static CommandRequest CreateRequest(string action)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("action must not be empty", nameof(action));

        return new CommandRequest
        {
            Command = WellKnownCommands.PlayMode,
            Parameters = new JsonObject { ["action"] = action }
        };
    }
}
