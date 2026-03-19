using System.Text.Json.Nodes;
using Unityctl.Cli.Execution;
using Unityctl.Shared.Protocol;

namespace Unityctl.Cli.Commands;

public static class ScreenshotCommand
{
    public static void Capture(
        string project,
        string view = "scene",
        int width = 1920,
        int height = 1080,
        string format = "png",
        int quality = 75,
        string? output = null,
        bool json = false)
    {
        var request = CreateCaptureRequest(view, width, height, format, quality, output);
        CommandRunner.Execute(project, request, json);
    }

    internal static CommandRequest CreateCaptureRequest(
        string view = "scene",
        int width = 1920,
        int height = 1080,
        string format = "png",
        int quality = 75,
        string? output = null)
    {
        var parameters = new JsonObject
        {
            ["view"] = view,
            ["width"] = width,
            ["height"] = height,
            ["format"] = format
        };

        if (string.Equals(format, "jpg", StringComparison.OrdinalIgnoreCase))
            parameters["quality"] = quality;

        if (!string.IsNullOrWhiteSpace(output))
            parameters["outputPath"] = output;

        return new CommandRequest
        {
            Command = WellKnownCommands.Screenshot,
            Parameters = parameters
        };
    }
}
