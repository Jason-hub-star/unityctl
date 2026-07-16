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
}
