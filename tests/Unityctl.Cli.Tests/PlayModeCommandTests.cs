using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class PlayModeCommandTests
{
    [Fact]
    public void CreateRequest_SetsAction()
    {
        var request = PlayModeCommand.CreateRequest("start");
        Assert.Equal(WellKnownCommands.PlayMode, request.Command);
        Assert.Equal("start", request.Parameters!["action"]!.ToString());
    }

    [Fact]
    public void CreateRequest_EmptyAction_Throws()
    {
        Assert.Throws<ArgumentException>(() => PlayModeCommand.CreateRequest(""));
    }

    [Fact]
    public void CreateStepRequest_HasStepAction()
    {
        var request = PlayModeCommand.CreateStepRequest(1);
        Assert.Equal(WellKnownCommands.PlayMode, request.Command);
        Assert.Equal("step", request.Parameters!["action"]!.ToString());
    }

    [Fact]
    public void CreateStepRequest_DefaultOneFrame_OmitsFramesParam()
    {
        var request = PlayModeCommand.CreateStepRequest(1);
        Assert.Null(request.Parameters!["frames"]);
    }

    [Fact]
    public void CreateStepRequest_MultipleFrames_SetsFramesParam()
    {
        var request = PlayModeCommand.CreateStepRequest(30);
        Assert.Equal(30, request.Parameters!["frames"]!.GetValue<int>());
    }
}
