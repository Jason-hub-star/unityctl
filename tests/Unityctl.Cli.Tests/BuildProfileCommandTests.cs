using Unityctl.Cli.Commands;
using Unityctl.Shared.Protocol;
using Xunit;

namespace Unityctl.Cli.Tests;

public sealed class BuildProfileCommandTests
{
    [CliTestFact]
    public void CreateListRequest_HasCorrectCommand()
    {
        var request = BuildProfileCommand.CreateListRequest();

        Assert.Equal(WellKnownCommands.BuildProfileList, request.Command);
        Assert.NotNull(request.Parameters);
        Assert.Empty(request.Parameters!);
    }

    [CliTestFact]
    public void CreateGetActiveRequest_HasCorrectCommand()
    {
        var request = BuildProfileCommand.CreateGetActiveRequest();

        Assert.Equal(WellKnownCommands.BuildProfileGetActive, request.Command);
        Assert.NotNull(request.Parameters);
        Assert.Empty(request.Parameters!);
    }

    [CliTestFact]
    public void CreateSetActiveRequest_HasCorrectCommandAndProfile()
    {
        var request = BuildProfileCommand.CreateSetActiveRequest("platform:Android");

        Assert.Equal(WellKnownCommands.BuildProfileSetActive, request.Command);
        Assert.Equal("platform:Android", request.Parameters!["profile"]?.GetValue<string>());
    }

    [CliTestFact]
    public async Task SetActiveAsync_InvalidTimeout_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            BuildProfileCommand.SetActiveAsync("C:/Project", "platform:Android", 0, json: true));
    }
}
