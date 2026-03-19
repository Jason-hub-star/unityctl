using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public class NavMeshCommandTests
{
    [Fact]
    public void CreateBakeRequest_HasCorrectCommand()
    {
        var request = NavMeshCommand.CreateBakeRequest();
        Assert.Equal(WellKnownCommands.NavMeshBake, request.Command);
        Assert.NotNull(request.RequestId);
    }

    [Fact]
    public void CreateClearRequest_HasCorrectCommand()
    {
        var request = NavMeshCommand.CreateClearRequest();
        Assert.Equal(WellKnownCommands.NavMeshClear, request.Command);
        Assert.NotNull(request.RequestId);
    }

    [Fact]
    public void CreateGetSettingsRequest_HasCorrectCommand()
    {
        var request = NavMeshCommand.CreateGetSettingsRequest();
        Assert.Equal(WellKnownCommands.NavMeshGetSettings, request.Command);
        Assert.NotNull(request.RequestId);
    }
}
