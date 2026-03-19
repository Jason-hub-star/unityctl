using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using ModelContextProtocol.Server;
using Unityctl.Core.Transport;
using Unityctl.Shared.Protocol;
using Unityctl.Shared.Serialization;

namespace Unityctl.Mcp.Tools;

[McpServerToolType]
internal sealed class ScreenshotTool(CommandExecutor executor)
{
    [McpServerTool(Name = "unityctl_screenshot_capture")]
    [Description("Capture a screenshot of the Unity Scene View or Game View camera and return as base64")]
    public async Task<string> CaptureAsync(
        [Description("Path to the Unity project directory")] string project,
        [Description("View to capture: scene or game (default: scene)")] string view = "scene",
        [Description("Image width in pixels (default: 1920)")] int width = 1920,
        [Description("Image height in pixels (default: 1080)")] int height = 1080,
        [Description("Image format: png or jpg (default: png)")] string format = "png",
        [Description("JPG quality 1-100 (default: 75, ignored for png)")] int quality = 75,
        CancellationToken cancellationToken = default)
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

        var request = new CommandRequest
        {
            Command = WellKnownCommands.Screenshot,
            Parameters = parameters
        };

        var response = await executor.ExecuteAsync(project, request, ct: cancellationToken);
        return JsonSerializer.Serialize(response, UnityctlJsonContext.Default.CommandResponse);
    }
}
