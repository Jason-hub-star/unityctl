using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public sealed class ScreenshotCommandTests
{
    [CliTestFact]
    public void CreateCaptureRequest_Default_HasCorrectCommand()
    {
        var request = ScreenshotCommand.CreateCaptureRequest();

        Assert.Equal(WellKnownCommands.Screenshot, request.Command);
    }

    [CliTestFact]
    public void CreateCaptureRequest_Default_HasSceneView()
    {
        var request = ScreenshotCommand.CreateCaptureRequest();

        Assert.Equal("scene", request.Parameters!["view"]?.GetValue<string>());
    }

    [CliTestFact]
    public void CreateCaptureRequest_WithGameView_IncludesViewType()
    {
        var request = ScreenshotCommand.CreateCaptureRequest(view: "game");

        Assert.Equal("game", request.Parameters!["view"]?.GetValue<string>());
    }

    [CliTestFact]
    public void CreateCaptureRequest_WithDimensions_IncludesWidthHeight()
    {
        var request = ScreenshotCommand.CreateCaptureRequest(width: 800, height: 600);

        Assert.Equal(800, request.Parameters!["width"]?.GetValue<int>());
        Assert.Equal(600, request.Parameters!["height"]?.GetValue<int>());
    }

    [CliTestFact]
    public void CreateCaptureRequest_DefaultDimensions_Is1920x1080()
    {
        var request = ScreenshotCommand.CreateCaptureRequest();

        Assert.Equal(1920, request.Parameters!["width"]?.GetValue<int>());
        Assert.Equal(1080, request.Parameters!["height"]?.GetValue<int>());
    }

    [CliTestFact]
    public void CreateCaptureRequest_WithPngFormat_IncludesFormat()
    {
        var request = ScreenshotCommand.CreateCaptureRequest(format: "png");

        Assert.Equal("png", request.Parameters!["format"]?.GetValue<string>());
    }

    [CliTestFact]
    public void CreateCaptureRequest_WithJpgFormat_IncludesQuality()
    {
        var request = ScreenshotCommand.CreateCaptureRequest(format: "jpg", quality: 50);

        Assert.Equal("jpg", request.Parameters!["format"]?.GetValue<string>());
        Assert.Equal(50, request.Parameters!["quality"]?.GetValue<int>());
    }

    [CliTestFact]
    public void CreateCaptureRequest_WithPngFormat_OmitsQuality()
    {
        var request = ScreenshotCommand.CreateCaptureRequest(format: "png");

        Assert.Null(request.Parameters!["quality"]);
    }

    [CliTestFact]
    public void CreateCaptureRequest_WithOutput_IncludesOutputPath()
    {
        var request = ScreenshotCommand.CreateCaptureRequest(output: "/tmp/screenshot.png");

        Assert.Equal("/tmp/screenshot.png", request.Parameters!["outputPath"]?.GetValue<string>());
    }

    [CliTestFact]
    public void CreateCaptureRequest_NoOutput_OmitsOutputPath()
    {
        var request = ScreenshotCommand.CreateCaptureRequest();

        Assert.Null(request.Parameters!["outputPath"]);
    }

    [CliTestFact]
    public void CreateCaptureRequest_WithOverlayFlag_IncludesOverlayParameter()
    {
        var request = ScreenshotCommand.CreateCaptureRequest(view: "game", includeOverlayUi: true);

        Assert.True(request.Parameters!["includeOverlayUi"]?.GetValue<bool>());
    }
}
