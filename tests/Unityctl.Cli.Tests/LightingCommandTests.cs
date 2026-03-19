using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class LightingCommandTests
{
    [Fact]
    public void CreateBakeRequest_HasCorrectCommand()
    {
        var request = LightingCommand.CreateBakeRequest();
        Assert.Equal(WellKnownCommands.LightingBake, request.Command);
        Assert.NotNull(request.RequestId);
    }

    [Fact]
    public void CreateCancelRequest_HasCorrectCommand()
    {
        var request = LightingCommand.CreateCancelRequest();
        Assert.Equal(WellKnownCommands.LightingCancel, request.Command);
        Assert.NotNull(request.RequestId);
    }

    [Fact]
    public void CreateClearRequest_HasCorrectCommand()
    {
        var request = LightingCommand.CreateClearRequest();
        Assert.Equal(WellKnownCommands.LightingClear, request.Command);
        Assert.NotNull(request.RequestId);
    }

    [Fact]
    public void CreateGetSettingsRequest_HasCorrectCommand()
    {
        var request = LightingCommand.CreateGetSettingsRequest();
        Assert.Equal(WellKnownCommands.LightingGetSettings, request.Command);
        Assert.NotNull(request.RequestId);
    }

    [Fact]
    public void CreateSetSettingsRequest_HasCorrectCommand()
    {
        var request = LightingCommand.CreateSetSettingsRequest("m_LightmapResolution", "40");
        Assert.Equal(WellKnownCommands.LightingSetSettings, request.Command);
        Assert.NotNull(request.RequestId);
    }

    [Fact]
    public void CreateSetSettingsRequest_EmptyProperty_Throws()
    {
        Assert.Throws<ArgumentException>(() => LightingCommand.CreateSetSettingsRequest("", "40"));
    }

    [Fact]
    public async Task BakeAsync_ZeroTimeout_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => LightingCommand.BakeAsync(".", 0, false));
    }
}
