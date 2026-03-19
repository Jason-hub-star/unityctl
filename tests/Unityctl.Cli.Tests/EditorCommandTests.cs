using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class EditorCommandTests
{
    [Fact]
    public void CreatePauseRequest_HasCorrectCommand()
    {
        var request = EditorCommand.CreatePauseRequest();
        Assert.Equal(WellKnownCommands.EditorPause, request.Command);
        Assert.NotNull(request.RequestId);
    }

    [Fact]
    public void CreatePauseRequest_DefaultAction_IsToggle()
    {
        var request = EditorCommand.CreatePauseRequest();
        Assert.Equal("toggle", request.Parameters!["action"]!.ToString());
    }

    [Fact]
    public void CreatePauseRequest_SetsActionParameter()
    {
        var request = EditorCommand.CreatePauseRequest("pause");
        Assert.Equal("pause", request.Parameters!["action"]!.ToString());
    }

    [Fact]
    public void CreateFocusGameViewRequest_HasCorrectCommand()
    {
        var request = EditorCommand.CreateFocusGameViewRequest();
        Assert.Equal(WellKnownCommands.EditorFocusGameView, request.Command);
        Assert.NotNull(request.RequestId);
    }

    [Fact]
    public void CreateFocusSceneViewRequest_HasCorrectCommand()
    {
        var request = EditorCommand.CreateFocusSceneViewRequest();
        Assert.Equal(WellKnownCommands.EditorFocusSceneView, request.Command);
        Assert.NotNull(request.RequestId);
    }
}
